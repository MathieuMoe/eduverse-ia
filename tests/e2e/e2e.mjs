// End-to-end test of EduVerse AI in demo mode (no AI key): student loop, teacher follow-up, lesson pack, 4 languages,
// desktop and 390 px mobile, light and dark. Fails on console errors or horizontal overflow. Saves the README screenshots in docs/.
//
//   cd tests/e2e && npm install && npx playwright install chromium   (or set CHROME_PATH to an installed Chrome)
//   BASE_URL=http://localhost:5250 npm test        (the app must be running, on a fresh database)
import { chromium } from "playwright";
import { mkdirSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const BASE = process.env.BASE_URL || "http://localhost:5250";
const DOCS = join(dirname(fileURLToPath(import.meta.url)), "..", "..", "docs");
mkdirSync(DOCS, { recursive: true });

const browser = await chromium.launch(process.env.CHROME_PATH ? { executablePath: process.env.CHROME_PATH } : {});
const failures = [];
const check = (ok, message) => { if (!ok) failures.push(message); console.log(`${ok ? "✓" : "✗"} ${message}`); };

async function session({ width = 1440, height = 960, dark = false, mobile = false } = {}) {
    const context = await browser.newContext({ viewport: { width, height }, deviceScaleFactor: mobile ? 2 : 1, isMobile: mobile, hasTouch: mobile, colorScheme: dark ? "dark" : "light" });
    await context.addInitScript((d) => { try { localStorage.setItem("hs_theme", d ? "dark" : "light"); } catch { } }, dark);
    const page = await context.newPage();
    page.on("console", (m) => { if (m.type() === "error") failures.push(`console error on ${page.url()}: ${m.text()}`); });
    page.on("pageerror", (e) => failures.push(`page error on ${page.url()}: ${e.message}`));
    page.on("dialog", (d) => d.dismiss());
    return { context, page };
}

async function noOverflow(page, label) {
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
    check(overflow <= 1, `${label}: no horizontal overflow (${overflow}px)`);
}

// A Practice-mode question is sometimes a Duolingo-style drill instead of the usual 4-option grid: true/false,
// fill-blank, reorder, match, multi-select or listen, mixed in for every subject by ExerciseBank.AnyDrill (task #73
// extends what was, through task #72, true/false-only for Mathematics-like subjects and fill-blank/reorder/match-only
// for Language/ForeignLanguage, into a mix of all kinds for every subject; task #96 adds multi-select as a 5th, task
// #114 adds listen as a 6th — it renders with the same options grid a classic question uses, just with the prompt
// spoken instead of shown). Waits for whichever of the 7 possible renderings the page actually shows and answers it
// — a drill not necessarily correctly, since the point there is that the UI works, not the score — and returns which
// kind it saw ("classic", "truefalse", "fillblank", "reorder", "match", "multiselect" or "listen"). classicIndex
// picks the option on a classic question, true/false or listen drill (wrapped to however many buttons that
// rendering has); it's ignored for the other drill kinds, which only ever need an answer submitted.
async function answerQuizQuestion(page, classicIndex = 0) {
    const options = page.locator("[data-testid=quiz-options] button:not([disabled])");
    const trueFalse = page.locator("[data-testid=quiz-truefalse] button:not([disabled])");
    const fillBlank = page.locator("[data-testid=quiz-fillblank]");
    const reorder = page.locator("[data-testid=quiz-reorder]");
    const match = page.locator("[data-testid=quiz-match]");
    const multiSelect = page.locator("[data-testid=quiz-multiselect]");
    const listen = page.locator("[data-testid=quiz-listen]");
    await Promise.race([options.first().waitFor(), trueFalse.first().waitFor(), fillBlank.first().waitFor(), reorder.first().waitFor(), match.first().waitFor(), multiSelect.first().waitFor(), listen.first().waitFor()]);
    if (await listen.count()) {
        await page.click("#qz-listen-replay");
        await options.nth(classicIndex % (await options.count())).click();
        return "listen";
    }
    if (await fillBlank.count()) {
        await page.fill("#qz-fillblank-input", "test");
        await page.click("#qz-fillblank-check");
        return "fillblank";
    }
    if (await reorder.count()) {
        const poolSelector = "#qz-reorder-pool [data-pos]:not([disabled])";
        const total = await page.locator(poolSelector).count();
        for (let k = 0; k < total; k++) {
            await page.locator(poolSelector).first().click();
        }
        await page.click("#qz-reorder-check");
        return "reorder";
    }
    if (await match.count()) {
        const lefts = page.locator("#qz-match-left [data-left]");
        const rights = page.locator("#qz-match-right [data-right]");
        const n = await lefts.count();
        for (let k = 0; k < n; k++) {
            await lefts.nth(k).click();
            await rights.nth(k).click();
        }
        await page.click("#qz-match-check");
        return "match";
    }
    if (await multiSelect.count()) {
        await page.locator("[data-ms-index]").first().click();
        await page.click("#qz-multiselect-check");
        return "multiselect";
    }
    if (await trueFalse.count()) {
        await trueFalse.nth(classicIndex % (await trueFalse.count())).click();
        return "truefalse";
    }
    await options.nth(classicIndex % (await options.count())).click();
    return "classic";
}

const homes = { student: "Student", kid: "Student", teacher: "Teacher", director: "Institution", tutor: "Tutor" };
async function demo(page, role) {
    await page.goto(BASE + "/culture/en?returnUrl=/");
    await page.click(`[data-testid=demo-${role}]`);
    await page.waitForURL(`**/${homes[role]}`);
}

// ---------- Landing ----------
{
    const { context, page } = await session();
    await page.goto(BASE + "/culture/en?returnUrl=/");
    check(await page.locator("h1").first().isVisible(), "landing renders");
    await page.screenshot({ path: join(DOCS, "screenshot-home.png") });
    await context.close();
}

// ---------- Student: dashboard, learning loop, feedback, chat ----------
{
    const { context, page } = await session();
    await demo(page, "student");
    check(await page.locator("[data-testid=subject-card]").count() >= 1, "student sees the fractions subject card");
    check((await page.locator("[data-testid=user-name]").textContent()).trim() === "Sara", "signed in as the demo student Sara");

    await page.goto(BASE + "/Student/Learn?subject=fractions");
    await page.waitForSelector(".ln-option");
    check(await page.locator(".ln-option").count() >= 3, "an exercise with options is shown");
    check(await page.locator("#ln-map li").count() === 7, "mastery map lists the 7 concepts");

    // Answer until a wrong option shows the feedback on the misconception behind it
    let wrong = false;
    for (let i = 0; i < 6 && !wrong; i++) {
        await page.waitForSelector(".ln-option:not([disabled])");
        await page.locator(".ln-option").nth(i % 2).click();
        await page.waitForSelector("#ln-feedback [role=status]");
        wrong = await page.getByText("Why this answer is tempting").isVisible();
        if (!wrong) {
            await page.click("#ln-continue");
            await page.waitForSelector(".ln-option:not([disabled]), #ln-understood");
            if (await page.locator("#ln-understood").isVisible()) await page.click("#ln-understood");
        }
    }
    check(wrong, "a wrong answer explains the misconception behind it");

    // Socratic chat
    await page.fill("#ln-chat-input", "I think 1/2 + 1/3 = 2/5");
    await page.click("#ln-chat-send");
    await page.waitForFunction(() => document.querySelectorAll("#ln-chat .flex").length >= 2);
    check(true, "the tutor answers in the chat");
    await page.screenshot({ path: join(DOCS, "screenshot.png") });

    await page.click("#ln-continue");
    await page.waitForSelector(".ln-option, #ln-understood");
    check(true, "continue leads to the next step");

    // Explanation of a concept (method chosen by the code)
    const explained = await page.evaluate(async () => (await fetch("/api/learn/fractions/explain/add-different", { method: "POST" })).ok);
    check(explained, "explanation endpoint answers");

    await page.goto(BASE + "/Student");
    await context.close();
}

// ---------- New student: placement test ----------
{
    const { context, page } = await session();
    await page.goto(BASE + "/culture/en?returnUrl=/Account/Login");
    await page.fill("#Email", "nora@eduverse.demo");
    await page.fill("#Password", "EduVerse-demo-2026");
    await page.click("form >> button[type=submit] >> nth=0");
    await page.waitForURL("**/Student");
    await page.goto(BASE + "/Student/Learn?subject=fractions");
    await page.click("#ln-diag-start");
    let questions = 0;
    const done = () => page.getByText("Your starting point is ready").isVisible();
    const ready = () => page.locator(".ln-option:not([disabled])").or(page.getByText("Your starting point is ready")).first().waitFor();
    while (questions < 6) {
        await ready();
        if (await done()) break;
        await page.locator(".ln-option").first().click();
        await page.waitForSelector("#ln-continue");
        await page.click("#ln-continue");
        questions++;
        await ready();
        if (await done()) break;
    }
    await page.screenshot({ path: join(DOCS, "screenshot-diagnostic.png") });
    check(await page.getByText("Your starting point is ready").isVisible(), `placement test finishes after ${questions} questions`);
    await context.close();
}

// ---------- Teacher: classes, heatmap, alerts, lesson pack, lesson in 3 levels, summary ----------
{
    const { context, page } = await session();
    await demo(page, "teacher");
    check(await page.locator("[data-testid=class-card]").count() === 1, "teacher sees the demo class");
    await page.click("[data-testid=class-card]");
    await page.waitForSelector("[data-testid=heatmap]");
    check(await page.locator("[data-testid=heatmap] tbody tr").count() === 4, "heatmap shows the 4 students");
    check(await page.locator("[data-testid=alerts] li").count() >= 2, "alerts from the tutor are listed");
    await page.screenshot({ path: join(DOCS, "screenshot-teacher.png"), fullPage: true });
    // Ideas of the students: the teacher approves one so that the others can vote for it
    await page.goto(BASE + "/Moderation/Ideas");
    check(await page.locator("[data-testid=mod-ideas] > li").count() >= 10, "the teacher sees the ideas of the students with their votes");
    await page.screenshot({ path: join(DOCS, "screenshot-moderation.png"), fullPage: true });
    await page.locator("[data-testid=mod-ideas] > li:has-text('Napoleón Bonaparte') button[data-testid$='-Approved']").click();
    await page.locator("[data-testid=mod-saved]").waitFor();
    check(true, "an idea is approved in one click");
    await page.goto(BASE + "/Teacher");
    await page.screenshot({ path: join(DOCS, "screenshot-classes.png") });
    await page.goto(BASE + "/Teacher/Class?id=1");

    await page.locator("[data-testid=heatmap] tbody tr th a").first().click();
    await page.waitForSelector("#student table");
    check(true, "student history opens from the heatmap");
    await page.fill("[data-testid=teacher-note-text]", "Praise the effort, fractions need more time");
    await page.click("[data-testid=teacher-note-send]");
    await page.locator("[data-testid=teacher-note]").first().waitFor();
    check(true, "the teacher leaves a note for a student of their class");
    await page.fill("[data-testid=teacher-note-text]", "call me at bob@mail.com");
    await page.click("[data-testid=teacher-note-send]");
    await page.locator("[data-testid=teacher-note-error]").waitFor();
    check(true, "a note with contact data is refused");
    await page.locator("[data-testid=teacher-notes]").screenshot({ path: join(DOCS, "screenshot-teacher-note.png") });

    await page.goto(BASE + "/Teacher/Pack");
    await page.click("#pk-sample");
    await page.click("#pk-submit");
    await page.waitForSelector("[data-tab=Exercises]");
    await page.click("[data-tab=Slides]");
    check(await page.locator("#pk-body ol li").count() >= 2, "lesson pack generated offline with slides");
    await page.screenshot({ path: join(DOCS, "screenshot-pack.png") });

    await page.goto(BASE + "/Teacher/Lesson");
    await page.click("#ls-sample");
    await page.click("#ls-submit");
    await page.waitForSelector("#ls-body:not(.hidden)");
    check(true, "lesson in 3 levels generated offline");

    await page.goto(BASE + "/Teacher/Summary");
    await page.click("#sm-sample");
    await page.click("#sm-submit");
    await page.waitForSelector("#sm-body:not(.hidden)");
    check(true, "class summary generated offline");
    await context.close();
}

// ---------- Student platform: catalog, adaptive test, placement, arena, duel, achievements ----------
{
    const { context, page } = await session();
    await demo(page, "student");
    check(await page.locator("[data-testid=game-kpis] > div").count() === 4, "dashboard shows XP, level, streak and rank");
    check(await page.locator("[data-testid=quests] li").count() === 3, "three daily quests");
    // Daily goal and happy hour: pick "Serious" (30 XP) and an evening happy hour, saved on the server
    await page.click("[data-testid=daily-goal] label:has([data-testid=goal-30])");
    await page.selectOption("[data-testid=happy-hour-select]", "19");
    await page.click("[data-testid=goal-save]");
    await page.locator("[data-testid=daily-goal-progress]").waitFor();
    check((await page.locator("[data-testid=daily-goal-progress]").textContent()).includes("/ 30 XP"), "the daily goal is saved");
    check(await page.locator("[data-testid=happy-hour-select]").inputValue() === "19", "the happy hour is saved");
    await page.locator("[data-testid=daily-goal]").screenshot({ path: join(DOCS, "screenshot-daily-goal.png") });
    // The story of your mistakes (phase 5): the week's missed topics told as a short adventure, then a practice link
    await page.click("[data-testid=mistake-story-go]");
    await Promise.race([page.locator("[data-testid=mistake-story-text]").waitFor(), page.locator("[data-testid=mistake-story-none]").waitFor()]);
    check(await page.locator("[data-testid=mistake-story-text], [data-testid=mistake-story-none]").count() === 1, "the story of your mistakes is told (or explained why not)");
    if (await page.locator("[data-testid=mistake-story-text]").count()) {
        await page.locator("[data-testid=mistake-story]").screenshot({ path: join(DOCS, "screenshot-mistake-story.png") });
    }
    // Gem shop (phase 2): the demo badges and quests already paid Sara some gems
    const gemsBefore = +(await page.locator("[data-testid=dashboard-gems]").textContent()).replace(/\D/g, "");
    check(gemsBefore >= 50, `the demo student starts with gems from badges and quests (${gemsBefore})`);
    await page.click("[data-testid=dashboard-gems]");
    await page.locator("[data-testid=shop-items]").waitFor();
    check(await page.locator("[data-testid=shop-items] > li").count() === 6, "the shop offers six items");
    await page.click("[data-testid=shop-buy-StreakFreeze]");
    await page.locator("[data-testid=shop-notice]").waitFor();
    check(await page.locator("[data-testid=shop-notice]").getAttribute("data-notice") === "Shop_Bought", "a streak freeze can be bought with gems");
    check((await page.locator("[data-testid=shop-freezes]").textContent()).trim().startsWith("1"), "the freeze is waiting for a missed day");
    check(+(await page.locator("[data-testid=shop-gems]").textContent()).replace(/\D/g, "") === gemsBefore - 50, "the gems are spent");
    await page.screenshot({ path: join(DOCS, "screenshot-shop.png"), fullPage: true });
    // Weekly league (phase 3): the demo classmates share Sara's Bronze group; Mia (6) never competes
    await page.goto(BASE + "/Student");
    await page.click("[data-testid=dashboard-league]");
    await page.locator("[data-testid=league-table]").waitFor();
    check(await page.locator("[data-testid=league-division]").getAttribute("data-division") === "Bronze", "everyone starts in the Bronze league");
    check(await page.locator("[data-testid=league-row]").count() >= 3, "the league groups the classmates who compete");
    check(await page.locator("[data-testid=league-row][data-me='1']").count() === 1, "the student sees herself in the league table");
    // Friend quest (phase 7): team up with a classmate for a shared weekly XP target
    if (await page.locator("[data-testid=friend-start]").count()) {
        await page.click("[data-testid=friend-start]");
        await page.locator("[data-testid=friend-notice]").waitFor();
        check(await page.locator("[data-testid=friend-notice]").getAttribute("data-notice") === "Friend_Started", "a friend quest can be started with a classmate");
    }
    check(await page.locator("[data-testid=friend-pair]").count() === 1, "the friend quest shows both partners and the shared progress");
    await page.screenshot({ path: join(DOCS, "screenshot-league.png"), fullPage: true });
    // Streak at risk: an evening reminder on the dashboard when nothing was done today (depends on the clock, so only checked when shown)
    await page.goto(BASE + "/Student");
    if (await page.locator("[data-testid=streak-risk]").count()) {
        check((await page.locator("[data-testid=streak-risk]").textContent()).includes("streak"), "the streak reminder names the streak");
    }
    // Roleplay (phase 6): haggle with the Roman merchant; the missions tick in code, the character follows its script offline
    await page.goto(BASE + "/Student/Roleplay");
    check(await page.locator("[data-testid=roleplay-list] > li").count() === 8, "eight roleplays to choose from");
    await page.click("[data-testid=roleplay-market]");
    await page.locator("[data-testid=roleplay-character]").first().waitFor();
    check(await page.locator("[data-testid=roleplay-goal][data-met='0']").count() === 4, "the roleplay starts with four open missions");
    await page.fill("[data-testid=roleplay-input]", "I can pay 30 sesterces. Can you give me a discount?");
    await page.click("[data-testid=roleplay-send]");
    await page.waitForFunction(() => document.querySelectorAll("[data-testid=roleplay-goal][data-met='1']").length === 3);
    check(await page.locator("[data-testid=roleplay-character]").count() === 2, "the merchant answers each line");
    await page.fill("[data-testid=roleplay-input]", "Deal! Thank you, Marcus.");
    await page.click("[data-testid=roleplay-send]");
    await page.locator("[data-testid=roleplay-end]").waitFor();
    check(await page.locator("[data-testid=roleplay-end]").getAttribute("data-completed") === "1", "completing every mission ends the roleplay with a reward");
    await page.screenshot({ path: join(DOCS, "screenshot-roleplay.png"), fullPage: true });
    // History trips: a country's timeline walked in order; each era ends with a check written and marked by the code
    await page.goto(BASE + "/Student/Countries");
    check(await page.locator("[data-testid=country-list] > li").count() >= 4, "several countries to travel through");
    check(await page.locator("[data-testid=country-new-go]").isDisabled() && (await page.locator("[data-testid=country-new-offline]").textContent()).includes("engine"),
        "a new country needs an AI engine (offline the page says so)");
    await page.click("[data-testid=country-spain]");
    check(await page.locator("[data-testid=country-era][data-state=Open]").count() === 1, "only the first era of the timeline is open");
    // Walk Spain's whole timeline: every right answer is a fact written on the era's card (or, for "what came
    // first", the era that comes first on the timeline)
    for (let step = 0; step < 6; step++) {
        const era = page.locator("[data-testid=country-era][data-state=Open]");
        const eraText = await era.textContent();
        const timelineText = await page.locator("[data-testid=country-timeline]").textContent();
        await era.locator("[data-testid=country-check]").click();
        await page.locator("[data-testid=country-quiz]").waitFor();
        for (const set of await page.locator("[data-testid=country-quiz] fieldset").all()) {
            const legend = await set.locator("legend").textContent();
            const options = await set.locator("label").all();
            const texts = await Promise.all(options.map(async (o) => (await o.textContent()).trim()));
            let pick = 0;
            const who = legend.match(/What is (.+) remembered for\?/);
            if (legend.includes("came first")) {
                pick = texts.map((x) => timelineText.indexOf(x)).reduce((best, at, i, all) => (at >= 0 && (all[best] < 0 || at < all[best]) ? i : best), 0);
            } else if (who) {
                const card = await era.locator("li", { hasText: who[1] }).first().textContent();
                pick = Math.max(0, texts.findIndex((x) => card.includes(x)));
            } else {
                pick = Math.max(0, texts.findIndex((x) => eraText.includes(x)));
            }
            await options[pick].click();
        }
        await page.click("[data-testid=country-submit]");
        await page.locator("[data-testid=country-result]").waitFor();
        if (step === 0) {
            check(await page.locator("[data-testid=country-result]").getAttribute("data-passed") === "1", "reading the era is enough to pass its check");
            await page.screenshot({ path: join(DOCS, "screenshot-countries.png"), fullPage: true });
        }
        await page.click("[data-testid=country-continue]");
        await page.locator("[data-testid=country-timeline]").waitFor();
        if (step === 0) {
            check(await page.locator("[data-testid=country-era][data-state=Done]").count() === 1 && await page.locator("[data-testid=country-era][data-state=Open]").count() === 1, "passing an era opens the next one");
        }
    }
    check(await page.locator("[data-testid=country-era][data-state=Done]").count() === 6, "every era of Spain passed by reading the timeline");
    // Timeline challenge: put the shuffled eras back in order with the arrows → gold stamp
    const titles = await page.locator("[data-testid=country-era] h3").allTextContents();
    for (let i = 0; i < titles.length; i++) {
        const wanted = titles[i].replace("✅", "").trim();
        for (let guard = 0; guard < 8; guard++) {
            const now = (await page.locator("[data-testid=order-item]").allTextContents()).findIndex((x) => x.includes(wanted));
            if (now <= i) break;
            await page.locator("[data-testid=order-item]").nth(now).locator("button[data-move='-1']").click();
        }
    }
    await page.click("[data-testid=order-check]");
    await page.locator("[data-testid=order-status][data-won]").waitFor();
    check(await page.locator("[data-testid=order-status]").getAttribute("data-won") === "1", "the timeline challenge is won with the eras in order");
    await page.locator("[data-testid=country-order]").screenshot({ path: join(DOCS, "screenshot-country-order.png") });
    await page.goto(BASE + "/Student/Countries");
    check(await page.locator("[data-testid=passport-stamp]").count() === 1, "Spain's stamp is in the passport");
    // Learning path (phase 4): units of lesson → lesson → chest → lesson → boss; the first lesson is open
    await page.goto(BASE + "/Student/Path?area=History&level=Primary3");
    check(await page.locator("[data-testid=path-unit]").count() >= 3, "the history path has several units");
    check(await page.locator("[data-testid=path-node][data-state=Open]").count() === 1, "exactly one step of the path is open");
    check(await page.locator("[data-testid=path-node][data-kind=Boss]").count() === await page.locator("[data-testid=path-unit]").count(), "every unit ends with a boss");
    await page.screenshot({ path: join(DOCS, "screenshot-path.png") });
    await page.click("[data-testid=path-node][data-state=Open] [data-testid=path-go]");
    for (let i = 0; !(await page.locator("[data-testid=quiz-summary]").count()) && i < 8; i++) {
        await page.locator("[data-testid=quiz-options] button:not([disabled])").first().waitFor();
        await page.locator("[data-testid=quiz-options] button:not([disabled])").first().click();
        await page.locator("[data-testid=quiz-next]").waitFor();
        await page.click("[data-testid=quiz-next]");
    }
    await page.locator("[data-testid=path-back]").waitFor();
    check(await page.locator("[data-testid=path-back]").isVisible(), "a lesson of the path ends with 'continue the path'");
    await page.click("[data-testid=path-back]");
    await page.locator("[data-testid=path]").waitFor();
    check(page.url().includes("/Student/Path"), "the student goes back to the path after a lesson");
    await page.goto(BASE + "/Student");
    check(await page.locator("[data-testid=activity-calendar] [data-tip]").count() >= 35, "monthly activity calendar");
    await page.goto(BASE + "/Student/Explore");
    check(await page.locator("[data-testid=levels] li").count() === 10, "ten levels from early years to doctorate");
    check(await page.locator("[data-testid=subjects] section").count() === 10, "ten subjects in the catalog");
    await page.screenshot({ path: join(DOCS, "screenshot-explore.png") });

    // Adaptive test: answer every question, the answer key only arrives after answering
    await page.goto(BASE + "/Student/Quiz?area=Physics&level=HighSchool");
    await page.locator("[data-testid=quiz-options] button").first().waitFor();
    // The hands-free "oral exam" toggle is server-rendered hidden; quiz.js reveals it once the browser actually
    // offers both speech recognition and speech synthesis (true in this headless browser, so it should unhide).
    check(await page.locator("#qz-oral").isHidden() === false, "oral exam toggle revealed when the browser supports voice");
    // Physics mixes in all 5 drill kinds (task #72's True/False, task #73's fill-blank/reorder/match, task #96's
    // multi-select): the 4th, 7th and 10th of these 10 questions can be one instead of the usual options grid (see
    // answerQuizQuestion — task #95 tightened the frequency from every 4th answered item to every 3rd), and a drill
    // doesn't log a review entry yet (task #69's documented limitation), so only 7 of the 10 questions below are
    // expected back in the review list.
    let trickShown = false;
    for (let i = 0; i < 10; i++) {
        await answerQuizQuestion(page, i % 4);
        await page.locator("[data-testid=quiz-next]").waitFor();
        if (!trickShown && await page.locator("[data-testid=quiz-trick]").count()) {
            // Phase 5: a wrong classic answer offers a memory trick (offline here: a technique filled with the real answer)
            await page.click("[data-testid=quiz-trick]");
            await page.waitForFunction(() => (document.querySelector("[data-testid=quiz-trick-text]")?.textContent || "").length > 40);
            trickShown = true;
        }
        if (i === 1) {
            check(await page.locator("[data-state=right]").count() === 1, "the right option is shown after answering");
            await page.screenshot({ path: join(DOCS, "screenshot-quiz.png") });
        }
        await page.click("[data-testid=quiz-next]");
    }
    await page.locator("[data-testid=quiz-summary]").waitFor();
    check(+(await page.locator("[data-testid=quiz-xp]").textContent()) >= 10, "XP won during the test");
    // End-of-lesson screens (phase 1 of the Duolingo pass): stat tiles, daily goal ring and quests, all from the server
    await page.locator("[data-testid=lesson-goal]").waitFor();
    await page.locator("[data-testid=lesson-mascot]").waitFor();
    check((await page.locator("[data-testid=lesson-mascot]").textContent()).trim().length > 10, "the Tutor reacts to the finished lesson");
    check(trickShown, "a wrong answer offers a memory trick");
    check(await page.locator("[data-testid=lesson-end] #le-time").textContent() !== "0", "the end-of-lesson screen shows the time spent");
    check(await page.locator("[data-testid=lesson-quests] li").count() === 3, "the end-of-lesson screen shows the day's quests");
    check(await page.locator("[data-testid=lesson-league]").count() === 1, "the end-of-lesson screen shows the place in the weekly league");
    await page.waitForTimeout(1200); // let the count-up and the goal ring finish for the screenshot
    await page.screenshot({ path: join(DOCS, "screenshot-lesson-end.png") });
    check(await page.locator("#qz-sound").getAttribute("aria-pressed") === "true", "sound effects are on by default");
    await page.click("#qz-sound");
    check(await page.locator("#qz-sound").getAttribute("aria-pressed") === "false", "sound effects can be turned off");
    await page.click("#qz-sound");
    await page.click("[data-testid=quiz-review]");
    await page.locator("[data-testid=review]").waitFor();
    check(await page.locator("[data-testid=review] > li").count() === 7, "the finished test is saved with its classic questions for review");
    await page.screenshot({ path: join(DOCS, "screenshot-history.png") });
    await page.goto(BASE + "/Student/History");
    check(await page.locator("[data-testid=history] li").count() >= 9, "list of tests done");

    // Changing the level applies it to every subject
    await page.goto(BASE + "/Student/Explore?level=HighSchool");
    await page.click("[data-testid=apply-level] button");
    await page.waitForLoadState("networkidle");
    const levels = await page.locator("[data-testid=test-level]").allTextContents();
    check(levels.length === 10 && levels.every((l) => l.trim() === "High school"), "the level applies to all the subjects");
    check(await page.locator("[data-testid=apply-level]").count() === 0, "the level shown is now the level of study");
    await page.goto(BASE + "/Student/Explore?level=Primary3");
    await page.click("[data-testid=apply-level] button");
    await page.waitForLoadState("networkidle");

    // Placement test finds a level. Task #74 mixes drills into Placement too (never true/false — see
    // ExerciseBank.AnyDrill's allowTrueFalse — but fill-blank/reorder/match can appear), so answer via the
    // same generic helper the Practice-mode drills use instead of assuming the classic options grid.
    await page.goto(BASE + "/Student/Quiz?area=Mathematics&mode=Placement");
    for (let i = 0; i < 10; i++) {
        const done = page.locator("[data-testid=quiz-summary]");
        if (await done.count()) break;
        const kind = await answerQuizQuestion(page, 0);
        check(kind !== "truefalse", "no true/false drill appears during a Placement test");
        await page.locator("[data-testid=quiz-next]").waitFor();
        await page.click("[data-testid=quiz-next]");
    }
    await page.locator("[data-testid=placed-level]").waitFor();
    check((await page.locator("[data-testid=placed-level]").textContent()).length > 3, "placement test gives a level");

    // Arena: ranking, teams, the duel Alex sent to Sara
    await page.goto(BASE + "/Student/Arena");
    check(await page.locator("[data-testid=leaderboard] ol li").count() === 4, "class ranking with nicknames");
    check(!(await page.locator("[data-testid=leaderboard]").textContent()).includes("Sara"), "real names are never shown in the ranking");

    // A student can pick their own avatar and nickname (never free text: always one of the safe options).
    const nicknameBefore = await page.locator("[data-testid=nickname]").textContent();
    await page.locator('[data-testid=avatar-option][value="7"]').check({ force: true });
    await page.locator('[data-testid=nickname-option][value="2"]').check({ force: true });
    await page.click('[data-testid=arena-settings] button[type=submit]');
    await page.locator("[data-testid=nickname]").waitFor();
    const nicknameAfter = await page.locator("[data-testid=nickname]").textContent();
    check(nicknameAfter !== nicknameBefore, "picking a new avatar and nickname changes what's shown");
    check(await page.locator('[data-testid=avatar-option][value="7"]').isChecked(), "the picked avatar stays selected after saving");

    await page.screenshot({ path: join(DOCS, "screenshot-arena.png") });
    await page.click("[data-testid=play-duel]");
    // Task #74 mixes drills into a Duel too (any of the 5 kinds), so answer via the generic helper here as well.
    for (let i = 0; i < 5; i++) {
        await answerQuizQuestion(page, 0);
        await page.locator("[data-testid=quiz-next]").waitFor();
        await page.click("[data-testid=quiz-next]");
    }
    await page.locator("[data-testid=quiz-summary]").waitFor();
    check(/won|lost|draw/i.test(await page.locator("[data-testid=quiz-summary]").textContent()), "duel result when both played");
    // Daily review: hints in a ladder and confidence before answering
    await page.goto(BASE + "/Student");
    await page.click("[data-testid=review-card]");
    await page.locator("[data-testid=quiz-options] button").first().waitFor();
    await page.click("[data-testid=hint]");
    await page.locator("[data-state=removed]").first().waitFor();
    check(await page.locator("[data-state=removed]").count() === 1, "first hint removes one wrong option");
    await page.click("[data-testid=hint]");
    await page.click("[data-testid=hint]");
    await page.locator("#qz-hint-text:not(.hidden)").waitFor();
    check(await page.locator("[data-state=removed]").count() === 2 && await page.locator("[data-testid=hint]").isDisabled(), "50/50 then the method, three hints at most");
    await page.click("[data-confidence='2']");
    await page.screenshot({ path: join(DOCS, "screenshot-review.png") });
    for (let i = 0; i < 10; i++) {
        if (await page.locator("[data-testid=quiz-summary]").count()) break;
        await page.locator("[data-testid=quiz-options] button:not([disabled])").first().waitFor();
        await page.locator("[data-testid=quiz-options] button:not([disabled])").first().click();
        await page.locator("[data-testid=quiz-next]").waitFor();
        await page.click("[data-testid=quiz-next]");
    }
    await page.locator("[data-testid=quiz-summary]").waitFor();
    check((await page.locator("[data-testid=quiz-summary]").textContent()).includes("Review done"), "daily review finished");

    // Evo's notebook: Evo makes Sara's own mistakes
    await page.goto(BASE + "/Student");
    await page.click("[data-testid=notebook-card]");
    await page.locator("[data-testid=evo-answer]").waitFor();
    check(await page.locator("[data-testid=quiz-options] button").count() === 4, "Evo's notebook asks what Evo did wrong");
    await page.screenshot({ path: join(DOCS, "screenshot-evo.png") });
    for (let i = 0; i < 6; i++) {
        await page.locator("[data-testid=quiz-options] button").first().waitFor();
        await page.locator("[data-testid=quiz-options] button").first().click();
        await page.locator("[data-testid=quiz-next]").waitFor();
        await page.click("[data-testid=quiz-next]");
    }
    await page.locator("[data-testid=quiz-summary]").waitFor();

    // Check Evo in any subject of the catalog
    await page.goto(BASE + "/Student/Explore");
    await page.click("[data-testid=evo-History]");
    await page.locator("[data-testid=evo-answer]").waitFor();
    check((await page.locator("[data-testid=quiz-options]").textContent()).includes("Tutor is right"), "check the Tutor offers \"The Tutor is right\"");

    // Adventure: doors, hearts and a friendly guardian, story written offline
    await page.goto(BASE + "/Student/Adventures");
    await page.locator("label:has([data-testid=theme-Jungle])").click();
    await page.selectOption("#adv-area", "Geography");
    await page.click("[data-testid=start-adventure]");
    await page.locator("[data-testid=adventure-map]").waitFor();
    check((await page.locator("[data-testid=hearts]").textContent()).includes("❤️"), "the adventure shows the map and the hearts");
    check((await page.locator("[data-testid=story-line]").textContent()).length > 10, "each door has a line of the story");
    await page.screenshot({ path: join(DOCS, "screenshot-adventure.png") });
    // Task #74 mixes drills into Adventure too (any of the 5 kinds, forced to boss difficulty once InBoss), so
    // answer via the generic helper rather than assuming the classic options grid on every door/boss turn.
    for (let i = 0; i < 20; i++) {
        if (await page.locator("[data-testid=quiz-summary]").count()) break;
        await answerQuizQuestion(page, 0);
        await page.locator("[data-testid=quiz-next]").waitFor();
        await page.click("[data-testid=quiz-next]");
    }
    await page.locator("[data-testid=adventure-end]").waitFor();
    check(await page.locator("[data-testid=adventure-end]").isVisible(), "the adventure ends with the story's ending");

    // Short survey at the end of the adventure, then ideas: propose the next world and vote
    await page.click("[data-testid=rate-send]");
    check((await page.locator("#qz-rate-status").textContent()).length > 0, "the rating asks for the stars first");
    await page.locator("[data-testid=rate-star-4]").check({ force: true });
    await page.click("[data-testid=rate-send]");
    await page.locator("[data-testid=rate-thanks]").waitFor();
    check(true, "the adventure is rated in a few seconds");
    await page.click("[data-testid=rate-thanks] a");
    await page.click("[data-testid=ideas-toggle-Adventure]"); // ideas are tucked behind a small round icon: open it first
    await page.locator("[data-testid=idea-list-Adventure] li[data-testid]").first().waitFor();
    await page.fill("[data-testid=idea-text-Adventure]", "Volcanes");
    await page.click("[data-testid=idea-send-Adventure]");
    await page.locator("[data-ideas=Adventure] [data-idea-status][data-outcome=Created]").waitFor();
    // The status text is stamped before the list is reloaded (see ideas.js), so wait for the
    // list item itself rather than racing the reload right after the status appears.
    await page.locator("[data-testid=idea-list-Adventure] li:has-text('Volcanes')").waitFor();
    check(await page.locator("[data-testid=idea-list-Adventure] li:has-text('Volcanes')").count() === 1, "a student proposes the next adventure and sees it waiting for an adult");
    const pirates = page.locator("[data-testid=idea-list-Adventure] li:has-text('Piratas') [data-vote]");
    const votes = parseInt((await pirates.textContent()).replace(/\D/g, ""), 10);
    await pirates.click();
    await page.locator("[data-testid=idea-list-Adventure] li:has-text('Piratas') [data-vote][aria-pressed=true]").waitFor();
    check(parseInt((await page.locator("[data-testid=idea-list-Adventure] li:has-text('Piratas') [data-vote]").textContent()).replace(/\D/g, ""), 10) === votes + 1, "a vote is added to an approved idea, once");
    await page.locator("[data-testid=ideas-menu-Adventure]").screenshot({ path: join(DOCS, "screenshot-ideas.png") });
    await page.goto(BASE + "/Student/Interview");
    await page.click("[data-testid=ideas-toggle-Figure]");
    await page.locator("[data-testid=idea-list-Figure] li[data-testid]").first().waitFor();
    check(await page.locator("[data-testid=idea-list-Figure] li:has-text('Napoleón Bonaparte')").count() === 1, "the figure approved by the teacher is open to votes");
    await page.fill("[data-testid=idea-text-Figure]", "Newton");
    await page.click("[data-testid=idea-send-Figure]");
    await page.locator("[data-ideas=Figure] [data-idea-status][data-outcome=Exists]").waitFor();
    check((await page.locator("[data-ideas=Figure] [data-idea-status]").textContent()).includes("Isaac Newton"), "a figure that already exists is not proposed again");

    // Evo, the tutor of every page: knows the progress and proposes actions
    await page.goto(BASE + "/Student/Explore");
    await page.click("[data-testid=companion-open]");
    await page.locator("[data-testid=companion-reply]").first().waitFor();
    check(await page.locator("[data-testid=companion] [data-testid=companion-actions] a").count() > 0, "Evo greets with ideas computed from the progress");
    await page.click("#companion-quick [data-ask]");
    await page.waitForFunction(() => document.querySelectorAll("[data-testid=companion-reply]").length >= 2 && document.getElementById("companion").dataset.busy === "false");
    await page.fill("[data-testid=companion-text]", "How am I doing?");
    await page.click("[data-testid=companion-send]");
    await page.waitForFunction(() => document.querySelectorAll("[data-testid=companion-reply]").length >= 3 && document.getElementById("companion").dataset.busy === "false");
    check((await page.locator("[data-testid=companion-reply]").last().textContent()).includes("streak"), "Evo knows the level, the streak and the weak topics");
    await page.screenshot({ path: join(DOCS, "screenshot-companion.png") });

    // The hub: "A tu manera" lets the student pick a metaphor before asking, without leaving the same panel/thread
    const repliesBeforeStyle = await page.locator("[data-testid=companion-reply]").count();
    await page.click("[data-testid=companion-mode-style]");
    check(await page.locator("[data-testid=companion-style-picker]").isVisible(), "the style picker shows once the 'your way' tab is chosen");
    await page.click('[data-style="recipe"]');
    check(await page.locator("[data-testid=companion-style-badge]").isVisible(), "picking a style shows its badge and hides the picker");
    await page.fill("[data-testid=companion-text]", "Explain fractions to me");
    await page.click("[data-testid=companion-send]");
    await page.waitForFunction((n) => document.querySelectorAll("[data-testid=companion-reply]").length > n + 1 && document.getElementById("companion").dataset.busy === "false", repliesBeforeStyle);
    await page.click("#companion-style-off");
    check(await page.locator("[data-testid=companion-style-badge]").isHidden(), "removing the style clears it and returns to the plain chat");
    await page.screenshot({ path: join(DOCS, "screenshot-companion-style.png") });

    await page.goto(BASE + "/Student/Achievements");
    await page.click("[data-testid=companion-open]");
    await page.waitForFunction(() => document.querySelectorAll("#companion-chat p").length >= 5);
    check(true, "the conversation follows the student to another page");
    await page.keyboard.press("Escape");
    check(await page.locator("[data-testid=companion]").isHidden(), "Esc closes the panel");

    // Learn starts with a catalog: every subject at the student's level, the own subjects and the trades
    await page.goto(BASE + "/Student/Learn");
    check(await page.locator("[data-testid=learn-subjects] a").count() >= 10, "learn offers every subject of the catalog");
    await page.screenshot({ path: join(DOCS, "screenshot-learn-catalog.png"), fullPage: true });
    await page.click("[data-testid^=learn-area-History]");
    await page.locator("#ln-map li").first().waitFor();
    check(await page.locator("#ln-map li").count() >= 1, "history is learned with the tutor, topic by topic");

    // Trades and practical skills, with the science behind and an internal certificate
    await page.goto(BASE + "/Student/Learn");
    await page.click("[data-testid=learn-tab-trades]");
    await page.locator("[data-testid=trades]").waitFor();
    check(await page.locator("[data-testid=trades] article").count() === 10, "ten trades, from electricity to beekeeping");
    await page.screenshot({ path: join(DOCS, "screenshot-trades.png"), fullPage: true });
    await page.click("[data-testid=learn-tab-hobbies]");
    await page.locator("[data-testid=trade-trade-chess]").waitFor();
    check(await page.locator("[data-testid=trades] article").count() === 6, "six hobbies in their own tab: astronomy, music, magic, survival, chess, dinosaurs");
    await page.screenshot({ path: join(DOCS, "screenshot-hobbies.png"), fullPage: true });
    await page.click("[data-testid=learn-trade-astronomy]");
    await page.locator("#ln-map li").first().waitFor();
    check(await page.locator("[data-testid=breadcrumb] a[href='/Student/Learn?tab=hobbies']").count() >= 1, "a hobby is learned with the tutor and its breadcrumb goes back to the hobbies");
    await page.goto(BASE + "/Student/Certificate?subject=trade-garden");
    check((await page.locator("[data-testid=certificate-code]").textContent()).trim() === "EV-GARDEN26", "the certificate of the garden shows its code");
    await page.screenshot({ path: join(DOCS, "screenshot-certificate.png") });
    await page.goto(BASE + "/Student/Learn?subject=trade-electricity");
    await page.locator("[data-testid=ln-certificate]").waitFor();
    check(true, "a trade is learned with the tutor and shows the certificate goal");

    // Breadcrumb at the top of the pages: back button and navigable path
    await page.goto(BASE + "/Student/Dossier?figure=darwin");
    check((await page.locator("[data-testid=breadcrumb]").textContent()).includes("Charles Darwin"), "the breadcrumb shows the path of the page");
    await page.click("[data-testid=breadcrumb-back]");
    await page.waitForURL("**/Student/Interview?figure=darwin");
    check(true, "the back button of the breadcrumb goes up one level");

    // Dossier of a figure: discovery, dates, evidence
    await page.goto(BASE + "/Student/Dossier?figure=galileo");
    await page.locator("[data-testid=dossier] [data-discovery] svg").waitFor();
    check(await page.locator("[data-testid=dossier] ol li").count() >= 5, "the dossier has the life in dates");
    await page.screenshot({ path: join(DOCS, "screenshot-dossier.png"), fullPage: true });

    // Some questions come with a picture drawn in the browser
    await page.goto(BASE + "/Student/Quiz?area=Mathematics&level=Primary3&mode=Practice");
    await page.locator("[data-testid=quiz-diagram] svg").waitFor();
    check(true, "a question with a picture (diagram)");
    await page.screenshot({ path: join(DOCS, "screenshot-diagram.png") });

    // Explain it to Evo: the rubric finds the key ideas round by round
    await page.goto(BASE + "/Student/Teach");
    check(await page.locator("[data-testid=teach-concepts] a").count() === 8, "eight concepts to explain to Evo");
    await page.click("[data-testid=teach-photosynthesis]");
    await page.fill("#teach-text", "Plants use the light of the sun to make their food.");
    await page.click("[data-testid=teach-send]");
    await page.locator("[data-idea=light].tone-mint").waitFor();
    check(await page.locator("[data-testid=teach-ideas] .tone-mint").count() === 2, "Evo understood two ideas and asks about the missing one");
    await page.fill("#teach-text", "They take water and carbon dioxide, and they give off oxygen.");
    await page.click("[data-testid=teach-send]");
    await page.locator("[data-testid=teach-end]").waitFor();
    check((await page.locator("[data-testid=teach-end]").textContent()).includes("understood everything"), "Evo learned all the key ideas");
    await page.screenshot({ path: join(DOCS, "screenshot-teach.png") });

    // Interview (simulation) and its comprehension check
    await page.goto(BASE + "/Student/Interview");
    await page.click("[data-testid=figure-curie]");
    check(await page.locator("[data-testid=simulation-notice]").isVisible(), "the interview says it is a simulation");
    check(await page.locator("[data-testid=iv-avatar] svg .av-mouth-open").count() === 1, "the illustrated figure has a mouth that can open");
    check(await page.locator("aside[data-theme=chemistry]").count() === 1, "Marie Curie's card has the chemistry-era backdrop");
    check(await page.locator("#iv-root").getAttribute("data-area") === "chemistry", "the figure's area is exposed to JS for the themed intro flourish");
    await page.click("[data-testid=iv-voice]"); // no audio in the headless browser
    await page.click("[data-testid=iv-start]");
    await page.locator("[data-testid=iv-answer]").first().waitFor();
    const mouths = new Set();
    for (let i = 0; i < 6; i++) { mouths.add(await page.locator("[data-testid=iv-avatar]").getAttribute("data-mouth")); await page.waitForTimeout(60); }
    check(mouths.size > 1, "the lips move while the figure speaks");
    const suggestionCount = await page.locator("#iv-suggestions [data-question]").count();
    check(await page.locator("#iv-suggestions p.uppercase").count() >= 2, "the suggested questions are grouped under category headers");
    for (const [i, b] of (await page.locator("#iv-suggestions [data-question]").all()).entries()) {
        await page.locator("#iv-root[data-busy=false]").waitFor();
        await b.click();
        await page.waitForFunction((n) => document.querySelectorAll("[data-testid=iv-answer]").length >= n, i + 2);
    }
    await page.locator("#iv-root[data-busy=false]").waitFor();
    await page.locator("[data-testid=iv-check]").waitFor();
    check(await page.locator("[data-testid=iv-answer]").count() === suggestionCount + 1, "the figure greets and answers the suggested questions offline");
    check(await page.locator("#iv-meter-fill").evaluate((el) => el.style.width) === "100%", "the curiosity meter fills up once every suggested question is asked");
    check(await page.locator("#iv-meter").getAttribute("aria-valuenow") === "100", "the curiosity meter reports 100% once complete");
    check(await page.locator(".iv-path-dot.is-lit").count() === suggestionCount, "every milestone on the life-path lights up alongside the curiosity meter");
    await page.locator("[data-testid=iv-riddle-option]").first().waitFor();
    check(await page.locator("[data-testid=iv-riddle-option]").count() >= 2, "the figure poses a riddle with several options once everything is discovered");
    await page.locator("[data-testid=iv-riddle-option]").first().click();
    await page.locator("[data-testid=iv-postcard]").waitFor();
    check(await page.locator("[data-testid=iv-postcard]").isVisible(), "a collectible postcard caps the interview once the riddle is answered");
    check(await page.locator("[data-testid=iv-postcard-download]").isVisible(), "the postcard offers a download button");
    for (let i = 0; i < 2; i++) {
        await page.locator("[data-testid=iv-myth-option]:not([disabled])").first().waitFor();
        await page.locator("[data-testid=iv-myth-option]:not([disabled])").first().click();
    }
    check(await page.locator("[data-testid=iv-myth-option]").count() === 4, "the interview ends with a two-item true-or-false myth-busting round");
    check(await page.locator("[data-testid=iv-myth-option]:not([disabled])").count() === 0, "every myth claim gets answered and locked in");
    await page.screenshot({ path: join(DOCS, "screenshot-interview.png") });
    await page.click("[data-testid=iv-tab-dossier]");
    await page.locator("#iv-panel-dossier [data-discovery] svg").waitFor();
    check(await page.locator("#iv-panel-dossier [data-testid=dossier-source]").count() >= 1, "the dossier shows the discovery and links to the historical source");
    await page.click("#iv-tab-chat");
    await page.click("[data-testid=iv-check]");
    for (let i = 0; i < 3; i++) {
        await page.locator("[data-testid=quiz-options] button").first().waitFor();
        await page.locator("[data-testid=quiz-options] button").first().click();
        await page.locator("[data-testid=quiz-next]").waitFor();
        await page.click("[data-testid=quiz-next]");
    }
    await page.locator("[data-testid=quiz-summary]").waitFor();
    check(await page.locator("[data-testid=quiz-summary]").isVisible(), "the interview comprehension check ends on a summary screen");

    await page.goto(BASE + "/Student/Album");
    check(await page.locator("[data-testid=album-card-curie][data-done='1']").count() === 1, "completing a figure's riddle unlocks its card in the album");
    check(await page.locator("[data-testid=album-grid] [data-done='0']").count() >= 1, "other figures stay locked in the album until their own interview is completed");
    await page.screenshot({ path: join(DOCS, "screenshot-album.png") });

    await page.goto(BASE + "/Student/Timeline");
    check(await page.locator("[data-testid=timeline-empty]").isVisible(), "the timeline challenge asks for at least two completed figures before it unlocks");

    // Complete a second figure (Newton) so the timeline challenge has enough figures to actually play.
    await page.goto(BASE + "/Student/Interview?figure=newton");
    await page.click("[data-testid=iv-start]");
    await page.locator("[data-testid=iv-answer]").first().waitFor();
    for (const b of await page.locator("#iv-suggestions [data-question]").all()) {
        await page.locator("#iv-root[data-busy=false]").waitFor();
        await b.click();
        await page.locator("#iv-root[data-busy=false]").waitFor();
    }
    await page.locator("[data-testid=iv-riddle-option]").first().waitFor();
    await page.locator("[data-testid=iv-riddle-option]").first().click();
    await page.locator("[data-testid=iv-postcard]").waitFor();
    for (let i = 0; i < 2; i++) {
        await page.locator("[data-testid=iv-myth-option]:not([disabled])").first().waitFor();
        await page.locator("[data-testid=iv-myth-option]:not([disabled])").first().click();
    }

    await page.goto(BASE + "/Student/Timeline");
    check(await page.locator("[data-testid=timeline-game]").isVisible(), "the timeline challenge unlocks once two figures are completed");
    const tlCount = await page.locator("[data-testid=tl-figure]").count();
    check(tlCount === 2, "the timeline challenge lists exactly the completed figures");
    for (let i = 0; i < tlCount; i++) {
        await page.locator("[data-testid=tl-figure]:not([disabled])").first().click();
    }
    await page.click("[data-testid=tl-check]");
    check(await page.locator("[data-testid=tl-result]").isVisible(), "checking the timeline order shows a result");
    check(await page.locator("[data-testid=tl-slot][data-state]").count() === tlCount, "every placed figure is marked right or wrong against the real chronology");
    await page.screenshot({ path: join(DOCS, "screenshot-timeline.png") });

    await page.goto(BASE + "/Student/Album");
    check(await page.locator("[data-testid='connection-curie-newton'][data-unlocked='1']").count() === 1, "a curated connection unlocks once both of its figures are completed");
    check(await page.locator("[data-testid=album-connections] [data-unlocked='0']").count() >= 1, "connections involving an uncompleted figure stay locked");

    await page.goto(BASE + "/Student/Achievements");
    check(await page.locator("[data-testid=badges] li").count() === 31, "all badges with their progress");
    await page.screenshot({ path: join(DOCS, "screenshot-achievements.png"), fullPage: true });
    await page.goto(BASE + "/Student");
    check((await page.locator("[data-testid=curiosity]").count()) === 1, "what I want to know widget is on the dashboard");
    await page.screenshot({ path: join(DOCS, "screenshot-student.png") });
    await context.close();
}

// ---------- Kids mode (6 years old) ----------
{
    const { context, page } = await session();
    await demo(page, "kid");
    // Landing on /Student after sign-in: a playful home with no XP, rankings or percentages to read.
    await page.waitForLoadState("networkidle");
    check(await page.locator("[data-testid=kid-greeting]").count() === 1, "kid home shows the mascot greeting, not the adult dashboard");
    check(await page.locator("[data-testid=kid-subjects]").count() === 1, "kid home shows subject tiles to play");
    check(await page.locator("[data-testid=game-kpis]").count() === 0, "kid home hides the XP/rank/streak-number KPIs");
    await page.screenshot({ path: join(DOCS, "screenshot-kids-home.png") });

    await page.goto(BASE + "/Student/Achievements");
    await page.waitForLoadState("networkidle");
    check(await page.locator("[data-testid=kid-badges]").count() === 1, "kid achievements is a sticker shelf");
    check(await page.locator("[data-testid=badges]").count() === 0, "kid achievements hides the adult badge grid with percentages");
    await page.screenshot({ path: join(DOCS, "screenshot-kids-achievements.png") });

    await page.goto(BASE + "/Student/Quiz?area=Mathematics&level=EarlyYears");
    await page.locator("[data-testid=quiz-options] button").first().waitFor();
    check(await page.locator("#qz-root.kids").count() === 1, "kids mode for early years");
    const height = (await page.locator("[data-testid=quiz-options] button").first().boundingBox()).height;
    check(height >= 70, "big buttons for young children");
    check((await page.locator("#qz-speak").getAttribute("aria-pressed")) === "true", "questions read aloud by default");
    await page.screenshot({ path: join(DOCS, "screenshot-kids.png") });
    await context.close();
}

// ---------- Institution: dashboard, dropout radar with a family message, quality and plan ----------
{
    const { context, page } = await session();
    await demo(page, "director");
    check(await page.locator("[data-testid=institution-classes] tbody tr").count() === 2, "director sees the 2 classes of the academy");
    await page.screenshot({ path: join(DOCS, "screenshot-institution.png") });
    await page.goto(BASE + "/Institution/Risk");
    check(await page.locator("[data-testid=risk-list] li").count() === 9, "dropout radar lists the 9 students");
    await page.locator("[data-family]").first().click();
    await page.click("#family-submit");
    await page.waitForSelector("#family-result:not(.hidden)");
    check((await page.locator("#family-result").innerText()).includes("Omar"), "family message written from the measured facts");
    await page.screenshot({ path: join(DOCS, "screenshot-risk.png") });
    await page.goto(BASE + "/Institution/Quality");
    check(await page.locator("[data-testid=methods] li").count() === 5, "quality shows the 5 explanation methods");
    await context.close();
}

// ---------- Marketplace: recommendations, slots, booking, simulated payment, cancellation ----------
{
    const { context, page } = await session();
    await demo(page, "student");
    await page.goto(BASE + "/Marketplace");
    check(await page.getByText("Recommended for you").isVisible(), "the student gets recommended teachers");
    check(await page.locator("[data-testid=tutor-card]").count() >= 6, "the marketplace lists the teachers");
    await page.screenshot({ path: join(DOCS, "screenshot-marketplace.png") });
    await page.goto(BASE + "/Marketplace?subject=french");
    check(await page.locator("[data-testid=tutor-card]").count() === 1, "filter by subject");
    await page.locator("[data-testid=tutor-card]").first().click();
    await page.waitForSelector("[data-testid=slots]");
    await page.locator("[data-testid=slots] label").first().click();
    await page.fill("#book-note", "I have a DELF exam soon.");
    await page.click("[data-testid=book]");
    await page.waitForURL("**/Marketplace/Bookings**");
    check(await page.locator("[data-testid=pay]").count() === 1, "booking waits for the payment");
    await page.click("[data-testid=pay]");
    await page.waitForLoadState("networkidle");
    check(await page.getByText("Payment simulated").isVisible(), "simulated payment confirms the lesson");
    await page.screenshot({ path: join(DOCS, "screenshot-bookings.png") });
    await context.close();
}

// ---------- Private teacher: earnings, bookings, profile and availability ----------
{
    const { context, page } = await session();
    await demo(page, "tutor");
    check(await page.locator("[data-testid=tutor-upcoming] li").count() >= 2, "private teacher sees the upcoming lessons");
    await page.screenshot({ path: join(DOCS, "screenshot-tutor.png") });
    await page.goto(BASE + "/Tutor/Profile");
    await page.selectOption("#from-Friday", "16");
    await page.selectOption("#to-Friday", "18");
    await page.click("button[type=submit]:has-text('Save')");
    await page.waitForLoadState("networkidle");
    check(await page.getByText("Profile saved.").isVisible() && (await page.inputValue("#from-Friday")) === "16", "availability is saved");
    await context.close();
}

// ---------- Interface options: collapsible menu, display menu, quick navigation, focus mode ----------
{
    const { context, page } = await session();
    await demo(page, "student");
    const width = () => page.evaluate(() => document.getElementById("hs-application-sidebar").getBoundingClientRect().width);
    const expanded = await width();
    await page.click("[data-ui-toggle=sidebar]");
    await page.waitForTimeout(400);
    check(await width() < expanded / 2, `sidebar collapses to icons (${Math.round(expanded)} → ${Math.round(await width())} px)`);
    await page.reload();
    check(await page.evaluate(() => document.documentElement.classList.contains("sb-collapsed")), "collapsed sidebar is remembered after a reload");
    await page.keyboard.press("[");
    await page.waitForTimeout(400);
    check(!(await page.evaluate(() => document.documentElement.classList.contains("sb-collapsed"))), "the [ shortcut expands it again");

    // Each nav item's own height, not padded out by its still-in-flow (pre-hover) tooltip bubble.
    const navGaps = await page.evaluate(() => [...document.querySelectorAll("#hs-application-sidebar li.hs-tooltip")]
        .map((li) => li.getBoundingClientRect().height - li.querySelector("a").getBoundingClientRect().height));
    check(navGaps.every((gap) => gap < 1), `sidebar nav items have no leftover tooltip gap (${navGaps.map((g) => Math.round(g)).join(",")})`);

    await page.click("[data-testid=display-menu]");
    await page.click("[data-ui=text][data-value=l]");
    check(await page.evaluate(() => getComputedStyle(document.documentElement).fontSize) === "17.5px", "text size L enlarges the whole interface");
    await page.click("[data-ui=accent][data-value=teal]");
    check(await page.evaluate(() => document.documentElement.dataset.accent) === "teal", "accent colour changes");
    await page.click("[data-ui=theme][data-value=dark]");
    check(await page.evaluate(() => document.documentElement.classList.contains("dark")), "theme can be chosen in the display menu");
    await page.click("[data-ui-reset]");
    check(await page.evaluate(() => !document.documentElement.dataset.accent && document.documentElement.style.fontSize === ""), "reset restores the defaults");

    await page.keyboard.press("Escape");
    await page.keyboard.press("Control+k");
    await page.waitForSelector("#palette:not(.hidden)");
    await page.keyboard.type("new subj");
    await page.keyboard.press("Enter");
    await page.waitForURL("**/Student/NewSubject");
    check(true, "Ctrl+K quick navigation opens a page");

    await page.goto(BASE + "/Student/Learn?subject=fractions");
    await page.waitForSelector(".ln-option, #ln-understood");
    await page.click("[data-testid=focus-toggle]");
    check(await page.evaluate(() => getComputedStyle(document.getElementById("hs-application-sidebar")).display === "none"), "focus mode hides the menu and the panels");
    await page.screenshot({ path: join(DOCS, "screenshot-focus.png") });
    await page.keyboard.press("Escape");
    check(!(await page.evaluate(() => document.documentElement.classList.contains("focus-mode"))), "Esc leaves focus mode");
    await context.close();
}

// ---------- Dark mode, menu collapsed ----------
{
    const { context, page } = await session({ dark: true });
    await context.addInitScript(() => { try { localStorage.setItem("ev_ui", JSON.stringify({ sidebar: "collapsed" })); } catch { } });
    await demo(page, "teacher");
    await page.click("[data-testid=class-card]");
    await page.waitForSelector("[data-testid=heatmap]");
    await page.screenshot({ path: join(DOCS, "screenshot-dark.png") });
    await context.close();
}

// ---------- Student: a true/false drill shows up inside a normal Mathematics practice, Duolingo-style ----------
{
    const { context, page } = await session();
    await demo(page, "student");
    // Since task #73 (and task #96's multi-select), each of the 3 drill slots in a 10-question Practice test (the
    // 4th, 7th and 10th question, task #95's tighter frequency) is one of 5 kinds (true/false, fill-blank, reorder,
    // match, multi-select) picked at random per attempt (ExerciseBank.AnyDrill), not always true/false as it was
    // through task #72, so this retries fresh attempts until a true/false one turns up.
    let sawDrill = false;
    for (let t = 0; t < 20 && !sawDrill; t++) {
        await page.goto(BASE + "/Student/Quiz?area=Mathematics&level=Primary3&mode=Practice");
        await page.locator("[data-testid=quiz-options] button").first().waitFor();
        for (let i = 0; i < 10 && !sawDrill; i++) {
            if (await page.locator("[data-testid=quiz-truefalse]").count()) {
                sawDrill = true;
                check(await page.locator("[data-testid=quiz-truefalse] button").count() === 2, "the drill shows exactly two buttons (true/false), not the usual options grid");
                check(await page.locator("[data-testid=hint]").count() === 0, "no hint button on a true/false drill");
                await page.screenshot({ path: join(DOCS, "screenshot-truefalse.png") });
                await page.locator("[data-testid=quiz-truefalse] button").nth(1).click();
                await page.locator("[data-testid=quiz-next]").waitFor();
                check((await page.locator("#qz-feedback").textContent()).length > 0, "the drill explains the answer like a classic question");
                await page.click("[data-testid=quiz-next]");
            } else {
                await answerQuizQuestion(page);
                await page.locator("[data-testid=quiz-next]").waitFor();
                await page.click("[data-testid=quiz-next]");
            }
        }
    }
    check(sawDrill, "a true/false drill appears during a Mathematics practice test");
    await context.close();
}

// ---------- Student: a multi-select ("select all that apply") drill shows up, Duolingo-style (task #96) ----------
{
    const { context, page } = await session();
    await demo(page, "student");
    let sawMultiSelect = false;
    for (let t = 0; t < 20 && !sawMultiSelect; t++) {
        await page.goto(BASE + "/Student/Quiz?area=Mathematics&level=Primary3&mode=Practice");
        await page.locator("[data-testid=quiz-options] button").first().waitFor();
        for (let i = 0; i < 10 && !sawMultiSelect; i++) {
            if (await page.locator("[data-testid=quiz-multiselect]").count()) {
                sawMultiSelect = true;
                const checkBtn = page.locator("#qz-multiselect-check");
                check(await checkBtn.isHidden(), "the Check button stays hidden until at least one option is picked");
                const opts = page.locator("[data-ms-index]");
                const n = await opts.count();
                check(n >= 3, "a multi-select drill offers at least 3 options to choose among");
                await opts.first().click();
                check(await opts.first().getAttribute("aria-pressed") === "true", "picking an option marks it pressed");
                check(await checkBtn.isVisible(), "the Check button appears once an option is picked");
                await opts.first().click();
                check(await checkBtn.isHidden(), "unpicking the only selected option hides the Check button again");
                // Pick every option to guarantee the full correct set is included, whichever indices they are —
                // this checks the UI wires through correctly, not whether the student "wins" the drill.
                for (let k = 0; k < n; k++) {
                    await opts.nth(k).click();
                }
                await page.screenshot({ path: join(DOCS, "screenshot-multiselect.png") });
                await checkBtn.click();
                await page.locator("[data-testid=quiz-next]").waitFor();
                check((await page.locator("#qz-feedback").textContent()).length > 0, "the multi-select drill explains the answer like a classic question");
                await page.click("[data-testid=quiz-next]");
            } else {
                await answerQuizQuestion(page);
                await page.locator("[data-testid=quiz-next]").waitFor();
                await page.click("[data-testid=quiz-next]");
            }
        }
    }
    check(sawMultiSelect, "a multi-select drill appears during a Mathematics practice test");
    await context.close();
}

// ---------- Student: fill-blank, reorder and match drills inside a Language practice test (task #71) ----------
{
    const { context, page } = await session();
    await demo(page, "student");
    await page.goto(BASE + "/Student/Quiz?area=Language&level=Primary3&mode=Practice");
    const seenKinds = new Set();
    for (let i = 0; i < 10; i++) {
        const kind = await answerQuizQuestion(page);
        seenKinds.add(kind);
        await page.locator("[data-testid=quiz-next]").waitFor();
        if (["fillblank", "reorder", "match"].includes(kind) && !seenKinds.has("screenshotted")) {
            check((await page.locator("#qz-feedback").textContent()).length > 0, `the ${kind} drill explains the answer like a classic question`);
            await page.screenshot({ path: join(DOCS, `screenshot-${kind}.png`) });
            seenKinds.add("screenshotted");
        }
        await page.click("[data-testid=quiz-next]");
    }
    check(!seenKinds.has("truefalse"), "Language has no true/false content, so no true/false drill appears");
    check(["fillblank", "reorder", "match"].some((k) => seenKinds.has(k)), "a fill-blank, reorder or match drill appears during a Language practice test");
    await context.close();
}

// ---------- Student: fill-blank, reorder and match drills inside a ForeignLanguage practice test (task #71) ----------
{
    const { context, page } = await session();
    await demo(page, "student");
    await page.goto(BASE + "/Student/Quiz?area=ForeignLanguage&level=Primary3&mode=Practice");
    const seenKinds = new Set();
    for (let i = 0; i < 10; i++) {
        const kind = await answerQuizQuestion(page);
        seenKinds.add(kind);
        await page.locator("[data-testid=quiz-next]").waitFor();
        await page.click("[data-testid=quiz-next]");
    }
    check(!seenKinds.has("truefalse"), "ForeignLanguage has no true/false content, so no true/false drill appears");
    check(["fillblank", "reorder", "match"].some((k) => seenKinds.has(k)), "a fill-blank, reorder or match drill appears during a ForeignLanguage practice test");
    await context.close();
}

// ---------- Student: fill-blank, reorder and match now also appear for a non-language subject (task #73) ----------
{
    const { context, page } = await session();
    await demo(page, "student");
    // Programming had true/false drills only (task #72); task #73 adds its own fill-blank/reorder/match content,
    // and task #96 adds multi-select, wiring ExerciseBank.AnyDrill to mix all 5 kinds for every subject, so retry
    // fresh attempts (3 drill slots each now, task #95's tighter frequency) until one of the 4 new kinds turns up.
    const seenKinds = new Set();
    for (let t = 0; t < 20 && !["fillblank", "reorder", "match", "multiselect"].some((k) => seenKinds.has(k)); t++) {
        await page.goto(BASE + "/Student/Quiz?area=Programming&level=Primary3&mode=Practice");
        for (let i = 0; i < 10; i++) {
            const kind = await answerQuizQuestion(page);
            seenKinds.add(kind);
            await page.locator("[data-testid=quiz-next]").waitFor();
            await page.click("[data-testid=quiz-next]");
        }
    }
    check(["fillblank", "reorder", "match", "multiselect"].some((k) => seenKinds.has(k)), "a fill-blank, reorder, match or multi-select drill appears during a Programming (non-language) practice test");
    await context.close();
}

// ---------- Student: Socratic ladder, "tell me how you thought", "How I learn" ----------
{
    const { context, page } = await session();
    await demo(page, "student");
    await page.goto(BASE + "/Student/Quiz?area=Mathematics&level=Primary3&mode=Practice");
    await page.locator("[data-testid=quiz-options] button").first().waitFor();
    await page.click("[data-testid=ladder-open]");
    await page.locator("[data-testid=ladder-question]").first().waitFor();
    await page.fill("[data-testid=ladder-reply]", "I think I should add first");
    await page.click("[data-testid=ladder-next]");
    await page.waitForFunction(() => document.querySelectorAll("[data-testid=ladder-question]").length >= 2);
    check(await page.locator("[data-testid=ladder-question]").count() >= 2, "the ladder asks a second guiding question");
    await page.locator("[data-testid=ladder]").screenshot({ path: join(DOCS, "screenshot-ladder.png") });
    let thought = false;
    for (let i = 0; i < 10 && !thought; i++) {
        await answerQuizQuestion(page);
        await page.locator("[data-testid=quiz-next]").waitFor();
        if (await page.locator("[data-testid=thinking]").count()) {
            thought = true;
            await page.click("[data-testid=reason-Slip]");
            await page.locator("[data-testid=thinking-error]").waitFor();
            check((await page.locator("[data-testid=thinking-error]").getAttribute("data-error")) === "Slip", "the chosen reason is diagnosed as a slip");
            check(await page.locator("[data-testid=thinking-tip]").isVisible(), "the Tutor gives a tip for the way of thinking");
        } else {
            await page.click("[data-testid=quiz-next]");
        }
    }
    check(thought, "a wrong answer offers \"tell me how you thought\"");
    await page.goto(BASE + "/Student/Learner");
    check(await page.locator("[data-testid=learner-summary]").isVisible(), "How I learn shows the summary");
    check(await page.locator("[data-testid=learner-insights] li").count() >= 1, "How I learn lists insights from the demo history");
    const toggle = page.locator("[data-testid^=insight-toggle-]").first();
    await toggle.click();
    await page.waitForLoadState("networkidle");
    await page.reload();
    check(await page.locator("[data-testid=learner-insights] li.opacity-60, [data-testid=learner-insights] li[class*=opacity-60]").count() >= 1, "\"That is not me\" hides an insight");
    await page.fill("[data-testid=learner-note]", "I learn better with a quiet start");
    await page.click("[data-testid=learner-note-send]");
    await page.waitForFunction(() => document.querySelector("[data-testid=learner-notes]")?.textContent.includes("quiet start"));
    check(true, "a note is added to How I learn");
    await page.click("[data-testid=learner-summary-ai]");
    await page.waitForFunction(() => document.querySelector("#lr-summary")?.dataset.engine);
    check((await page.locator("#lr-summary-engine").textContent())?.trim().length > 0, "the summary rewrite (offline fallback) explains why the text looks unchanged");
    await page.screenshot({ path: join(DOCS, "screenshot-learner.png"), fullPage: true });
    await context.close();
}

// ---------- Student: exam plan, homework review, defend your answers, session opening, interests ----------
{
    const { context, page } = await session();
    await demo(page, "student");
    // Interests first, so the tests that follow can use them
    await page.goto(BASE + "/Student/Learner");
    await page.click("[data-testid=interest-football]");
    await page.waitForLoadState("networkidle");
    check((await page.locator("[data-testid=interest-football]").getAttribute("aria-pressed")) === "true", "an interest is saved and shown as picked");
    await page.locator("[data-testid=interests]").screenshot({ path: join(DOCS, "screenshot-interests.png") });

    // Plan for an exam
    await page.goto(BASE + "/Student/Plan");
    await page.fill("[data-testid=plan-name]", "Maths test");
    await page.click("[data-testid=plan-save]");
    await page.locator("[data-testid=plan-summary]").waitFor();
    check(await page.locator("[data-testid=plan-task]").count() >= 1, "the plan lists today's tasks");
    // (earlier blocks already practised some topics today, so a task can be done: then open a focus session by hand)
    if (await page.locator("[data-testid=plan-start]").count()) {
        await page.locator("[data-testid=plan-start]").first().click();
        await page.waitForURL("**/Student/Quiz**");
    } else {
        await page.goto(BASE + "/Student/Quiz?area=Mathematics&level=Primary3&mode=Focus&topic=math.percent");
    }
    await page.locator("[data-testid=quiz-options] button").first().waitFor();
    while (!(await page.locator("[data-testid=quiz-summary]").count())) {
        await page.locator("[data-testid=quiz-options] button:not([disabled])").first().click();
        await page.locator("[data-testid=quiz-next]").waitFor();
        await page.click("[data-testid=quiz-next]");
    }
    check(await page.locator("[data-testid=focus-back]").count() === 1, "a finished plan session offers the way back to the plan");
    await page.goto(BASE + "/Student/Plan");
    await page.screenshot({ path: join(DOCS, "screenshot-plan.png"), fullPage: true });

    // Homework: typed steps, the wrong line is found by the code
    await page.goto(BASE + "/Student/Homework");
    await page.fill("[data-testid=hw-steps]", "3 + 4 = 7\n7 x 2 = 15\n15 - 1 = 14");
    await page.click("[data-testid=hw-send]");
    await page.locator("[data-testid=hw-line]").first().waitFor();
    check(await page.locator("[data-testid=hw-line][data-verdict=Wrong]").count() >= 1, "homework review finds the wrong step");
    await page.screenshot({ path: join(DOCS, "screenshot-homework.png"), fullPage: true });

    // Defend: finish a test with a right answer, then defend it
    let defended = false;
    for (let t = 0; t < 6 && !defended; t++) {
        await page.goto(BASE + "/Student/Quiz?area=Mathematics&level=Primary3&mode=Practice");
        for (let i = 0; !(await page.locator("[data-testid=quiz-summary]").count()) && i < 40; i++) {
            await answerQuizQuestion(page, i % 2);
            await page.locator("[data-testid=quiz-next]").waitFor();
            await page.click("[data-testid=quiz-next]");
        }
        await page.locator("[data-testid=quiz-summary]").waitFor();
        defended = (await page.locator("[data-testid=quiz-defend]").count()) > 0;
    }
    check(defended, "a finished test with right answers offers to defend them");
    await page.click("[data-testid=quiz-defend]");
    // The "tutor" rate limit (30/min) is shared by every e2e browser context (same IP), and the steps just before this
    // one use it heavily: if the defence start is refused, wait for the window to reset and reload once.
    await Promise.race([page.locator("[data-testid=defend-prompt]").waitFor(), page.locator("[data-testid=defend-error]").waitFor()]);
    if (await page.locator("[data-testid=defend-error]").count()) {
        console.log(`  (defence start refused: ${await page.locator("[data-testid=defend-error]").textContent()} — retrying after the rate-limit window)`);
        await page.waitForTimeout(61000);
        await page.reload();
    }
    await page.locator("[data-testid=defend-prompt]").waitFor();
    await page.locator("[data-testid=defend-options] button").first().click();
    await page.fill("[data-testid=defend-why]", "Because I split the number into groups and counted them");
    await page.screenshot({ path: join(DOCS, "screenshot-defend.png") });
    await page.click("[data-testid=defend-send]");
    await page.waitForFunction(() => document.querySelector("[data-testid=defend-card]")?.textContent.includes("💬"));
    check(true, "defending an answer is judged on the answer and the reason");

    // Session opening on the dashboard
    await page.goto(BASE + "/Student");
    check(await page.locator("[data-testid=opening]").isVisible(), "the dashboard opens with what the Tutor remembers");
    await page.locator("[data-testid=opening]").screenshot({ path: join(DOCS, "screenshot-opening.png") });
    // Rhythm, detective and "Pip makes your mistakes": answer fast (mostly wrong) on one topic
    let paced = false, detected = false;
    for (let t = 0; t < 4 && !detected; t++) {
        await page.goto(BASE + "/Student/Quiz?area=Mathematics&level=Primary3&mode=Focus&topic=math.percent");
        for (let i = 0; !(await page.locator("[data-testid=quiz-summary]").count()) && i < 12; i++) {
            await page.locator("[data-testid=quiz-options] button:not([disabled])").first().waitFor();
            await page.locator("[data-confidence='2']").click(); // bet: sure
            await page.locator("[data-testid=quiz-options] button:not([disabled])").first().click();
            await page.locator("[data-testid=quiz-next]").waitFor();
            if (!paced && await page.locator("[data-testid=quiz-pace]").count()) {
                paced = true;
                await page.locator("#qz-feedback").screenshot({ path: join(DOCS, "screenshot-pace.png") });
            }
            await page.click("[data-testid=quiz-next]");
        }
        if (t === 0) check(await page.locator("[data-testid=quiz-bets]").count() === 1, "the test ends with how the bets went");
        await page.goto(BASE + "/Student/Learner");
        detected = (await page.locator("[data-testid=detective-case]").count()) > 0;
    }
    check(paced, "answering very fast and wrong gets a gentle rhythm warning");
    check(detected, "the detective points at the step below a weak topic");
    await page.locator("[data-testid=detective]").screenshot({ path: join(DOCS, "screenshot-detective.png") });
    // "My path": built only from facts already measured above (answers, defence, teaching the Tutor)
    await page.goto(BASE + "/Student/Journey");
    check((await page.locator("[data-testid=journey], [data-testid=journey-empty]").count()) === 1, "my path shows real milestones or an honest empty state");
    await page.screenshot({ path: join(DOCS, "screenshot-journey.png") });
    await page.goto(BASE + "/Student/Learner");
    const sureCount = Number(await page.locator("[data-testid=quadrant-Solid]").getAttribute("data-count")) + Number(await page.locator("[data-testid=quadrant-FalseCertainty]").getAttribute("data-count"));
    check(sureCount >= 5, "the confidence map fills with the answers given as sure");
    await page.locator("[data-testid=confidence-map]").screenshot({ path: join(DOCS, "screenshot-confidence-map.png") });
    let own = false;
    for (let t = 0; t < 3 && !own; t++) {
        await page.goto(BASE + "/Student/Pip");
        await page.click("[data-testid=pip-start]");
        await page.locator("[data-testid=pip-lines]").waitFor();
        own = (await page.locator("[data-testid=pip-own]").count()) > 0;
    }
    check(own, "Pip stumbles on a topic the student got wrong");
    await page.screenshot({ path: join(DOCS, "screenshot-pip-own.png") });

    // Step by step (fading examples): read, then fill in less and less help, then alone
    await page.goto(BASE + "/Student/Fading");
    await page.click("[data-testid=fading-kind-Order]");
    await page.locator("[data-testid=fading-problem]").waitFor();
    check(await page.locator("[data-testid=fading-input]").count() === 0, "the first example is fully worked");
    await page.screenshot({ path: join(DOCS, "screenshot-fading.png") });
    await page.click("[data-testid=fading-send]");
    for (let stage = 1; stage <= 3; stage++) {
        await page.locator(`[data-testid=fading-stage][data-stage="${stage}"]`).waitFor();
        const [a, b, c] = (await page.locator("[data-testid=fading-problem]").innerText()).split(/[^0-9]+/).filter(Boolean).map(Number);
        const inputs = page.locator("[data-testid=fading-input]");
        if (stage === 2) {
            await inputs.nth(0).fill(String(b * c));
            await inputs.nth(1).fill(String(a + b * c));
            await page.screenshot({ path: join(DOCS, "screenshot-fading-2.png") });
        } else {
            await inputs.first().fill(String(a + b * c));
        }
        await page.click("[data-testid=fading-send]");
    }
    await page.locator("[data-testid=fading-finished]").waitFor();
    check(true, "the help fades stage by stage until the student solves one alone");

    await page.goto(BASE + "/Student/Pip");
    await page.click("[data-testid=pip-start]");
    await page.locator("[data-testid=pip-lines]").waitFor();
    check(await page.locator("[data-testid=pip-lines] button").count() >= 2, "Pip shows its work line by line");
    await page.screenshot({ path: join(DOCS, "screenshot-pip.png") });
    await page.click("[data-testid=pip-line-0]");
    await page.click("[data-testid=pip-reason-0]");
    await page.click("[data-testid=pip-send]");
    await page.locator("[data-testid=pip-result]").waitFor();
    check(true, "the student corrects Pip and the code checks the line and the reason");
    await page.waitForFunction(() => { const v = document.querySelector("[data-testid=pip-voice]"); return !v || (v.textContent.length > 8 && !v.textContent.includes("…")); });
    check(true, "Pip answers back (AI or offline line)");
    await page.goto(BASE + "/Student/Pip");
    check(await page.locator("[data-testid=pip-stats] li").count() >= 1, "the Pip page shows which mistakes the student spots");
    await page.goto(BASE + "/Student/Pip");
    await page.click("[data-testid=pip-start-language]");
    await page.locator("[data-testid=pip-lines]").waitFor();
    check(await page.locator("[data-testid=pip-lines] button").count() >= 2, "Pip also brings language problems");
    await page.screenshot({ path: join(DOCS, "screenshot-pip-language.png") });
    await page.goto(BASE + "/Student/Letter");
    check(await page.locator("[data-testid=letter-part]").count() >= 2, "the family letter is written from the week's facts");
    await page.locator("[data-testid=letter]").screenshot({ path: join(DOCS, "screenshot-letter.png") });
    await context.close();
}

// ---------- Teacher documents, assignments, open answers and the progress report ----------
{
    const VOLCANO = "A volcano is an opening in the crust of the Earth. Under the volcano there is a chamber full of magma, which is melted rock. When the pressure grows, the magma rises through the chimney and leaves the crater as lava. An eruption can also throw ashes and gases into the atmosphere. The lava cools down and becomes solid rock, so each eruption makes the volcano a little bigger. Scientists who study volcanoes are called volcanologists. They measure small earthquakes around the mountain, because the tremors often announce an eruption several days before it happens. Some volcanoes are extinct and will never erupt again, while others are only dormant and may wake up after many centuries.";
    const teacher = await session();
    await demo(teacher.page, "teacher");
    await teacher.page.click("[data-testid=class-card]");
    await teacher.page.click("[data-testid=course-link]");
    await teacher.page.waitForSelector("[data-testid=materials]");
    const courseUrl = teacher.page.url().split("#")[0];
    await teacher.page.fill("[data-testid=material-title]", "Volcanoes");
    await teacher.page.selectOption("[data-testid=material-target]", "Test");
    await teacher.page.fill("[data-testid=material-text]", "too short");
    await teacher.page.click("[data-testid=material-add]");
    await teacher.page.locator("[data-testid=material-error]").waitFor();
    check(true, "a document that is too short is refused");
    await teacher.page.fill("[data-testid=material-title]", "Volcanoes");
    await teacher.page.selectOption("[data-testid=material-target]", "Test");
    await teacher.page.fill("[data-testid=material-text]", VOLCANO);
    await teacher.page.click("[data-testid=material-add]");
    await teacher.page.locator("[data-testid=material]").first().waitFor();
    check(await teacher.page.locator("[data-testid=material]").count() === 1, "the teacher adds a document for the class");
    for (const kind of ["practice", "writing", "material"]) {
        await teacher.page.click(`[data-testid=assign-${kind}]`);
        await teacher.page.waitForLoadState("networkidle");
    }
    check(await teacher.page.locator("[data-testid=assign-row]").count() === 3, "the teacher sets three assignments (topic, open answer, document)");

    const student = await session();
    await demo(student.page, "student");
    await student.page.goto(BASE + "/Student");
    check(await student.page.locator("[data-testid=assignment][data-done='0']").count() === 3, "the student sees the work of the teacher on the dashboard");
    await student.page.locator("[data-testid=assignments]").screenshot({ path: join(DOCS, "screenshot-assignments.png") });
    await student.page.goto(BASE + "/Student/Materials");
    check(await student.page.locator("[data-testid=student-material]").count() === 1, "the student sees the documents of their class");
    await student.page.screenshot({ path: join(DOCS, "screenshot-materials.png") });
    await student.page.click("[data-testid=material-quiz]");
    await student.page.locator("[data-testid=quiz-options] button").first().waitFor();
    check((await student.page.locator("[data-testid=quiz-stage]").innerText()).includes("_____"), "without an AI the document becomes fill-the-gap questions");
    await student.page.screenshot({ path: join(DOCS, "screenshot-material-quiz.png") });
    for (let i = 0; !(await student.page.locator("[data-testid=quiz-summary]").count()) && i < 12; i++) {
        await student.page.locator("[data-testid=quiz-options] button:not([disabled])").first().click();
        await student.page.locator("[data-testid=quiz-next]").waitFor();
        await student.page.click("[data-testid=quiz-next]");
    }
    await student.page.locator("[data-testid=quiz-summary]").waitFor();

    await student.page.goto(BASE + "/Student/Writing");
    await student.page.click("[data-testid=writing-task-weekend]");
    await student.page.fill("[data-testid=writing-text]", "I played.");
    await student.page.click("[data-testid=writing-send]");
    await student.page.locator("[data-testid=writing-score]").waitFor();
    check((await student.page.locator("[data-testid=writing-check][data-key=length]").getAttribute("data-ok")) === "false", "a text that is too short is told so by the code");
    await student.page.fill("[data-testid=writing-text]", "Last weekend I went to the mountains with my family because the weather was sunny. First we walked to a small lake, and then we ate sandwiches near the water.\n\nAfter lunch my sister found a frog, but it jumped away very fast. Finally we came back home tired and happy, and I want to go again soon.");
    await student.page.click("[data-testid=writing-send]");
    await student.page.locator("[data-testid=writing-before]").waitFor();
    const score = student.page.locator("[data-testid=writing-score]");
    check((await score.getAttribute("data-passed")) === (await score.getAttribute("data-total")), "the second version meets every measured criterion and is compared with the first");
    await student.page.screenshot({ path: join(DOCS, "screenshot-writing.png"), fullPage: true });
    await student.page.goto(BASE + "/Student");
    check(await student.page.locator("[data-testid=assignment][data-done='1']").count() === 2, "work really done is ticked by itself");
    await student.context.close();

    await teacher.page.goto(courseUrl);
    check(await teacher.page.locator("[data-testid=assign-row] td[data-done='1']").count() === 2, "the teacher sees who did the work and the result");
    await teacher.page.screenshot({ path: join(DOCS, "screenshot-course.png"), fullPage: true });
    await teacher.page.locator("[data-testid=report-open]").nth(3).click();
    await teacher.page.waitForSelector("[data-testid=report]");
    check(await teacher.page.locator("[data-testid=report-work] li").count() === 3, "the progress report of a student can be printed");
    await teacher.page.locator("[data-testid=report]").screenshot({ path: join(DOCS, "screenshot-report.png") });

    // The teacher's own assistant: the same follow-up already shown on /Teacher/Class, across every class
    await teacher.page.goto(BASE + "/Teacher");
    await teacher.page.click("[data-testid=companion-open]");
    await teacher.page.locator("[data-testid=companion-reply]").first().waitFor();
    await teacher.page.click("#companion-quick [data-ask]");
    await teacher.page.waitForFunction(() => document.querySelectorAll("[data-testid=companion-reply]").length >= 2 && document.getElementById("companion").dataset.busy === "false");
    check((await teacher.page.locator("[data-testid=companion-reply]").last().textContent()).length > 0, "the teacher's assistant answers from the real follow-up of their classes");
    await teacher.page.screenshot({ path: join(DOCS, "screenshot-teacher-companion.png") });

    // The one-click nudge: a button, not a link — it posts and confirms right there in the chat, never navigates away
    const repliesBefore = await teacher.page.locator("[data-testid=companion-reply]").count();
    const nudgeBtn = teacher.page.locator("[data-nudge]").first();
    check(await nudgeBtn.count() > 0, "the assistant suggests nudging the struggling students of its hardest class");
    await nudgeBtn.click();
    await teacher.page.waitForFunction((n) => document.querySelectorAll("[data-testid=companion-reply]").length > n, repliesBefore);
    check(teacher.page.url().endsWith("/Teacher"), "clicking the nudge stays on the page instead of navigating");
    check((await teacher.page.locator("[data-testid=companion-reply]").last().textContent()).length > 0, "the nudge confirms in the chat");
    await teacher.context.close();
}

// ---------- Mobile 390 px, every page, light and dark ----------
for (const dark of [false, true]) {
    for (const role of ["student", "teacher", "director", "tutor"]) {
        const { context, page } = await session({ width: 390, height: 844, mobile: true, dark });
        await page.goto(BASE + "/culture/en?returnUrl=/");
        await noOverflow(page, `mobile${dark ? " dark" : ""} /`);
        await demo(page, role);
        const pages = {
            student: ["/Student", "/Student/Explore", "/Student/Quiz?area=Programming&level=HighSchool", "/Student/Arena", "/Student/Achievements", "/Student/Adventures", "/Student/Teach?concept=percent", "/Student/Interview?figure=newton", "/Student/Album", "/Student/Timeline", "/Student/Learn?subject=fractions", "/Student/Learner", "/Student/Journey", "/Student/Plan", "/Student/Homework", "/Student/Letter", "/Student/Pip", "/Student/Fading", "/Student/Materials", "/Student/Writing", "/Student/NewSubject", "/Student/Path?area=History&level=Primary3", "/Student/League", "/Student/Shop", "/Student/Roleplay", "/Student/Roleplay?scenario=airport", "/Student/Roleplay?scenario=rubberduck", "/Marketplace", "/Marketplace/Bookings", "/About"],
            teacher: ["/Teacher", "/Teacher/Class?id=1", "/Teacher/Course?id=1", "/Teacher/Pack", "/Teacher/Lesson", "/Teacher/Summary", "/Moderation/Ideas"],
            director: ["/Institution", "/Institution/Risk", "/Institution/Quality"],
            tutor: ["/Tutor", "/Tutor/Profile", "/Marketplace"],
        }[role];
        for (const url of pages) {
            await page.goto(BASE + url);
            await page.waitForLoadState("networkidle");
            await noOverflow(page, `mobile${dark ? " dark" : ""} ${url}`);
        }
        if (role === "student" && !dark) {
            await page.goto(BASE + "/Student/Learn?subject=fractions");
            await page.waitForSelector(".ln-option, #ln-understood, #ln-diag-start");
            await page.screenshot({ path: join(DOCS, "screenshot-mobile.png") });
        }
        await context.close();
    }
}

// ---------- 4 languages ----------
{
    const { context, page } = await session();
    await demo(page, "student");
    for (const [culture, word] of [["fr", "Les fractions"], ["es", "Fracciones"], ["ca", "Fraccions"], ["en", "Fractions"]]) {
        await page.goto(`${BASE}/culture/${culture}?returnUrl=/Student`);
        check(await page.getByText(word, { exact: false }).first().isVisible(), `interface in ${culture}`);
    }
    await context.close();
}

await browser.close();
if (failures.length) {
    console.error(`\n${failures.length} failure(s):\n- ` + failures.join("\n- "));
    process.exit(1);
}
console.log("\nAll end-to-end checks passed.");
