![Torpedo Logo](https://github.com/b0wter/Torpedo/blob/master/assets/logo_small.png)

Goal
====
Torpedo is a small web server that is designed to share downloadable files with your friends.
The links will expire automatically based on either of two conditions:

* If the download is older than X days.
* If the token (see below) has expired.

The project is fully usable except for the automatic removal of old downloads.

![Screenshot](https://user-images.githubusercontent.com/614261/50368705-148f3280-058c-11e9-9482-2bb5810b88c0.png)

How To
======
For each downloadable file in your downloads folder you need to create a text file with the extension ".token".
E.g. you have the files `cook.png` and `my_zipped_secret.zip` you need to add:

* `cook.token` and
* `my_zipped_secret.token`

These files are simple text files that contain an arbitrary number of lines. Each of these lines is interpreted as a token.
A file might look like this:

```
abcdef
12345678:2019-01-01
```

The first value `abcdef` has not been used and therefor does not have an expiration date. The second value has been used and expires on the first of January 2019. The colon is not part of the token.

Download likes look like this:

```
https://myserver/api/download?filename=cook.png&token=abcdef
```

Where `cook.png` is a route paramter for the file to download and `abcdef` is the validation token.

Last modified file timestamps are used to check if a download is too old to be served. Torpedo uses the token file as reference not the content file.

Configuration
=============
The application needs a configuration file to work properly. The file is written in json and looks like this:
```
{
	"BasePath": "/home/b0wter/tmp/torpedo",
	"DefaultDownloadLifetime": "7.00:00:00",
	"DefaultTokenLifetime": "2.00:00:00"
}

```
The lifetimes are given in the format: `$Days.$Hours:$Minutes:$Seconds`.
The `BasePath` is the path to the files where you store your downloads. Please make sure that the user running this app has read/write access (write access is needed to periodically delete old downloads).

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
Currently there is no native support for certificates. I recommend running this app behind a reverse proxy (which may offer https).

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

Todo
====
* Crons to periodically delete old files.
