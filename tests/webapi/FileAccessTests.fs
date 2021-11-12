module WebApiTests.FileAccessTests

open System.IO
open Xunit
open FsUnit.Xunit

let private fileListWithDownloads =
    [|
        "file.zip"; "file.token"; "whatever.txt"; ".gitignore"; "run_me"; "archive.7z"; "archive.token"
    |]
    
let private fileListWithoutDownloads =    
    [|
        "picture.png"; "image.jpg"; "whatever.txt"; ".gitignore"; "run_me"; "archive.7z"; "leftover.token"
    |]

let private combineWith path filename =
    Path.Combine(path, filename)

[<Fact>]
let ``customGetFilesWithTokens with downloads in file list returns download pairs`` () =
    let dummyFolder = "dummy_folder"
    let combinator = combineWith dummyFolder

    let getFiles = (fun _ -> fileListWithDownloads)
    
    let pairs = WebApi.FileAccess.customGetFilesWithTokens getFiles
                                               Path.GetFileNameWithoutExtension
                                               Path.GetFileName
                                               Path.GetExtension
                                               Path.Combine
                                               dummyFolder
                                               
    let toContain = [ (combinator "file.token", combinator "file.zip"); (combinator "archive.token", combinator "archive.7z") ]
    
    pairs |> should haveLength (toContain |> List.length)
    toContain |> List.iter (fun t -> pairs |> should contain t)
    //pairs |> should contain (combinator "file.token", combinator "file.zip")
    //pairs |> should contain (combinator "archive.token", combinator "archive.7z")
    
[<Fact>]
let ``customGetFilesWithTokens without downloads in file list returns empty array`` () =
    let getFiles = (fun _ -> fileListWithoutDownloads)
    
    WebApi.FileAccess.customGetFilesWithTokens getFiles
                                               Path.GetFileNameWithoutExtension
                                               Path.GetFileName
                                               Path.GetExtension
                                               Path.Combine
                                               "dummy folder"
    |> should be Empty
    
[<Fact>]
let ``customGetFilesWithTokens with empty file list returns empty array`` () =
    let getFiles _ = Array.empty<string>
    
    WebApi.FileAccess.customGetFilesWithTokens getFiles
                                               Path.GetFileNameWithoutExtension
                                               Path.GetFileName
                                               Path.GetExtension
                                               Path.Combine
                                               "dummy folder"
    |> should be Empty
    
[<Theory>]
[<InlineData("test.file", "test.token", "dummy_folder", "../test.file", false)>]
[<InlineData("test.file", "test.token", "dummy_folder", "subfolder/../test.file", false)>]
[<InlineData("test.file", "test.token", "myfolder/../dummy_folder", "test.file", false)>]
[<InlineData("test.file", "test.token", "../dummy_folder", "test.file", false)>]
[<InlineData("test.file", "test.token", "dummy_folder", "../test.file", false)>]
[<InlineData("test.file", "test.token", "dummy_folder", "test.file", true)>]
[<InlineData("test.file", "test.token", "", "test.file", true)>]
let ``existsIn with different valid and invalid file/folder names returns expected result`` localFile localToken basePath requestedFile expected =
    let combinator filename = Path.Combine(basePath, filename)
    
    WebApi.FileAccess.customExistsIn (fun _ -> [| (localToken |> combinator , localFile |> combinator ) |])
                                     Path.Combine
                                     basePath
                                     requestedFile
    |> should equal expected

