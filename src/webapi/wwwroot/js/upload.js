function validateToken(token) {
    const Http = new XMLHttpRequest();
    const url = "/api/upload/validate";
    const form = new FormData();
    
    form.append("token", token);
    
    Http.addEventListener('error', function(event) {
        console.log('Something went wrong.');
        console.log(event);
    });
    
    Http.onreadystatechange=function(ev) {
        console.log(ev);
        if(this.readyState === 4 && this.status === 200) {
            document.getElementById("action-button").disabled = false;
        }
        else if(this.readyState === 4) {
            console.log("No success status code.");
        }
    };
    
    Http.open("POST", url);
    Http.send(form);
}

function onValidationButtonClick () {
    const token = document.getElementById("token").value;
    validateToken(token);
}

function onDocumentReady() {
    console.log("doc loaded");
}

document.addEventListener('DOMContentLoaded', onDocumentReady, false);
