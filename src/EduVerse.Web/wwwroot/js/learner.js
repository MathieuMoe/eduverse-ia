// "How I learn": the student corrects what the tutor noticed ("that is not me"), adds notes about themselves and can ask the tutor
// to write the card as a short message. Every change reloads the page, which is computed on the server.
(() => {
    "use strict";
    const { t, api } = window.Edu;
    const $ = (id) => document.getElementById(id);

    document.querySelectorAll("[data-insight]").forEach((b) => b.addEventListener("click", async () => {
        b.disabled = true;
        try {
            await api(`/api/learner/insight/${encodeURIComponent(b.dataset.insight)}`, { json: { hidden: b.dataset.hide === "true" } });
            location.reload();
        } catch { b.disabled = false; }
    }));

    const interests = document.querySelectorAll("[data-interest]");
    interests.forEach((b) => b.addEventListener("click", async () => {
        const picked = [...interests].filter((x) => x.getAttribute("aria-pressed") === "true").map((x) => x.dataset.interest);
        const key = b.dataset.interest;
        const next = picked.includes(key) ? picked.filter((k) => k !== key) : [...picked, key];
        if (next.length > 3) { $("lr-interests-state").textContent = t("InterestsMax"); return; }
        try {
            await api("/api/learner/interests", { json: { keys: next } });
            location.reload();
        } catch { $("lr-interests-state").textContent = t("InterestsError"); }
    }));

    document.querySelectorAll("[data-note-delete]").forEach((b) => b.addEventListener("click", async () => {
        b.disabled = true;
        try {
            await api(`/api/learner/notes/${encodeURIComponent(b.dataset.noteDelete)}`, { method: "DELETE" });
            location.reload();
        } catch { b.disabled = false; }
    }));

    $("lr-note-form")?.addEventListener("submit", async (e) => {
        e.preventDefault();
        const error = $("lr-note-error");
        error.classList.add("hidden");
        try {
            await api("/api/learner/notes", { json: { text: $("lr-note").value } });
            location.reload();
        } catch (err) {
            error.textContent = t("Err_" + (err.data?.code || "generic"), err.message);
            error.classList.remove("hidden");
        }
    });

    $("lr-summary-ai")?.addEventListener("click", async () => {
        const b = $("lr-summary-ai");
        b.disabled = true;
        try {
            const r = await api("/api/learner/summary", { method: "POST" });
            $("lr-summary").textContent = r.summary;
            $("lr-summary-engine").textContent = r.fallback ? t("AiFallbackShort", "") : "";
            $("lr-summary").dataset.engine = r.engine;
        } catch (err) {
            $("lr-summary-engine").textContent = err.message;
        }
        b.disabled = false;
    });
})();
