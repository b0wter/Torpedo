module WebApi.Helpers
open System.Runtime.CompilerServices

/// <summary>
/// Maps a collection, applies a filter and then uses another mapping to return the collection to its original type.
/// </summary>
let mapFilterRemap (forth: 'a->'b) (back: 'b->'a) (predicate: 'b -> bool) (items: 'a seq) : 'a seq =
    items |> Seq.map forth |> Seq.filter predicate |> Seq.map back

/// <summary>
/// Checks if a collection of Result<...,...> contains at least one error value.
/// </summary>
let containsError (items: Result<'a, 'b> seq) : bool =
    items |> Seq.exists (fun element -> match element with | Ok _ -> false | Error _ -> true)
    
/// <summary>
/// Filters a collection of Result<...,...> and returns only Ok values.
/// </summary>  
let filterOks (items: Result<'a, 'b> seq) : 'a seq =
    items
    |> Seq.map (fun item -> match item with 
                            | Ok success -> Some success
                            | Error _    -> None)
    |> Seq.choose id                            
                            
[<Extension>]
type System.String with
    static member IsNotNullOrWhiteSpace(s: string): bool =
        System.String.IsNullOrWhiteSpace(s) = false
        