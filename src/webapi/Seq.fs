module Seq
    let skipOrEmpty<'a> (count: int) (items: 'a seq) : 'a seq =
        if count >= (items |> Seq.length) then 
            Seq.empty<'a>
        else
            items |> Seq.skip count
