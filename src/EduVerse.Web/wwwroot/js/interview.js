// Interview with a historical figure (simulation). The illustrated figure moves its lips while it speaks with its own
// browser voice (or while its words appear, with the voice off). Offline, prepared answers; with the AI, free questions.
// After 3 questions, a comprehension test made of checked facts.
(() => {
    "use strict";
    const { t, fmt, esc, api, lang } = window.Edu;
    const $ = (id) => document.getElementById(id);
    const root = $("iv-root");
    const { figure } = root.dataset;
    const female = root.dataset.female === "true";
    const pitch = Number(root.dataset.pitch) || 1;
    const rate = Number(root.dataset.rate) || 1;
    const avatar = $("iv-avatar");
    const chat = $("iv-chat");
    const field = $("iv-text");
    const history = [];
    const synth = "speechSynthesis" in window ? window.speechSynthesis : null;
    const reduced = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const NEEDED = 3;
    let voiceOn = !!synth;
    let busy = false;

    // ---- voice: its own browser voice (figurevoice.js) ----
    const voice = window.Edu.figureVoice({ female, pitch, rate, index: Number(root.dataset.voiceIndex) || 0, key: figure });
    const showVoice = () => { const n = voice.name(); $("iv-voice-name").textContent = n ? fmt(t("VoiceName"), n) : ""; };
    setTimeout(showVoice, 300);
    window.speechSynthesis?.addEventListener?.("voiceschanged", () => setTimeout(showVoice, 50));

    // ---- lips: three mouth shapes, moved by the words of the voice or by the typing ----
    let mouthTimer = null;
    function setMouth(m) { avatar.dataset.mouth = m; }
    function startTalking() {
        avatar.classList.add("is-talking");
        avatar.classList.remove("is-thinking");
        clearInterval(mouthTimer);
        if (reduced) { setMouth("mid"); return; }
        const shapes = ["open", "mid", "closed", "mid", "open", "closed"];
        let i = 0;
        mouthTimer = setInterval(() => setMouth(shapes[i++ % shapes.length]), 105);
    }
    function stopTalking() {
        clearInterval(mouthTimer);
        avatar.classList.remove("is-talking");
        setMouth("closed");
    }

    function speak(text) {
        if (!voiceOn || !synth) return Promise.resolve();
        return voice.speak(text, { onstart: startTalking, onend: stopTalking });
    }

    async function type(el, text) {
        if (reduced) { el.textContent = text; return; }
        const step = Math.max(1, Math.round(text.length / 120));
        for (let i = 0; i < text.length; i += step) {
            el.textContent = text.slice(0, i + step);
            await new Promise((r) => setTimeout(r, 22));
        }
        el.textContent = text;
    }

    function bubble(who, text = "") {
        const row = document.createElement("div");
        row.className = who === "me" ? "flex justify-end" : "flex items-start gap-3";
        row.innerHTML = who === "me"
            ? `<p class="py-2.5 px-4 rounded-2xl rounded-tr-sm bg-primary text-primary-foreground max-w-[85%]">${esc(text)}</p>`
            : `<span class="size-10 shrink-0 rounded-full overflow-hidden" data-avatar="${esc(figure)}" data-mouth="closed" aria-hidden="true">${window.Edu.avatar(figure)}</span><p class="py-2.5 px-4 rounded-2xl rounded-tl-sm bg-surface text-foreground max-w-[85%]" data-testid="iv-answer">${esc(text)}</p>`;
        chat.appendChild(row);
        row.scrollIntoView({ block: "nearest", behavior: reduced ? "auto" : "smooth" });
        return row.querySelector("p");
    }

    function thinking() {
        avatar.classList.add("is-thinking");
        const p = bubble("figure", "…");
        p.removeAttribute("data-testid");
        p.innerHTML = `<span class="inline-flex gap-1" aria-label="${esc(t("Thinking"))}"><span class="size-2 rounded-full bg-current animate-bounce"></span><span class="size-2 rounded-full bg-current animate-bounce [animation-delay:150ms]"></span><span class="size-2 rounded-full bg-current animate-bounce [animation-delay:300ms]"></span></span>`;
        return p.closest("div");
    }

    async function say(text) {
        const p = bubble("figure");
        const voice = speak(text);
        if (!voiceOn || !synth) startTalking();
        await type(p, text);
        if (!voiceOn || !synth) stopTalking();
        await voice;
    }

    function progress() {
        const n = history.length;
        $("iv-progress").textContent = n < NEEDED ? fmt(t("InterviewProgress"), n, NEEDED) : t("InterviewReady");
        if (n >= NEEDED) $("iv-check").classList.remove("hidden");
    }

    async function ask(question) {
        question = question.trim();
        if (question.length < 2 || busy) return;
        busy = true;
        root.dataset.busy = "true";
        bubble("me", question);
        field.value = "";
        const wait = thinking();
        try {
            const r = await api(`/api/interview/${encodeURIComponent(figure)}`, { json: { question, history } });
            wait.remove();
            avatar.classList.remove("is-thinking");
            history.push({ question, answer: r.answer });
            progress();
            await say(r.answer);
        } catch (e) {
            wait.remove();
            avatar.classList.remove("is-thinking");
            bubble("figure", e.message);
        }
        busy = false;
        root.dataset.busy = "false";
    }

    // ---- controls ----
    const label = $("iv-speak-label");
    if (!synth) { $("iv-speak").hidden = true; }
    $("iv-voice-next").hidden = !synth;
    $("iv-voice-next").addEventListener("click", () => {
        voice.next();
        showVoice();
        voiceOn = true;
        speak($("iv-greeting").content.textContent.trim().split(/[.!?]/)[0] + ".");
    });

    $("iv-speak").addEventListener("click", () => {
        voiceOn = !voiceOn;
        $("iv-speak").setAttribute("aria-pressed", String(voiceOn));
        $("iv-speak").firstChild.textContent = voiceOn ? "🔊 " : "🔇 ";
        label.textContent = voiceOn ? t("VoiceOn") : t("VoiceOff");
        if (!voiceOn) { synth?.cancel(); stopTalking(); }
    });

    window.Edu.voice.dictation($("iv-mic"), field, () => {});
    $("iv-form").addEventListener("submit", (e) => { e.preventDefault(); ask(field.value); });
    $("iv-suggestions").querySelectorAll("[data-question]").forEach((b) => b.addEventListener("click", () => {
        if (busy) return;
        b.disabled = true;
        ask(b.dataset.question);
    }));

    // The voice can only start after a click (browser rule): the interview opens with a button.
    $("iv-start-btn").addEventListener("click", async () => {
        busy = true;
        $("iv-start").remove();
        progress();
        root.dataset.busy = "true";
        await say($("iv-greeting").content.textContent.trim());
        root.dataset.busy = "false";
        busy = false;
        field.focus();
    });
})();

// Tabs: the interview and the dossier of the figure (the conversation stays when switching).
(() => {
    "use strict";
    const tabs = [document.getElementById("iv-tab-chat"), document.getElementById("iv-tab-dossier")];
    if (!tabs[0] || !tabs[1]) return;
    const show = (tab) => tabs.forEach((x) => {
        const on = x === tab;
        x.setAttribute("aria-selected", String(on));
        document.getElementById(x.getAttribute("aria-controls")).hidden = !on;
    });
    tabs.forEach((x, i) => {
        x.addEventListener("click", () => show(x));
        x.addEventListener("keydown", (e) => {
            if (e.key === "ArrowRight" || e.key === "ArrowLeft") { const y = tabs[(i + 1) % 2]; show(y); y.focus(); }
        });
    });
})();
