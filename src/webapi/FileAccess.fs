module WebApi.FileAccess
open System
open System.IO

type DownloadPair = (string * string)

/// <summary>
/// File extension for tokens.
/// </summary>
let tokenExtension = ".token"

/// <summary>
/// Returns a list of (Token filename * content filename) for all files in the given folder.
/// Only considers those Token files which have exactly one corresponding content file.
/// (e.g.: cook.png cook.bmp & cook.token would NOT be returned but shark.png & shark.token would be)
/// </summary>
let getFilesWithTokens (folder: string): DownloadPair[] =
    Directory.GetFiles(folder)
    |> Array.map Path.GetFileName
    |> Array.groupBy (fun name -> Path.GetFileNameWithoutExtension(name))
    |> Array.map     (fun (name,  fullnames)  -> (name, fullnames |> Array.map Path.GetExtension))
    |> Array.filter  (fun (_,     extensions) -> extensions |> Array.exists (fun extension -> extension.EndsWith(tokenExtension)))
    |> Array.filter  (fun (_,     extensions) -> extensions.Length = 2)
    |> Array.map     (fun (name,  extensions) -> (Path.Combine(folder, name + extensions.[0]), (Path.Combine(folder, name + extensions.[1]))))
    |> Array.map     (fun (first, second)     -> if first.EndsWith(tokenExtension) then (first, second) else (second, first))
    
/// <summar>
/// Reads text contents from a file. 
/// Will throw exceptions if the file is unknown or the contents
/// cannot be read.
/// </summary>
let getTextContent (filename: string) =
    File.ReadAllText(filename)    
    
/// <summary>
/// Get the timestamp of the last mofication (local system time).
/// Will throw an exception if the file does not exist.
/// </summary>    
let getLastModified (filename: string) =
    File.GetLastWriteTime(filename)    
    
/// <summary>
/// Retrieves the last modification dates for a list of files.
/// Will throw an exception if any of the files does not exist.
/// </summary>    
let getFileDates (filenames: string seq) =
    filenames
    |> Seq.map File.GetLastWriteTime    
    
/// <summary>
/// Checks if the given file is downloadable meaning a matching Token file exists
/// and the Token is uniquely bound to a single content file.
/// </summary>
let existsIn (folder: string) (filename: string): bool =
    if folder.Contains("..") || filename.Contains("..") then 
        false 
    else
        getFilesWithTokens folder |> Array.map snd |> Array.contains (Path.Combine(folder, filename))
    
/// <summary>
/// Interprets the filename as a combination of directory and filename and checks if it exists.
/// Will always return false if there is a ".." in the filename.
/// </summary>    
let fileExists (filename: string) : bool =
    if filename.Contains("..") then
        false 
    else
        File.Exists filename
    
/// <summary>
/// Tries to open a file stream for the given file. 
/// Returns Some FileStream if successful otherwise None.
/// </summary>
let fileStream (filename: string): FileStream option =
    let path = Path.GetDirectoryName(filename)
    
    if String.IsNullOrWhiteSpace(path) then 
        None
    else
        let file = Path.GetFileName(filename)
        
        if file |> existsIn path then 
            Some (File.OpenRead(filename))
        else
            None

/// <summary>
/// Writes the given content into the given file using the default character encoding.
/// </summary>
let persistStringAsFile (filename: string) (content: string) =
    File.WriteAllText(filename, content)