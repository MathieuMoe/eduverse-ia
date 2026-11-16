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

    // ---- voice: a female or male voice of the language when the browser has one ----
    const FEMALE = ["female", "woman", "zira", "hazel", "susan", "libby", "sonia", "maisie", "aria", "jenny", "samantha", "karen", "moira", "tessa", "fiona", "victoria", "helena", "laura", "elvira", "lucia", "paulina", "monica", "mónica", "marisol", "sabina", "dalia", "hortense", "julie", "denise", "amelie", "amélie", "audrey", "aurelie", "eloise", "brigitte", "joana", "alba", "montserrat", "herena"];
    const MALE = ["male", "david", "mark", "george", "ryan", "guy", "james", "daniel", "alex", "fred", "thomas", "pablo", "alvaro", "álvaro", "jorge", "raul", "raúl", "diego", "enrique", "juan", "carlos", "paul", "henri", "claude", "remy", "rémy", "jerome", "jérôme", "enric", "jordi"];
    const LOCALES = { en: "en-GB", es: "es-ES", fr: "fr-FR", ca: "ca-ES" };
    const code = lang.slice(0, 2);
    let chosen = null;
    let genderMatched = false;

    function pickVoice() {
        if (!synth) return;
        const all = synth.getVoices().filter((v) => v.lang.toLowerCase().startsWith(code));
        const isFemale = (v) => FEMALE.some((n) => v.name.toLowerCase().includes(n));
        const isMale = (v) => !isFemale(v) && MALE.some((n) => v.name.toLowerCase().includes(n));
        const match = all.filter(female ? isFemale : isMale);
        const prefer = (list) => list.find((v) => v.lang.replace("_", "-") === LOCALES[code]) || list[0];
        chosen = prefer(match) || prefer(all) || null;
        genderMatched = match.length > 0;
    }
    if (synth) { pickVoice(); synth.addEventListener?.("voiceschanged", pickVoice); }

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
        return new Promise((resolve) => {
            if (!voiceOn || !synth) { resolve(); return; }
            synth.cancel();
            const u = new SpeechSynthesisUtterance(text);
            u.lang = chosen?.lang || LOCALES[code] || "en-GB";
            if (chosen) u.voice = chosen;
            // When the browser has no voice of the right kind, the pitch makes the difference.
            u.pitch = Math.max(0.1, Math.min(2, genderMatched ? pitch : pitch * (female ? 1.25 : 0.8)));
            u.rate = rate;
            u.onstart = startTalking;
            u.onend = u.onerror = () => { stopTalking(); resolve(); };
            synth.speak(u);
            // Safety net: some browsers never fire onend.
            setTimeout(() => { if (!synth.speaking) { stopTalking(); resolve(); } }, 1500 + text.length * 120);
        });
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
