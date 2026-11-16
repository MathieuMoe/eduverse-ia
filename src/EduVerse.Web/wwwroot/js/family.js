// Message to a student's family: the facts are measured by the platform, the AI (or the offline template) writes the text.
(() => {
    "use strict";
    const { t, fmt, esc, api, busy, notice, engineLabel, error, copyBtn, copyButtons } = window.Edu;
    const $ = (id) => document.getElementById(id);
    let student = null;

    document.addEventListener("click", (e) => {
        const b = e.target.closest("[data-family]");
        if (!b) return;
        student = b.dataset.family;
        $("family-empty").classList.add("hidden");
        $("family-form").classList.remove("hidden");
        $("family-result").classList.add("hidden");
        notice($("family-notice"), null);
        $("family-student").textContent = fmt(t("FamilyFor"), b.dataset.name);
        if (matchMedia("(max-width: 1279px)").matches) $("family-panel").scrollIntoView({ behavior: "smooth" });
    });

    $("family-form").addEventListener("submit", async (e) => {
        e.preventDefault();
        $("family-error").classList.add("hidden");
        try {
            const d = await busy($("family-submit"), api("/api/institution/family", {
                json: { studentId: student, tone: $("family-tone").value, language: $("family-language").value, note: $("family-note").value || null },
            }));
            notice($("family-notice"), d);
            const m = d.message;
            const text = `${m.subject}\n\n${m.body}`;
            const out = $("family-result");
            out.classList.remove("hidden");
            out.innerHTML = `<div class="flex items-center justify-between gap-2"><h3 class="text-sm font-semibold text-foreground">${esc(t("FamilyEmail"))}</h3>
                    <span class="flex items-center gap-2"><span class="text-[11px] text-muted-foreground-1">${esc(engineLabel(d.engine))}</span>${copyBtn(text)}</span></div>
                <div class="mt-2 rounded-2xl tone-lavender p-4 text-sm">
                    <p class="font-semibold text-foreground">${esc(m.subject)}</p>
                    <p class="mt-2 whitespace-pre-line text-foreground">${esc(m.body)}</p>
                </div>
                ${m.keyPoints.length ? `<h3 class="mt-4 text-sm font-semibold text-foreground">${esc(t("FamilyKeyPoints"))}</h3><ul class="mt-1 list-disc ps-5 text-sm text-foreground space-y-1">${m.keyPoints.map((p) => `<li>${esc(p)}</li>`).join("")}</ul>` : ""}
                <p class="mt-3 text-xs text-muted-foreground-1">${esc(t("FamilyReview"))}</p>`;
            copyButtons(out);
        } catch (err) { error($("family-error"), err.message); }
    });
})();
