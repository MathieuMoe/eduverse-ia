// Test runner of the student platform: practice, placement, duels and AI topics.
// The server keeps the answer key; the browser only gets it after answering. Young children get big buttons and the question read aloud.
(() => {
    "use strict";
    const { t, fmt, esc, api, lang } = window.Edu;
    const $ = (id) => document.getElementById(id);
    const root = $("qz-root");
    const stage = $("qz-stage");
    const { area, level, mode, topic, duel, theme, figure } = root.dataset;
    const kids = root.dataset.kids === "1";
    const speakButton = $("qz-speak");
    const oralButton = $("qz-oral");
    let question = null;
    let xp = 0;
    let speak = kids;
    // "Oral exam": a hands-free loop over the existing voice-in/voice-out of this page (Tutor speaks, then listens on
    // its own) — not a new backend, just the Tutor's own prompt/feedback/mic already wired here, chained together.
    let oral = false;
    let listening = false;
    let attemptId = null;
    let confidence = 1;
    let bets = []; // answers of this test given as sure (2) or not sure (0)
    let shownAt = 0; // when the current question appeared (rhythm advice only)
    let hintsUsed = 0;
    let rungsUsed = 0;
    const reasons = JSON.parse(root.dataset.reasons || "{}");
    const ladderAllowed = mode !== "Placement" && mode !== "Duel";

    const btn = "py-2.5 px-5 inline-flex justify-center items-center gap-x-2 text-sm font-semibold rounded-full focus:outline-hidden focus-visible:ring-2 focus-visible:ring-primary/30 disabled:opacity-50";
    const primary = `${btn} bg-primary text-primary-foreground hover:bg-primary-hover`;
    const light = `${btn} border border-layer-line bg-layer text-foreground hover:bg-layer-hover`;
    const voices = { en: "en-GB", es: "es-ES", fr: "fr-FR", ca: "ca-ES" };

    // onend fires even when speak is off (no-op synthesis) so an oral-mode caller can always chain the next step.
    // force bypasses the speak toggle: a "Listen and answer" drill (task #114) has nothing to read if speech is off,
    // so its prompt must be heard regardless of whether the student has the speaker icon enabled.
    function say(text, onend, force) {
        if ((!speak && !force) || !("speechSynthesis" in window)) { onend?.(); return; }
        window.speechSynthesis.cancel();
        const u = new SpeechSynthesisUtterance(text);
        u.lang = voices[lang.slice(0, 2)] || "en-GB";
        u.rate = kids ? 0.9 : 1;
        u.onend = u.onerror = () => onend?.();
        window.speechSynthesis.speak(u);
    }

    speakButton.addEventListener("click", () => {
        speak = !speak;
        speakButton.setAttribute("aria-pressed", String(speak));
        if (speak && question) say(question.prompt);
        else window.speechSynthesis?.cancel();
    });

    /** Turns hands-free mode off (a misheard answer, a mic error, or the student turning it off) and says why, if asked to. */
    function stopOral(message) {
        if (!oral) return;
        oral = false;
        oralButton.setAttribute("aria-pressed", "false");
        if (message) {
            const heard = $("qz-heard");
            if (heard) { heard.textContent = message; heard.classList.remove("hidden"); }
        }
    }

    /** Listens once and answers (used by the mic button and, in oral mode, right after the Tutor finishes speaking). */
    async function autoListen() {
        const mic = $("qz-mic");
        const heard = $("qz-heard");
        if (listening || !question || !mic) return;
        listening = true;
        mic.setAttribute("aria-pressed", "true");
        mic.classList.add("animate-pulse");
        if (heard) { heard.textContent = t("Listening"); heard.classList.remove("hidden"); }
        try {
            const said = await window.Edu.voice.listen();
            const open = question.options.map((o, i) => stage.querySelector(`[data-index="${i}"]`)?.dataset.state === "removed" ? "\u0000" : o);
            const i = window.Edu.voice.match(said, open);
            listening = false;
            if (i >= 0) {
                if (heard) { heard.textContent = fmt(t("Heard"), said[0]); heard.classList.remove("hidden"); }
                answer(i);
            } else {
                if (heard) { heard.textContent = fmt(t("NotUnderstood"), said[0]); heard.classList.remove("hidden"); }
                stopOral(oral ? t("OralPaused") : null);
            }
        } catch (e) {
            listening = false;
            if (e.message !== "no-speech" && heard) { heard.textContent = t("MicError"); heard.classList.remove("hidden"); }
            stopOral(oral && e.message !== "no-speech" ? t("OralPaused") : null);
        }
        mic.setAttribute("aria-pressed", "false");
        mic.classList.remove("animate-pulse");
    }

    if (oralButton && window.Edu.voice?.supported && "speechSynthesis" in window) {
        oralButton.classList.remove("hidden");
        oralButton.addEventListener("click", () => {
            oral = !oral;
            oralButton.setAttribute("aria-pressed", String(oral));
            if (oral) {
                if (!speak) { speak = true; speakButton.setAttribute("aria-pressed", "true"); }
                if (question && !listening) autoListen();
            } else {
                window.speechSynthesis?.cancel();
            }
        });
    }

    function setProgress(done, total) {
        const p = total ? Math.round(100 * done / total) : 0;
        $("qz-bar-fill").style.width = p + "%";
        $("qz-bar").setAttribute("aria-valuenow", String(p));
    }

    function toast(reward) {
        const items = [...(reward?.badges || []).map((b) => `${b.icon} ${esc(fmt(t("NewBadge"), b.name))}`), ...(reward?.quests || []).map((q) => `🎯 ${esc(fmt(t("QuestDone"), q.name))}`)];
        const el = $("qz-toast");
        if (!items.length) return;
        el.innerHTML = items.join("<br>");
        el.classList.remove("hidden");
        setTimeout(() => el.classList.add("hidden"), 6000);
    }

    function addXp(amount) {
        if (!amount) return;
        xp += amount;
        $("qz-xp").textContent = String(xp);
        const pop = document.createElement("span");
        pop.className = "xp-pop absolute top-16 right-6 text-lg font-bold text-primary pointer-events-none";
        pop.textContent = `+${amount} XP`;
        stage.appendChild(pop);
        setTimeout(() => pop.remove(), 1200);
    }

    // Replays a CSS animation class even if the element already carries it (e.g. two correct answers in a row).
    function pulse(el, cls, ms) {
        if (!el) return;
        el.classList.remove(cls);
        void el.offsetWidth; // force a reflow so the browser treats the next add as a fresh animation start
        el.classList.add(cls);
        setTimeout(() => el.classList.remove(cls), ms);
    }

    // A small confetti burst (Duolingo-style streak milestone) — a handful of coloured squares fanning outward
    // and fading, built with CSS custom properties per dot so no JS animation loop or extra library is needed.
    function confettiBurst(target) {
        const colors = ["#f6c85f", "#6fcf97", "#56ccf2", "#bb6bd9", "#f2994a"];
        const burst = document.createElement("span");
        burst.className = "qz-confetti";
        for (let i = 0; i < 14; i++) {
            const dot = document.createElement("span");
            const angle = (Math.PI * 2 * i) / 14 + Math.random() * 0.4;
            const dist = 40 + Math.random() * 30;
            dot.style.setProperty("--qz-confetti-end", `translate(${Math.round(Math.cos(angle) * dist)}px, ${Math.round(Math.sin(angle) * dist - 20)}px)`);
            dot.style.setProperty("--qz-confetti-rot", `${Math.round(Math.random() * 360)}deg`);
            dot.style.background = colors[i % colors.length];
            dot.style.animationDelay = `${Math.round(Math.random() * 60)}ms`;
            burst.appendChild(dot);
        }
        target.appendChild(burst);
        setTimeout(() => burst.remove(), 1000);
    }

    function adventureHtml(a) {
        if (!a) return "";
        $("qz-title").textContent = a.title;
        const doors = Array.from({ length: a.doors }, (_, i) => {
            const state = i < a.passed ? "✅" : i === a.passed && !a.inBoss ? "🚪" : "🔒";
            return `<li class="size-9 sm:size-10 rounded-full grid place-items-center text-lg ${i === a.passed && !a.inBoss ? "bg-white dark:bg-white/20 ring-2 ring-primary animate-pulse" : "bg-white/60 dark:bg-white/10"}" aria-label="${esc(fmt(t("DoorN"), i + 1))}">${state}</li>`;
        }).join(`<li class="h-0.5 w-3 sm:w-5 bg-foreground/20 rounded" aria-hidden="true"></li>`);
        const hp = Math.round(100 * a.bossHp / a.bossMax);
        const hearts = "❤️".repeat(Math.max(0, a.hearts)) + "🤍".repeat(Math.max(0, a.maxHearts - a.hearts));
        return `<div class="rounded-3xl tone-${a.inBoss ? "pink" : "sky"} p-4 sm:p-5" data-testid="adventure-map">
            <div class="flex flex-wrap items-center justify-between gap-3">
                <ol class="flex items-center gap-1 sm:gap-1.5" aria-label="${esc(t("Map"))}">${doors}<li class="h-0.5 w-3 sm:w-5 bg-foreground/20 rounded" aria-hidden="true"></li>
                    <li class="size-11 rounded-2xl grid place-items-center text-2xl ${a.inBoss ? "bg-white dark:bg-white/20 ring-2 ring-rose-400" : "bg-white/60 dark:bg-white/10"}" aria-label="${esc(a.boss)}">🐲</li></ol>
                <span class="text-lg tracking-wider" aria-label="${esc(fmt(t("HeartsN"), a.hearts))}" data-testid="hearts">${hearts}</span>
            </div>
            ${a.inBoss ? `<div class="mt-3"><div class="flex justify-between text-xs text-foreground/70"><span>${esc(a.boss)}</span><span>${a.bossHp}/${a.bossMax}</span></div>
                <div class="mt-1 h-2.5 rounded-full bg-white/70 dark:bg-white/10 overflow-hidden"><div class="h-full rounded-full bg-rose-500 transition-all" style="width:${hp}%"></div></div></div>` : ""}
            ${a.intro ? `<p class="mt-3 text-sm font-medium text-foreground">${esc(a.intro)}</p>` : ""}
            <p class="mt-2 text-sm text-foreground/80 italic" data-testid="story-line">${esc(a.line)}</p>
        </div>`;
    }

    function render(q) {
        question = q;
        attemptId = q.id;
        setProgress(q.number - 1, q.length);
        $("qz-level").textContent = mode === "Review" ? `${q.areaName} · ${q.levelName} · ` : q.levelName;
        if (mode === "Review") $("qz-level").classList.remove("hidden");
        const dots = [1, 2, 3].map((d) => `<span class="size-1.5 rounded-full ${d <= q.difficulty ? "bg-primary" : "bg-layer-line"}"></span>`).join("");
        stage.innerHTML = `${adventureHtml(q.adventure)}
            <div class="${q.adventure ? "mt-5 " : ""}flex flex-wrap items-center justify-between gap-2 text-xs text-muted-foreground-1">
                <span>${esc(q.adventure ? fmt(t("QuestionN"), q.number) : fmt(t("QuestionOf"), q.number, q.length))}</span>
                <span class="inline-flex items-center gap-2"><span class="inline-flex items-center py-0.5 px-2 rounded-full bg-surface text-foreground">${esc(q.topic)}</span><span class="inline-flex gap-1 tip" data-tip="${esc(t("Difficulty"))}" aria-label="${esc(fmt(t("DifficultyN"), q.difficulty))}">${dots}</span></span>
            </div>
            ${q.visual?.startsWith("dg:") ? `<div class="quiz-diagram mt-5 flex justify-center" data-testid="quiz-diagram">${window.Edu.diagram(q.visual)}</div>` : q.visual ? `<p class="quiz-visual mt-5 text-4xl text-center leading-snug" aria-hidden="true">${esc(q.visual)}</p>` : ""}
            ${q.exerciseKind === "Listen" && "speechSynthesis" in window ? `<h2 class="quiz-prompt mt-4 text-xl font-semibold text-foreground text-center sr-only" id="qz-prompt">${esc(q.prompt)}</h2>
            <div class="mt-4 flex flex-col items-center gap-2" data-testid="quiz-listen">
                <button type="button" id="qz-listen-replay" class="size-16 rounded-full bg-primary text-primary-foreground text-2xl grid place-items-center hover:bg-primary-hover" aria-label="${esc(t("ListenReplay"))}">🔊</button>
                <p class="text-sm text-muted-foreground-1">${esc(t("ListenHint"))}</p>
            </div>` : `<h2 class="quiz-prompt mt-4 text-xl font-semibold text-foreground text-center" id="qz-prompt">${esc(q.prompt)}</h2>`}
            ${q.code ? `<pre class="mt-4 p-4 rounded-2xl bg-[#2b2230] text-[#f3eefe] text-sm overflow-x-auto"><code>${esc(q.code)}</code></pre>` : ""}
            ${q.evo ? `<div class="mt-5 flex items-start gap-3 justify-center" data-testid="evo-answer">
                <span class="size-11 shrink-0 rounded-full tone-lavender grid place-items-center text-2xl" aria-hidden="true">🦉</span>
                <div class="text-left"><p class="inline-block py-2 px-3.5 rounded-2xl rounded-tl-sm tone-sun text-foreground"><span class="text-xs text-foreground/60 block">${esc(t("EvoAnswered"))}</span><span class="text-lg font-semibold">${esc(q.evo)}</span></p>
                <p class="mt-1.5 text-sm font-medium text-foreground">${esc(q.evoTask || "")}</p></div></div>` : ""}
            ${kids || q.mode === "Placement" ? "" : `<div class="mt-5 flex flex-wrap items-center justify-center gap-2 text-sm" role="radiogroup" aria-label="${esc(t("HowSure"))}" data-testid="confidence">
                <span class="text-muted-foreground-1 me-1">${esc(t("HowSure"))}</span>
                ${[[0, "🤔", t("Unsure")], [1, "🙂", t("ThinkSo")], [2, "😎", t("Sure")]].map(([v, e, label]) => `<button type="button" role="radio" aria-checked="${v === 1}" data-confidence="${v}" class="py-1 px-3 rounded-full border ${v === 1 ? "border-primary bg-primary/10" : "border-layer-line"} text-foreground">${e} ${esc(label)}</button>`).join("")}
            </div>`}
            ${q.exerciseKind === "TrueFalse" ? `<div class="mt-6 grid grid-cols-2 gap-3" role="group" aria-labelledby="qz-prompt" data-testid="quiz-truefalse">
                <button type="button" class="quiz-option flex items-center justify-center gap-2 w-full p-5 rounded-2xl border border-layer-line bg-layer text-foreground text-lg font-semibold hover:bg-layer-hover disabled:cursor-default" data-index="0">❌ ${esc(t("False"))}</button>
                <button type="button" class="quiz-option flex items-center justify-center gap-2 w-full p-5 rounded-2xl border border-layer-line bg-layer text-foreground text-lg font-semibold hover:bg-layer-hover disabled:cursor-default" data-index="1">✅ ${esc(t("True"))}</button>
            </div>` : q.exerciseKind === "FillBlank" ? `<div class="mt-6" data-testid="quiz-fillblank">
                <label for="qz-fillblank-input" class="sr-only">${esc(t("Answer"))}</label>
                <input id="qz-fillblank-input" type="text" autocomplete="off" class="w-full py-3 px-4 rounded-2xl border border-layer-line bg-layer text-foreground text-lg text-center" placeholder="${esc(t("Answer"))}">
                <button type="button" id="qz-fillblank-check" class="${primary} mt-3 w-full justify-center" data-testid="quiz-check">${esc(t("Check"))}</button>
            </div>` : q.exerciseKind === "Reorder" ? `<div class="mt-6" data-testid="quiz-reorder">
                <div id="qz-reorder-built" class="flex flex-wrap gap-2 min-h-14 p-3 rounded-2xl border-2 border-dashed border-layer-line mb-3"></div>
                <div id="qz-reorder-pool" class="flex flex-wrap gap-2" role="group" aria-labelledby="qz-prompt">
                    ${q.tokens.map((tok, i) => `<button type="button" class="quiz-token py-2 px-3.5 rounded-xl border border-layer-line bg-layer text-foreground text-sm font-medium hover:bg-layer-hover" data-pos="${i}">${esc(tok)}</button>`).join("")}
                </div>
                <button type="button" id="qz-reorder-check" class="${primary} mt-3 w-full justify-center" style="display:none" data-testid="quiz-check">${esc(t("Check"))}</button>
            </div>` : q.exerciseKind === "Match" ? `<div class="mt-6" data-testid="quiz-match">
                <div class="grid grid-cols-2 gap-3">
                    <div class="space-y-2" id="qz-match-left">${q.matchLeft.map((l, i) => `<button type="button" class="quiz-match-item w-full text-left py-2.5 px-3.5 rounded-xl border border-layer-line bg-layer text-foreground text-sm font-medium hover:bg-layer-hover" data-left="${i}">${esc(l)}</button>`).join("")}</div>
                    <div class="space-y-2" id="qz-match-right">${q.matchRight.map((r, i) => `<button type="button" class="quiz-match-item w-full text-left py-2.5 px-3.5 rounded-xl border border-layer-line bg-layer text-foreground text-sm font-medium hover:bg-layer-hover" data-right="${i}">${esc(r)}</button>`).join("")}</div>
                </div>
                <button type="button" id="qz-match-check" class="${primary} mt-3 w-full justify-center" style="display:none" data-testid="quiz-check">${esc(t("Check"))}</button>
            </div>` : q.exerciseKind === "MultiSelect" ? `<div class="mt-6" data-testid="quiz-multiselect">
                <div class="grid sm:grid-cols-2 gap-3" role="group" aria-labelledby="qz-prompt">
                    ${q.options.map((o, i) => `<button type="button" class="quiz-ms-option flex items-center gap-3 w-full text-left p-4 rounded-2xl border border-layer-line bg-layer text-foreground text-base font-medium hover:bg-layer-hover" aria-pressed="false" data-ms-index="${i}">
                        <span class="size-5 shrink-0 rounded-md border-2 border-layer-line grid place-items-center text-xs" aria-hidden="true" data-ms-check></span><span>${esc(o)}</span></button>`).join("")}
                </div>
                <button type="button" id="qz-multiselect-check" class="${primary} mt-3 w-full justify-center" style="display:none" data-testid="quiz-check">${esc(t("Check"))}</button>
            </div>` : `<div class="mt-6 grid sm:grid-cols-2 ${q.options.every((o) => String(o).length < 40) && !q.code ? "2xl:grid-cols-4" : ""} gap-3" role="group" aria-labelledby="qz-prompt" data-testid="quiz-options">
                ${q.options.map((o, i) => `<button type="button" class="quiz-option flex items-center gap-3 w-full text-left p-4 rounded-2xl border border-layer-line bg-layer text-foreground text-base font-medium hover:bg-layer-hover disabled:cursor-default" data-index="${i}">
                    <span class="size-7 shrink-0 rounded-full bg-surface grid place-items-center text-xs font-bold" aria-hidden="true">${"ABCD"[i]}</span><span>${esc(o)}</span></button>`).join("")}
            </div>`}
            ${q.exerciseKind ? "" : `<div class="mt-4 flex flex-wrap items-center justify-between gap-2">
                <button type="button" id="qz-hint" class="py-1.5 px-3.5 inline-flex items-center gap-x-1.5 text-sm font-medium rounded-full border border-layer-line bg-layer text-foreground hover:bg-layer-hover disabled:opacity-50" data-testid="hint">💡 <span id="qz-hint-label"></span></button>
                ${ladderAllowed ? `<button type="button" id="qz-ladder-open" class="py-1.5 px-3.5 inline-flex items-center gap-x-1.5 text-sm font-medium rounded-full border border-layer-line bg-layer text-foreground hover:bg-layer-hover disabled:opacity-50" data-testid="ladder-open">🪜 ${esc(t("LadderOpen"))}</button>` : ""}
                ${window.Edu.voice?.supported ? `<button type="button" id="qz-mic" class="py-1.5 px-3.5 inline-flex items-center gap-x-1.5 text-sm font-medium rounded-full border border-layer-line bg-layer text-foreground hover:bg-layer-hover" aria-pressed="false" data-testid="mic">🎤 ${esc(t("SayAnswer"))}</button>` : ""}
                <span class="text-[11px] text-muted-foreground-1">${esc(t("HintCost"))}</span>
            </div>
            <p id="qz-heard" class="hidden mt-2 text-sm text-muted-foreground-1" role="status"></p>
            <p id="qz-hint-text" class="hidden mt-3 p-3 rounded-2xl tone-sky text-sm text-foreground" role="status"></p>
            <section id="qz-ladder" class="hidden mt-3 p-4 rounded-2xl tone-lavender text-left" aria-label="${esc(t("LadderOpen"))}" data-testid="ladder">
                <div class="flex flex-wrap items-center justify-between gap-2"><p class="text-sm font-semibold text-foreground">🪜 ${esc(t("LadderOpen"))}</p><p class="text-xs text-foreground/70" id="qz-ladder-step"></p></div>
                <div id="qz-ladder-chat" class="mt-3 space-y-2 text-sm" aria-live="polite"></div>
                <form id="qz-ladder-form" class="mt-3 flex flex-wrap gap-2">
                    <label for="qz-ladder-reply" class="sr-only">${esc(t("LadderReply"))}</label>
                    <input id="qz-ladder-reply" class="flex-1 min-w-40 py-2 px-3.5 bg-layer border border-layer-line rounded-xl text-sm text-foreground placeholder:text-muted-foreground" maxlength="300" autocomplete="off" placeholder="${esc(t("LadderReply"))}" data-testid="ladder-reply">
                    <button type="submit" class="${light}" id="qz-ladder-next" data-testid="ladder-next"></button>
                </form>
                <p class="mt-2 text-[11px] text-foreground/70">${esc(t("LadderRule"))}</p>
            </section>`}
            <div id="qz-feedback" class="hidden mt-5"></div>`;
        stage.querySelectorAll("[data-index]").forEach((b) => b.addEventListener("click", () => answer(+b.dataset.index)));
        confidence = 1;
        shownAt = Date.now();
        hintsUsed = 0;
        rungsUsed = 0;
        $("qz-ladder-open")?.addEventListener("click", () => { $("qz-ladder-open").disabled = true; $("qz-ladder").classList.remove("hidden"); climb(null); });
        $("qz-ladder-form")?.addEventListener("submit", (e) => { e.preventDefault(); const f = $("qz-ladder-reply"); const said = f.value.trim(); f.value = ""; climb(said); });
        stage.querySelectorAll("[data-confidence]").forEach((b) => b.addEventListener("click", () => {
            confidence = +b.dataset.confidence;
            stage.querySelectorAll("[data-confidence]").forEach((x) => {
                const on = x === b;
                x.setAttribute("aria-checked", String(on));
                x.classList.toggle("border-primary", on);
                x.classList.toggle("bg-primary/10", on);
                x.classList.toggle("border-layer-line", !on);
            });
        }));
        updateHint();
        $("qz-hint")?.addEventListener("click", hint);
        $("qz-mic")?.addEventListener("click", () => autoListen());
        $("qz-fillblank-check")?.addEventListener("click", checkFillBlank);
        $("qz-fillblank-input")?.addEventListener("keydown", (e) => { if (e.key === "Enter") { e.preventDefault(); checkFillBlank(); } });
        if (q.exerciseKind === "Reorder") wireReorder(q.tokens);
        if (q.exerciseKind === "Match") wireMatch(q.matchLeft, q.matchRight);
        if (q.exerciseKind === "MultiSelect") wireMultiSelect();
        $("qz-listen-replay")?.addEventListener("click", () => say(q.prompt, null, true));
        stage.querySelector("[data-index]")?.focus();
        $("qz-fillblank-input")?.focus();
        if (q.exerciseKind === "Listen") {
            // The prompt itself is the thing to hear, not a lead-in to read options aloud — speak it alone, and force
            // it regardless of the speak toggle, since a muted "Listen and answer" drill would have nothing to answer.
            say(q.prompt, () => { if (oral) autoListen(); }, true);
        } else {
            const spokenOptions = q.exerciseKind === "TrueFalse" ? [t("False"), t("True")].join(", ") : q.options.join(", ");
            say((q.adventure ? (q.adventure.intro ? q.adventure.intro + " " : "") + q.adventure.line + " " : "") + q.prompt + (q.visual && !q.visual.startsWith("dg:") ? "" : ". " + spokenOptions),
                () => { if (oral) autoListen(); });
        }
    }

    function updateHint() {
        const b = $("qz-hint");
        if (!b) return;
        $("qz-hint-label").textContent = hintsUsed >= 3 ? t("NoMoreHints") : fmt(t("HintN"), hintsUsed + 1, 3);
        b.disabled = hintsUsed >= 3;
    }

    async function hint() {
        const b = $("qz-hint");
        b.disabled = true;
        try {
            const h = await api(`/api/quiz/${encodeURIComponent(question.id)}/hint`, { json: {} });
            hintsUsed = h.level;
            h.removed.forEach((i) => {
                const o = stage.querySelector(`[data-index="${i}"]`);
                if (o) { o.dataset.state = "removed"; o.disabled = true; o.setAttribute("aria-label", t("Removed")); }
            });
            if (h.text) {
                const p = $("qz-hint-text");
                p.textContent = "🧭 " + h.text;
                p.classList.remove("hidden");
                say(h.text);
            }
        } catch (e) { showError(e); }
        updateHint();
    }

    function ladderBubble(who, text) {
        const row = document.createElement("div");
        row.className = who === "me" ? "flex justify-end" : "flex items-start gap-2";
        row.innerHTML = who === "me"
            ? `<p class="py-2 px-3.5 rounded-2xl rounded-tr-sm bg-primary text-primary-foreground max-w-[85%]">${esc(text)}</p>`
            : `<span class="size-8 shrink-0 rounded-full bg-white/70 dark:bg-white/10 grid place-items-center" aria-hidden="true">🦉</span><p class="py-2 px-3.5 rounded-2xl rounded-tl-sm bg-white/80 dark:bg-white/10 text-foreground max-w-[85%]" data-testid="ladder-question">${esc(text)}</p>`;
        $("qz-ladder-chat").appendChild(row);
        row.scrollIntoView({ block: "nearest", behavior: "smooth" });
    }

    function ladderLabels(done) {
        $("qz-ladder-step").textContent = fmt(t("LadderStep"), rungsUsed, 4) + " · " + t(rungsUsed <= 2 ? "LadderFree" : "LadderCosts");
        const next = $("qz-ladder-next");
        next.textContent = rungsUsed >= 2 ? t("LadderNextCosts") : t("LadderNext");
        if (done) { $("qz-ladder-form").classList.add("hidden"); }
    }

    // "Think with the Tutor": one rung per call; the server decides the rung and never gives the answer.
    async function climb(said) {
        const next = $("qz-ladder-next");
        next.disabled = true;
        if (said) ladderBubble("me", said);
        try {
            const r = await api(`/api/quiz/${encodeURIComponent(question.id)}/ladder`, { json: { reply: said || null } });
            rungsUsed = r.rung;
            if (r.reaction) ladderBubble("evo", r.reaction);
            ladderBubble("evo", r.question);
            say(r.question);
            ladderLabels(r.done || r.rung >= r.max);
        } catch (e) { showError(e); }
        next.disabled = false;
    }

    // "Tell me how you thought": after a wrong answer the student picks a reason or explains; the code chooses the remedy.
    function thinkingBlock(number) {
        return `<section class="mt-4 p-4 rounded-2xl bg-white/70 dark:bg-white/10 text-left" id="qz-think" data-number="${number}" data-testid="thinking">
            <p class="text-sm font-semibold text-foreground">🧠 ${esc(t("ThinkTitle"))} <span class="font-normal text-foreground/70">${esc(t("ThinkXp"))}</span></p>
            <div class="mt-2 flex flex-wrap gap-1.5">${Object.entries(reasons).map(([k, label]) => `<button type="button" class="py-1 px-3 rounded-full text-xs font-semibold border border-layer-line bg-layer text-foreground hover:bg-layer-hover" data-reason="${esc(k)}" data-testid="reason-${esc(k)}">${esc(label)}</button>`).join("")}</div>
            <form class="mt-3 flex flex-wrap gap-2" id="qz-think-form">
                <label for="qz-think-text" class="sr-only">${esc(t("ThinkPlaceholder"))}</label>
                <input id="qz-think-text" class="flex-1 min-w-40 py-2 px-3.5 bg-layer border border-layer-line rounded-xl text-sm text-foreground placeholder:text-muted-foreground" maxlength="400" autocomplete="off" placeholder="${esc(t("ThinkPlaceholder"))}" data-testid="thinking-text">
                ${window.Edu.voice?.supported ? `<button type="button" id="qz-think-mic" class="${light}" aria-pressed="false" aria-label="${esc(t("SayAnswer"))}">🎤</button>` : ""}
                <button type="submit" class="${light}" data-testid="thinking-send">${esc(t("ThinkSend"))}</button>
            </form>
            <p class="mt-2 text-[11px] text-foreground/70">🔒 ${esc(t("ThinkPrivacy"))}</p>
            <p class="mt-2 text-sm text-red-700 dark:text-red-300 hidden" role="alert" id="qz-think-error"></p>
        </section>`;
    }

    function wireThinking(id) {
        const box = $("qz-think");
        if (!box) return;
        const number = +box.dataset.number;
        async function send(body) {
            box.querySelectorAll("button, input").forEach((x) => { x.disabled = true; });
            try {
                const r = await api("/api/thinking", { json: { attemptId: id, number, ...body } });
                addXp(r.reward?.xp);
                toast(r.reward);
                box.innerHTML = `<p class="text-xs font-semibold uppercase tracking-wide text-foreground/60">🧠 ${esc(t("ThinkResult"))}</p>
                    <p class="mt-1"><span class="inline-flex py-0.5 px-2.5 rounded-full text-xs font-semibold bg-primary text-primary-foreground" data-testid="thinking-error" data-error="${esc(r.error)}">${esc(r.label)}</span></p>
                    <p class="mt-2 text-sm text-foreground">${esc(r.feedback)}</p>
                    ${r.question ? `<p class="mt-1 text-sm font-medium text-foreground">❓ ${esc(r.question)}</p>` : ""}
                    <p class="mt-2 text-sm text-foreground/80" data-testid="thinking-tip">👉 ${esc(r.tip)}</p>
                    <div class="mt-3 flex flex-wrap gap-2">${r.action ? `<a class="${light}" href="${esc(r.action.url)}" data-testid="thinking-action">${esc(r.action.label)}</a>` : ""}<a class="${light}" href="/Student/Learner">🧭 ${esc(t("ThinkCard"))}</a></div>`;
            } catch (e) {
                const err = $("qz-think-error");
                err.textContent = e.message;
                err.classList.remove("hidden");
                box.querySelectorAll("button, input").forEach((x) => { x.disabled = false; });
            }
        }
        box.querySelectorAll("[data-reason]").forEach((b) => b.addEventListener("click", () => send({ reason: b.dataset.reason })));
        $("qz-think-form").addEventListener("submit", (e) => {
            e.preventDefault();
            const text = $("qz-think-text").value.trim();
            if (text.length < 3) { $("qz-think-text").focus(); return; }
            send({ text });
        });
        if ($("qz-think-mic")) window.Edu.voice.dictation($("qz-think-mic"), $("qz-think-text"), () => {});
    }

    function finishAnswer(res, isDrill, answered, markCorrect) {
        if (res.confidence !== 1 && res.confidence !== undefined) bets.push({ sure: res.confidence === 2, correct: res.correct });
        markCorrect?.();
        $("qz-run").textContent = String(res.run);
        addXp(res.reward?.xp);
        toast(res.reward);
        if (res.correct) {
            pulse($("qz-run").parentElement, "qz-streak-pop", 500);
            if ((res.run || 0) > 0 && res.run % 5 === 0) confettiBurst(stage);
        } else {
            pulse(stage, "qz-shake", 400);
        }
        const fb = $("qz-feedback");
        fb.className = `mt-5 p-4 rounded-2xl ${res.correct ? "tone-mint" : "tone-pink"}`;
        void fb.offsetWidth;
        fb.classList.add("qz-pop-in");
        const notes = [];
        if (res.confidence === 2 && res.correct) notes.push("😎 " + t("SureRight"));
        if (res.confidence === 2 && !res.correct) notes.push("🔁 " + t("SureWrong"));
        if (res.confidence === 0 && res.correct) notes.push("🤔 " + t("UnsureRight"));
        if (res.hints > 0 && res.correct) notes.push("💡 " + fmt(t("HintsUsed"), res.hints));
        if (res.rungs > 0 && res.correct) notes.push("🪜 " + t(res.rungs <= 2 ? "LadderHelpedFree" : "LadderHelped"));
        fb.innerHTML = `<p class="font-semibold text-foreground">${res.correct ? "✅ " + esc(t(kids ? "KidsRight" : "Right")) : "💡 " + esc(t("NotQuite"))}</p>
            <p class="mt-1 text-sm text-foreground/80">${esc(res.explanation)}</p>
            ${notes.map((n) => `<p class="mt-1 text-xs text-foreground/70">${esc(n)}</p>`).join("")}
            ${res.pace ? `<p class="mt-2 p-2.5 rounded-xl bg-white/60 dark:bg-white/10 text-sm text-foreground" data-testid="quiz-pace" data-pace="${esc(res.pace.kind)}">⏱️ ${esc(res.pace.text)}</p>` : ""}
            ${res.correct || !Object.keys(reasons).length || isDrill ? "" : thinkingBlock(answered.number)}
            <div class="mt-3 flex justify-end"><button type="button" class="${primary}" id="qz-next" data-testid="quiz-next">${esc(res.summary ? t("SeeResults") : t("Next"))} →</button></div>`;
        wireThinking(answered.id);
        const next = $("qz-next");
        next.focus();
        const advance = () => res.summary ? summary(res.summary) : render(res.next);
        next.addEventListener("click", advance);
        say(res.correct ? t(kids ? "KidsRight" : "Right") : t("NotQuite") + ". " + res.explanation, () => { if (oral) advance(); });
    }

    async function answer(index) {
        const buttons = [...stage.querySelectorAll("[data-index]")];
        if (buttons.some((b) => b.disabled && b.dataset.state !== "removed") || buttons[index].dataset.state === "removed") return;
        buttons.forEach((b) => { b.disabled = true; });
        stage.querySelectorAll("[data-confidence]").forEach((b) => { b.disabled = true; });
        if ($("qz-hint")) $("qz-hint").disabled = true;
        if ($("qz-ladder-open")) $("qz-ladder-open").disabled = true;
        $("qz-ladder-form")?.querySelectorAll("input, button").forEach((x) => { x.disabled = true; });
        const answered = { id: question.id, number: question.number };
        const seconds = shownAt ? Math.max(1, Math.round((Date.now() - shownAt) / 1000)) : 0;
        const isDrill = question.exerciseKind === "TrueFalse" || question.exerciseKind === "Listen";
        let res;
        try {
            res = question.exerciseKind === "TrueFalse"
                ? await api(`/api/quiz/${encodeURIComponent(question.id)}/truefalse`, { json: { value: index === 1, confidence, seconds } })
                : question.exerciseKind === "Listen"
                    ? await api(`/api/quiz/${encodeURIComponent(question.id)}/listen`, { json: { choice: index, confidence, seconds } })
                    : await api(`/api/quiz/${encodeURIComponent(question.id)}/answer`, { json: { choice: index, confidence, seconds } });
        } catch (e) {
            buttons.forEach((b) => { b.disabled = b.dataset.state === "removed"; });
            $("qz-ladder-form")?.querySelectorAll("input, button").forEach((x) => { x.disabled = false; });
            if ($("qz-ladder-open") && rungsUsed === 0) $("qz-ladder-open").disabled = false;
            showError(e);
            return;
        }

        finishAnswer(res, isDrill, answered, () => {
            buttons[res.correctIndex].dataset.state = "right";
            if (!res.correct) buttons[index].dataset.state = "wrong";
        });
    }

    async function submitDrill(kind, extra, onError) {
        const answered = { id: question.id, number: question.number };
        const seconds = shownAt ? Math.max(1, Math.round((Date.now() - shownAt) / 1000)) : 0;
        stage.querySelectorAll("[data-confidence]").forEach((b) => { b.disabled = true; });
        let res;
        try {
            res = await api(`/api/quiz/${encodeURIComponent(question.id)}/${kind}`, { json: { ...extra, confidence, seconds } });
        } catch (e) {
            stage.querySelectorAll("[data-confidence]").forEach((b) => { b.disabled = false; });
            onError?.();
            showError(e);
            return;
        }
        finishAnswer(res, true, answered, null);
    }

    function checkFillBlank() {
        const b = $("qz-fillblank-check");
        const input = $("qz-fillblank-input");
        if (!b || !input || b.disabled) return;
        b.disabled = true; input.disabled = true;
        submitDrill("fillblank", { value: input.value.trim() }, () => { b.disabled = false; input.disabled = false; });
    }

    function wireReorder(tokens) {
        const poolButtons = [...$("qz-reorder-pool").querySelectorAll("[data-pos]")];
        const builtEl = $("qz-reorder-built");
        const checkBtn = $("qz-reorder-check");
        const order = [];
        function renderBuilt() {
            builtEl.innerHTML = order.map((pos) => `<button type="button" class="quiz-token py-2 px-3.5 rounded-xl border border-primary bg-primary/10 text-foreground text-sm font-medium" data-pos="${pos}">${esc(tokens[pos])}</button>`).join("");
            builtEl.querySelectorAll("[data-pos]").forEach((b) => b.addEventListener("click", () => {
                const pos = +b.dataset.pos;
                order.splice(order.indexOf(pos), 1);
                poolButtons[pos].disabled = false;
                poolButtons[pos].classList.remove("opacity-30");
                renderBuilt();
            }));
            checkBtn.style.display = order.length !== tokens.length ? "none" : "";
        }
        poolButtons.forEach((b) => b.addEventListener("click", () => {
            if (b.disabled) return;
            const pos = +b.dataset.pos;
            order.push(pos);
            b.disabled = true;
            b.classList.add("opacity-30");
            renderBuilt();
        }));
        checkBtn.addEventListener("click", () => {
            checkBtn.disabled = true;
            submitDrill("reorder", { order: order.map((pos) => tokens[pos]) }, () => { checkBtn.disabled = false; });
        });
    }

    function wireMatch(lefts, rights) {
        const leftEls = [...$("qz-match-left").querySelectorAll("[data-left]")];
        const rightEls = [...$("qz-match-right").querySelectorAll("[data-right]")];
        const checkBtn = $("qz-match-check");
        const pairs = {};
        let selectedLeft = null;
        leftEls.forEach((el) => el.addEventListener("click", () => {
            if (el.disabled) return;
            leftEls.forEach((x) => x.classList.remove("border-primary", "bg-primary/10"));
            selectedLeft = el;
            el.classList.add("border-primary", "bg-primary/10");
        }));
        rightEls.forEach((el) => el.addEventListener("click", () => {
            if (el.disabled || !selectedLeft) return;
            const leftEl = selectedLeft;
            const leftText = lefts[+leftEl.dataset.left];
            const rightText = rights[+el.dataset.right];
            pairs[leftText] = rightText;
            leftEl.disabled = true;
            leftEl.classList.remove("border-primary", "bg-primary/10");
            el.disabled = true;
            selectedLeft = null;
            checkBtn.style.display = Object.keys(pairs).length !== lefts.length ? "none" : "";
            // Duolingo-style instant feedback: color the pair green/red as soon as it's made, rather than waiting
            // for the final Check. Read-only server check (CheckMatchPairAsync) — doesn't score or advance the attempt.
            api(`/api/quiz/${encodeURIComponent(question.id)}/match/pair`, { json: { left: leftText, right: rightText } })
                .then((res) => {
                    const cls = res.correct ? ["border-green-500", "bg-green-500/10"] : ["border-red-500", "bg-red-500/10"];
                    leftEl.classList.add(...cls);
                    el.classList.add(...cls);
                })
                .catch(() => {
                    leftEl.classList.add("opacity-40");
                    el.classList.add("opacity-40");
                });
        }));
        checkBtn.addEventListener("click", () => {
            checkBtn.disabled = true;
            submitDrill("match", { pairs }, () => { checkBtn.disabled = false; });
        });
    }

    // MultiSelect ("select all that apply", task #96): a toggle per option instead of one tap, so the Check button
    // only appears once at least one is picked — unlike Reorder/Match there's no fixed count to wait for.
    function wireMultiSelect() {
        const optionEls = [...stage.querySelectorAll("[data-ms-index]")];
        const checkBtn = $("qz-multiselect-check");
        const picked = new Set();
        optionEls.forEach((el) => el.addEventListener("click", () => {
            if (el.disabled) return;
            const i = +el.dataset.msIndex;
            const on = !picked.has(i);
            if (on) picked.add(i); else picked.delete(i);
            el.setAttribute("aria-pressed", String(on));
            el.classList.toggle("border-primary", on);
            el.classList.toggle("bg-primary/10", on);
            el.querySelector("[data-ms-check]").textContent = on ? "✓" : "";
            checkBtn.style.display = picked.size === 0 ? "none" : "";
        }));
        checkBtn.addEventListener("click", () => {
            checkBtn.disabled = true;
            optionEls.forEach((el) => { el.disabled = true; });
            submitDrill("multiselect", { indices: [...picked] }, () => { checkBtn.disabled = false; optionEls.forEach((el) => { el.disabled = false; }); });
        });
    }

    function summary(s) {
        setProgress(1, 1);
        question = null;
        const stars = [1, 2, 3].map((i) => `<span class="${i <= s.stars ? "" : "grayscale opacity-30"}">⭐</span>`).join("");
        const placed = s.placed ? `<div class="mt-5 p-4 rounded-2xl tone-lavender text-left">
                <p class="text-sm text-foreground/70">${esc(t("YourLevelIs"))}</p><p class="text-xl font-semibold text-foreground" data-testid="placed-level">${esc(s.placed.name)}</p>
                <a class="${primary} mt-3" href="/Student/Quiz?area=${encodeURIComponent(area)}&level=${encodeURIComponent(s.placed.level)}">${esc(t("PractiseThisLevel"))}</a></div>` : "";
        const adventure = s.won === true || s.won === false ? `<div class="mt-4 p-4 rounded-2xl ${s.won ? "tone-sun" : "tone-sky"}" data-testid="adventure-end">
                <p class="text-4xl" aria-hidden="true">${s.won ? "🏆" : "🌙"}</p>
                <p class="mt-2 text-lg font-semibold text-foreground">${esc(t(s.won ? "AdventureWon" : "AdventureLost"))}</p>
                <p class="mt-1 text-sm text-foreground/80">${esc(s.ending)}</p></div>
            <form class="mt-4 p-4 rounded-2xl border border-layer-line bg-layer text-left space-y-3" id="qz-rate" data-testid="adventure-rating">
                <p class="text-sm font-semibold text-foreground">📝 ${esc(t("RateTitle"))}</p>
                <fieldset><legend class="text-xs text-muted-foreground-1">${esc(t("RateStars"))}</legend>
                    <div class="mt-1 flex gap-1">${[1, 2, 3, 4, 5].map((n) => `<label class="cursor-pointer text-2xl has-checked:scale-110"><input type="radio" name="stars" value="${n}" class="sr-only peer" data-testid="rate-star-${n}"><span class="grayscale opacity-40 peer-checked:grayscale-0 peer-checked:opacity-100" aria-label="${n}">⭐</span></label>`).join("")}</div></fieldset>
                <fieldset><legend class="text-xs text-muted-foreground-1">${esc(t("RateFelt"))}</legend>
                    <div class="mt-1 flex flex-wrap gap-2">${["TooEasy", "JustRight", "TooHard"].map((f) => `<label class="cursor-pointer"><input type="radio" name="felt" value="${f}" class="sr-only peer" ${f === "JustRight" ? "checked" : ""}><span class="inline-block py-1 px-3 rounded-full text-xs font-semibold border border-layer-line peer-checked:bg-primary peer-checked:text-primary-foreground peer-checked:border-primary">${esc(t("Felt_" + f))}</span></label>`).join("")}</div></fieldset>
                <fieldset><legend class="text-xs text-muted-foreground-1">${esc(t("RateFun"))}</legend>
                    <div class="mt-1 flex flex-wrap gap-2">${[["true", "😄"], ["false", "😐"]].map(([v, e]) => `<label class="cursor-pointer"><input type="radio" name="fun" value="${v}" class="sr-only peer" ${v === "true" ? "checked" : ""}><span class="inline-block py-1 px-3 rounded-full text-xs font-semibold border border-layer-line peer-checked:bg-primary peer-checked:text-primary-foreground peer-checked:border-primary">${e} ${esc(t(v === "true" ? "Yes" : "No"))}</span></label>`).join("")}</div></fieldset>
                <div class="flex flex-wrap items-center gap-3"><button type="submit" class="${primary}" data-testid="rate-send">${esc(t("RateSend"))}</button>
                    <p class="text-sm text-foreground" role="status" aria-live="polite" id="qz-rate-status"></p></div>
            </form>` : "";
        const duelText = s.duel === null || s.duel === undefined ? (mode === "Duel" ? `<p class="mt-4 text-sm text-muted-foreground-1">${esc(t("DuelWaiting"))}</p>` : "")
            : `<p class="mt-4 text-lg font-semibold text-foreground">${esc(t(s.duel > 0 ? "DuelWon" : s.duel < 0 ? "DuelLost" : "DuelDraw"))}</p>`;
        const topics = s.topics.length ? `<ul class="mt-5 space-y-2 text-left">${s.topics.map((x) => {
            const p = Math.round(100 * x.correct / x.total);
            return `<li><div class="flex justify-between text-sm"><span class="text-foreground">${esc(x.name)}</span><span class="text-muted-foreground-1">${x.correct}/${x.total}</span></div>
                <div class="mt-1 h-1.5 rounded-full bg-surface overflow-hidden"><div class="h-full rounded-full ${p >= 70 ? "bg-emerald-500" : p >= 40 ? "bg-amber-500" : "bg-rose-500"}" style="width:${p}%"></div></div></li>`;
        }).join("")}</ul>` : "";
        const sure = bets.filter((b) => b.sure), unsure = bets.filter((b) => !b.sure);
        const betLines = [];
        if (sure.length) betLines.push("😎 " + fmt(t("BetSure"), sure.filter((b) => b.correct).length, sure.length));
        if (unsure.length) betLines.push("🤔 " + fmt(t("BetUnsure"), unsure.filter((b) => b.correct).length, unsure.length));
        const betBlock = betLines.length ? `<div class="mt-4 p-3 rounded-2xl tone-lavender text-left text-sm text-foreground" data-testid="quiz-bets">${betLines.map((x) => `<p>${esc(x)}</p>`).join("")}
            <a class="mt-1 inline-block text-xs text-primary hover:underline" href="/Student/Learner#lr-map-title">${esc(t("BetMap"))} →</a></div>` : "";
        bets = [];
        stage.innerHTML = `<div class="text-center" data-testid="quiz-summary">
            <p class="text-4xl tracking-widest" aria-label="${esc(fmt(t("StarsN"), s.stars))}">${stars}</p>
            ${mode === "Review" ? `<p class="mt-2 text-sm font-medium text-primary">⚡ ${esc(t("ReviewDone"))}</p>` : mode === "Notebook" || mode === "Evo" ? `<p class="mt-2 text-sm font-medium text-primary">🔎 ${esc(fmt(t("EvoDone"), s.correct))}</p>` : ""}
            <h2 class="mt-3 text-2xl font-semibold text-foreground">${esc(fmt(t("ScoreOf"), s.correct, s.length))}</h2>
            <p class="mt-1 text-sm text-muted-foreground-1">${esc(fmt(t("XpWon"), xp))}</p>
            ${adventure}${duelText}${placed}${topics}${betBlock}
            <div class="mt-6 flex flex-wrap justify-center gap-2">
                ${mode === "Duel" ? `<a class="${primary}" href="/Student/Arena">${esc(t("BackToArena"))}</a>` : mode === "Focus" ? `<a class="${primary}" href="/Student/Plan" data-testid="focus-back">${esc(t("PlanBack"))}</a>` : mode === "Interview" ? `<a class="${primary}" href="/Student/Interview">${esc(t("OtherInterviews"))}</a>` : mode === "Adventure" ? `<a class="${primary}" href="/Student/Adventures">${esc(t("NewAdventure"))}</a>` : mode === "Review" ? `<a class="${primary}" href="/Student">${esc(t("Dashboard"))}</a>` : `<button type="button" class="${primary}" id="qz-again">${esc(t("Again"))}</button>`}
                ${["Practice", "Review", "Focus", "Adventure"].includes(mode) && s.correct > 0 ? `<a class="${light}" href="/Student/Defend?attempt=${encodeURIComponent(attemptId)}" data-testid="quiz-defend">🛡️ ${esc(t("DefendOpen"))}</a>` : ""}
                <a class="${light}" href="/Student/History?id=${encodeURIComponent(attemptId)}" data-testid="quiz-review">${esc(t("Review"))}</a>
                <a class="${light}" href="/Student/Explore">${esc(t("OtherSubjects"))}</a>
            </div></div>`;
        $("qz-rate")?.addEventListener("submit", async (e) => {
            e.preventDefault();
            const f = e.currentTarget;
            const stars = +(f.querySelector("input[name=stars]:checked")?.value || 0);
            const out = $("qz-rate-status");
            if (!stars) { out.textContent = t("RateNeedStars"); return; }
            try {
                await api(`/api/adventure-rating/${encodeURIComponent(attemptId)}`, { json: { stars, felt: f.felt.value, fun: f.fun.value === "true" } });
                f.innerHTML = `<p class="text-sm text-foreground" data-testid="rate-thanks">🙏 ${esc(t("RateThanks"))} <a class="font-semibold text-primary underline" href="/Student/Adventures#ideas">💡 ${esc(t("RateProposeNext"))}</a></p>`;
            } catch (err) { out.textContent = err.message; }
        });
        $("qz-again")?.addEventListener("click", () => { xp = 0; $("qz-xp").textContent = "0"; $("qz-run").textContent = "0"; start(); });
        say(s.ending || fmt(t("ScoreOf"), s.correct, s.length));
    }

    function showError(e) {
        const needsAi = e?.data?.code === "needs_ai";
        const nothing = e?.data?.code === "nothing_to_review";
        const box = document.createElement("div");
        box.className = "mt-4 p-3 rounded-xl text-sm bg-red-50 text-red-800 dark:bg-red-500/15 dark:text-red-300";
        box.setAttribute("role", "alert");
        box.textContent = needsAi ? t("QuizNeedsAi") : nothing ? t("NothingToReview") : e.message;
        if (nothing) box.className = "mt-4 p-3 rounded-xl text-sm tone-mint text-foreground";
        stage.querySelector("[role=alert]")?.remove();
        stage.appendChild(box);
    }

    async function start() {
        stage.innerHTML = `<div class="py-10 text-center text-sm text-muted-foreground-1"><span class="animate-spin inline-block size-5 border-2 border-current border-t-transparent rounded-full" aria-hidden="true"></span> ${esc(t("Generating"))}</div>`;
        try {
            const body = { area, level, mode: mode === "Topic" ? "Practice" : mode, topic: topic || null, duelId: duel ? +duel : null, figure: figure || null };
            const url = root.dataset.material ? `/api/materials/${encodeURIComponent(root.dataset.material)}/quiz` : mode === "Topic" ? "/api/quiz/topic" : mode === "Adventure" ? "/api/quiz/adventure" : "/api/quiz/start";
            const res = await api(url, { json: mode === "Adventure" ? { area, level, theme } : body });
            render(res.question);
            if (res.fallback) {
                const n = document.createElement("p");
                n.className = "mt-3 text-xs text-amber-700 dark:text-amber-300";
                n.textContent = fmt(t("Fallback"), res.fallback);
                stage.appendChild(n);
            }
        } catch (e) {
            stage.innerHTML = `<div class="py-6 text-center"><p class="text-4xl" aria-hidden="true">🦉</p></div>`;
            showError(e);
            stage.insertAdjacentHTML("beforeend", `<div class="mt-4 flex justify-center"><a class="${light}" href="/Student/Explore">${esc(t("OtherSubjects"))}</a></div>`);
        }
    }

    document.addEventListener("keydown", (e) => {
        if (!question || e.target.closest("input,textarea,select") || e.ctrlKey || e.metaKey || e.altKey) return;
        const i = "1234".indexOf(e.key) >= 0 ? "1234".indexOf(e.key) : "abcd".indexOf(e.key.toLowerCase());
        if (i >= 0 && e.key.length === 1) stage.querySelector(`[data-index="${i}"]`)?.click();
    });

    start();
})();
