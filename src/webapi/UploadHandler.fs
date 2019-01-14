module WebApi.UploadHandler

open System
open Giraffe
open WebApi
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2.ContextInsensitive
open WebApi.Helpers

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

(*
let validateTokenInContextItems basePath =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            match ctx.Request.Form
        }
        *)

let uploadWorkflow (basePath: string) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            do sprintf "%s" (ctx.Request.ContentType) |> ignore
            return! (match ctx.Request.HasFormContentType with
                     | false ->
                         do printfn "Request does not have form content-type."
                         (failWithStatusCodeAndMessage ctx next 500 "Form did not include any files.") next ctx
                     | true  -> 
                        printfn "Upload contains %d files." (ctx.Request.Form.Files.Count)
                        do ctx.Request.Form.Keys |> Seq.iter (printfn "%A")
                        ctx.Request.Form.Files
                        |> Seq.iter (fun file -> let stream = System.IO.File.OpenWrite(System.IO.Path.Combine(basePath, file.FileName))
                                                 do printfn "Upload for %s." file.FileName
                                                 file.CopyTo(stream)
                                                 stream.Flush()
                                                 stream.Close())
                        next ctx
                     )
        }