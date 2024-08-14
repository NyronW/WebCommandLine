// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener("DOMContentLoaded", function () {
    const ajaxHandler = (endpoint, options) => {
        const { method, headers, body } = options;
        return $.ajax({
            url: endpoint,
            method: method,
            headers: headers,
            data: body,
            contentType: headers['Content-Type'],
            dataType: 'json'
        });
    };

    window.cli = new WebCLI('/MyWebCli', ajaxHandler);
});