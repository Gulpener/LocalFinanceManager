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
            return (navigator.maxTouchPoints || 0) > 0 && ('ontouchstart' in window || navigator.msMaxTouchPoints > 0);
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
                const path = (window.location && window.location.pathname || '').toLowerCase();

                if (path === '/transactions' && !hasOpenModal && !isEditableElement(event.target)) {
                    const rows = Array.from(document.querySelectorAll("tr[data-testid='transaction-row']"));
                    const activeRow = document.activeElement && document.activeElement.closest
                        ? document.activeElement.closest("tr[data-testid='transaction-row']")
                        : null;
                    const rowIndex = activeRow ? Number(activeRow.getAttribute('data-row-index') || '0') : 0;

                    if ((event.ctrlKey || event.metaKey) && normalizedKey === 'a') {
                        event.preventDefault();
                        rows.forEach(row => {
                            const checkbox = row.querySelector("input[type='checkbox']");
                            if (checkbox && !checkbox.checked) {
                                checkbox.click();
                            }
                        });
                    }

                    if ((event.ctrlKey || event.metaKey) && normalizedKey === 'd') {
                        event.preventDefault();
                        rows.forEach(row => {
                            const checkbox = row.querySelector("input[type='checkbox']");
                            if (checkbox && checkbox.checked) {
                                checkbox.click();
                            }
                        });
                    }

                    if (key === ' ' || key === 'Spacebar' || key === 'Space') {
                        event.preventDefault();
                        const targetRow = activeRow || rows[0];
                        const checkbox = targetRow && targetRow.querySelector("input[type='checkbox']");
                        if (checkbox) {
                            checkbox.click();
                        }
                    }

                    if (normalizedKey === '/') {
                        event.preventDefault();
                        const filterElement = document.querySelector('#assignmentStatusFilter');
                        if (filterElement && typeof filterElement.focus === 'function') {
                            filterElement.focus();
                        }
                    }

                    if (normalizedKey === 'arrowdown') {
                        event.preventDefault();
                        const nextIndex = Math.min(rowIndex + 1, Math.max(rows.length - 1, 0));
                        const nextRow = rows[nextIndex];
                        if (nextRow && typeof nextRow.focus === 'function') {
                            nextRow.focus();
                        }
                    }

                    if (normalizedKey === 'arrowup') {
                        event.preventDefault();
                        const prevIndex = Math.max(rowIndex - 1, 0);
                        const prevRow = rows[prevIndex];
                        if (prevRow && typeof prevRow.focus === 'function') {
                            prevRow.focus();
                        }
                    }

                    if (normalizedKey === 'home') {
                        event.preventDefault();
                        const firstRow = rows[0];
                        if (firstRow && typeof firstRow.focus === 'function') {
                            firstRow.focus();
                        }
                    }

                    if (normalizedKey === 'end') {
                        event.preventDefault();
                        const lastRow = rows[rows.length - 1];
                        if (lastRow && typeof lastRow.focus === 'function') {
                            lastRow.focus();
                        }
                    }

                    if (normalizedKey === 'enter') {
                        const targetRow = activeRow || rows[0];
                        const assignButton = targetRow && targetRow.querySelector("button.btn-outline-primary");
                        if (assignButton && typeof assignButton.click === 'function') {
                            event.preventDefault();
                            assignButton.click();
                        }
                    }
                }

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