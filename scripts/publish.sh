#
# Create binaries.
#
mkdir -p out 
cd src/webapi/
dotnet clean
rm -rf out 
for ARCHITECTURE in ${VERSIONS[@]}; do
    echo -e "${CYAN}Publishing for $ARCHITECTURE.${RESET}"
    dotnet publish -c Release -o out/$ARCHITECTURE --self-contained --runtime $ARCHITECTURE
    cd out/$ARCHITECTURE
    ZIP_FILENAME="${PROJECT_NAME}_${ARCHITECTURE}_${NEW_TAG}.zip"
    echo -e "${CYAN}Zipping $ARCHITECTURE release as $ZIP_FILENAME.${RESET}"
    zip -r $ZIP_FILENAME *
    MARKDOWN_URLS="${MARKDOWN_URLS} ${URL}"
    mv $ZIP_FILENAME ../ 
    cd ../..
done
cd ../..
cp src/webapi/**/$PROJECT_NAME*.zip out
