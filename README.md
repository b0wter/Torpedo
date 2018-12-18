Goal
====
Torpedo is a small web server that is designed to share downloadable files with your friends.
The links will expire automatically based on either of two conditions:

* If the download is older than X days.
* If the token (see below) has expired.

How To
======
For each downloadable file in your downloads folder you need to create a text file with the extension ".token".
E.g. you have the files `cook.png` and `my_zipped_secret.zip` you need to add:

* `cook.token` and
* `my_zipped_secret.token`

These files are simple text files that contain an arbitrary number of lines. Each of these lines is interpreted as token.
A file might look like this:

```
abcdef
12345678:2019-01-01
```

The first value `abcdef` has not been used and therefor does not have an expiration date. The second value has been used and expires on the first of January 2019. The colon is not part of the token.

Download likes look like this:

```
https://myserver/api/downloads/cook.png/abcdef
```

Where `cook.png` is an route paramter for the file to download and `abcdef` is the validation token.

Last modified file timestamps are used to check if a download is too old to be served. Torpedo uses the token file as reference not the content file.

Todo
====
* Crons to periodically delete old files.
* Configuration files.
* Url decode filenames.
