window.profileDropdown = {
    init: function (dotNetRef, elementId) {
        // Defer registration by one event loop tick so the click that opened
        // the dropdown doesn't immediately trigger the outside-click handler.
        setTimeout(function () {
            document.addEventListener('click', function handler(e) {
                var el = document.getElementById(elementId);
                if (el && !el.contains(e.target)) {
                    dotNetRef.invokeMethodAsync('CloseDropdown');
                    document.removeEventListener('click', handler);
                }
            });
        }, 0);
    }
};
