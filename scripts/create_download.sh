#!/usr/bin/env bash

if ! [ -x "$(command -v 7z)" ]; then
        echo "7zip is not installed."
        exit 1
fi

if [ "$#" -ge 4 ]; then
        TOKEN=$(cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 32 | head -n 1)
        PASSWORD=$(cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 32 | head -n 1)
        TOKEN_FILE=$(basename -- "$3" ".7z").token
        DOWNLOAD_FILE=$(basename -- "$3" ".7z").7z
        SERVER_URL=$(echo ${1%/})

        echo "Removing old files (if they exist)."
        rm $DOWNLOAD_FILE 2> /dev/null
        rm $2/$DOWNLOAD_FILE 2> /dev/null
        rm $2/$TOKEN_FILE 2> /dev/null

        echo "Creating 7z archive."
        7z a -p$PASSWORD -mhe=on $DOWNLOAD_FILE "${@:4}"
        echo "Writing token file."
        echo "$TOKEN" >> "$2/${TOKEN_FILE}"
        echo "Copying content file."
        cp $DOWNLOAD_FILE $2
        echo ""

        echo "The following password is NOT recoverable! Please write it down carefully."
        echo "7z password:    $PASSWORD"
        echo "The download token was written to: $2/${TOKEN_FILE}"
        echo "Download token: $TOKEN"
        echo "Download url:"
        echo "$SERVER_URL/api/download?filename=$DOWNLOAD_FILE&token=$TOKEN"
else
        echo "You ned to supply at least three parameters to this script:"
        echo "1. the base url of your torpedo instance"
        echo "2. folder for the download"
        echo "3. name of the download"
        echo "4. an arbitrary number of files that will be used as the contents of the new zip, must be parseable by 7zip"
fi
