module Torpedo.App

open System
open System.Globalization
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe
open WebApi
open WebApi.DownloadHandler
open WebApi.UploadHandler
open WebApi.Helpers
open WebApi.Configuration
open Hangfire.MemoryStorage
open Hangfire
open Microsoft.AspNetCore.Http.Features
open Microsoft.Extensions.Hosting

// ---------------------------------
// Global configuration
// ---------------------------------

let invariant = CultureInfo.InvariantCulture

/// <summary>
/// Tries to retrieve the configuration filename from the environment variables.
/// Returns a fallback value if the variable is not set.
/// </summary>
let getConfigFile ()  =
    let fallbackName = "config.json"
    try
        let configFilename = Environment.GetEnvironmentVariable("TORPEDO_CONFIG")
        if configFilename |> String.IsNullOrWhiteSpace then
            printfn "Configuration file not specified through the environment. Will use fallback value '%s'." fallbackName
            fallbackName
        else
            configFilename
    with
    | :? Security.SecurityException ->
        printfn "The current user does not have access to the environment variables. Using default configuration file '%s'." fallbackName
        fallbackName
        
/// <summary>
/// Creates a configuration from a local configuration file.
/// Will also check environment variables.
/// </summary>
/// <remarks>
/// Does not use the default ASP .Net core way of configuration because the configuration needs to be initialized
/// before any other code is executed because functions like `downloadFile` are only initialized once.
/// Any change in the configuration is not reflected in the curried function.
/// </remarks>
let updateConfigFromLocalFile =
    let filename = getConfigFile ()
    let config = filename |> Configuration.FromConfigAndEnvironment 
    if config.IsValid then
        do Configuration.Instance <- config
        true
    else
        false
        
// ---------------------------------
// Web app
// ---------------------------------
let downloadFile =
    downloadWorkflow 
        Configuration.Instance.BasePath
        Configuration.Instance.DownloadLifeTime
        Configuration.Instance.TokenLifeTime
        
let requiresExistenceOfFile =
    requiresExistenceOfFileInContext
        Configuration.Instance.BasePath
        
let uploadFile : HttpFunc -> Microsoft.AspNetCore.Http.HttpContext -> HttpFuncResult =
    uploadWorkflow 
        Configuration.Configuration.Instance.BasePath
        
let validateUploadTokenAndExit : HttpHandler =
    validateUploadToken
        Configuration.Instance.BasePath
        (fun next ctx -> ("true" |> Core.text) next ctx)
        (fun next ctx -> ("false" |> Core.text) next ctx)
        
let validateUploadTokenAndContinue : HttpHandler =
    validateUploadToken
        Configuration.Instance.BasePath
        (fun next ctx -> next ctx)
        (fun next ctx -> ((Views.notFoundView "The given token is unknown.") |> htmlView) next ctx)
        
let webApp =
    choose [
        (* Route for the download api. *)
        route "/api/download"
            >=> requiresQueryParameters [| "filename"; "token" |] true
            >=> requiresExistenceOfFile
            >=> downloadFile 
        route "/api/download"
            >=> requiresQueryParameters [| "filename"; "token" |] true
            >=> (Views.notFoundView "The file could not be found." |> htmlView)
        route "/api/download"
            >=> requiresQueryParameters [| "filename" |] false
            >=> requiresExistenceOfFile >=> (Views.badRequestView "Your request is missing the 'token' query parameter." |> htmlView)
        route "/api/download"
            >=> requiresQueryParameters [| "token" |] false
            >=> (Views.badRequestView "Your request is missing the 'filename' query parameter." |> htmlView)
        route "/api/download"
            >=> (Views.badRequestView "Your request is missing the 'filename' as well as the 'token' query parameters." |> htmlView)
        route "/download"
            >=> (Views.indexView |> htmlView)
        
        (* Route for the upload api. *)
        route "/api/upload"
            >=> requiresFeatureEnabled (fun () -> Configuration.Instance.UploadsEnabled)
            >=> requiresFormParameters [| "token" |] true
            >=> (validateTokenInContextItems Configuration.Instance.BasePath)
            >=> validateUploadTokenAndContinue
            >=> uploadFile
            >=> (Views.uploadFinishedView |> htmlView)
        route "/api/upload"
            >=> (Views.featureNotEnabledview "file upload" |> htmlView)
        route "/api/upload/validate"
            >=> requiresFormParameters [| "token" |] true
            >=> validateUploadTokenAndExit
        route "/upload"
            >=> requiresFeatureEnabled (fun () -> Configuration.Instance.UploadsEnabled)
            >=> (Views.uploadView |> htmlView)
        route "/upload"
            >=> (Views.featureNotEnabledview "file upload" |> htmlView)
 
        (* general routes *)
        route "/"
            >=> redirectTo true "/download"
        route "/about"
            >=> (Views.aboutView |> htmlView)
        setStatusCode 404 >=> (Views.notFoundView "Page not found :(" |> htmlView)
    ]

// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

// ---------------------------------
// Crons
// ---------------------------------

let cleanOldDownloads () =
    Cleanup.cleanAll Configuration.Instance.BasePath
                     Configuration.Instance.TokenLifeTime
                     Configuration.Instance.DownloadLifeTime
                     
// ---------------------------------
// Config and Main
// ---------------------------------

let configureCors (builder : CorsPolicyBuilder) =
    builder.WithOrigins("http://localhost:8080")
           .AllowAnyMethod()
           .AllowAnyHeader()
           |> ignore

let configureApp (app : IApplicationBuilder) =  
    let env = app.ApplicationServices.GetService<IWebHostEnvironment>()
    (match env.IsDevelopment() with
    | true  -> app.UseDeveloperExceptionPage()
    | false -> app.UseGiraffeErrorHandler errorHandler)
        .UseHttpsRedirection()
        .UseStaticFiles()
        .UseCors(configureCors)
        .UseGiraffe(webApp)

let configureServices (services : IServiceCollection) =
    services.AddHangfire(
        fun c -> 
            c.UseMemoryStorage() |> ignore
            do RecurringJob.AddOrUpdate((fun () -> do cleanOldDownloads ()), Cron.Hourly Configuration.Instance.CronIntervalInHours)
        ) |> ignore
    services.AddCors() |> ignore
    services.AddGiraffe() |> ignore
    services.AddHangfireServer() |> ignore
    services.Configure<FormOptions>(fun (x: FormOptions) -> x.ValueLengthLimit <- Int32.MaxValue
                                                            x.MultipartBodyLengthLimit <- Int64.MaxValue)
    |> ignore

let configureLogging (builder : ILoggingBuilder) =
    builder.AddFilter(fun l -> l.Equals LogLevel.Error)
           .AddConsole()
           .AddDebug() |> ignore

/// <summary>
/// Prints a highlighted error message and then returns an error exit code.
/// Flushes the console output stream to make sure the colors are reset properly.
/// </summary>
let exitWithError error =
    do Console.ForegroundColor <- ConsoleColor.Red
    do printfn "%s" error
    do Console.ResetColor ()
    do Console.Out.Flush ()
    -1

/// <summary>
/// Creates the webserver.
/// </summary>
let buildKestrel () =
    WebHostBuilder()
     .UseKestrel(fun options -> options.Limits.MaxRequestBodySize <- System.Nullable())//Nullable<Int64>(Configuration.Instance.MaxUploadSize * 1024L * 1024L))
     .UseIISIntegration()
     .UseWebRoot(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot"))
     .Configure(Action<IApplicationBuilder> configureApp)
     .ConfigureServices(configureServices)
     .ConfigureLogging(configureLogging)
     .Build()

[<EntryPoint>]
let main _ =
    if updateConfigFromLocalFile then 
        let host = buildKestrel ()
        do host.Run ()
        0
    else
        exitWithError "The configuration is missing the `BasePath` property."
