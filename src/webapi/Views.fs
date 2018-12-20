module WebApi.Views

open Giraffe
open Giraffe
open Giraffe.GiraffeViewEngine

let footer = tag "footer"

let masterView (content: XmlNode list) =
    html [ _id "background" ] [
        head [] [
            title [] [ str "Giraffe Sample" ]
            link [ _rel "stylesheet"
                   _type "text/css"
                   _href "/css/mini-dark.min.css" ]       
                   
            link [ _rel "stylesheet"
                   _type "text/css"
                   _href "/css/custom.css" ]       
        ]
        body [ _class "transparent" ] [
        
            div [ _id "outer" ] [
                div [ _id "middle" ] [
                    div [ _id "inner" ]
                        content
                ]
            ]
        ]
    ]
    
let indexView =
    [
        h1 [] [ str "Welcome to Torpedo" ]
        p [] [ str "Enter the filename and your download tokens into the fields below." ]
        div [ _class "input-group vertical" ] [
            input [ _type "text"; _class "transparent button-margin"; _id "Filename"; _placeholder "Filename" ]
            input [ _type "text"; _class "transparent button-margin"; _id "Token"; _placeholder "Token" ]
            button [ ] [ str "Download" ]
        ]
    ]
    |> masterView
    
let badRequestView (message: string) =
    [
        h1 [] [ str "500"]
        p [] [ str message ]
    ]    
    |> masterView
    
let notFoundView (message: string) =
    [
        h1 [] [ str "404" ]
        p [] [ str message ]
    ]
    |> masterView