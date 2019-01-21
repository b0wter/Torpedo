![Torpedo Logo](https://github.com/b0wter/Torpedo/blob/master/assets/logo_small.png)

Goal
====
Torpedo is a small web server that is designed to share downloadable files with your friends.
The links will expire automatically based on either of two conditions:

* If the download is older than X days.
* If the token (see below) has expired.

The project is in a fully usable state.

![Screenshot](https://user-images.githubusercontent.com/614261/50368705-148f3280-058c-11e9-9482-2bb5810b88c0.png)

How To
======

Disclaimer
----------
Please note that I am not a security expert! This software is not built with security as its top-most priority. Use this software at your own risk and use common sense!

Setup downloads
---------------
For each downloadable file in your downloads folder you need to create a text file with the extension ".token".
E.g. you have the files `cook.png` and `my_zipped_secret.zip` you need to add:

* `cook.token` and
* `my_zipped_secret.token`

These files are simple text files that contain an arbitrary number of lines. Each of these lines is interpreted as a token value.
A file might look like this:

```
abcdef
12345678:2019-01-01
```

The first value `abcdef` has not been used and therefor does not have an expiration date. The second value has been used and expires on the first of January 2019. The colon is not part of the token value. Expiration dates are set when a download of the given file is attempted. If you want to, you can set an expiration date manually or edit a previously written date using a text editor. Expiration can be given as:

```
yyyy-MM-dd HH:mm:ss
yyyy-MM-dd
yyyy-MM-ddTHH\\:mm\\:ss.fffffffzzz (ISO 8601)
```

You can add comments as well:

```
abcdef # This is a comment.
123456:2019-01-01 # This is also a comment.
```

Download files
--------------
There are two ways to download files.

Use direct links in the following form:

```
http://myserver/api/download?filename=cook.png&token=abcdef
```
Where `cook.png` is a route paramter for the file to download and `abcdef` is the validation token.
Alternativly you open the root page:

```
http://myserver
```

and enter the filename and token into the input fields.

Lifetime of downloads and tokens
--------------------------------
There are two checks that are performed before a download is served:

### Lifetime of a download
The "last written" timestamp of the token file (not the download file) is used to check how old the download is. If the timespan is larger than the `DownloadLifetime` value in the configuration the download is considered expired and will not be served.

### Lifetime of a token value
You can define any number of token values inside a token file (see above). The lifetime of each value is tracked individually and can bet set manually by simply writing a value into the .token file (see above).

This means that you can create multiple download urls per file, e.h. for sharing with multiple users.

This lifetime is a workaround for the fact that the webserver has no idea if a file download was successful. There might be any number of problems that's why a successful download cannot be tracked. That's why this lifetime gives people multiple chances to try and download the file.

Setup Uploads
-------------
You can enable uploads for remote users. To do so you have to set `UploadsEnabled` to true. You may optionally define a maximum upload size by setting `MaxUploadSize` (bytes). Please note that there is a hardcoded size limit of ~1.5GB for remote uploads. This is because of the way ASP.NET Core and Giraffe handle uploads. Please note that uploads are *not* checked for malicious content!

Uploads use the same `BasePath` as downloads. To enable uploads to a folder create a file named `.token`. This file follows the same syntax as the regular download token files.

Upload files
------------
To upload a file you need to give an upload token to a remote user. The user needs to go to:

```
http://myserver/upload
```

and enter the token. The Upload-button is not enabled until the user validates his token. You can also use a tool like curl or wget to upload the file directly:

```
http://myserver/api/upload

Method: POST
Content: multipart/form-data
Form parameter: token = $YOUR_TOKEN
```

Please note that the token is only verified *after* the file finished uploading. That's why the GUI requires the user to validate the token before starting the upload. If you want to upload a token in a script and want to check a token, you can do so by sending a request the following way:

```
const Http = new XMLHttpRequest();
const url = "/api/upload/validate";
const form = new FormData();

form.append("token", token);

Http.open("POST", url);
Http.send(form);
```

You will either receive "true" or "false" as result.

Configuration
=============
The application needs a configuration file to work properly. The file is written in json and looks like this:
```
{
	"BasePath": "/home/b0wter/tmp/torpedo",
	"DownloadLifetime": "7.00:00:00",
	"TokenLifetime": "2.00:00:00",
	"CleanExpiredDownloads": false,
	"CronIntervalInHours": 24,
	"UploadsEnabled": false,
	"MaxUploadSize": 1073741824
}

```

* *BasePath*: needs to point to the directory you want to use to serve files.
* *DownloadLifetime*: Expiration time of a download. This is counted from the moment you create the token file (using the last written timestamp). Given in the format $DAYS:HOURS:MINUTES:SECONDS.
* *TokenLifetime*: Expiration time of a single token value. This is set the moment the first user uses this token. Given in the format $DAYS:HOURS:MINUTES:SECONDS.
* *CleanExpiredDownloads*: Makes Torpedo periodically check all downloads and delete the token and content files for expired downloads.
* *CronIntervalInHours*: Interval in which the aforementioned action is performed. Given in hours.
* *UploadsEnabled*: Enables the upload feature for remote users. Note that you will still need to create upload tokens manually.
* *MaxUploadSize*: Maximum size of uploads, given in bytes. Please note that the framework currently limits Torpedo to an upload size of about 1.5 GB.

Docker
======
To use Torpedo from inside a Docker container you'll need to do the following:

* Create folder on your host system for your downloads, e.g. `/srv/torpedo`.
* Create a configuration file with `BasePath` set to `/app/content` (see Configuration part, or simply use the config_docker.json in this repo)).
* Use the following parameters for your docker run: `-p 8080:80 --mount type=bind,source=/your/local/folder/,target=/app/content/`

A full command might look like this:
```
docker run -d --restart=always -p 8080:80 --mount type=bind,source=/home/b0wter/tmp/torpedo,target=/app/content/ --name mytorpedo b0wter/torpedo
```
It is possible to use another folder as `/app/content/` inside the container but that requires you to supply a custom `config.json` as bind-mount.

To build your own image you only need the docker runtime. Run the following command from the folder of the cloned repository:
```
docker build -t torpedo .
```

Images are auto-generated from this repository and can be found on [Docker Hub](https://hub.docker.com/r/b0wter/torpedo).


HTTPS
=====
Currently there is no native support for certificates. I recommend running this app behind a reverse proxy (which may offer https). Either Caddy or Traefic are perfect choices to do so as they offer Let's Encrypt support out of the box.

Creating downloads
==================
Downloads can easily be created manually. However, this repository contains a bash script that makes creating downloads even easiert. It takes (at least) three parameters:

1. the path to your downloads directory
2. the name of the new 7z download (downloads created by this script are always zipped and encrypted)
3. A list of files to be added to the 7z file (may contain wildcards).

```
./create_download.sh /srv/torpedo new_download.7z /home/b0wter/downloads/linux_isos/*
```

This will automatically create a random password (using /dev/random), zip the given files, copy the zip to the destination folder and create a new token file with a random token. Please make sure that you write down the password created for the new download as it cannot be recovered!
