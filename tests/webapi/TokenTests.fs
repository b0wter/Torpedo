module WebApiTests.TokenTests

open Xunit
open FsUnit.Xunit
open System
open WebApi.Tokens

let private createTokenWithSingleValue value comment expiration filename =
    {
        Token.Values = [
            {
                TokenValue.Value = value;
                TokenValue.Comment = comment;
                TokenValue.ExpirationDate = expiration;
            }
        ];
        Token.Filename = filename
    }
    
let private createTokenWithMultipleValues values filename =
    {
        Token.Values = values |> List.map (fun (value, expiration, comment) -> { Value = value; ExpirationDate = expiration; Comment = comment })
        Token.Filename = filename
    }
    
let private createTokenWithSingleValueAndDummyFilename value expiration comment =
    createTokenWithSingleValue value comment expiration "dummy.token"    
    
let private createTokenWithMultipleValuesAndDummyFilename values =
    createTokenWithMultipleValues values "dummy.token"
    
[<Fact>]
let ``setExpirationDate on token without expiration date sets an expiration date`` () =
    let token = createTokenWithMultipleValues [ ("a", None, None); ("b", None, None); ("aa", None, None) ] "dummy.token"
    let result = setExpirationDate (DateTime(2000, 1, 1)) token "a" 
    let date = (result.Values |> Seq.head).ExpirationDate
    
    date |> should equal (Some (DateTime(2000, 1, 1)))
    
[<Fact>]
let ``setExpirationDate on token with expiration date does not set new expiration date`` () =
    let oldDate = Some (DateTime(1900, 1, 1))
    let token = createTokenWithSingleValueAndDummyFilename "a" oldDate None
    let result = setExpirationDate (DateTime(2000, 1, 1)) token "a" 
    let date = (result.Values |> Seq.head).ExpirationDate
    
    date |> should equal oldDate
    
[<Fact>]
let ``tokenContainsValue with matching value returns true`` () =
    createTokenWithMultipleValuesAndDummyFilename [ ("a", None, None); ("b", None, None); ("c", None, None) ]
    |> tokenContainsValue "a"
    |> should be True
    
[<Fact>]
let ``tokenContainsValue without matching value returns false`` () =
    createTokenWithMultipleValuesAndDummyFilename [ ("b", None, None); ("c", None, None) ]
    |> tokenContainsValue "a"
    |> should be False
