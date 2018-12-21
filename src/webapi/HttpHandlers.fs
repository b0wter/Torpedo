namespace WebApi
open Giraffe.HttpStatusCodeHandlers

module HttpHandlers =

    open System
    open System.Net.Mime
    open Microsoft.AspNetCore.Http
    open FSharp.Control.Tasks.V2.ContextInsensitive
    open Microsoft.Extensions.Primitives
    open Giraffe
    open WebApi
            
// Room for additional HTTP handlers (non api).
