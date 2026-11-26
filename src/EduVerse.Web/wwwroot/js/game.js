// Dashboard extras of the "more like Duolingo" pass: "the story of your mistakes" (phase 5). The topics are picked on the
// server from the week's answers; the AI (or a story written in advance) only tells it.
(() => {
    "use strict";
    const { t, fmt, esc, api } = window.Edu;
    const box = document.getElementById("mistake-story");
    const button = document.getElementById("mistake-story-go");
    if (!box || !button) return;
    button.addEventListener("click", async () => {
        button.disabled = true;
        const out = document.getElementById("mistake-story-out");
        out.innerHTML = `<p class="text-sm text-muted-foreground-1"><span class="animate-spin inline-block size-4 border-2 border-current border-t-transparent rounded-full align-middle" aria-hidden="true"></span> ${esc(t("StoryWriting"))}</p>`;
        try {
            const r = await api("/api/game/mistake-story", { json: {} });
            out.innerHTML = `<article class="le-rise mt-1 p-4 rounded-2xl tone-lavender text-left" data-testid="mistake-story-text">
                <h3 class="text-base font-semibold text-foreground">📖 ${esc(r.title)}</h3>
                <p class="mt-2 text-sm leading-relaxed text-foreground/90 whitespace-pre-line">${esc(r.story)}</p>
                <p class="mt-3 text-xs text-foreground/70">${esc(fmt(t("StoryTopics"), r.topics.join(" · ")))}</p>
                ${r.fallback ? `<p class="mt-2 text-xs text-amber-700 dark:text-amber-300">${esc(fmt(t("Fallback"), r.fallback))}</p>` : ""}
                <a class="mt-3 py-2 px-4 inline-flex items-center gap-x-2 text-sm font-semibold rounded-full bg-primary text-primary-foreground hover:bg-primary-hover" href="${esc(r.practice)}" data-testid="mistake-story-practice">🎯 ${esc(t("StoryPractice"))}</a>
            </article>`;
        } catch (e) {
            out.innerHTML = `<p class="text-sm ${e.data?.code === "no_mistakes" ? "text-foreground" : "text-red-700 dark:text-red-300"}" role="status" data-testid="mistake-story-none">${esc(e.data?.code === "no_mistakes" ? t("StoryNoMistakes") : e.message)}</p>`;
            button.disabled = false;
        }
    });
})();
