// "Don't trust the AI blindly": a round of short claims to judge true or false. The claim bank and the answer are
// both decided by code (see EduVerse.Core.Tutor.PipClaims) — on purpose, nothing here is written by an AI.
(() => {
    "use strict";
    const { t, esc, api } = window.Edu;
    const card = document.getElementById("pc-card");
    if (!card) return;
    const primary = "py-2 px-4 inline-flex justify-center items-center gap-x-2 text-sm font-semibold rounded-full bg-primary text-primary-foreground hover:bg-primary-hover";
    const light = "py-2 px-4 inline-flex justify-center items-center gap-x-2 text-sm font-semibold rounded-full border border-layer-line bg-layer text-foreground shadow-2xs hover:bg-layer-hover";

    let queue = [];

    function showClaim() {
        if (queue.length === 0) {
            card.innerHTML = `<p class="text-sm font-semibold text-foreground" data-testid="pipclaims-done">🏁 ${esc(t("PipClaimsDone"))}</p>
                <button type="button" class="${primary}" id="pc-start" data-testid="pipclaims-again">🔁 ${esc(t("PipClaimsAgain"))}</button>`;
            document.getElementById("pc-start").addEventListener("click", start);
            return;
        }

        const claim = queue[0];
        card.innerHTML = `<p class="text-lg font-medium text-foreground" data-testid="pipclaims-statement">🤖 “${esc(claim.statement)}”</p>
            <div class="flex flex-wrap gap-3">
                <button type="button" class="${primary}" data-true="true" data-testid="pipclaims-true">✅ ${esc(t("PipClaimsTrue"))}</button>
                <button type="button" class="${light}" data-true="false" data-testid="pipclaims-false">❌ ${esc(t("PipClaimsFalse"))}</button>
            </div>`;
        card.querySelectorAll("[data-true]").forEach((b) => b.addEventListener("click", async () => {
            card.querySelectorAll("[data-true]").forEach((x) => (x.disabled = true));
            try {
                const r = await api("/api/pipclaims/answer", { json: { id: claim.id, true: b.getAttribute("data-true") === "true" } });
                card.innerHTML = `<p class="text-lg font-semibold text-foreground" data-testid="pipclaims-result" data-correct="${r.correct}">${r.correct ? "🎉 " + esc(t("PipClaimsCorrect")) : "🤔 " + esc(t("PipClaimsWrong"))}</p>
                    <p class="text-sm text-foreground">${esc(r.wasTrue ? t("PipClaimsWasTrue") : t("PipClaimsWasFalse"))}</p>
                    <p class="text-sm text-foreground bg-muted-1 rounded-lg p-3" data-testid="pipclaims-explanation">${esc(r.explanation)}</p>
                    ${r.reward?.xp ? `<p class="text-sm font-semibold text-foreground">⭐ +${r.reward.xp} XP</p>` : ""}
                    <button type="button" class="${primary}" id="pc-next" data-testid="pipclaims-next">➡️ ${esc(t("PipClaimsNext"))}</button>`;
                document.getElementById("pc-next").addEventListener("click", () => {
                    queue.shift();
                    showClaim();
                });
            } catch (err) {
                card.innerHTML = `<p class="text-sm text-red-700 dark:text-red-300" role="alert">${esc(err.message)}</p>
                    <button type="button" class="${light}" id="pc-start">${esc(t("PipClaimsAgain"))}</button>`;
                document.getElementById("pc-start").addEventListener("click", start);
            }
        }));
    }

    async function start() {
        card.innerHTML = `<p class="text-sm text-muted-foreground-1" aria-live="polite">⏳ ${esc(t("PipClaimsLoading"))}</p>`;
        try {
            const r = await api("/api/pipclaims/round", { method: "POST" });
            queue = r.claims;
            showClaim();
        } catch (err) {
            card.innerHTML = `<p class="text-sm text-red-700 dark:text-red-300" role="alert">${esc(err.message)}</p>
                <button type="button" class="${light}" id="pc-start">${esc(t("PipClaimsAgain"))}</button>`;
            document.getElementById("pc-start").addEventListener("click", start);
        }
    }

    document.getElementById("pc-start")?.addEventListener("click", start);
})();
