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
    const flag = root.dataset.flag || "🛂";

    // A small burst of confetti (same look as the quiz's streak milestones)
    function confetti(target) {
        if (matchMedia("(prefers-reduced-motion: reduce)").matches) return;
        const colors = ["#f6c85f", "#6fcf97", "#56ccf2", "#bb6bd9", "#f2994a"];
        const burst = document.createElement("span");
        burst.className = "qz-confetti";
        for (let i = 0; i < 18; i++) {
            const dot = document.createElement("span");
            const angle = (Math.PI * 2 * i) / 18 + Math.random() * 0.4;
            const dist = 50 + Math.random() * 40;
            dot.style.setProperty("--qz-confetti-end", `translate(${Math.round(Math.cos(angle) * dist)}px, ${Math.round(Math.sin(angle) * dist - 20)}px)`);
            dot.style.setProperty("--qz-confetti-rot", `${Math.round(Math.random() * 360)}deg`);
            dot.style.background = colors[i % colors.length];
            burst.appendChild(dot);
        }
        target.style.position ||= "relative";
        target.appendChild(burst);
        setTimeout(() => burst.remove(), 1100);
    }
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
            const result = quiz.querySelector("[data-testid=country-result]");
            if (r.passed) confetti(result);
            if (r.stamp) {
                // The passport stamp slams onto the result
                result.insertAdjacentHTML("afterbegin", `<span class="ct-slam float-end ms-3 size-20 rounded-full border-4 border-[#b8453b] bg-white/70 grid place-items-center text-4xl" aria-hidden="true">${esc(flag)}</span>`);
            }
            quiz.querySelector("[data-testid=country-continue]").addEventListener("click", () => location.reload());
        });
        quiz.querySelector("input")?.focus();
    }

    document.addEventListener("click", (e) => {
        const button = e.target.closest("button[data-check]");
        if (button) check(button);
        // Timeline challenge: move an era up or down
        const move = e.target.closest("button[data-move]");
        if (move) {
            const item = move.closest("li");
            const sibling = +move.dataset.move < 0 ? item.previousElementSibling : item.nextElementSibling;
            if (sibling) {
                if (+move.dataset.move < 0) sibling.before(item); else sibling.after(item);
                move.focus();
            }
        }
    });

    // The time guide: ask a question about an open era (the AI answers from its facts; offline, the facts answer)
    async function ask(box, question) {
        const chat = box.querySelector("[data-guide-chat]");
        const form = box.querySelector("[data-guide-form]");
        chat.insertAdjacentHTML("beforeend", `<li class="le-rise flex justify-end"><p class="max-w-[85%] py-1.5 px-3 rounded-2xl rounded-br-sm bg-primary text-primary-foreground text-sm">${esc(question)}</p></li>`);
        form.querySelector("button").disabled = true;
        try {
            const r = await api(`/api/countries/${encodeURIComponent(country)}/eras/${box.dataset.guide}/ask`, { json: { question } });
            chat.insertAdjacentHTML("beforeend", `<li class="le-rise flex items-start gap-2"><span class="size-8 shrink-0 rounded-full tone-sun grid place-items-center" aria-hidden="true">🦉</span>
                <p class="max-w-[85%] py-1.5 px-3 rounded-2xl rounded-tl-sm bg-layer text-foreground text-sm" data-testid="guide-answer">${esc(r.answer)}</p></li>`);
            if (r.fallback) chat.insertAdjacentHTML("beforeend", `<li class="text-xs text-amber-700 dark:text-amber-300">${esc(fmt(t("Fallback"), r.fallback))}</li>`);
        } catch (err) {
            chat.insertAdjacentHTML("beforeend", `<li class="text-sm text-red-700 dark:text-red-300" role="alert">${esc(err.message)}</li>`);
        }
        form.querySelector("button").disabled = false;
    }

    document.addEventListener("submit", (e) => {
        const form = e.target.closest("[data-guide-form]");
        if (!form) return;
        e.preventDefault();
        const input = form.querySelector("input");
        const question = input.value.trim();
        if (!question) return;
        input.value = "";
        ask(form.closest("[data-guide]"), question);
    });

    document.addEventListener("click", (e) => {
        const hint = e.target.closest("[data-guide-hint]");
        if (hint) ask(hint.closest("[data-guide]"), hint.dataset.guideHint);
    });

    const orderCheck = document.getElementById("order-check");
    orderCheck?.addEventListener("click", async () => {
        const status = document.getElementById("order-status");
        const order = [...document.querySelectorAll("#order-list > li")].map((li) => li.dataset.key);
        orderCheck.disabled = true;
        try {
            const r = await api(`/api/countries/${encodeURIComponent(country)}/order`, { json: { order } });
            status.className = `mt-2 p-3 rounded-xl text-sm text-foreground ${r.won ? "tone-mint" : "tone-sun"}`;
            status.dataset.won = r.won ? "1" : "0";
            status.textContent = r.won ? `🥇 ${t("CountryOrderWon")}${r.gems ? ` · 💎 ${r.gems}` : ""}` : fmt(t("CountryOrderTry"), r.right, r.total);
            if (r.won) confetti(status);
        } catch (err) { status.textContent = err.message; }
        orderCheck.disabled = false;
    });
})();
