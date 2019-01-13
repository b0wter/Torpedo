module WebApi.Tokens

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

/// <summary>
/// Checks if a given token contains at least one valid token value.
/// </summary>
let isTokenStillValid (getLastWriteTime: string -> DateTime) (downloadLifeTime: TimeSpan) (token: Token) : bool =
    let lastWriteCondition (token: Token) : bool =
        let lastWrittenTo = getLastWriteTime token.TokenFilename
        let threshold = lastWrittenTo + downloadLifeTime
        DateTime.Now < threshold
            
    let valueCondition (token: Token) : bool =
        let withExpiration = token.Values |> Seq.where (fun v -> match v.ExpirationDate with
                                                                 | Some _ -> true
                                                                 | None -> false)
        withExpiration
        |> Seq.exists (fun v -> (v.ExpirationDate.Value > DateTime.Now))
        
    (token |> lastWriteCondition) && (token |> valueCondition)
        
