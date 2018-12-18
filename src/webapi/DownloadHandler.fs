module WebApi.DownloadHandler

open System
open System.Net.Mime
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.Extensions.Primitives
open Giraffe
open WebApi
        
/// <summary>
/// Base path for all downloads. Is added to the filenames of all incoming requests.
/// </summary>        
let basePath = "/home/b0wter/tmp/torpedo"

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
    (text responseText) next ctx    

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
            return! (create404 ctx next "Download stream not set. Please contact the administrator.")
    }
        
/// <summary>
/// Handler for download requests.
/// </summary>        
let handleGetFileDownload (filename: string, tokenValue: string) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        let notAvailableBecause = create404 ctx next
        let downloadPair = basePath 
                           |> FileAccess.getFilesWithTokens
                           |> Helpers.mapFilterRemap (fun (tokenfile, contentfile) -> (FileAccess.getLastModified tokenfile, (tokenfile, contentfile)))
                                                     (fun (lastmodified, tuple) -> tuple)
                                                     (fun (lastmodified, _) -> (DateTime.Now - defaultDownloadLifeTime) <= lastmodified)
                           |> Seq.tryFind (fun (_, contentfile) -> IO.Path.GetFileName(contentfile) = filename)
        match downloadPair with 
        | Some (tokenfilename, contentfilename) ->
            let tokenResult = tokenfilename |> FileAccess.getTextContent |> TokenSerializer.AsTotal |> (TokenSerializer.deserializeToken tokenfilename)
            match tokenResult with 
            | Ok token ->
                if token.Values |> Seq.map (fun v -> v.Value) |> Seq.contains tokenValue then
                    
                    // persist the token with its new expiration date
                    Tokens.setExpirationTimeSpan defaultTokenLifeTime token tokenValue
                    |> TokenSerializer.serializeToken
                    |> FileAccess.persistStringAsFile token.Filename
                    
                    getFileStreamResponseAsync contentfilename filename ctx next 
                else 
                    notAvailableBecause "Unknown token."
            | Error err ->
                notAvailableBecause "Could not read token file. Please contact the system administrator."
        | None ->
            notAvailableBecause (sprintf "The download is either unknown or has expired. The default lifetime of a download is %f days." defaultDownloadLifeTime.TotalDays)
