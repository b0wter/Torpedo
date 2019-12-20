module WebApi.TokenSerializer
open System
open System.Globalization
open System.Globalization
open System.IO
open WebApi.Tokens
open WebApi.Helpers
open b0wter.FSharp

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
        | Some date -> sprintf "%s%s%s" prefix (date.ToString("yyyy-MM-dd HH:mm:ss")) suffix
        | None      -> ""

    sprintf "%s%s%s" (tokenvalue.Value) (tokenvalue.ExpirationDate |> noneDateAsEmpty ";" "") (tokenvalue.Comment |> noneStringAsEmpty " # " "")

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
/// Tries to parse the given string in a specific format. If that fails uses framework defaults to parse the date.
/// If that fails returns None. Returns Some DateTime otherweise.
/// </summary>
let private parseStringAsDateTime (s: string) : DateTime option =
    match DateTime.TryParseExact(s, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None) with
    | (true, date) -> Some date
    | (false, _)   ->
        match DateTime.TryParse(s) with
        | (true, date) -> Some date
        | (false, _)   -> None
    
/// <summary>
/// Deserializes a TokenValue.
/// (See serializeTokenValue for format details.)
/// </summary>    
let deserializeTokenValue (content: string) : Result<TokenValue, string> =    
    let content = content.Replace("#", ";")
    (*
                                                       | [| value ; expiration; comment |] -> ( trimTokenValue value; TokenValue.ExpirationDate = Some (DateTime.ParseExact(expiration, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)); TokenValue.Comment = Some (comment.TrimStart(' ', '\t')) }
                                                       | [| value ; expiration |]          -> ( TokenValue.Value = trimTokenValue value; TokenValue.ExpirationDate = Some (DateTime.ParseExact(expiration, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)); TokenValue.Comment = None }
                                                       | [| value |]                       -> ( TokenValue.Value = trimTokenValue value; TokenValue.ExpirationDate = None; TokenValue.Comment = None }
                                                       *)
    
    let (value, expiration, comment, shouldHaveDate, isValid) = match content.Split(";") with 
                                                                | [| value ; expiration; comment |] -> ( trimTokenValue value, (parseStringAsDateTime expiration), Some (comment.TrimStart(' ', '\t')), true, true )
                                                                | [| value ; expiration |]          -> ( trimTokenValue value, (parseStringAsDateTime expiration), None, true, true )
                                                                | [| value |]                       -> ( trimTokenValue value, None, None, false, true )
                                                                | _                                 -> ( "", None, None, false, false)
                                                                
    if isValid && (if shouldHaveDate && expiration.IsNone then false else true) then
        Ok { Value = value; ExpirationDate = expiration; Comment = comment }
    else
        Error "The string split returnd either to many or too few arguments or a token file contains an invalid date."
    
/// <summary>
/// Deserializes a Token by deserializing each line using deserializeTokenValue.
/// </summary>
let deserializeToken (tokenFilename: string) (contentFilename: string) (content: TokenValueContent) : Result<Token, string> =
    let lines = match content with 
                | AsLines l -> l
                | AsTotal t -> t.Split(System.Environment.NewLine)
                |> Array.filter (String.isNullOrWhiteSpace >> not)
                
    let parsed = lines
                 |> Array.map deserializeTokenValue                
                 |> Array.map (fun t -> match t with 
                                           | Ok token -> if String.IsNullOrWhiteSpace(token.Value) then (Error "Token has empty value.") else (Ok token)
                                           | Error err -> Error err)
    
    if parsed |> Helpers.containsError then
        Error ("Could not deserialize the token: " + tokenFilename + ".")
    else 
        Ok ({ Values = parsed |> Helpers.filterOks; TokenFilename = tokenFilename; ContentFilename = contentFilename })
