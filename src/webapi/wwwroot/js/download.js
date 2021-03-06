/*
    Code for the download page.
 */
function redirectToDownload(filename, token) {
    window.location.href = "/api/download?filename=" + filename + "&token=" + token;
}

function getFilenameValue() {
    var filename = document.getElementById('filename');
    filename = encodeURI(filename.value);
    return filename;
}

function getTokenValue() {
    var token = document.getElementById('token').value;
    return token;
}

function readAndRedirect() {
    var filename = getFilenameValue();
    var token = getTokenValue();
    redirectToDownload(filename, token);
}

function makeEnterOnFieldClickForButton(field, button) {
    field.addEventListener("keyup", function(event) {
        event.preventDefault();
        if (event.keyCode === 13) {
            button.click();
        }
    });
}

function onDocumentReady() {
    var button = document.getElementById('action-button');
    var downloadField = document.getElementById('filename');
    var tokenField = document.getElementById('token');
    makeEnterOnFieldClickForButton(downloadField, button);
    makeEnterOnFieldClickForButton(tokenField, button);
}

document.addEventListener('DOMContentLoaded', onDocumentReady, false);

