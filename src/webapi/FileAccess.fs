module WebApi.FileAccess
open System
open System.IO

type ContentFilename = string
type TokenFilename = string
type DownloadPair = (TokenFilename * ContentFilename)

/// <summary>
/// File extension for tokens.
/// </summary>
let tokenExtension = ".token"

/// <summary>
/// Returns a list of (Token filename * content filename) for all files in the given folder.
/// Only considers those Token files which have exactly one corresponding content file.
/// (e.g.: cook.png cook.bmp & cook.token would NOT be returned but shark.png & shark.token would be)
/// </summary>
let customGetFilesWithTokens (getFilesInFolder: string -> string [])
                             (getFileNameWithoutExtension: string -> string) 
                             (getFileName: string -> string) 
                             (getFileExtension: string -> string) 
                             (pathCombinator: (string * string) -> string)
                             (folder: string): DownloadPair[] =
    getFilesInFolder(folder)
    |> Array.map getFileName
    |> Array.groupBy (fun name -> getFileNameWithoutExtension(name))
    |> Array.map     (fun (name,  fullnames)  -> (name, fullnames |> Array.map getFileExtension))
    |> Array.filter  (fun (_,     extensions) -> extensions |> Array.exists (fun extension -> extension.EndsWith(tokenExtension)))
    |> Array.filter  (fun (_,     extensions) -> extensions.Length = 2)
    |> Array.map     (fun (name,  extensions) -> (pathCombinator(folder, name + extensions.[0]), (pathCombinator(folder, name + extensions.[1]))))
    |> Array.map     (fun (first, second)     -> if first.EndsWith(tokenExtension) then (first, second) else (second, first))
    
let getFilesWithTokens =
    customGetFilesWithTokens Directory.GetFiles
                             Path.GetFileNameWithoutExtension
                             Path.GetFileName
                             Path.GetExtension
                             Path.Combine
    
/// <summar>
/// Reads text contents from a file. 
/// Will throw exceptions if the file is unknown or the contents
/// cannot be read.
/// </summary>
let customGetTextContent (textFileReader: string -> string) (filename: string) =
    textFileReader(filename)    
    
let getTextContent =
    customGetTextContent File.ReadAllText
    
/// <summary>
/// Get the timestamp of the last mofication (local system time).
/// Will throw an exception if the file does not exist.
/// </summary>    
let customGetLastModified (lastWriteTimeProvider: string -> DateTime) (filename: string) =
    lastWriteTimeProvider(filename)    
        
let getLastModified =        
    customGetLastModified File.GetLastWriteTime

/// <summary>
/// Retrieves the last modification dates for a list of files.
/// Will throw an exception if any of the files does not exist.
/// </summary>    
let getFileDates (filenames: string seq) =
    filenames
    |> Seq.map getLastModified
    
/// <summary>
/// Checks if the given file is downloadable meaning a matching Token file exists
/// and the Token is uniquely bound to a single content file.
/// </summary>
let customExistsIn (getFilesWithTokens: string -> DownloadPair []) (pathCombinator: (string * string) -> string) (folder: string) (filename: string): bool =
    if folder.Contains("..") || filename.Contains("..") then 
        false 
    else
        let t = getFilesWithTokens folder
        t |> Array.map snd |> Array.contains (pathCombinator(folder, filename))
    
let existsIn =
    customExistsIn (getFilesWithTokens) Path.Combine
    
/// <summary>
/// Interprets the filename as a combination of directory and filename and checks if it exists.
/// Will always return false if there is a ".." in the filename.
/// </summary>    
let customFileExists (fileExistenceChecker: string -> bool) (filename: string) : bool =
    if filename.Contains("..") then
        false 
    else
        fileExistenceChecker filename
    
let fileExists =
    customFileExists File.Exists 
    
/// <summary>
/// Tries to open a file stream for the given file. 
/// Returns Some FileStream if successful otherwise None.
/// </summary>
let fileStream (folder: string) (filename: string): FileStream option =
    let filename = Path.Combine(folder, filename)
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
let customPersistStringAsFile (persistor: (string * string) -> unit) (filename: string) (content: string) : bool =
    try
        persistor(filename, content)
        true
    with
    | ex ->
        false
    
let persistStringAsFile =
    customPersistStringAsFile File.WriteAllText 