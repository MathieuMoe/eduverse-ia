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
    await page.goto(BASE + "/Teacher");
    await page.screenshot({ path: join(DOCS, "screenshot-classes.png") });
    await page.goto(BASE + "/Teacher/Class?id=1");

    await page.locator("[data-testid=heatmap] tbody tr th a").first().click();
    await page.waitForSelector("#student table");
    check(true, "student history opens from the heatmap");

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
    check((await page.locator("[data-testid=quiz-options]").textContent()).includes("Evo is right"), "check Evo offers \"Evo is right\"");

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
    check(await page.locator("[data-testid=badges] li").count() === 23, "all badges with their progress");
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

// ---------- Mobile 390 px, every page, light and dark ----------
for (const dark of [false, true]) {
    for (const role of ["student", "teacher", "director", "tutor"]) {
        const { context, page } = await session({ width: 390, height: 844, mobile: true, dark });
        await page.goto(BASE + "/culture/en?returnUrl=/");
        await noOverflow(page, `mobile${dark ? " dark" : ""} /`);
        await demo(page, role);
        const pages = {
            student: ["/Student", "/Student/Explore", "/Student/Quiz?area=Programming&level=HighSchool", "/Student/Arena", "/Student/Achievements", "/Student/Adventures", "/Student/Teach?concept=percent", "/Student/Interview?figure=newton", "/Student/Learn?subject=fractions", "/Student/NewSubject", "/Marketplace", "/Marketplace/Bookings", "/About"],
            teacher: ["/Teacher", "/Teacher/Class?id=1", "/Teacher/Pack", "/Teacher/Lesson", "/Teacher/Summary"],
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
