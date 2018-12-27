module WebApi.Helpers
open System.Collections.Generic
open System.Linq
open System.Runtime.CompilerServices

/// <summary>
/// Maps items of a collection and runs a filter afterwards. The original item is returned for all mapped items that passed the filter.
/// </summary>
let mappedFilter (transformation: 'a->'b) (predicate: 'b -> bool) (items: 'a seq) : 'a seq =
    items 
    |> Seq.map (fun item -> (item, item |> transformation ))
    |> Seq.filter (fun (_, mappedItem) -> mappedItem |> predicate)
    |> Seq.map fst

/// <summary>
/// checks if the sequence contains a None element.
/// </summary>
let containsNone (items: seq<'a option>) : bool =
    items |> Seq.exists (fun element -> match element with | Some _ -> false | None -> true)

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
                            
let appendTo (items: IDictionary<obj, obj>) (key: string) (value: string) =
    match items.ContainsKey(key) with 
    | true ->
        items.[key] <- items.[key].ToString() + value
        items
    | false ->
        items.Add(key, value)
        items
                            
let addIfNotExisting (items: IDictionary<obj, obj>) (key: string) (value: string) =
    match items.ContainsKey(key) with 
    | true ->
        items
    | false ->
        items.Add(key, value)
        items                            
                            
[<Extension>]
type System.String with
    static member IsNotNullOrWhiteSpace(s: string): bool =
        System.String.IsNullOrWhiteSpace(s) = false

