module WebApi.DownloadHandler

open System
open System.Net.Mime
open System.Numerics
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.Extensions.Primitives
open Giraffe
open WebApi

/// <summary>
/// Sets the error status code and the error message in the given context.
/// </Summary>
let private failWithStatusCodeAndMessage (ctx: HttpContext) (next: HttpFunc) (statusCode: int) (message: string) =
    do ctx.SetStatusCode(statusCode)
    do ctx.Items.Add("errormessage", message)
    next ctx

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
            return! (failWithStatusCodeAndMessage ctx next 500 "Download stream not set. Please contact the administrator.")
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
    (basePath, filename)

/// <summary>
/// Checks if the given HttpContext contains all of the given parameters als query parameters.
/// Adds the parameter names and their values to the HttpContext.Items.
/// </summary>
let requiresQueryParameters (parameters: string seq) (addToContex: bool) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let queryParameters = parameters |> Seq.map (fun p -> (p, p |> ctx.TryGetQueryStringValue))
            
            if queryParameters |> Seq.map snd |> Helpers.containsNone then 
                return None
            else 
                let parametersToAdd = if addToContex then queryParameters else Seq.empty
                parametersToAdd 
                |> Seq.filter (fun (_, result) -> match result with 
                                                  | Some s -> true 
                                                  | None   -> false)
                |> Seq.map (fun (name, result) -> (name, Option.get result))
                |> Seq.iter (fun (name, value) -> ctx.Items <- Helpers.addIfNotExisting ctx.Items name value)
                
                return! next ctx
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
        
/// <summary>
/// Searches for a given file, checks for valid tokens and returns a FileStream.
/// </summary>        
let getDownloadFilestream (basePath: string) (downloadLifeTime: TimeSpan) (tokenLifeTime: TimeSpan) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let filename, tokenValue = (ctx.Items.["filename"].ToString(), ctx.Items.["token"].ToString())
            let basePath, filename = filename |> createCompletePathAndFilename basePath
            let downloadPair = basePath 
                               |> FileAccess.getFilesWithTokens
                               |> Helpers.mappedFilter (fun (tokenfile, contentfile) -> (FileAccess.getLastModified tokenfile, (tokenfile, contentfile)))
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
                        
                        return! getFileStreamResponseAsync contentfilename filename ctx next 
                    else 
                        return! failWithStatusCodeAndMessage ctx next 404 "Unknown token."
                | Error err ->
                    return! failWithStatusCodeAndMessage ctx next 500 "Could not read token file. Please contact the system administrator."
            | None ->
                return! failWithStatusCodeAndMessage ctx next 400 (sprintf "The download is either unknown or has expired. The default lifetime of a download is %.1f days and it will expire %.1f days after you first download attempt." downloadLifeTime.TotalDays tokenLifeTime.TotalDays)
        }        
