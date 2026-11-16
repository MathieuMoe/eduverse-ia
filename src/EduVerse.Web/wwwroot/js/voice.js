// Voice for the student platform: speech recognition of the browser (dictation, spoken answers).
// It only works where the browser offers it; the button is hidden otherwise. The audio is processed by the browser's own service.
(() => {
    "use strict";
    const Recognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    const voices = { en: "en-GB", es: "es-ES", fr: "fr-FR", ca: "ca-ES" };
    const numbers = {
        en: ["zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten"],
        es: ["cero", "uno", "dos", "tres", "cuatro", "cinco", "seis", "siete", "ocho", "nueve", "diez"],
        fr: ["zero", "un", "deux", "trois", "quatre", "cinq", "six", "sept", "huit", "neuf", "dix"],
        ca: ["zero", "u", "dos", "tres", "quatre", "cinc", "sis", "set", "vuit", "nou", "deu"],
    };
    const letters = { en: ["a", "b", "c", "d"], es: ["a", "be", "ce", "de"], fr: ["a", "be", "ce", "de"], ca: ["a", "be", "ce", "de"] };

    const norm = (s) => " " + String(s).toLowerCase().normalize("NFD").replace(/[̀-ͯ]/g, "").replace(/[^\p{L}\p{N}/.,]+/gu, " ").trim() + " ";

    function lang() { return (document.documentElement.lang || "en").slice(0, 2); }

    /** Listens once and resolves with what was said (rejects if not supported or nothing heard). */
    function listen() {
        return new Promise((resolve, reject) => {
            if (!Recognition) { reject(new Error("unsupported")); return; }
            const r = new Recognition();
            r.lang = voices[lang()] || "en-GB";
            r.interimResults = false;
            r.maxAlternatives = 3;
            r.onresult = (e) => resolve([...e.results[0]].map((a) => a.transcript));
            r.onerror = (e) => reject(new Error(e.error || "error"));
            r.onend = () => reject(new Error("no-speech"));
            r.start();
        });
    }

    /** Index of the option said (its text, its letter or a number word), or -1. */
    function match(transcripts, options) {
        const l = lang();
        for (const raw of transcripts) {
            let said = norm(raw);
            (numbers[l] || numbers.en).forEach((w, i) => { said = said.replace(new RegExp(` ${w} `, "g"), ` ${i} `); });
            const exact = options.findIndex((o) => norm(o) === said);
            if (exact >= 0) return exact;
            const contained = options.map((o, i) => [norm(o).trim(), i]).filter(([o]) => o.length > 0 && said.includes(` ${o} `)).sort((a, b) => b[0].length - a[0].length);
            if (contained.length) return contained[0][1];
            const letter = (letters[l] || letters.en).findIndex((x) => said.trim() === x || said.includes(` ${l === "en" ? "option" : "opcion"} ${x} `) || said.trim() === `${x}.`);
            if (letter >= 0 && letter < options.length) return letter;
        }
        return -1;
    }

    /** Adds a microphone button that fills a text field with what was said. */
    function dictation(button, field, onError) {
        if (!Recognition) { button.hidden = true; return; }
        button.addEventListener("click", async () => {
            button.setAttribute("aria-pressed", "true");
            button.classList.add("animate-pulse");
            try {
                const said = await listen();
                field.value = (field.value ? field.value.trimEnd() + " " : "") + said[0];
                field.dispatchEvent(new Event("input"));
                field.focus();
            } catch (e) { if (e.message !== "no-speech" && onError) onError(e); }
            button.setAttribute("aria-pressed", "false");
            button.classList.remove("animate-pulse");
        });
    }

    window.Edu = window.Edu || {};
    window.Edu.voice = { supported: !!Recognition, listen, match, dictation, norm };
})();
