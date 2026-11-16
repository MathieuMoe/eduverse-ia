// "Step by step" (fading examples): the Tutor solves a whole problem, then the last result is missing, then every result,
// and at the end the student solves one alone. The server builds and checks every problem; this page only shows it.
(() => {
    "use strict";
    const { t, fmt, esc, api } = window.Edu;
    const card = document.getElementById("fd-card");
    if (!card) return;
    const picker = card.innerHTML;
    const light = "py-2 px-4 inline-flex justify-center items-center gap-x-2 text-sm font-semibold rounded-full border border-layer-line bg-layer text-foreground shadow-2xs hover:bg-layer-hover";
    const primary = "py-2 px-4 inline-flex justify-center items-center gap-x-2 text-sm font-semibold rounded-full bg-primary text-primary-foreground hover:bg-primary-hover";
    const input = "w-24 py-1.5 px-3 bg-layer border border-layer-line rounded-xl text-sm text-foreground font-mono";

    const wirePicker = () => card.querySelectorAll("[data-fading-kind]").forEach((b) => b.addEventListener("click", () => start(b.dataset.fadingKind)));
    const fail = (err) => {
        card.innerHTML = `<p class="text-sm text-red-700 dark:text-red-300" role="alert" data-testid="fading-error">${esc(t("Err_" + (err.data?.code || "generic"), err.message))}</p>` + picker;
        wirePicker();
    };

    function show(v, note) {
        if (v.done) {
            card.innerHTML = `<div data-testid="fading-finished"><p class="text-lg font-semibold text-foreground">🎉 ${esc(t("FadingFinished"))}</p>${note || ""}</div>` + picker;
            wirePicker();
            return;
        }
        const dots = Array.from({ length: v.stages }, (_, i) => `<span class="size-2.5 rounded-full ${i <= v.stage ? "bg-primary" : "bg-layer-line"}"></span>`).join("");
        let blank = 0;
        const lines = v.lines.map((line, i) => {
            const work = line.work === null ? "…" : esc(line.work);
            const result = line.result === null
                ? `<label class="sr-only" for="fd-a${blank}">${esc(fmt(t("FadingResultOf"), i + 1))}</label><input id="fd-a${blank}" class="${input}" inputmode="numeric" autocomplete="off" maxlength="12" data-answer="${blank++}" data-testid="fading-input" />`
                : `<span class="font-semibold">${esc(String(line.result))}</span>`;
            return `<li class="flex flex-wrap items-center gap-2 font-mono text-sm text-foreground" data-testid="fading-line">${i + 1}. ${work} = ${result}</li>`;
        }).join("");
        const alone = v.stage >= v.stages - 1
            ? `<div class="flex flex-wrap items-center gap-2 text-sm text-foreground"><label for="fd-a0">${esc(t("FadingAnswer"))}</label><input id="fd-a0" class="${input}" inputmode="numeric" autocomplete="off" maxlength="12" data-answer="0" data-testid="fading-input" /></div>` : "";
        card.innerHTML = `${note || ""}
            <div class="flex items-center gap-3"><span class="flex gap-1.5" aria-hidden="true">${dots}</span><span class="text-xs text-muted-foreground-1" data-testid="fading-stage" data-stage="${v.stage}">${esc(t("FadingStage" + v.stage))}</span></div>
            <p class="text-2xl font-semibold text-foreground font-mono" data-testid="fading-problem">${esc(v.problem)}</p>
            <ol class="space-y-2">${lines}</ol>${alone}
            <div class="flex flex-wrap items-center gap-3"><button type="button" class="${primary}" id="fd-send" data-testid="fading-send">${esc(t(v.stage === 0 ? "FadingRead" : "FadingCheck"))}</button>
                <button type="button" class="${light}" id="fd-quit">${esc(t("FadingOther"))}</button></div>`;
        card.querySelector("[data-answer]")?.focus();
        document.getElementById("fd-quit").addEventListener("click", () => { card.innerHTML = picker; wirePicker(); });
        const send = async () => {
            const answers = [...card.querySelectorAll("[data-answer]")].map((x) => x.value);
            document.getElementById("fd-send").disabled = true;
            try {
                const r = await api(`/api/fading/${encodeURIComponent(v.id)}/answer`, { json: { answers } });
                const solved = `<ol class="mt-1 space-y-0.5 font-mono text-sm">${r.solution.map((s, i) => `<li>${i + 1}. ${esc(s.work)} = <span class="font-semibold">${esc(String(s.result))}</span></li>`).join("")}</ol>`;
                const xp = r.reward?.xp ? ` ⭐ +${r.reward.xp} XP` : "";
                const msg = v.stage === 0 ? "" : r.correct
                    ? `<p class="p-2.5 rounded-xl tone-mint text-sm text-foreground" data-testid="fading-result" data-ok="true">✅ ${esc(t("FadingRight"))}${xp}</p>`
                    : `<div class="p-2.5 rounded-xl tone-pink text-sm text-foreground" data-testid="fading-result" data-ok="false">💡 ${esc(t("FadingWrong"))}${solved}</div>`;
                show(r.next, msg);
            } catch (err) { fail(err); }
        };
        document.getElementById("fd-send").addEventListener("click", send);
        card.querySelectorAll("[data-answer]").forEach((x) => x.addEventListener("keydown", (e) => { if (e.key === "Enter") send(); }));
    }

    async function start(kind) {
        try { show(await api("/api/fading/start", { json: { kind } })); } catch (err) { fail(err); }
    }

    wirePicker();
})();
