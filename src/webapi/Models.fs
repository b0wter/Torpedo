namespace Torpedo.Models

open System

[<CLIMutable>]
type Download =
    {
        Filename: string
        Token: string
    }