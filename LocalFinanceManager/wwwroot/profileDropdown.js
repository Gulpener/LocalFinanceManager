window.profileDropdown = {
    init: function (dotNetRef, elementId) {
        document.addEventListener('click', function handler(e) {
            var el = document.getElementById(elementId);
            if (el && !el.contains(e.target)) {
                dotNetRef.invokeMethodAsync('CloseDropdown');
                document.removeEventListener('click', handler);
            }
        });
    }
};
