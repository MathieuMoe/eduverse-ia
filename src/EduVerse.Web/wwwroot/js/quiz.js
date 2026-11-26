// Test runner of the student platform: practice, placement, duels and AI topics.
// The server keeps the answer key; the browser only gets it after answering. Young children get big buttons and the question read aloud.
(() => {
    "use strict";
    const { t, fmt, esc, api, lang } = window.Edu;
    const $ = (id) => document.getElementById(id);
    const root = $("qz-root");
    const stage = $("qz-stage");
    const { area, level, mode, topic, duel, theme, figure, unit } = root.dataset;
    const pathUrl = root.dataset.path || "";
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

    // Sound effects (Duolingo-style): tiny tones synthesized with the Web Audio API, so there are no audio files to
    // load. The AudioContext is only created on the first sound, which always follows a click (browsers refuse to
    // start one before a user gesture). The student's choice is saved on the server (/api/game/sounds).
    let sounds = root.dataset.sounds !== "0";
    let audio = null;
    function tone(freq, start, length, type = "sine", gain = 0.12) {
        const osc = audio.createOscillator();
        const vol = audio.createGain();
        osc.type = type;
        osc.frequency.setValueAtTime(freq, audio.currentTime + start);
        vol.gain.setValueAtTime(0.0001, audio.currentTime + start);
        vol.gain.exponentialRampToValueAtTime(gain, audio.currentTime + start + 0.02);
        vol.gain.exponentialRampToValueAtTime(0.0001, audio.currentTime + start + length);
        osc.connect(vol).connect(audio.destination);
        osc.start(audio.currentTime + start);
        osc.stop(audio.currentTime + start + length + 0.05);
    }
    const sfxNotes = {
        correct: [[660, 0, 0.12], [880, 0.1, 0.22]],
        wrong: [[220, 0, 0.18, "triangle"], [175, 0.14, 0.26, "triangle"]],
        streak: [[523, 0, 0.1], [659, 0.08, 0.1], [784, 0.16, 0.1], [1047, 0.24, 0.25]],
        complete: [[523, 0, 0.15], [659, 0.14, 0.15], [784, 0.28, 0.15], [1047, 0.42, 0.4]],
        goal: [[784, 0, 0.12], [988, 0.1, 0.12], [1175, 0.2, 0.12], [1568, 0.3, 0.45]],
    };
    function sfx(name) {
        if (!sounds || !sfxNotes[name]) return;
        try {
            audio = audio || new (window.AudioContext || window.webkitAudioContext)();
            if (audio.state === "suspended") audio.resume();
            for (const [f, st, len, type] of sfxNotes[name]) tone(f, st, len, type);
        } catch { /* no audio device: stay silent */ }
    }
    $("qz-sound")?.addEventListener("click", async (e) => {
        sounds = !sounds;
        const b = e.currentTarget;
        b.setAttribute("aria-pressed", String(sounds));
        b.textContent = sounds ? "🔔" : "🔕";
        if (sounds) sfx("correct");
        try { await api("/api/game/sounds", { json: { on: sounds } }); } catch { /* the toggle still works for this page */ }
    });

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
        const items = [...(reward?.badges || []).map((b) => `${b.icon} ${esc(fmt(t("NewBadge"), b.name))}`), ...(reward?.quests || []).map((q) => `🎯 ${esc(fmt(t("QuestDone"), q.name))}`),
            ...(reward?.gems ? [`💎 ${esc(fmt(t("GemsWon"), reward.gems))}`] : [])];
        const el = $("qz-toast");
        if (!items.length) return;
        el.innerHTML = items.join("<br>");
        el.classList.remove("hidden");
        setTimeout(() => el.classList.add("hidden"), 6000);
    }

    function addXp(amount, boosted) {
        if (!amount) return;
        xp += amount;
        $("qz-xp").textContent = String(xp);
        const pop = document.createElement("span");
        pop.className = "xp-pop absolute top-16 right-6 text-lg font-bold text-primary pointer-events-none";
        pop.textContent = `+${amount} XP${boosted ? " ⚡×2" : ""}`;
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
        const villain = a.unitBoss ? `<li class="size-11 rounded-2xl grid place-items-center text-2xl bg-white dark:bg-white/20 ring-2 ring-rose-400 qz-streak-pop" aria-label="${esc(a.boss)}">🐲</li>` : "";
        const hearts = "❤️".repeat(Math.max(0, a.hearts)) + "🤍".repeat(Math.max(0, a.maxHearts - a.hearts));
        return `<div class="rounded-3xl tone-${a.inBoss ? "pink" : "sky"} p-4 sm:p-5" data-testid="adventure-map">
            <div class="flex flex-wrap items-center justify-between gap-3">
                ${a.unitBoss ? `<ol class="flex items-center gap-2" data-testid="unit-boss">${villain}<li class="text-sm font-semibold text-foreground">${esc(a.boss)}${a.legendary ? ` <span class="ms-1 text-xs py-0.5 px-2 rounded-full tone-sun" data-testid="legendary-badge">👑 ${esc(t("Legendary"))}</span>` : ""}</li></ol>` : `<ol class="flex items-center gap-1 sm:gap-1.5" aria-label="${esc(t("Map"))}">${doors}<li class="h-0.5 w-3 sm:w-5 bg-foreground/20 rounded" aria-hidden="true"></li>
                    <li class="size-11 rounded-2xl grid place-items-center text-2xl ${a.inBoss ? "bg-white dark:bg-white/20 ring-2 ring-rose-400" : "bg-white/60 dark:bg-white/10"}" aria-label="${esc(a.boss)}">🐲</li></ol>`}
                <span class="flex items-center gap-2"><span class="text-lg tracking-wider" aria-label="${esc(fmt(t("HeartsN"), a.hearts))}" data-testid="hearts" id="qz-hearts">${hearts}</span>
                ${a.hearts < a.maxHearts ? `<button type="button" id="qz-potion" class="py-1 px-2.5 rounded-full text-xs font-semibold bg-white/80 dark:bg-white/15 text-foreground hover:bg-white tip tip-b" data-tip="${esc(t("PotionTip"))}" data-max="${a.maxHearts}" data-testid="adventure-potion">🧪 +1 ❤️ · 💎 ${esc(root.dataset.potionPrice || "")}</button>` : ""}</span>
            </div>
            <p class="hidden mt-2 text-xs font-medium text-foreground" role="status" id="qz-potion-status"></p>
            ${a.inBoss ? `<div class="mt-3"><div class="flex justify-between text-xs text-foreground/70"><span>${esc(a.boss)}</span><span>${a.bossHp}/${a.bossMax}</span></div>
                <div class="mt-1 h-2.5 rounded-full bg-white/70 dark:bg-white/10 overflow-hidden"><div class="h-full rounded-full bg-rose-500 transition-all" style="width:${hp}%"></div></div></div>` : ""}
            ${a.intro ? `<p class="mt-3 text-sm font-medium text-foreground">${esc(a.intro)}</p>` : ""}
            <p class="mt-2 text-sm text-foreground/80 italic" data-testid="story-line">${esc(a.line)}</p>
        </div>`;
    }

    // Potion (phase 2): gems buy one heart back during an adventure; the server checks the price and the maximum.
    stage.addEventListener("click", async (e) => {
        const b = e.target.closest("#qz-potion");
        if (!b || !attemptId) return;
        b.disabled = true;
        const out = $("qz-potion-status");
        try {
            const r = await api(`/api/quiz/${encodeURIComponent(attemptId)}/potion`, { json: {} });
            const max = +b.dataset.max;
            $("qz-hearts").textContent = "❤️".repeat(r.hearts) + "🤍".repeat(Math.max(0, max - r.hearts));
            $("qz-hearts").setAttribute("aria-label", fmt(t("HeartsN"), r.hearts));
            pulse($("qz-hearts"), "qz-streak-pop", 500);
            sfx("streak");
            if (r.hearts >= max) b.remove(); else b.disabled = false;
            out.textContent = fmt(t("PotionDrunk"), r.gems);
        } catch (err) {
            out.textContent = err.data?.code === "potion_notenoughgems" ? t("PotionNoGems") : err.message;
            b.disabled = false;
        }
        out.classList.remove("hidden");
    });

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
        addXp(res.reward?.xp, res.reward?.boosted);
        toast(res.reward);
        if (res.correct) {
            pulse($("qz-run").parentElement, "qz-streak-pop", 500);
            const milestone = (res.run || 0) > 0 && res.run % 5 === 0;
            if (milestone) confettiBurst(stage);
            sfx(milestone ? "streak" : "correct");
        } else {
            pulse(stage, "qz-shake", 400);
            sfx("wrong");
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
            ${res.correct || isDrill || !answered.number ? "" : `<div class="mt-2"><button type="button" class="py-1.5 px-3 rounded-full text-xs font-semibold bg-white/70 dark:bg-white/10 text-foreground hover:bg-white" id="qz-trick" data-number="${answered.number}" data-testid="quiz-trick">💡 ${esc(t("TrickAsk"))}</button>
                <div class="mt-2 hidden p-3 rounded-xl bg-white/70 dark:bg-white/10 text-sm text-foreground" id="qz-trick-out" aria-live="polite" data-testid="quiz-trick-text"></div></div>`}
            ${res.correct || !Object.keys(reasons).length || isDrill ? "" : thinkingBlock(answered.number)}
            <div class="mt-3 flex justify-end"><button type="button" class="${primary}" id="qz-next" data-testid="quiz-next">${esc(res.summary ? t("SeeResults") : t("Next"))} →</button></div>`;
        wireThinking(answered.id);
        // "Give me a trick" (phase 5): a memory hook for the question just missed (AI, or a technique offline)
        $("qz-trick")?.addEventListener("click", async (e) => {
            const b = e.currentTarget;
            const out = $("qz-trick-out");
            b.disabled = true;
            out.classList.remove("hidden");
            out.textContent = t("TrickWriting");
            try {
                const r = await api(`/api/quiz/${encodeURIComponent(attemptId)}/trick/${b.dataset.number}`, { method: "POST" });
                out.innerHTML = `<p class="text-xs font-semibold text-foreground/70">💡 ${esc(t("TrickTitle"))}</p><p class="mt-1">${esc(r.trick)}</p>`;
                b.remove();
            } catch (err) { out.textContent = err.message; b.disabled = false; }
        });
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
            ${unit ? "" : `<form class="mt-4 p-4 rounded-2xl border border-layer-line bg-layer text-left space-y-3" id="qz-rate" data-testid="adventure-rating">
                <p class="text-sm font-semibold text-foreground">📝 ${esc(t("RateTitle"))}</p>
                <fieldset><legend class="text-xs text-muted-foreground-1">${esc(t("RateStars"))}</legend>
                    <div class="mt-1 flex gap-1">${[1, 2, 3, 4, 5].map((n) => `<label class="cursor-pointer text-2xl has-checked:scale-110"><input type="radio" name="stars" value="${n}" class="sr-only peer" data-testid="rate-star-${n}"><span class="grayscale opacity-40 peer-checked:grayscale-0 peer-checked:opacity-100" aria-label="${n}">⭐</span></label>`).join("")}</div></fieldset>
                <fieldset><legend class="text-xs text-muted-foreground-1">${esc(t("RateFelt"))}</legend>
                    <div class="mt-1 flex flex-wrap gap-2">${["TooEasy", "JustRight", "TooHard"].map((f) => `<label class="cursor-pointer"><input type="radio" name="felt" value="${f}" class="sr-only peer" ${f === "JustRight" ? "checked" : ""}><span class="inline-block py-1 px-3 rounded-full text-xs font-semibold border border-layer-line peer-checked:bg-primary peer-checked:text-primary-foreground peer-checked:border-primary">${esc(t("Felt_" + f))}</span></label>`).join("")}</div></fieldset>
                <fieldset><legend class="text-xs text-muted-foreground-1">${esc(t("RateFun"))}</legend>
                    <div class="mt-1 flex flex-wrap gap-2">${[["true", "😄"], ["false", "😐"]].map(([v, e]) => `<label class="cursor-pointer"><input type="radio" name="fun" value="${v}" class="sr-only peer" ${v === "true" ? "checked" : ""}><span class="inline-block py-1 px-3 rounded-full text-xs font-semibold border border-layer-line peer-checked:bg-primary peer-checked:text-primary-foreground peer-checked:border-primary">${e} ${esc(t(v === "true" ? "Yes" : "No"))}</span></label>`).join("")}</div></fieldset>
                <div class="flex flex-wrap items-center gap-3"><button type="submit" class="${primary}" data-testid="rate-send">${esc(t("RateSend"))}</button>
                    <p class="text-sm text-foreground" role="status" aria-live="polite" id="qz-rate-status"></p></div>
            </form>`}` : "";
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
            <div id="qz-lesson-end" data-testid="lesson-end" aria-live="polite"></div>
            <p class="text-4xl tracking-widest" aria-label="${esc(fmt(t("StarsN"), s.stars))}">${stars}</p>
            ${mode === "Review" ? `<p class="mt-2 text-sm font-medium text-primary">⚡ ${esc(t("ReviewDone"))}</p>` : mode === "Notebook" || mode === "Evo" ? `<p class="mt-2 text-sm font-medium text-primary">🔎 ${esc(fmt(t("EvoDone"), s.correct))}</p>` : ""}
            <h2 class="mt-3 text-2xl font-semibold text-foreground">${esc(fmt(t("ScoreOf"), s.correct, s.length))}</h2>
            <p class="mt-1 text-sm text-muted-foreground-1">${esc(fmt(t("XpWon"), xp))}</p>
            ${adventure}${duelText}${placed}${topics}${betBlock}
            <div class="mt-6 flex flex-wrap justify-center gap-2">
                ${pathUrl ? `<a class="${primary}" href="${esc(pathUrl)}" data-testid="path-back">🧭 ${esc(t("PathContinue"))}</a>` : mode === "Duel" ? `<a class="${primary}" href="/Student/Arena">${esc(t("BackToArena"))}</a>` : mode === "Focus" ? `<a class="${primary}" href="/Student/Plan" data-testid="focus-back">${esc(t("PlanBack"))}</a>` : mode === "Interview" ? `<a class="${primary}" href="/Student/Interview">${esc(t("OtherInterviews"))}</a>` : mode === "Adventure" ? `<a class="${primary}" href="/Student/Adventures">${esc(t("NewAdventure"))}</a>` : mode === "Review" ? `<a class="${primary}" href="/Student">${esc(t("Dashboard"))}</a>` : `<button type="button" class="${primary}" id="qz-again">${esc(t("Again"))}</button>`}
                ${["Practice", "Review", "Focus", "Adventure"].includes(mode) && s.defendable ? `<a class="${light}" href="/Student/Defend?attempt=${encodeURIComponent(attemptId)}" data-testid="quiz-defend">🛡️ ${esc(t("DefendOpen"))}</a>` : ""}
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
        lessonEnd();
    }

    // Counts a number up inside an element (the stat tiles of the end-of-lesson screen).
    function countUp(el, to, suffix = "", ms = 700) {
        if (!el) return;
        const reduce = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
        if (reduce || to <= 0) { el.textContent = to + suffix; return; }
        const t0 = performance.now();
        const step = (now) => {
            const k = Math.min(1, (now - t0) / ms);
            el.textContent = Math.round(to * (1 - Math.pow(1 - k, 3))) + suffix;
            if (k < 1) requestAnimationFrame(step);
        };
        requestAnimationFrame(step);
    }

    // End-of-lesson screens: XP, accuracy and time tiles, the streak kept alive today, the daily goal ring and the
    // day's quests. Everything is read from the server; a failure just leaves the classic summary below.
    async function lessonEnd() {
        const box = $("qz-lesson-end");
        if (!box || !attemptId) return;
        let e;
        try { e = await api(`/api/game/lesson-end/${encodeURIComponent(attemptId)}`); } catch { return; }
        if (!box.isConnected) return;
        const time = `${Math.floor(e.seconds / 60)}:${String(e.seconds % 60).padStart(2, "0")}`;
        const tile = (tone, icon, label, id, delay) => `<div class="le-rise p-3 rounded-2xl ${tone}" style="animation-delay:${delay}ms">
                <p class="text-xs font-semibold uppercase tracking-wide text-foreground/70"><span aria-hidden="true">${icon}</span> ${esc(label)}</p>
                <p class="mt-1 text-2xl font-bold text-foreground" id="${id}">0</p></div>`;
        const streak = e.milestone ? `<div class="le-rise mt-3 p-5 rounded-2xl tone-sun text-center" style="animation-delay:420ms" data-testid="lesson-milestone">
                <p class="le-flame text-6xl" aria-hidden="true">🔥</p>
                <p class="mt-2 text-2xl font-bold text-foreground">${esc(fmt(t("MilestoneTitle"), e.streak))}</p>
                <p class="mt-1 text-sm text-foreground/75">${esc(fmt(t("MilestoneText"), e.milestoneGems))}</p></div>` : e.streakExtended ? `<div class="le-rise mt-3 p-4 rounded-2xl tone-peach flex items-center justify-center gap-3" style="animation-delay:420ms" data-testid="lesson-streak">
                <span class="le-flame text-4xl" aria-hidden="true">🔥</span>
                <div class="text-left"><p class="text-lg font-semibold text-foreground">${esc(fmt(t("LessonStreak"), e.streak))}</p>
                <p class="text-xs text-foreground/70">${esc(t("LessonStreakSub"))}</p></div></div>` : "";
        const quests = (e.quests || []).map((q) => `<li class="flex items-center gap-2 text-sm"><span aria-hidden="true">${q.done ? "✅" : "🎯"}</span>
                <span class="grow text-left text-foreground">${esc(q.text)}</span>
                <span class="text-xs text-muted-foreground-1">${q.progress}/${q.target}</span></li>`).join("");
        box.innerHTML = `<div class="mb-6 p-4 sm:p-5 rounded-3xl border border-layer-line bg-layer">
            <p class="le-rise text-lg font-semibold text-foreground">🎉 ${esc(t("LessonComplete"))}${e.happyHour ? ` <span class="ms-1 align-middle text-xs py-0.5 px-2 rounded-full tone-mint">⚡ ${esc(t("HappyHourOn"))}</span>` : ""}${e.gems ? ` <a href="/Student/Shop" class="gem-pop ms-1 align-middle text-xs py-0.5 px-2 rounded-full tone-sky hover:underline" style="animation-delay:700ms" data-testid="lesson-gems">💎 +${e.gems}</a>` : ""}</p>
            <div class="mt-3 grid grid-cols-3 gap-2 sm:gap-3">
                ${tile("tone-sun", "⭐", t("LessonXp"), "le-xp", 80)}
                ${tile("tone-mint", "🎯", t("LessonAccuracy"), "le-acc", 180)}
                ${tile("tone-sky", "⏱️", t("LessonTime"), "le-time", 280)}
            </div>
            ${streak}
            <div class="le-rise mt-3 p-4 rounded-2xl ${e.goalReached ? "tone-mint" : "bg-surface"} flex items-center gap-4" style="animation-delay:560ms" data-testid="lesson-goal" data-reached="${e.goalReached ? "1" : "0"}">
                <div class="xp-ring size-14 shrink-0 rounded-full grid place-items-center" id="le-ring" style="--p:0" role="img" aria-label="${esc(fmt(t("GoalOf"), e.todayXp, e.goal))}">
                    <span class="size-11 rounded-full bg-white/90 dark:bg-[#262236] grid place-items-center text-xs font-bold text-foreground">${e.goalPercent}%</span></div>
                <div class="text-left min-w-0"><p class="text-sm font-semibold text-foreground">${esc(e.goalJustReached ? t("GoalJustReached") : e.goalReached ? t("GoalReached") : t("GoalToday"))}</p>
                <p class="text-xs text-foreground/70">${esc(fmt(t("GoalOf"), e.todayXp, e.goal))}</p></div></div>
            ${e.league ? `<a href="/Student/League" class="le-rise mt-3 flex items-center gap-3 p-3 rounded-2xl ${e.league.zone === "Promoted" ? "tone-mint" : e.league.zone === "Demoted" ? "tone-pink" : "bg-surface"} hover:opacity-90" style="animation-delay:620ms" data-testid="lesson-league">
                <span class="text-2xl" aria-hidden="true">${esc(e.league.emoji)}</span>
                <span class="grow text-left text-sm text-foreground">${esc(fmt(t("LeaguePlace"), e.league.name, e.league.position, e.league.size))}</span>
                <span class="text-xs font-semibold text-foreground/70">${e.league.zone === "Promoted" ? "▲" : e.league.zone === "Demoted" ? "▼" : ""}</span></a>` : ""}
            ${quests ? `<ul class="le-rise mt-3 space-y-1.5" style="animation-delay:680ms" data-testid="lesson-quests">${quests}</ul>` : ""}
        </div>`;
        countUp($("le-xp"), e.xp);
        countUp($("le-acc"), e.accuracy, "%");
        $("le-time").textContent = time;
        const ring = $("le-ring");
        const reduce = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
        if (reduce) ring.style.setProperty("--p", e.goalPercent);
        else {
            const t0 = performance.now();
            const grow = (now) => {
                const k = Math.min(1, (now - t0) / 900);
                ring.style.setProperty("--p", Math.round(e.goalPercent * k));
                if (k < 1) requestAnimationFrame(grow);
            };
            setTimeout(() => requestAnimationFrame(grow), 560);
        }
        sfx("complete");
        if (e.goalJustReached || e.milestone) setTimeout(() => { confettiBurst(box); sfx("goal"); }, 900);
        if (e.milestone) setTimeout(() => confettiBurst(box), 1500);
        mascotSays(box.firstElementChild);
    }

    // The Tutor reacts to the lesson (phase 5): the mood is decided on the server, the words come from the AI or a
    // line written in advance. A failure just leaves the screen without the bubble.
    async function mascotSays(panel) {
        if (!panel) return;
        let m;
        try { m = await api(`/api/game/mascot/${encodeURIComponent(attemptId)}`, { method: "POST" }); } catch { return; }
        if (!panel.isConnected) return;
        panel.insertAdjacentHTML("afterbegin", `<div class="le-rise mb-3 flex items-start gap-3 text-left" data-testid="lesson-mascot" data-mood="${esc(m.mood)}">
            <span class="size-11 shrink-0 rounded-full tone-sun grid place-items-center text-2xl" aria-hidden="true">🦉</span>
            <p class="relative py-2 px-3.5 rounded-2xl rounded-tl-sm tone-sun text-sm font-medium text-foreground">${esc(m.emoji)} ${esc(m.line)}</p></div>`);
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
            const body = { area, level, mode: mode === "Topic" ? "Practice" : mode, topic: topic || null, duelId: duel ? +duel : null, figure: figure || null, path: !!pathUrl };
            const url = root.dataset.material ? `/api/materials/${encodeURIComponent(root.dataset.material)}/quiz` : mode === "Topic" ? "/api/quiz/topic" : unit ? "/api/quiz/boss" : mode === "Adventure" ? "/api/quiz/adventure" : "/api/quiz/start";
            const res = await api(url, { json: unit ? { area, level, topic: unit, legendary: root.dataset.legend === "1" } : mode === "Adventure" ? { area, level, theme } : body });
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
