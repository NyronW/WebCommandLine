// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener("DOMContentLoaded", function () {
    function ajaxHttpHandler(endpoint, options) {
        return new Promise((resolve, reject) => {
            $.ajax({
                url: endpoint,
                method: options.method,
                headers: options.headers,
                data: options.body,
                success: function(data) {
                    resolve(data); // Return raw data as is
                },
                error: function(xhr) {
                    // Handle error response here (including parsing)
                    reject(xhr.responseText || xhr.statusText);
                }
            });
        });
    }

    function axiosHttpHandler(endpoint, options) {
        const { method, headers, body } = options;
        return axios({
            url: endpoint,
            method: method,
            headers: headers,
            data: body,
            responseType: 'text'
        }).then(response => {
            console.log(response);

            return response.data
        }) // Return the response as raw text
            .catch(error => {
                // Handle the error here (including parsing)
                return Promise.reject(error.response?.data || error.message);
            });
    }



    window.cli = new WebCLI('/MyWebCli', axiosHttpHandler);
});