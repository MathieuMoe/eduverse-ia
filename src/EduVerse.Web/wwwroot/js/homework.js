// "Review my steps": sends the typed steps and, if the student wants, a photo of the notebook. The photo is resized and re-encoded in the
// browser (which also drops its metadata) and only sent with the explicit consent; the server keeps nothing.
(() => {
    "use strict";
    const { t, api, esc } = window.Edu;
    const $ = (id) => document.getElementById(id);
    const MAX_SIDE = 1600;
    let photo = null;

    const clearPhoto = () => {
        photo = null;
        if ($("hw-photo")) $("hw-photo").value = "";
        $("hw-preview")?.classList.add("hidden");
        $("hw-preview")?.classList.remove("flex");
    };

    $("hw-photo")?.addEventListener("change", (e) => {
        const file = e.target.files?.[0];
        if (!file) return clearPhoto();
        const url = URL.createObjectURL(file);
        const img = new Image();
        img.onload = () => {
            const scale = Math.min(1, MAX_SIDE / Math.max(img.width, img.height));
            const canvas = document.createElement("canvas");
            canvas.width = Math.round(img.width * scale);
            canvas.height = Math.round(img.height * scale);
            canvas.getContext("2d").drawImage(img, 0, 0, canvas.width, canvas.height);
            photo = canvas.toDataURL("image/jpeg", 0.85);
            $("hw-thumb").src = photo;
            $("hw-preview").classList.remove("hidden");
            $("hw-preview").classList.add("flex");
            URL.revokeObjectURL(url);
        };
        img.onerror = () => { URL.revokeObjectURL(url); clearPhoto(); };
        img.src = url;
    });
    $("hw-remove")?.addEventListener("click", clearPhoto);

    const icon = { Right: "✅", Wrong: "❌", Unchecked: "➖" };

    $("hw-form").addEventListener("submit", async (e) => {
        e.preventDefault();
        const error = $("hw-error");
        error.classList.add("hidden");
        $("hw-send").disabled = true;
        $("hw-wait").classList.remove("hidden");
        try {
            const r = await api("/api/homework/check", { json: { steps: $("hw-steps").value, image: photo, consent: !!$("hw-consent")?.checked } });
            $("hw-lines").innerHTML = r.steps.map((s) => `<li class="${s.verdict === "Wrong" ? "font-semibold text-red-700 dark:text-red-300" : "text-foreground"}" data-testid="hw-line" data-verdict="${esc(s.verdict)}"><span aria-hidden="true">${icon[s.verdict]}</span> <span class="sr-only">${esc(t("Hw" + s.verdict))}</span> ${esc(s.text)}</li>`).join("");
            $("hw-feedback").textContent = r.feedback;
            $("hw-hint").textContent = r.hint ? "💭 " + r.hint : "";
            $("hw-hint").classList.toggle("hidden", !r.hint);
            $("hw-tip").textContent = r.tip ? "👉 " + r.tip : "";
            $("hw-tip").classList.toggle("hidden", !r.tip);
            $("hw-engine").textContent = r.engine === "demo" ? t("HwCodeOnly") : r.engine + (r.fallback ? " · " + r.fallback : "");
            $("hw-result").classList.remove("hidden");
            $("hw-result").scrollIntoView({ block: "nearest" });
        } catch (err) {
            error.textContent = t("Err_" + (err.data?.code || "generic"), err.message);
            error.classList.remove("hidden");
        } finally {
            $("hw-send").disabled = false;
            $("hw-wait").classList.add("hidden");
        }
    });
})();
