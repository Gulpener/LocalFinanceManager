window.theme = {
    set: (value) => document.documentElement.setAttribute("data-theme", value),
    getOsPreference: () =>
        window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light",
};

window.sidebar = {
    getCollapsed: () => localStorage.getItem("sidebar-collapsed") === "true",
    setCollapsed: (value) => localStorage.setItem("sidebar-collapsed", String(value)),
};
