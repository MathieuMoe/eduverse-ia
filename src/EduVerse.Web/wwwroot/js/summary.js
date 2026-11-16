// Class summary: type or dictate the notes of the class, get a summary for students, absent students and families,
// homework, review questions and the plan for the next class. History in this browser.
(() => {
    "use strict";
    const { t, fmt, esc, api, busy, engineLabel, error, copyBtn, copyButtons } = window.Edu;
    const $ = (id) => document.getElementById(id);
    const KEY = "eduverse_summaries";
    const SPEECH = { English: "en-GB", Spanish: "es-ES", French: "fr-FR", Catalan: "ca-ES" };
    let current = null;

    const history = () => { try { return JSON.parse(localStorage.getItem(KEY) || "[]"); } catch { return []; } };
    const saveHistory = (list) => { try { localStorage.setItem(KEY, JSON.stringify(list.slice(0, 15))); } catch { } };
    const updateCount = () => { $("sm-count").textContent = fmt(t("CharsCount"), $("sm-notes").value.length); };

    function renderHistory() {
        $("sm-history").innerHTML = history().map((h, i) => `<li class="flex items-center justify-between gap-2">
            <button type="button" data-history="${i}" class="text-start text-primary hover:underline truncate">${esc(h.summary.title)}</button>
            <span class="shrink-0 text-xs text-muted-foreground-1">${esc(Edu.date(h.at))}</span></li>`).join("") || `<li class="text-xs text-muted-foreground-1">${esc(t("NoHistory"))}</li>`;
    }

    /** The text to publish on the platform or send to the students. */
    function forStudents(s) {
        const lines = [s.title, "", s.summary, "", t("KeyIdeas") + ":", ...s.keyIdeas.map((k) => "- " + k)];
        if (s.homework.length) lines.push("", t("Homework") + ":", ...s.homework.map((h) => "- " + h.task + (h.due ? ` (${h.due})` : "")));
        if (s.reviewQuestions.length) lines.push("", t("Review") + ":", ...s.reviewQuestions.map((q, i) => `${i + 1}. ${q.question}`));
        return lines.join("\n");
    }

    function markdown(s) {
        return [`# ${s.title}`, "", s.summary, "", `## ${t("KeyIdeas")}`, ...s.keyIdeas.map((k) => "- " + k), "",
            `## ${t("Homework")}`, ...(s.homework.length ? s.homework.map((h) => "- " + h.task + (h.due ? ` (${h.due})` : "")) : ["- " + t("NoHomework")]), "",
            `## ${t("ForAbsent")}`, s.forAbsent, "", `## ${t("FamilyNote")}`, s.familyNote, "",
            `## ${t("Review")}`, ...s.reviewQuestions.map((q, i) => `${i + 1}. ${q.question}\n   - ${t("Answer")}: ${q.answer}`), "",
            `## ${t("Difficulties")}`, ...s.difficulties.map((d) => "- " + d), "", `## ${t("NextClass")}`, ...s.nextClass.map((n) => "- " + n)].join("\n");
    }

    function list(items, empty) {
        return items.length ? `<ul class="mt-1 list-disc ps-5 space-y-1 text-sm text-foreground">${items.map((x) => `<li>${esc(x)}</li>`).join("")}</ul>` : `<p class="mt-1 text-sm text-muted-foreground-1">${esc(empty)}</p>`;
    }

    function render() {
        const s = current.summary;
        $("sm-empty").classList.add("hidden");
        const body = $("sm-body");
        body.classList.remove("hidden");
        body.innerHTML = `
            <div class="p-4 md:p-5 border-b border-card-divider flex flex-wrap items-start justify-between gap-3">
                <div class="min-w-0"><h2 class="text-lg font-semibold text-foreground">${esc(s.title)}</h2><p class="text-xs text-muted-foreground-1">${esc(engineLabel(current.engine))}</p></div>
                <span class="flex flex-wrap gap-1">${copyBtn(forStudents(s)).replace(esc(t("Copy")), esc(t("CopyStudents")))}
                    <button type="button" id="sm-md" class="py-1 px-2.5 text-xs font-semibold rounded-full border border-layer-line bg-layer text-foreground hover:bg-layer-hover">${esc(t("DownloadMd"))}</button></span>
            </div>
            <div class="p-4 md:p-5 space-y-5">
                ${s.warnings.length ? `<p class="text-sm rounded-lg p-2.5 bg-amber-50 text-amber-900 dark:bg-amber-500/10 dark:text-amber-200">⚠️ ${s.warnings.map((w) => esc(t("Warn_" + w, w))).join(" ")}</p>` : ""}
                <p class="text-[15px] leading-relaxed text-foreground">${esc(s.summary)}</p>
                <div><h3 class="text-sm font-semibold text-foreground">${esc(t("KeyIdeas"))}</h3>${list(s.keyIdeas, "–")}</div>
                <div class="rounded-lg border border-primary/30 bg-primary/5 p-3">
                    <h3 class="text-sm font-semibold text-foreground">📝 ${esc(t("Homework"))}</h3>
                    ${s.homework.length ? `<ul class="mt-1 space-y-1 text-sm text-foreground">${s.homework.map((h) => `<li>• ${esc(h.task)}${h.due ? ` <span class="ms-1 py-0.5 px-1.5 rounded-md text-xs bg-card border border-card-line">${esc(h.due)}</span>` : ""}</li>`).join("")}</ul>` : `<p class="mt-1 text-sm text-muted-foreground-1">${esc(t("NoHomework"))}</p>`}
                </div>
                <div class="grid md:grid-cols-2 gap-4">
                    <div class="rounded-lg bg-surface p-3"><div class="flex items-center justify-between gap-2"><h3 class="text-sm font-semibold text-foreground">${esc(t("ForAbsent"))}</h3>${copyBtn(s.forAbsent)}</div><p class="mt-1 text-sm text-foreground">${esc(s.forAbsent)}</p></div>
                    <div class="rounded-lg bg-surface p-3"><div class="flex items-center justify-between gap-2"><h3 class="text-sm font-semibold text-foreground">${esc(t("FamilyNote"))}</h3>${copyBtn(s.familyNote)}</div><p class="mt-1 text-sm text-foreground">${esc(s.familyNote)}</p></div>
                </div>
                <div><h3 class="text-sm font-semibold text-foreground">${esc(t("Review"))}</h3>
                    <ol class="mt-1 list-decimal ps-5 space-y-2 text-sm text-foreground">${s.reviewQuestions.map((q) => `<li>${esc(q.question)}<details><summary class="text-xs text-primary cursor-pointer">${esc(t("ShowAnswer"))}</summary><p class="text-muted-foreground-1">${esc(q.answer)}</p></details></li>`).join("")}</ol></div>
                <div class="grid md:grid-cols-2 gap-4 pt-2 border-t border-card-divider">
                    <div><h3 class="text-sm font-semibold text-foreground">⚠️ ${esc(t("Difficulties"))}</h3>${list(s.difficulties, t("NoDifficulties"))}</div>
                    <div><h3 class="text-sm font-semibold text-foreground">➡️ ${esc(t("NextClass"))}</h3>${list(s.nextClass, "–")}</div>
                </div>
            </div>`;
        copyButtons(body);
    }

    // ---------- Dictation (Web Speech API, Chrome / Edge) ----------
    const Recognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    let recognition = null;
    let listening = false;
    function toggleDictation() {
        const button = $("sm-dictate");
        if (!Recognition) { error($("sm-error"), t("NoSpeech")); return; }
        if (listening) { listening = false; recognition?.stop(); return; }
        recognition = new Recognition();
        recognition.lang = SPEECH[$("sm-language").value] || "en-GB";
        recognition.continuous = true;
        recognition.interimResults = false;
        recognition.onresult = (event) => {
            for (let i = event.resultIndex; i < event.results.length; i++) {
                if (event.results[i].isFinal) {
                    const area = $("sm-notes");
                    area.value = (area.value ? area.value.trimEnd() + " " : "") + event.results[i][0].transcript.trim() + ".";
                    updateCount();
                }
            }
        };
        recognition.onend = () => {
            if (listening) { try { recognition.start(); } catch { } return; }
            button.setAttribute("aria-pressed", "false");
            button.classList.remove("ring-2", "ring-red-400");
            button.querySelector("span").textContent = t("Dictate");
        };
        listening = true;
        button.setAttribute("aria-pressed", "true");
        button.classList.add("ring-2", "ring-red-400");
        button.querySelector("span").textContent = t("StopDictation");
        try { recognition.start(); } catch { }
    }

    $("sm-dictate").addEventListener("click", toggleDictation);
    $("sm-notes").addEventListener("input", updateCount);
    $("sm-sample").addEventListener("click", async () => {
        const s = await api("/api/samples/summary");
        $("sm-notes").value = s.notes;
        if (!$("sm-subject").value) $("sm-subject").value = t("SampleSubject");
        if (!$("sm-group").value) $("sm-group").value = "2B";
        updateCount();
    });
    $("sm-form").addEventListener("submit", async (e) => {
        e.preventDefault();
        $("sm-error").classList.add("hidden");
        const request = {
            subject: $("sm-subject").value.trim(), group: $("sm-group").value.trim(), date: $("sm-date").value,
            stage: $("sm-stage").value, language: $("sm-language").value, notes: $("sm-notes").value,
        };
        try {
            const d = await busy($("sm-submit"), api("/api/summary", { json: request }));
            Edu.notice($("sm-notice"), d);
            current = { ...d, request, at: new Date().toISOString() };
            render();
            saveHistory([current, ...history()]);
            renderHistory();
            if (matchMedia("(max-width: 1279px)").matches) $("sm-result").scrollIntoView({ behavior: "smooth" });
        } catch (err) { error($("sm-error"), err.message); }
    });
    document.addEventListener("click", (e) => {
        if (e.target.closest("#sm-md")) {
            const a = document.createElement("a");
            a.href = URL.createObjectURL(new Blob([markdown(current.summary)], { type: "text/markdown" }));
            a.download = (current.summary.title || "class").replace(/[^\p{L}\p{N} _-]+/gu, "") + ".md";
            a.click();
            return;
        }
        const h = e.target.closest("[data-history]");
        if (h) {
            current = history()[+h.dataset.history];
            const r = current.request;
            $("sm-subject").value = r.subject; $("sm-group").value = r.group; $("sm-date").value = r.date; $("sm-stage").value = r.stage; $("sm-language").value = r.language; $("sm-notes").value = r.notes;
            updateCount(); render();
        }
    });
    $("sm-date").value = new Date().toISOString().slice(0, 10);
    renderHistory();
    updateCount();
})();
