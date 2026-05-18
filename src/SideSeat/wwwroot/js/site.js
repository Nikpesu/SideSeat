(() => {
  const storageKey = "ss-theme";
  const root = document.documentElement;
  const toggleButton = document.getElementById("ss-theme-toggle");

  const getPreferredTheme = () => {
    const stored = localStorage.getItem(storageKey);
    if (stored === "light" || stored === "dark") {
      return stored;
    }

    return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
  };

  const setTheme = (theme) => {
    root.setAttribute("data-theme", theme);
    localStorage.setItem(storageKey, theme);

    if (toggleButton) {
      toggleButton.textContent = theme === "dark" ? "Tema: Dark" : "Tema: Light";
      toggleButton.setAttribute("aria-pressed", theme === "dark" ? "true" : "false");
    }
  };

  const initialTheme = getPreferredTheme();
  setTheme(initialTheme);

  if (toggleButton) {
    toggleButton.addEventListener("click", () => {
      const nextTheme = root.getAttribute("data-theme") === "dark" ? "light" : "dark";
      setTheme(nextTheme);
    });
  }
})();
