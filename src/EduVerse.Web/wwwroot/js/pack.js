// Lesson pack: one document in, a full lesson out (plan, slides, worksheet, 10 exercises, exam with solutions, rubric).
// Packs are saved on the server for the teacher; printable and exportable to Markdown.
(() => {
    "use strict";
    const { t, fmt, esc, api, busy, badge, engineLabel, error, notice } = window.Edu;
    const $ = (id) => document.getElementById(id);
    const TABS = ["Plan", "Slides", "Worksheet", "Exercises", "Exam", "Rubric"];
    let current = null;
    let tab = "Plan";

    const updateCount = () => { $("pk-count").textContent = fmt(t("CharsCount"), $("pk-text").value.length); };
    const list = (items) => `<ul class="list-disc ps-5 space-y-1">${items.map((x) => `<li>${esc(x)}</li>`).join("")}</ul>`;
    const h3 = (text) => `<h3 class="text-sm font-semibold text-foreground">${esc(text)}</h3>`;

    async function loadHistory() {
        try {
            const items = await api("/api/packs");
            $("pk-history").innerHTML = items.map((p) => `<li class="flex items-center justify-between gap-2">
                <button type="button" data-open="${p.id}" class="text-start text-primary hover:underline truncate">${esc(p.title)}</button>
                <span class="shrink-0 text-xs text-muted-foreground-1">${esc(Edu.date(p.createdAt))}</span></li>`).join("")
                || `<li class="text-xs text-muted-foreground-1">${esc(t("NoHistory"))}</li>`;
        } catch { }
    }

    function section(name, p) {
        switch (name) {
            case "Plan":
                return `<div class="space-y-4">
                    ${p.objectives.length ? `<div>${h3(t("Objectives"))}<div class="mt-2 text-sm text-foreground">${list(p.objectives)}</div></div>` : ""}
                    <div>${h3(fmt(t("PlanMinutes"), p.plan.reduce((a, s) => a + s.minutes, 0)))}
                    <ol class="mt-2 space-y-2">${p.plan.map((s) => `<li class="flex gap-3 p-4 rounded-2xl border border-card-divider">
                        <span class="shrink-0 w-14 text-sm font-semibold text-primary">${s.minutes} min</span>
                        <span class="text-sm"><span class="font-medium text-foreground">${esc(s.title)}</span><span class="block text-muted-foreground-1">${esc(s.activity)}</span></span></li>`).join("")}</ol></div></div>`;
            case "Slides":
                return `<ol class="grid sm:grid-cols-2 gap-3">${p.slides.map((s, i) => `<li class="aspect-[4/3] p-4 rounded-2xl border border-card-divider bg-surface/40 flex flex-col overflow-hidden">
                    <span class="text-[11px] text-muted-foreground-1">${i + 1} / ${p.slides.length}</span>
                    <span class="mt-1 font-semibold text-foreground">${esc(s.title)}</span>
                    <div class="mt-2 text-xs text-foreground overflow-hidden">${list(s.bullets)}</div></li>`).join("")}</ol>`;
            case "Worksheet":
                return `<div class="space-y-4 text-sm text-foreground">
                    <p class="whitespace-pre-line">${esc(p.worksheet.summary)}</p>
                    ${p.worksheet.keyPoints.length ? `<div>${h3(t("KeyIdeas"))}<div class="mt-2">${list(p.worksheet.keyPoints)}</div></div>` : ""}
                    ${p.worksheet.tasks.length ? `<div>${h3(t("Tasks"))}<ol class="mt-2 list-decimal ps-5 space-y-1">${p.worksheet.tasks.map((x) => `<li>${esc(x)}</li>`).join("")}</ol></div>` : ""}</div>`;
            case "Exercises":
                return `<ol class="space-y-3">${p.exercises.map((e, i) => `<li class="p-4 rounded-2xl border border-card-divider text-sm">
                    <p class="font-medium text-foreground">${i + 1}. ${esc(e.question)}</p>
                    <ul class="mt-2 grid sm:grid-cols-2 gap-1">${e.options.map((o, j) => `<li class="${j === e.correctIndex ? "text-teal-700 dark:text-teal-300 font-medium" : "text-muted-foreground-1"}">${"ABCD"[j]}) ${esc(o)}${j === e.correctIndex ? " ✓" : ""}</li>`).join("")}</ul>
                    ${e.explanation ? `<p class="mt-2 text-xs text-muted-foreground-1">${esc(e.explanation)}</p>` : ""}</li>`).join("")}</ol>`;
            case "Exam":
                return `<p class="text-sm text-muted-foreground-1">${esc(fmt(t("ExamPoints"), p.exam.reduce((a, q) => a + q.points, 0)))}</p>
                    <ol class="mt-3 space-y-3">${p.exam.map((q, i) => `<li class="p-4 rounded-2xl border border-card-divider text-sm">
                    <p class="font-medium text-foreground">${i + 1}. ${esc(q.question)} <span class="text-muted-foreground-1 font-normal">(${q.points} ${esc(t("Points"))})</span></p>
                    <details class="mt-2"><summary class="cursor-pointer text-primary text-xs">${esc(t("ShowSolution"))}</summary><p class="mt-1 text-muted-foreground-1 whitespace-pre-line">${esc(q.solution)}</p></details></li>`).join("")}</ol>`;
            default:
                return `<div class="overflow-x-auto"><table class="min-w-full text-sm"><thead><tr class="border-b border-card-divider text-xs text-muted-foreground-1">
                    <th scope="col" class="py-2 pe-3 text-start font-medium">${esc(t("Criterion"))}</th>
                    ${["Excellent", "Good", "Sufficient", "Insufficient"].map((l) => `<th scope="col" class="py-2 px-2 text-start font-medium">${esc(t("Rubric_" + l))}</th>`).join("")}</tr></thead>
                    <tbody class="divide-y divide-card-divider">${p.rubric.map((c) => `<tr><th scope="row" class="py-2 pe-3 text-start font-medium text-foreground align-top">${esc(c.criterion)}</th>
                    ${c.levels.map((l) => `<td class="py-2 px-2 align-top text-muted-foreground-1 min-w-36">${esc(l)}</td>`).join("")}</tr>`).join("")}</tbody></table></div>`;
        }
    }

    function render() {
        const p = current.pack;
        $("pk-empty").classList.add("hidden");
        const body = $("pk-body");
        body.classList.remove("hidden");
        const counts = { Plan: p.plan.length, Slides: p.slides.length, Worksheet: p.worksheet.tasks.length, Exercises: p.exercises.length, Exam: p.exam.length, Rubric: p.rubric.length };
        body.innerHTML = `<div class="p-4 md:p-5 border-b border-card-divider flex flex-wrap items-start justify-between gap-3">
                <div class="min-w-0"><h2 class="text-lg font-semibold text-foreground">${esc(p.title)}</h2><p class="text-xs text-muted-foreground-1">${esc(engineLabel(current.engine))}</p></div>
                <div class="flex flex-wrap gap-2">
                    <button type="button" id="pk-print" class="py-1.5 px-3 text-xs font-semibold rounded-full border border-layer-line bg-layer text-foreground hover:bg-layer-hover">${esc(t("Print"))}</button>
                    <button type="button" id="pk-md" class="py-1.5 px-3 text-xs font-semibold rounded-full border border-layer-line bg-layer text-foreground hover:bg-layer-hover">${esc(t("DownloadMd"))}</button>
                    <button type="button" id="pk-delete" class="py-1.5 px-3 text-xs font-semibold rounded-full border border-layer-line bg-layer text-red-600 hover:bg-layer-hover">${esc(t("Delete"))}</button>
                </div></div>
            <nav class="px-4 md:px-5 pt-3 flex gap-1 overflow-x-auto border-b border-card-divider" role="tablist">
                ${TABS.map((k) => `<button type="button" role="tab" aria-selected="${k === tab}" data-tab="${k}" class="shrink-0 py-2 px-3 -mb-px text-sm border-b-2 ${k === tab ? "border-primary text-primary font-medium" : "border-transparent text-muted-foreground-1 hover:text-foreground"}">${esc(t("Pack_" + k))} <span class="text-xs opacity-70">${counts[k]}</span></button>`).join("")}
            </nav>
            <div class="p-4 md:p-5" role="tabpanel">${section(tab, p)}</div>`;
    }

    function markdown(p) {
        const lines = [`# ${p.title}`, "", `## ${t("Objectives")}`, ...p.objectives.map((o) => `- ${o}`), "", `## ${t("Pack_Plan")}`,
            ...p.plan.map((s) => `- **${s.title}** (${s.minutes} min): ${s.activity}`), "", `## ${t("Pack_Slides")}`];
        p.slides.forEach((s, i) => lines.push("", `### ${i + 1}. ${s.title}`, ...s.bullets.map((b) => `- ${b}`)));
        lines.push("", `## ${t("Pack_Worksheet")}`, "", p.worksheet.summary, "", ...p.worksheet.keyPoints.map((k) => `- ${k}`), "", ...p.worksheet.tasks.map((x, i) => `${i + 1}. ${x}`));
        lines.push("", `## ${t("Pack_Exercises")}`);
        p.exercises.forEach((e, i) => lines.push("", `${i + 1}. ${e.question}`, ...e.options.map((o, j) => `   - ${"ABCD"[j]}) ${o}${j === e.correctIndex ? " ✓" : ""}`)));
        lines.push("", `## ${t("Pack_Exam")}`);
        p.exam.forEach((q, i) => lines.push("", `${i + 1}. ${q.question} (${q.points} ${t("Points")})`, `   > ${q.solution.replace(/\n/g, " ")}`));
        lines.push("", `## ${t("Pack_Rubric")}`, "", `| ${t("Criterion")} | ${t("Rubric_Excellent")} | ${t("Rubric_Good")} | ${t("Rubric_Sufficient")} | ${t("Rubric_Insufficient")} |`, "|---|---|---|---|---|",
            ...p.rubric.map((c) => `| ${[c.criterion, ...c.levels].map((x) => x.replace(/\|/g, "/")).join(" | ")} |`));
        return lines.join("\n");
    }

    function print() {
        const p = current.pack;
        const w = window.open("", "_blank");
        if (!w) return;
        const html = section("Plan", p) + `<h2>${esc(t("Pack_Worksheet"))}</h2>` + section("Worksheet", p) + `<h2>${esc(t("Pack_Exercises"))}</h2>` + section("Exercises", p)
            + `<h2>${esc(t("Pack_Exam"))}</h2>` + section("Exam", p) + `<h2>${esc(t("Pack_Rubric"))}</h2>` + section("Rubric", p);
        w.document.write(`<!doctype html><html lang="${document.documentElement.lang}"><head><meta charset="utf-8"><title>${esc(p.title)}</title>
            <style>body{font-family:system-ui,sans-serif;max-width:800px;margin:2rem auto;padding:0 1rem;line-height:1.5;color:#111}li{margin:.4rem 0}table{border-collapse:collapse}th,td{border:1px solid #ccc;padding:.3rem;vertical-align:top;text-align:left}details>*{display:block}summary{display:none}@media print{button{display:none}}</style></head><body>
            <button onclick="print()">🖨️</button><h1>${esc(p.title)}</h1>${html}</body></html>`);
        w.document.close();
    }

    async function open(id) {
        try {
            current = await api(`/api/packs/${id}`);
            tab = "Plan";
            notice($("pk-notice"), null);
            render();
        } catch (e) { error($("pk-error"), e.message); }
    }

    document.addEventListener("click", async (e) => {
        const tb = e.target.closest("[data-tab]");
        if (tb) { tab = tb.dataset.tab; render(); return; }
        const op = e.target.closest("[data-open]");
        if (op) { open(op.dataset.open); return; }
        if (e.target.closest("#pk-print")) { print(); return; }
        if (e.target.closest("#pk-md")) {
            const a = document.createElement("a");
            a.href = URL.createObjectURL(new Blob([markdown(current.pack)], { type: "text/markdown" }));
            a.download = (current.pack.title || "lesson").replace(/[^\p{L}\p{N}]+/gu, "-").toLowerCase() + ".md";
            a.click();
            URL.revokeObjectURL(a.href);
            return;
        }
        if (e.target.closest("#pk-delete") && current?.id) {
            try {
                await api(`/api/packs/${current.id}`, { method: "DELETE" });
                current = null;
                $("pk-body").classList.add("hidden");
                $("pk-empty").classList.remove("hidden");
                loadHistory();
            } catch (err) { error($("pk-error"), err.message); }
        }
    });

    $("pk-file").addEventListener("change", async (e) => {
        const f = e.target.files[0];
        if (!f) return;
        try {
            $("pk-text").value = (await Edu.readFile(f)).replace(/[ \t]+/g, " ").trim().slice(0, 20000);
            if (!$("pk-title").value) $("pk-title").value = f.name.replace(/\.[^.]+$/, "");
            updateCount();
        } catch { error($("pk-error"), t("FileError")); }
        e.target.value = "";
    });
    $("pk-sample").addEventListener("click", async () => {
        const s = await api("/api/samples/pack");
        $("pk-title").value = s.title; $("pk-subject").value = s.subject; $("pk-text").value = s.text; updateCount();
    });
    $("pk-text").addEventListener("input", updateCount);

    $("pk-form").addEventListener("submit", async (e) => {
        e.preventDefault();
        $("pk-error").classList.add("hidden");
        const request = {
            title: $("pk-title").value.trim(), subject: $("pk-subject").value.trim(), stage: $("pk-stage").value,
            text: $("pk-text").value, language: $("pk-language").value,
        };
        try {
            const d = await busy($("pk-submit"), api("/api/packs", { json: request }));
            current = d;
            tab = "Plan";
            notice($("pk-notice"), d);
            render();
            loadHistory();
            if (matchMedia("(max-width: 1279px)").matches) $("pk-result").scrollIntoView({ behavior: "smooth" });
        } catch (err) { error($("pk-error"), err.message); }
    });

    updateCount();
    loadHistory();
})();
