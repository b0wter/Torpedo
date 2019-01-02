
module WebApiTests.HelperTests
    open Xunit
    open FsUnit.Xunit
    open System.Collections.Generic
    open WebApiTests
    open FsUnit
    open System
    
    type ``containsNone`` ()=
        [<Fact>] member x.
         ``Seq with None element returns true`` ()=
            [ Some "string"; None; Some "test" ] |> Seq.ofList
            |> WebApi.Helpers.containsNone
            |> should be True

        [<Fact>] member x.
         ``Seq without None element returns false`` ()=
            [ Some "string"; Some "test" ] |> Seq.ofList
            |> WebApi.Helpers.containsNone
            |> should be False

        [<Fact>] member x.
         ``Seq without elements returns false`` ()=
            [ ] |> Seq.ofList 
            |> WebApi.Helpers.containsNone
            |> should be False

    type ``containsError`` ()=
        [<Fact>] member x.
         ``Seq with Error element returns true`` ()=
            [ Ok "string"; Error "rekt" ; Ok "test" ] |> Seq.ofList
            |> WebApi.Helpers.containsError
            |> should be True

        [<Fact>] member x.
         ``Seq without Error element returns true`` ()=
            [ Ok "string"; Ok "test" ] |> Seq.ofList
            |> WebApi.Helpers.containsError
            |> should be False

        [<Fact>] member x.
         ``Seq without elements returns false`` ()=
            [ ] |> Seq.ofList
            |> WebApi.Helpers.containsError
            |> should be False
                
    type ``filterOks`` ()=
        [<Fact>] member x.
         ``Seq with Error elements turns into Seq of results`` ()=
            let filtered = [ Ok "string"; Error "rekt" ; Ok "test" ] |> Seq.ofList |> WebApi.Helpers.filterOks in
                filtered |> Seq.length |> should equal 2
           
        [<Fact>] member x.
         ``Seq without Error elements turns into Seq of results`` ()=
            let filtered = [ Ok "string"; Ok "test" ] |> Seq.ofList |> WebApi.Helpers.filterOks in
                filtered |> Seq.length |> should equal 2
            
    type ``appendTo`` ()=
        [<Fact>] member x.
         ``Dictionary with existing key appends value`` ()=
            let dict = Helpers.newDictionaryWith<obj, obj> [| ("key" :> obj, "value" :> obj) |] in
                let value = (WebApi.Helpers.appendTo dict "key" "appended").["key"].ToString() in
                    value |> should equal "valueappended"
                
        [<Fact>] member x.
         ``Dictionary without existing key adds value`` ()=
            let dict = Dictionary<obj, obj>() in
                let value = (WebApi.Helpers.appendTo dict "key" "appended").["key"].ToString() in
                    value |> should equal "appended"
                
    type ``addIfNotExisting`` ()=
        [<Fact>] member x.
         ``Dictionary with existing key does not add value`` ()=
            let dict = Helpers.newDictionaryWith<obj, obj> [| ("key" :> obj, "value" :> obj) |] in
                let value = (WebApi.Helpers.addIfNotExisting dict "key" "appended").["key"].ToString() in
                    value |> should equal "value"
                
        [<Fact>] member x.
         ``Dictionary without existing key adds value`` ()=
            let dict = Dictionary<obj, obj>() in
                let value = (WebApi.Helpers.addIfNotExisting dict "key" "appended").["key"].ToString() in
                    value |> should equal "appended"
                
    type ``splitAtPredicate`` ()=
        [<Fact>] member x.
         ``True predicate splits the list (includeInFirst = true)`` ()=
            let items = [ 1; 2; 3; 4; 5; 6; 7; 8; 9; 10 ] |> Seq.ofList in
                let predicate = (fun i -> i = 5) in
                    let (left, right) = items |> (WebApi.Helpers.splitAtPredicate id id predicate true) in
                        let test = (fun () ->
                            do [ 1; 2; 3; 4; 5 ] |> List.iter (fun i -> left |> should contain i)
                            do [ 6; 7; 8; 9; 10 ] |> List.iter (fun i -> right |> should contain i)
                        ) in 
                            test ()
                    
        [<Fact>] member x.
         ``True predicate splits the list (includeInFirst = false)`` ()=
            let items = [ 1; 2; 3; 4; 5; 6; 7; 8; 9; 10 ] |> Seq.ofList in
                let predicate = (fun i -> i = 5) in
                    let (left, right) = items |> (WebApi.Helpers.splitAtPredicate id id predicate false) in
                        let test = (fun () ->
                            do [ 1; 2; 3; 4; ] |> List.iter (fun i -> left |> should contain i)
                            do [ 5; 6; 7; 8; 9; 10 ] |> List.iter (fun i -> right |> should contain i)
                        ) in 
                            test ()
                    
        [<Fact>] member x.
         ``Always false Predicate returns full seq`` ()=
            let items = [ 1; 2; 3; 4; 6; 7; 8; 9; 10 ] |> Seq.ofList in
                let predicate = (fun _ -> false) in
                    let (left, right) = items |> (WebApi.Helpers.splitAtPredicate id id predicate true) in
                        let test = (fun () ->
                            do left |> List.ofSeq |> should haveLength 9
                        ) in 
                            test ()
                    
