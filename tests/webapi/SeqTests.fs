module WebApiTests.SeqTests

open Xunit
open FsUnit.Xunit

[<Fact>]
let ``takeMax with empty seq returns empty seq`` () =
    Seq.empty<int>
    |> Seq.takeMax 1
    |> should equal (Seq.empty<int>)
    
[<Fact>]
let ``takeMax with longer seq returns specified number of items`` () =
    let filtered = [ 1; 2; 3; 4; 5; 6; 7; 9; 10 ] 
                   |> Seq.takeMax 5
                   |> List.ofSeq
                   
    filtered |> should haveLength 5
    [ 1..5 ] |> List.iteri (fun index value -> filtered.[index] |> should equal value)

[<Fact>]
let ``takeMax with matching seq item count returns full seq`` () =
    let filtered = [ 1; 2; 3; 4; 5; ] 
                   |> Seq.takeMax 5
                   |> List.ofSeq
                   
    filtered |> should haveLength 5
    [ 1..5 ] |> List.iteri (fun index value -> filtered.[index] |> should equal value)

[<Fact>]
let ``takeMax smaller seq item count returns full seq`` () =
    let filtered = [ 1; 2; 3; 4; 5; ] 
                   |> Seq.takeMax 10
                   |> List.ofSeq
                   
    filtered |> should haveLength 5
    [ 1..5 ] |> List.iteri (fun index value -> filtered.[index] |> should equal value)

[<Fact>]
let ``skipOrEmpty with empty seq returns empty seq`` () =
    Seq.empty<int>
    |> Seq.skipOrEmpty 1
    |> should equal (Seq.empty<int>)

[<Fact>]
let ``skipOrEmpty with enough items in seq skips items`` () =
    let skipped = [ 1; 2; 3; 4; 5; 6; 7; 8; 9; 10 ]
                  |> Seq.skipOrEmpty 5
                  |> List.ofSeq
        
    skipped |> should haveLength 5
    [ 6..10 ] |> List.iteri (fun index value -> skipped.[index] |> should equal value)
    
[<Fact>]
let ``skipOrEmpty with matching item count returns empty seq`` () =
    [ 1; 2; 3; 4; 5; ]
    |> Seq.skipOrEmpty 5
    |> should equal (Seq.empty<int>)   
