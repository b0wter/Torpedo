#!/usr/bin/env bash

if [ "$#" -ge 3 ]; then
	rm $2
	TOKEN=$(cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 32 | head -n 1)
	PASSWORD=$(cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 32 | head -n 1)
	7z a -p$PASSWORD -mhe=on $2 "${@:3}"
	cp $2 $1
	TOKEN_FILE=$(basename -- "$2" ".7z")
	echo "$TOKEN" >> "$1/${TOKEN_FILE}.token"

	echo "The following password is NOT recoverable! Please write them down carefully."
	echo "7z password:    $PASSWORD"
	echo "The download token was written to $1/${TOKEN_FILE}.token."
	echo "Download token: $TOKEN"
else
	echo "You ned to supply at least three parameters to this script:"
       	echo "1. folder for the download"
	echo "2. name of the download"
	echo "3. an arbitrary number of files that will be used as the contents of the new zip"
fi
