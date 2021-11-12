module WebApi.Cleanup

open System
open System.IO
open Helpers
open FileAccess
open WebApi.TokenSerializer
open WebApi.Tokens
    
let private cleanFolder (downloadLifeTime: TimeSpan) (folder: FolderName) =
    let pairs = folder
                |> getFilesWithTokens
                |> Array.map (fun (tokenFileName, contentFileName) -> (tokenFileName |> getTextContent |> AsTotal, tokenFileName, contentFileName))
                |> Array.map (fun (tokenContent, tokenFileName, contentFileName) -> tokenContent |> deserializeToken tokenFileName contentFileName)
    
    do pairs
    |> filterErrors
    |> Seq.iter (printfn "%s. Download will not be deleted.")
    
    let toDelete = pairs
                   |> filterOks
                   |> Seq.filter (fun x -> x |> isTokenStillValid downloadLifeTime |> not)
    
    do toDelete
    |> Seq.iter (fun token -> [ token.TokenFilename; token.ContentFilename ] |> List.iter (fun s -> printfn "Deleting file: %s" s
                                                                                                    File.Delete(s)))
    
let cleanAll basePath (tokenLifeTime: TimeSpan) (downloadLifeTime: TimeSpan) =
    do printfn "Cleaning all downloads older than %A or whose tokens have expired after %A" downloadLifeTime tokenLifeTime
    let cleaner = cleanFolder downloadLifeTime
    
    basePath
    |> getAllSubFolders true
    |> List.iter cleaner
    
