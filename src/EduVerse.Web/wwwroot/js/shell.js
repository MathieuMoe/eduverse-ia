// Interface shell: display preferences (sidebar, text size, theme, accent, motion, focus mode), shortcuts and the Ctrl+K quick navigation.
// Preferences are kept in this browser only (localStorage "ev_ui"; the theme in "hs_theme", Preline's convention).
(() => {
    "use strict";
    const T = JSON.parse(document.getElementById("i18n")?.textContent || "{}");
    const t = (k, f) => T[k] ?? f ?? k;
    const html = document.documentElement;
    const KEY = "ev_ui";
    const read = () => { try { return JSON.parse(localStorage.getItem(KEY) || "{}"); } catch { return {}; } };
    const save = (ui) => { try { localStorage.setItem(KEY, JSON.stringify(ui)); } catch { } };
    const theme = () => { try { return localStorage.getItem("hs_theme") || "auto"; } catch { return "auto"; } };
    const dark = () => matchMedia("(prefers-color-scheme: dark)").matches;

    function apply(ui) {
        html.classList.toggle("sb-collapsed", ui.sidebar === "collapsed");
        if (ui.accent && ui.accent !== "plum") html.dataset.accent = ui.accent; else delete html.dataset.accent;
        html.style.fontSize = ui.text === "s" ? "15px" : ui.text === "l" ? "17.5px" : "";
        html.classList.toggle("reduce-motion", ui.motion === "reduce");
        const th = theme();
        html.classList.toggle("dark", th === "dark" || (th === "auto" && dark()));
        sync(ui);
    }

    // Shows the current choice in the Display menu (aria-pressed)
    function sync(ui) {
        const current = { sidebar: ui.sidebar || "expanded", text: ui.text || "m", accent: ui.accent || "plum", theme: theme() };
        document.querySelectorAll("[data-ui]").forEach((b) => b.setAttribute("aria-pressed", String(current[b.dataset.ui] === b.dataset.value)));
        document.querySelectorAll("[data-ui-check=motion]").forEach((c) => { c.checked = ui.motion === "reduce"; });
    }

    function set(name, value) {
        const ui = read();
        if (name === "theme") {
            try { localStorage.setItem("hs_theme", value); } catch { }
        } else {
            ui[name] = value;
            save(ui);
        }
        apply(ui);
    }

    const toggleSidebar = () => set("sidebar", read().sidebar === "collapsed" ? "expanded" : "collapsed");
    const toggleTheme = () => set("theme", html.classList.contains("dark") ? "light" : "dark");

    /** Focus mode (learning page): hides the sidebar and the side panels to keep only the exercise. */
    function setFocus(on) {
        const ui = read();
        ui.focus = on;
        save(ui);
        html.classList.toggle("focus-mode", on);
        document.querySelectorAll("[data-focus-toggle]").forEach((b) => b.setAttribute("aria-pressed", String(on)));
    }

    document.addEventListener("click", (e) => {
        const b = e.target.closest("[data-ui]");
        if (b) { set(b.dataset.ui, b.dataset.value); return; }
        if (e.target.closest("[data-ui-toggle=sidebar]")) { toggleSidebar(); return; }
        if (e.target.closest("#theme-toggle")) { toggleTheme(); return; }
        if (e.target.closest("[data-focus-toggle]")) { setFocus(!html.classList.contains("focus-mode")); return; }
        if (e.target.closest("[data-ui-reset]")) {
            save({});
            try { localStorage.removeItem("hs_theme"); } catch { }
            html.classList.remove("focus-mode");
            apply({});
            return;
        }
        if (e.target.closest("[data-palette-open]")) open();
    });
    document.addEventListener("change", (e) => {
        if (e.target.matches("[data-ui-check=motion]")) set("motion", e.target.checked ? "reduce" : "");
    });
    matchMedia("(prefers-color-scheme: dark)").addEventListener("change", () => apply(read()));

    // ---------- Greeting by the time of day, on the user's clock ----------
    document.querySelectorAll("[data-greeting]").forEach((el) => {
        const h = new Date().getHours();
        const key = h < 5 ? "Greet_Evening" : h < 12 ? "Greet_Morning" : h < 19 ? "Greet_Afternoon" : "Greet_Evening";
        el.textContent = t(key, el.textContent).replace("{0}", el.dataset.greeting);
    });

    // ---------- Quick navigation (Ctrl+K) ----------
    const palette = document.getElementById("palette");
    const input = document.getElementById("palette-input");
    const list = document.getElementById("palette-list");
    let items = [];
    let active = 0;
    const esc = (s) => String(s ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]);

    function commands() {
        const nav = [...document.querySelectorAll("a[data-nav]")].map((a) => ({
            label: a.querySelector("span:last-child")?.textContent.trim() || a.title, hint: t("Palette_Go"), run: () => { location.href = a.href; },
        }));
        const langs = [...document.querySelectorAll("a[data-palette-lang]")].map((a) => ({
            label: a.dataset.paletteLang, hint: t("Palette_Language"), run: () => { location.href = a.href; },
        }));
        const actions = [
            { label: t("Cmd_ToggleTheme"), hint: t("Palette_Action"), run: toggleTheme },
            { label: t("Cmd_ToggleSidebar"), hint: "[", run: toggleSidebar },
            { label: t("Cmd_TextLarger"), hint: t("Palette_Action"), run: () => set("text", read().text === "s" ? "m" : "l") },
            { label: t("Cmd_TextSmaller"), hint: t("Palette_Action"), run: () => set("text", read().text === "l" ? "m" : "s") },
        ];
        if (document.querySelector("[data-focus-toggle]")) actions.push({ label: t("Cmd_Focus"), hint: "F", run: () => setFocus(!html.classList.contains("focus-mode")) });
        const logout = document.querySelector("[data-palette-logout]");
        if (logout) actions.push({ label: t("Cmd_Logout"), hint: t("Palette_Action"), run: () => logout.click() });
        return [...nav, ...actions, ...langs];
    }

    function render() {
        const q = input.value.trim().toLowerCase().normalize("NFD").replace(/\p{M}/gu, "");
        const all = commands();
        items = q ? all.filter((c) => c.label.toLowerCase().normalize("NFD").replace(/\p{M}/gu, "").includes(q)) : all;
        active = Math.min(active, Math.max(0, items.length - 1));
        list.innerHTML = items.map((c, i) => `<li role="option" aria-selected="${i === active}" data-i="${i}"
            class="flex items-center justify-between gap-3 py-2.5 px-3 rounded-xl text-sm cursor-pointer ${i === active ? "bg-dropdown-item-hover text-foreground" : "text-foreground/90"}">
            <span class="truncate">${esc(c.label)}</span><span class="shrink-0 text-xs text-muted-foreground-1">${esc(c.hint)}</span></li>`).join("")
            || `<li class="py-6 text-center text-sm text-muted-foreground-1">${esc(t("Palette_NoResults"))}</li>`;
        list.querySelector('[aria-selected="true"]')?.scrollIntoView({ block: "nearest" });
    }

    function open() {
        if (!palette) return;
        palette.classList.remove("hidden");
        input.value = "";
        active = 0;
        render();
        input.focus();
    }

    const close = () => palette?.classList.add("hidden");
    const run = (i) => { const c = items[i]; if (c) { close(); c.run(); } };

    input?.addEventListener("input", () => { active = 0; render(); });
    input?.addEventListener("keydown", (e) => {
        if (e.key === "ArrowDown") { active = Math.min(items.length - 1, active + 1); render(); e.preventDefault(); }
        else if (e.key === "ArrowUp") { active = Math.max(0, active - 1); render(); e.preventDefault(); }
        else if (e.key === "Enter") { run(active); e.preventDefault(); }
    });
    list?.addEventListener("click", (e) => { const li = e.target.closest("[data-i]"); if (li) run(+li.dataset.i); });
    palette?.addEventListener("click", (e) => { if (e.target === palette) close(); });

    // ---------- Keyboard shortcuts ----------
    const typing = (el) => el && (el.isContentEditable || /^(input|textarea|select)$/i.test(el.tagName));
    document.addEventListener("keydown", (e) => {
        if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "k") { e.preventDefault(); palette?.classList.contains("hidden") ? open() : close(); return; }
        if (e.key === "Escape") {
            if (!palette?.classList.contains("hidden")) { close(); return; }
            if (html.classList.contains("focus-mode")) setFocus(false);
            return;
        }
        if (typing(e.target) || e.ctrlKey || e.metaKey || e.altKey) return;
        if (e.key === "[") { toggleSidebar(); e.preventDefault(); }
        else if (e.key === "/") { open(); e.preventDefault(); }
        else if (e.key.toLowerCase() === "f" && document.querySelector("[data-focus-toggle]")) { setFocus(!html.classList.contains("focus-mode")); e.preventDefault(); }
    });

    sync(read());
    document.querySelectorAll("[data-focus-toggle]").forEach((b) => b.setAttribute("aria-pressed", String(html.classList.contains("focus-mode"))));
})();
