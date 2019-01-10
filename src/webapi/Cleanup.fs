module WebApi.Cleanup

open System
open System.IO
open Helpers
open FileAccess
open WebApi.TokenSerializer

let getAllSubFolders (includeBase: bool) basePath =
    let rec findFoldersRecursively (folder: FolderName) : FolderName list =
        let subFolders = folder |> Directory.GetDirectories |> List.ofArray
        let subSubFolders = subFolders |> List.collect (fun f -> Path.Combine(folder, f) |> findFoldersRecursively)
        subFolders @ subSubFolders
    let subFolders = basePath |> findFoldersRecursively
    if includeBase then basePath :: subFolders else subFolders
    
let private isTokenStilValid (downloadLifeTime: TimeSpan) (token: Tokens.Token) : bool =
    do printfn "Checking validity of Token (%s, %s)." token.TokenFilename token.ContentFilename
    let lastWriteCondition (token: Tokens.Token) : bool =
        let lastWrittenTo = File.GetLastWriteTime(token.TokenFilename)
        let threshold = lastWrittenTo + downloadLifeTime
        if DateTime.Now > threshold then
            do printfn "Token file no longer valid. Last written to: %A, treshold was %A." lastWrittenTo threshold
            false
        else
            do printfn "Token is still valid. Last written to %A." lastWrittenTo
            true
            
    let valueCondition (token: Tokens.Token) : bool =
        let withExpiration = token.Values |> Seq.where (fun v -> match v.ExpirationDate with
                                                                 | Some _ -> true
                                                                 | None -> false)
        withExpiration
        |> Seq.exists (fun v -> (v.ExpirationDate.Value > DateTime.Now))
        
    (token |> lastWriteCondition) && (token |> valueCondition)
        
    
let private cleanFolder (downloadLifeTime: TimeSpan) (folder: FolderName) =
    let pairs = folder
                |> getFilesWithTokens
                |> Array.map (fun (tokenFileName, contentFileName) -> (tokenFileName |> WebApi.FileAccess.getTextContent |> AsTotal, tokenFileName, contentFileName))
                |> Array.map (fun (tokenContent, tokenFileName, contentFileName) -> tokenContent |> TokenSerializer.deserializeToken tokenFileName contentFileName)
    
    do pairs
    |> filterErrors
    |> Seq.iter (printfn "%s. Download will not be deleted.")
    
    let toDelete = pairs
                   |> filterOks
                   |> Seq.filter (fun x -> x |> isTokenStilValid downloadLifeTime |> not)
    
    do toDelete
    |> Seq.iter (fun token -> [ token.TokenFilename; token.ContentFilename ] |> List.iter (fun s -> printfn "Deleting file: %s" s
                                                                                                    File.Delete(s)))
    
let cleanAll basePath (tokenLifeTime: TimeSpan) (downloadLifeTime: TimeSpan) =
    do printfn "Cleaning all downloads older than %A or whose tokens have expired after %A" downloadLifeTime tokenLifeTime
    let cleaner = cleanFolder downloadLifeTime
    
    basePath
    |> getAllSubFolders true
    |> List.iter cleaner
    
