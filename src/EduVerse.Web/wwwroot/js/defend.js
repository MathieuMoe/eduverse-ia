// "Defend your answers": the same topics with other numbers, then "why?". The server checks the answer; the explanation is not stored.
(() => {
    "use strict";
    const { t, fmt, esc, api } = window.Edu;
    const root = document.getElementById("df-root");
    const card = document.getElementById("df-card");
    if (!root || !card) return;
    const attempt = root.dataset.attempt;
    const light = "py-2 px-4 inline-flex justify-center items-center gap-x-2 text-sm font-semibold rounded-full border border-layer-line bg-layer text-foreground shadow-2xs hover:bg-layer-hover";
    const primary = "py-2 px-4 inline-flex justify-center items-center gap-x-2 text-sm font-semibold rounded-full bg-primary text-primary-foreground hover:bg-primary-hover";
    let choice = null;

    const fail = (err) => {
        card.innerHTML = `<p class="text-sm text-red-700 dark:text-red-300" role="alert" data-testid="defend-error">${esc(t("Err_" + (err.data?.code || "generic"), err.message))}</p><a class="${light}" href="/Student/History">${esc(t("DefendBack"))}</a>`;
    };

    const show = (r) => {
        if (r.done) {
            card.innerHTML = `<div class="text-center space-y-3" data-testid="defend-summary">
                <p class="text-4xl" aria-hidden="true">${r.verified === r.total ? "🏅" : "🛡️"}</p>
                <p class="text-xl font-semibold text-foreground">${esc(fmt(t("DefendVerified"), r.verified, r.total))}</p>
                <p class="text-sm text-muted-foreground-1">${esc(t(r.verified === r.total ? "DefendAll" : "DefendSome"))}</p>
                <div class="flex justify-center gap-2"><a class="${primary}" href="/Student/History">${esc(t("DefendBack"))}</a><a class="${light}" href="/Student">${esc(t("Dashboard"))}</a></div></div>`;
            return;
        }
        const q = r.question;
        choice = null;
        card.innerHTML = `<div class="flex justify-between text-xs text-muted-foreground-1"><span>${esc(fmt(t("DefendNumber"), q.number, q.total))}</span><span>${esc(q.topic)}</span></div>
            <p class="text-lg font-semibold text-foreground" id="df-prompt" data-testid="defend-prompt">${esc(q.prompt)}</p>
            ${q.code ? `<pre class="p-3 rounded-xl bg-surface text-sm overflow-x-auto"><code>${esc(q.code)}</code></pre>` : ""}
            ${q.visual?.startsWith("dg:") && window.Edu.diagram ? `<div class="flex justify-center">${window.Edu.diagram(q.visual)}</div>` : q.visual ? `<p class="text-3xl text-center" aria-hidden="true">${esc(q.visual)}</p>` : ""}
            <p class="text-sm text-foreground/80">${esc(t("DefendOther"))}</p>
            <div class="grid sm:grid-cols-2 gap-3" role="group" aria-labelledby="df-prompt" data-testid="defend-options">
                ${q.options.map((o, i) => `<button type="button" class="text-start py-3 px-4 rounded-2xl border border-layer-line bg-layer text-foreground hover:bg-layer-hover aria-pressed:ring-2 aria-pressed:ring-primary" data-index="${i}" aria-pressed="false"><span class="font-semibold me-2">${"ABCD"[i]}</span>${esc(o)}</button>`).join("")}
            </div>
            <div><label class="block mb-1.5 text-sm font-medium text-foreground" for="df-why">${esc(t("DefendWhy"))}</label>
                <textarea id="df-why" rows="3" maxlength="400" class="py-2.5 px-3.5 block w-full bg-layer border border-layer-line rounded-xl text-sm text-foreground placeholder:text-muted-foreground" placeholder="${esc(t("DefendWhyPlaceholder"))}" data-testid="defend-why"></textarea></div>
            <div class="flex flex-wrap items-center gap-3"><button type="button" class="${primary}" id="df-send" data-testid="defend-send">${esc(t("DefendSend"))}</button>
                <span class="text-sm text-red-700 dark:text-red-300 hidden" role="alert" id="df-error"></span></div>`;
        card.querySelectorAll("[data-index]").forEach((b) => b.addEventListener("click", () => {
            card.querySelectorAll("[data-index]").forEach((x) => x.setAttribute("aria-pressed", "false"));
            b.setAttribute("aria-pressed", "true");
            choice = +b.dataset.index;
        }));
        document.getElementById("df-send").addEventListener("click", async (e) => {
            const error = document.getElementById("df-error");
            error.classList.add("hidden");
            if (choice === null) { error.textContent = t("DefendPick"); error.classList.remove("hidden"); return; }
            e.currentTarget.disabled = true;
            try {
                const a = await api(`/api/defence/${q.id}/answer`, { json: { choice, why: document.getElementById("df-why").value } });
                card.querySelectorAll("[data-index]").forEach((b) => { b.disabled = true; });
                card.querySelector("[data-testid=defend-send]").remove();
                card.insertAdjacentHTML("beforeend", `<div class="p-4 rounded-2xl ${a.verified ? "tone-mint" : "tone-sky"}" data-testid="defend-feedback" data-verified="${a.verified}">
                    <p class="text-sm font-semibold text-foreground">${a.correct ? "✅ " + esc(t("DefendRight")) : "❌ " + esc(fmt(t("DefendWrong"), a.answer))} · ${a.reasoned ? "💬 " + esc(t("DefendReasoned")) : "💬 " + esc(t("DefendShort"))}</p>
                    <p class="mt-2 text-sm text-foreground">${esc(a.feedback)}</p>
                    <p class="mt-2 text-xs text-foreground/70">${esc(a.explanation)}</p>
                    ${a.reward?.xp ? `<p class="mt-2 text-sm font-semibold text-foreground">⭐ +${a.reward.xp} XP</p>` : ""}</div>
                    <div class="flex justify-end"><button type="button" class="${primary}" id="df-next" data-testid="defend-next">${esc(t("DefendNext"))} →</button></div>`);
                document.getElementById("df-next").addEventListener("click", async () => {
                    try { show(await api(`/api/defence/${attempt}/next`, { method: "GET" })); } catch (err) { fail(err); }
                });
            } catch (err) {
                error.textContent = t("Err_" + (err.data?.code || "generic"), err.message);
                error.classList.remove("hidden");
                e.currentTarget.disabled = false;
            }
        });
    };

    (async () => {
        try { show(await api(`/api/defence/${encodeURIComponent(attempt)}/start`, { method: "POST" })); } catch (err) { fail(err); }
    })();
})();
