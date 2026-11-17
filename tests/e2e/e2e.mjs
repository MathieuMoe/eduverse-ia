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
    check(await page.locator("[data-testid=activity-calendar] [title]").count() >= 35, "monthly activity calendar");
    await page.goto(BASE + "/Student/Explore");
    check(await page.locator("[data-testid=levels] li").count() === 10, "ten levels from early years to doctorate");
    check(await page.locator("[data-testid=subjects] section").count() === 10, "ten subjects in the catalog");
    await page.screenshot({ path: join(DOCS, "screenshot-explore.png") });

    // Adaptive test: answer every question, the answer key only arrives after answering
    await page.goto(BASE + "/Student/Quiz?area=Physics&level=HighSchool");
    for (let i = 0; i < 10; i++) {
        await page.locator("[data-testid=quiz-options] button").first().waitFor();
        await page.locator("[data-testid=quiz-options] button").nth(i % 4).click();
        await page.locator("[data-testid=quiz-next]").waitFor();
        if (i === 1) {
            check(await page.locator("[data-state=right]").count() === 1, "the right option is shown after answering");
            await page.screenshot({ path: join(DOCS, "screenshot-quiz.png") });
        }
        await page.click("[data-testid=quiz-next]");
    }
    await page.locator("[data-testid=quiz-summary]").waitFor();
    check(+(await page.locator("[data-testid=quiz-xp]").textContent()) >= 10, "XP won during the test");
    await page.click("[data-testid=quiz-review]");
    await page.locator("[data-testid=review]").waitFor();
    check(await page.locator("[data-testid=review] > li").count() === 10, "the finished test is saved with its 10 questions for review");
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

    // Placement test finds a level
    await page.goto(BASE + "/Student/Quiz?area=Mathematics&mode=Placement");
    for (let i = 0; i < 10; i++) {
        const done = page.locator("[data-testid=quiz-summary]");
        if (await done.count()) break;
        await page.locator("[data-testid=quiz-options] button").first().waitFor();
        await page.locator("[data-testid=quiz-options] button").first().click();
        await page.locator("[data-testid=quiz-next]").waitFor();
        await page.click("[data-testid=quiz-next]");
    }
    await page.locator("[data-testid=placed-level]").waitFor();
    check((await page.locator("[data-testid=placed-level]").textContent()).length > 3, "placement test gives a level");

    // Arena: ranking, teams, the duel Alex sent to Sara
    await page.goto(BASE + "/Student/Arena");
    check(await page.locator("[data-testid=leaderboard] ol li").count() === 4, "class ranking with nicknames");
    check(!(await page.locator("[data-testid=leaderboard]").textContent()).includes("Sara"), "real names are never shown in the ranking");
    await page.screenshot({ path: join(DOCS, "screenshot-arena.png") });
    await page.click("[data-testid=play-duel]");
    for (let i = 0; i < 5; i++) {
        await page.locator("[data-testid=quiz-options] button").first().waitFor();
        await page.locator("[data-testid=quiz-options] button").first().click();
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
    for (let i = 0; i < 20; i++) {
        if (await page.locator("[data-testid=quiz-summary]").count()) break;
        await page.locator("[data-testid=quiz-options] button").first().waitFor();
        await page.locator("[data-testid=quiz-options] button").first().click();
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
    await page.click("[data-testid=iv-voice]"); // no audio in the headless browser
    await page.click("[data-testid=iv-start]");
    await page.locator("[data-testid=iv-answer]").first().waitFor();
    const mouths = new Set();
    for (let i = 0; i < 6; i++) { mouths.add(await page.locator("[data-testid=iv-avatar]").getAttribute("data-mouth")); await page.waitForTimeout(60); }
    check(mouths.size > 1, "the lips move while the figure speaks");
    for (const [i, b] of (await page.locator("#iv-suggestions [data-question]").all()).slice(0, 3).entries()) {
        await page.locator("#iv-root[data-busy=false]").waitFor();
        await b.click();
        await page.waitForFunction((n) => document.querySelectorAll("[data-testid=iv-answer]").length >= n, i + 2);
    }
    await page.locator("#iv-root[data-busy=false]").waitFor();
    await page.locator("[data-testid=iv-check]").waitFor();
    check(await page.locator("[data-testid=iv-answer]").count() === 4, "the figure greets and answers the suggested questions offline");
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
    check(true, "interview check finished");

    await page.goto(BASE + "/Student/Achievements");
    check(await page.locator("[data-testid=badges] li").count() === 28, "all badges with their progress");
    await page.screenshot({ path: join(DOCS, "screenshot-achievements.png"), fullPage: true });
    await page.goto(BASE + "/Student");
    await page.screenshot({ path: join(DOCS, "screenshot-student.png") });
    await context.close();
}

// ---------- Kids mode (6 years old) ----------
{
    const { context, page } = await session();
    await demo(page, "kid");
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
        await page.locator("[data-testid=quiz-options] button:not([disabled])").first().waitFor();
        await page.locator("[data-testid=quiz-options] button:not([disabled])").first().click();
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
    check(true, "the summary can be rewritten (offline fallback)");
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
            await page.locator("[data-testid=quiz-options] button:not([disabled])").first().waitFor();
            await page.locator("[data-testid=quiz-options] button:not([disabled])").nth(i % 2).click();
            await page.locator("[data-testid=quiz-next]").waitFor();
            await page.click("[data-testid=quiz-next]");
        }
        await page.locator("[data-testid=quiz-summary]").waitFor();
        defended = (await page.locator("[data-testid=quiz-defend]").count()) > 0;
    }
    check(defended, "a finished test with right answers offers to defend them");
    await page.click("[data-testid=quiz-defend]");
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
            student: ["/Student", "/Student/Explore", "/Student/Quiz?area=Programming&level=HighSchool", "/Student/Arena", "/Student/Achievements", "/Student/Adventures", "/Student/Teach?concept=percent", "/Student/Interview?figure=newton", "/Student/Learn?subject=fractions", "/Student/Learner", "/Student/Plan", "/Student/Homework", "/Student/Letter", "/Student/Pip", "/Student/Fading", "/Student/Materials", "/Student/Writing", "/Student/NewSubject", "/Marketplace", "/Marketplace/Bookings", "/About"],
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
