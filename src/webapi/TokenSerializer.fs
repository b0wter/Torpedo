module WebApi.TokenSerializer
open System
open WebApi.Tokens

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
    match tokenvalue.ExpirationDate with 
    | Some date -> sprintf "%s:%s" tokenvalue.Value <| date.ToString("yyyy-MM-dd")
    | None      -> tokenvalue.Value

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
    
/// <summary>
/// Deserializes a TokenValue.
/// (See serializeTokenValue for format details.)
/// </summary>    
let deserializeTokenValue (content: string) : Result<TokenValue, string> =    
    match content.Split(":") with 
    | [| value ; expiration |] -> Ok { TokenValue.Value = value; TokenValue.ExpirationDate = Some (DateTime.Parse(expiration)) }    
    | [| value |]              -> Ok { TokenValue.Value = value; TokenValue.ExpirationDate = None }
    | _                        -> Error "String split delivered zero or more than two parts."
    
/// <summary>
/// Deserializes a Token by deserializing each line using deserializeTokenValue.
/// </summary>    
let deserializeToken (filename: string) (content: TokenValueContent) : Result<Token, string> =
    let lines = match content with 
                | AsLines l -> l
                | AsTotal t -> t.Split(Environment.NewLine)
                
    let parsed = lines
                 |> Array.map deserializeTokenValue                
    
    if parsed |> Helpers.containsError then
        Error "Could not deserialize the token."                                                       
    else 
        Ok ({ Values = parsed |> Helpers.filterOks; Filename = filename })
