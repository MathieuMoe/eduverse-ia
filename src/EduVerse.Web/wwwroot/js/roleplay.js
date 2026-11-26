// Roleplays (phase 6 of the "more like Duolingo" pass): talk to a character to reach missions. The server ticks the
// missions in code on the student's words; the AI (or a script, offline) plays the character, read aloud in its language.
(() => {
    "use strict";
    const { t, fmt, esc, api } = window.Edu;
    const root = document.getElementById("rp-root");
    if (!root) return;
    const $ = (id) => document.getElementById(id);
    const voices = { en: "en-GB", es: "es-ES", fr: "fr-FR", ca: "ca-ES" };
    const Recognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    let state = null;
    let speak = true;
    let shown = 0;

    function say(text) {
        if (!speak || !("speechSynthesis" in window) || !state) return;
        try {
            speechSynthesis.cancel();
            const u = new SpeechSynthesisUtterance(text);
            u.lang = voices[state.speaks] || "en-GB";
            speechSynthesis.speak(u);
        } catch { /* no voice: the text is on screen */ }
    }

    function render(rp) {
        state = rp;
        const chat = $("rp-chat");
        rp.history.slice(shown).forEach((line) => {
            chat.insertAdjacentHTML("beforeend", line.student
                ? `<li class="le-rise flex justify-end"><p class="max-w-[85%] py-2 px-3.5 rounded-2xl rounded-br-sm bg-primary text-primary-foreground text-sm" data-testid="roleplay-student">${esc(line.text)}</p></li>`
                : `<li class="le-rise flex items-start gap-2"><span class="size-9 shrink-0 rounded-full tone-sky grid place-items-center text-lg" aria-hidden="true">${esc(rp.emoji)}</span>
                    <p class="max-w-[85%] py-2 px-3.5 rounded-2xl rounded-tl-sm bg-surface text-foreground text-sm" data-testid="roleplay-character" lang="${esc(rp.speaks)}">${esc(line.text)}</p></li>`);
            if (!line.student) say(line.text);
        });
        shown = rp.history.length;
        chat.scrollTop = chat.scrollHeight;
        $("rp-goals").innerHTML = rp.goals.map((g) => `<li class="flex items-start gap-2 text-sm ${g.met ? "text-foreground" : "text-muted-foreground-1"}" data-met="${g.met ? "1" : "0"}" data-testid="roleplay-goal">
            <span aria-hidden="true">${g.met ? "✅" : "⬜"}</span><span>${esc(g.label)}</span></li>`).join("");
        $("rp-turns").textContent = fmt(t("RoleplayTurns"), rp.turnsLeft);
        if (rp.finished) {
            $("rp-form").classList.add("hidden");
            $("rp-chat").insertAdjacentHTML("beforeend", `<li class="le-rise p-4 rounded-2xl ${rp.completed ? "tone-mint" : "tone-sky"} text-center" data-testid="roleplay-end" data-completed="${rp.completed ? "1" : "0"}">
                <p class="text-3xl" aria-hidden="true">${rp.completed ? "🏆" : "🌙"}</p>
                <p class="mt-1 font-semibold text-foreground">${esc(rp.completed ? fmt(t("RoleplayDone"), rp.reward?.xp || 0, rp.reward?.gems || 0) : t("RoleplayOut"))}</p>
                <div class="mt-3 flex flex-wrap justify-center gap-2"><a class="py-2 px-4 rounded-full text-sm font-semibold bg-primary text-primary-foreground" href="/Student/Roleplay?scenario=${encodeURIComponent(rp.scenario)}">${esc(t("RoleplayAgain"))}</a>
                <a class="py-2 px-4 rounded-full text-sm font-semibold border border-layer-line bg-layer text-foreground" href="/Student/Roleplay">${esc(t("RoleplayOthers"))}</a></div></li>`);
        }
    }

    $("rp-speak").addEventListener("click", (e) => {
        speak = !speak;
        e.currentTarget.setAttribute("aria-pressed", String(speak));
        e.currentTarget.textContent = speak ? "🔊" : "🔇";
        if (!speak && "speechSynthesis" in window) speechSynthesis.cancel();
    });

    if (Recognition) {
        const mic = $("rp-mic");
        mic.classList.remove("hidden");
        mic.addEventListener("click", () => {
            const r = new Recognition();
            r.lang = voices[state?.speaks] || "en-GB";
            r.interimResults = false;
            r.onresult = (e) => { $("rp-text").value = (($("rp-text").value + " ") + e.results[0][0].transcript).trim(); };
            r.onerror = () => { $("rp-status").textContent = t("RoleplayMicError"); };
            try { r.start(); $("rp-status").textContent = t("RoleplayListening"); } catch { /* already listening */ }
        });
    }

    $("rp-form").addEventListener("submit", async (e) => {
        e.preventDefault();
        const text = $("rp-text").value.trim();
        if (!text || !state) return;
        const send = e.currentTarget.querySelector("[type=submit]");
        send.disabled = true;
        $("rp-status").textContent = t("RoleplayThinking");
        try {
            const r = await api(`/api/roleplay/${encodeURIComponent(state.id)}/say`, { json: { text } });
            $("rp-text").value = "";
            $("rp-status").textContent = r.fallback ? fmt(t("Fallback"), r.fallback) : "";
            render(r.roleplay);
        } catch (err) { $("rp-status").textContent = err.message; }
        send.disabled = false;
    });

    (async () => {
        try { render(await api("/api/roleplay/start", { json: { scenario: root.dataset.scenario } })); }
        catch (err) { $("rp-status").textContent = err.message; }
    })();
})();
