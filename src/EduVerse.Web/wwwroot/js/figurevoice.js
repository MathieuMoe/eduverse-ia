// The browser voice of a historical figure: a voice of the page language, female or male, a different one for each
// figure when the browser has several (the most natural first), with its own pitch and speed. The student can change it.
(() => {
    "use strict";
    const synth = "speechSynthesis" in window ? window.speechSynthesis : null;
    const LOCALES = { en: "en-GB", es: "es-ES", fr: "fr-FR", ca: "ca-ES" };
    const FEMALE = ["female", "woman", "zira", "hazel", "susan", "libby", "sonia", "maisie", "aria", "jenny", "samantha", "karen", "moira", "tessa", "fiona", "victoria", "helena", "laura", "elvira", "lucia", "paulina", "monica", "mónica", "marisol", "sabina", "dalia", "ximena", "elena", "hortense", "julie", "denise", "amelie", "amélie", "audrey", "aurelie", "eloise", "brigitte", "vivienne", "joana", "alba", "montserrat", "herena"];
    const MALE = ["male", "david", "mark", "george", "ryan", "guy", "james", "daniel", "alex", "fred", "thomas", "oliver", "pablo", "alvaro", "álvaro", "jorge", "raul", "raúl", "diego", "enrique", "juan", "carlos", "tomas", "paul", "henri", "claude", "remy", "rémy", "jerome", "jérôme", "enric", "jordi"];
    const QUALITY = /natural|neural|online|premium|enhanced|google/i;

    function create({ female, pitch, rate, index, key }) {
        const code = (document.documentElement.lang || "en").slice(0, 2);
        let chosen = null;
        let matched = false;
        let list = [];
        const storeKey = `figure-voice-${key}`;

        const isFemale = (v) => FEMALE.some((n) => v.name.toLowerCase().includes(n));
        const isMale = (v) => !isFemale(v) && MALE.some((n) => v.name.toLowerCase().includes(n));
        const better = (a, b) => (QUALITY.test(b.name) ? 1 : 0) - (QUALITY.test(a.name) ? 1 : 0) || a.name.localeCompare(b.name);

        function pick() {
            if (!synth) return;
            list = synth.getVoices().filter((v) => v.lang.toLowerCase().startsWith(code)).sort(better);
            let saved = null;
            try { saved = localStorage.getItem(storeKey); } catch { }
            const kind = list.filter(female ? isFemale : isMale);
            matched = kind.length > 0;
            chosen = list.find((v) => v.name === saved)
                || (kind.length ? kind[index % kind.length] : null)
                || list.filter((v) => !(female ? isMale : isFemale)(v))[index % Math.max(1, list.length)]
                || list[0] || null;
        }
        if (synth) { pick(); synth.addEventListener?.("voiceschanged", pick); }

        function speak(text, { onstart, onboundary, onend } = {}) {
            return new Promise((resolve) => {
                if (!synth) { resolve(); return; }
                synth.cancel();
                const u = new SpeechSynthesisUtterance(text);
                u.lang = chosen?.lang || LOCALES[code] || "en-GB";
                if (chosen) u.voice = chosen;
                // Without a voice of the right kind, the pitch makes the difference between figures.
                const p = matched || (chosen && (female ? isFemale(chosen) : isMale(chosen))) ? pitch : pitch * (female ? 1.25 : 0.8);
                u.pitch = Math.max(0.1, Math.min(2, p));
                u.rate = rate;
                u.onstart = () => onstart?.();
                u.onboundary = (e) => onboundary?.(e);
                u.onend = u.onerror = () => { onend?.(); resolve(); };
                synth.speak(u);
                setTimeout(() => { if (!synth.speaking) { onend?.(); resolve(); } }, 1500 + text.length * 120);
            });
        }

        /** Next voice of the language for this figure (remembered in this browser). */
        function next() {
            if (!list.length) return null;
            const i = chosen ? list.indexOf(chosen) : -1;
            chosen = list[(i + 1) % list.length];
            matched = true;
            try { localStorage.setItem(storeKey, chosen.name); } catch { }
            return chosen.name;
        }

        return { supported: !!synth, speak, next, cancel: () => synth?.cancel(), name: () => chosen?.name?.replace(/^(Microsoft|Google)\s+/, "").replace(/\s*\(.*\)$/, "").replace(/ Online.*$/, "") || "" };
    }

    window.Edu = window.Edu || {};
    window.Edu.figureVoice = create;
})();
