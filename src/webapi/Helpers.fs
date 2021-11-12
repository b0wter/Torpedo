module WebApi.Helpers
open System.Collections.Generic
open Giraffe
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2.ContextInsensitive

let errorCodeToView (statusCode: int) =
    match statusCode with
    | 400 -> Views.badRequestView
    | 404 -> Views.notFoundView
    | 500 -> Views.internalErrorView
    | _   -> Views.badRequestView

/// <summary>
/// Sets the error status code and the error message in the given context.
/// </Summary>
let failWithStatusCodeAndMessage (ctx: HttpContext) (next: HttpFunc) (statusCode: int) (message: string) =
    do ctx.SetStatusCode(statusCode)
    do ctx.Items.Add("errormessage", message)
    (statusCode |> errorCodeToView) message |> htmlView

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
                            
/// <summary>
/// Filters a collection of Result<...,...> and returns only Error values.
/// </summary>  
let filterErrors (items: Result<'a, 'b> seq) : 'b seq =
    items
    |> Seq.map (fun item -> match item with 
                            | Ok _    -> None
                            | Error e -> Some e)
    |> Seq.choose id                            
                           
/// <summary>
/// Adds or appends a string with a given key to the given dictionary.
/// In case the key exists the string is appended else it is added.
/// </summary>
let appendTo (items: IDictionary<obj, obj>) (key: string) (value: string) : IDictionary<obj, obj> =
    match items.ContainsKey(key) with 
    | true ->
        items.[key] <- items.[key].ToString() + value
        items
    | false ->
        items.Add(key, value)
        items
                            
/// <summary>
/// Adds a string value with a string key to the dictionary in case the key does not exist.
/// </summary>
let addIfNotExisting (items: IDictionary<obj, obj>) (key: string) (value: string) =
    match items.ContainsKey(key) with 
    | true ->
        items
    | false ->
        items.Add(key, value)
        items
        
/// <summary>
/// Takes an input, splits it at the first element for which the given predicate holds true.
/// If includeInLeft is true, the first element for which the predicate is true is included in the first tuple item.
/// </summary>
let splitAtPredicate<'a, 'b, 'c> (splitToElements: 'a -> 'b seq) (aggregate: 'b seq -> 'c) (predicate: 'b -> bool) (includeInFirst: bool) (items: 'a) : 'c * 'c =
    let splits = items |> splitToElements
    let splitIndex = splits |> Seq.tryFindIndex predicate
    let splitIndex = match splitIndex with 
                     | Some index -> index
                     | None -> splits |> Seq.length
    
    let splitIndex = if includeInFirst then splitIndex + 1 else splitIndex
    (
        splits |> Seq.takeMax splitIndex |> aggregate, 
        splits |> Seq.skipOrEmpty<'b> splitIndex |> aggregate
    )

let splitAtPredicateId (predicate: 'a -> bool) (includeLeft: bool) (items: 'a seq) : 'a seq * 'a seq =
    splitAtPredicate id id predicate includeLeft items
                            
let bind switchFunction =
    fun input ->
        match input with
        | Ok s -> switchFunction s
        | Error f -> Error f
                            
let requireParamterIn (get: string -> string option) (ctx: HttpContext) (required: string seq) (addToContext: bool) : HttpContext option =
    let parameters = required |> Seq.map (fun p -> (p, p |> get))
    
    if parameters |> Seq.map snd |> containsNone then 
        None 
    else 
        let parametersToAdd = if addToContext then parameters else Seq.empty
        parametersToAdd 
        |> Seq.filter (fun (_, result) -> match result with 
                                          | Some s -> true 
                                          | None   -> false)
        |> Seq.map (fun (name, result) -> (name, Option.get result))
        |> Seq.iter (fun (name, value) -> ctx.Items <- addIfNotExisting ctx.Items name value)
        
        Some ctx
        
let requiresFeatureEnabled (featureCheck: unit -> bool) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            match featureCheck () with
            | true  -> return! next ctx
            | false -> return None
        }
    

