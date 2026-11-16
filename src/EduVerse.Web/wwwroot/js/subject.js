// Subject from a topic: the AI proposes the concepts and their order, the server validates the map (no cycle, known prerequisites).
(() => {
    "use strict";
    const { t, fmt, esc, api, busy, engineLabel } = window.Edu;
    const $ = (id) => document.getElementById(id);
    const form = $("sb-form");
    if (!form) return;
    const forStudent = form.dataset.student === "1";

    form.addEventListener("submit", async (e) => {
        e.preventDefault();
        $("sb-error").classList.add("hidden");
        try {
            const d = await busy($("sb-submit"), api("/api/subjects", { json: { topic: $("sb-topic").value, stage: $("sb-stage").value } }));
            const out = $("sb-result");
            out.classList.remove("hidden");
            const link = forStudent
                ? `<a class="py-2 px-3 inline-flex items-center gap-x-2 text-sm font-medium rounded-full bg-primary text-primary-foreground hover:bg-primary-hover" href="/Student/Learn?subject=${encodeURIComponent(d.id)}">${esc(t("StartLearning"))}</a>`
                : `<a class="py-2 px-3 inline-flex items-center gap-x-2 text-sm font-medium rounded-full bg-primary text-primary-foreground hover:bg-primary-hover" href="/Teacher">${esc(t("UseInClass"))}</a>`;
            out.innerHTML = `<div class="flex items-center justify-between gap-2"><h3 class="font-semibold text-foreground">${esc(d.name)}</h3><span class="text-[11px] text-muted-foreground-1">${esc(engineLabel(d.engine))}</span></div>
                <p class="mt-1 text-sm text-muted-foreground-1">${esc(fmt(t("SubjectCreated"), d.concepts.length))}</p>
                <ol class="mt-3 space-y-1 list-decimal ps-5 text-sm text-foreground">${d.concepts.map((c) => `<li>${esc(c)}</li>`).join("")}</ol>
                <div class="mt-4">${link}</div>`;
        } catch (err) {
            $("sb-error").textContent = err.message;
            $("sb-error").classList.remove("hidden");
        }
    });
})();
