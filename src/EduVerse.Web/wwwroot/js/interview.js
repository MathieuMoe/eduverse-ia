// Interview with a historical figure (simulation). Offline, prepared answers to the suggested questions; with the AI, free questions.
// After 3 questions, a comprehension test made of checked facts.
(() => {
    "use strict";
    const { t, esc, api, lang } = window.Edu;
    const $ = (id) => document.getElementById(id);
    const root = $("iv-root");
    const { figure } = root.dataset;
    const chat = $("iv-chat");
    const field = $("iv-text");
    const emoji = root.querySelector("span[aria-hidden]").textContent;
    const history = [];
    let speak = false;
    const voices = { en: "en-GB", es: "es-ES", fr: "fr-FR", ca: "ca-ES" };

    window.Edu.voice.dictation($("iv-mic"), field, () => {});

    $("iv-speak").addEventListener("click", () => {
        speak = !speak;
        $("iv-speak").setAttribute("aria-pressed", String(speak));
        if (!speak) window.speechSynthesis?.cancel();
    });

    function say(text) {
        if (!speak || !("speechSynthesis" in window)) return;
        window.speechSynthesis.cancel();
        const u = new SpeechSynthesisUtterance(text);
        u.lang = voices[lang.slice(0, 2)] || "en-GB";
        window.speechSynthesis.speak(u);
    }

    function bubble(who, text) {
        const row = document.createElement("div");
        row.className = who === "me" ? "flex justify-end" : "flex items-start gap-3";
        row.innerHTML = who === "me"
            ? `<p class="py-2.5 px-4 rounded-2xl rounded-tr-sm bg-primary text-primary-foreground max-w-[85%]">${esc(text)}</p>`
            : `<span class="size-10 shrink-0 rounded-full tone-peach grid place-items-center text-xl" aria-hidden="true">${esc(emoji)}</span><p class="py-2.5 px-4 rounded-2xl rounded-tl-sm bg-surface text-foreground max-w-[85%]" data-testid="iv-answer">${esc(text)}</p>`;
        chat.appendChild(row);
        row.scrollIntoView({ block: "nearest", behavior: "smooth" });
    }

    async function ask(question) {
        question = question.trim();
        if (question.length < 2) return;
        bubble("me", question);
        field.value = "";
        try {
            const r = await api(`/api/interview/${encodeURIComponent(figure)}`, { json: { question, history } });
            bubble("figure", r.answer);
            say(r.answer);
            history.push({ question, answer: r.answer });
            if (history.length >= 3) $("iv-check").classList.remove("hidden");
        } catch (e) {
            bubble("figure", e.message);
        }
    }

    $("iv-form").addEventListener("submit", (e) => { e.preventDefault(); ask(field.value); });
    $("iv-suggestions").querySelectorAll("[data-question]").forEach((b) => b.addEventListener("click", () => { b.disabled = true; b.classList.add("opacity-50"); ask(b.dataset.question); }));
})();
