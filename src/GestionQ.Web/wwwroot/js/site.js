// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Global listener to force uppercase on all text inputs
document.addEventListener('input', function (e) {
    if (e.target && (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA')) {
        // Only apply to text-based inputs
        const type = e.target.type ? e.target.type.toLowerCase() : '';
        if (type === 'text' || type === 'search' || e.target.tagName === 'TEXTAREA') {
            // Check if element has a data attribute preventing uppercase (just in case)
            if (e.target.dataset.noUppercase === "true" || type === 'email' || type === 'password') {
                return;
            }
            
            const start = e.target.selectionStart;
            const end = e.target.selectionEnd;
            const oldVal = e.target.value;
            const newVal = oldVal.toUpperCase();
            
            if (oldVal !== newVal) {
                e.target.value = newVal;
                // Restore selection to prevent cursor jumping to the end
                if (start !== null && end !== null) {
                    e.target.setSelectionRange(start, end);
                }
            }
        }
    }
});
