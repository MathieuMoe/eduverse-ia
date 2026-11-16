// Plan towards an exam: saves, changes or removes the goal. The plan itself is built on the server every time the page loads.
(() => {
    "use strict";
    const { t, api } = window.Edu;
    const $ = (id) => document.getElementById(id);
    const form = $("pl-form");
    if (!form) return;
    const levels = JSON.parse(form.dataset.levels || "{}");

    const fillLevels = () => {
        const select = $("pl-level");
        const keep = select.value;
        select.innerHTML = "";
        (levels[$("pl-area").value] || []).forEach((l) => {
            const o = document.createElement("option");
            o.value = l.value;
            o.textContent = l.name;
            select.appendChild(o);
        });
        if ([...select.options].some((o) => o.value === keep)) select.value = keep;
    };
    $("pl-area").addEventListener("change", fillLevels);

    $("pl-change")?.addEventListener("click", () => { $("pl-form-card").classList.remove("hidden"); $("pl-date").focus(); });
    $("pl-remove")?.addEventListener("click", async (e) => {
        e.currentTarget.disabled = true;
        try { await api("/api/plan", { method: "DELETE" }); location.reload(); } catch { e.currentTarget.disabled = false; }
    });

    form.addEventListener("submit", async (e) => {
        e.preventDefault();
        const error = $("pl-error");
        error.classList.add("hidden");
        try {
            await api("/api/plan", { json: { area: $("pl-area").value, level: $("pl-level").value, title: $("pl-name").value, examOn: $("pl-date").value, minutes: +$("pl-minutes").value } });
            location.reload();
        } catch (err) {
            error.textContent = t("Err_" + (err.data?.code || "generic"), err.message);
            error.classList.remove("hidden");
        }
    });
})();
