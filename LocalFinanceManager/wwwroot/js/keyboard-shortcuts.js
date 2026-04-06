window.downloadFileFromBytes = function (fileName, contentType, bytes) {
    const blob = new Blob([new Uint8Array(bytes)], { type: contentType });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};

window.localFinanceKeyboard = (function () {
    let globalHandler = null;
    let globalDotNetRef = null;
    let trapHandler = null;
    let trappedModal = null;
    let previouslyFocusedElement = null;

    function isEditableElement(target) {
        if (!target) {
            return false;
        }

        const tagName = (target.tagName || '').toLowerCase();
        return target.isContentEditable || tagName === 'input' || tagName === 'textarea' || tagName === 'select';
    }

    function getFocusableElements(container) {
        if (!container) {
            return [];
        }

        const selector = [
            'a[href]',
            'button:not([disabled])',
            'textarea:not([disabled])',
            'input:not([disabled])',
            'select:not([disabled])',
            '[tabindex]:not([tabindex="-1"])'
        ].join(',');

        return Array.from(container.querySelectorAll(selector))
            .filter(element => element.offsetParent !== null || element === document.activeElement);
    }

    return {
        isTouchDevice: function () {
            return (navigator.maxTouchPoints || 0) > 0;
        },

        getOperatingSystem: function () {
            const userAgent = navigator.userAgent || '';
            if (/Mac/i.test(userAgent)) {
                return 'macos';
            }
            if (/Win/i.test(userAgent)) {
                return 'windows';
            }
            if (/Linux/i.test(userAgent)) {
                return 'linux';
            }
            return 'unknown';
        },

        registerGlobalShortcuts: function (dotNetRef, callbackMethodName) {
            this.unregisterGlobalShortcuts();
            globalDotNetRef = dotNetRef;
            const callback = callbackMethodName || 'HandleGlobalShortcut';

            globalHandler = function (event) {
                if (event.defaultPrevented || !globalDotNetRef) {
                    return;
                }

                const key = event.key || '';
                const normalizedKey = key.length === 1 ? key.toLowerCase() : key;
                const hasOpenModal = !!document.querySelector('.modal.show');

                if (normalizedKey === '?' && !isEditableElement(event.target) && !hasOpenModal) {
                    event.preventDefault();
                    globalDotNetRef.invokeMethodAsync(callback, '?');
                }

                if (normalizedKey === 'Escape') {
                    globalDotNetRef.invokeMethodAsync(callback, 'Escape');
                }
            };

            document.addEventListener('keydown', globalHandler, true);
        },

        unregisterGlobalShortcuts: function () {
            if (globalHandler) {
                document.removeEventListener('keydown', globalHandler, true);
                globalHandler = null;
            }
            globalDotNetRef = null;
        },

        trapFocus: function (modalSelector) {
            this.releaseFocusTrap();

            trappedModal = document.querySelector(modalSelector);
            if (!trappedModal) {
                return;
            }

            previouslyFocusedElement = document.activeElement;

            trapHandler = function (event) {
                if (event.key !== 'Tab' || !trappedModal) {
                    return;
                }

                const focusable = getFocusableElements(trappedModal);
                if (focusable.length === 0) {
                    event.preventDefault();
                    return;
                }

                const first = focusable[0];
                const last = focusable[focusable.length - 1];

                if (event.shiftKey && document.activeElement === first) {
                    event.preventDefault();
                    last.focus();
                } else if (!event.shiftKey && document.activeElement === last) {
                    event.preventDefault();
                    first.focus();
                }
            };

            document.addEventListener('keydown', trapHandler, true);
        },

        releaseFocusTrap: function () {
            if (trapHandler) {
                document.removeEventListener('keydown', trapHandler, true);
                trapHandler = null;
            }

            trappedModal = null;

            if (previouslyFocusedElement && typeof previouslyFocusedElement.focus === 'function') {
                previouslyFocusedElement.focus();
            }
            previouslyFocusedElement = null;
        },

        focusSelector: function (selector) {
            const element = document.querySelector(selector);
            if (element && typeof element.focus === 'function') {
                element.focus();
                return true;
            }
            return false;
        },

        isActiveElement: function (selector) {
            const element = document.querySelector(selector);
            return !!element && element === document.activeElement;
        },

        printPage: function () {
            window.print();
        }
    };
})();