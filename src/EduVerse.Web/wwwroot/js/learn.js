// Student learning loop: placement test → explanation (method chosen by the code) → practice → feedback per misconception.
// The answers are checked on the server; the browser never receives the correct option before answering.
(() => {
    "use strict";
    const { t, fmt, esc, api, busy, notice, engineLabel, badge } = window.Edu;
    const $ = (id) => document.getElementById(id);
    const subject = $("ln-root").dataset.subject;
    const base = `/api/learn/${encodeURIComponent(subject)}`;
    const stage = $("ln-stage");
    let data = null;          // state payload: subject, concepts, states, next
    let chatConcept = null;

    const conceptName = (key) => data?.concepts.find((c) => c.key === key)?.name || key || "";
    const statusClass = {
        Mastered: "bg-emerald-500", Practising: "bg-sky-500", Learning: "bg-amber-500", NotStarted: "bg-gray-300 dark:bg-neutral-600",
    };
    const btn = "py-2 px-4 inline-flex justify-center items-center gap-x-2 text-sm font-semibold rounded-full focus:outline-hidden disabled:opacity-50 disabled:pointer-events-none";
    const primary = `${btn} bg-primary text-primary-foreground hover:bg-primary-hover`;
    const light = `${btn} border border-layer-line bg-layer text-foreground shadow-2xs hover:bg-layer-hover`;
    const pill = (text) => `<span class="inline-flex items-center py-0.5 px-2 rounded-full text-xs font-medium bg-primary/10 text-primary">${esc(text)}</span>`;
    const small = (text) => `<p class="text-[11px] text-muted-foreground-1">${esc(text)}</p>`;

    function showError(e) {
        stage.innerHTML = `<div class="py-8 text-center"><p class="text-sm text-red-600" role="alert">${esc(e.message)}</p>
            <button type="button" class="${light} mt-4" id="ln-retry">${esc(t("Retry"))}</button></div>`;
        $("ln-retry").addEventListener("click", load);
    }

    function renderMap() {
        const mastered = data.mastered;
        $("ln-progress").textContent = fmt(t("MasteredOf"), mastered, data.total);
        const cert = $("ln-certificate");
        if (cert && data.trade) {
            cert.hidden = false;
            cert.innerHTML = data.certificate
                ? `<a href="${esc(data.certificate.url)}" class="flex items-center gap-3 p-4 rounded-3xl tone-mint hover:-translate-y-0.5 transition-transform"><span class="text-3xl" aria-hidden="true">🎓</span><span class="min-w-0"><span class="block font-semibold text-foreground">${esc(t("CertificateEarned"))}</span><span class="block text-sm text-foreground/70">${esc(t("SeeCertificate"))} · ${esc(data.certificate.code)}</span></span></a>`
                : `<div class="p-4 rounded-3xl tone-sun"><p class="font-semibold text-foreground">🎓 ${esc(t("CertificateGoal"))}</p><div class="mt-2 h-2 rounded-full bg-white/60 dark:bg-white/10 overflow-hidden"><div class="h-full bg-primary rounded-full" style="width:${Math.round(100 * mastered / Math.max(1, data.total))}%"></div></div><p class="mt-1 text-xs text-foreground/70">${esc(fmt(t("MasteredOf"), mastered, data.total))}</p></div>`;
        }
        $("ln-map").innerHTML = data.concepts.map((c) => {
            const s = data.states[c.key];
            const p = s ? Math.round(s.pKnown * 100) : 0;
            const status = s && s.attempts > 0 ? s.status : "NotStarted";
            const current = data.next.conceptKey === c.key;
            return `<li class="-mx-2 px-2 py-1.5 rounded-xl ${current ? "tone-lavender" : ""}" ${current ? 'aria-current="step"' : ""}>
                <div class="flex items-center justify-between gap-2 text-sm">
                    <span class="flex items-center gap-x-2 min-w-0"><span class="size-2 shrink-0 rounded-full ${statusClass[status]}"></span><span class="truncate text-foreground" title="${esc(c.description)}">${esc(c.name)}</span></span>
                    <span class="text-xs text-muted-foreground-1 shrink-0">${s ? p + " %" : "–"}</span>
                </div>
                <div class="mt-1.5 h-2 rounded-full bg-surface overflow-hidden"><div class="h-full rounded-full ${statusClass[status]}" style="width:${s ? p : 0}%"></div></div>
            </li>`;
        }).join("");
    }

    async function load() {
        try {
            data = await api(base);
            renderMap();
            go(data.next);
        } catch (e) { showError(e); }
    }

    function setChat(conceptKey) {
        const key = conceptKey || data.concepts[0]?.key;
        if (!key || key === chatConcept) return;
        chatConcept = key;
        $("ln-chat-concept").textContent = fmt(t("ChatAbout"), conceptName(key));
        $("ln-chat").innerHTML = "";
        api(`${base}/chat/${encodeURIComponent(key)}`).then((list) => {
            $("ln-chat").innerHTML = list.length ? list.map(bubble).join("") : `<p class="text-muted-foreground-1">${esc(t("ChatEmpty"))}</p>`;
            scrollChat();
        }).catch(() => { });
    }

    const bubble = (m) => m.fromStudent
        ? `<div class="flex justify-end"><p class="max-w-[85%] py-2 px-3.5 rounded-2xl rounded-ee-md bg-primary text-primary-foreground whitespace-pre-line">${esc(m.text)}</p></div>`
        : `<div class="flex"><p class="max-w-[85%] py-2 px-3.5 rounded-2xl rounded-es-md bg-surface text-foreground whitespace-pre-line">${esc(m.text)}</p></div>`;
    const scrollChat = () => { $("ln-chat").scrollTop = $("ln-chat").scrollHeight; };

    function go(next) {
        notice($("ln-notice"), null);
        setChat(next.conceptKey);
        switch (next.kind) {
            case "Diagnostic": return diagnosticIntro(next);
            case "Learn": return explain(next);
            case "Practice":
            case "Review": return exercise(next);
            default: return done();
        }
    }

    // ---------- Placement test ----------
    function diagnosticIntro(next) {
        if (next.reason === "Resume") return diagnosticQuestion();
        stage.innerHTML = `<div class="max-w-2xl">
            ${pill(t("Step_Diagnostic"))}
            <h2 class="mt-3 text-xl font-semibold text-foreground">${esc(t("DiagTitle"))}</h2>
            <p class="mt-2 text-sm text-muted-foreground-1">${esc(t("DiagIntro"))}</p>
            <button type="button" id="ln-diag-start" class="${primary} mt-5">${esc(t("DiagStart"))}</button></div>`;
        $("ln-diag-start").addEventListener("click", (e) => busy(e.currentTarget, diagnosticQuestion()));
    }

    async function diagnosticQuestion() {
        try {
            const d = await api(`${base}/diagnostic`, { method: "POST" });
            if (d.finished) return diagnosticDone(d);
            notice($("ln-notice"), d);
            renderExercise(d.exercise, {
                label: fmt(t("DiagProgress"), d.progress.answered + 1, d.progress.max),
                engine: d.engine,
                diagnostic: true,
            });
        } catch (e) { showError(e); }
    }

    function diagnosticDone(d) {
        data = d.state;
        renderMap();
        stage.innerHTML = `<div class="max-w-2xl">
            ${pill(t("DiagDone"))}
            <h2 class="mt-3 text-xl font-semibold text-foreground">${esc(t("DiagResultTitle"))}</h2>
            <p class="mt-2 text-sm text-muted-foreground-1">${esc(d.start ? fmt(t("DiagResultStart"), conceptName(d.start)) : t("DiagResultAll"))}</p>
            <button type="button" id="ln-continue" class="${primary} mt-5">${esc(t("Continue"))}</button></div>`;
        $("ln-continue").addEventListener("click", () => go(data.next));
    }

    // ---------- Explanation ----------
    async function explain(next) {
        stage.innerHTML = `<div class="py-10 text-center text-sm text-muted-foreground-1">${esc(t("Explaining"))}</div>`;
        try {
            const d = await api(`${base}/explain/${encodeURIComponent(next.conceptKey)}`, { method: "POST" });
            notice($("ln-notice"), d);
            const x = d.explanation;
            const switched = d.methodsTried.length > 1;
            stage.innerHTML = `<article class="max-w-3xl">
                <div class="flex flex-wrap items-center gap-2">${pill(t("Step_Learn"))}${badge(fmt(t("MethodLabel"), t("Method_" + x.method)), "bg-surface text-foreground")}
                    <span class="ms-auto">${small(engineLabel(d.engine))}</span></div>
                <p class="mt-3 text-sm text-muted-foreground-1">${esc(t("Reason_" + next.reason))}${switched ? " " + esc(t("SwitchedHint")) : ""}</p>
                <h2 class="mt-2 text-xl font-semibold text-foreground">${esc(x.title)}</h2>
                <div class="mt-3 text-[15px] leading-relaxed text-foreground whitespace-pre-line">${esc(x.body)}</div>
                ${x.example ? `<div class="mt-4 p-4 rounded-2xl tone-lavender"><h3 class="text-xs font-semibold uppercase text-muted-foreground-1">${esc(t("Example"))}</h3><p class="mt-1 text-sm text-foreground whitespace-pre-line">${esc(x.example)}</p></div>` : ""}
                ${x.checkQuestion ? `<details class="mt-4 p-4 rounded-2xl border border-card-divider"><summary class="cursor-pointer text-sm font-medium text-foreground">${esc(t("CheckYourself"))}: ${esc(x.checkQuestion)}</summary><p class="mt-2 text-sm text-muted-foreground-1">${esc(x.checkAnswer)}</p></details>` : ""}
                <div class="mt-6 flex flex-wrap gap-3">
                    <button type="button" id="ln-understood" class="${primary}">${esc(t("Understood"))}</button>
                    <button type="button" id="ln-ask" class="${light}">${esc(t("AskTutor"))}</button>
                </div></article>`;
            $("ln-understood").addEventListener("click", async (e) => {
                try {
                    data = await busy(e.currentTarget, api(`${base}/explained/${encodeURIComponent(next.conceptKey)}`, { method: "POST" }));
                    renderMap();
                    go(data.next);
                } catch (err) { showError(err); }
            });
            $("ln-ask").addEventListener("click", () => $("ln-chat-input").focus());
        } catch (e) { showError(e); }
    }

    // ---------- Practice ----------
    async function exercise(next) {
        stage.innerHTML = `<div class="py-10 text-center text-sm text-muted-foreground-1">${esc(t("Loading"))}</div>`;
        try {
            const d = await api(`${base}/exercise/${encodeURIComponent(next.conceptKey)}`, { method: "POST" });
            notice($("ln-notice"), d);
            renderExercise(d.exercise, {
                label: next.reason === "ReviewPrerequisite" ? t("Step_Prerequisite") : next.kind === "Review" ? t("Step_Review") : t("Step_Practice"),
                reason: t("Reason_" + next.reason),
                engine: d.engine,
            });
        } catch (e) { showError(e); }
    }

    function renderExercise(x, o) {
        const stars = "●".repeat(x.difficulty) + "○".repeat(3 - x.difficulty);
        stage.innerHTML = `<div class="max-w-3xl">
            <div class="flex flex-wrap items-center gap-2">${pill(o.label)}
                <span class="text-xs text-muted-foreground-1">${esc(conceptName(x.conceptKey))} · <span title="${esc(t("Difficulty"))}" aria-label="${esc(fmt(t("DifficultyOf"), x.difficulty))}">${stars}</span></span>
                <span class="ms-auto">${small(engineLabel(o.engine))}</span></div>
            ${o.reason ? `<p class="mt-3 text-sm text-muted-foreground-1">${esc(o.reason)}</p>` : ""}
            ${x.visual?.startsWith("dg:") && window.Edu.diagram ? `<div class="mt-4 flex justify-center quiz-diagram">${window.Edu.diagram(x.visual)}</div>` : x.visual ? `<p class="mt-4 text-4xl text-center leading-snug" aria-hidden="true">${esc(x.visual)}</p>` : ""}
            <h2 class="mt-3 text-lg font-semibold text-foreground whitespace-pre-line" id="ln-question">${esc(x.question)}</h2>
            ${x.code ? `<pre class="mt-3 p-3 rounded-xl bg-[#2b2230] text-[#f3eefe] text-sm overflow-x-auto"><code>${esc(x.code)}</code></pre>` : ""}
            <div class="mt-4 grid sm:grid-cols-2 gap-3" role="group" aria-labelledby="ln-question" id="ln-options">
                ${x.options.map((opt, i) => `<button type="button" data-i="${i}" class="ln-option text-start p-4 rounded-2xl border border-layer-line bg-layer text-foreground hover:border-primary hover:bg-layer-hover focus:outline-hidden focus:ring-2 focus:ring-primary/30">
                    <span class="font-semibold me-2 text-muted-foreground-1">${"ABCD"[i]}</span>${esc(opt)}</button>`).join("")}
            </div>
            ${o.diagnostic ? "" : `<div class="mt-4 flex flex-wrap items-center gap-2"><button type="button" id="ln-hint" class="py-1.5 px-3.5 inline-flex items-center gap-x-1.5 text-sm font-medium rounded-full border border-layer-line bg-layer text-foreground hover:bg-layer-hover disabled:opacity-50" data-testid="tutor-hint">💡 <span id="ln-hint-label">${esc(fmt(t("HintN"), 1, 3))}</span></button><span class="text-[11px] text-muted-foreground-1">${esc(t("HintCost"))}</span></div>
            <p id="ln-hint-text" class="hidden mt-3 p-3 rounded-2xl tone-sky text-sm text-foreground" role="status"></p>`}
            <div id="ln-feedback" class="mt-5"></div></div>`;
        stage.querySelectorAll(".ln-option").forEach((b) => b.addEventListener("click", () => answer(x, +b.dataset.i, o)));
        $("ln-hint")?.addEventListener("click", async () => {
            const b = $("ln-hint");
            b.disabled = true;
            try {
                const h = await api("/api/learn/hint", { json: { exerciseId: x.id, choice: 0 } });
                if (h.text) { $("ln-hint-text").textContent = "🧭 " + h.text; $("ln-hint-text").classList.remove("hidden"); }
                h.removed.forEach((i) => { const opt = stage.querySelector(`.ln-option[data-i="${i}"]`); if (opt) { opt.disabled = true; opt.classList.add("opacity-40", "line-through"); } });
                $("ln-hint-label").textContent = h.level >= h.max ? t("NoMoreHints") : fmt(t("HintN"), h.level + 1, h.max);
                b.disabled = h.level >= h.max;
            } catch (e) { showError(e); b.disabled = false; }
        });
    }

    async function answer(x, choice, o) {
        const options = [...stage.querySelectorAll(".ln-option")];
        options.forEach((b) => { b.disabled = true; });
        if ($("ln-hint")) $("ln-hint").disabled = true;
        try {
            const d = await api("/api/learn/answer", { json: { exerciseId: x.id, choice } });
            options.forEach((b, i) => {
                b.classList.remove("hover:border-primary", "hover:bg-layer-hover");
                if (i === d.correctIndex) b.classList.add("border-teal-500", "bg-teal-50", "dark:bg-teal-500/10");
                else if (i === choice) b.classList.add("border-red-500", "bg-red-50", "dark:bg-red-500/10");
                else b.classList.add("opacity-60");
            });
            const events = (d.events || []).map(eventHtml).join("");
            const head = d.correct
                ? `<p class="font-semibold text-teal-700 dark:text-teal-300">✓ ${esc(t("Correct"))}</p>`
                : `<p class="font-semibold text-red-700 dark:text-red-300">✗ ${esc(t("NotQuite"))}</p>`;
            const mistake = d.misconception ? `<p class="mt-2 text-sm text-foreground"><span class="font-medium">${esc(t("WhyWrong"))}</span> ${esc(d.misconception.text)}</p>` : "";
            const mastery = d.state ? `<p class="mt-2 text-xs text-muted-foreground-1">${esc(fmt(t("MasteryNow"), Math.round(d.state.pKnown * 100)))}</p>` : "";
            $("ln-feedback").innerHTML = `<div class="p-5 rounded-2xl border ${d.correct ? "border-teal-200 dark:border-teal-500/30" : "border-red-200 dark:border-red-500/30"}" role="status">
                ${head}${mistake}${o.diagnostic ? "" : `<p class="mt-2 text-sm text-muted-foreground-1 whitespace-pre-line">${esc(d.explanation)}</p>`}${mastery}
                ${events ? `<ul class="mt-3 space-y-2">${events}</ul>` : ""}
                ${d.certificate ? `<div class="mt-4 p-4 rounded-2xl tone-mint" data-testid="ln-certificate-earned"><p class="text-lg font-semibold text-foreground">🎓 ${esc(t("CertificateEarned"))}</p><p class="text-sm text-foreground/70">+${d.reward?.xp ?? 0} XP · ${esc(d.certificate.code)}</p><a class="${primary} mt-3" href="${esc(d.certificate.url)}">${esc(t("SeeCertificate"))}</a></div>` : ""}
                <button type="button" id="ln-continue" class="${primary} mt-4">${esc(t("Continue"))}</button></div>`;
            $("ln-continue").focus();
            $("ln-continue").addEventListener("click", () => (o.diagnostic ? diagnosticQuestion() : load()));
        } catch (e) {
            options.forEach((b) => { b.disabled = false; });
            showError(e);
        }
    }

    function eventHtml(e) {
        const text = {
            Mastered: t("Event_Mastered"),
            SwitchMethod: fmt(t("Event_SwitchMethod"), t("Method_" + e.detail)),
            RecurringMisconception: fmt(t("Event_Recurring"), e.text || e.detail),
            NeedsTeacher: t("Event_NeedsTeacher"),
        }[e.kind];
        const cls = {
            Mastered: "bg-teal-50 text-teal-800 dark:bg-teal-500/15 dark:text-teal-300",
            NeedsTeacher: "bg-violet-50 text-violet-800 dark:bg-violet-500/15 dark:text-violet-300",
        }[e.kind] || "bg-amber-50 text-amber-800 dark:bg-amber-500/15 dark:text-amber-300";
        return text ? `<li class="p-2 rounded-md text-sm ${cls}" data-event="${esc(e.kind)}">${esc(text)}</li>` : "";
    }

    function done() {
        stage.innerHTML = `<div class="py-8 text-center max-w-xl mx-auto">
            <p class="text-4xl" aria-hidden="true">🎓</p>
            <h2 class="mt-3 text-xl font-semibold text-foreground">${esc(t("DoneTitle"))}</h2>
            <p class="mt-2 text-sm text-muted-foreground-1">${esc(t("DoneText"))}</p>
            <a href="/Student/NewSubject" class="${primary} mt-5">${esc(t("DoneNewSubject"))}</a></div>`;
    }

    // ---------- Socratic chat ----------
    $("ln-chat-form").addEventListener("submit", async (e) => {
        e.preventDefault();
        const input = $("ln-chat-input");
        const message = input.value.trim();
        if (!message || !chatConcept) return;
        const box = $("ln-chat");
        if (box.querySelector("p.text-muted-foreground-1")) box.innerHTML = "";
        box.insertAdjacentHTML("beforeend", bubble({ fromStudent: true, text: message }));
        input.value = "";
        scrollChat();
        try {
            const d = await busy($("ln-chat-send"), api(`${base}/chat/${encodeURIComponent(chatConcept)}`, { json: { message } }));
            box.insertAdjacentHTML("beforeend", bubble({ fromStudent: false, text: d.text }) + (d.fallback ? `<p class="text-[11px] text-amber-700 dark:text-amber-300">${esc(fmt(t("Fallback"), d.fallback))}</p>` : ""));
        } catch (err) {
            box.insertAdjacentHTML("beforeend", `<p class="text-sm text-red-600" role="alert">${esc(err.message)}</p>`);
        }
        scrollChat();
    });

    $("ln-reset").addEventListener("click", async (e) => {
        if (!confirm(t("RetakeConfirm"))) return;
        try {
            data = await busy(e.currentTarget, api(`${base}/reset`, { method: "POST" }));
            renderMap();
            go(data.next);
        } catch (err) { showError(err); }
    });

    load();
})();
