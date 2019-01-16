module WebApi.Tokens

open System
open System

/// <summary>
/// Part of a Token. A TokenValue represents a single value with its own expiration date.
/// </summary>
type TokenValue =
    {
        Value: string;
        ExpirationDate: DateTime option;
        Comment: string option;
    }
    
/// <summary>
/// A Token represents a set of possible values which "unlock" a download.
/// Does not have a separate expiration date since file modifications are used.
/// </summary>    
type Token = 
    {
        /// <summary>
        /// Collection of token values.
        /// </summary>
        Values: TokenValue seq;
        /// <summary>
        /// Filename for this token. Is required for easier serialization.
        /// </summary>
        TokenFilename: string;
        /// <summary>
        /// Filename of the content this token is meant for.
        ContentFilename: string;
    }
    
/// <summary>
/// Sets the expiration date for a TokenValue inside a Token.
/// If the TokenValue already has an expiration date it remains unchanged.
/// </summary>    
let setExpirationDate (date: DateTime) (token: Token) (tokenvalue: string): Token =
    { token with Values = token.Values |> Seq.map (fun element -> if element.Value = tokenvalue then 
                                                                    match element.ExpirationDate with 
                                                                    | Some date when date <> Unchecked.defaultof<DateTime> ->
                                                                        { element with ExpirationDate = Some date }
                                                                    | None ->
                                                                        { element with ExpirationDate = Some date }
                                                                    | _ ->
                                                                        element
                                                                  else 
                                                                    element)
                                                                  }

/// <summary>
/// Sets the expiration date based on a TimeSpan from DateTime.Now.
/// </summary>
let setExpirationTimeSpan (span: TimeSpan) =
    setExpirationDate (DateTime.Now + span)
                                        
/// <summary>
/// Checks if a Token contains a TokenValue with the given value.
/// </summary>
let tokenContainsValue (value: string) (token: Token) =
    token.Values |> Seq.exists (fun element -> element.Value = value)                                        

let private isDateLargerThan (value: DateTime) (ifNone: bool) (toCheck: DateTime option) : bool =
    match toCheck with
    | Some date ->
        value > date
    | None ->
        ifNone

/// <summary>
/// Checks if a given token contains at least one valid token value.
/// </summary>
let customIsTokenStillValid (getLastWriteTime: string -> DateTime) (downloadLifeTime: TimeSpan) (token: Token) : bool =
    let lastWriteCondition (token: Token) : bool =
        let lastWrittenTo = getLastWriteTime token.TokenFilename
        let threshold = lastWrittenTo + downloadLifeTime
        let result = DateTime.Now < threshold
        result
            
    let valueCondition (token: Token) : bool =
        let result = token.Values
                     |> Seq.map (fun t -> t.ExpirationDate)
                     |> Seq.exists (isDateLargerThan DateTime.Now true)
        result
        
    (token |> lastWriteCondition) && (token |> valueCondition)
        
let isTokenStillValid =
    customIsTokenStillValid
        IO.File.GetLastWriteTime
        
let containsValue (v: string) (t: Token) : bool =
    t.Values |> Seq.exists(fun tv -> tv.Value = v)
    
let seqContainsValue (v: string) (t: Token seq) : bool =
    t |> Seq.exists (containsValue v)
    
let findTokenContainingValue (v: string) (ts: Token seq): Token option =
    ts |> Seq.tryFind(fun t -> t.Values |> Seq.exists (fun x -> x.Value = v))