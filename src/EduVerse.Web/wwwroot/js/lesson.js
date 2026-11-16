// Lesson in 3 levels: send the lesson, show the three versions with their measured readability,
// print a worksheet per level, export Markdown, keep the last lessons in this browser.
(() => {
    "use strict";
    const { t, fmt, esc, api, busy, num, badge, engineLabel, error, copyBtn, copyButtons } = window.Edu;
    const $ = (id) => document.getElementById(id);
    const KEY = "eduverse_lessons";
    const LEVELS = ["Basic", "Standard", "Advanced"];
    const levelClass = {
        Basic: "bg-teal-100 text-teal-800 dark:bg-teal-500/15 dark:text-teal-300",
        Standard: "bg-sky-100 text-sky-800 dark:bg-sky-500/15 dark:text-sky-300",
        Advanced: "bg-violet-100 text-violet-800 dark:bg-violet-500/15 dark:text-violet-300",
    };
    let current = null;
    let tab = "Basic";

    const history = () => { try { return JSON.parse(localStorage.getItem(KEY) || "[]"); } catch { return []; } };
    const saveHistory = (list) => { try { localStorage.setItem(KEY, JSON.stringify(list.slice(0, 10))); } catch { } };

    function renderHistory() {
        const list = history();
        $("ls-history").innerHTML = list.map((h, i) => `<li class="flex items-center justify-between gap-2">
            <button type="button" data-history="${i}" class="text-start text-primary hover:underline truncate">${esc(h.request.title || h.request.subject || t("Untitled"))}</button>
            <span class="shrink-0 text-xs text-muted-foreground-1">${esc(Edu.date(h.at))}</span></li>`).join("") || `<li class="text-xs text-muted-foreground-1">${esc(t("NoHistory"))}</li>`;
    }

    const updateCount = () => { $("ls-count").textContent = fmt(t("CharsCount"), $("ls-text").value.length); };

    function paragraphs(text) {
        return text.split(/\n{2,}|\n(?=- )/).map((p) => p.startsWith("- ")
            ? `<ul class="list-disc ps-5 space-y-1">${p.split("\n").map((l) => `<li>${esc(l.replace(/^- /, ""))}</li>`).join("")}</ul>`
            : `<p>${esc(p).replace(/\n/g, "<br>")}</p>`).join("");
    }

    function readabilityRow(label, s, cls) {
        return `<div class="rounded-lg border border-card-line p-3 min-w-0">
            <div class="flex items-center justify-between gap-2">${cls ? badge(label, cls) : `<span class="text-xs font-medium text-muted-foreground-1">${esc(label)}</span>`}<span class="text-lg font-semibold text-foreground">${s.score}</span></div>
            <div class="mt-2 h-1.5 rounded-full bg-surface overflow-hidden"><div class="h-full bg-primary" style="width:${s.score}%"></div></div>
            <p class="mt-1.5 text-xs text-muted-foreground-1">${esc(t("Read_" + s.level))}</p>
            <p class="text-[11px] text-muted-foreground-1">${esc(fmt(t("StatsLine"), s.words, num(s.wordsPerSentence, 1), s.readingMinutes))}</p>
        </div>`;
    }

    function render() {
        const { result, engine, request } = current;
        const v = result.versions.find((x) => x.level === tab) || result.versions[0];
        $("ls-empty").classList.add("hidden");
        const body = $("ls-body");
        body.classList.remove("hidden");
        body.innerHTML = `
            <div class="p-4 md:p-5 border-b border-card-divider">
                <div class="flex flex-wrap items-start justify-between gap-3">
                    <div class="min-w-0"><h2 class="text-lg font-semibold text-foreground">${esc(request.title || t("Untitled"))}</h2>
                        <p class="text-sm text-muted-foreground-1">${esc(request.subject)} · ${esc(t("Stage_" + request.stage))} · ${esc(engineLabel(engine))}</p></div>
                    <span class="flex flex-wrap gap-2">
                        <button type="button" id="ls-md" class="py-1.5 px-2.5 text-xs font-medium rounded-md border border-layer-line bg-layer text-foreground hover:bg-layer-hover">${esc(t("DownloadMd"))}</button>
                    </span>
                </div>
                ${result.objectives.length ? `<div class="mt-3"><h3 class="text-xs font-semibold uppercase text-muted-foreground-1">${esc(t("Objectives"))}</h3>
                    <ul class="mt-1 list-disc ps-5 text-sm text-foreground space-y-0.5">${result.objectives.map((o) => `<li>${esc(o)}</li>`).join("")}</ul></div>` : ""}
                <h3 class="mt-4 text-xs font-semibold uppercase text-muted-foreground-1">${esc(t("ReadabilityTitle"))}</h3>
                <div class="mt-2 grid grid-cols-2 lg:grid-cols-4 gap-2">
                    ${readabilityRow(t("Original"), result.original)}
                    ${result.versions.map((x) => readabilityRow(t("Level_" + x.level), x.stats, levelClass[x.level])).join("")}
                </div>
                <p class="mt-1 text-[11px] text-muted-foreground-1">${esc(t("ReadabilityHint"))}</p>
                ${result.warnings.length ? `<p class="mt-2 text-sm rounded-lg p-2.5 bg-amber-50 text-amber-900 dark:bg-amber-500/10 dark:text-amber-200">⚠️ ${result.warnings.map((w) => esc(t("Warn_" + w, w))).join(" ")}</p>` : ""}
            </div>
            <div class="px-4 md:px-5 pt-4">
                <nav class="flex gap-1 p-1 rounded-lg bg-surface" role="tablist" aria-label="${esc(t("Levels"))}">
                    ${LEVELS.map((l) => `<button type="button" role="tab" data-tab="${l}" aria-selected="${tab === l}" class="flex-1 py-2 px-3 text-sm font-medium rounded-md ${tab === l ? "bg-card text-foreground shadow-2xs" : "text-muted-foreground-1 hover:text-foreground"}">${esc(t("Level_" + l))}</button>`).join("")}
                </nav>
            </div>
            <div class="p-4 md:p-5 space-y-5" role="tabpanel">
                <div class="flex flex-wrap items-center justify-between gap-2">
                    <p class="text-sm text-muted-foreground-1">${esc(t("LevelHint_" + v.level))}</p>
                    <span class="flex gap-1">${copyBtn(v.text)}
                        <button type="button" data-print="students" class="py-1 px-2.5 text-xs font-semibold rounded-full border border-layer-line bg-layer text-foreground hover:bg-layer-hover">${esc(t("PrintStudents"))}</button>
                        <button type="button" data-print="teacher" class="py-1 px-2 text-xs font-medium rounded-md bg-primary text-primary-foreground hover:bg-primary-hover">${esc(t("PrintTeacher"))}</button></span>
                </div>
                <article class="text-[15px] leading-relaxed text-foreground space-y-3">${paragraphs(v.text)}</article>
                ${v.glossary.length ? `<div><h3 class="text-sm font-semibold text-foreground">${esc(t("Glossary"))}</h3>
                    <dl class="mt-2 grid sm:grid-cols-2 gap-2">${v.glossary.map((g) => `<div class="rounded-lg bg-surface p-2.5"><dt class="text-sm font-semibold text-foreground">${esc(g.term)}</dt><dd class="text-sm text-muted-foreground-1">${esc(g.definition)}</dd></div>`).join("")}</dl></div>` : ""}
                ${v.questions.length ? `<div><h3 class="text-sm font-semibold text-foreground">${esc(t("QuestionsTitle"))}</h3>
                    <ol class="mt-2 list-decimal ps-5 space-y-2 text-sm text-foreground">${v.questions.map((q) => `<li>${esc(q.question)}
                        <details class="mt-0.5"><summary class="text-xs text-primary cursor-pointer">${esc(t("ShowAnswer"))}</summary><p class="text-sm text-muted-foreground-1">${esc(q.answer)}</p></details></li>`).join("")}</ol></div>` : ""}
                ${v.activity ? `<div class="rounded-lg border border-primary/30 bg-primary/5 p-3"><h3 class="text-sm font-semibold text-foreground">${esc(t("Activity"))}</h3><p class="mt-1 text-sm text-foreground">${esc(v.activity)}</p></div>` : ""}
                ${result.teacherTips.length ? `<div><h3 class="text-sm font-semibold text-foreground">${esc(t("TeacherTips"))}</h3>
                    <ul class="mt-1 list-disc ps-5 text-sm text-muted-foreground-1 space-y-1">${result.teacherTips.map((x) => `<li>${esc(x)}</li>`).join("")}</ul></div>` : ""}
            </div>`;
        copyButtons(body);
    }

    // ---------- Exports ----------
    function markdown() {
        const { result, request } = current;
        const lines = [`# ${request.title || t("Untitled")}`, "", `${request.subject} · ${t("Stage_" + request.stage)}`, ""];
        if (result.objectives.length) lines.push(`## ${t("Objectives")}`, ...result.objectives.map((o) => `- ${o}`), "");
        for (const v of result.versions) {
            lines.push(`## ${t("Level_" + v.level)} (${v.stats.score}/100)`, "", v.text, "");
            if (v.glossary.length) lines.push(`### ${t("Glossary")}`, ...v.glossary.map((g) => `- **${g.term}**: ${g.definition}`), "");
            if (v.questions.length) lines.push(`### ${t("QuestionsTitle")}`, ...v.questions.map((q, i) => `${i + 1}. ${q.question}\n   - ${t("Answer")}: ${q.answer}`), "");
            if (v.activity) lines.push(`### ${t("Activity")}`, v.activity, "");
        }
        if (result.teacherTips.length) lines.push(`## ${t("TeacherTips")}`, ...result.teacherTips.map((x) => `- ${x}`));
        return lines.join("\n");
    }

    function download(name, text, type) {
        const a = document.createElement("a");
        a.href = URL.createObjectURL(new Blob([text], { type }));
        a.download = name;
        a.click();
        setTimeout(() => URL.revokeObjectURL(a.href), 1000);
    }

    /** A clean worksheet in a new window, ready to print or save as PDF. */
    function print(withAnswers) {
        const { request } = current;
        const v = current.result.versions.find((x) => x.level === tab);
        const w = window.open("", "_blank");
        if (!w) return;
        w.document.write(`<!doctype html><html lang="${document.documentElement.lang}"><head><meta charset="utf-8"><title>${esc(request.title)} · ${esc(t("Level_" + v.level))}</title>
            <style>body{font-family:system-ui,sans-serif;max-width:720px;margin:2rem auto;padding:0 1rem;line-height:1.6;color:#111}h1{font-size:1.5rem;margin-bottom:.2rem}.meta{color:#555;margin-bottom:1.5rem}
            dt{font-weight:600}dd{margin:0 0 .6rem}li{margin:.6rem 0}.answer{color:#0f766e}.line{border-bottom:1px solid #bbb;height:1.6rem}.box{border:1px solid #ccc;border-radius:8px;padding:.8rem}@media print{button{display:none}}</style></head><body>
            <button onclick="print()">🖨️</button>
            <h1>${esc(request.title)}</h1><p class="meta">${esc(request.subject)} · ${esc(t("Level_" + v.level))} · ${esc(t("NameLine"))} ____________________</p>
            ${paragraphs(v.text)}
            ${v.glossary.length ? `<h2>${esc(t("Glossary"))}</h2><dl>${v.glossary.map((g) => `<dt>${esc(g.term)}</dt><dd>${esc(g.definition)}</dd>`).join("")}</dl>` : ""}
            ${v.questions.length ? `<h2>${esc(t("QuestionsTitle"))}</h2><ol>${v.questions.map((q) => `<li>${esc(q.question)}${withAnswers ? `<div class="answer">${esc(q.answer)}</div>` : `<div class="line"></div><div class="line"></div>`}</li>`).join("")}</ol>` : ""}
            ${v.activity ? `<h2>${esc(t("Activity"))}</h2><div class="box">${esc(v.activity)}</div>` : ""}
            </body></html>`);
        w.document.close();
    }

    // ---------- Input ----------
    $("ls-file").addEventListener("change", async (e) => {
        const f = e.target.files[0];
        if (!f) return;
        try {
            $("ls-text").value = (await Edu.readFile(f)).replace(/[ \t]+/g, " ").trim().slice(0, 15000);
            if (!$("ls-title").value) $("ls-title").value = f.name.replace(/\.[^.]+$/, "");
            updateCount();
        } catch { error($("ls-error"), t("FileError")); }
        e.target.value = "";
    });
    $("ls-sample").addEventListener("click", async () => {
        const s = await api("/api/samples/lesson");
        $("ls-title").value = s.title; $("ls-subject").value = s.subject; $("ls-text").value = s.text; updateCount();
    });
    $("ls-text").addEventListener("input", updateCount);

    $("ls-form").addEventListener("submit", async (e) => {
        e.preventDefault();
        $("ls-error").classList.add("hidden");
        const request = {
            title: $("ls-title").value.trim(), subject: $("ls-subject").value.trim(), stage: $("ls-stage").value,
            text: $("ls-text").value, language: $("ls-language").value, questions: +$("ls-questions").value || 4,
        };
        try {
            const d = await busy($("ls-submit"), api("/api/lesson", { json: request }));
            Edu.notice($("ls-notice"), d);
            current = { ...d, request, at: new Date().toISOString() };
            tab = "Basic";
            render();
            saveHistory([current, ...history()]);
            renderHistory();
            if (matchMedia("(max-width: 1279px)").matches) $("ls-result").scrollIntoView({ behavior: "smooth" });
        } catch (err) { error($("ls-error"), err.message); }
    });

    document.addEventListener("click", (e) => {
        const tb = e.target.closest("[data-tab]");
        if (tb) { tab = tb.dataset.tab; render(); return; }
        const pr = e.target.closest("[data-print]");
        if (pr) { print(pr.dataset.print === "teacher"); return; }
        if (e.target.closest("#ls-md")) { download((current.request.title || "lesson") + ".md", markdown(), "text/markdown"); return; }
        const h = e.target.closest("[data-history]");
        if (h) {
            current = history()[+h.dataset.history];
            const r = current.request;
            $("ls-title").value = r.title; $("ls-subject").value = r.subject; $("ls-stage").value = r.stage; $("ls-language").value = r.language; $("ls-text").value = r.text; updateCount();
            tab = "Basic"; render();
        }
    });
    renderHistory();
    updateCount();
})();
