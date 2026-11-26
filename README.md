<img src="src/EduVerse.Web/wwwroot/img/logo.svg" width="64" alt="" />

# EduVerse AI

**The AI teacher that knows each student.** One educational platform for **students, teachers, institutions and private teachers**. Students get a learning platform **from 5 years old to doctorate** in 10 subjects, with adaptive tests, placement tests, XP, streaks, badges, class rankings and duels. The tutor tests, explains, practises with feedback on every mistake, **changes its way of explaining when one does not work**, and **calls the human teacher** when a student is stuck. Teachers see their class at a glance and get a full lesson from any document; institutions get an early dropout alert; families can book a private teacher recommended from what the student finds hardest.

Built with **ASP.NET Core (.NET 10)**, **EF Core + SQLite**, **ASP.NET Core Identity**, your choice of AI (**Claude, ChatGPT, Gemini or a local Ollama model**) and a dashboard UI made with **Tailwind CSS + Preline UI**. Works **without any AI key** in demo mode.

![EduVerse AI: the learning loop with feedback on the misconception behind a wrong answer](docs/screenshot.png)

<details>
<summary>Student platform (catalog, adaptive test, arena, kids mode), teacher follow-up, institution, dropout radar, marketplace, private teacher, lesson pack, focus mode, mobile and dark mode</summary>

![Student dashboard: XP, level, streak, rank, quests, activity calendar, podium, curiosity thread](docs/screenshot-student.png)
![Catalog of tests: 10 subjects, 10 levels from early years to doctorate](docs/screenshot-explore.png)
![Adaptive test: the answer key arrives only after answering](docs/screenshot-quiz.png)
![Duolingo-style drill: true/false](docs/screenshot-truefalse.png)
![Duolingo-style drill: fill in the blank](docs/screenshot-fillblank.png)
![Duolingo-style drill: reorder the words](docs/screenshot-reorder.png)
![Duolingo-style drill: match the pairs](docs/screenshot-match.png)
![Duolingo-style drill: select every correct option](docs/screenshot-multiselect.png)
![Learning path: units of lesson, lesson, chest, lesson and a boss, unlocked in order](docs/screenshot-path.png)
![End of a lesson: XP, accuracy and time, the daily goal ring, the weekly league and the Tutor's reaction](docs/screenshot-lesson-end.png)
![Daily goal and happy hour (double XP for 30 minutes a day)](docs/screenshot-daily-goal.png)
![Weekly league: Bronze to Diamond, promotion and demotion zones, and the friend quest of the week](docs/screenshot-league.png)
![Gem shop: streak freezes, double XP, heart potions and avatar frames — gems are only earned by learning](docs/screenshot-shop.png)
![Roleplay: haggling with a Roman merchant; the missions tick in code, the AI only plays the character](docs/screenshot-roleplay.png)
![History trips: the timeline of a country, its main figures and a strange-but-true fact; each era ends with a check written by the code](docs/screenshot-countries.png)
![Timeline challenge: put the eras of a stamped country back in order to turn its passport stamp gold](docs/screenshot-country-order.png)
![The story of your mistakes: the week's most-missed topics told as a short adventure](docs/screenshot-mistake-story.png)
![Arena: weekly ranking with nicknames, teams, class challenge, duels](docs/screenshot-arena.png)
![Achievements: titles, badges and their progress](docs/screenshot-achievements.png)
![Review of a finished test](docs/screenshot-history.png)
![Daily review with the hint ladder and the confidence meter](docs/screenshot-review.png)
![The Tutor's notebook: The Tutor makes the student's own mistakes](docs/screenshot-evo.png)
![Adventure: doors, hearts, the story and the final guardian](docs/screenshot-adventure.png)
![The Tutor of every page: knows the progress, remembers the conversation and proposes actions](docs/screenshot-companion.png)
![Explain it to the Tutor: the student teaches, the rubric finds the key ideas](docs/screenshot-teach.png)
![Interview with Marie Curie: illustrated figure that moves its lips, own voice, educational simulation](docs/screenshot-interview.png)
![Figure album: collectible cards unlocked by completing each historical figure's interview](docs/screenshot-album.png)
![Timeline mini-game: place completed figures in the order they were born](docs/screenshot-timeline.png)
![Learn: choose what to learn among every subject at your level, your own subjects and the trades](docs/screenshot-learn-catalog.png)
![Trades and practical skills: electricity, solar energy, garden, plumbing, carpentry, first aid, cooking, bike repair, painting and beekeeping, each concept with the science behind it](docs/screenshot-trades.png)
![Hobbies: astronomy, music, magic and illusions, survival and orienteering, chess, dinosaurs and fossils](docs/screenshot-hobbies.png)
![Students' ideas: propose the next adventure, figure, trade or hobby and vote for the approved ones](docs/screenshot-ideas.png)
![Think with the Tutor: a ladder of guiding questions that never gives the answer](docs/screenshot-ladder.png)
![How I learn: what the Tutor has noticed, editable by the student](docs/screenshot-learner.png)
![Exam plan: the Tutor builds a daily plan from what the student knows and adjusts it every day](docs/screenshot-plan.png)
![Review my steps: the code checks the arithmetic of each line, the AI only explains (photo optional, never stored)](docs/screenshot-homework.png)
![Defend your answers: other numbers, then "why?" — judged on the answer and the reason](docs/screenshot-defend.png)
![The Tutor opens the session with what it remembers of last time](docs/screenshot-opening.png)
![Correct Pip: the student finds the mistake of an AI student and says what went wrong](docs/screenshot-pip.png)
![Correct Pip, language version: agreement, past tense and capital letters in the student's language](docs/screenshot-pip-language.png)
![Materials and assignments: the teacher adds documents for the Tutor, sets work with a date and sees measured progress](docs/screenshot-course.png)
![Progress report of a student: before and now, topic by topic, the work set and what the Tutor found](docs/screenshot-report.png)
![Write it, say it: an open answer measured by the code and compared with the version before](docs/screenshot-writing.png)
![A teacher's document as a test: without an AI, sentences of the document with a missing word](docs/screenshot-material-quiz.png)
![Work from the teacher on the student's dashboard, ticked by itself when it is really done](docs/screenshot-assignments.png)
![The detective: when a topic is weak, the Tutor checks the step below it](docs/screenshot-detective.png)
![My path: the student's own evolution told as a story, built only from facts already measured elsewhere](docs/screenshot-journey.png)
![Step by step: worked examples where the help fades until the student solves one alone](docs/screenshot-fading-2.png)
![Confidence map: what the student believed against what happened, in four boxes](docs/screenshot-confidence-map.png)
![Rhythm: a gentle note when the student rushes, gets stuck or gets tired](docs/screenshot-pace.png)
![Pip stumbles on a topic the student got wrong in their own tests](docs/screenshot-pip-own.png)
![Letter for the family: the week written by code from what was measured, printable](docs/screenshot-letter.png)
![The teacher in the loop: a short note for a student that the student reads and the Tutor takes into account](docs/screenshot-teacher-note.png)
![What I like: examples and word problems about the student's interests](docs/screenshot-interests.png)
![Moderation: teachers and directors approve the ideas, follow the votes and see how the adventures are rated](docs/screenshot-moderation.png)
![Internal certificate of a trade, printable](docs/screenshot-certificate.png)
![Dossier of Galileo: the discovery with an original illustration, the life in dates and the historical evidence with a link to the source](docs/screenshot-dossier.png)
![Some test questions come with a picture drawn in the browser](docs/screenshot-diagram.png)
![Kids mode for 5-8 year olds: pictures, big buttons, questions read aloud](docs/screenshot-kids.png)
![Kids home: the mascot, one big "continue" tile, the streak as stars, subjects to play — no XP, no rankings, no percentages; now with floating sparkles, a bouncing mascot and bouncy pop-in cards for a game-app feel](docs/screenshot-kids-home.png)
![Kids achievements: a sticker shelf, no rank ladder, no XP numbers](docs/screenshot-kids-achievements.png)
![Institution dashboard](docs/screenshot-institution.png)
![Dropout radar with a message to the family](docs/screenshot-risk.png)
![Marketplace with recommendations](docs/screenshot-marketplace.png)
![Lessons booked with the simulated payment](docs/screenshot-bookings.png)
![Private teacher: earnings after commission](docs/screenshot-tutor.png)
![Class follow-up: heatmap students × concepts, alerts from the tutor, frequent mistakes](docs/screenshot-teacher.png)
![Teacher dashboard](docs/screenshot-classes.png)
![Focus mode](docs/screenshot-focus.png)
![Lesson pack from a document](docs/screenshot-pack.png)
![Placement test result](docs/screenshot-diagnostic.png)
![Home](docs/screenshot-home.png)
![Dark mode with the menu collapsed to icons](docs/screenshot-dark.png)
<img src="docs/screenshot-mobile.png" width="320" alt="Mobile" />

</details>

## Features

**Student platform** (catalog of tests from 5 years old to doctorate)
- 📚 **10 subjects × 10 levels**: mathematics, physics, chemistry, biology, language, a foreign language, history, geography, economics and programming, from early years (5–6) through primary, secondary, high school, degree, master and **doctorate**
- 🧮 **Offline question banks checked in code**: numeric questions are generated *and solved* by the code (arithmetic to eigenvalues, kinematics to Lorentz factors, pH, Punnett squares, compound interest, Python programs whose output is computed…); fact questions come from a checked table in 4 languages, wrong options are typical mistakes. A test checks thousands of generated questions (4 distinct options, one right)
- 🎯 **Adaptive tests**: 2 right answers in a row → harder, a wrong one → easier; **every new test avoids the questions already seen** (last 300 of the subject and level); knowledge per topic tracked with Bayesian Knowledge Tracing; topics to review shown in the catalog
- 🦜 **Duolingo-style exercise variety**: every 3rd question of a test can be a true/false, fill-in-the-blank, word-reorder, match-the-pairs or select-every-correct-option drill instead of the usual 4-option grid, for all 10 subjects, in 4 languages, with the same instant feedback and XP as a classic question, and never the same drill twice within one attempt while another is available. They mix into **practice tests, adventures, duels and placement tests** alike — a drill answered mid-adventure still opens the next door, one answered mid-placement still narrows the level search, and both duelists always get the identical drill. Placement leaves out true/false specifically (a 50 % guess could corrupt the 2-right-or-wrong-decides-the-level binary search it runs); every drill is generated and checked in code, nothing left to the AI
- 💬 **Why, right or wrong**: the feedback after each answer always says why — a miss now also names the correct answer out loud — worded for the student's stage (shorter and warmer in primary, the reasoning spelled out from high school up); the Tutor's own chat and explanations also know the student's most recent quiz misses
- 📝 **Tests done** saved with every question, the answer given and the right one: list per subject and **review** of each test
- 🎚️ **One level of study for all subjects**: changing it (dashboard or catalog) applies it to every subject; a placement test can then refine one subject
- 🧭 **Placement test per subject**: a binary search over the 10 levels (2 questions per level, 10 at most) gives the level to practise
- 🦉 **Check the Tutor** (every subject): the mascot answers and the student decides if it is right; when it is wrong, its answer is a typical mistake and the student picks the right one (the Tutor is right 1 time in 3, so guessing "wrong" does not work)
- 📓 **The Tutor's notebook**: The Tutor solves fraction exercises making the student's **own** mistakes from the tutor history (e.g. adding the denominators) and the student says what went wrong
- 💡 **Hints in a ladder**, decided in code, in the tests and in the tutor: one wrong option disappears → 50/50 → the method with the answer hidden (or its first letter); the answer is never given and each hint takes 25 % of the XP of the question
- ⚡ **Daily review** (spaced repetition, Leitner boxes per topic: 1, 2, 4, 7, 15, 30 days): 6 questions from every subject on the topics about to be forgotten
- 😎 **Confidence meter**: before answering, "not sure / I think so / sure"; sure and right earns a bonus, sure and wrong sends the topic to today's review; the achievements page shows the calibration
- 🗺️ **Adventures with a final guardian**: choose a world (space, ocean, jungle, castle, dino valley, circus) and a subject; 5 doors open with right answers, then a friendly guardian asks 4 questions on the student's **weakest topics** at the hardest difficulty; 3 hearts for the whole adventure. The code decides the questions and the progress; the AI only tells the story (age-appropriate, no violence), and offline stories are written in advance in 4 languages. The theme picker itself is playful: floating sparkles, a staggered "pop-in" entrance for each world card, and a wiggle/bounce on hover
- 📚 **Learn any subject with the tutor**: Learn opens with a catalog to choose what to learn — the 10 subjects of the catalog at the student's level (each topic of the question bank becomes a concept of the tutor, with exercises generated and solved in code, pictures included), fractions with the detection of misconceptions, the student's own subjects created with the AI, the trades and the hobbies; "continue where you left off" shows the last ones
- 🛠️ **Trades and practical skills** (a tab of Learn, next to the subjects): **electricity at home, solar energy, vegetable garden, plumbing, carpentry, first aid, cooking and baking, bicycle mechanics, painting and decorating, beekeeping**, each with 5 concepts connected to the science behind them (Ohm's law, the photovoltaic effect, photosynthesis, pressure, Pythagoras, fermentation and the Maillard reaction, gear ratios, pigments, the hexagons of the comb…) and 15 questions written and checked by hand in 4 languages, so they work offline with the same tutor (explanations in 5 methods, adaptive exercises, knowledge tracing). Safety notes where they matter (electricity, first aid with a pointer to a certified course and 112, power tools, hives). When every concept is mastered the code issues an **internal certificate** (printable, with a code) plus XP and the badges *Apprentice* and *Master of trades*; it proves the theory learned, not a professional qualification
- 🎨 **Hobbies** (third tab of Learn): fun things to learn for the pleasure of it — **astronomy, music, magic and illusions, survival and orienteering, chess, dinosaurs and fossils** — built the same way (5 concepts linked to physics, biology, maths, history…, 15 checked questions in 4 languages, certificate) with their own badge *Curious mind*
- 💡 **Students' ideas and feedback**: students propose a historical figure to interview, the next adventure world, a trade or a hobby, and vote for the ideas of the others. The code normalises the text ("Napoleón" = "napoleon": the same idea becomes one more vote), recognises what already exists, filters links, contact details and rude words, and limits proposals to 5 a day. Names are never shown and a new idea is visible to other students only after an adult approves it. Teachers and directors have a moderation page (To review → Approved → In preparation → Available / Discarded) ranked by votes; when an idea becomes available the Tutor tells every student who asked for it and its proposers win XP and the badge *Idea maker*. At the end of each adventure a 10-second survey (stars, too easy / just right / too hard, fun) feeds the ratings per world shown to the adults
- 🎯 **Exam plan**: the student says what exam and when, and how many minutes a day; the code builds a day-by-day plan from the mastery of each topic (more sessions for the weak ones, a mock test near the end, rest days) and **rebuilds it every day**, so it adjusts itself. It says honestly if the time is not enough. Each task opens a focused session on one topic; the Tutor (companion) knows the plan
- 📝 **Review my steps** (homework): the student types — or photographs — the steps of a problem; **the code verifies the arithmetic of every line** (so the AI can never mark a right calculation as wrong) and the AI only explains the first wrong step. Photos need consent, are not available for young children, are checked (type, size ≤ 4 MB), kept in memory only and **never stored**
- 🛡️ **Defend your answers**: after a test, up to 3 of the topics answered right come back with other numbers and a "why?"; the answer is checked by the server and the reason by the AI (or an offline keyword judge); only "understood" results are kept — never the text — and the teacher sees an *Understood* column per student
- 🦉 **The Tutor remembers**: the dashboard opens with what happened last time (days away, topic, score) and one next step chosen by code: the exam plan, the reviews due, the hardest topic, or a warm welcome back
- 📄 **The teacher's documents for the Tutor**: a teacher pastes a text or loads a .txt/.md/.pdf (read in the browser) for a class and attaches it to **the course**, to **a historical figure** or turns it into **a test**. An optional link (a video, a page) can go with it. The document is **labelled** on upload — subject, a few topics, a one-sentence summary — by the AI when one is configured, or guessed by the code from the document's own words otherwise; the teacher can correct the labels. A labelled course document can serve the Tutor even in a different platform subject than the class's own, when its label matches. The code chooses the pieces that share words with what the student asks (no AI needed for that) and hands them to the Tutor as *data, never instructions*: in the lessons and chat of the course, in the interview with the figure (offline, the matching piece is quoted) and as a test — written by the AI only from the document, or offline as fill-the-gap questions whose right answer is, by construction, the teacher's own word. Only the students of that class get them
- ✍️ **Write it, say it** (open answers): seven tasks to write or to say aloud (dictated by voice). The code measures what can be counted — length, paragraphs, linking words, capital letters, repeated words, filler words in speech — and the AI judges four fixed criteria, each with a fragment of the student's own text that the code verifies is really there (the AI may not invent what the student wrote). Sending a new version shows the improvement. **The text is never stored**, only the measures
- 🗓️ **Assignments that tick themselves**: the teacher sets work with a date — practise a topic, an open answer, the test of a document. Nobody ticks anything: it is done when the student's own activity after the assignment shows it, with the result. The student sees the pending work on the dashboard
- 📈 **Measured progress and a printable report**: for each student, right answers of the last 30 days against the 30 before (no percentage under 5 answers), topic by topic, the work done, and what the Tutor found (root one step below, false certainties). Made only from the student's own answers
- 🕵️ **The prerequisite detective**: when a topic is weak, the cause is often one step below (fractions fail because division is weak). A hand-written map of "what you need first" for mathematics (20 links, from addition to derivatives) and the student's own answers give a verdict in code: *check the step below*, *the root is there*, or *your base is solid, practise the topic itself* — with one button to do it. The teacher sees the same finding for each student. When the teacher has labelled a document for that same subject, the detective points at it first — "your teacher's own document about this" — before the generic practice button, so the remedy starts with the class's own material
- 🧭 **"My path"**: the student's own evolution told as a story, not a dashboard — a topic that turned from weak to solid across two months, the first time an answer was really defended (a new question, right, with a real reason), the first concept explained completely to the Tutor, the longest run of days studying in a row. Nothing new is tracked for it: every milestone is built from facts the platform already measures elsewhere (quiz answers, defences, "explain it to the Tutor" sessions, XP), just read across months instead of inside a short window — 100% code, no AI call, and no milestone without real, dated evidence behind it
- 💭 **"What I want to know"**: a small dashboard widget where a student jots down what they're curious about, in their own words — not graded, not measured, just a thread to come back to. They can explore a question whenever they like, which drops it straight into "learn any topic" already filled in, or dismiss it. A gentle daily limit and a cap on how many can wait open keep it a thread, not a backlog, and the widget rotates through them so nothing waits forever unseen
- 🪄 **Step by step** (fading examples): the Tutor solves a whole problem, then the last result is missing, then every result, and at the end the student solves one alone. A wrong try shows the solution and brings another problem of the same kind. Six kinds (order of operations, percentage, mean, perimeter, discount, equation), all computed and checked in code
- ⏱️ **Rhythm and tiredness**: the page measures the seconds on each question; the code notices three things — very fast and wrong (*read twice*), very slow and wrong (*ask for a hint earlier*), a long test that started well and drops (*take a break*) — and says one gentle sentence. It never changes the score and is off in placement tests and duels
- 🪞 **Pip makes your mistakes**: two times out of three, Pip stumbles on a topic the student got wrong lately in their own tests (or on a kind of mistake they did not spot last time), so the student corrects in someone else what they do wrong themselves
- 🎯 **Confidence map**: before each answer the student can bet 😎 *sure* or 🤔 *not sure*. *How I learn* turns those bets into four boxes — *you know it and you know that you know it*, *sure… but not yet*, *you know more than you think*, *still to learn, and you know it* — with the topics in each, a score of how well belief matched result, and whether it is getting better. Every test ends with how its bets went. Topics that are "sure and wrong" get a review button, reach the teacher, and add two kind lines to the family letter
- 🐣 **Correct Pip** (teach by correcting): Pip, a student-AI, solves a problem with **one typical mistake** (forgotten carry, wrong borrowing, adding before multiplying, adding fractions straight across, forgetting ÷ 100, perimeter instead of area, forgetting to divide a mean, dividing instead of multiplying in a unit change, multiplying instead of dividing for a speed). The student taps the first wrong line and says what went wrong. The problem and the mistake are generated in code, so the platform always knows which line is wrong and why: it works offline, nothing is invented by an AI, and the server keeps only the seed. Pip answers back in character (written by the AI from the facts, or by code offline) and the page shows which kinds of mistake the student spots best. There is also a **language** version (subject-verb agreement, present instead of past, missing capital letter) in the four languages, built from a small controlled lexicon so the code always knows the right form. XP only for line + reason (capped per day)
- 💌 **Letter for the family**: the week as a short printable letter (days studied, answers, what went best, what is still hard, the exam, one idea to help at home). Written entirely by code from measured facts — no AI, nothing is sent anywhere
- 🌉 **Cross-subject bridges**: when a student is struggling with a topic they have already mastered an equivalent of elsewhere (proportion in Maths ↔ percentages in Economics, density in Physics ↔ states of matter in Chemistry, probability ↔ genetics, and more), the Tutor points it out — "you already know this from {subject}" — turning the platform's unified, cross-subject mastery tracking into something the student actually sees
- 👨‍👩‍👧 **Teach your family**: when a topic is freshly mastered, instead of a report *to* the family, the Tutor hands the student a short script *to teach it themselves* to a parent or sibling — an explanation plus two real check-questions pulled from the same question bank as the quizzes, so they can really test what they just taught. Written entirely by code from measured facts, no AI
- 🕵️ **Don't trust AI blindly**: a short true-or-false game where an AI-voiced claim about a concept (density, photosynthesis, prime numbers, the fall of Rome…) is sometimes right and sometimes subtly wrong, and the student has to judge it rather than take it on faith — the point is the habit of doubting AI output, not just the subject matter. Every claim and its correctness are decided in code, so it works offline, with a small daily XP reward
- 🔮 **A letter from your future self**: a one-off milestone letter — not weekly — written as if from the student's own future self, triggered by round numbers of topics mastered across every subject. It looks back at real mistakes overcome and, as its one forward step, picks up the student's own open "what I want to know" question and proposes exploring it next, one click away
- 👩‍🏫 **The teacher in the loop**: from the class follow-up a teacher leaves up to 3 short notes (160 characters) for a student of their own class. The student reads them on the dashboard and the Tutor takes them into account as *data, not instructions* (stripped of markup, same text filter as the student's own notes: no links, contact details or rude words)
- 💛 **What I like**: up to 3 interests from a fixed list (never free text). The AI is told to set examples in those worlds, and offline, some word problems of primary mathematics are written around them — with the numbers and answers always computed in code
- 🪜 **Think with the Tutor, tell me how you thought, How I learn**: in a test the student can climb a **Socratic ladder** of 4 guiding questions (restate the problem, what is known, first step, method) — the first two are free, the last two count as hints, and **the Tutor never reveals the answer** (the code checks every reply for leaks and falls back to a safe question). After a wrong answer the student can say in a few words how they reasoned; the AI (or an offline keyword classifier) names the kind of mistake — misread, concept, procedure, slip, guess, forgot, rushed — and the code picks the remedy (re-read, explain, step by step, practise, think together, review, slow down); only the kind is stored, never the text. The page **How I learn** shows what the Tutor has noticed (best explanation method, autonomy without hints, over/under-confidence, main kind of mistake), lets the student say "that is not me", add notes about themselves, and the Tutor uses it to explain in the best method first; badge *Thinker*. Code decides, AI writes: all numbers are computed, the AI only phrases them
- 🦉 **The Tutor of every page**: a button in the header opens the Tutor from anywhere. The Tutor knows the student's progress and **evolution** (level, streak, tests and results, accuracy, tests and XP of the last 7 days compared with the 7 before, subjects improving, weak topics with their mastery, reviews due, recurring mistakes, concepts explained, interviews), **remembers the conversation** across pages (the student can delete it) and knows the current page. It proposes **actions computed in code** (review, practise the weakest topic, explain it to the Tutor, the Tutor's notebook, an adventure, an interview); the AI writes the reply and may only choose among those actions. During a test or an exercise the Tutor helps with the method and **never gives the answer**. Offline it answers "what should I do now?", "how am I doing?" and "help me with this page"
- 🎭 **Explain it to me your way**: the Tutor is a small hub, not just a chat — a "Your way" tab lets the student pick a metaphor (video game, recipe, adventure story, friendly debate) and every explanation from then on is framed through it, the facts unchanged (the metaphor is a system-prompt instruction, never content the platform invents). A badge shows the active style with a one-click way to drop it; picking a style is free, no round trip
- 🧠 **Real error memory**: a recurring mistake is not just counted — it is really remembered. Weeks later, Evo's notebook quietly repeats one of the student's own past mistakes among ordinary practice, never labelled as a test; only when that later, camouflaged check comes back right does the Tutor say a mistake is truly gone ("checked again weeks later, for real"), and a relapse afterwards puts it back under watch automatically
- 🎲 **A genuine confidence detector — lucky guess vs. real mastery**: the self-reported "sure / I think so / not sure" on each answer is the one signal raw accuracy and the knowledge tracker never see. When a topic already looks known but most of its right answers were "not sure", the Tutor says so honestly instead of just praising the score, and offers a one-click way to confirm it for real with a fresh, un-hinted question
- 🧑‍🏫 **Explain it to the Tutor**: the student becomes the teacher and explains a concept (fractions, percentages, Pythagoras, photosynthesis, density, loops…) in their own words, writing or by voice. A **rubric in code** checks the key ideas (words in 4 languages, case and accents ignored), the ideas stay secret until found, and the Tutor asks a naive question about the first one missing; up to 3 rounds, then a model explanation. The AI only writes the Tutor's words; offline, the Tutor's questions are written in advance
- 🖼️ **Pictures in some questions**: clocks, fraction circles, 100-square grids, rectangles, right triangles, lines on axes, angles, bar charts, polygons, circuits, series and parallel resistors, Punnett squares and supply and demand. The server sends a small specification of numbers (`dg:clock:3:30`) and the browser draws the SVG, so the pictures follow the theme, need no translation and cannot carry markup
- 🎙️ **Talk with the tutor**: answer test questions **with the microphone** (the option is recognised by its text, its letter or a number word) and dictate explanations, using the speech recognition of the browser (hidden where it is not available)
- 🎧 **Oral exam, hands-free**: a toggle next to "read aloud" chains the test's own voice both ways — the Tutor speaks the question, listens for the spoken answer, speaks the feedback, and moves to the next question on its own, with no tapping until the test ends. Built on the same read-aloud and microphone already used elsewhere on the page; a misheard answer turns it off and says so, rather than guessing or looping forever
- ⏳ **Interviews with historical figures**: original illustrated characters that **move their lips while they speak**, blink and look up while thinking, each with **its own voice** (a female or male browser voice of the language when available, its own pitch and speed); 16 figures answer in the first person (Marie Curie, Leonardo da Vinci, Isaac Newton, Ada Lovelace, Galileo Galilei, Charles Darwin, Miguel de Cervantes, Rosalind Franklin, Ibn Battuta, Adam Smith, Emmy Noether, Jean-François Champollion, Albert Einstein, William Shakespeare, Jane Goodall and Mansa Musa), one or more for each subject, **clearly labelled as an educational simulation** from documented facts (no invented quotes, age-appropriate, answers read aloud on request). Offline they answer the suggested questions; with an AI engine, any question. A themed intro animation opens the interview (a different flourish per subject), a progressive path lights up a milestone per question discovered, and the interview ends with a **downloadable postcard** (the figure's portrait and a fact, rendered client-side as a PNG), a 2-question **myth-busting round** (a surprising true fact and a common misconception, with the explanation either way) and a 3-question comprehension check built from checked facts
- 📚 **Dossier of each figure** (also a tab of the interview): what they discovered or did, explained with an **original illustration** (Jupiter's moons as Galileo drew them, Newton's prism, Darwin's finches, Photo 51 and the double helix, the Rosetta Stone, the pin factory…), their life in dates, a curious fact and **the historical evidence with a link to the original source** (NobelPrize.org, British Museum, Cambridge Digital Library, Library of Congress, Wellcome Collection, arXiv, Project Gutenberg…). It can be listened to with the voice of the figure. Each figure takes a **different browser voice** when there are several, with its own pitch and speed, and the student can try another voice
- 🖼️ **Figure album, timeline game and hidden connections**: completing a figure's interview (every suggested question discovered, its closing riddle answered) unlocks its card in a collectible **album** (`/Student/Album`); once 2 or more figures are unlocked, a **chronological timeline mini-game** (`/Student/Timeline`) lets the student place them in the order they were born and checks the result against the real dates. The album also reveals **curated connections between pairs of figures** once both are completed — real, specific links (Newton was born the year Galileo died and built on his work; Darwin and Newton rest a few steps apart in Westminster Abbey; Curie and Rosalind Franklin both likely died of illnesses linked to the radiation their discoveries depended on…), never a generic "both were scientists"
- ✨ **Any topic with the AI**: the AI writes 10 questions of the student's level, the parser keeps only valid ones (4 distinct options, one right) and the topic is treated as data, not instructions
- 🏆 **Gamification** (rules in Core, fair: no time pressure, no paid boosts): XP per answer (effort counts too), levels and titles (Explorer → Sage), **daily streak** and monthly calendar, **3 daily quests** chosen per student and day, **25 badges** with progress
- 🏟️ **Arena**: weekly class ranking and **teams** (score = average XP, so small teams can win), weekly class challenge, **duels** (two classmates answer the same 5 questions, same seed). Only **nicknames** are shown (an animal + a word), never real names, and a student can opt out of competition. The student can **pick their own avatar and nickname** from the safe, anonymous options (12 animals × 8 words) — free text is never allowed, so a nickname can never leak a real name
- 🧸 **Kids mode** for 5–8 year olds: pictures (count the apples, which flag, which animal), big buttons, questions **read aloud** with the browser's speech synthesis, an original owl mascot (the Tutor), and its own 3-stop menu (Home, Explore, Achievements) instead of the full menu built for an older student managing their own study plan. **Home and Achievements are their own screens for this age**, not the adult dashboard behind a size change: no XP numbers, no class ranking, no percentages — a big mascot greeting, the streak as a row of stars, one big "keep going" tile and subject tiles to tap, and achievements as a sticker shelf

**Student space: AI tutor**
- 🧭 **Adaptive placement test**: a binary search along the concepts ordered by prerequisites (about log2(n)+1 questions), then a starting probability per concept
- 🗺️ **Mastery map** per subject: the probability of knowing each concept, updated after every answer with **Bayesian Knowledge Tracing**
- 💡 **Explanations adapted to the student** with 5 methods (step by step, visual, real life, worked example, analogy); after 2 failures in a row the tutor **switches to a method not tried yet**
- ✍️ **Progressive exercises** (difficulty up with a streak, down after a mistake), **checked on the server**: the browser never receives the right option before answering
- 🧩 **Feedback on the misconception**: every wrong option is a typical mistake with a code ("you added the denominators"), so the student learns *why*, and recurring mistakes are detected
- 🔁 **Spaced review** of concepts mastered more than 7 days ago, and a **prerequisite refresh** when a concept resists
- 💬 **Socratic tutor** in a chat: hints and questions, never the final answer
- ➕ **Any subject from a topic**: the AI proposes the concept map, the code validates it (unique keys, known prerequisites, no cycle)
- 🏫 Join a teacher's class with a 6-character code

**Teacher space**
- 👥 Classes with a **join code** (no look-alike characters)
- 🌡️ **Heatmap students × concepts**, students who need help first, average progress, hardest concept
- 🔔 **Alerts from the tutor**: a recurring mistake, or a student still stuck after 3 explanation methods; the teacher marks them as handled
- 📊 **Most frequent mistakes of the class**, with the concept they belong to, and each student's recent answers
- 📦 **Lesson pack from a document** (PDF or text): 50-minute class plan, slides, worksheet, 10 exercises, exam with solutions, 4-level rubric; saved, printable, exportable to Markdown
- 🪜 **Lesson in 3 levels** with measured readability and 🎙️ **class summary** by dictation (from MatGPT Teacher)
- 🦉 **The teacher's own assistant**: the same companion widget the student has on every page, now also open to the teacher, scoped to **their own classes** instead of one student's progress. It knows who needs help first, how each class is doing and which concept to reteach — all computed in code from the same class follow-up data (heatmap, alerts, mistakes) — and proposes actions (check on a student, review a class, reteach a concept); the AI only writes the reply and picks among them, offline it answers the same questions from the facts directly. No new data is collected for it
- 🎯 **One-click practice nudge**: when a class has a hardest concept and students still struggling with it, the assistant offers a one-click action that writes each of them a short teacher note ("Practise: {concept}") — the existing, student-facing note the Tutor already reads — straight from the chat, with no page to fill in and no new system (notes keep their own 3-per-student limit, so a repeat click is told so rather than silently dropped)

**Institution space** (role *Management*)
- 🏫 Dashboard of the academy: students and active students, answers of the week, progress per class, teachers; teachers join the institution with a **code**
- 🚨 **Early dropout alert** (`DropoutRisk`): a score from simple, documented rules (inactivity, activity drop, accuracy, open alerts, progress), with the factors of every student; a high score is a reason to call, not a label
- ✉️ **Message to the family** written by the AI from the facts measured by the platform (first name only, contact data removed by the parser), warm or formal, in 4 languages; offline template without AI
- ⭐ **Quality**: right answers per explanation method, best method, alerts handled and average time to handle them, concepts mastered after a change of method
- 💶 **Plan and cost** computed in code (academy plan, included seats, cost per active student)

**Marketplace** (students and role *Private teacher*)
- 🔎 Private teachers with filters (subject, language, level, price) and **recommendations from the concepts the student finds hardest**, with the reasons (`TutorMatch`)
- 📅 **Free one-hour slots** from the teacher's weekly availability (time zone and daylight saving handled, 12 h notice, bookings excluded)
- 🧾 Booking → **simulated payment** (no card data, nothing charged) → confirmed → done → rating; free cancellation up to 24 h before, always refunded when the teacher cancels; unpaid bookings released after 30 minutes
- 💼 Teacher space: profile, subjects, languages, levels, price, weekly availability, upcoming lessons, reviews, **earnings after the platform commission** (15 %, 12 % from 500 €/month, 8 % from 1,500 €)

**Interface**
- 🎨 Design inspired by modern SaaS dashboards: floating glass panels, **Urbanist** font (served locally), plum and lavender palette, pastel KPI cards with **mini bar charts** of the week (all answers hatched, right answers solid) and trend pills
- ↔️ **Collapsible main menu** (icons only) with the button in the header or the `[` key, remembered in the browser
- 🎛️ **Display menu**: text size S / M / L (the whole interface scales), light / dark / system theme, 5 accent colours, reduced animations, reset
- ⌨️ **Quick navigation** with `Ctrl K` (or `/`): pages, actions (theme, menu, text size, focus, sign out) and languages
- 🎯 **Focus mode** on the learning page (`F`, `Esc` to leave): only the exercise stays on screen

**Everywhere**
- 🔐 Accounts with roles (ASP.NET Core Identity) and **demo accounts** with realistic, fictitious histories
- 🤖 **Choose your AI** in the header; 🧪 **offline demo mode**: the built-in *Fractions* subject works fully without AI (exercises generated in code, explanations from templates), and if an AI call fails, the offline version answers and the reason is shown
- 🗣️ Interface and content in **English, Français, Español and Català**; 🌗 responsive layout with dark mode

## How it works

```mermaid
flowchart LR
    S[Student] -->|placement test| DG[Diagnostic<br/>binary search]
    DG --> MS[(Mastery per concept<br/>SQLite)]
    MS --> P[NextStepPlanner<br/>learn · practise · review · prerequisite]
    P -->|explain| AI{AiEngines<br/>Claude · ChatGPT · Gemini · Ollama}
    AI -.fails.-> DEMO[Offline tutor]
    P -->|exercise| EX[FractionExercises in code<br/>or AI + strict parser]
    EX -->|answer checked on the server| ME[MasteryEngine<br/>BKT · method switch · recurring mistakes]
    ME --> MS
    ME -->|NeedsTeacher / recurring| AL[Alerts]
    AL --> T[Teacher<br/>heatmap · mistakes · alerts]
    MS --> T
```

Technical choices worth noting:

- **The code decides, the AI writes.** Mastery (`Bkt`, `MasteryEngine`), the next step (`NextStepPlanner`), the placement test (`Diagnostic`), method switches, alerts and the class report (`ClassInsights`) are plain C#, explainable and unit-tested. The model only writes explanations, exercises for generated subjects, concept maps and Socratic replies.
- **Never trust the model.** `LearningPrompts` parsers drop exercises without exactly 4 distinct options or with an invalid index, repair concept maps (unknown prerequisites removed, cycles broken) or reject them; `PackPrompt` keeps only valid exercises and complete rubrics.
- **Mathematically right by construction.** The built-in subject's exercises are generated in code, and a test checks 6,300 of them: the right option is right, no wrong option has the same value, every wrong option carries its misconception.
- **Answers checked on the server.** Exercises are stored as pending; the browser receives the question and the options only, and gets the correction with the answer (anti-cheating, and the history is reliable for the teacher).
- **Fallback, not failure** (`AiRunner`): if the AI fails (key, quota, model, timeout, invalid JSON) the offline version answers and the interface shows why.
- **Safe for minors.** Every tutor prompt asks for an empathetic, encouraging tone and age-appropriate content, forbids asking for personal data, brings off-topic talk back to learning and, if a student writes something worrying, suggests talking to their teacher or a trusted adult.
- **Privacy by design.** Only a first name is asked. The AI receives the school stage, the mastery and the mistake codes, never a name or an email; user texts go inside `<data>` tags declared as data, not instructions. The AI recommends; a person decides.
- **One structured-output layer for every provider** (Claude tool use, OpenAI-style function calling for ChatGPT/Ollama, Gemini native function calling), shared with the MatGPT apps.

## Project structure

```
src/
  EduVerse.Core/            Curriculum (subjects, concept maps, built-in Fractions), Learning (BKT, mastery engine,
                              diagnostic, planner, exercises in code, misconceptions, prompts, parsers, offline tutor),
                              Content (lesson pack), Teaching (class insights, weekly activity, join codes),
                              Institutions (dropout risk, quality, plans, family message), Marketplace (slots, commission, matching, booking rules),
                              Catalog (levels, 10 offline question banks, adaptive test, placement, AI topic prompt), Gamification (XP, streaks,
                              quests, badges, nicknames, teams, rankings, duels), Lessons and Summaries (from MatGPT Teacher)
  EduVerse.Infrastructure/  EF Core SQLite + Identity (entities, DbContext, schema version, demo seeder), learning, classroom,
                              institution, marketplace, quiz and gamification stores,
                              structured AI clients (Claude, OpenAI-compatible, Gemini), engine registry, AI adapters
  EduVerse.Web/             Razor Pages (landing, accounts, student, teacher, institution, marketplace and private teacher spaces),
                              minimal APIs, AiRunner, localization
    wwwroot/js/               shell.js (menu, display preferences, Ctrl+K, shortcuts), learn.js (learning loop and chat),
                              pack.js, subject.js, lesson.js, summary.js, common.js
    Styles/app.css            Tailwind v4 + Preline theme tokens (palette, accents, pastel tones, heatmap scale)
tests/
  EduVerse.Tests/           xUnit: BKT, mastery engine, diagnostic, planner, exercises, parsers, demos, class insights,
                              persistence on SQLite, web integration (WebApplicationFactory)
  e2e/                      Playwright end-to-end test (desktop + 390 px mobile, light + dark) that takes the screenshots
tools/
  generate_resx.py          The 4 interface languages (key: en, fr, es, ca)
```

## Getting started

Requirements: [.NET 10 SDK](https://dotnet.microsoft.com/download) or Docker.

```bash
git clone https://github.com/MathieuMoe/eduverse-ia.git
cd eduverse-ia
dotnet run --project src/EduVerse.Web
# open https://localhost:7250
```

On first start the app creates the SQLite database (`src/EduVerse.Web/App_Data/eduverse.db`, git-ignored) and the demo accounts. Click one of the **Try as…** buttons (student, teacher, management, private teacher, kids mode), or sign in:

| Account | Role | History |
|---|---|---|
| `teacher@eduverse.demo` | Teacher | Class *Maths 2B*, join code `FRAC24` |
| `sara@eduverse.demo` | Student | Keeps adding the denominators (recurring mistake); 8 tests, a 7-day streak, badges, a duel to play against Alex |
| `leo@eduverse.demo` | Student | Stuck on comparing fractions after 3 methods (teacher alerted) |
| `alex@eduverse.demo` | Student | Masters almost everything; a spaced review is due |
| `nora@eduverse.demo` | Student | New: starts with the placement test |
| `mia@eduverse.demo` | Student (6 years) | Kids mode: early-years tests with pictures and voice |
| `director@eduverse.demo` | Management | Academia Horizonte (code `HRZN24`): classes of Laura and Pablo, students with varied dropout signals |
| `tutor@eduverse.demo` | Private teacher | Diego, maths and physics: rated lessons, upcoming bookings, earnings |

Five more fictitious private teachers (Grace, Ahmed, Claire, Marc, Inés) and students of a second class (Iker, Omar, Elena, Noa, Hugo) are created too.

Password of every demo account: `EduVerse-demo-2026`. To start again from scratch, stop the app and delete `App_Data`.

The schema is created with `EnsureCreated` and versioned with SQLite `PRAGMA user_version`: in Development a database of an older version is recreated automatically (with the demo data); in other environments the app refuses to start with a clear message (add EF Core migrations before a real deployment).

Add one or more AI keys (stored with user-secrets, never in the repo); every configured engine appears in the header selector:

```bash
dotnet user-secrets set "Gemini:ApiKey" "AQ...." --project src/EduVerse.Web     # free tier: https://aistudio.google.com/apikey
dotnet user-secrets set "OpenAI:ApiKey" "sk-..." --project src/EduVerse.Web     # ChatGPT
dotnet user-secrets set "Claude:ApiKey" "sk-ant-..." --project src/EduVerse.Web # Claude
dotnet user-secrets set "Ollama:Enabled" "true" --project src/EduVerse.Web      # local and free: ollama pull llama3.2
```

| Setting | Default | Description |
|---|---|---|
| `ConnectionStrings:EduVerse` | `Data Source=App_Data/eduverse.db` | SQLite database (switching to SQL Server is a provider change). |
| `Demo:Enabled` | `true` | Creates the demo accounts and shows the "try as…" buttons. |
| `Marketplace:TimeZone` | `Europe/Madrid` | Time zone of the private teachers' weekly availability. |
| `Ai:Default` | first configured | Default engine: `claude`, `openai`, `gemini`, `ollama` or `demo`. |
| `Gemini:ApiKey` / `GEMINI_API_KEY` | – | Enables Gemini (native API, works with `AQ.` keys). Model `gemini-flash-lite-latest`. |
| `OpenAI:ApiKey` / `OPENAI_API_KEY` | – | Enables ChatGPT. Model `gpt-5-mini`. |
| `Claude:ApiKey` / `ANTHROPIC_API_KEY` | – | Enables Claude. Model `claude-sonnet-4-5`. |
| `Ollama:Enabled` | `false` | Enables a local Ollama model (`Ollama:Model`, `Ollama:BaseUrl`). |

With Docker: `docker compose up --build`, then open http://localhost:8080.

## API

All endpoints need a signed-in user (cookie) and the role of their space (*Student*, *Teacher*, *Management*).

| Endpoint | Returns |
|---|---|
| `GET /api/learn/{subject}` | Subject, ordered concepts, mastery states, next step |
| `POST /api/learn/{subject}/diagnostic` | Next placement-test question, or `{ finished, start }` |
| `POST /api/learn/{subject}/explain/{concept}` | `{ engine, fallback, explanation }` with the method chosen by the code |
| `POST /api/learn/{subject}/explained/{concept}` | Marks the explanation as read (practice comes next) |
| `POST /api/learn/{subject}/exercise/{concept}` | `{ engine, fallback, exercise }` without the answer |
| `POST /api/learn/answer` | `{ correct, correctIndex, explanation, misconception, events, state }` (body: `exerciseId`, `choice`) |
| `GET` / `POST /api/learn/{subject}/chat/{concept}` | Socratic chat history / reply (body: `message`) |
| `POST /api/learn/{subject}/reset` | Starts the subject again with a new placement test |
| `POST /api/subjects` | New subject from a topic (body: `topic`, `stage`); `409 needs_ai` in demo mode |
| `GET` / `POST /api/packs` · `GET` / `DELETE /api/packs/{id}` | Lesson packs of the teacher (body: `title`, `subject`, `stage`, `text`, `language`) |
| `POST /api/lesson` · `POST /api/summary` | Lesson in 3 levels · class summary |
| `POST /api/quiz/start` | First question of a test, without the answer (body: `area`, `level`, `mode` = `Practice`, `Placement`, `Duel`, `Evo`, `Notebook` or `Review`, `duelId`; `409 nothing_to_review` when no topic is due) |
| `POST /api/quiz/topic` | Test on a free topic written by the AI (body: `area`, `level`, `topic`); `409 needs_ai` in demo mode |
| `GET /api/quiz/{id}` · `POST /api/quiz/{id}/answer` | Current question · `{ correct, correctIndex, explanation, run, hints, confidence, next, summary, reward }` (body: `choice`, `confidence` 0–2) |
| `POST /api/quiz/adventure` | Starts an adventure (body: `area`, `level`, `theme` = `Space`, `Ocean`, `Jungle` or `Castle`); the story comes from the AI or, offline, from the written stories |
| `GET /api/companion` · `POST /api/companion` · `DELETE /api/companion` | the Tutor on every page: greeting, ideas and conversation (query `page`, `title`) · message (body: `message`, `page`, `title`, optional `style` = `game`/`recipe`/`story`/`debate`): `{ reply, actions, engine }` · delete the conversation |
| `GET /api/teacher-companion` · `POST /api/teacher-companion` · `DELETE /api/teacher-companion` | the teacher's own assistant: same shape as `/api/companion`, scoped to the teacher's own classes; teacher-only |
| `POST /api/teacher-companion/practice-note` | one-click nudge (query `classId`): writes a teacher note to every struggling student of that class; `{ saved, full, concept }` |
| `POST /api/teach/{concept}` | Explain it to the Tutor (body: `text`, `sessionId`): `{ ideas, reaction, question, round, done, model, reward }`; missing ideas stay secret |
| `POST /api/interview/{figure}` | Question to a historical figure (body: `question`, `history`): `{ answer, matched, engine }`; the check is `POST /api/quiz/start` with `mode = Interview` and `figure` |
| `POST /api/quiz/{id}/hint` · `POST /api/learn/hint` | Next step of the hint ladder (test · tutor): `{ level, removed, text }` |
| `POST /api/institution/family` | Message to a student's family (body: `studentId`, `tone`, `language`, `note`); role *Management* |

The marketplace and the institution pages use Razor Pages handlers (forms with antiforgery): booking, simulated payment, cancellation, rating, profile and availability, joining an institution.

Rate limits per IP: 10 AI calls per minute (explanations, chat, subjects, packs, lessons, summaries), 30 tutor messages (the Tutor, explain it to the Tutor, interviews), 40 practice calls and 120 test calls per minute.

## Tests

```bash
dotnet test                                   # 664 xUnit tests: rules, question banks, parsers, demos, SQLite, web integration

# End-to-end (the app running on a fresh database, see Getting started)
cd tests/e2e
npm install
npx playwright install chromium
npm test                                      # BASE_URL=http://localhost:5250 by default
```

## Roadmap

- [x] Institutions: dashboard, dropout radar, messages to families, quality, plan
- [x] Marketplace: private teachers, recommendations, slots, bookings, simulated payment, ratings, earnings
- [ ] Real payments with Stripe Connect (split of the commission) and invoices; online lessons with video
- [ ] Courses sold in the marketplace; CSV import of an institution's students (as in MatGPT Director)
- [ ] Language coach and exercise scanner from a photo
- [x] Student platform: catalog from 5 years old to doctorate, adaptive and placement tests, XP, streaks, quests, badges, rankings, teams, duels, kids mode
- [ ] Virtual labs (phase 2): interactive simulations in physics, chemistry, biology, maths, programming and the other subjects, computed in code and guided by the AI
- [ ] Knowledge generator (phase 3): any document → mind map, flashcards with spaced repetition, summary, test and a two-voice podcast read by the browser
- [ ] Competency maps across subjects and adaptive learning paths
- [ ] EF Core migrations and SQL Server for production; parental consent flow for minors

## Author

**Mathieu Moreau**, Full Stack .NET Developer & UI/UX Designer
[LinkedIn](https://www.linkedin.com/in/mathieu-moreau-045b26114/) · [GitHub](https://github.com/MathieuMoe) · see also [MatGPT Teacher](https://github.com/MathieuMoe/ai-teacher-assistant), [MatGPT Director](https://github.com/MathieuMoe/education-assistant), [MatGPT Tutor](https://github.com/MathieuMoe/ai-study-tutor), [MatGPT Quiz](https://github.com/MathieuMoe/ai-quiz-generator), [MatGPT Simulador](https://github.com/MathieuMoe/ai-oral-exam-simulator), [MatGPT StudyPlan](https://github.com/MathieuMoe/ai-study-planner) and [MatGPT Temario](https://github.com/MathieuMoe/ai-syllabus-builder)

Licensed under the [MIT License](LICENSE).
