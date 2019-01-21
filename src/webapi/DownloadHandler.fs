module WebApi.DownloadHandler

open System
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.Extensions.Primitives
open Giraffe
open WebApi
open WebApi.Helpers

/// <summary>
/// Definition of an error which defines a statuscode and a reason.
/// </summary>
type DownloadError = {
    StatusCode: int;
    Reason: string;
}

/// <summary>
/// Asynchronously writes a FileStream into a HTTP response.
/// </summary>
let private getFileStreamResponseAsync folder file downloadname (ctx: HttpContext) (next: HttpFunc) =
    task {
        let stream = FileAccess.fileStream folder file
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
            //return! (failWithStatusCodeAndMessage ctx next 500 "Download stream not set. Please contact the administrator.")
            return! next ctx
            
    }
        
/// <summary>
/// Uses the given filename and the base path from the configuration too add these to
/// and retrieve the path and the filename.
/// </summary>
/// <remarks>
/// This is useful because the filename might contains a relative path oder a folder name.
/// </summary>        
let private createCompletePathAndFilename basePath filename =
    let fullpath = IO.Path.Combine(basePath, filename)
    let basePath = IO.Path.GetDirectoryName(fullpath)
    let filename = IO.Path.GetFileName(fullpath);
    (basePath, filename, fullpath)

/// <summary>
/// Checks if the given HttpContext contains all of the given parameters als query parameters.
/// Adds the parameter names and their values to the HttpContext.Items.
/// </summary>
let requiresQueryParameters (parameters: string seq) (addToContex: bool) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            match Helpers.requireParamterIn (ctx.TryGetQueryStringValue) ctx parameters addToContex with
            | Some context -> return! next context
            | None         -> return None
        }

/// <summary>
/// Checks if the context contains a filename item and if that filename points to an existing file.
/// </summary>
let requiresExistanceOfFileInContext (basePath: string) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            if ctx.Items.ContainsKey("filename") then 
                let filename = ctx.Items.["filename"].ToString()
                let filename = IO.Path.Combine(basePath, filename)
                if filename |> FileAccess.fileExists then 
                    return! (next ctx)
                else
                    ctx.Items.Add("errormessage", "File does not exist.")
                    return None
            else 
                ctx.Items.Add("errormessage", "Context does not contain filename item.")
                return None
        }
        
        
let (>>=) twoTrackInput switchFunction =
    Helpers.bind switchFunction twoTrackInput
        
let getDownloadPair (downloadLifeTime : TimeSpan) (tokenLifeTime : TimeSpan) path filename : Result<FileAccess.DownloadPair, DownloadError> =
    let pair = path 
               |> FileAccess.getFilesWithTokens
               |> Helpers.mappedFilter (fun (tokenfile, contentfile) -> (FileAccess.getLastModified tokenfile, (tokenfile, contentfile)))
                                       (fun (lastmodified, _) -> (DateTime.Now - downloadLifeTime) <= lastmodified)
               |> Seq.tryFind (fun (_, contentfile) -> IO.Path.GetFileName(contentfile) = filename)
    match pair with
    | Some p -> Ok p
    | None   -> Error { StatusCode = 400; Reason = (sprintf "The download is either unknown or has expired. The default lifetime of a download is %.1f days and it will expire %.1f days after you first download attempt." downloadLifeTime.TotalDays tokenLifeTime.TotalDays) }
    
let deserializeToken (pair : FileAccess.DownloadPair) : Result<Tokens.Token, DownloadError> =
    let tokenfile, contentfile = pair
    let result = tokenfile |> FileAccess.getTextContent |> TokenSerializer.AsTotal |> (TokenSerializer.deserializeToken tokenfile contentfile)
    match result with
    | Ok token  -> Ok token
    | Error msg -> Error { StatusCode = 500; Reason = "Could not read the token file. Please contact the administrator." }
    
let checkTokenValue (value: string) (token: Tokens.Token) : Result<Tokens.Token, DownloadError> =
    let result = token.Values |> Seq.map (fun v -> v.Value) |> Seq.contains value
    match result with
    | true  -> Ok token
    | false -> Error { StatusCode = 404; Reason = "Unknown token." }
    
let expireToken (tokenValue: string) (tokenLifeTime: TimeSpan) (token: Tokens.Token) : Result<Tokens.Token, DownloadError> =
    Ok (Tokens.setExpirationTimeSpan tokenLifeTime token tokenValue)
    
let persistToken (token: Tokens.Token) : Result<Tokens.Token, DownloadError> =    
    match token |> TokenSerializer.serializeToken |> FileAccess.persistStringAsFile token.TokenFilename with
    | true  -> Ok token
    | false -> Error { StatusCode = 500; Reason = "Could not write token file. Please contact the administrator." }
    
let downloadWorkflow (basePath: string) (downloadLifeTime: TimeSpan) (tokenLifeTime: TimeSpan) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let downloadPairFrom = (getDownloadPair downloadLifeTime tokenLifeTime)
            let rawFilename, tokenValue = (ctx.Items.["filename"].ToString(), ctx.Items.["token"].ToString())
            let basePath, filename, fullFilename = rawFilename |> createCompletePathAndFilename basePath
            
            let d = (downloadPairFrom basePath filename)
                    >>= deserializeToken
                    >>= (checkTokenValue tokenValue)
                    >>= (expireToken tokenValue tokenLifeTime)
                    >>= persistToken
          
            match d with
            | Ok token -> return! (getFileStreamResponseAsync basePath filename filename ctx next)
            | Error e  -> return! (failWithStatusCodeAndMessage ctx next e.StatusCode e.Reason next ctx)
        }
