// The companion of every page: the Tutor for a student (progress, tests, reviews), the class assistant for a teacher
// (every class's own follow-up). Which one depends on `data-api` on the panel, set by the layout from the user's role.
// Self-contained: it does not depend on common.js, which some pages load after it.
(() => {
    "use strict";
    const panel = document.getElementById("companion");
    const openBtn = document.getElementById("companion-open");
    if (!panel || !openBtn) return;
    const api = panel.dataset.api || "/api/companion";
    const T = JSON.parse(document.getElementById("i18n")?.textContent || "{}");
    const t = (k) => T[k] ?? k;
    const esc = (s) => String(s ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]);
    const lang = (document.documentElement.lang || "en").slice(0, 2);
    const $ = (id) => document.getElementById(id);
    const chat = $("companion-chat");
    const field = $("companion-text");
    let loaded = false;
    let busy = false;
    let speakOn = false;
    let style = null; // the metaphor Evo explains through this session ("game", "recipe", "story", "debate"), or null for the plain chat
    // The chip's own text is already localized server-side: read it rather than duplicating the 4 languages in JS.
    const styleLabel = (code) => panel.querySelector(`.companion-style[data-style="${code}"]`)?.textContent.trim() || code;
    const page = () => location.pathname;
    const title = () => (document.querySelector("main h1")?.textContent || document.title || "").trim().slice(0, 120);

    async function call(url, init) {
        const r = await fetch(url, init);
        if (r.status === 429) throw new Error(t("RateLimited"));
        if (!r.ok) throw new Error(t("GenericError"));
        return r.json();
    }

    function speak(text) {
        if (!speakOn || !("speechSynthesis" in window)) return;
        speechSynthesis.cancel();
        const u = new SpeechSynthesisUtterance(text);
        u.lang = { en: "en-GB", es: "es-ES", fr: "fr-FR", ca: "ca-ES" }[lang] || "en-GB";
        speechSynthesis.speak(u);
    }

    // A "Practice" action is a one-click nudge (a POST, no navigation): it stays on the page and reports back in the chat.
    // Every other kind is a plain link to somewhere on the platform the teacher can already see.
    function actionsHtml(actions) {
        if (!actions?.length) return "";
        return `<div class="mt-2 flex flex-col gap-1.5" data-testid="companion-actions">${actions.map((a) => a.kind === "Practice"
            ? `<button type="button" class="flex items-center gap-2 py-2 px-3 rounded-xl border border-layer-line bg-layer hover:bg-layer-hover text-sm text-foreground text-left w-full" data-action="${esc(a.id)}" data-nudge="${esc(a.url)}"><span aria-hidden="true">${esc(a.icon)}</span><span class="grow">${esc(a.label)}</span></button>`
            : `<a href="${esc(a.url)}" class="flex items-center gap-2 py-2 px-3 rounded-xl border border-layer-line bg-layer hover:bg-layer-hover text-sm text-foreground" data-action="${esc(a.id)}"><span aria-hidden="true">${esc(a.icon)}</span><span class="grow">${esc(a.label)}</span><span aria-hidden="true" class="text-muted-foreground-1">→</span></a>`).join("")}</div>`;
    }

    function bubble(fromStudent, text, actions) {
        const row = document.createElement("div");
        row.className = fromStudent ? "flex justify-end" : "flex items-start gap-2";
        row.innerHTML = fromStudent
            ? `<p class="py-2 px-3.5 rounded-2xl rounded-tr-sm bg-primary text-primary-foreground text-sm max-w-[85%] whitespace-pre-line">${esc(text)}</p>`
            : `<span class="size-8 shrink-0 rounded-full tone-lavender grid place-items-center" aria-hidden="true">🦉</span><div class="max-w-[85%] min-w-0"><p class="py-2 px-3.5 rounded-2xl rounded-tl-sm tone-sun text-sm text-foreground whitespace-pre-line" data-testid="companion-reply">${esc(text)}</p>${actionsHtml(actions)}</div>`;
        chat.appendChild(row);
        chat.scrollTop = chat.scrollHeight;
        return row;
    }

    async function load() {
        chat.innerHTML = "";
        try {
            const r = await call(`${api}?page=${encodeURIComponent(page())}&title=${encodeURIComponent(title())}`);
            r.messages.forEach((m) => bubble(m.fromUser, m.text));
            bubble(false, r.greeting, r.actions);
            loaded = true;
        } catch (e) {
            bubble(false, e.message);
        }
    }

    async function ask(text) {
        text = (text || "").trim();
        if (!text || busy) return;
        busy = true;
        panel.dataset.busy = "true";
        bubble(true, text);
        field.value = "";
        const wait = bubble(false, "…");
        try {
            const r = await call(api, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ message: text, page: page(), title: title(), style }) });
            wait.remove();
            bubble(false, r.reply, r.actions);
            speak(r.reply);
        } catch (e) {
            wait.remove();
            bubble(false, e.message);
        }
        busy = false;
        panel.dataset.busy = "false";
    }

    function open() {
        panel.hidden = false;
        openBtn.setAttribute("aria-expanded", "true");
        if (!loaded) load();
        setTimeout(() => field.focus(), 50);
    }

    function close() {
        panel.hidden = true;
        openBtn.setAttribute("aria-expanded", "false");
        if ("speechSynthesis" in window) speechSynthesis.cancel();
        openBtn.focus();
    }

    chat.addEventListener("click", async (e) => {
        const btn = e.target.closest("[data-nudge]");
        if (!btn || btn.disabled) return;
        btn.disabled = true;
        try {
            const r = await call(btn.dataset.nudge, { method: "POST" });
            bubble(false, r.saved > 0 ? t("PracticeNudgeSent") : t("PracticeNudgeFull"));
        } catch (err) {
            bubble(false, err.message);
            btn.disabled = false;
        }
    });

    openBtn.addEventListener("click", () => (panel.hidden ? open() : close()));
    $("companion-close").addEventListener("click", close);
    document.addEventListener("keydown", (e) => { if (e.key === "Escape" && !panel.hidden) close(); });
    $("companion-form").addEventListener("submit", (e) => { e.preventDefault(); ask(field.value); });
    field.addEventListener("keydown", (e) => { if (e.key === "Enter" && !e.shiftKey) { e.preventDefault(); ask(field.value); } });
    panel.querySelectorAll("[data-ask]").forEach((b) => b.addEventListener("click", () => ask(b.dataset.ask)));

    // The hub: "Talk" is the plain chat, "Your way" lets the student pick a metaphor (game, recipe, adventure, debate)
    // that Evo explains through from then on. Both modes share the same thread, form and history — only the style tag changes.
    const modes = $("companion-modes");
    if (modes) {
        const picker = $("companion-style-picker");
        const badge = $("companion-style-badge");
        const badgeLabel = $("companion-style-label");
        function paintTabs() {
            modes.querySelectorAll(".companion-tab").forEach((b) => {
                const on = b.dataset.mode === (modes.dataset.active || "chat");
                b.setAttribute("aria-selected", String(on));
                b.classList.toggle("bg-primary", on);
                b.classList.toggle("text-primary-foreground", on);
                b.classList.toggle("border-primary", on);
                b.classList.toggle("bg-layer", !on);
                b.classList.toggle("text-foreground", !on);
                b.classList.toggle("border-layer-line", !on);
            });
        }

        function showStyleTab() {
            modes.dataset.active = "style";
            picker.hidden = !!style;
            badge.hidden = !style;
            if (style) badgeLabel.textContent = styleLabel(style);
            paintTabs();
        }

        function showChatTab() {
            modes.dataset.active = "chat";
            style = null;
            picker.hidden = true;
            badge.hidden = true;
            paintTabs();
        }

        modes.querySelector('[data-mode="chat"]').addEventListener("click", showChatTab);
        modes.querySelector('[data-mode="style"]').addEventListener("click", showStyleTab);
        picker.querySelectorAll(".companion-style").forEach((b) => b.addEventListener("click", () => {
            style = b.dataset.style;
            picker.hidden = true;
            badge.hidden = false;
            badgeLabel.textContent = styleLabel(style);
            paintTabs();
            bubble(false, t("StylePicked").replace("{0}", styleLabel(style)));
            field.focus();
        }));
        $("companion-style-off").addEventListener("click", showChatTab);
        paintTabs();
    }

    $("companion-speak").addEventListener("click", (e) => {
        speakOn = !speakOn;
        e.currentTarget.setAttribute("aria-pressed", String(speakOn));
        e.currentTarget.textContent = speakOn ? "🔊" : "🔈";
        if (!speakOn && "speechSynthesis" in window) speechSynthesis.cancel();
    });

    $("companion-clear").addEventListener("click", async () => {
        if (!confirmClear()) return;
        try { await call(api, { method: "DELETE" }); } catch { }
        loaded = false;
        load();
    });
    // A second click confirms (no browser dialog).
    let armed = 0;
    function confirmClear() {
        const b = $("companion-clear");
        if (Date.now() - armed < 4000) { armed = 0; b.classList.remove("ring-2", "ring-red-500"); return true; }
        armed = Date.now();
        b.classList.add("ring-2", "ring-red-500");
        b.title = t("ClearConfirm");
        bubble(false, t("ClearConfirm"));
        setTimeout(() => b.classList.remove("ring-2", "ring-red-500"), 4000);
        return false;
    }

    // Dictation with the speech recognition of the browser, when there is one.
    const Recognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    if (Recognition) {
        const mic = $("companion-mic");
        mic.hidden = false;
        mic.addEventListener("click", () => {
            const r = new Recognition();
            r.lang = { en: "en-GB", es: "es-ES", fr: "fr-FR", ca: "ca-ES" }[lang] || "en-GB";
            mic.classList.add("animate-pulse");
            r.onresult = (e) => { field.value = (field.value ? field.value + " " : "") + e.results[0][0].transcript; field.focus(); };
            r.onend = () => mic.classList.remove("animate-pulse");
            r.start();
        });
    }
})();
