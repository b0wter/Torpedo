module WebApiTests.Helpers
    open System
    open System.Collections.Generic

    let isOk (result: Result<'a, 'b>) : bool =
        match result with 
        | Error _ -> false
        | Ok _    -> true
        
    let isSome (option: option<'a>) : bool =
        match option with
        | Some _ -> true
        | None -> false    
        
    let newDictionaryWith<'a, 'b when 'a : equality> ([<ParamArray>] args: ('a * 'b) []) =
        let dict = Dictionary<'a, 'b>()
        do args |> Array.iter (fun (key, value) -> dict.Add(key, value))
        dict
        