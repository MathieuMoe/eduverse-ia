// Shared helpers of the EduVerse pages (strings, formatting, API calls, copy buttons, AI fallback notice).
window.Edu = (() => {
    "use strict";
    const T = JSON.parse(document.getElementById("i18n")?.textContent || "{}");
    const lang = document.documentElement.lang || "en";
    const t = (key, fallback) => T[key] ?? fallback ?? key;
    const fmt = (s, ...a) => (s || "").replace(/\{(\d+)\}/g, (_, i) => a[+i] ?? "");
    const esc = (s) => String(s ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]);
    const money = (v) => new Intl.NumberFormat(lang, { style: "currency", currency: "EUR", maximumFractionDigits: 0 }).format(v || 0);
    const num = (v, d = 0) => new Intl.NumberFormat(lang, { maximumFractionDigits: d }).format(v || 0);
    const date = (v) => v ? new Intl.DateTimeFormat(lang, { day: "numeric", month: "short", year: "numeric" }).format(new Date(v)) : "–";
    const day = (d) => new Intl.DateTimeFormat(lang, { weekday: "long" }).format(new Date(2024, 0, 1 + ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"].indexOf(d)));

    async function api(url, options = {}) {
        const init = { ...options };
        if (options.json !== undefined) {
            init.method = init.method || "POST";
            init.headers = { "Content-Type": "application/json" };
            init.body = JSON.stringify(options.json);
            delete init.json;
        }
        const r = await fetch(url, init);
        if (r.status === 429) throw new Error(t("RateLimited"));
        let data = null;
        try { data = await r.json(); } catch { }
        if (!r.ok) {
            const e = new Error(data?.code === "ai_error" ? (data.detail ? fmt(t("AiErrorDetail"), data.detail) : t("AiError"))
                : t("Err_" + (data?.code || "generic"), r.status === 401 ? t("SessionExpired") : t("GenericError")));
            e.data = data;
            throw e;
        }
        return data;
    }

    /** Disables a button and shows a waiting label while the promise runs. */
    async function busy(button, promise) {
        const html = button.innerHTML;
        button.disabled = true;
        button.innerHTML = `<span class="animate-spin inline-block size-4 border-2 border-current border-t-transparent rounded-full" aria-hidden="true"></span>${esc(t("Working"))}`;
        try { return await promise; } finally { button.disabled = false; button.innerHTML = html; }
    }

    function copyButtons(root) {
        root.querySelectorAll("[data-copy-text]").forEach((b) => b.addEventListener("click", () => {
            navigator.clipboard?.writeText(b.dataset.copyText).then(() => {
                const before = b.textContent;
                b.textContent = t("Copied");
                setTimeout(() => { b.textContent = before; }, 1500);
            });
        }));
    }

    const copyBtn = (text) => `<button type="button" data-copy-text="${esc(text)}" class="py-1 px-2.5 text-xs font-semibold rounded-full border border-layer-line bg-layer text-foreground hover:bg-layer-hover">${esc(t("Copy"))}</button>`;
    const engineLabel = (e) => e === "demo" ? t("EngineDemo") : e === "code" || e === "queue" ? t("EngineCode") : e;
    const error = (el, message) => { el.textContent = message; el.classList.remove("hidden"); };
    const priorityClass = {
        High: "bg-red-100 text-red-800 dark:bg-red-500/15 dark:text-red-300",
        Medium: "bg-amber-100 text-amber-800 dark:bg-amber-500/15 dark:text-amber-300",
        Low: "bg-surface text-muted-foreground-1",
    };
    const badge = (text, cls) => `<span class="inline-flex items-center py-0.5 px-2 rounded-full text-xs font-medium ${cls}">${esc(text)}</span>`;

    /** Shows (or hides) the notice saying the AI failed and the offline version answered, with the reason. */
    function notice(el, d) {
        if (!el) return;
        if (d?.fallback) {
            el.textContent = fmt(t("Fallback"), d.fallback);
            el.classList.remove("hidden");
        } else {
            el.classList.add("hidden");
        }
    }

    /** Text of a .txt/.md file or a PDF (read in the browser with pdf.js, 30 pages max). */
    async function readFile(file) {
        if (file.type === "application/pdf" || file.name.toLowerCase().endsWith(".pdf")) {
            const pdfjs = await import("/lib/pdfjs/pdf.min.mjs");
            pdfjs.GlobalWorkerOptions.workerSrc = "/lib/pdfjs/pdf.worker.min.mjs";
            const pdf = await pdfjs.getDocument({ data: await file.arrayBuffer() }).promise;
            const pages = [];
            for (let i = 1; i <= Math.min(pdf.numPages, 30); i++) {
                const content = await (await pdf.getPage(i)).getTextContent();
                pages.push(content.items.map((x) => x.str + (x.hasEOL ? "\n" : " ")).join(""));
            }
            return pages.join("\n\n");
        }
        return file.text();
    }

    return { notice, readFile, t, fmt, esc, money, num, date, day, api, busy, copyButtons, copyBtn, engineLabel, error, priorityClass, badge, lang };
})();
