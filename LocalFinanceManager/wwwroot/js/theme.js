window.theme = {
    set: (value) => document.documentElement.setAttribute("data-theme", value),
    getOsPreference: () =>
        window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light",
};
