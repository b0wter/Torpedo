namespace Torpedo.Models

open System

[<CLIMutable>]
type Message =
    {
        Text : string
    }
    
type Token =
    {
        Value: string;
        ExpirationDate: DateTime option;
    }
    
type TokenCollection =
    {
        Tokens: Token seq;
        ExpirationDate: DateTime;
    }