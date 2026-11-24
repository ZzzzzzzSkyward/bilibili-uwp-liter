# Execution dispatch — ACTIVE (all three requirements reviews PASS)

Read applicable repository instructions, `plan.md`, all three stream requirements/reviews and this file. Read `checkpoint.md` before writing. Explicit ownership below overrides earlier ownership proposals only within their approved semantics; it does not approve unrelated work. No executor may weaken assertions, fabricate a pass or edit another owner's files.

## Source executor — sequential U1 then PB1

Model: `gpt-6.1-sol` / `high`.

Start by `git pull --ff-only`, inspect current branch/status and preserve round/memory files and other owner's in-flight work. Root/main docs remain unassigned to you.

**U1 ownership:** `src/BiliLite.UWP/Api/CommentApi.cs`; existing `plan/tests/comments/CommentsProbe.csproj`, `Program.cs`, `CommentProbeStubs.cs`, and at most a narrowly needed new request-only stub file inside that same batch. Follow exact reviewed encoding/empty-token/form-multiplicity/caller contract. Keep the shared ApiHelper and transports unchanged. Add meaningful real-builder assertions first, record their genuine red behavior, then source correction and unchanged green assertions. Run owning comments batch with Bash and the installed scratch SDK; no live action.

Write `logic-data-errors/u1/execution.md` with files, changes, exact commands/results and red/green output, blocked Windows/session gate, commit and remote SHA. Commit/push U1 before starting PB1, using only its owned product/tests/evidence files.

**PB1 additional ownership begins after U1 commit/push:** `src/BiliLite.UWP/Parsing/ApiEnvelope.cs`, `Parsing/Video/VideoEnvelope.cs`, `Parsing/Comment/WebCommentActionParser.cs`; existing comments/c1/season1 probe Program files and project/stubs only if needed for their existing real-source test seams. No new product file/framework/API migration. Follow reviewed requirements §6, sharing a bounded integer-token reader in the existing ApiEnvelope file, including BigInteger, no permissive string/float conversion, preserved payload/message semantics. Permanent assertions must fail before source correction and pass unchanged afterward. Run comments/c1/season1 and diff checks.

Write separate `logic-data-errors/pb1/execution.md`, commit/push PB1, and write `logic-data-errors/execution.md` as an index of both slices and their final status. You may commit that index with PB1. Report only artifact paths/status in chat.

**Source executor has first commit/push ownership for the shared index.** Stage exact files; never add all untracked round/memory/other-owner changes. No automatic rebase of shared history. If origin changed incompatibly, record a blocker and continue unaffected verification.

## Quality executor — QF1 harness restoration

Model: `gpt-6.1-sol` / `high`. Follow the exact scope that quality `review.md` approves; any CHANGES must be resolved first.

Start with `git pull --ff-only` and preserve other owners' files. Own only:

- `plan/tests/g3/G3Probe.csproj` for the existing formatter dependency;
- `plan/tests/LM05Stubs.cs` for the real subtitle type namespace/member contract and `plan/tests/LM05.cs` for its matching using-directive correction only, preserving all fixtures/assertions;
- `plan/tests/run-api01.sh`, `API01Stubs.cs`, `run-auth01b.sh`, `AUTH01BStubs.cs`, `AUTH01BLoginVMStubs.cs` for current real-service/extracted-method dependencies;
- `plan/tests/API01.cs` for the three named refresh fixtures/setup/expectations in revised requirements §4.1–4.2, with the valid same-owner replacement jar, distinguishable previous/replacement tokens/CSRF, explicit phase assertions, and per-assertion preserved-or-stronger mapping; **preserve/revert the two original real `global::CookieService` constructions** (baseline lines 147 and 232, including `TestMigratedEndpointRequests`) and use the compatibility wrapper's supported constructor/isolated LocalFolder state at assignments to ApiHelper.Cookie. The prior namespace-change instruction is withdrawn by PASS `verification/amendment-review.md`. Real namespaced service can be used in the other named fixtures where no derived-wrapper assignment occurs; no fake global service or product edit;
- `plan/tests/AUTH01B.cs` only for `SeedAccountState`, `CheckOriginalAccountState`, `TestProductionAccountCommitAdapter` fixture/snapshot additions for current provenance/estimated expiry/browser-refresh owner state. No other scenario expectation changes are granted. `AUTH01BLoginVM.cs` remains unchanged; newly exposed incompatible expectations require a requirements amendment and independent review.
- `plan/tests/playersess/Program.cs`, `PlayersessProbe.csproj`, and narrowly necessary harness-only sources/scripts in that batch for deterministic async-gate tests of actual current history methods.
- `verification/execution.md` and concise Markdown evidence under `verification/`.

No product/manifest/build/main-doc changes. No comments/c1/season1 edits. Revised quality requirements §4.1–4.3 control exact dependency/extraction and fixture contracts: real refresh policy/transaction/bridge and required support inputs; actual static readonly refresh mirror initializer and verbatim current method bodies; confirmation uses replacement CSRF and previous refresh token. Preserve all original behavioral intents/assertions, with the specifically reviewed obsolete fixture setup/literal updates mapped one by one. For stale source coupling, require stronger actual-source serialization/pending-tail evidence plus negative control; do not simply delete/broaden a failing check. If a direct test cannot be implemented in bounded scope under the review, leave the item FAILED with exact evidence. Treat newly uncovered real product behavior failures as findings for a separately reviewed source slice, never fake a success in stubs.

Use SDK/runtime/cache environment from `verification/evidence/baseline.md`, with per-agent scratch at `/workspace/work/maintenance-foundation/quality-execution/`. Targeted independent tests may run while source executor edits disjoint files. Run whole-suite only after the source owner has finished both slices or against a clearly recorded immutable source snapshot. gRPC-dependent baseline execution must be serial to avoid generated-output races.

Quality executor may edit/test concurrently but **must not commit/push until `verification/commit-window.md` exists and grants it the shared-index window**. If work is complete before that, write execution evidence with status READY FOR COMMIT and finish; the orchestrator will dispatch a follow-up after writing the window. Then stage only owned files/evidence, commit and push dev, and update execution evidence. This is internal ownership coordination, not a request for user permission.

## Main documentation and archive

Not yet granted. A separate single owner will update PLAN/ROADMAP/VALIDATION/DEVELOPMENT and archive finalized round/memory after source/testing results are reviewed. Main orchestrator continues writing only round/memory files. No release/master/deployment operation.
