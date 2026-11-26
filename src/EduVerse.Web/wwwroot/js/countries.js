// Trips through the history of a country: the timeline is server-rendered; this script runs the 3-question check of
// an era (written and marked on the server) and asks the AI for the timeline of another country.
(() => {
    "use strict";
    const { t, fmt, esc, api } = window.Edu;

    // A new country written by the AI (needs a real engine: offline the server answers needs_ai)
    const form = document.getElementById("country-new");
    if (form) {
        const status = document.getElementById("country-new-status");
        form.addEventListener("submit", async (e) => {
            e.preventDefault();
            const go = form.querySelector("button");
            go.disabled = true;
            status.className = "mt-2 text-sm text-muted-foreground-1";
            status.textContent = t("CountryWriting");
            try {
                const r = await api("/api/countries", { json: { name: document.getElementById("country-name").value } });
                location.href = `/Student/Countries?country=${encodeURIComponent(r.key)}`;
            } catch (err) {
                status.className = "mt-2 p-3 rounded-xl text-sm tone-sun text-foreground";
                status.textContent = err?.data?.code === "needs_ai" ? t("CountryNeedsAi") : err.message;
                go.disabled = false;
            }
        });
    }

    const root = document.getElementById("country-root");
    if (!root) return;
    const country = root.dataset.country;

    async function check(button) {
        const box = button.closest("[data-check-box]");
        button.disabled = true;
        let c;
        try {
            c = await api(`/api/countries/${encodeURIComponent(country)}/eras/${button.dataset.check}/check`, { method: "POST" });
        } catch (err) {
            button.disabled = false;
            box.insertAdjacentHTML("beforeend", `<p class="mt-2 text-sm text-red-700 dark:text-red-300" role="alert">${esc(err.message)}</p>`);
            return;
        }
        box.innerHTML = `<form class="space-y-4" data-testid="country-quiz">${c.questions.map((q, i) => `
            <fieldset class="le-rise" data-q="${i}"><legend class="text-sm font-semibold text-foreground">${i + 1}. ${esc(q.prompt)}</legend>
            <div class="mt-2 grid gap-2">${q.options.map((o, j) => `
                <label class="flex items-start gap-2 p-2.5 rounded-xl border border-layer-line bg-layer text-sm text-foreground cursor-pointer hover:bg-layer-hover has-checked:border-primary has-checked:ring-2 has-checked:ring-primary/20" data-option="${j}">
                    <input type="radio" class="mt-0.5 accent-primary" name="q${i}" value="${j}" required data-testid="country-option" /><span>${esc(o)}</span></label>`).join("")}</div></fieldset>`).join("")}
            <button type="submit" class="${button.className}" data-testid="country-submit">${esc(t("CountrySubmit"))}</button></form>`;
        const quiz = box.querySelector("form");
        quiz.addEventListener("submit", async (e) => {
            e.preventDefault();
            const answers = c.questions.map((_, i) => +(quiz.querySelector(`input[name=q${i}]:checked`)?.value ?? -1));
            quiz.querySelector("button[type=submit]").disabled = true;
            let r;
            try { r = await api(`/api/countries/checks/${encodeURIComponent(c.id)}`, { json: { answers } }); }
            catch (err) { quiz.insertAdjacentHTML("beforeend", `<p class="text-sm text-red-700 dark:text-red-300" role="alert">${esc(err.message)}</p>`); return; }
            r.results.forEach((res, i) => {
                const set = quiz.querySelector(`[data-q="${i}"]`);
                set.querySelectorAll("input").forEach((input) => { input.disabled = true; });
                set.querySelector(`[data-option="${res.correct}"]`).classList.add("tone-mint");
                if (!res.right) set.querySelector(`[data-option="${answers[i]}"]`)?.classList.add("tone-peach");
                set.insertAdjacentHTML("beforeend", `<p class="mt-1.5 text-xs text-muted-foreground-1">${res.right ? "✅" : "❌"} ${esc(res.explanation)}</p>`);
            });
            const prize = r.reward.xp ? ` · +${r.reward.xp} XP · 💎 ${r.reward.gems}` : "";
            quiz.querySelector("button[type=submit]").remove();
            quiz.insertAdjacentHTML("beforeend", `<div class="le-rise p-4 rounded-2xl ${r.passed ? "tone-mint" : "tone-sun"}" role="status" data-testid="country-result" data-passed="${r.passed ? "1" : "0"}">
                <p class="text-base font-semibold text-foreground">${r.passed ? "🎉" : "💪"} ${esc(fmt(t(r.passed ? "CountryPassed" : "CountryFailed"), r.score, r.results.length))}${esc(prize)}</p>
                ${r.stamp ? `<p class="mt-1 text-sm font-semibold text-foreground" data-testid="country-stamp">🛂 ${esc(t("CountryStamp"))}</p>` : ""}
                ${r.reward.badges.map((b) => `<p class="mt-1 text-sm text-foreground">${esc(b.icon)} ${esc(b.name)}</p>`).join("")}
                <button type="button" class="mt-3 ${button.className}" data-testid="country-continue">${esc(t(r.passed ? "CountryContinue" : "CountryRetry"))}</button></div>`);
            quiz.querySelector("[data-testid=country-continue]").addEventListener("click", () => location.reload());
        });
        quiz.querySelector("input")?.focus();
    }

    document.addEventListener("click", (e) => {
        const button = e.target.closest("button[data-check]");
        if (button) check(button);
    });
})();
