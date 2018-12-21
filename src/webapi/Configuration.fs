module WebApi.Configuration

open Newtonsoft.Json
open System

/// <summary>
/// Contains all user-configurable settings. Is automatically filled by the ASP .NET Core framework.
/// (see Program.fs -> tryBindConfiguration)
/// </summary>
type Configuration() =
        [<JsonIgnore>]
        static let mutable instance = Configuration ()
        let mutable basePath: string = ""
        let mutable defaultDownloadLifeTime: TimeSpan = TimeSpan.FromDays(7.0)
        let mutable defaultTokenLifeTime: TimeSpan = TimeSpan.FromDays(2.0)
        
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
        member this.DefaultDownloadLifeTime
            with get() = defaultDownloadLifeTime
            and  set(value) = defaultDownloadLifeTime <- value
            
        /// <summary>
        /// Lifetime of a download once the first download attempt has been started.
        /// </summary>
        member this.DefaultTokenLifeTime
            with get() = defaultTokenLifeTime
            and  set(value) = defaultTokenLifeTime <- value
