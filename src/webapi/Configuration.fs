module WebApi.Configuration

open Newtonsoft.Json
open System

/// <summary>
/// Contains all user-configurable settings.
/// </summary>
type Configuration() =
        [<JsonIgnore>]
        static let mutable instance = Configuration ()
        let mutable basePath: string = ""
        let mutable downloadLifeTime: TimeSpan = TimeSpan.FromDays(7.0)
        let mutable tokenLifeTime: TimeSpan = TimeSpan.FromDays(2.0)
        let mutable cleanExpiredDownloads: bool = false
        let mutable cronIntervalInHours: int = 24
        let mutable uploadsEnabled: bool = false
        
        /// <summary>
        /// Use this Singleton to access the configuration from anywhere.
        /// </summary>
        [<JsonIgnore>]
        static member Instance 
            with get() = instance
            and set value = instance <- value
        
        /// <summary>
        /// Path to the downloads folder.
        /// </summary>
        member this.BasePath
            with get() = basePath
            and  set value = basePath <- value
            
        /// <summary>
        /// Lifetime of a download. The check is done against the current time and the last time
        /// the token file was modified.
        /// </summary>
        member this.DownloadLifeTime
            with get() = downloadLifeTime
            and  set value = downloadLifeTime <- value
            
        /// <summary>
        /// Lifetime of a download once the first download attempt has been started.
        /// </summary>
        member this.TokenLifeTime
            with get() = tokenLifeTime
            and  set value = tokenLifeTime <- value

        /// <summary>
        /// Gets/sets wether the expired downloads are periodically deleted.
        /// </summary>
        member this.CleanExpiredDownloads
            with get() = cleanExpiredDownloads
            and set value = cleanExpiredDownloads <- value
            
        /// <summary>
        /// Interval in which the downloads are cleared. Given in hours.
        /// </summary>
        member this.CronIntervalInHours
            with get() = cronIntervalInHours
            and set value = cronIntervalInHours <- value
            
        /// <summary>
        /// Enables the upload feature.
        /// </summary>
        member this.UploadsEnabled
            with get() = uploadsEnabled
            and set value = uploadsEnabled <- value
            
        /// <summary>
        /// Returns whether the configuration is valid.
        /// The only required value is `BasePath`.
        /// Every other setting is optional and can be initialized with a meaningful default.
        /// </summary>
        /// <returns>
        /// `true` if `BasePath` is set;
        /// `false` otherwise
        /// </returns>
        member this.IsValid =
            this.BasePath |> (not << String.IsNullOrWhiteSpace)
            
        member this.MergeEnvironmentVariables () =
            let nonParsable = System.Collections.Generic.List<string>(6)
            let tryGet (key: string) map fallback =
                let fullKey = $"TORPEDO_%s{key.ToUpper()}"
                let r = Environment.GetEnvironmentVariable(fullKey)
                if r |> String.IsNullOrWhiteSpace then fallback
                else
                    try
                        printfn "Found envrionment variable for %s." key
                        map r
                    with
                    | _ ->
                        do nonParsable.Add $"Could not override '%s{key}' because the value '%s{r}' could not be parsed. The default value '%A{fallback}' will be used." 
                        fallback
            this.BasePath               <- tryGet (nameof this.BasePath) id this.BasePath
            this.UploadsEnabled         <- tryGet (nameof this.UploadsEnabled) bool.Parse this.UploadsEnabled
            this.CleanExpiredDownloads  <- tryGet (nameof this.CleanExpiredDownloads) bool.Parse this.CleanExpiredDownloads
            this.DownloadLifeTime       <- tryGet (nameof this.DownloadLifeTime) TimeSpan.Parse this.DownloadLifeTime
            this.TokenLifeTime          <- tryGet (nameof this.TokenLifeTime) TimeSpan.Parse this.TokenLifeTime
            this.CronIntervalInHours    <- tryGet (nameof this.CronIntervalInHours) Int32.Parse this.CronIntervalInHours
            do nonParsable |> Seq.iter Console.WriteLine
            
        /// <summary>
        /// Tries to create configuration by deserializing a given file.
        /// </summary>
        /// <returns>
        /// `Some config` if the file exists and is a valid configuration; `None` otherwise.
        /// </returns>
        static member TryFromConfigFile filename =
            if System.IO.File.Exists(filename) then 
                let config = System.IO.File.ReadAllText(filename)
                             |> JsonConvert.DeserializeObject<Configuration>
                Some config
            else
                None

        /// <summary>
        /// Creates a new configuration that tries to deserialize a configuration from a file and then applies
        /// environmental variables.
        /// Will use a default initialization if the configuration file does not exist.
        /// </summary>
        static member FromConfigAndEnvironment filename =
            let config = match filename |> Configuration.TryFromConfigFile with
                         | Some c -> c
                         | None ->
                             printfn "Did not find configuration file. A default configuration will be used instead. Environment variables are still applied."
                             Configuration()
            do config.MergeEnvironmentVariables()
            config
            

            