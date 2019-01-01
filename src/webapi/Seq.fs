module Seq
    let skipOrEmpty<'a> (count: int) (items: 'a seq) : 'a seq =
        if count >= (items |> Seq.length) then 
            Seq.empty<'a>
        else
            items |> Seq.skip count
            
    let takeMax<'a> (count: int) (items: 'a seq) : 'a seq =
        let amountToTake = System.Math.Min(count, items |> Seq.length)
        items |> Seq.take amountToTake
