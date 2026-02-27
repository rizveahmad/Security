// Security Platform – AJAX progressive-enhancement utilities
// Attach to elements with data-ajax-* attributes for unobtrusive AJAX support.

(function ($) {
    'use strict';

    // Generic helper: POST JSON via fetch with anti-forgery token.
    window.Security = window.Security || {};

    window.Security.postJson = function (url, data, onSuccess, onError) {
        const token = document.querySelector('input[name="__RequestVerificationToken"]');
        fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token ? token.value : ''
            },
            body: JSON.stringify(data)
        })
        .then(function (response) {
            if (!response.ok) throw new Error('Network response was not ok: ' + response.status);
            return response.json();
        })
        .then(onSuccess)
        .catch(onError || function (err) { console.error('AJAX error:', err); });
    };

})(jQuery);
