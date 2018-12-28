﻿module Torpedo.App

open System
open System.Globalization
open System.IO
open System.Reflection.Metadata
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.StaticFiles
open Giraffe
open Microsoft.Extensions.Configuration
open Newtonsoft.Json
open Torpedo
open WebApi
open WebApi.DownloadHandler
open WebApi.HttpHandlers

// ---------------------------------
// Web app
// ---------------------------------

let invariant = CultureInfo.InvariantCulture

/// <summary>
/// Checks if the file set as base path in the configuration file exists.
/// This check is necessary due to a bug in the .net core 2.1 framework.
/// Throwing an exception or using Environment.Exit(...) leads to a deadlock
/// while shutting the application down.
/// </summary>
let updateConfigFromLocalFile =
    if System.IO.File.Exists("config.json") then 
        let config = System.IO.File.ReadAllText("config.json")
                     |> JsonConvert.DeserializeObject<Configuration.Configuration>
        do Configuration.Configuration.Instance <- config
        true
    else
        false
        
let downloadFile =
    getDownloadFilestream 
        Configuration.Configuration.Instance.BasePath
        Configuration.Configuration.Instance.DefaultDownloadLifeTime
        Configuration.Configuration.Instance.DefaultTokenLifeTime
        
let requiresExistanceOfFile =
    requiresExistanceOfFileInContext
        Configuration.Configuration.Instance.BasePath
        
let webApp =
    choose [
        route "/api/download" >=> requiresQueryParameters [| "filename"; "token" |] true  >=> requiresExistanceOfFile >=> downloadFile >=> renderErrorCode
        route "/api/download" >=> requiresQueryParameters [| "filename"; "token" |] true  >=> (Views.internalErrorView "The file could not be found." |> htmlView)
        route "/api/download" >=> requiresQueryParameters [| "filename" |]          false >=> requiresExistanceOfFile >=> (Views.badRequestView "Your request is missing the 'token' query parameter." |> htmlView)
        route "/api/download" >=> requiresQueryParameters [| "token" |]             false >=> (Views.badRequestView "Your request is missing the 'filename' query parameter." |> htmlView)
        route "/api/download" >=> (Views.badRequestView "Your request is missing the 'filename' as well as the 'token' query parameters." |> htmlView)
        
        route "/" >=> (Views.indexView |> htmlView)
        setStatusCode 404 >=> (Views.notFoundView "Page not found :(" |> htmlView) ]
        
// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

// ---------------------------------
// Config and Main
// ---------------------------------

let configureCors (builder : CorsPolicyBuilder) =
    builder.WithOrigins("http://localhost:8080")
           .AllowAnyMethod()
           .AllowAnyHeader()
           |> ignore

let configureApp (app : IApplicationBuilder) =  
    let env = app.ApplicationServices.GetService<IHostingEnvironment>()
    (match env.IsDevelopment() with
    | true  -> app.UseDeveloperExceptionPage()
    | false -> app.UseGiraffeErrorHandler errorHandler)
        .UseHttpsRedirection()
        .UseStaticFiles()
        .UseCors(configureCors)
        .UseGiraffe(webApp)

let configureServices (services : IServiceCollection) =
    services.AddCors()    |> ignore
    services.AddGiraffe() |> ignore

let configureLogging (builder : ILoggingBuilder) =
    builder.AddFilter(fun l -> l.Equals LogLevel.Error)
           .AddConsole()
           .AddDebug() |> ignore

/// <summar>
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
     .UseKestrel()
     .UseIISIntegration()
     .UseWebRoot(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot"))
     .Configure(Action<IApplicationBuilder> configureApp)
     .ConfigureServices(configureServices)
     .ConfigureLogging(configureLogging)
     .Build()

[<EntryPoint>]
let main _ =
    if updateConfigFromLocalFile then 
        if Configuration.Configuration.Instance.BasePath |> System.IO.Directory.Exists then
            do printfn "Contents of download folder (%s):" Configuration.Configuration.Instance.BasePath
            do System.IO.Directory.GetFiles(Configuration.Configuration.Instance.BasePath)
               |> Array.iter (fun file -> printfn "%s" file)
            
            let host = buildKestrel ()       
            do host.Run ()
            0
        else 
            exitWithError (sprintf "The 'BasePath' set in your 'config.json' does not exist or is not accesible.%sValue: %s" Environment.NewLine Configuration.Configuration.Instance.BasePath)
    else
        exitWithError "Could not find 'config.json' in the application startup path."
