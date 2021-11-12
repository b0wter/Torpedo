module WebApiTests.TokenSerializerTests
    open Xunit
    open FsUnit.Xunit
    open WebApi.TokenSerializer
    open System
    open WebApi.Tokens
    
    let private deserializeTokenTest (lengthAssert: int -> unit) (valueAssert: string -> unit) (dateAssert: DateTime option -> unit) (commentAssert: string option -> unit) (content: string) =
        let token = deserializeToken "dummy.filename" "dummy.content" (AsTotal content)
        
        match token with 
        | Error _  -> failwith "Token could not be deserialized."
        | Ok token ->
            token.Values |> Seq.length |> lengthAssert
            
            token.Values |> Seq.iter (fun (tokenvalue : TokenValue) ->
                    tokenvalue.Value          |> valueAssert
                    tokenvalue.ExpirationDate |> dateAssert
                    tokenvalue.Comment        |> commentAssert
                )

    [<Theory>]
    [<InlineData("abcdef;2000-01-01 # This is a comment." )>]
    [<InlineData("abcdef;2000-01-01# This is a comment." )>]
    [<InlineData("abcdef;2000-01-01 #This is a comment." )>]
    [<InlineData("abcdef;2000-01-01 ; This is a comment." )>]
    [<InlineData("abcdef;2000-01-01; This is a comment." )>]
    [<InlineData("abcdef;2000-01-01 ;This is a comment." )>]
    let ``deserialize token with value, expiration date and comment`` token =
        token
        |> deserializeTokenTest
                (fun length  -> length  |> should equal 1)
                (fun value   -> value   |> should equal "abcdef")
                (fun date    -> date    |> should equal (Some (DateTime(2000, 1, 1))))
                (fun comment -> comment |> should equal (Some "This is a comment."))
                
    [<Theory>]
    [<InlineData("abcdef;2000-01-01")>]
    [<InlineData("abcdef; 2000-01-01")>]
    [<InlineData("abcdef; 2000-01-01 ")>]
    let ``deserialize token with value and expiration date`` token =
        token
        |> deserializeTokenTest
                (fun length  -> length  |> should equal 1)
                (fun value   -> value   |> should equal "abcdef")
                (fun date    -> date    |> should equal (Some (DateTime(2000, 1, 1))))
                (fun comment -> comment |> should equal None)
                
    [<Fact>]
    let ``deserialize token with value`` () =
        "abcdef" 
        |> deserializeTokenTest
                (fun length  -> length  |> should equal 1)
                (fun value   -> value   |> should equal "abcdef")
                (fun date    -> date    |> should equal None)
                (fun comment -> comment |> should equal None)
                
    [<Fact>]
    let ``deserialize token with multiple values`` () =
        """
        abcdef;2000-01-01
        abcdef;2000-01-01
        abcdef;2000-01-01
        """ 
        |> deserializeTokenTest
                (fun length  -> length  |> should equal 3)
                (fun value   -> value   |> should equal "abcdef")
                (fun date    -> date    |> should equal (Some (DateTime(2000, 1, 1))))
                (fun comment -> comment |> should equal None)
                
    [<Fact>]
    let ``serialize token with value, expiration date and comment`` () =
        let token = {
                Token.Values = [ 
                    {
                        TokenValue.Value = "abcdef"
                        TokenValue.ExpirationDate = Some (DateTime(2000, 1, 1))
                        TokenValue.Comment = Some "This is a comment."
                    }
                ] |> Seq.ofList
                Token.TokenFilename = "filename"
                Token.ContentFilename = "content"
            }
        
        serializeToken token
        |> should equal "abcdef;2000-01-01 00:00:00 # This is a comment."
        
    [<Fact>]
    let ``serialize token with value and expiration date`` () =
        let token = {
                Token.Values = [ 
                    {
                        TokenValue.Value = "abcdef"
                        TokenValue.ExpirationDate = Some (DateTime(2000, 1, 1))
                        TokenValue.Comment = None
                    }
                ] |> Seq.ofList
                Token.TokenFilename = "filename"
                Token.ContentFilename = "content"
            }
        
        serializeToken token
        |> should equal "abcdef;2000-01-01 00:00:00"
        
    [<Fact>]
    let ``serialize token with value`` () =
        let token = {
                Token.Values = [ 
                    {
                        TokenValue.Value = "abcdef"
                        TokenValue.ExpirationDate = None
                        TokenValue.Comment = None
                    }
                ] |> Seq.ofList
                Token.TokenFilename = "filename"
                Token.ContentFilename = "content"
            }
        
        serializeToken token
        |> should equal "abcdef"
        
