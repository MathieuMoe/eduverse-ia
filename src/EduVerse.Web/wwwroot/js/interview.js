// Interview with a historical figure (simulation). The illustrated figure moves its lips while it speaks with its own
// browser voice (or while its words appear, with the voice off). Offline, prepared answers; with the AI, free questions.
// After 3 questions, a comprehension test made of checked facts.
(() => {
    "use strict";
    const { t, fmt, esc, api, lang } = window.Edu;
    const $ = (id) => document.getElementById(id);
    const root = $("iv-root");
    const { figure, emoji, years, name, area } = root.dataset;
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

    // testid defaults to "iv-answer" for a figure bubble (a genuine interview answer); pass null for banter that
    // isn't one (the "thinking" dots, the riddle's own lines) so it's never counted as one, even for the instant
    // between the bubble appearing and its text finishing — removing the attribute only after the fact would race.
    function bubble(who, text = "", testid = "iv-answer") {
        const row = document.createElement("div");
        row.className = who === "me" ? "flex justify-end" : "flex items-start gap-3";
        row.innerHTML = who === "me"
            ? `<p class="py-2.5 px-4 rounded-2xl rounded-tr-sm bg-primary text-primary-foreground max-w-[85%]">${esc(text)}</p>`
            : `<span class="size-10 shrink-0 rounded-full overflow-hidden" data-avatar="${esc(figure)}" data-mouth="closed" aria-hidden="true">${window.Edu.avatar(figure)}</span><p class="py-2.5 px-4 rounded-2xl rounded-tl-sm bg-surface text-foreground max-w-[85%]"${testid ? ` data-testid="${esc(testid)}"` : ""}>${esc(text)}</p>`;
        chat.appendChild(row);
        row.scrollIntoView({ block: "nearest", behavior: reduced ? "auto" : "smooth" });
        return row.querySelector("p");
    }

    function thinking() {
        avatar.classList.add("is-thinking");
        const p = bubble("figure", "…", null);
        p.innerHTML = `<span class="inline-flex gap-1" aria-label="${esc(t("Thinking"))}"><span class="size-2 rounded-full bg-current animate-bounce"></span><span class="size-2 rounded-full bg-current animate-bounce [animation-delay:150ms]"></span><span class="size-2 rounded-full bg-current animate-bounce [animation-delay:300ms]"></span></span>`;
        return p.closest("div");
    }

    async function say(text, testid = "iv-answer") {
        const p = bubble("figure", "", testid);
        const voice = speak(text);
        if (!voiceOn || !synth) startTalking();
        await type(p, text);
        if (!voiceOn || !synth) stopTalking();
        await voice;
        return p;
    }

    // Curiosity meter: how many of this figure's own suggested questions have been asked (discovered),
    // out of however many it offers — separate from NEEDED, which only gates the comprehension check.
    const totalSuggestions = $("iv-suggestions").querySelectorAll("[data-question]").length;
    const askedSuggestions = new Set();

    // Progressive life-path: a companion to the % bar — one milestone dot per suggested question, keyed by its
    // own question text (the same key askedSuggestions already uses), lighting up as each is discovered.
    const pathEl = $("iv-path");
    const pathDots = new Map();
    if (pathEl && totalSuggestions) {
        $("iv-suggestions").querySelectorAll("[data-question]").forEach((b, i) => {
            const dot = document.createElement("span");
            dot.className = "iv-path-dot";
            dot.setAttribute("aria-hidden", "true");
            dot.textContent = String(i + 1);
            pathEl.appendChild(dot);
            pathDots.set(b.dataset.question, dot);
        });
    } else if (pathEl) {
        pathEl.closest('[data-testid="iv-path-wrap"]')?.classList.add("hidden");
    }

    function progress() {
        const n = history.length;
        $("iv-progress").textContent = n < NEEDED ? fmt(t("InterviewProgress"), n, NEEDED) : t("InterviewReady");
        if (n >= NEEDED) $("iv-check").classList.remove("hidden");
        const discovered = Math.min(askedSuggestions.size, totalSuggestions);
        const pct = totalSuggestions ? Math.round((discovered / totalSuggestions) * 100) : 0;
        if (totalSuggestions) {
            $("iv-meter").setAttribute("aria-valuenow", String(pct));
            $("iv-meter-fill").style.width = pct + "%";
            $("iv-meter-label").textContent = fmt(t("InterviewDiscovered"), discovered, totalSuggestions);
        }
        askedSuggestions.forEach((q) => {
            const dot = pathDots.get(q);
            if (dot && !dot.classList.contains("is-lit")) {
                dot.classList.add("is-lit");
                dot.textContent = "✓";
            }
        });
    }

    function flashMood(mood, ms = 750) {
        avatar.classList.add(mood);
        setTimeout(() => avatar.classList.remove(mood), ms);
    }

    // The collectible "postcard" shown once the riddle is answered, right after everything about the figure is discovered.
    // It also renders to an offscreen <canvas> (no library) so the visitor can keep a real image file of it.
    function postcard(fact) {
        const title = fmt(t("InterviewPostcardTitle"), name);
        const row = document.createElement("div");
        row.className = "flex justify-center";
        row.innerHTML = `<div class="max-w-sm w-full rounded-2xl tone-sun p-5 text-center space-y-1.5" data-testid="iv-postcard">
            <p class="text-4xl" aria-hidden="true">${esc(emoji)}</p>
            <p class="font-semibold text-foreground">${esc(title)}</p>
            <p class="text-xs text-muted-foreground-1">${esc(years)}</p>
            <p class="text-sm text-foreground">${esc(fact)}</p>
            <button type="button" class="py-1.5 px-3 rounded-full border border-layer-line text-xs text-foreground hover:bg-layer-hover mt-1" data-testid="iv-postcard-download">⬇ ${esc(t("InterviewPostcardDownload"))}</button>
        </div>`;
        chat.appendChild(row);
        row.scrollIntoView({ block: "nearest", behavior: reduced ? "auto" : "smooth" });
        row.querySelector('[data-testid="iv-postcard-download"]').addEventListener("click", () => downloadPostcard(title, fact));
    }

    // Wraps text onto several lines inside maxWidth, drawing each with fillText — canvas has no built-in wrapping.
    function wrapText(ctx, text, x, y, maxWidth, lineHeight) {
        const words = text.split(" ");
        let line = "";
        for (const word of words) {
            const test = line ? line + " " + word : word;
            if (ctx.measureText(test).width > maxWidth && line) {
                ctx.fillText(line, x, y);
                line = word;
                y += lineHeight;
            } else {
                line = test;
            }
        }
        ctx.fillText(line, x, y);
        return y + lineHeight;
    }

    function downloadPostcard(title, fact) {
        const W = 800, H = 500;
        const canvas = document.createElement("canvas");
        canvas.width = W;
        canvas.height = H;
        const ctx = canvas.getContext("2d");

        const bg = ctx.createLinearGradient(0, 0, W, H);
        bg.addColorStop(0, "#fff6d6");
        bg.addColorStop(1, "#fbe7a4");
        ctx.fillStyle = bg;
        ctx.fillRect(0, 0, W, H);

        ctx.strokeStyle = "rgba(43, 35, 48, 0.25)";
        ctx.lineWidth = 2;
        ctx.setLineDash([10, 8]);
        ctx.strokeRect(20, 20, W - 40, H - 40);
        ctx.setLineDash([]);

        ctx.textAlign = "center";
        ctx.fillStyle = "#2b2330";
        ctx.font = "88px system-ui, sans-serif";
        ctx.fillText(emoji, W / 2, 150);

        ctx.font = "bold 34px system-ui, sans-serif";
        ctx.fillText(title, W / 2, 220);

        ctx.font = "18px system-ui, sans-serif";
        ctx.fillStyle = "#7e808c";
        ctx.fillText(years, W / 2, 255);

        ctx.font = "22px system-ui, sans-serif";
        ctx.fillStyle = "#2b2330";
        wrapText(ctx, fact, W / 2, 320, W - 140, 32);

        ctx.font = "16px system-ui, sans-serif";
        ctx.fillStyle = "#a3a2ae";
        ctx.fillText("EduVerse AI", W / 2, H - 35);

        const a = document.createElement("a");
        a.href = canvas.toDataURL("image/png");
        a.download = `eduverse-${figure}.png`;
        a.click();
    }

    // Once curiosity hits 100%, the figure poses one of its checked facts back as a playful riddle; the answer key
    // stays on the server (/riddle only shuffles options, /riddle/check verifies), then a postcard caps the moment.
    let riddleDone = false;
    async function riddle() {
        if (riddleDone) return;
        riddleDone = true;
        try {
            const r = await api(`/api/interview/${encodeURIComponent(figure)}/riddle`);
            // Riddle banter, not interview Q&A answers, so it's never tagged data-testid=iv-answer (counted elsewhere).
            await say(t("InterviewRiddleIntro"), null);
            bubble("figure", r.question, null);
            const wrap = document.createElement("div");
            wrap.className = "flex flex-wrap gap-2";
            r.options.forEach((opt) => {
                const b = document.createElement("button");
                b.type = "button";
                b.className = "py-1.5 px-3 rounded-full border border-layer-line text-sm text-foreground hover:bg-layer-hover disabled:opacity-40";
                b.textContent = "💬 " + opt;
                b.dataset.testid = "iv-riddle-option";
                b.addEventListener("click", async () => {
                    wrap.querySelectorAll("button").forEach((x) => { x.disabled = true; });
                    const res = await api(`/api/interview/${encodeURIComponent(figure)}/riddle/check`, { json: { checkIndex: r.checkIndex, answer: opt } });
                    await say(res.correct ? t("InterviewRiddleCorrect") : fmt(t("InterviewRiddleWrong"), res.answer), null);
                    flashMood(res.correct ? "is-happy" : "is-surprised");
                    postcard(res.answer);
                    myths();
                });
                wrap.appendChild(b);
            });
            chat.appendChild(wrap);
            wrap.scrollIntoView({ block: "nearest", behavior: reduced ? "auto" : "smooth" });
        } catch {
            riddleDone = false; // a bonus feature: let a later trigger retry rather than losing it to one network hiccup
        }
    }

    // One last bonus after the postcard: a couple of true-or-false claims about the figure (some real surprises,
    // some common misconceptions), answered one at a time; the answer key stays server-side until /myths/check.
    let mythsDone = false;
    async function myths() {
        if (mythsDone) return;
        mythsDone = true;
        try {
            const r = await api(`/api/interview/${encodeURIComponent(figure)}/myths`);
            if (!r.items?.length) return;
            await say(t("InterviewMythIntro"), null);
            for (const item of r.items) {
                bubble("figure", item.statement, null);
                const wrap = document.createElement("div");
                wrap.className = "flex flex-wrap gap-2";
                [{ label: t("InterviewMythTrue"), value: true }, { label: t("InterviewMythFalse"), value: false }].forEach(({ label, value }) => {
                    const b = document.createElement("button");
                    b.type = "button";
                    b.className = "py-1.5 px-3 rounded-full border border-layer-line text-sm text-foreground hover:bg-layer-hover disabled:opacity-40";
                    b.textContent = (value ? "✅ " : "❌ ") + label;
                    b.dataset.testid = "iv-myth-option";
                    wrap.appendChild(b);
                });
                chat.appendChild(wrap);
                wrap.scrollIntoView({ block: "nearest", behavior: reduced ? "auto" : "smooth" });
                const choice = await new Promise((resolve) => {
                    wrap.querySelectorAll("button").forEach((b, i) => b.addEventListener("click", () => {
                        wrap.querySelectorAll("button").forEach((x) => { x.disabled = true; });
                        resolve(i === 0);
                    }));
                });
                const res = await api(`/api/interview/${encodeURIComponent(figure)}/myths/check`, { json: { index: item.index, answer: choice } });
                flashMood(res.correct ? "is-happy" : "is-surprised");
                await say((res.correct ? t("InterviewMythCorrect") : t("InterviewMythWrong")) + " " + res.explanation, null);
            }
            await say(t("InterviewMythDone"), null);
        } catch {
            mythsDone = false; // let a later trigger retry rather than losing the bonus round to one network hiccup
        }
    }

    async function ask(question, suggestionKey = null) {
        question = question.trim();
        if (question.length < 2 || busy) return;
        busy = true;
        root.dataset.busy = "true";
        bubble("me", question);
        field.value = "";
        const wasComplete = totalSuggestions > 0 && askedSuggestions.size >= totalSuggestions;
        if (suggestionKey) askedSuggestions.add(suggestionKey);
        const wait = thinking();
        // A minimum visible time for the "thinking" bubble: the offline answer resolves almost instantly,
        // which made the dots flash too fast to notice.
        const minWait = new Promise((r) => setTimeout(r, 500));
        try {
            const [r] = await Promise.all([api(`/api/interview/${encodeURIComponent(figure)}`, { json: { question, history } }), minWait]);
            wait.remove();
            avatar.classList.remove("is-thinking");
            history.push({ question, answer: r.answer });
            progress();
            await say(r.answer);
            const nowComplete = totalSuggestions > 0 && askedSuggestions.size >= totalSuggestions;
            flashMood(nowComplete && !wasComplete ? "is-surprised" : "is-happy");
            if (nowComplete && !wasComplete) riddle();
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
        ask(b.dataset.question, b.dataset.question);
    }));

    // A short themed flourish (era icon) before the greeting, purely decorative — skipped outright under
    // prefers-reduced-motion rather than just stripped of motion, since it has no informational content.
    const introIcons = {
        mathematics: "📐", physics: "🍎", chemistry: "🧪", biology: "🌱", language: "🖋️",
        history: "📜", geography: "🧭", economics: "📈", programming: "💻",
    };
    async function playIntro() {
        const icon = introIcons[area];
        if (!icon || reduced) return;
        $("iv-start-hint").classList.add("hidden");
        const introEl = $("iv-intro");
        introEl.classList.remove("hidden");
        $("iv-intro-icon").textContent = icon;
        await new Promise((r) => setTimeout(r, 1100));
        introEl.classList.add("hidden");
    }

    // The voice can only start after a click (browser rule): the interview opens with a button.
    $("iv-start-btn").addEventListener("click", async () => {
        busy = true;
        await playIntro();
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
