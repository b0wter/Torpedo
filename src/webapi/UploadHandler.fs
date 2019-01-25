module WebApi.UploadHandler

open System
open System.IO
open Giraffe
open WebApi
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2.ContextInsensitive
open Giraffe
open WebApi.Helpers
open WebApi.TokenSerializer

let requiresFormParameters (parameters: string seq) (addToContext: bool) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let findInFormItems (ctx: HttpContext) key =
                match ctx.Request.ContentType.StartsWith("multipart/form-data") with
                | true ->
                    if ctx.Request.Form.ContainsKey(key) then
                        Some ctx.Request.Form.[key].[0]
                    else
                        None
                | false ->
                    None
                    
            match Helpers.requireParamterIn (findInFormItems ctx) ctx parameters addToContext with
            | Some context -> return! next context
            | None         -> return! (failWithStatusCodeAndMessage ctx next 400 "You have not supplied a token." next ctx)
        }

let validateTokenInContextItems basePath =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            // TODO: needs implementation!
            return! next ctx
        }
        
let uploadWorkflow (basePath: string) : HttpHandler =
    // TODO: Remove references to System.IO methods: Path.Combine & Path.GetDirectory.
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            return! (match ctx.Request.HasFormContentType with
                     | false ->
                         do printfn "Request does not have form content-type."
                         (failWithStatusCodeAndMessage ctx next 500 "Form did not include any files.") next ctx
                     | true  -> 
                        do printfn "Upload contains %d files." (ctx.Request.Form.Files.Count)
                        let path = ctx.Items.["folder"].ToString() |> System.IO.Path.GetDirectoryName
                        ctx.Request.Form.Files
                        |> Seq.iter (fun file ->
                                                 let newName = FileAccess.getUniqueFilename (System.IO.Path.Combine(path, file.FileName))
                                                 let stream = System.IO.File.OpenWrite(newName)
                                                 do printfn "Saving %s as %s." file.FileName newName
                                                 file.CopyTo(stream)
                                                 stream.Flush()
                                                 stream.Close())
                        next ctx
                     )
        }
        
let validateUploadToken (basePath: string) (onSuccess: HttpHandler) (onError: HttpHandler): HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let files = basePath |> FileAccess.getFilesFromFolder
            let tokens = files
                         |> List.map (fun s -> s |> FileAccess.getTextContent |> TokenValueContent.AsTotal |> (TokenSerializer.deserializeToken s ""))
                         |> filterOks
                         
            let toSearch = (string)ctx.Items.["token"]
            let result = tokens |> Tokens.findTokenContainingValue toSearch
            match result with
            | None -> return! (onError next ctx) //("false" |> text) next ctx
            | Some token ->
                match token |> (Tokens.isTokenStillValid Configuration.Configuration.Instance.DownloadLifeTime) with
                | true ->
                    do ctx.Items.Add("folder", token.TokenFilename)
                    return! (onSuccess next ctx) //("true" |> text) next ctx
                | false ->
                    return! (onError next ctx) //("false" |> text) next ctx
        }