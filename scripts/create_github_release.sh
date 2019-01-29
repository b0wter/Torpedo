#! /usr/bin/env bash

PROJECT_NAME=torpedo
OWNER=b0wter
REMOTE=https://api.github.com
VERSIONS=(fedora-x64 ubuntu-x64 linux-x64 win10-x64 osx-x64)

cd ..

# Includes only one line:
# GITHUB_ACCESS_KEY=my_access_key
# You can create an OAuth key from your profile's developer settings.
source scripts/credentials.sh
# Creates a new release for the current commit or retrieves the tag set for this commit.
source scripts/git_release.sh
echo "The working tag is: $NEW_TAG."

#
# Replace current version in source view.
#
perl -pi -e "s/(?<=You are running version: )\d.\d.\d/$NEW_TAG/;" src/webapi/Views.fs

#
# Create binaries.
#
source scripts/publish.sh

#
# Create Github release.
#

# Check wether a release with this tag exists.
RELEASES=$(curl --header "Authorization: token $GITHUB_ACCESS_KEY" $REMOTE/repos/$OWNER/$PROJECT_NAME/releases | jq '.[].tag_name')

if [[ $RELEASES != *"$NEW_TAG"* ]]; then
	# Release does NOT exist -> create it!
	echo "The release does not exist, creating a new one."
	curl --request POST --header "Authorization: token $GITHUB_ACCESS_KEY" --header "Content-Type: application/json" --data "{\"tag_name\": \"$NEW_TAG\",\"target_commitish\": \"master\"}" $REMOTE/repos/$OWNER/$PROJECT_NAME/releases | jq '.id'
else
	echo "The release exists."
fi

# Get the upload_url for the release.
UPLOAD_URL=$(curl --header "Authorization: token $GITHUB_ACCESS_KEY" $REMOTE/repos/$OWNER/$PROJECT_NAME/releases/tags/$NEW_TAG | jq '.upload_url' | rev | cut -c15- | rev | cut -c2-)

for ARCHITECTURE in ${VERSIONS[@]}; do
	echo "Uploading asset for architecture $ARCHITECTURE (version $NEW_TAG)."
	curl --header "Authorization: token $GITHUB_ACCESS_KEY" \
	     --header "Content-Type: application/zip" \
	     --data-binary "@out/torpedo_${ARCHITECTURE}_${NEW_TAG}.zip" \
             $UPLOAD_URL?name=torpedo_${ARCHITECTURE}_${NEW_TAG}.zip
done
