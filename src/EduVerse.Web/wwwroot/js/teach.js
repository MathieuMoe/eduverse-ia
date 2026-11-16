// "Explain it to Evo": the student writes (or dictates) an explanation; the server checks the key ideas with a rubric
// and Evo asks about the missing ones. Up to 3 rounds, then the model explanation.
(() => {
    "use strict";
    const { t, fmt, esc, api } = window.Edu;
    const $ = (id) => document.getElementById(id);
    const concept = $("teach-root").dataset.concept;
    const chat = $("teach-chat");
    const field = $("teach-text");
    let sessionId = null;

    window.Edu.voice.dictation($("teach-mic"), field, () => bubble("evo", t("MicError")));

    function bubble(who, text) {
        const row = document.createElement("div");
        row.className = who === "me" ? "flex justify-end" : "flex items-start gap-3";
        row.innerHTML = who === "me"
            ? `<p class="py-2.5 px-4 rounded-2xl rounded-tr-sm bg-primary text-primary-foreground max-w-[85%] whitespace-pre-line">${esc(text)}</p>`
            : `<span class="size-10 shrink-0 rounded-full tone-lavender grid place-items-center text-xl" aria-hidden="true">🦉</span><p class="py-2.5 px-4 rounded-2xl rounded-tl-sm tone-sun text-foreground max-w-[85%]">${esc(text)}</p>`;
        chat.appendChild(row);
        row.scrollIntoView({ block: "nearest", behavior: "smooth" });
    }

    $("teach-form").addEventListener("submit", async (e) => {
        e.preventDefault();
        const text = field.value.trim();
        if (text.length < 2) return;
        const send = $("teach-send");
        send.disabled = true;
        bubble("me", text);
        field.value = "";
        try {
            const r = await api(`/api/teach/${encodeURIComponent(concept)}`, { json: { text, sessionId } });
            sessionId = r.sessionId;
            r.ideas.forEach((idea, i) => {
                const li = document.querySelector(`[data-idea="${idea.key}"]`);
                li.className = `py-1 px-2.5 rounded-full text-xs ${idea.covered ? "tone-mint text-foreground" : r.done ? "tone-pink text-foreground" : "bg-surface text-muted-foreground-1"}`;
                li.textContent = idea.name ? `${idea.covered ? "✅" : "✗"} ${idea.name}` : `❓ ${fmt(t("SecretIdea"), i + 1)}`;
            });
            bubble("evo", [r.reaction, r.question].filter(Boolean).join(" "));
            $("teach-round").textContent = fmt(t("Round"), Math.min(r.round + 1, r.maxRounds), r.maxRounds);
            if (r.done) {
                const found = r.ideas.filter((x) => x.covered).length;
                $("teach-form").classList.add("hidden");
                const end = $("teach-end");
                end.className = "space-y-3";
                end.setAttribute("data-testid", "teach-end");
                end.innerHTML = `<div class="p-4 rounded-2xl ${found === r.ideas.length ? "tone-mint" : "tone-sky"}">
                        <p class="text-lg font-semibold text-foreground">${found === r.ideas.length ? "🎓 " + esc(t("TaughtAll")) : "📘 " + esc(fmt(t("TaughtSome"), found, r.ideas.length))}</p>
                        <p class="text-sm text-foreground/70">+${r.reward.xp} XP</p></div>
                    <div class="p-4 rounded-2xl bg-surface"><p class="text-xs font-semibold uppercase tracking-wide text-muted-foreground-1">${esc(t("ModelExplanation"))}</p><p class="mt-1 text-sm text-foreground">${esc(r.model)}</p></div>
                    <div class="flex flex-wrap gap-2"><a class="py-2.5 px-5 inline-flex items-center text-sm font-semibold rounded-full bg-primary text-primary-foreground" href="/Student/Teach?concept=${encodeURIComponent(concept)}">${esc(t("TeachAgain"))}</a>
                    <a class="py-2.5 px-5 inline-flex items-center text-sm font-semibold rounded-full border border-layer-line bg-layer text-foreground" href="/Student/Teach">${esc(t("OtherConcepts"))}</a></div>`;
                (r.reward.badges || []).forEach((b) => bubble("evo", `${b.icon} ${fmt(t("NewBadge"), b.name)}`));
            }
        } catch (err) {
            bubble("evo", err.message);
        }
        send.disabled = false;
        field.focus();
    });
})();
