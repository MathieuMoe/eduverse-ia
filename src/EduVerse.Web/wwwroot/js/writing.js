// "Write it, say it": the student picks a task, writes (or dictates) an answer and gets it back measured by the code and, with an AI,
// judged on four criteria. Sending again shows the improvement against the version before. The text is never stored.
(() => {
    "use strict";
    const { t, fmt, esc, api } = window.Edu;
    const $ = (id) => document.getElementById(id);
    const card = $("wr-card");
    if (!card) return;
    const text = $("wr-text"), count = $("wr-count"), error = $("wr-error"), result = $("wr-result");
    let task = null;

    const words = () => (text.value.match(/[\p{L}\p{N}]+(?:['’-][\p{L}\p{N}]+)*/gu) || []).length;
    const showCount = () => { count.textContent = task ? fmt(t("WritingCount"), words(), task.min, task.max) : ""; };

    function pick(button) {
        document.querySelectorAll("[data-task]").forEach((b) => b.setAttribute("aria-pressed", String(b === button)));
        task = { key: button.dataset.task, spoken: button.dataset.spoken === "1", min: +button.dataset.min, max: +button.dataset.max };
        $("wr-title").textContent = button.dataset.title;
        $("wr-prompt").textContent = button.dataset.prompt;
        card.classList.remove("hidden");
        result.innerHTML = "";
        error.classList.add("hidden");
        text.value = "";
        showCount();
        text.focus();
    }

    function show(r) {
        const before = r.before ? `<p class="p-2.5 rounded-xl tone-lavender text-sm text-foreground" data-testid="writing-before">${esc(fmt(t(r.passed > r.before.passed ? "WritingBetter" : r.passed < r.before.passed ? "WritingWorse" : "WritingSame"), r.before.passed, r.before.total, r.passed, r.total))}</p>` : "";
        const checks = r.checks.map((c) => `<li class="flex gap-2 text-sm text-foreground" data-testid="writing-check" data-key="${esc(c.key)}" data-ok="${c.ok}"><span aria-hidden="true">${c.ok ? "✅" : "✏️"}</span><span>${esc(c.text)}</span></li>`).join("");
        const rubric = r.rubric.length
            ? `<div data-testid="writing-rubric"><p class="text-sm font-semibold text-foreground">🦉 ${esc(fmt(t("WritingRubric"), r.rubricTotal, r.rubric.length * 3))}</p><ul class="mt-2 space-y-2">${r.rubric.map((x) => `<li class="p-3 rounded-2xl bg-surface/60 text-sm text-foreground">
                    <p class="flex justify-between gap-3 font-medium"><span>${esc(x.name)}</span><span aria-label="${x.score}/3">${"●".repeat(x.score)}${"○".repeat(3 - x.score)}</span></p>
                    ${x.quote ? `<p class="mt-1 italic text-foreground/80">“${esc(x.quote)}”</p>` : ""}<p class="mt-1">${esc(x.advice)}</p></li>`).join("")}</ul>
                ${r.next ? `<p class="mt-3 p-3 rounded-2xl tone-sun text-sm text-foreground">👉 ${esc(r.next)}</p>` : ""}</div>`
            : `<p class="text-xs text-muted-foreground-1" data-testid="writing-offline">${esc(t("WritingOffline"))}</p>`;
        result.innerHTML = `${before}
            <p class="text-sm font-semibold text-foreground" data-testid="writing-score" data-passed="${r.passed}" data-total="${r.total}">${esc(fmt(t("WritingScore"), r.passed, r.total))}${r.reward?.xp ? ` ⭐ +${r.reward.xp} XP` : ""}</p>
            <ul class="space-y-1.5">${checks}</ul>${rubric}
            <p class="text-xs text-muted-foreground-1">${esc(t("WritingAgain"))}</p>`;
    }

    document.querySelectorAll("[data-task]").forEach((b) => b.addEventListener("click", () => pick(b)));
    text.addEventListener("input", showCount);
    if (window.Edu.voice) window.Edu.voice.dictation($("wr-mic"), text, () => {}); else $("wr-mic").hidden = true;
    $("wr-send").addEventListener("click", async (e) => {
        error.classList.add("hidden");
        if (!task || !text.value.trim()) { error.textContent = t("Err_writing_empty"); error.classList.remove("hidden"); return; }
        const button = e.currentTarget; // currentTarget is gone after the first await
        button.disabled = true;
        try { show(await api("/api/writing/check", { json: { task: task.key, text: text.value } })); }
        catch (err) { error.textContent = t("Err_" + (err.data?.code || "generic"), err.message); error.classList.remove("hidden"); }
        button.disabled = false;
    });
    const selected = $("wr-tasks").dataset.selected;
    if (selected) document.querySelector(`[data-task="${CSS.escape(selected)}"]`)?.click();
})();
