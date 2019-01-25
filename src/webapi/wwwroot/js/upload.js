/*
    Code for the upload page.
 */
function validateToken(token) {
    const Http = new XMLHttpRequest();
    const url = "/api/upload/validate";
    const form = new FormData();
    
    resetValidation();
    form.append("token", token);
    
    Http.addEventListener('error', function(event) {
        console.log('Something went wrong.');
        console.log(event);
    });
    
    Http.onreadystatechange=function(ev) {
        console.log(ev);
        if(this.readyState === 4 && this.status === 200 && Http.responseText === "true") {
            console.log(Http.responseText);
            document.getElementById("action-button").disabled = false;
            addClassToElement('subaction-button', 'success');
        }
        else if(this.readyState === 4) {
            console.log("No success status code.");
            document.getElementById("action-button").disabled = true;
            addClassToElement('subaction-button', 'error');
        }
    };
    
    Http.open("POST", url);
    Http.send(form);
}

function resetValidation() {
    removeClassFromElement('subaction-button', 'success');
    removeClassFromElement('subaction-button', 'error');
    document.getElementById("action-button").disabled = true;
}

function addClassToElement(elementId, className) {
    const e = document.getElementById(elementId);
    e.classList.add(className);
}

function removeClassFromElement(elementId, className) {
    const e = document.getElementById(elementId);
    e.classList.remove(className);
}

function onValidationButtonClick () {
    const token = document.getElementById("token").value;
    validateToken(token);
}

function onDocumentReady() {
}

document.addEventListener('DOMContentLoaded', onDocumentReady, false);
