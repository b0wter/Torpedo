module WebApi.TokenSerializer
open System
open System.IO
open WebApi.Tokens
open WebApi.Helpers

/// <summary>
/// Defines the type of content used to deserialized from.
/// Content is accepted as a single string in which case it will be split using Environment.NewLine
/// or as an array of strings.
/// <summary>
type TokenValueContent =
    | AsLines of string[]
    | AsTotal of string

/// <summary>
/// Serializes a TokenValue into the format:
/// $VALUE
/// if no expiration date is set, or:
/// $VALUE:$EXPIRATIONDATE
/// </summary>
let serializeTokenValue (tokenvalue: TokenValue): string =
    let noneStringAsEmpty (prefix: string) (suffix: string) (s: string option) =
        match s with 
        | Some content -> sprintf "%s%s%s" prefix content suffix
        | None -> ""
        
    let noneDateAsEmpty (prefix: string) (suffix: string) (d: DateTime option) =
        match d with 
        | Some date -> sprintf "%s%s%s" prefix (date.ToString("yyyy-MM-dd")) suffix
        | None      -> ""

    sprintf "%s%s%s" (tokenvalue.Value) (tokenvalue.ExpirationDate |> noneDateAsEmpty ":" "") (tokenvalue.Comment |> noneStringAsEmpty " # " "")

/// <summary>
/// Serializes a Token by serializing each TokenValue using the serializeTokenValue method.
/// </summary>
let serializeToken (token: Token): string =
    token.Values
    |> Seq.map serializeTokenValue
    |> Seq.reduce (fun collector item -> sprintf "%s%s%s" collector Environment.NewLine item)
       
/// <summary>
/// Serializes a Token option using the serializeToken method.
/// </summary>       
let serializeTokenOption (token: Token option) =
    match token with 
    | Some string -> Some (serializeToken string)
    | None        -> None
    
let private trimTokenValue (token: string) =
    token
     .TrimStart(' ', '\t')
     .TrimEnd(' ', '\t')
    
/// <summary>
/// Deserializes a TokenValue.
/// (See serializeTokenValue for format details.)
/// </summary>    
let deserializeTokenValue (content: string) : Result<TokenValue, string> =    
    let content = content.Replace("#", ":")
    
    match content.Split(":") with 
    | [| value ; expiration; comment |] -> Ok { TokenValue.Value = trimTokenValue value; TokenValue.ExpirationDate = Some (DateTime.Parse(expiration)); TokenValue.Comment = Some (comment.TrimStart(' ', '\t')) }
    | [| value ; expiration |]          -> Ok { TokenValue.Value = trimTokenValue value; TokenValue.ExpirationDate = Some (DateTime.Parse(expiration)); TokenValue.Comment = None }    
    | [| value |]                       -> Ok { TokenValue.Value = trimTokenValue value; TokenValue.ExpirationDate = None; TokenValue.Comment = None }
    | _                                 -> Error "String split delivered zero or more than two parts."
    
/// <summary>
/// Deserializes a Token by deserializing each line using deserializeTokenValue.
/// </summary>
let deserializeToken (tokenFilename: string) (contentFilename: string) (content: TokenValueContent) : Result<Token, string> =
    let lines = match content with 
                | AsLines l -> l
                | AsTotal t -> t.Split(Environment.NewLine)
                |> Array.filter String.IsNotNullOrWhiteSpace
                
    let parsed = lines
                 |> Array.map deserializeTokenValue                
                 |> Array.map (fun t -> match t with 
                                           | Ok token -> if String.IsNullOrWhiteSpace(token.Value) then (Error "Token has empty value.") else (Ok token)
                                           | Error err -> Error err)
    
    if parsed |> Helpers.containsError then
        Error "Could not deserialize the token."                                                       
    else 
        Ok ({ Values = parsed |> Helpers.filterOks; TokenFilename = tokenFilename; ContentFilename = contentFilename })
