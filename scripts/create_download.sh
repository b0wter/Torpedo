#!/usr/bin/env bash

if ! [ -x "$(command -v 7z)" ]; then
	echo "7zip is not installed."
	exit 1
fi

if [ "$#" -ge 4 ]; then
	rm $3
	TOKEN=$(cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 32 | head -n 1)
	PASSWORD=$(cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 32 | head -n 1)
	7z a -p$PASSWORD -mhe=on $3 "${@:4}"
	cp $3 $2
	TOKEN_FILE=$(basename -- "$3" ".7z")
	echo "$TOKEN" >> "$2/${TOKEN_FILE}.token"

	echo "The following password is NOT recoverable! Please write it down carefully."
	echo "7z password:    $PASSWORD"
	echo "The download token was written to $2/${TOKEN_FILE}.token."
	echo "Download token: $TOKEN"
	echo "Download url:"
	echo "$1?filename=$3&token=$TOKEN"
else
	echo "You ned to supply at least three parameters to this script:"
	echo "1. the base url of your torpedo instance"
       	echo "2. folder for the download"
	echo "3. name of the download"
	echo "4. an arbitrary number of files that will be used as the contents of the new zip, must be parseable by 7zip"
fi
