# Maintenance foundation — orchestration plan

Date: 2026-11-24 (Asia/Shanghai). Branch: `dev`. Synced initial HEAD: `41dc562`.

## User objective and scope

Understand this repository before changing behavior, establish an evidence-based maintenance plan for logic, data structures, errors, UI/UX, and engineering quality, then execute the highest-priority safe work. Reuse the existing plans rather than reopen device-accepted features or invent new feature work. A one-time 20-minute wake-up was scheduled for 2026-11-24 22:51:25 Asia/Shanghai.

The main session is an orchestrator under `AGENTS.md` and `cloud/CONTRACT.md`: it reads code, owns this round and memory, and delegates all product, test, build, and main documentation changes. All substantive agent inputs and outputs are files in this round. Dispatch messages identify these files only.

## Baseline and constraints

- Startup sync completed: `git fetch origin --prune`, `git checkout dev`, `git pull --ff-only`; working tree was clean.
- Application: C#/XAML UWP for Windows 10/11, seven solution projects; REST/gRPC parsing, VM state ownership, player sessions, credential transactions, local media, diagnostics.
- Source of current work: `docs/ROADMAP.md`, `plan/PLAN.md`, feature plans, `bug_report.md`, `code_review.md`, and `document/BILIBILI_API_RESEARCH.md`.
- Current computer: Linux. No `dotnet`, `powershell`, or `pwsh` executable was found on PATH at intake. Full UWP compilation and runtime/device validation require Windows/VS/Windows SDK; do not report these passed here.
- Honor sparse-checkout restrictions in `cloud/REPO.md`: do not change sparse configuration or fetch excluded large blobs. The current checkout has the four sampled large source files and `remote.origin.promisor=false`; historical partial-clone limitations require fresh verification, not blanket assumptions (see `intake.md`). Classify missing source/tooling separately from product failures.
- Preserve account-parser hold, measurement-gated player/pacing changes, parked features, signing identity, binary assets, and live update JSON unless current user authorization clearly requires them.

## Three workstreams and file ownership

| Stream | Requirements owner/model | Requirements artifact | Scope |
| --- | --- | --- | --- |
| Logic, data, errors | `logic_requirements`, `gpt-6.1-sol` / `xhigh` | `logic-data-errors/requirements.md` | Confirm backlog against sources; prioritize U1, U2 and contracts; lifecycle, paging, cancellation, persistence, credentials and error recovery; identify one safe executable slice. |
| UI/UX and user journeys | `ux_requirements`, `gpt-6.1-sol` / `xhigh` | `ui-ux/requirements.md` | Core user needs, browsing/playback/comments/live/login/download/local/settings/diagnostics; loading/empty/error/retry, accessibility, keyboard, text scaling, themes; evidence and device boundaries. |
| Verification and engineering quality | `quality_requirements`, `gpt-6.1-sol` / `xhigh` | `verification/requirements.md` | Reproducible baseline, probe coverage, tool availability, CI/build/release gates, dependency and documentation ownership, maintenance cadence and metrics. Establish runnable probe evidence where feasible. |

Only those requirement artifacts (and explicitly named companion evidence under their own directory) may be written by requirements owners. Test/dependency scratch work belongs under `/workspace/work/maintenance-foundation/`. Raw test/build/download logs stay in that scratch tree, not in tracked round files; write concise reproducible command/result evidence into Markdown artifacts. No product, test, build or main documentation changes in requirements/review stages.

## Stages and gates

1. **Requirements (complete):** three specialists read `cloud/memory/INDEX.md`, the inputs above and real sources; each wrote actionable requirements, risks, acceptance and an executable next slice. Findings have source/command evidence and distinguish confirmed defects, hypotheses and external blockers.
2. **Requirements review (active):** independent expert reviewers (`gpt-6.1-sol` / `xhigh`) write stream-local `review.md`, using `review-dispatch.md`. PASS is required before execution. CHANGES goes back to its requirements owner and is reviewed again.
3. **Plan synthesis:** orchestrator writes `maintenance-plan.md` covering cross-stream priorities, owners, invariants, acceptance, verification, cadence, dependencies and stop conditions. This is the canonical deliverable for the requested maintenance plan, linked from memory.
4. **Execution:** assign a single executor (`gpt-6.1-sol` / `high`) explicit source/test/docs ownership for the first approved safe slice. Executors start with `git pull --ff-only`, record commands/results in `execution.md`, commit only owned files, and push to `dev`. A separate quality executor may run independent baseline checks if ownership is disjoint. No uncontrolled shared-index commits.
5. **Implementation review:** independent reviewers write `final-review.md`; actionable P0/P1 issues return to executor. Environment-blocked checks are FAILED/BLOCKED with evidence, never passed or silently omitted.
6. **Close and continue:** orchestrator verifies local/remote SHA, evidence and worktree; writes append-only memory and the next round if safe prioritized work remains. No release, deployment, master updates or device-pass claims.

## Acceptance of the maintenance plan

- Explains repository architecture and existing plan status with evidence.
- Gives each work item a priority, rationale, scope, owner role, dependencies, acceptance tests and status (ready, measurement/maintainer/environment blocked, parked, completed).
- Covers logic, data modeling, error handling/recovery, UI/UX/accessibility, performance/resources, diagnostics/privacy, testing/build/release, dependencies and documentation.
- Keeps protocol, cursor, session, credential and settings invariants explicit; proposes incremental changes with regression evidence rather than broad rewrites.
- States current verification results honestly and includes the exact Windows/device follow-up scenarios.
- Includes a repeatable triage cadence and concrete next actions.

## Activity

- 2026-11-24 22:34 Asia/Shanghai: repository instructions, roadmap, architecture, feature index and plan intake read; requirements dispatch prepared.
- User steering: stay synchronized with `origin/dev`. The orchestrator rechecks fetch/pull and exact local/remote SHA during intake and after executor commits; preserve all owned in-flight work.
- Requirements stage complete. Baseline: 41/43 directory batches pass; g3 compile input and playersess source assertion fail; legacy single-file stages expose additional harness compile failures. Actual-source malformed-code experiment reproduced 16 exceptions across four reader paths. Requirements reviewers assigned next; see stream evidence for exact classifications.
