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

let failWithStatusCodeAndMessage (ctx: HttpContext) (next: HttpFunc) (statusCode: int) (message: string) =
    do ctx.SetStatusCode(500)
    do ctx.Items.Add("errormessage", "You cannot use '...' in filenames.")
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
            return! (create404 ctx next "Download stream not set. Please contact the administrator.") next ctx
    }
        
[<Obsolete>]        
let getQueryparameters (ctx: HttpContext) : Result<(string * string), string> =
    match (ctx.GetQueryStringValue "filename", ctx.GetQueryStringValue "token") with 
    | (Error _, _) -> Error "You need to supply a filename as query parameter."
    | (_, Error _) -> Error "You need to supply a token as query parameter."
    | (Ok filename, Ok tokenvalue) -> Ok (filename, tokenvalue)
        
/// <summary>
/// Uses the given filename and the base path from the configuration too add these to
/// and retrieve the path and the filename.
/// </summary>
/// <remarks>
/// This is useful because the filename might contains a relative path oder a folder name.
/// </summary>        
let createCompletePathAndFilename filename =
    let basePath = Configuration.Instance.BasePath
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

[<Obsolete>]
let requiresFilenameAndTokenQueryParameters : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            match (ctx.GetQueryStringValue "filename", ctx.GetQueryStringValue "token") with 
            | (Error _, _) -> return None
            | (_, Error _) -> return None
            | (Ok filename, Ok tokenValue) ->
                do ctx.Items.Add("filename", filename)
                do ctx.Items.Add("tokenvalue", tokenValue)
                return! next ctx
        }
        
let requiresExistanceOfFileInContext : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            if ctx.Items.ContainsKey("filename") then 
                let filename = ctx.Items.["filename"].ToString()
                if filename |> IO.File.Exists then 
                    return! (next ctx)
                else
                    ctx.Items.Add("errormessage", "File does not exist.")
                    return None
            else 
                ctx.Items.Add("errormessage", "Context does not contain filename item.")
                return None
        }
        
let getDownloadFilestream : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let filename, tokenValue = (ctx.Items.["filename"].ToString(), ctx.Items.["token"].ToString())
            
            if filename.Contains("..") then 
                return! failWithStatusCodeAndMessage ctx next 400 "You cannot use '..' in filenames."
            else
                let basePath, filename = filename |> createCompletePathAndFilename 
                let downloadLifeTime = Configuration.Instance.DefaultDownloadLifeTime
                let tokenLifeTime = Configuration.Instance.DefaultTokenLifeTime
                
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
        
/// <summary>
/// Handler for download requests.
/// </summary>        
let handleGetFileDownload : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        let notAvailableBecause = create404 ctx next
        
        do printfn "%A" (ctx.Items.["filename"])
        do printfn "%A" (ctx.Items.["tokenvalue"])
        
        match (ctx.GetQueryStringValue "filename", ctx.GetQueryStringValue "token") with 
        | (Error _, _) -> notAvailableBecause "You need to supply a filename as query parameter." next ctx
        | (_, Error _) -> notAvailableBecause "You need to supply a token as query parameters." next ctx
        | (Ok filename, Ok tokenValue) ->
            let filename = System.Net.WebUtility.UrlDecode(filename)
            if filename.Contains("..") then 
                failWithStatusCodeAndMessage ctx next 500 "You cannot use '..' in filenames."
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
                            
                            getFileStreamResponseAsync contentfilename filename ctx next 
                        else 
                            notAvailableBecause "Unknown token." next ctx
                    | Error err ->
                        notAvailableBecause "Could not read token file. Please contact the system administrator." next ctx
                | None ->
                    notAvailableBecause (sprintf "The download is either unknown or has expired. The default lifetime of a download is %.1f days and it will expire %.1f days after you first download attempt." downloadLifeTime.TotalDays tokenLifeTime.TotalDays) next ctx
