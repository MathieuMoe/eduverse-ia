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
