module WebApi.Views

open Giraffe
open Giraffe
open Giraffe.GiraffeViewEngine

let footer = tag "footer"

let headTags =
    [
        title [] [ str "Torpedo" ]
        
        link [ _rel "stylesheet"
               _type "text/css"
               _href "/css/mini-dark.min.css" ]       
               
        link [ _rel "stylesheet"
               _type "text/css"
               _href "/css/custom.css" ]       
               
        script [ _src "/js/custom.js" ] []
                
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
        meta [ _name "msapplication-TileColor"; _content "#ffffff" ]
        meta [ _name "msapplication-TileImage"; _content "/ms-icon-144x144.png" ]
        meta [ _name "theme-color"; _content "#ffffff" ]
    ]
    
let footerView =
    div [ _id "footer" ] [
        img [ _src "/images/logo_white.png"; _id "footer-image" ]
    ]

let masterView (content: XmlNode list) =
    html [ _id "background" ] [
        head [] headTags

        body [ _class "transparent" ] [
        
            div [ _id "outer" ] [
                div [ _id "middle" ] [
                    div [ _id "inner" ]
                        content
                ]
            ]
            
            footerView
        ]
    ]
    
let indexView =
    [
        h1 [] [ str "Welcome to Torpedo" ]
        p [] [ str "Enter the filename and your download token into the fields below." ]
        div [ _class "input-group vertical" ] [
            input [ _type "text"; _class "transparent button-margin input-field"; _id "filename"; _name "filename" ; _placeholder "Filename" ]
            input [ _type "text"; _class "transparent button-margin input-field"; _id "token"; _name "token"; _placeholder "Token" ]
            button [ _type "button"; _onclick "readAndRedirect()"; _id "download-button" ] [ str "Download" ]
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