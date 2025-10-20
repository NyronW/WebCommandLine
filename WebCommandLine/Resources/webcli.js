// Interface for HTTP Handlers
class WebCLI {
    constructor(endpoint, httpHandler, options) {
        var self = this;
        self.history = [];   //Command history
        self.cmdOffset = 0;    //Reverse offset into history
        self.endpoint = endpoint || "/webcli"; //default endpoint
        self.httpHandler = httpHandler || self.defaultFetchHandler;
        
        // Configuration options
        options = options || {};
        self.enableAutoCopy = options.enableAutoCopy !== false; //default to true
        self.enableRightClickPaste = options.enableRightClickPaste !== false; //default to true

        self.createElements();
        self.wireEvents();
        self.enableCopyPasteFeatures();
        self.showGreeting();
        self.busy(false);
    }

    wireEvents() {
        var self = this;

        self.keyDownHandler = function (e) { self.onKeyDown(e); };
        self.clickHandler = function (e) { self.onClick(e); };

        document.addEventListener('keydown', self.keyDownHandler);
        self.ctrlEl.addEventListener('click', self.clickHandler);
    }

    onClick() {
        this.focus();
    }

    onKeyDown(e) {
        var self = this, ctrlStyle = self.ctrlEl.style;

        //Ctrl + Backquote (Document)
        if (e.ctrlKey && e.keyCode == 192) {
            if (ctrlStyle.display == "none") {
                ctrlStyle.display = "";
                self.focus();
            }
            else {
                ctrlStyle.display = "none";
            }
            return;
        }

        if (self.isBusy) { return; }

        //Other keys (when input has focus)
        if (self.inputEl === document.activeElement) {
            switch (e.keyCode)  //http://keycode.info/
            {
                case 13: //Enter
                    return self.runCmd();

                case 38: //Up
                    if ((self.history.length + self.cmdOffset) > 0) {
                        self.cmdOffset--;
                        self.inputEl.value = self.history[self.history.length + self.cmdOffset];
                        e.preventDefault();
                    }
                    break;

                case 40: //Down
                    if (self.cmdOffset < -1) {
                        self.cmdOffset++;
                        self.inputEl.value = self.history[self.history.length + self.cmdOffset];
                        e.preventDefault();
                    }
                    break;
            }
        }
    }

    runCmd() {
        var self = this, txt = self.inputEl.value.trim();

        self.cmdOffset = 0;         //Reset history index
        self.inputEl.value = "";    //Clear input
        self.writeLine(txt, "cmd"); //Write cmd to output
        if (txt === "") { return; }  //If empty, stop processing
        self.history.push(txt);     //Add cmd to history

        //Client command:
        var tokens = txt.split(" "),
            cmd = tokens[0].toUpperCase();

        if (cmd === "CLS") { self.outputEl.innerHTML = ""; return; }

        //Server command:
        self.busy(true);

        let requestOptions = {
            method: "post",
            headers: new Headers({
                "Content-Type": "application/json"
            }),
            body: JSON.stringify({ cmdLine: txt })
        };

        // Use the provided handler to make the request
        this.httpHandler(this.endpoint, requestOptions)
            .then(function (rawResponse) {
                return self.handleResponse(rawResponse);
            })
            .catch(function (rawError) {
                return self.handleResponseError(rawError);
            })
            .then(function (result)  //Finally
            {
                var output = result.output;
                var style = result.isError ? "error" : "ok";

                if (result.isHTML) {
                    self.writeHTML(output);
                } else {
                    self.writeLine(output, style);
                    self.newLine();
                }

                self.busy(false);
                self.focus();
            });

        self.inputEl.blur();
    }

    handleResponse(rawResponse) {
        try {
            return JSON.parse(rawResponse);
        } catch (e) {
            return {
                output: rawResponse,
                isError: true,
                isHTML: false,
            };
        }
    }

    handleResponseError(rawError) {
        let parsedError;

        try {
            parsedError = JSON.parse(rawError);
        } catch (e) {
            parsedError = {
                output: rawError || "An error occurred while sending command to remote server",
                isError: true,
                isHTML: false,
            };
        }

        return parsedError;
    }

    focus() {
        this.inputEl.focus();
    }

    scrollToBottom() {
        this.ctrlEl.scrollTop = this.ctrlEl.scrollHeight;
    }

    newLine() {
        this.outputEl.appendChild(document.createElement("br"));
        this.scrollToBottom();
    }

    writeLine(txt, cssSuffix) {
        var lineContainer = document.createElement("div");
        lineContainer.className = "webcli-line";
        
        var span = document.createElement("span");
        cssSuffix = cssSuffix || "ok";
        span.className = "webcli-" + cssSuffix;
        span.innerText = txt;
        
        lineContainer.appendChild(span);
        this.outputEl.appendChild(lineContainer);
        this.scrollToBottom();
    }

    writeHTML(markup) {
        var div = document.createElement("div");
        div.className = "webcli-line webcli-html";
        div.innerHTML = markup;
        this.outputEl.appendChild(div);
        this.scrollToBottom();
    }

    showGreeting() {
        this.writeLine("Web CLI [Version 1.0.4]", "cmd");
        this.newLine();
    }

    createElements() {
        var self = this, doc = document;

        //Create & store CLI elements
        self.ctrlEl = doc.createElement("div");   //CLI control (outer frame)
        self.resizeHandleEl = doc.createElement("div"); // Resize handle
        self.outputEl = doc.createElement("div");   //Div holding console output
        self.inputEl = doc.createElement("input"); //Input control
        self.busyEl = doc.createElement("div");   //Busy animation

        //Add classes
        self.ctrlEl.className = "webcli";
        self.resizeHandleEl.className = "resize-handle"; // Add class for resize handle
        self.outputEl.className = "webcli-output";
        self.inputEl.className = "webcli-input";
        self.busyEl.className = "webcli-busy";

        //Add attribute
        self.inputEl.setAttribute("spellcheck", "false");

        //Assemble them
        self.ctrlEl.appendChild(self.resizeHandleEl); // Add resize handle at the top
        self.ctrlEl.appendChild(self.outputEl);
        self.ctrlEl.appendChild(self.inputEl);
        self.ctrlEl.appendChild(self.busyEl);

        //Hide ctrl & add to DOM
        self.ctrlEl.style.display = "none";
        doc.body.appendChild(self.ctrlEl);

        // Add event listener for resizing
        self.addResizeFunctionality();
    }

    // Function to add resizing functionality
    addResizeFunctionality() {
        var self = this;
        var isResizing = false;

        self.resizeHandleEl.addEventListener("mousedown", function (e) {
            isResizing = true;
            document.body.style.cursor = "ns-resize";
            document.body.style.userSelect = "none"; // Prevent text selection while resizing
        });

        document.addEventListener("mousemove", function (e) {
            if (!isResizing) return;
            const newHeight = window.innerHeight - e.clientY;
            if (newHeight > 100 && newHeight < window.innerHeight * 0.8) {
                self.ctrlEl.style.height = `${newHeight}px`;
            }
        });

        document.addEventListener("mouseup", function () {
            isResizing = false;
            document.body.style.cursor = "auto";
            document.body.style.userSelect = "auto"; // Re-enable text selection
        });
    }

    busy(b) {
        this.isBusy = b;
        this.busyEl.style.display = b ? "block" : "none";
        this.inputEl.style.display = b ? "none" : "block";
    }

    // Default fetch handler
    defaultFetchHandler(endpoint, options) {
        return fetch(endpoint, options).then(response => response.text());
    }

    // Copy and paste functionality
    enableCopyPasteFeatures() {
        var self = this;
        
        // Enable auto-copy if configured
        if (self.enableAutoCopy) {
            // Listen for text selection in the output area
            self.outputEl.addEventListener('mouseup', function() {
                self.handleAutoCopy();
            });
            
            // Also listen for keyboard selection (Shift + Arrow keys, etc.)
            self.outputEl.addEventListener('keyup', function() {
                self.handleAutoCopy();
            });
        }
        
        // Enable right-click paste if configured
        if (self.enableRightClickPaste) {
            // Add right-click to paste functionality to output area
            self.outputEl.addEventListener('contextmenu', function(e) {
                e.preventDefault();
                e.stopPropagation();
                self.pasteFromClipboard();
            });
            
            // Also add right-click paste to input area for consistency
            self.inputEl.addEventListener('contextmenu', function(e) {
                e.preventDefault();
                e.stopPropagation();
                self.pasteFromClipboard();
            });
        }
    }

    handleAutoCopy() {
        var self = this;
        var selection = window.getSelection();
        var selectedText = selection.toString().trim();
        
        // Only copy if there's actually selected text
        if (selectedText.length > 0) {
            self.copyToClipboard(selectedText);
        }
    }

    copyToClipboard(text) {
        var self = this;
        
        // Use the modern Clipboard API if available
        if (navigator.clipboard && window.isSecureContext) {
            navigator.clipboard.writeText(text).catch(function(err) {
                console.log('Clipboard API failed, using fallback');
                self.fallbackCopy(text);
            });
        } else {
            // Fallback for older browsers or non-secure contexts
            self.fallbackCopy(text);
        }
    }

    fallbackCopy(text) {
        // Create a temporary textarea element
        var textArea = document.createElement("textarea");
        textArea.value = text;
        textArea.style.position = "fixed";
        textArea.style.left = "-999999px";
        textArea.style.top = "-999999px";
        textArea.style.opacity = "0";
        textArea.style.pointerEvents = "none";
        
        document.body.appendChild(textArea);
        textArea.focus();
        textArea.select();
        
        try {
            var successful = document.execCommand('copy');
            if (!successful) {
                console.log('Fallback copy failed');
            }
        } catch (err) {
            console.log('Fallback copy error:', err);
        }
        
        document.body.removeChild(textArea);
    }

    // Simple paste functionality
    pasteFromClipboard() {
        var self = this;
        
        // Focus the input field first
        self.focus();
        
        // Use the modern Clipboard API if available
        if (navigator.clipboard && window.isSecureContext) {
            navigator.clipboard.readText().then(function(text) {
                self.insertTextAtCursor(text);
            }).catch(function(err) {
                console.log('Clipboard read failed:', err);
                self.showPasteFeedback("Paste failed - clipboard access denied");
            });
        } else {
            // Fallback: try to read from a temporary textarea
            self.fallbackPaste();
        }
    }

    insertTextAtCursor(text) {
        var self = this;
        var input = self.inputEl;
        var start = input.selectionStart;
        var end = input.selectionEnd;
        var currentValue = input.value;
        
        // Insert the text at cursor position
        var newValue = currentValue.substring(0, start) + text + currentValue.substring(end);
        input.value = newValue;
        
        // Set cursor position after the inserted text
        var newCursorPos = start + text.length;
        input.setSelectionRange(newCursorPos, newCursorPos);
        
        self.showPasteFeedback("Pasted " + text.length + " characters");
    }

    fallbackPaste() {
        var self = this;
        
        // Create a temporary textarea to trigger paste
        var textArea = document.createElement("textarea");
        textArea.style.position = "fixed";
        textArea.style.left = "-999999px";
        textArea.style.top = "-999999px";
        textArea.style.opacity = "0";
        
        document.body.appendChild(textArea);
        textArea.focus();
        
        // Listen for paste event on the textarea
        textArea.addEventListener('paste', function(e) {
            e.preventDefault();
            var pastedText = (e.clipboardData || window.clipboardData).getData('text');
            if (pastedText) {
                self.insertTextAtCursor(pastedText);
            } else {
                self.showPasteFeedback("Paste failed - no text data");
            }
            document.body.removeChild(textArea);
        });
        
        // Trigger paste
        try {
            document.execCommand('paste');
        } catch (err) {
            console.log('Fallback paste failed:', err);
            self.showPasteFeedback("Paste failed");
            document.body.removeChild(textArea);
        }
    }

    showPasteFeedback(message) {
        // Show temporary feedback
        var feedback = document.createElement("div");
        feedback.className = "webcli-paste-feedback";
        feedback.innerText = message;
        feedback.style.cssText = `
            position: absolute;
            top: 10px;
            right: 10px;
            background: #333;
            color: white;
            padding: 5px 10px;
            border-radius: 3px;
            font-size: 12px;
            z-index: 1000;
        `;
        
        this.ctrlEl.appendChild(feedback);
        
        setTimeout(function() {
            if (feedback.parentNode) {
                feedback.parentNode.removeChild(feedback);
            }
        }, 2000);
    }
}


