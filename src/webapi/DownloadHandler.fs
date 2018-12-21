module WebApi.DownloadHandler

open System
open System.Net.Mime
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.Extensions.Primitives
open Giraffe
open WebApi
open WebApi
open WebApi.Configuration
        
/// <summary>
/// Base path for all downloads. Is added to the filenames of all incoming requests.
/// </summary>        
let defaultBasePath = "/home/b0wter/tmp/torpedo"

/// <summary>
/// Default lifetime of a download. If the last file modification is older than this it is not considered a downloadable file.
/// </summary>
let defaultDownloadLifeTime = TimeSpan.FromDays(7.0)

/// <summary>
/// Default lifetime for a token once the first download has been attempted.
/// </summary>
let defaultTokenLifeTime = TimeSpan.FromDays(2.0)

/// <summary>
/// Creates a 404 response for the given context.
/// </summary>
let create404 (ctx: HttpContext) (next: HttpFunc) responseText =
    ctx.Response.StatusCode <- 404
    Views.badRequestView responseText |> htmlView
    // (text responseText) next ctx    

/// <summary>
/// Asynchronously writes a FileStream into a HTTP response.
/// </summary>
let private getFileStreamResponseAsync file downloadname (ctx: HttpContext) (next: HttpFunc) =
    task {
        let stream = FileAccess.fileStream file
        match stream with 
        | Some stream ->
            do ctx.Response.ContentType <- "application/octet-stream"
            let disposition = System.Net.Mime.ContentDisposition(FileName = downloadname, Inline = false)
            do ctx.Response.Headers.Add("Content-Disposition", StringValues(disposition.ToString()));
            return! ctx.WriteStreamAsync
                true 
                stream
                None
                None
        | None ->
            return! (create404 ctx next "Download stream not set. Please contact the administrator.") next ctx
    }
        
/// <summary>
/// Handler for download requests.
/// </summary>        
let handleGetFileDownload : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        let notAvailableBecause = create404 ctx next
        
        match (ctx.GetQueryStringValue "filename", ctx.GetQueryStringValue "token") with 
        | (Error _, _) -> notAvailableBecause "You need to supply a filename as query parameter." next ctx
        | (_, Error _) -> notAvailableBecause "You need to supply a token as query parameters." next ctx
        | (Ok filename, Ok tokenValue) ->
            let filename = System.Net.WebUtility.UrlDecode(filename)
            if filename.Contains("..") then 
                notAvailableBecause "You cannot use '..' in filenames." next ctx
            else
                // Create some abbreviations.
                let basePath = Configuration.Instance.BasePath
                let fullpath = IO.Path.Combine(basePath, filename)
                let basePath = IO.Path.GetDirectoryName(fullpath)
                let filename = IO.Path.GetFileName(fullpath);
                let downloadLifeTime = Configuration.Instance.DefaultDownloadLifeTime
                let tokenLifeTime = Configuration.Instance.DefaultTokenLifeTime
                
                // Get the tuple containing the token and content filename.
                let downloadPair = basePath 
                                   |> FileAccess.getFilesWithTokens
                                   |> Helpers.mapFilterRemap (fun (tokenfile, contentfile) -> (FileAccess.getLastModified tokenfile, (tokenfile, contentfile)))
                                                             (fun (lastmodified, tuple) -> tuple)
                                                             (fun (lastmodified, _) -> (DateTime.Now - downloadLifeTime) <= lastmodified)
                                   |> Seq.tryFind (fun (_, contentfile) -> IO.Path.GetFileName(contentfile) = filename)
                                   
                match downloadPair with 
                | Some (tokenfilename, contentfilename) ->
                    let tokenResult = tokenfilename |> FileAccess.getTextContent |> TokenSerializer.AsTotal |> (TokenSerializer.deserializeToken tokenfilename)
                    match tokenResult with 
                    | Ok token ->
                        if token.Values |> Seq.map (fun v -> v.Value) |> Seq.contains tokenValue then
                            
                            // persist the token with its new expiration date (new expiration date is only set if it doesnt already exist, see method for details)
                            Tokens.setExpirationTimeSpan tokenLifeTime token tokenValue
                            |> TokenSerializer.serializeToken
                            |> FileAccess.persistStringAsFile token.Filename
                            
                            getFileStreamResponseAsync contentfilename filename ctx next 
                        else 
                            notAvailableBecause "Unknown token." next ctx
                    | Error err ->
                        notAvailableBecause "Could not read token file. Please contact the system administrator." next ctx
                | None ->
                    notAvailableBecause (sprintf "The download is either unknown or has expired. The default lifetime of a download is %.1f days and it will expire %.1f days after you first download attempt." downloadLifeTime.TotalDays tokenLifeTime.TotalDays) next ctx
