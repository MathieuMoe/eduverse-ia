// Ideas of the students: propose an idea and vote for the ones an adult has approved. The names of the students are never shown.
(() => {
    "use strict";
    const { t, fmt, esc, api } = window.Edu;
    const pills = {
        New: ["bg-surface text-muted-foreground-1", "⏳", "IdeaPending"],
        Approved: ["tone-sky text-foreground", "🗳️", "IdeaApproved"],
        InProgress: ["tone-sun text-foreground", "🛠️", "IdeaInProgress"],
        Available: ["tone-mint text-foreground", "✅", "IdeaAvailable"],
    };

    document.querySelectorAll("[data-ideas]").forEach((root) => {
        const kind = root.dataset.ideas;
        const list = root.querySelector("[data-idea-list]");
        const status = root.querySelector("[data-idea-status]");
        const form = root.querySelector("[data-idea-form]");

        function item(i) {
            const [cls, icon, label] = pills[i.status] || pills.New;
            const vote = i.canVote
                ? `<button type="button" class="py-1 px-2.5 inline-flex items-center gap-1 rounded-full text-xs font-semibold border ${i.voted ? "border-primary bg-primary text-primary-foreground" : "border-layer-line bg-layer text-foreground hover:bg-layer-hover"}"
                     data-vote="${i.id}" ${i.voted ? "disabled" : ""} aria-pressed="${i.voted}" title="${esc(t(i.voted ? "IdeaVoted" : "IdeaVote"))}">👍 ${i.votes}</button>`
                : `<span class="text-xs text-muted-foreground-1">👍 ${i.votes}</span>`;
            return `<li class="flex items-center gap-3 p-2.5 rounded-2xl bg-surface/60" data-testid="idea-${i.id}">
                <span class="min-w-0 flex-1"><span class="block text-sm font-medium text-foreground truncate">${esc(i.text)}${i.mine ? ` <span class="text-xs text-primary">· ${esc(t("IdeaMine"))}</span>` : ""}</span>
                <span class="mt-0.5 inline-flex items-center gap-1 py-0.5 px-2 rounded-full text-[11px] font-semibold ${cls}">${icon} ${esc(t(label))}</span></span>
                ${vote}</li>`;
        }

        async function load() {
            try {
                const data = await api(`/api/ideas/${kind}`);
                list.innerHTML = data.items.length ? data.items.map(item).join("") : `<li class="text-sm text-muted-foreground-1">${esc(t("IdeaNone"))}</li>`;
            } catch (e) {
                list.innerHTML = `<li class="text-sm text-red-700 dark:text-red-300">${esc(e.message)}</li>`;
            }
        }

        list.addEventListener("click", async (e) => {
            const b = e.target.closest("[data-vote]");
            if (!b) return;
            b.disabled = true;
            try {
                await api(`/api/ideas/vote/${b.dataset.vote}`, { method: "POST" });
                status.textContent = t("IdeaThanksVote");
                await load();
            } catch (err) {
                status.textContent = err.message;
                b.disabled = false;
            }
        });

        form.addEventListener("submit", async (e) => {
            e.preventDefault();
            const text = form.text.value.trim();
            if (text.length < 2) { status.textContent = t("Idea_TooShort"); form.text.focus(); return; }
            const button = form.querySelector("button[type=submit]");
            button.disabled = true;
            try {
                const r = await api(`/api/ideas/${kind}`, { json: { text, reason: form.reason.value.trim() || null } });
                status.textContent = r.outcome === "Exists" ? fmt(t("Idea_Exists"), r.existing)
                    : r.outcome === "Joined" ? fmt(t("Idea_Joined"), Math.max(0, (r.item?.votes ?? 1) - 1)) : t("Idea_" + r.outcome);
                status.dataset.outcome = r.outcome;
                if (r.outcome === "Created" || r.outcome === "Joined") { form.reset(); await load(); }
            } catch (err) {
                status.textContent = err.message;
            } finally {
                button.disabled = false;
            }
        });

        load();
    });
})();
