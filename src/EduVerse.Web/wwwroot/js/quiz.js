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
    let question = null;
    let xp = 0;
    let speak = kids;
    let attemptId = null;
    let confidence = 1;
    let hintsUsed = 0;

    const btn = "py-2.5 px-5 inline-flex justify-center items-center gap-x-2 text-sm font-semibold rounded-full focus:outline-hidden focus-visible:ring-2 focus-visible:ring-primary/30 disabled:opacity-50";
    const primary = `${btn} bg-primary text-primary-foreground hover:bg-primary-hover`;
    const light = `${btn} border border-layer-line bg-layer text-foreground hover:bg-layer-hover`;
    const voices = { en: "en-GB", es: "es-ES", fr: "fr-FR", ca: "ca-ES" };

    function say(text) {
        if (!speak || !("speechSynthesis" in window)) return;
        window.speechSynthesis.cancel();
        const u = new SpeechSynthesisUtterance(text);
        u.lang = voices[lang.slice(0, 2)] || "en-GB";
        u.rate = kids ? 0.9 : 1;
        window.speechSynthesis.speak(u);
    }

    speakButton.addEventListener("click", () => {
        speak = !speak;
        speakButton.setAttribute("aria-pressed", String(speak));
        if (speak && question) say(question.prompt);
        else window.speechSynthesis?.cancel();
    });

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
                <span class="inline-flex items-center gap-2"><span class="inline-flex items-center py-0.5 px-2 rounded-full bg-surface text-foreground">${esc(q.topic)}</span><span class="inline-flex gap-1" title="${esc(t("Difficulty"))}" aria-label="${esc(fmt(t("DifficultyN"), q.difficulty))}">${dots}</span></span>
            </div>
            ${q.visual?.startsWith("dg:") ? `<div class="quiz-diagram mt-5 flex justify-center" data-testid="quiz-diagram">${window.Edu.diagram(q.visual)}</div>` : q.visual ? `<p class="quiz-visual mt-5 text-4xl text-center leading-snug" aria-hidden="true">${esc(q.visual)}</p>` : ""}
            <h2 class="quiz-prompt mt-4 text-xl font-semibold text-foreground text-center" id="qz-prompt">${esc(q.prompt)}</h2>
            ${q.code ? `<pre class="mt-4 p-4 rounded-2xl bg-[#2b2230] text-[#f3eefe] text-sm overflow-x-auto"><code>${esc(q.code)}</code></pre>` : ""}
            ${q.evo ? `<div class="mt-5 flex items-start gap-3 justify-center" data-testid="evo-answer">
                <span class="size-11 shrink-0 rounded-full tone-lavender grid place-items-center text-2xl" aria-hidden="true">🦉</span>
                <div class="text-left"><p class="inline-block py-2 px-3.5 rounded-2xl rounded-tl-sm tone-sun text-foreground"><span class="text-xs text-foreground/60 block">${esc(t("EvoAnswered"))}</span><span class="text-lg font-semibold">${esc(q.evo)}</span></p>
                <p class="mt-1.5 text-sm font-medium text-foreground">${esc(q.evoTask || "")}</p></div></div>` : ""}
            ${kids || q.mode === "Placement" ? "" : `<div class="mt-5 flex flex-wrap items-center justify-center gap-2 text-sm" role="radiogroup" aria-label="${esc(t("HowSure"))}" data-testid="confidence">
                <span class="text-muted-foreground-1 me-1">${esc(t("HowSure"))}</span>
                ${[[0, "🤔", t("Unsure")], [1, "🙂", t("ThinkSo")], [2, "😎", t("Sure")]].map(([v, e, label]) => `<button type="button" role="radio" aria-checked="${v === 1}" data-confidence="${v}" class="py-1 px-3 rounded-full border ${v === 1 ? "border-primary bg-primary/10" : "border-layer-line"} text-foreground">${e} ${esc(label)}</button>`).join("")}
            </div>`}
            <div class="mt-6 grid sm:grid-cols-2 gap-3" role="group" aria-labelledby="qz-prompt" data-testid="quiz-options">
                ${q.options.map((o, i) => `<button type="button" class="quiz-option flex items-center gap-3 w-full text-left p-4 rounded-2xl border border-layer-line bg-layer text-foreground text-base font-medium hover:bg-layer-hover disabled:cursor-default" data-index="${i}">
                    <span class="size-7 shrink-0 rounded-full bg-surface grid place-items-center text-xs font-bold" aria-hidden="true">${"ABCD"[i]}</span><span>${esc(o)}</span></button>`).join("")}
            </div>
            <div class="mt-4 flex flex-wrap items-center justify-between gap-2">
                <button type="button" id="qz-hint" class="py-1.5 px-3.5 inline-flex items-center gap-x-1.5 text-sm font-medium rounded-full border border-layer-line bg-layer text-foreground hover:bg-layer-hover disabled:opacity-50" data-testid="hint">💡 <span id="qz-hint-label"></span></button>
                ${window.Edu.voice?.supported ? `<button type="button" id="qz-mic" class="py-1.5 px-3.5 inline-flex items-center gap-x-1.5 text-sm font-medium rounded-full border border-layer-line bg-layer text-foreground hover:bg-layer-hover" aria-pressed="false" data-testid="mic">🎤 ${esc(t("SayAnswer"))}</button>` : ""}
                <span class="text-[11px] text-muted-foreground-1">${esc(t("HintCost"))}</span>
            </div>
            <p id="qz-heard" class="hidden mt-2 text-sm text-muted-foreground-1" role="status"></p>
            <p id="qz-hint-text" class="hidden mt-3 p-3 rounded-2xl tone-sky text-sm text-foreground" role="status"></p>
            <div id="qz-feedback" class="hidden mt-5"></div>`;
        stage.querySelectorAll("[data-index]").forEach((b) => b.addEventListener("click", () => answer(+b.dataset.index)));
        confidence = 1;
        hintsUsed = 0;
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
        $("qz-hint").addEventListener("click", hint);
        $("qz-mic")?.addEventListener("click", async () => {
            const mic = $("qz-mic");
            mic.setAttribute("aria-pressed", "true");
            mic.classList.add("animate-pulse");
            const heard = $("qz-heard");
            try {
                const said = await window.Edu.voice.listen();
                const open = q.options.map((o, i) => stage.querySelector(`[data-index="${i}"]`)?.dataset.state === "removed" ? "\u0000" : o);
                const i = window.Edu.voice.match(said, open);
                heard.textContent = i >= 0 ? fmt(t("Heard"), said[0]) : fmt(t("NotUnderstood"), said[0]);
                heard.classList.remove("hidden");
                if (i >= 0) answer(i);
            } catch (e) {
                if (e.message !== "no-speech") { heard.textContent = t("MicError"); heard.classList.remove("hidden"); }
            }
            mic.setAttribute("aria-pressed", "false");
            mic.classList.remove("animate-pulse");
        });
        stage.querySelector("[data-index]")?.focus();
        say((q.adventure ? (q.adventure.intro ? q.adventure.intro + " " : "") + q.adventure.line + " " : "") + q.prompt + (q.visual && !q.visual.startsWith("dg:") ? "" : ". " + q.options.join(", ")));
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

    async function answer(index) {
        const buttons = [...stage.querySelectorAll("[data-index]")];
        if (buttons.some((b) => b.disabled && b.dataset.state !== "removed") || buttons[index].dataset.state === "removed") return;
        buttons.forEach((b) => { b.disabled = true; });
        stage.querySelectorAll("[data-confidence]").forEach((b) => { b.disabled = true; });
        if ($("qz-hint")) $("qz-hint").disabled = true;
        let res;
        try {
            res = await api(`/api/quiz/${encodeURIComponent(question.id)}/answer`, { json: { choice: index, confidence } });
        } catch (e) {
            buttons.forEach((b) => { b.disabled = b.dataset.state === "removed"; });
            showError(e);
            return;
        }

        buttons[res.correctIndex].dataset.state = "right";
        if (!res.correct) buttons[index].dataset.state = "wrong";
        $("qz-run").textContent = String(res.run);
        addXp(res.reward?.xp);
        toast(res.reward);
        const fb = $("qz-feedback");
        fb.className = `mt-5 p-4 rounded-2xl ${res.correct ? "tone-mint" : "tone-pink"}`;
        const notes = [];
        if (res.confidence === 2 && res.correct) notes.push("😎 " + t("SureRight"));
        if (res.confidence === 2 && !res.correct) notes.push("🔁 " + t("SureWrong"));
        if (res.confidence === 0 && res.correct) notes.push("🤔 " + t("UnsureRight"));
        if (res.hints > 0 && res.correct) notes.push("💡 " + fmt(t("HintsUsed"), res.hints));
        fb.innerHTML = `<p class="font-semibold text-foreground">${res.correct ? "✅ " + esc(t(kids ? "KidsRight" : "Right")) : "💡 " + esc(t("NotQuite"))}</p>
            <p class="mt-1 text-sm text-foreground/80">${esc(res.explanation)}</p>
            ${notes.map((n) => `<p class="mt-1 text-xs text-foreground/70">${esc(n)}</p>`).join("")}
            <div class="mt-3 flex justify-end"><button type="button" class="${primary}" id="qz-next" data-testid="quiz-next">${esc(res.summary ? t("SeeResults") : t("Next"))} →</button></div>`;
        say(res.correct ? t(kids ? "KidsRight" : "Right") : t("NotQuite") + ". " + res.explanation);
        const next = $("qz-next");
        next.focus();
        next.addEventListener("click", () => res.summary ? summary(res.summary) : render(res.next));
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
                <p class="mt-1 text-sm text-foreground/80">${esc(s.ending)}</p></div>` : "";
        const duelText = s.duel === null || s.duel === undefined ? (mode === "Duel" ? `<p class="mt-4 text-sm text-muted-foreground-1">${esc(t("DuelWaiting"))}</p>` : "")
            : `<p class="mt-4 text-lg font-semibold text-foreground">${esc(t(s.duel > 0 ? "DuelWon" : s.duel < 0 ? "DuelLost" : "DuelDraw"))}</p>`;
        const topics = s.topics.length ? `<ul class="mt-5 space-y-2 text-left">${s.topics.map((x) => {
            const p = Math.round(100 * x.correct / x.total);
            return `<li><div class="flex justify-between text-sm"><span class="text-foreground">${esc(x.name)}</span><span class="text-muted-foreground-1">${x.correct}/${x.total}</span></div>
                <div class="mt-1 h-1.5 rounded-full bg-surface overflow-hidden"><div class="h-full rounded-full ${p >= 70 ? "bg-emerald-500" : p >= 40 ? "bg-amber-500" : "bg-rose-500"}" style="width:${p}%"></div></div></li>`;
        }).join("")}</ul>` : "";
        stage.innerHTML = `<div class="text-center" data-testid="quiz-summary">
            <p class="text-4xl tracking-widest" aria-label="${esc(fmt(t("StarsN"), s.stars))}">${stars}</p>
            ${mode === "Review" ? `<p class="mt-2 text-sm font-medium text-primary">⚡ ${esc(t("ReviewDone"))}</p>` : mode === "Notebook" || mode === "Evo" ? `<p class="mt-2 text-sm font-medium text-primary">🔎 ${esc(fmt(t("EvoDone"), s.correct))}</p>` : ""}
            <h2 class="mt-3 text-2xl font-semibold text-foreground">${esc(fmt(t("ScoreOf"), s.correct, s.length))}</h2>
            <p class="mt-1 text-sm text-muted-foreground-1">${esc(fmt(t("XpWon"), xp))}</p>
            ${adventure}${duelText}${placed}${topics}
            <div class="mt-6 flex flex-wrap justify-center gap-2">
                ${mode === "Duel" ? `<a class="${primary}" href="/Student/Arena">${esc(t("BackToArena"))}</a>` : mode === "Interview" ? `<a class="${primary}" href="/Student/Interview">${esc(t("OtherInterviews"))}</a>` : mode === "Adventure" ? `<a class="${primary}" href="/Student/Adventures">${esc(t("NewAdventure"))}</a>` : mode === "Review" ? `<a class="${primary}" href="/Student">${esc(t("Dashboard"))}</a>` : `<button type="button" class="${primary}" id="qz-again">${esc(t("Again"))}</button>`}
                <a class="${light}" href="/Student/History?id=${encodeURIComponent(attemptId)}" data-testid="quiz-review">${esc(t("Review"))}</a>
                <a class="${light}" href="/Student/Explore">${esc(t("OtherSubjects"))}</a>
            </div></div>`;
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
            const url = mode === "Topic" ? "/api/quiz/topic" : mode === "Adventure" ? "/api/quiz/adventure" : "/api/quiz/start";
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
