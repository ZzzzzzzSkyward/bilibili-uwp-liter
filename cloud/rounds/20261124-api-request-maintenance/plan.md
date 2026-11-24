# Next-wave API and action-recovery maintenance

Date: 2026-11-24 Asia/Shanghai. Branch: `dev` synchronized initially at `41dc562fba6e45896b0c4ba604f9ec38f4661e73`.

## Purpose and dependencies

The maintenance-foundation requirements identified additional source-confirmed defects already documented in API research, plus a comment-dialog recovery gap. Prepare precise independent requirements while the preceding round is in read-only requirements review. This is preparation only: no execution before this round's requirements PASS and release of prior source/test/documentation ownership. Keep U1/PB1 priority and preserve exact commit-specific test evidence.

Read `AGENTS.md`, `cloud/CONTRACT.md`, `cloud/REPO.md`, `cloud/memory/INDEX.md`, and the preceding round's `logic-data-errors/requirements.md`, `ui-ux/requirements.md`, and `verification/evidence/baseline.md`. Use actual sources. Keep requirements concise (roughly 100–140 lines each), complete enough to implement without guessing. All substantive communication is via this round's files.

## Requirements assignments

| Workstream | Owner/model | Output and ownership | Required scope |
| --- | --- | --- | --- |
| Favorite-folder requests | `logic_requirements`, `gpt-6.1-sol` / `xhigh` | `favorites/requirements.md` plus concise Markdown evidence under `favorites/` | U9 CreateFavorite/EditFavorite separator, exact fields/privacy/IDs/encoding/signature ordering, real-source red-before/green-after tests and minimal owning existing probe. No live folder writes. |
| Search identity and ordering | `quality_requirements`, `gpt-6.1-sol` / `xhigh` | `search/requirements.md` plus concise Markdown evidence under `search/` | U5 seven BUVID overrides and U8 nonempty app order; direct/configured API/proxy/area boundaries, WBI/sign inputs, query/identity preservation, actual-source seams. No fabricated identity or global cookie-merge change. |
| Comment send lifetime/retry | `ux_requirements`, `gpt-6.1-sol` / `xhigh` | `comment-recovery/requirements.md` plus concise Markdown evidence under that directory | UX-02/E1: proper dialog event lifetime, draft retention, typed response acceptance, single in-flight send, failure retry, blank input, cancellation/late completion; minimal real-source async probe with UI-only stubs if feasible. Determine honestly which implementation/verification is possible on Linux. Source diagnosis is not a fabricated device bug report. |

Requirement owners do not write product/tests/build/main docs or git history. Scratch reproducers only under `/workspace/work/maintenance-foundation/` in a stream-owned directory; no raw logs committed. Read current official API documentation with the relevant skill if required to resolve a framework-specific lifecycle contract; existing source and supplied documentation may otherwise suffice.

## Requirements acceptance

Each artifact must state current behavior/evidence class, goals/non-goals, exact request/state/data/error contract, boundaries and risks, minimal expected file ownership, meaningful tests (real source, no duplicate implementation), pre/post evidence plan, Windows/session limitations, priority and terminal/blocker status. Inspect existing probes before proposing new batches and preserve original assertions. Avoid changing shared ApiHelper or credential semantics to fix local callers.

## Requirements review (complete); execution active

All three independent requirements reviews PASS, after corrected compatibility-wrapper ownership and dialog Closing/raw-decoder revisions. Execution ownership and serialized commit windows are in `execution-dispatch.md`: source_executor owns U9 then E1 sequentially; search_executor owns disjoint U5/U8 files with a later commit window. No shared main docs owner is active yet.

Revision assignment: `ux_requirements` resolves `comment-recovery/review.md` CR-R1/CR-R2 in its own requirements/evidence, then independent re-review. Orchestrator decision for this maintenance slice: preserve the existing typed action parser/HttpResults raw-body compatibility policy rather than expand into global strict-decoder work. State exact salvage behavior and limits, add faithful actual-source raw-body fixtures, and do not claim HTML-with-embedded-JSON rejection if the real decoder accepts it. Add earliest accepted Closing invalidation plus separate Closing/Closed race evidence per review. No user/device report is invented; no product edit before PASS.

## Later stages (not authorized yet)

Independent expert requirements reviewers (`gpt-6.1-sol` / `xhigh`) write per-stream `review.md`. Only PASS permits explicitly owned executors (`gpt-6.1-sol` / `high`), with commits/push serialized in the shared checkout. Main docs have one separate owner. Independent implementation review follows. A Windows build/session/device block must remain explicit; no release/master/deployment action.

Main orchestrator owns this plan, checkpoint and later memory. The preceding round's live checkpoint remains the primary continuation point until it closes.
