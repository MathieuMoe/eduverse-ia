// Reads the discovery of a dossier aloud with the voice of the figure, the lips of the illustrated figure moving.
(() => {
    "use strict";
    document.querySelectorAll("[data-read-dossier]").forEach((button) => {
        const root = button.closest("[data-figure]");
        if (!root || !window.Edu?.figureVoice) return;
        const voice = window.Edu.figureVoice({
            female: root.dataset.female === "true", pitch: Number(root.dataset.pitch) || 1, rate: Number(root.dataset.rate) || 1,
            index: Number(root.dataset.voiceIndex) || 0, key: root.dataset.figure,
        });
        if (!voice.supported) { button.hidden = true; return; }
        const avatar = root.querySelector("[data-avatar][data-mouth]") || document.getElementById("iv-avatar");
        let timer = null;
        const talk = (on) => {
            clearInterval(timer);
            if (!avatar) return;
            avatar.classList.toggle("is-talking", on);
            avatar.dataset.mouth = "closed";
            if (on) { const s = ["open", "mid", "closed", "mid"]; let i = 0; timer = setInterval(() => { avatar.dataset.mouth = s[i++ % s.length]; }, 110); }
        };
        let playing = false;
        button.addEventListener("click", async () => {
            if (playing) { voice.cancel(); talk(false); playing = false; button.setAttribute("aria-pressed", "false"); return; }
            playing = true;
            button.setAttribute("aria-pressed", "true");
            await voice.speak(root.querySelector("[data-dossier-text]").textContent.trim(), { onstart: () => talk(true), onend: () => talk(false) });
            playing = false;
            button.setAttribute("aria-pressed", "false");
        });
    });
})();
