module WebApi.Views

open System.Net
open System.Threading.Tasks
open FSharp.Control.Tasks.V2.ContextInsensitive
open Giraffe
open Giraffe.GiraffeViewEngine

let private headTags (scripts: string list) =
    [
        title [] [ str "Torpedo" ]
        
        link [ _rel "stylesheet"
               _type "text/css"
               _href "/css/mini-dark.min.css" ]       
               
        link [ _rel "stylesheet"
               _type "text/css"
               _href "/css/custom.css" ]       
               
        link [ _rel "apple-touch-icon"; _sizes "57x57";   _href "/images/favicon/apple-icon-57x57.png" ]
        link [ _rel "apple-touch-icon"; _sizes "60x60";   _href "/images/favicon/apple-icon-60x60.png" ]
        link [ _rel "apple-touch-icon"; _sizes "72x72";   _href "/images/favicon/apple-icon-72x72.png" ]
        link [ _rel "apple-touch-icon"; _sizes "76x76";   _href "/images/favicon/apple-icon-76x76.png" ]
        link [ _rel "apple-touch-icon"; _sizes "114x114"; _href "/images/favicon/apple-icon-114x114.png" ]
        link [ _rel "apple-touch-icon"; _sizes "120x120"; _href "/images/favicon/apple-icon-120x120.png" ]
        link [ _rel "apple-touch-icon"; _sizes "144x144"; _href "/images/favicon/apple-icon-144x144.png" ]
        link [ _rel "apple-touch-icon"; _sizes "152x152"; _href "/images/favicon/apple-icon-152x152.png" ]
        link [ _rel "apple-touch-icon"; _sizes "180x180"; _href "/images/favicon/apple-icon-180x180.png" ]
        link [ _rel "icon"; _type "image/png"; _sizes "192x192";  _href "/images/favicon/android-icon-192x192.png" ]
        link [ _rel "icon"; _type "image/png"; _sizes "32x32";    _href "/images/favicon/favicon-32x32.png" ]
        link [ _rel "icon"; _type "image/png"; _sizes "96x96";    _href "/images/favicon/favicon-96x96.png" ]
        link [ _rel "icon"; _type "image/png"; _sizes "16x16";    _href "/images/favicon/favicon-16x16.png" ]
        link [ _rel "manifest"; _href "/manifest.json" ]
        meta [ _name "msapplication-TileColor"; _content "#170433" ]
        meta [ _name "msapplication-TileImage"; _content "/ms-icon-144x144.png" ]
        meta [ _name "theme-color"; _content "#170433" ]
        
        meta [ _name "viewport"; _content "width=device-width, initial-scale=1"]
    ]
    @ (scripts |> List.map (fun s -> script [ _src (sprintf "/js/%s" s) ] []))
    
let private headerView =
    div [ _id "header" ] [
        a [ _href "/about"; _class "centered-text margin-top-05em link-text" ] [ str "About"]
    ]
    
let private footerView =
    div [ _id "footer" ] [
        img [ _src "/images/logo_white.png"; _id "footer-image" ]
    ]

let private masterView (scripts: string list) (content: XmlNode list) =
    html [ _id "background" ] [
        head [] (scripts |> headTags)
        
        body [ _class "transparent" ] [
            headerView
            div [] [
                div [ _id "outer" ] [
                    div [ _id "middle" ] [
                        div [ _id "inner" ]
                            content
                    ]
                ]
            ]
            
            footerView
        ]
    ]
    
let private inputBoxView =
    div [] [
        script [ _src "/js/download.js" ] []
        div [ _class "input-group vertical" ] [
            input [ _type "text"; _class "transparent button-margin input-field"; _id "filename"; _name "filename" ; _placeholder "Filename" ]
            input [ _type "text"; _class "transparent button-margin input-field"; _id "token"; _name "token"; _placeholder "Token" ]
            button [ _type "button"; _onclick "readAndRedirect()"; _id "action-button"; _class "action-button round-button" ] [ str "Download" ]
            a [ _href "/upload"; _class "centered-text margin-top-05em link-text" ] [ str "If you have an upload token, click here."]
        ]
    ]

let private uploadInputBoxView =
    div [] [
        script [ _src "/js/upload.js" ] []
        form [ _enctype "multipart/form-data"; _action "/api/upload"; _method "post"; _class "invisible-form" ] [
            div [ _class "input-group vertical invisible-form" ] [
                div [ _class "inputAndButton" ] [
                    input [ _type "text"; _class "transparent button-margin input-field width100percent"; _id "token"; _name "token"; _placeholder "Token"; _oninput "resetValidation()"]
                    button [ _type "button"; _id "subaction-button"; _onclick "onValidationButtonClick()"; _class "action-button round-button" ] [ str "Validate" ]
                ]
                input [ _type "file"; _id "subaction-button"; _placeholder "Filename"; _name "file"; _class "file-chooser-margin" ]
                button [ _type "submit"; _id "action-button"; _disabled; _class "action-button round-button"] [ str "Upload" ]
                a [ _href "/download"; _class "centered-text margin-top-05em link-text" ] [ str "If you have a download token and filename, click here."]
            ]
        ]
    ]
    
let indexView =
    [
        h1 [] [ str "Welcome to Torpedo" ]
        p [] [ str "Enter the filename and your download token into the fields below." ]
        inputBoxView
    ]
    |> masterView [ "download.js" ]
    
let badRequestView (message: string) =
    [
        h1 [] [ str "400"]
        p [] [ str message ]
        p [] [ str "Try again :)" ]
        inputBoxView
    ]    
    |> masterView []
    
let notFoundView (message: string) =
    [
        h1 [] [ str "404" ]
        p [] [ str message ]
        p [] [ str "Try again :)" ]
        inputBoxView
    ]
    |> masterView []
    
let internalErrorView (message: string) =
    [
        h1 [] [ str "500" ]
        p [] [ str message ]
        p [] [ str "Try again :)" ]
        inputBoxView
    ]
    |> masterView []

let uploadView =
    [
        h1 [] [ str "Welcome to Torpedo" ]
        p [] [ str "If you have an upload token you can use it here to upload files."]
        uploadInputBoxView
    ]
    |> masterView [ "upload.js" ]
    
let uploadFinishedView =
    [
        h1 [] [ str "Upload successful!" ]
        p [] [ str "You can now close this browser tab or return to a previous page." ]
        div [ _class "padding-8px" ] [
            a [ _href "/"; _class "centered-text margin-top-05em link-text" ] [ str "Return"]
        ]
    ]
    |> masterView []
    
let featureNotEnabledview (name: string) =
    [
        h1 [] [ str (sprintf "The %s feature is not enabled." name) ]
        p [] [ str "If you think this should be enabled, please contact the administrator." ]
    ]
    |> masterView []
    
let aboutView =
    [
        h1 [] [ str "Written with F# and ❤️" ]
        div [] [
            p [] [
                span [] [ str "If you have feedback please email " ]
                a [ _href "mailto:torpedo.http@gmail.com"; _class "centered-text margin-top-05em link-text" ] [ str "me"]
                span [] [ str "." ]
            ]
            p [] [
                span [] [ str "Fork me on " ]
                a [ _href "https://github.com/b0wter/torpedo"; _class "centered-text margin-top-05em link-text" ] [ str "GitHub"]
                span [] [ str "." ]
            ]
            p [] [ small [] [ str "You are running ] ]
            br []
            p [] [
                a [ _href "javascript:history.back()"; _class "centered-text margin-top-05em link-text" ] [ str "Return"]
            ]
        ]
    ]
    |> masterView []