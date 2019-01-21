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
        let mutable maxUploadSize: Int64 = Int64.MaxValue
        
        /// <summary>
        /// Use this Singleton to access the configuration from anywhere.
        /// </summary>
        [<JsonIgnore>]
        static member Instance 
            with get() = instance
            and set(value) = instance <- value
        
        /// <summary>
        /// Path to the downloads folder.
        /// </summary>
        member this.BasePath
            with get() = basePath
            and  set(value) = basePath <- value
            
        /// <summary>
        /// Lifetime of a download. The check is done against the current time and the last time
        /// the token file was modified.
        /// </summary>
        member this.DownloadLifeTime
            with get() = downloadLifeTime
            and  set(value) = downloadLifeTime <- value
            
        /// <summary>
        /// Lifetime of a download once the first download attempt has been started.
        /// </summary>
        member this.TokenLifeTime
            with get() = tokenLifeTime
            and  set(value) = tokenLifeTime <- value

        /// <summary>
        /// Gets/sets wether the expired downloads are periodically deleted.
        /// </summary>
        member this.CleanExpiredDownloads
            with get() = cleanExpiredDownloads
            and set(value) = cleanExpiredDownloads <- value
            
        /// <summary>
        /// Interval in which the downloads are cleared. Given in hours.
        /// </summary>
        member this.CronIntervalInHours
            with get() = cronIntervalInHours
            and set(value) = cronIntervalInHours <- value
            
        /// <summary>
        /// Enables the upload feature.
        /// </summary>
        member this.UploadsEnabled
            with get() = uploadsEnabled
            and set(value) = uploadsEnabled <- value
            
        /// <summary>
        /// Sets the maximum size for file uploads in bytes.
        /// </summary>
        member this.MaxUploadSize
            with get() = maxUploadSize
            and set(value) = maxUploadSize <- value