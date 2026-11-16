<img src="src/EduVerse.Web/wwwroot/img/logo.svg" width="64" alt="" />

# EduVerse AI

**The AI teacher that knows each student.** One educational platform for **students, teachers, institutions and private teachers**. Students get a learning platform **from 5 years old to doctorate** in 10 subjects, with adaptive tests, placement tests, XP, streaks, badges, class rankings and duels. The tutor tests, explains, practises with feedback on every mistake, **changes its way of explaining when one does not work**, and **calls the human teacher** when a student is stuck. Teachers see their class at a glance and get a full lesson from any document; institutions get an early dropout alert; families can book a private teacher recommended from what the student finds hardest.

Built with **ASP.NET Core (.NET 10)**, **EF Core + SQLite**, **ASP.NET Core Identity**, your choice of AI (**Claude, ChatGPT, Gemini or a local Ollama model**) and a dashboard UI made with **Tailwind CSS + Preline UI**. Works **without any AI key** in demo mode.

![EduVerse AI: the learning loop with feedback on the misconception behind a wrong answer](docs/screenshot.png)

<details>
<summary>Student platform (catalog, adaptive test, arena, kids mode), teacher follow-up, institution, dropout radar, marketplace, private teacher, lesson pack, focus mode, mobile and dark mode</summary>

![Student dashboard: XP, level, streak, rank, quests, activity calendar, podium](docs/screenshot-student.png)
![Catalog of tests: 10 subjects, 10 levels from early years to doctorate](docs/screenshot-explore.png)
![Adaptive test: the answer key arrives only after answering](docs/screenshot-quiz.png)
![Arena: weekly ranking with nicknames, teams, class challenge, duels](docs/screenshot-arena.png)
![Achievements: titles, badges and their progress](docs/screenshot-achievements.png)
![Review of a finished test](docs/screenshot-history.png)
![Daily review with the hint ladder and the confidence meter](docs/screenshot-review.png)
![The Tutor's notebook: The Tutor makes the student's own mistakes](docs/screenshot-evo.png)
![Adventure: doors, hearts, the story and the final guardian](docs/screenshot-adventure.png)
![The Tutor of every page: knows the progress, remembers the conversation and proposes actions](docs/screenshot-companion.png)
![Explain it to the Tutor: the student teaches, the rubric finds the key ideas](docs/screenshot-teach.png)
![Interview with Marie Curie: illustrated figure that moves its lips, own voice, educational simulation](docs/screenshot-interview.png)
![Learn: choose what to learn among every subject at your level, your own subjects and the trades](docs/screenshot-learn-catalog.png)
![Trades and practical skills: electricity, solar energy, garden, plumbing, carpentry, first aid, cooking, bike repair, painting and beekeeping, each concept with the science behind it](docs/screenshot-trades.png)
![Hobbies: astronomy, music, magic and illusions, survival and orienteering, chess, dinosaurs and fossils](docs/screenshot-hobbies.png)
![Internal certificate of a trade, printable](docs/screenshot-certificate.png)
![Dossier of Galileo: the discovery with an original illustration, the life in dates and the historical evidence with a link to the source](docs/screenshot-dossier.png)
![Some test questions come with a picture drawn in the browser](docs/screenshot-diagram.png)
![Kids mode for 5-8 year olds: pictures, big buttons, questions read aloud](docs/screenshot-kids.png)
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
- 📝 **Tests done** saved with every question, the answer given and the right one: list per subject and **review** of each test
- 🎚️ **One level of study for all subjects**: changing it (dashboard or catalog) applies it to every subject; a placement test can then refine one subject
- 🧭 **Placement test per subject**: a binary search over the 10 levels (2 questions per level, 10 at most) gives the level to practise
- 🦉 **Check the Tutor** (every subject): the mascot answers and the student decides if it is right; when it is wrong, its answer is a typical mistake and the student picks the right one (the Tutor is right 1 time in 3, so guessing "wrong" does not work)
- 📓 **The Tutor's notebook**: The Tutor solves fraction exercises making the student's **own** mistakes from the tutor history (e.g. adding the denominators) and the student says what went wrong
- 💡 **Hints in a ladder**, decided in code, in the tests and in the tutor: one wrong option disappears → 50/50 → the method with the answer hidden (or its first letter); the answer is never given and each hint takes 25 % of the XP of the question
- ⚡ **Daily review** (spaced repetition, Leitner boxes per topic: 1, 2, 4, 7, 15, 30 days): 6 questions from every subject on the topics about to be forgotten
- 😎 **Confidence meter**: before answering, "not sure / I think so / sure"; sure and right earns a bonus, sure and wrong sends the topic to today's review; the achievements page shows the calibration
- 🗺️ **Adventures with a final guardian**: choose a world (space, ocean, jungle, castle) and a subject; 5 doors open with right answers, then a friendly guardian asks 4 questions on the student's **weakest topics** at the hardest difficulty; 3 hearts for the whole adventure. The code decides the questions and the progress; the AI only tells the story (age-appropriate, no violence), and offline stories are written in advance in 4 languages
- 📚 **Learn any subject with the tutor**: Learn opens with a catalog to choose what to learn — the 10 subjects of the catalog at the student's level (each topic of the question bank becomes a concept of the tutor, with exercises generated and solved in code, pictures included), fractions with the detection of misconceptions, the student's own subjects created with the AI, the trades and the hobbies; "continue where you left off" shows the last ones
- 🛠️ **Trades and practical skills** (a tab of Learn, next to the subjects): **electricity at home, solar energy, vegetable garden, plumbing, carpentry, first aid, cooking and baking, bicycle mechanics, painting and decorating, beekeeping**, each with 5 concepts connected to the science behind them (Ohm's law, the photovoltaic effect, photosynthesis, pressure, Pythagoras, fermentation and the Maillard reaction, gear ratios, pigments, the hexagons of the comb…) and 15 questions written and checked by hand in 4 languages, so they work offline with the same tutor (explanations in 5 methods, adaptive exercises, knowledge tracing). Safety notes where they matter (electricity, first aid with a pointer to a certified course and 112, power tools, hives). When every concept is mastered the code issues an **internal certificate** (printable, with a code) plus XP and the badges *Apprentice* and *Master of trades*; it proves the theory learned, not a professional qualification
- 🎨 **Hobbies** (third tab of Learn): fun things to learn for the pleasure of it — **astronomy, music, magic and illusions, survival and orienteering, chess, dinosaurs and fossils** — built the same way (5 concepts linked to physics, biology, maths, history…, 15 checked questions in 4 languages, certificate) with their own badge *Curious mind*
- 🦉 **The Tutor of every page**: a button in the header opens the Tutor from anywhere. The Tutor knows the student's progress and **evolution** (level, streak, tests and results, accuracy, tests and XP of the last 7 days compared with the 7 before, subjects improving, weak topics with their mastery, reviews due, recurring mistakes, concepts explained, interviews), **remembers the conversation** across pages (the student can delete it) and knows the current page. It proposes **actions computed in code** (review, practise the weakest topic, explain it to the Tutor, the Tutor's notebook, an adventure, an interview); the AI writes the reply and may only choose among those actions. During a test or an exercise the Tutor helps with the method and **never gives the answer**. Offline it answers "what should I do now?", "how am I doing?" and "help me with this page"
- 🧑‍🏫 **Explain it to the Tutor**: the student becomes the teacher and explains a concept (fractions, percentages, Pythagoras, photosynthesis, density, loops…) in their own words, writing or by voice. A **rubric in code** checks the key ideas (words in 4 languages, case and accents ignored), the ideas stay secret until found, and the Tutor asks a naive question about the first one missing; up to 3 rounds, then a model explanation. The AI only writes the Tutor's words; offline, the Tutor's questions are written in advance
- 🖼️ **Pictures in some questions**: clocks, fraction circles, 100-square grids, rectangles, right triangles, lines on axes, angles, bar charts, polygons, circuits, series and parallel resistors, Punnett squares and supply and demand. The server sends a small specification of numbers (`dg:clock:3:30`) and the browser draws the SVG, so the pictures follow the theme, need no translation and cannot carry markup
- 🎙️ **Talk with the tutor**: answer test questions **with the microphone** (the option is recognised by its text, its letter or a number word) and dictate explanations, using the speech recognition of the browser (hidden where it is not available)
- ⏳ **Interviews with historical figures**: original illustrated characters that **move their lips while they speak**, blink and look up while thinking, each with **its own voice** (a female or male browser voice of the language when available, its own pitch and speed); 12 figures answer in the first person (Marie Curie, Leonardo da Vinci, Isaac Newton, Ada Lovelace, Galileo Galilei, Charles Darwin, Miguel de Cervantes, Rosalind Franklin, Ibn Battuta, Adam Smith, Emmy Noether and Jean-François Champollion), one or more for each subject, **clearly labelled as an educational simulation** from documented facts (no invented quotes, age-appropriate, answers read aloud on request). Offline they answer the suggested questions; with an AI engine, any question. The interview ends with a 3-question comprehension check built from checked facts
- 📚 **Dossier of each figure** (also a tab of the interview): what they discovered or did, explained with an **original illustration** (Jupiter's moons as Galileo drew them, Newton's prism, Darwin's finches, Photo 51 and the double helix, the Rosetta Stone, the pin factory…), their life in dates, a curious fact and **the historical evidence with a link to the original source** (NobelPrize.org, British Museum, Cambridge Digital Library, Library of Congress, Wellcome Collection, arXiv, Project Gutenberg…). It can be listened to with the voice of the figure. Each figure takes a **different browser voice** when there are several, with its own pitch and speed, and the student can try another voice
- ✨ **Any topic with the AI**: the AI writes 10 questions of the student's level, the parser keeps only valid ones (4 distinct options, one right) and the topic is treated as data, not instructions
- 🏆 **Gamification** (rules in Core, fair: no time pressure, no paid boosts): XP per answer (effort counts too), levels and titles (Explorer → Sage), **daily streak** and monthly calendar, **3 daily quests** chosen per student and day, **25 badges** with progress
- 🏟️ **Arena**: weekly class ranking and **teams** (score = average XP, so small teams can win), weekly class challenge, **duels** (two classmates answer the same 5 questions, same seed). Only **nicknames** are shown (an animal + a word), never real names, and a student can opt out of competition
- 🧸 **Kids mode** for 5–8 year olds: pictures (count the apples, which flag, which animal), big buttons, questions **read aloud** with the browser's speech synthesis, an original owl mascot (the Tutor)

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
| `GET /api/companion` · `POST /api/companion` · `DELETE /api/companion` | the Tutor on every page: greeting, ideas and conversation (query `page`, `title`) · message (body: `message`, `page`, `title`): `{ reply, actions, engine }` · delete the conversation |
| `POST /api/teach/{concept}` | Explain it to the Tutor (body: `text`, `sessionId`): `{ ideas, reaction, question, round, done, model, reward }`; missing ideas stay secret |
| `POST /api/interview/{figure}` | Question to a historical figure (body: `question`, `history`): `{ answer, matched, engine }`; the check is `POST /api/quiz/start` with `mode = Interview` and `figure` |
| `POST /api/quiz/{id}/hint` · `POST /api/learn/hint` | Next step of the hint ladder (test · tutor): `{ level, removed, text }` |
| `POST /api/institution/family` | Message to a student's family (body: `studentId`, `tone`, `language`, `note`); role *Management* |

The marketplace and the institution pages use Razor Pages handlers (forms with antiforgery): booking, simulated payment, cancellation, rating, profile and availability, joining an institution.

Rate limits per IP: 10 AI calls per minute (explanations, chat, subjects, packs, lessons, summaries), 30 tutor messages (the Tutor, explain it to the Tutor, interviews), 40 practice calls and 120 test calls per minute.

## Tests

```bash
dotnet test                                   # 261 xUnit tests: rules, question banks, parsers, demos, SQLite, web integration

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
