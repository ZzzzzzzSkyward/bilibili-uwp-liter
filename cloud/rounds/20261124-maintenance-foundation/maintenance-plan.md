# Repository maintenance plan

Prepared 2026-11-24 (Asia/Shanghai). **Draft under independent requirements review; implementation status is updated separately below.** Development branch: `dev`. Intake source: `41dc562fba6e45896b0c4ba604f9ec38f4661e73`, verified equal to `origin/dev` and GitHub's ref.

## Objective and existing-plan relationship

Maintain reliable browsing, playback, comments, live viewing, account recovery, downloads/local media and settings/diagnostics. Prioritize failure to complete an existing operation, incorrect data/state and unrecoverable errors before cosmetic cleanup or endpoint parity. Changes are small, independently reviewed and supported by behavior evidence.

This is an operating layer over the existing plans, not a replacement backlog. Use `docs/ROADMAP.md` for status, `plan/PLAN.md` for slices/dispositions, `plan/VALIDATION.md` for scoped device scenarios, feature plans for their established contracts, and `bug_report.md` for actual reported defects. Existing card/settings/refresh/local-library work remains accepted under the maintainer's policy. Old proposed layouts, checkout notes and historical inventory gaps are checked against current sources before reuse.

Detailed requirements and evidence live beside this plan:

- [Logic, data and errors](logic-data-errors/requirements.md), [source/reproducer evidence](logic-data-errors/source-evidence.md).
- [UI/UX and core journeys](ui-ux/requirements.md), [source evidence](ui-ux/evidence.md).
- [Verification and engineering quality](verification/requirements.md), [baseline results](verification/evidence/baseline.md).
- Next-wave [API/action recovery scope](../20261124-api-request-maintenance/plan.md).
- [Live orchestration checkpoint](checkpoint.md) for continuation and active ownership.

## Architecture and invariants

The shipped application is C#/XAML UWP, with a gRPC library, JS login bridge, full-trust conversion companion and MSIX packaging project. WebApi and ApiTester are unshipped legacy projects. Preserve the current transport/rendering stack and pure-service test seams; a maintenance change does not require a new framework or broad player rewrite.

| Area and owner | Invariant to retain | Evidence required when touched |
| --- | --- | --- |
| Request builders (`Api/`) | Exact endpoint, method, fields, encoding and signing input; IDs/cursors retain full precision; raw credentials are values, never concatenated form fragments. | Parse real constructed requests, retaining duplicate-key multiplicity; reserved/Unicode values and signed input boundaries. |
| Transport (`HttpHelper`, gRPC) | Transport status remains separate from business/payload status; current origin/cookie policy stays explicit. | Failure/timeout fixtures and affected source regressions; actual cookie/session behavior requires Windows/session evidence. |
| DTO/parser/mapper (`Models/Api`, `Parsing/`) | Select data/result explicitly; missing/malformed/out-of-range code fails as a typed value; valid empty data remains distinct from malformed data; parsers never mutate UI or toast. | Required/optional/null/wrong-type/large-integer/unknown-item fixtures and preserved error categories/messages. |
| VMs, paging and queues (`Modules`, paging services) | Only current request generation publishes rows/cursor; cursor stays bound to source; failed append retains accepted rows and retry identity. | Delayed/stale completion, source switch, duplicate/empty pages, failed append/retry and refresh replacement. |
| Media/lifecycle (`PlayerSessionState`, guards, window helper) | One media owner per open; obsolete resources released once; inactive callbacks rejected; deferrals always complete; one serialized history drain. | Pending open/replace/close and blocked-report scenarios; SMTC, native resource and suspension behavior measured on Windows. |
| Credentials/settings/persistence | One credential commit path and token provenance; preserve pending refresh recovery, cookie origins and setting-key ownership; failed write retains last good state. | Existing auth/refresh rollback/single-flight tests; reproduce concurrency before storage redesign; do not migrate held account parsers. |
| Views/controls | One current visual owner on recycle; content controls appear only with content; user text remains Chinese and concise; theme, full-title access and keyboard routes stay usable. | Real-source rules plus scoped Windows scale/theme/focus/UIA checks; source-string checks do not prove rendered behavior. |
| Diagnostics | Reuse endpoint/request/session/stage evidence and existing sanitization; capture stays opt-in; export stays user controlled. | Synthetic-secret/redaction and bundle-manifest tests; no committed raw credentials, signed bodies or personal captures. |

## Prioritized work queue

Statuses distinguish **confirmed/reproduced defects**, **source gaps**, **hypotheses**, **blocked prerequisites**, and **accepted/parked scope**. A passed offline probe never establishes a live-service or device pass. Each execution needs a named owner and requirements PASS; this table alone does not grant shared-file ownership.

| Priority / ID | Work and reason | Owner, dependency and scope | Completion evidence / current status |
| --- | --- | --- | --- |
| P0 / U1 | Correct comment like/add/reply CSRF forms; token currently contaminates type/message. | API executor; only CommentApi and owning comments probes. Preserve raw AddComment versus already-escaped ReplyComment convention; do not change GetCSRF globally. | Exactly one encoded csrf value in all four action forms; IDs/type/action/message/root/parent exact; red-before/green-after real builder tests. **Requirements review active.** Windows/session follow-up blocked here. |
| P1 / PB1 | Prevent malformed integer business codes escaping typed failures. Sixteen actual-source exceptions reproduced across data/result/video/action readers. | Parsing executor after U1 releases shared comments tests; one bounded integer-code reader in existing ApiEnvelope, reused by the two specialized readers. | Int32/Int64 overflow and BigInteger yield Envelope/envelope.code without throw; boundaries/rejection messages and each payload rule retained; comments/c1/season1 regressions. **Requirements review active.** |
| P1 / QF1 | Restore failing/early-masked test harnesses so maintenance has a trustworthy gate. | Quality executor; disjoint g3, LM05, credential runners/stubs and playersess test ownership. No product changes to satisfy obsolete source-text checks. | Existing assertions execute; real formatter and credential services linked; platform stubs match real types; current serialized pending-tail behavior plus negative control replaces obsolete coupling without weakening intent. **Requirements review active.** |
| P1 / U9 | Separate ts and privacy in favorite-folder add/edit forms. Confirmed source defect. | Favorite API executor, next-wave requirements; existing request probe selected before execution. | Both privacy states, timestamp, title/intro, exact media ID and corrected signing input verified against real builder; no live folder writes. **Requirements in progress.** |
| P1 / U5; P2 / U8 | Preserve real direct-search BUVID and include selected app-search ordering. Confirmed source defects. | One SearchAPI owner, next-wave requirements; direct/configured-host/area/proxy and WBI boundaries explicit. | Seven builders preserve saved identity on direct calls; intentional proxy behavior separately tested; sort/filter/page/text and signing remain correct. **Requirements in progress.** |
| P1 / E1, UX-02 | Make comment submission await the dialog lifetime, retain draft and allow retry after failure; remove default-code false success. Source gap. | Dialog executor after U1; next-wave requirements define typed action response, single flight, cancellation and real-source async seam. | Blank input sends nothing; delayed send stays owned; repeated activation sends once; malformed/rejected/failed results retain input and permit retry; only accepted success closes. **Requirements in progress; rendered lifecycle verification requires Windows.** |
| P1 / U2 | Modernize the active live-chat discovery/auth flow as one contract. Existing research points partly at an unused legacy class; actual caller is LiveMessage. | Live/protocol owner; source-backed token/hosts/auth schema, genuine existing BUVID or verified app fallback, fixtures and a test session. Preserve BR-63 reconnect. | Missing hosts/token, rejected auth, cancellation, reconnect/leave and decoder protocol coverage, then actual session connection. **BLOCKED: identity/session/protocol integration evidence.** Do not patch only the unused getConf path. |
| P1 / background + BR-21 | Establish SMTC ownership and memory cause before player work. | Player/lifecycle owner after device measurements; reuse CARD_PLAN window/anchor behavior. | Feed-only, playback-only, playback+danmaku at matched depth/time; managed/native split and after-close baseline; then owner-specific fix and bounded suspension verification. **BLOCKED: Windows device/measurement.** |
| P1 / existing device scenarios | Preserve history pause/close tail, minimize restore and live reconnect expectations. | Maintainer/device runner; no speculative source rewrite. | Targeted Windows observations with version/time/journey; actual failure opens a new BR. Historical default-accepted rows remain accepted. **Current fresh evidence BLOCKED by platform.** |
| P2 / UX-03–06 | Improve persistent initial error/empty/retry states, truthful partial-download deletion/error feedback, and investigate narrow/scale/accessibility risks. | One flow at a time: recommend, download, then evidenced layout/name issues. Preserve implemented bulk actions, import/export, full-title access and theme fixes. | Success-empty versus initial-failure versus append-failure distinct; existing rows/draft retained; accurate deletion prompt; one useful failure summary; actual minimum usable Windows window plus 800×600/1280×720 effective-pixel samples, 100/150/200% text, themes, keyboard and Narrator evidence. **Scoped follow-ups; layout/accessibility hypotheses require Windows evidence before defect claims.** |
| P2 / QF2 | One reproducible full-suite entry and then CI using that command. | Quality/tooling owner after QF1; Bash, explicit SDK/ref packs, complete 43-batch + seven-probe inventory; shared stubs single-owned. | Includes g1/stab and both AUTH01B stages; 50 logical entries / 51 executable stages; failure cannot hide later stages; nonzero aggregate for any failed/missing required check. **PLAN ONLY: separate complete runner/workflow requirements and review required before implementation.** |
| P1 before next release / QF3 | Make release-layout guard enforce the documented small root launcher, and complete Windows compile/package/install/launch gates before an authorized release. Confirmed static gate gap. | Build owner; root BiliLiter.exe exists/<64 KB plus current entrypoint/companion/manifest checks. The bounded guard repair can be a later tooling slice; the release gate cannot be deferred through release. | Good/bad package fixtures and actual Windows compilation/full package/install/launch evidence. **Windows evidence BLOCKED; no release authorized.** |
| P2 / QF4 | Review dependency/SDK/transitive/native compatibility and support. | Single dependency owner; separate shipped graph from legacy tools; preserve existing package/restore model unless reviewed separately. | Scoped advisory inventory with time/source, pins/transitives and relevant Windows/native playback/export checks. gRPC-only clean advisory query does not certify full app. **Full compatibility gate BLOCKED by Windows; no bulk upgrades.** |
| P2 / persistence | Investigate aliased history snapshot and local-registry interleavings. | Storage owner only after delayed-writer/concurrent-caller reproducer. | Demonstrated stale/lost state reproduced first; if confirmed, ordered immutable writes and failed-write recovery tested. **HYPOTHESIS; reproduce before changing storage.** |
| P2 / pacing | Measure inert global pacing before enabling or removing it. | Transport owner with latency/concurrency/rate-limit evidence. | Matched burst/latency measurements and justified policy; no casual lasttime assignment. **MEASUREMENT BLOCKED.** |
| P2 / account P4/P5 | Preserve maintainer hold on account-parser migration and retire wrappers only after callers leave. | Account owner after explicit hold release. | Credential provenance/rollback/refresh matrix and one endpoint contract at a time. **MAINTAINER HELD.** |
| Parked / evidence-dependent | Old legacy card parser, multiwindow font risk, tiles, extra auth variants, alternate renderer, local-library expansion and unproven gift/review route migration. | Existing plan/reproducer or a separate requested feature. | Evidence and acceptance before work; endpoint differences alone do not prove service failure. **PARKED / external-evidence blocked.** |

The full [UI/UX requirements matrix](ui-ux/requirements.md), §6, controls later touched-interface checks: actual supported minimum window and the stated effective-pixel samples, essential actions and complete-title access without overlap, 100/150/200% text, light/dark/follow-system/high contrast, keyboard/focus restoration and Narrator/UIA names/states. No mobile-width support target is added by this plan.

## Verification baseline and gates

The current checkout is complete for sampled formerly excluded sources; no missing-source blocker is assumed. Host is Linux. SDK 8.0.427/runtime and reference packs 8.0.33 were installed under scratch; no home/system install or sparse configuration change. Baseline snapshot text matched 697 source hashes and used existing tracked assets.

| Gate | Current evidence | Required maintenance behavior |
| --- | --- | --- |
| Directory probes | **41/43 pass.** g3 cannot compile due omitted ReserveCardCopy dependency; playersess has one obsolete literal-source assertion failure among 203 checks. | Repair harness and preserve/strengthen behavioral intent; do not change accepted product behavior to satisfy an old literal. |
| Seven single-file probes | LM01–LM04 pass; LM05 compile fails; independently run API01/AUTH01B also compile fail, with later AUTH LoginVM stage masked. | Every logical probe/stage must actually run; a passing prefix is not a green chain. |
| Script invocation | Bash succeeds where explicit sh/dash fails. | Use `bash plan/tests/<batch>/run.sh`, `bash plan/tests/run-g1.sh`, and `bash plan/tests/run.sh` with the declared portable SDK root; fix user guidance under docs ownership. |
| Product changes | New regression assertions first reproduce the source defect; unchanged assertions then pass after repair. | Run owning behavior batches and relevant shared dependents, `git diff --check`, and independent diff review; broaden only for new failures/changes. |
| UWP compilation | **FAILED/BLOCKED: powershell missing; Linux lacks VS/UWP/Windows SDK.** | On Windows: `powershell -NoProfile -ExecutionPolicy Bypass -File ./build-release-x64.ps1 -Fast -NoVersionBump -Restore`; zero errors. No console-probe substitution for XAML/UWP compile. |
| Release/install/runtime | **FAILED/BLOCKED: Windows runtime/toolchain unavailable.** | Only an authorized release runs full build/version bump, checks correct MSIX/entrypoint/root launcher, installs and launches the app, then executes touched device journeys. No publishing/master movement in this task. |

Exact environment setup, commands, batch denominators and raw-log locations are in the baseline evidence. Raw logs/dependency downloads/build products are not committed. Keep expected compiler warnings distinct from new errors and newly introduced warnings.

## Ownership and execution discipline

The main session only orchestrates and writes round/memory files. Requirements/review experts use `gpt-6.1-sol` / `xhigh`; executors use `gpt-6.1-sol` / `high`. Requirements → independent review PASS → explicit file ownership → execution/evidence → independent implementation review → archive/continue. Every P0/P1 finding is fixed or carries a concrete FAILED/BLOCKED prerequisite and next action.

- One writer per source/test/shared-doc file. U1 and PB1 share comments tests and run sequentially. Search identity/sort have one owner. Credential harness stubs have one owner. PLAN/ROADMAP/VALIDATION/DEVELOPMENT have a single later docs owner.
- Executors pull `--ff-only`, commit only assigned files with scoped evidence, and push `dev`. Commits/pushes are serialized in this shared checkout; no force, shared-history rewrite, master updates or live-data edits.
- Orchestrator verifies branch and exact local/origin/GitHub SHA after changes, current CI status if a workflow exists, evidence and final worktree. Preserve in-flight work when remote changes; never discard it to obtain a clean report.
- Failed environment checks do not block independent offline work and never become PASS. New device bugs enter the existing report workflow; source-only findings are labeled as such.

## Maintenance cadence and measures

| When | Owner and action | Evidence/measure |
| --- | --- | --- |
| Every defect/change | Feature owner: capture smallest reproducer, source/SHA and category; implement one reviewed slice; update its existing plan and tests. | Red→green behavior, affected probe pass count/denominator, errors and new warnings, reviewer disposition, honest device gate. |
| Each active round / weekly triage | Orchestrator + quality owner: sync dev, run complete known inventory, triage new BRs and aged P0/P1, assign next actionable owner. | Attempted/passed/failed/blocked and unexecuted stages; open severity/age/owner; next action for every blocker. |
| Monthly / dependency change | Dependency owner: scoped restored advisory/version/native/support review and docs drift check; avoid opportunistic bulk upgrades. | Advisory scope/time/source, direct/transitive inventory, compatibility results and outstanding platform checks. |
| Changed UX flow / before release | Maintainer/device runner: targeted core-journey matrix, scale/themes/keyboard/UIA, input cancellation and error/retry, playback/account/export as affected. | Version/device/scenario/result; recovery and draft/data retention, clear destructive actions; no invented runtime pass. |
| Evidence of resource regression | Performance owner: repeat matched feed/playback/danmaku runs and after-close observations. | Depth/time, working set, managed/native contribution and ownership; choose a measured budget after baseline, not an arbitrary threshold. |

These are operating recommendations, not additional scheduled automations. The requested one-time 20-minute wake-up is scheduled separately for 22:51:25 Asia/Shanghai on 2026-11-24 and resumes from the checkpoint.

## Completion and continuation

The requested plan is reviewable when the architecture, evidence, prioritized owners, acceptance, cadence and blockers above have independent requirements PASS. A source slice is complete only with its actual diff, red/green evidence, implementation review and pushed SHA. Repository-wide runtime/release readiness is **not established** on this host.

Continue immediately with approved safe work while platform/measurement/maintainer-held items remain explicit. The checkpoint records active assignments; memory records each completed round and next action. Do not turn a blocked integration check into a claim that the whole repository is finished or defect-free.

## Execution results

Pending independent requirements review. No product or harness changes have been executed at the time of this draft.
