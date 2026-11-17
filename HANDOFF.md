# EduVerse AI: handoff (continue in another Claude session)

Source of the idea: ChatGPT conversation "Propuesta de aplicación educativa IA" (EduVerse AI: one platform, four profiles: student, teacher, institution, marketplace).
Decisions taken with Mathieu:
- First version = **MVP Phase 1**: accounts and roles; **student space** with the "AI teacher that knows the student"
  (placement test, mastery map per concept, explanation adapted to the student, progressive exercises, recurring mistakes,
  switch of explanation method, alert to the human teacher) + **teacher space** (lesson pack generator from a document:
  class plan, slides, worksheet, 10 exercises, exam with solutions, rubric; lesson in 3 levels; class summary; class follow-up).
- **Reuse the MatGPT code** (same base as ai-teacher-assistant: .NET 10, IStructuredAi with Claude / ChatGPT / Gemini / Ollama + offline demo,
  4 UI languages en/fr/es/ca, Tailwind v4 + Preline 5, Razor Pages + minimal APIs, TreatWarningsAsErrors).
- **Persistence: SQLite + EF Core + ASP.NET Core Identity** (demo accounts per role). Switching to SQL Server later = configuration.
- Principle everywhere: **the code computes, the AI writes**; every AI answer is validated; if the AI fails, the offline version answers and the reason is shown (see AiRunner in education-assistant).
- Repo name: eduverse-ai (namespace EduVerse), port https://localhost:7250 / http://localhost:5250, UserSecretsId eduverse-web-9c27e4.

## State (November 2026): MVP Phase 1 done
Builds with 0 warnings, 222 xUnit tests green, Playwright E2E green (desktop + 390 px mobile, light + dark, 4 languages), README with screenshots.

- Core: curriculum, BKT, MasteryEngine, Diagnostic, NextStepPlanner, FractionExercises, LessonPack (as before) +
  Teaching/ClassInsights (heatmap, concept summaries, frequent misconceptions), Teaching/JoinCode, Learning/Misconceptions.
  Fixes found by the tests: the NeedsTeacher alert now lists only the methods really tried; "ReviewPrerequisite" was unreachable
  (prerequisites always come first in the order) and is now "a concept resists (>= 3 answers, p < 0.4): review its weakest
  prerequisite, at most once a day"; addition exercises use the codes MultipliedInsteadOfAdding / NotConverted.
- Infrastructure: Data/ (AppUser, StudentProfile, SubjectRecord with JSON concepts, MasteryRecord, Attempt, PendingExercise,
  DiagnosticRecord, Classroom + ClassMember, Alert, LessonPackRecord, ChatMessage; EduVerseDb = IdentityDbContext on SQLite,
  DateTimeOffset stored as binary for sorting), DemoSeeder (teacher Laura + Alex/Sara/Leo/Nora replayed through MasteryEngine,
  class "Maths 2B" code FRAC24, password EduVerse-demo-2026), LearningStore, ClassroomStore, AiLearning, AiPackGenerator, ApiError.
  EnsureCreated at start (no migrations yet).
- Web: landing with "try as student/teacher", Account (Login, Register with role, Logout), Student (Index, Learn, NewSubject),
  Teacher (Index, Class, Pack, Lesson, Summary), About; APIs LearningEndpoints (/api/learn), ContentEndpoints (/api/subjects,
  /api/packs), TeachingEndpoints; AiRunner with fallback (+ AiUnavailableException / needs_ai); policies Student/Teacher;
  rate limits "ai" 10/min and "learn" 40/min; answers checked on the server (the browser never gets correctIndex before answering).
- Localization: tools/generate_resx.py (key: en, fr, es, ca) merges into the existing resx.
- E2E: tests/e2e (npm install, CHROME_PATH or npx playwright install chromium, app running on a fresh DB).

## Design (Nov 2026)
Inspired by the Dribbble "Recruitment Dashboard" by Ronas IT: Urbanist (wwwroot/lib/urbanist), plum/lavender tokens in Styles/app.css
(overrides of the Preline variables + accents via html[data-accent]), floating glass sidebar and header, pastel KPI cards (tone-*)
with mini bar charts (_MiniBars partial, Core/Teaching/Activity), pastel heatmap scale (heat-*). Interface preferences in
wwwroot/js/shell.js (localStorage "ev_ui"): collapsible sidebar (html.sb-collapsed, key "["), text size, theme, accent,
reduced motion, focus mode on Learn (html.focus-mode, F / Esc), Ctrl+K quick navigation.

## Institutions and marketplace (Nov 2026)
Roles Director (UI "Management") and Tutor (private teacher). Core: Institutions/DropoutRisk, Quality, Plans, FamilyMessage
(prompt + parser that removes contact data + DemoFamilyWriter); Marketplace/MarketplaceRules (Availability with time zone and DST,
Commission 15/12/8 %, TutorMatch, BookingRules). Infrastructure: Institution, InstitutionMember, TutorProfile, Booking;
InstitutionStore, MarketplaceStore, AiFamilyWriter; schema version via PRAGMA user_version (EduVerseDb.DatabaseVersion = 2,
recreated in Development, refused elsewhere). Web: /Institution (Index, Risk + family.js, Quality), /Marketplace (Index, Tutor,
Bookings), /Tutor (Index, Profile), POST /api/institution/family. Payments are simulated (no card data).
Seed: director Marta (Academia Horizonte, code HRZN24), teacher Pablo with class Maths 3A (Iker, Omar, Elena, Noa, Hugo),
6 private teachers (tutor@eduverse.demo = Diego) with rated lessons and upcoming bookings.

## Student platform, phase 1 (Nov 2026)
Asked by Mathieu: the student side as a real learning platform, from 5 years old to doctorate, in 3 phases
(1: catalog + tests + gamification + kids mode — DONE; 2: virtual labs in every subject, incl. grammar, languages, history,
geography, economics; 3: knowledge generator — document → mind map, flashcards, summary, test, podcast read with the browser voice).
Core/Catalog: Level (10), Area (10), Text (4 languages "en|es|fr|ca"), Mc (question builder), banks per subject (MathBank,
ScienceBanks, LanguageBanks, SocialBanks, ProgrammingBank, Facts), QuestionBanks.Generate(area, level, difficulty, seed, language),
AdaptiveQuiz, Placement (binary search over levels), TopicQuizPrompt (AI, parser). Core/Gamification: Xp, Streaks, Quests (FNV hash),
Badges (17), Nicknames, Teams, Leaderboard, Duels. Infrastructure: QuizAttempt, QuizAnswer, TopicSkill, SubjectLevel, XpEvent,
BadgeAward, QuestClaim, Duel; QuizStore, GamificationStore; StudentProfile.Level/Nickname/Avatar/Team/Compete; DatabaseVersion = 3.
Web: /Student (gamified dashboard), /Student/Explore, /Student/Quiz (quiz.js), /Student/Arena, /Student/Achievements; /api/quiz;
tutor answers give XP too. Strings of the student platform in tools/strings_student.py (merged by generate_resx.py).
Note: Text in Core uses the order en|es|fr|ca; generate_resx.py uses (en, fr, es, ca).
Tests done: QuizAnswer keeps prompt, options, choice, right index, explanation (review at /Student/History); new tests avoid the last
300 questions seen (QuizStore.KeyOf); ApplyLevelToAllAsync sets the level of every subject (dashboard settings, catalog button).
DatabaseVersion = 4. Rate limit "quiz" 120/min (no AI).
Demo: mia@eduverse.demo (kids mode); Sara has tests, a streak, badges and a duel from Alex waiting.

## Tutor fun, block A (Nov 2026)
Asked by Mathieu: ideas 1 (spot Evo's mistake), 2 (hint ladder) and 5 (daily review + confidence); 4 (adventures with a boss) next.
Core: Catalog/Hints.cs (ladder), Catalog/Evo.cs (EvoQuestions.Check, EvoNotebook with action descriptions of the 13 fraction
misconception codes, Spaced = Leitner boxes), QuizMode Evo/Notebook/Review, QuestionBanks.ForTopic, Xp.ForAnswer(hints, sure),
quests Review/CheckEvo, badges Reviewer/Detective. Infrastructure: QuizAnswer.Evo/Hints/Confidence, TopicSkill.Box/DueAt,
QuizAttempt.PlanJson/CurrentHints, PendingExercise.Hints; QuizStore.HintAsync/ReviewPlanAsync/MistakesAsync; LearningStore.HintAsync.
DatabaseVersion = 5. Seeded tests are placed on local days (LocalAt) so streaks do not depend on the hour of seeding.

## Tutor fun, block B: adventures (Nov 2026)
Core/Catalog/Adventures.cs: Theme, AdventureState, Adventure (5 doors, boss 4 HP, 3 hearts, wrong answer = heart lost and the door asks
again), Story, StoryPrompt (AI writes only the story; theme/subject/level sent, nothing typed by the student), DemoStories (4 themes ×
4 languages, gender-neutral wording). QuizMode.Adventure in QuizStore (AdventureJson, StoryJson, Won; boss = difficulty 3 on the
weakest TopicSkills), POST /api/quiz/adventure (AiRunner with offline fallback), /Student/Adventures, map/hearts/boss in quiz.js,
badges BossSlayer/Hero, Xp.AdventureWon. DatabaseVersion = 6.

## Tutor fun, block C: explain it to Evo, voice, interviews (Nov 2026)
Core/Tutor/TeachEvo.cs: 8 TeachConcepts with 3 Ideas each (keywords "en|es|fr|ca", ";" = any, "a+b" = all), Covered (normalised,
no accents), XpFor (10 per idea + 20 if complete), TeachPrompt (AI writes Evo's reaction/question; Demo offline). Alias
`using Txt = EduVerse.Core.Catalog.Text;` because the namespace EduVerse.Core.Text hides the type. Infrastructure/Tutor/TeachStore
(TeachSession entity, 3 rounds, XP "teach"). Core/Tutor/Interview.cs: 4 figures, prepared answers matched by keywords, InterviewPrompt
(simulation, documented facts, no quotes), Check → QuizMode.Interview (pool like Topic). TutorEndpoints: /api/teach/{concept},
/api/interview/{figure}, limiter "tutor" 30/min. wwwroot/js/voice.js (SpeechRecognition: listen, match, dictation) used by quiz.js
(🎤 button), teach.js and interview.js. Badges Teacher/TimeTraveler (23), quest TeachEvo. DatabaseVersion = 7.

## Pictures and talking figures (Nov 2026)
Core/Catalog/Diagram.cs: "dg:kind:args" specs (13 kinds, IsValid allows only digits, dots, commas, A/a, p, s), used by MathBank
(new Clock topic in Primary1, polygons, percent grid, rectangles, pies, Pythagoras, slope, angle, variance bars), ScienceBanks
(Ohm circuit, resistors, Punnett) and SocialBanks (market). wwwroot/js/diagrams.js draws them (quiz.js, History hydrates
[data-diagram]); colours --dg-accent/--dg-soft in app.css. wwwroot/js/avatars.js: original SVG figures with 3 mouths
(data-mouth), blink, bob while talking (.is-talking), look up (.is-thinking). Interviews.Voice(figure) → data-female/pitch/rate;
interview.js picks a voice by name heuristics, lips follow the speech (or the typing with the voice off), start button
because browsers need a click before speaking, data-busy for the e2e. Load avatars.js AFTER common.js (common.js replaces window.Edu).

## Evo on every page (Nov 2026)
Core/Tutor/Companion.cs: StudentSnapshot (names already localised), Suggest (actions decided in code: review, practice, teach,
notebook, adventure, interview, explore; max 3), Greeting, Progress, PageHelp, Demo (intents: help in a test → never the answer,
this page, how am I doing, what now, greetings), CompanionPrompt (snapshot as data, InTest rule, AI picks action ids, Parse filters
unknown ids). Infrastructure/Tutor/CompanionStore (SnapshotAsync from QuizAttempts, TopicSkills, reviews, misconceptions,
TeachSessions; CompanionMessage entity, last 200 kept, ClearAsync). DatabaseVersion = 8. TutorEndpoints: GET/POST/DELETE
/api/companion (limiter "tutor"). Layout: header button + <aside id="companion"> for students; wwwroot/js/companion.js is
self-contained on purpose (pages load common.js later, which replaces window.Edu).

## 12 interview figures (Nov 2026)
Core/Tutor/InterviewFigures.cs adds 8 figures (nested class More, read through Interviews.Registry so the static
initialisation order across the partial files does not matter). Each one: 4 prepared answers with keywords, 3 checks, a voice in
Interviews.Voice and an SVG in avatars.js. Companion.FigureFor picks the figure of each subject.

## "Evo" renamed "Tutor" (Nov 2026)
All the texts say "the Tutor" (fr "le Tuteur"): the companion, check the Tutor, the Tutor's notebook, explain it to the Tutor.
Code names keep "Evo" (QuizMode.Evo, EvoQuestions, EvoNotebook, TeachEvo.cs, quest keys) so data and URLs do not change.
The snapshot now has an Evolution (last 7 days vs the 7 before: answers, accuracy, tests, XP, areas improving by 10 points).

## Dossiers and voices (Nov 2026)
Core/Tutor/Dossiers.cs: one Dossier per figure (timeline, discovery, evidence, fact, checked https sources). Pages/Student/_Dossier
partial, /Student/Dossier and a tab in /Student/Interview. wwwroot/js/discoveries.js (12 original SVG illustrations),
figurevoice.js (voice per figure: Interviews.VoiceIndex among the same kind, natural voices first, "another voice" saved in
localStorage), dossier.js (listen with the figure's voice). If a source URL moves, change it in Dossiers.cs.

## Full width and breadcrumb (Nov 2026)
The content uses the whole width (no max-w on the main container or the page roots; only long paragraphs keep max-w-3xl).
Breadcrumb.cs + Pages/Shared/_Breadcrumb.cshtml, rendered by the layout on every page except the homes: back button + Home ›
trail › current. Pages deeper than the menu call Breadcrumb.Set(ViewData, current, (href, label)…) (Quiz by mode, Teach, Interview,
Dossier, History review, Learn, Teacher/Class, Marketplace/Tutor); the others take the label of their menu item.

## Trades and certificates (Nov 2026)
Core/Curriculum/Trades.cs + TradesContent.cs: 4 trades (keys "trade-…"), 5 concepts each with a science link (Area), 15 checked
questions each; Trades.Subject gives a built-in Subject, Trades.Exercise a question. DemoLearningAi and AiLearning serve them from
the bank (subject.BuiltIn). LearningStore lists them for students and teachers (ClassroomStore accepts them). Certificate entity
(unique per user and trade, code EV-XXXXXXXX), CertificateStore.IssueIfEarnedAsync called after each answer in /api/learn/answer
(XP 100, badges Apprentice/MasterCraftsman). The trades are the tab "trades" of /Student/Learn (partial _TradeCards; /Student/Trades redirects there) and /Student/Certificate (print CSS). Demo: Sara has the garden
certificate EV-GARDEN26 and has started electricity. DatabaseVersion = 9.

## More trades and hobbies (Nov 2026)
10 trades + 6 hobbies (16 entries). Trade has `TradeKind Kind` (Trade | Hobby); Trades.All = Registry.List joins the nested lists
Core (TradesContent.cs: electricity, garden, plumbing, carpentry), MoreTrades (TradesMore.cs: solar, firstaid, cooking),
MoreTrades2 (TradesMore2.cs: bike, painting, beekeeping), HobbyList (Hobbies.cs: astronomy, music, magic) and HobbyList2
(Hobbies2.cs: survival, chess, dinosaurs). Hobbies keep the "trade-" prefix so the learning, certificate and classroom code is shared;
Trades.OfKind / Trades.TabOf choose the Learn tab ("trades" or "hobbies"), the breadcrumb and the optgroup of the subject selector.
Badge Hobbyist (26 badges), BadgeStats.Hobbies. Demo: Sara has 2 astronomy concepts mastered. To add a trade or hobby: append it to
one of those lists (5 concepts, 3 questions per concept with difficulties 1-3, 3 wrong answers; TradeRuleTests checks it).

## Learn: catalog of every subject (Nov 2026)
Core/Curriculum/AreaSubjects.cs: "area-{Area}-{Level}" subjects; concepts = topics of the bank (sampled, cached), description =
explanation of a solved question, exercises from QuestionBanks.ForTopic (Exercise now has Visual and Code, shown by learn.js).
ICatalogNames (Core) gives localised names: LocalizedCatalogNames in Web, KeyCatalogNames by default. LearningStore.AreaLevelsAsync
= level of the student per area. /Student/Learn without a subject = catalog (continue, subjects, own, trades). Fixed: the DNA
complement question could lack distinct options.

## Next
1. Ideas left from the tutor list: 6 student-made tests for the class; then phase 2 virtual labs and phase 3 knowledge generator.
2. Real payments (Stripe Connect), video lessons, courses in the marketplace, CSV import of students.
3. EF Core migrations + SQL Server option; parental consent for minors; teacher can edit an AI concept map before assigning it.

## Students' ideas and adventure ratings (Nov 2026)
Core/Ideas/Ideas.cs: IdeaKind (Figure, Adventure, Trade, Hobby), IdeaStatus (New, Approved, InProgress, Available, Rejected), IdeaRules
(Normalize = grouping key, Check = length + no links/e-mails/phones/rude words, Existing = already offered, DailyLimit 5, AvailableXp 30),
IdeaNews. Infrastructure: entities StudentIdea (unique Kind+Key), IdeaVote (key IdeaId+UserId, Proposed, Reason, SeenAt), AdventureRating
(key AttemptId); Ideas/IdeaStore (ListAsync shows public ideas + the student's own pending ones, ProposeAsync, VoteAsync, AllAsync,
SetStatusAsync awards XP to proposers when Available, NewsAsync/MarkSeenAsync, RateAdventureAsync, AdventureRatingsAsync).
Web: IdeaEndpoints (/api/ideas/{kind} GET/POST, /api/ideas/vote/{id}, /api/adventure-rating/{attemptId}; limiter "ideas"), partial
Shared/_Ideas + js/ideas.js on Interview (list), Adventures and the trades/hobbies tabs of Learn; survey in quiz.js at the end of an
adventure; /Moderation/Ideas (policy Moderator = Teacher or Director) in both menus. The Tutor (StudentSnapshot.News) greets with the
idea that became available and proposes it as first action. Badge IdeaMaker (27 badges; 28 with Thinker). DatabaseVersion = 10. Demo: 10 seeded ideas
and 10 ratings. Possible next steps: AI check that a proposed figure is real, e-mail digest for the director, ideas per institution.

## Think with the Tutor, reasoning diagnosis, How I learn (Nov 2026)
Core/Tutor/Thinking.cs: ThinkingError (Misread, Concept, Procedure, Slip, Guess, Forgot, Rushed) -> Remedy via RemedyFor; offline keyword
Classify; ThinkingPrompt (tool thinking_diagnosis, enum schema, text in a <reasoning> data tag). Ladder.cs: 4 rungs, Free 2,
HintCost(rungs) = Clamp(rungs-2,0,2) added to the hints XP cost, not allowed in Placement/Duel, leak check + fallback question on rung 4.
LearnerProfile.cs: LearnerStats -> Insights (method, autonomy, overconfident, underconfident, error, reflective; minimum samples so nothing is
said without evidence), Summary (offline), ForPrompt (notes in <note> tags) and LearnerPrompt. Hints.cs guards one-character answers.
Infrastructure: entities ThinkingNote (kind only, unique AttemptId+Number), LearnerNote (<=160), LearnerHidden; QuizAttempt.CurrentRungs,
QuizAnswer.Rungs; Tutor/LearnerStore (WrongAnswerAsync, SaveThinkingAsync: +5 XP, 5 a day, Review remedy makes the topic due now; StatsAsync
over the last 200 answers; notes; hidden insights); QuizStore.LadderAsync. DatabaseVersion = 11.
Web: LearnerEndpoints (POST /api/quiz/{id}/ladder, POST /api/thinking, notes, /api/learner/insight/{key}, /api/learner/summary; limiter
"tutor"), Pages/Student/Learner + js/learner.js, quiz.js (ladder panel, thinking block), learn.js starts new concepts with the best method.
The Companion knows StudentSnapshot.Learner ("how do I learn?"). Badge Thinker (5 reflections). Demo: Sara has 6 notes of reasoning and
a visual-vs-step-by-step history. Possible next steps: notebook photo, interests woven into problems, goal and plan, weekly narrative report,
AI check of proposals.

## Plan, homework, defence, opening, interests (Nov 2026)

- Database version **12** at the time (now 13, recreated in Development): `StudyGoals` (PK UserId), `DefenceResults`, `StudentProfile.Interests` (csv of keys, max 3).
- **Plan**: `Core/Tutor/StudyPlan.cs` (pure, rebuilt daily from mastery), `Infrastructure/Tutor/PlanStore.cs`, `Web/PlanEndpoints.cs` (POST/DELETE `/api/plan`), page `/Student/Plan`, `QuizMode.Focus` (one topic, length 5). The Companion gets a `PlanBrief`.
- **Homework**: `Core/Tutor/Homework.cs` (the code checks the arithmetic, `Merge` never lets the AI mark a right line wrong), `Web/HomeworkEndpoints.cs` (`POST /api/homework/check`, photo: consent + not kids + magic bytes + ≤ 4 MB, memory only; demo engine with a photo → 409 `needs_ai`), `StructuredPrompt.Image` in the three providers.
- **Defence**: `Core/Tutor/Defence.cs`, `Infrastructure/Tutor/DefenceStore.cs` (stores seed, correct index and result, never the explanation), `Web/DefenceEndpoints.cs`, page `/Student/Defend`, column *Understood* in `/Teacher/Class`.
- **Opening**: `Core/Tutor/Opening.cs` + `Infrastructure/Tutor/OpeningStore.cs`; card `data-testid=opening` on `/Student`.
- **Interests**: `Core/Tutor/Interests.cs` (fixed list, prompt line, `InterestProblems` for primary maths), `POST /api/learner/interests`, card on `/Student/Learner`; `QuizStore.WithInterest` swaps a word problem in Practice (every 3rd question) and Focus (every 2nd); `StudentContext.Interests` goes into the AI prompts.
- **Letter**: `Core/Tutor/WeeklyLetter.cs` + `Infrastructure/Tutor/LetterStore.cs`, page `/Student/Letter` (code only, printable).
- **Teacher notes**: DB version **13** (`TeacherNotes`), `Infrastructure/Tutor/TeacherNoteStore.cs` (only students of the teacher's classes, max 3 per student, 160 chars), form in `/Teacher/Class?id=…&student=…`, shown on `/Student`, `StudentContext.TeacherNotes` goes into the Explain and Chat prompts.
- **Correct Pip**: DB version **14** (`PipChallenges`), `Core/Tutor/Pip.cs` (5 injected mistakes, `Generate(level, seed, language)` is deterministic), `Infrastructure/Tutor/PipStore.cs`, `Web/PipEndpoints.cs` (`POST /api/pip/start`, `POST /api/pip/{id}/answer`), page `/Student/Pip`. Pip's reply: `PipVoice` (AI tool `pip_reply` from the facts, offline fallback in code), `POST /api/pip/{id}/voice`; the per-mistake stats are rebuilt from the seeds (no extra column). Next: a real chat with Pip (the mistakes stay chosen by code), DB version now **15** (`PipChallenge.Subject`); 9 maths kinds + 3 language kinds (`PipSubject.Language`: agreement, past, capital; lexicon in `Pip.Lexicon`, 5 verbs per language: add verbs there); next: science kinds, more language kinds (articles, gender), Pip stats in `/Student/Learner` and for the teacher.
- **Detective, fading, rhythm, own mistakes (DB version 16)**: `QuizAnswer.Seconds`, `PipChallenge.Forced`, table `FadingRuns`.
  - `Core/Tutor/Prerequisites.cs` (map topic → step below, mathematics only; `Verdict` needs 4 answers: < 60 % root, ≥ 75 % solid) + `Infrastructure/Tutor/DetectiveStore.cs` (`CasesAsync`, `FalseCertaintiesAsync`, last 300 answers); cards on `/Student/Learner`, block "What the Tutor has found" in `/Teacher/Class?student=…`. A test checks every link of the map is a real topic of its level. To extend: add links for other areas in `Prerequisites.Map`.
  - `Core/Tutor/Fading.cs` (6 kinds, 4 stages, `Check`) + `FadingStore` + `Web/FadingEndpoints.cs` (`POST /api/fading/start`, `POST /api/fading/{id}/answer`), page `/Student/Fading` ("Step by step").
  - `Core/Tutor/Pace.cs` (`Read` → Rushing / Stuck / Tired). The seconds come from the page (`quiz.js`, clamped to 600 on the server) and are used only for advice; the answer API returns `pace`.
  - `PipStore.OwnMistakeAsync` + `Pip.ForTopic` (topic of the tests → kind of mistake; the mapping is approximate: multiply/integers/equations → order of operations).
  - Confidence map: `Core/Tutor/Calibration.cs` (`Map` → 4 quadrants, `Score` needs 8 bets, `Trend` compares the last 15 with the 15 before), `DetectiveStore.BetsAsync`, card `data-testid=confidence-map` on `/Student/Learner`; `quiz.js` shows the bets of the test in the summary (`quiz-bets`, counted in the page).
  - Confidence already existed in the tests (`QuizAnswer.Confidence`, calibration in Achievements); new: per-topic false certainties and the two lines of the family letter (`WeekFacts.SureWrongTopic`, `UnsureRightTopic`).
- **Teacher documents, open answers, assignments, progress (DB version 17)**: tables `Materials`, `WritingSubmissions`, `Assignments`; `QuizAttempt.MaterialId`. The demo seeder now adds a few tests older than 30 days (Sara, Alex, Leo) so the report has a "before".
  - `Core/Tutor/Materials.cs`: `Clean`, `Chunks`, `Excerpts` (word overlap, budget 1800 chars), `ForPrompt` (`<material>` block, data not instructions), `Quote` (offline), `MaterialQuiz.Offline` (cloze from the document's key terms) and `MaterialQuiz.Build` (AI, questions only from the document, reuses the `topic_quiz` schema). `Infrastructure/Tutor/MaterialStore.cs` (teacher owns the class; students of the class only). Used in `LearningEndpoints` (explain + chat → `StudentContext.Material`, only when the subject is the class's subject), `TutorEndpoints` interview, `POST /api/materials/{id}/quiz` (`MaterialEndpoints.cs`), `/Student/Quiz?material=ID`, page `/Student/Materials`. Targets: `MaterialTarget { Course, Figure, Test }`; a document of any target can be taken as a test.
  - `Core/Tutor/Writing.cs`: tasks (fixed list, 4 languages), `Measure`, `Check` (code criteria), `Merge` (drops quotes that are not in the text, clamps scores), `WritingPrompt` (tool `writing_review`). `WritingStore` keeps measures only. `POST /api/writing/check`, page `/Student/Writing` (`writing.js`, dictation through `voice.js`).
  - `Infrastructure/Tutor/AssignmentStore.cs`: `CreateAsync`, status derived from activity (`Focus` attempt on the topic, writing submission, attempt with `MaterialId`), `ProgressAsync` (30 days vs 30 before, min 5 answers), `TopicsAsync`. Teacher pages `/Teacher/Course?id=` (documents, assignments, progress; Razor post handlers) and `/Teacher/Report?id=&student=` (printable); student card `data-testid=assignments` on `/Student`.
  - Limits of this basic version: a document is plain text (no images/tables; PDFs are read in the browser, 30 pages); retrieval is word overlap (no embeddings), so a question worded very differently from the document finds nothing; the offline cloze test is simple (distractors are other key terms of the document); the oral task is dictation measured as text (no pronunciation); the AI paths (document test, rubric, interview with material) are covered by prompt/parse tests but were not run against a real engine here.
- **Document labels / subject tagging (DB version 18)**: when a teacher adds a document, it is tagged with a subject, a few topics and a one-sentence summary, so the Tutor can tell when it is useful even outside the class's own subject.
  - `Core/Tutor/MaterialTags.cs`: `MaterialLabels` record (`Area?`, `Tags`, `Summary`). `Offline(title, text)` guesses the subject from a small 4-language lexicon (needs `MinHits` = 3 words and a clear lead over the next subject) and takes the tags from the document's own key terms — no AI needed. `Merge(offline, area, tags, summary)` validates what the AI proposed: a subject string that isn't one of the platform's `Area` values is dropped (the code's guess is kept), empty tags/summary fall back to the code's. `Clean`/`Parse`/`Join` normalize tag lists (lower-case, max 6 tags of 30 chars). `CleanUrl` accepts an optional https-only link (for a document that is really a video or a page). `Score` boosts retrieval when a document's label matches the subject being studied. `MaterialTagPrompt` builds the AI tool-call (schema: `area`, `tags`, `summary`) and parses the answer.
  - `Material` gained `Area?`, `Tags`, `Summary`, `Url`, `LabelledBy` ("demo" = guessed by the code, an engine name = proposed by that AI, "teacher" = corrected by the teacher). `MaterialStore.CheckAsync` validates a document (including the link) before any AI is called, so nothing is spent on a document that will be refused anyway. `MaterialStore.AddAsync` has an overload taking the labels, the link and who labelled it. `MaterialStore.RelabelAsync` lets the teacher correct the subject and topics. `MaterialStore.ExcerptsAsync` now also offers a *Course* document to a student studying a *different* platform subject when the document's own label says it is about that subject (its first chunk counts even with no word overlap with the question).
  - `Materials.ExcerptsWithLabels` is the labelled version of `Materials.Excerpts` (a document's label match score is added to its chunks' word-overlap score, and makes the first chunk eligible on its own).
  - Wired in `/Teacher/Course`: an optional link field next to the document, the AI is asked for labels when one is configured (falls back to the code's guess otherwise), the resulting subject/topics/summary are shown under each document with who labelled it, and a small "Correct" form lets the teacher fix them.
  - Limits: for a video or a page the document is just the link plus whatever text the teacher pastes (a transcript, notes) — there is no actual video/image analysis in this basic version; the offline subject guess is a word lexicon, so a document that doesn't use any of those words is left unlabelled (not wrong — just "subject not detected").
- **The detective points at the teacher's own document**: the two features above did not talk to each other — the root-cause detective (`Prerequisites`/`DetectiveStore`, DB version 17) found *that* a topic was weak, but the Tutor never checked whether the teacher had already given the class something about it. Now it does, no new table needed.
  - `Core/Tutor/MaterialTags.TopicMatch(area, tags, title, caseArea, topicWords)`: 0 when the document is not labelled for the weak topic's subject (so it is never suggested for the wrong reason), 1 plus one point per word of the topic's name found in the document's tags or title otherwise — a pure, code-only score.
  - `Infrastructure/Tutor/MaterialStore.ForTopicAsync(studentId, area, topicName)`: the best *Course* document of the student's own classes for that subject, by `TopicMatch`, or null when the teacher has not labelled one.
  - Wired in `/Student/Learner`: each case of "the detective" now also shows a "📚 Your teacher's own document: …" link when one matches, before the usual "Practise"/"Check" button — so the first remedy is the class's own material, not a generic explanation. The link opens `/Student/Materials#material-ID`, which auto-expands and scrolls to that document.
  - Limits: matching is by subject plus shared words with the topic's localized name (no AI call, so it works offline too); a teacher who never labels a document, or labels it for the wrong subject, gets no suggestion — same honesty as the rest of the labelling feature.
- Next ideas: student's own notes as material; documents for an adventure or a duel; the teacher sees the open answer (needs consent to store text); "pride moments", detective maps for other areas; fading kinds for language.
