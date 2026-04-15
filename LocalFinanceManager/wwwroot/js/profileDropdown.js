var profileDropdownHandlers = {};

function cleanupProfileDropdown(elementId) {
    var registration = profileDropdownHandlers[elementId];
    if (!registration) {
        return;
    }

    if (registration.timeoutId) {
        clearTimeout(registration.timeoutId);
    }

    if (registration.handler) {
        document.removeEventListener('click', registration.handler);
    }

    delete profileDropdownHandlers[elementId];
}

window.profileDropdown = {
    init: function (dotNetRef, elementId) {
        cleanupProfileDropdown(elementId);

        // Defer registration by one event loop tick so the click that opened
        // the dropdown doesn't immediately trigger the outside-click handler.
        var timeoutId = setTimeout(function () {
            var handler = function (e) {
                var el = document.getElementById(elementId);
                if (el && !el.contains(e.target)) {
                    cleanupProfileDropdown(elementId);
                    dotNetRef.invokeMethodAsync('CloseDropdown').catch(function () {
                        // Ignore expected failures when the Blazor component has
                        // already been disposed during navigation/circuit teardown.
                    });
                }
            };

            profileDropdownHandlers[elementId] = {
                handler: handler,
                timeoutId: null
            };

            document.addEventListener('click', handler);
        }, 0);

        profileDropdownHandlers[elementId] = {
            handler: null,
            timeoutId: timeoutId
        };
    },

    dispose: function (elementId) {
        cleanupProfileDropdown(elementId);
    }
};
