namespace Torpedo

module HttpHandlers =

    open System.Net.Mime
    open Microsoft.AspNetCore.Http
    open FSharp.Control.Tasks.V2.ContextInsensitive
    open Microsoft.Extensions.Primitives
    open Giraffe
    open Torpedo.Models
    open WebApi
    open WebApi

    let handleGetHello : HttpHandler =
        fun (next : HttpFunc) (ctx : HttpContext) ->
            task {
                let response = {
                    Text = "Hello world, from Giraffe!"
                }
                return! json response next ctx
            }
            
    let handleGetDownload: HttpHandler =
        fun (next: HttpFunc) (ctx: HttpContext) ->
            task {
                let file = "cook.png"
                let stream = FileAccess.fileStream file
                match stream with 
                | Some stream ->
                    do ctx.Response.ContentType <- "application/octet-stream"
                    let disposition = System.Net.Mime.ContentDisposition(FileName = file, Inline = false)
                    do ctx.Response.Headers.Add("Content-Disposition", StringValues(disposition.ToString()));
                    return! ctx.WriteStreamAsync
                        true 
                        stream
                        None
                        None
                | None ->
                    return! json "Not found" next ctx
            }
            (*
    let handleGetDownload: HttpHandler =
        let stream = FileAccess.fileStream "gitkraken.rpm"
        match stream with
        | Some stream ->
            streamFile
                true 
                "/home/b0wter/tmp/torpedo/gitkraken.rpm"
                None
                None
        | None ->
            json "Not found"
            *)
