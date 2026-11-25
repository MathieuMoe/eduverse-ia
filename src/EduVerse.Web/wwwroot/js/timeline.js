// Timeline mini-game: put the figures the student has completed in birth order. Entirely client-side — the
// years are already shown elsewhere in the app (the dossier, the figure's own greeting), so this is an honour-system
// check like the curiosity meter, not a graded quiz with a hidden answer key.
(() => {
    "use strict";
    const { t, fmt, esc } = window.Edu;
    const $ = (id) => document.getElementById(id);
    const pool = $("tl-pool");
    const slots = $("tl-slots");
    const checkBtn = $("tl-check");
    const result = $("tl-result");
    const score = $("tl-score");
    const figures = pool.querySelectorAll("button[data-years]");
    const order = [];

    pool.querySelectorAll("button[data-years]").forEach((b) => b.addEventListener("click", () => {
        b.disabled = true;
        order.push(b);
        const li = document.createElement("li");
        li.className = "quiz-option py-2 px-3.5 rounded-xl border border-layer-line bg-layer text-sm font-medium text-foreground flex items-center gap-2";
        li.dataset.testid = "tl-slot";
        li.innerHTML = `<span aria-hidden="true">${esc(b.dataset.emoji)}</span> <span class="tl-slot-name">${esc(b.dataset.name)}</span>`;
        slots.appendChild(li);
        if (order.length === figures.length) {
            checkBtn.classList.remove("hidden");
        }
    }));

    checkBtn.addEventListener("click", () => {
        const correctOrder = [...order].sort((a, b) => Number(a.dataset.sortYear) - Number(b.dataset.sortYear));
        let right = 0;
        const items = slots.querySelectorAll("li");
        order.forEach((b, i) => {
            const ok = correctOrder[i] === b;
            items[i].dataset.state = ok ? "right" : "wrong";
            items[i].querySelector(".tl-slot-name").insertAdjacentHTML("afterend", ` <span class="opacity-70">(${esc(b.dataset.years)})</span>`);
            if (ok) right++;
        });
        checkBtn.classList.add("hidden");
        result.classList.remove("hidden");
        score.textContent = right === order.length ? t("TimelinePerfect") : fmt(t("TimelinePartial"), right, order.length);
        result.scrollIntoView({ block: "nearest", behavior: window.matchMedia("(prefers-reduced-motion: reduce)").matches ? "auto" : "smooth" });
    });
})();
