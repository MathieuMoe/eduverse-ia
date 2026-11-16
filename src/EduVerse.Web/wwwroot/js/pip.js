// "Correct Pip": Pip (a student-AI) solved a problem with one mistake. The student taps the first wrong line, then says what went wrong.
// The server generates the problem and keeps only the seed; the answer is checked there.
(() => {
    "use strict";
    const { t, fmt, esc, api } = window.Edu;
    const card = document.getElementById("pp-card");
    let subject = "maths";
    if (!card) return;
    const light = "py-2 px-4 inline-flex justify-center items-center gap-x-2 text-sm font-semibold rounded-full border border-layer-line bg-layer text-foreground shadow-2xs hover:bg-layer-hover";
    const primary = "py-2 px-4 inline-flex justify-center items-center gap-x-2 text-sm font-semibold rounded-full bg-primary text-primary-foreground hover:bg-primary-hover";
    const row = "w-full text-start py-3 px-4 rounded-2xl border border-layer-line bg-layer text-foreground hover:bg-layer-hover aria-pressed:ring-2 aria-pressed:ring-primary";

    const fail = (err) => {
        card.innerHTML = `<p class="text-sm text-red-700 dark:text-red-300" role="alert" data-testid="pip-error">${esc(t("Err_" + (err.data?.code || "generic"), err.message))}</p><button type="button" class="${light}" id="pp-start">${esc(t("PipAgain"))}</button>`;
        document.getElementById("pp-start").addEventListener("click", () => start(subject));
    };

    function show(p) {
        let line = null, reason = null;
        card.innerHTML = `${p.from ? `<p class="p-2.5 rounded-xl tone-lavender text-sm text-foreground" data-testid="pip-own">🪞 ${esc(fmt(t("PipOwn"), p.from))}</p>` : ""}
            <p class="text-xs text-muted-foreground-1">${esc(t("PipSolved"))}</p>
            <p class="text-2xl font-semibold text-foreground font-mono" data-testid="pip-problem">${esc(p.problem)}</p>
            <div><p class="mb-2 text-sm font-medium text-foreground">1. ${esc(t("PipTapLine"))}</p>
                <ol class="space-y-2 font-mono text-sm" data-testid="pip-lines">${p.lines.map((x, i) => `<li><button type="button" class="${row}" data-line="${i}" aria-pressed="false" data-testid="pip-line-${i}">${i + 1}. ${esc(x)}</button></li>`).join("")}</ol></div>
            <div><p class="mb-2 text-sm font-medium text-foreground">2. ${esc(t("PipWhy"))}</p>
                <div class="space-y-2" data-testid="pip-reasons">${p.reasons.map((x, i) => `<button type="button" class="${row}" data-reason="${i}" aria-pressed="false" data-testid="pip-reason-${i}">${esc(x)}</button>`).join("")}</div></div>
            <div class="flex flex-wrap items-center gap-3"><button type="button" class="${primary}" id="pp-send" data-testid="pip-send">${esc(t("PipCheck"))}</button>
                <span class="text-sm text-red-700 dark:text-red-300 hidden" role="alert" id="pp-error"></span></div>`;
        const pick = (attr, set) => card.querySelectorAll(`[${attr}]`).forEach((b) => b.addEventListener("click", () => {
            card.querySelectorAll(`[${attr}]`).forEach((x) => x.setAttribute("aria-pressed", "false"));
            b.setAttribute("aria-pressed", "true");
            set(+b.getAttribute(attr));
        }));
        pick("data-line", (v) => { line = v; });
        pick("data-reason", (v) => { reason = v; });
        document.getElementById("pp-send").addEventListener("click", async (e) => {
            const error = document.getElementById("pp-error");
            if (line === null || reason === null) { error.textContent = t("PipPick"); error.classList.remove("hidden"); return; }
            e.currentTarget.disabled = true;
            try {
                const r = await api(`/api/pip/${encodeURIComponent(p.id)}/answer`, { json: { line, reason } });
                const both = r.lineOk && r.reasonOk;
                card.innerHTML = `<div data-testid="pip-result" data-ok="${both}">
                    <p class="text-lg font-semibold text-foreground">${both ? "🎉 " + esc(t("PipBoth")) : r.lineOk ? "👍 " + esc(t("PipLineOnly")) : "🤔 " + esc(t("PipMissed"))}</p>
                    <p class="mt-2 text-sm text-foreground">${esc(r.fix)}</p>
                    <ol class="mt-3 space-y-1 font-mono text-sm">${r.right.map((x, i) => `<li class="${i === r.wrongStep ? "font-semibold text-emerald-700 dark:text-emerald-300" : "text-foreground"}">${i + 1}. ${esc(x)}</li>`).join("")}</ol>
                    ${r.reward?.xp ? `<p class="mt-2 text-sm font-semibold text-foreground">⭐ +${r.reward.xp} XP</p>` : ""}
                    <p class="mt-3 p-3 rounded-2xl tone-sun text-sm text-foreground" id="pp-voice" data-testid="pip-voice" aria-live="polite">🐣 …</p>
                    <div class="mt-4"><button type="button" class="${primary}" id="pp-start" data-testid="pip-next">${esc(t("PipAgain"))}</button></div></div>`;
                document.getElementById("pp-start").addEventListener("click", () => start(subject));
                try {
                    const v = await api(`/api/pip/${encodeURIComponent(p.id)}/voice`, { method: "POST" });
                    document.getElementById("pp-voice").textContent = "🐣 " + v.reply;
                } catch { document.getElementById("pp-voice")?.remove(); }
            } catch (err) { fail(err); }
        });
    }

    async function start(which) {
        subject = which;
        card.innerHTML = `<p class="text-sm text-muted-foreground-1">${esc(t("PipLoading"))}</p>`;
        try { show(await api("/api/pip/start", { json: { subject } })); } catch (err) { fail(err); }
    }

    card.querySelectorAll("[data-pip-start]").forEach((b) => b.addEventListener("click", () => start(b.dataset.pipStart)));
})();
