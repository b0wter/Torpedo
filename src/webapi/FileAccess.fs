module WebApi.FileAccess
open System
open Microsoft.CodeAnalysis.CSharp
open System.IO
open System.Linq.Expressions
open Torpedo.Models
open Newtonsoft.Json

/// <summary>
/// Dateiendung für Tokendateien.
/// </summary>
let tokenExtension = ".token"
let fileStorageDirectory = "/home/b0wter/tmp/torpedo/"

/// <summary>
/// Liefert eine Liste aus Tokendatei und Dateiname für alle Dateien in dem übergebenen Order für die genau eine
/// .token-Datei und eine andere Datei mit gleichem Namen aber anderer Extension existiert.
/// </summary>
let getFilesWithTokens (folder: string): (string * string)[] =
    Directory.GetFiles(folder)
    |> Array.map Path.GetFileName
    |> Array.groupBy (fun name -> Path.GetFileNameWithoutExtension(name))
    |> Array.map     (fun (name,  fullnames)  -> (name, fullnames |> Array.map Path.GetExtension))
    |> Array.filter  (fun (_,     extensions) -> extensions |> Array.exists (fun extension -> extension.EndsWith(tokenExtension)))
    |> Array.filter  (fun (_,     extensions) -> extensions.Length = 2)
    |> Array.map     (fun (name,  extensions) -> (name + extensions.[0], name + extensions.[1]))
    |> Array.map     (fun (first, second)     -> if first.EndsWith(tokenExtension) then (first, second) else (second, first))
    
let getDownloadableFiles (folder: string): (string * string)[] =
    getFilesWithTokens folder
    |> Array.map     (fun (tokenFile, contentFile) -> (tokenFile, contentFile, JsonConvert.DeserializeObject<TokenCollection>(tokenFile)))
    |> Array.filter  (fun (_, _, tokenCollection)  -> tokenCollection.ExpirationDate >= DateTime.Now)
    |> Array.filter  (fun (_, _, tokenCollection)  -> tokenCollection.Tokens 
                                                      |> Seq.exists (fun element -> match element.ExpirationDate with
                                                                                    | Some date -> date >= DateTime.Now
                                                                                    | None      -> true))
    |> Array.map     (fun (tokenFile, contentFile, _) -> (tokenFile, contentFile))
    
/// <summary>
/// Prüft ob eine bestimmte Datei downloadbar ist (d.h. eine passende Tokendatei existiert)
/// und der Token eindeutig dieser Datei zugeordnet werden kann.
/// </summary>
let existsIn (folder: string) (filename: string): bool =
    getDownloadableFiles folder |> Array.map snd |> Array.contains filename
    
// Download von Dateien laufen über `WriteStreamAsync` und den `StreamData` HTTP Handler.
// Damit wird am Ende ein `Stream` an den Client geliefert.
let fileStream (filename: string): FileStream option =
    let combined = Path.Combine(fileStorageDirectory, filename)
    let path = Path.GetDirectoryName(combined)
    let file = Path.GetFileName(filename)
    
    if file |> existsIn path then 
        Some (File.OpenRead(combined))
    else
        None
        
let expireTokenInCollection (tokenValue: string ) (collection: TokenCollection) =
    let newTokens = collection.Tokens
                    |> Seq.filter (fun t -> t.Value = tokenValue)
                    |> Seq.map (fun t -> { t with ExpirationDate = Some (DateTime.Now + TimeSpan.FromDays(2.0)) } )
    { collection with Tokens = newTokens }                
        
let setTokenToExpire (filename: string) (tokenValue: string) : bool =
    let combined = Path.Combine(fileStorageDirectory, filename)
    let path = Path.GetDirectoryName(combined)
    let file = Path.GetFileNameWithoutExtension(combined)
    let tokenFile = Path.Combine(path, file + tokenExtension)
    
    if tokenFile |> File.Exists then 
        File.ReadAllText(tokenFile)
        |> JsonConvert.DeserializeObject<TokenCollection>
        |> expireTokenInCollection tokenValue
        |> JsonConvert.SerializeObject
        |> (fun serialized -> File.WriteAllText(tokenFile, serialized))
        true
    else 
        false 
