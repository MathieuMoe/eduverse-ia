// The Tutor of every page of the student. The panel loads the conversation and the ideas (computed by the server
// from the tests, topics and reviews of the student) and sends the page the student is on. Self-contained: it does not
// depend on common.js, which some pages load after it.
(() => {
    "use strict";
    const panel = document.getElementById("companion");
    const openBtn = document.getElementById("companion-open");
    if (!panel || !openBtn) return;
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

    function actionsHtml(actions) {
        if (!actions?.length) return "";
        return `<div class="mt-2 flex flex-col gap-1.5" data-testid="companion-actions">${actions.map((a) =>
            `<a href="${esc(a.url)}" class="flex items-center gap-2 py-2 px-3 rounded-xl border border-layer-line bg-layer hover:bg-layer-hover text-sm text-foreground" data-action="${esc(a.id)}"><span aria-hidden="true">${esc(a.icon)}</span><span class="grow">${esc(a.label)}</span><span aria-hidden="true" class="text-muted-foreground-1">→</span></a>`).join("")}</div>`;
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
            const r = await call(`/api/companion?page=${encodeURIComponent(page())}&title=${encodeURIComponent(title())}`);
            r.messages.forEach((m) => bubble(m.fromStudent, m.text));
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
            const r = await call("/api/companion", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ message: text, page: page(), title: title() }) });
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

    openBtn.addEventListener("click", () => (panel.hidden ? open() : close()));
    $("companion-close").addEventListener("click", close);
    document.addEventListener("keydown", (e) => { if (e.key === "Escape" && !panel.hidden) close(); });
    $("companion-form").addEventListener("submit", (e) => { e.preventDefault(); ask(field.value); });
    field.addEventListener("keydown", (e) => { if (e.key === "Enter" && !e.shiftKey) { e.preventDefault(); ask(field.value); } });
    panel.querySelectorAll("[data-ask]").forEach((b) => b.addEventListener("click", () => ask(b.dataset.ask)));

    $("companion-speak").addEventListener("click", (e) => {
        speakOn = !speakOn;
        e.currentTarget.setAttribute("aria-pressed", String(speakOn));
        e.currentTarget.textContent = speakOn ? "🔊" : "🔈";
        if (!speakOn && "speechSynthesis" in window) speechSynthesis.cancel();
    });

    $("companion-clear").addEventListener("click", async () => {
        if (!confirmClear()) return;
        try { await call("/api/companion", { method: "DELETE" }); } catch { }
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
