module Torpedo.App

open System
open System.Globalization
open System.Reflection.Metadata
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.StaticFiles
open Giraffe
open Microsoft.Extensions.Configuration
open Torpedo
open WebApi

let mutable config: IConfiguration option = None

// ---------------------------------
// Web app
// ---------------------------------

let invariant = CultureInfo.InvariantCulture

let webApp =
    choose [
        route "/api/download" >=> DownloadHandler.handleGetFileDownload
        (*
        subRoute "/api"
            (choose [
                GET >=> choose [
                    route  "/download(/?)" >=> DownloadHandler.handleGetFileDownload           //>=> setStatusCode 500 >=> (Views.badRequestView "Download endpoint requires a token as route parameter." |> htmlView) 
                    //routex "/download/([^\/]*)(/?)" >=> setStatusCode 500 >=> (Views.badRequestView "To download a file you need to supply an url encoded filename and a download token as route parameters." |> htmlView)
                    //routef "/download/%s/%s"        handleGetFileDownload
                ]
                //POST >=> choose [
                //    route "/download(/?)"           >=> bindForm<Models.Download> (Some invariant) (fun d -> handleGetFileDownload (d.Filename, d.Token))
                //]
            ])
            *)
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

let tryBindConfiguration (config: IConfiguration option) =
    match config with 
    | Some options -> do options.GetSection("configuration").Bind(WebApi.Configuration.Configuration.Instance)
    | None -> ()

let configureApp (app : IApplicationBuilder) =  
    do tryBindConfiguration config
      
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
    do config <- Some (services.BuildServiceProvider().GetService<IConfiguration>())

let configureLogging (builder : ILoggingBuilder) =
    builder.AddFilter(fun l -> l.Equals LogLevel.Error)
           .AddConsole()
           .AddDebug() |> ignore

let buildIConfiguration = 
    ConfigurationBuilder().SetBasePath(System.IO.Directory.GetCurrentDirectory()).AddJsonFile("config.json", false, true).Build()

[<EntryPoint>]
let main _ =
    WebHostBuilder()
        .UseKestrel()
        .UseIISIntegration()
        .UseConfiguration(buildIConfiguration)
        .UseWebRoot(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot"))
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)
        .ConfigureLogging(configureLogging)
        .Build()
        .Run()
    0
