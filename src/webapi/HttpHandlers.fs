namespace WebApi

module HttpHandlers =

    open Microsoft.AspNetCore.Http
    open Giraffe
    open WebApi
            
    let renderErrorCode : HttpHandler =
        fun (next: HttpFunc) (ctx: HttpContext) ->
            match ctx.Response.StatusCode with 
            | 400 ->
                let message = if ctx.Items.ContainsKey("errormessage") then ctx.Items.["errormessage"].ToString() else "Your request is invalid. Please try again or contact the site administrator."
                let view = (Views.badRequestView message) |> htmlView
                (view next ctx)
            | 404 ->
                let message = if ctx.Items.ContainsKey("errormessage") then ctx.Items.["errormessage"].ToString() else "The given resource could not be found."
                let view = (Views.notFoundView message) |> htmlView
                (view next ctx)
            | 500 ->
                let message = if ctx.Items.ContainsKey("errormessage") then ctx.Items.["errormessage"].ToString() else "An internal server error has occured. Please contact the system administrator."
                let view = (Views.internalErrorView message) |> htmlView
                (view next ctx)
            | _ ->
                next ctx
