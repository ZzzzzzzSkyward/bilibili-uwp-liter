# QF1 harness restoration execution

Status: **COMMITTED / PUSHED — OFFLINE PASS**; assigned repair complete under commit-window.md. Product source and all final test inputs verified; Windows UWP/device checks remain FAILED/BLOCKED on Linux. Implementation review remains pending.

## Results and evidence

- **50/50 logical entries PASS; 51/51 executable stages PASS**: 43/43 directory batches plus LM01–LM05, API01 and both AUTH01B executables. Source HEAD `10dd84857c748f6638e187d891d6a6649f887bb6` after U1/PB1 release, with final QF1 harness changes in an isolated immutable copied snapshot. Serial execution prevents gRPC output races; SHA-256 verification of 918 inputs has zero mismatches.
- g3 unchanged real assertions PASS (386 plus nested 105 RichTextFlow); actual ReserveCardCopy included exactly once. LM05 original fixtures/assertions retained and PASS (22), with import/substitute namespace/member contract aligned to actual models.
- API01 PASS (76), using real global compatibility wrapper at both original ApiHelper.Cookie assignments per independently approved correction. Three named refresh fixtures now prove valid same-owner replacement session, previous-token/replacement-CSRF confirmation, response/owner persistence, pending-until-confirmed behavior, endpoint-specific 503 retry without re-rotation, and missing-code rejection at the actual refresh phase.
- AUTH01B Account-cookie PASS (70), unchanged LoginVM scenarios PASS (39). Real refresh policy/transaction/bridge/support inputs and exact Account mirror initializer/extraction retained; full snapshot provenance/estimated expiry/browser refresh ownership rollback assertions added. No LoginVM fixture amendment was needed.
- playersess PASS (217): preserved original checks plus 14 deterministic actual-source async-gate checks. Scratch serialization bypass fails 3/217; independent dropped-tail mutation fails 1/217; unchanged extracted methods pass all checks.
- `auth1`, `api02`, `auth3`, `auth4`, `webrefresh` pass independently and in full sweep. `API01Stubs.cs` unchanged. `git diff --check` exit 0.

Exact commands/results, genuine red failures, initial scratch permission-copy error and unchanged corrected-chain retry, all directory result rows, environment, counts/warnings/limits: [evidence/qf1-results.md](evidence/qf1-results.md). Each reviewed affected intent and preserved negative control: [assertion-mapping.md](assertion-mapping.md). Actual source signatures/ranges/hashes, Account/LoginVM extraction and instrumentation details: [source-extraction.md](source-extraction.md). Corrected wrapper finding: [ownership-conflict.md](ownership-conflict.md), reviewed by amendment-review.md and incorporated into controlling dispatch before API01 fixture work.

## Owned changed files

- `plan/tests/g3/G3Probe.csproj`: link actual existing formatter once.
- `plan/tests/LM05Stubs.cs`, `LM05.cs`: actual subtitle namespace/full primitive model shape and import-only consumer correction; original behavior checks unchanged.
- `plan/tests/run-api01.sh`: actual refresh policy/transaction compile inputs. `API01.cs`: only the three expressly reviewed refresh fixture bodies changed; outside those bodies exact source comparison passes. Both real global wrapper constructions retained.
- `plan/tests/run-auth01b.sh`: exact actual static mirror initializer, actual ICookieStore declaration, current real bridge/support compile inputs for both executables. Existing async-void-to-Task signature-only instrumentation retained.
- `plan/tests/AUTH01BStubs.cs`: actual estimated-expiry/provenance setting keys, optional exception log argument, ApplicationData LocalFolder primitive, request-boundary ApiHelper configuration and replacement of duplicate MyProfileModel by actual product UserModels.
- `plan/tests/AUTH01BLoginVMStubs.cs`: external SaveLogin signature accepts/records AppkeyIdentity; duplicate model declarations replaced by actual UserModels; shared request config moved to common platform substitutes. Counters/failure injection retained.
- `plan/tests/AUTH01B.cs`: only SeedAccountState, CheckOriginalAccountState and TestProductionAccountCommitAdapter add current snapshot/output field coverage; every old assertion remains. `AUTH01BLoginVM.cs` byte unchanged.
- `plan/tests/playersess/Program.cs`, `PlayersessProbe.csproj`, `run.sh`, new `HistoryCheckpointProbe.cs`: compile actual four history methods and all five current field declarations with primitive wrapper/report gates, retain existing portable SDK/--player options, replace sole obsolete literal check with wiring plus stronger actual behavior assertions; generated sources stay temp.
- This execution record and owned evidence files listed above. No quality product/comments/c1/season1/manifest/build/main-doc change, aggregate runner, CI, new framework or duplicate business implementation.

## Commit and release of ownership

Implementation/evidence commit: `ca991490f0826268d22984a2275b3e28190acad8` (`test: restore current-source probe harness coverage`). `git push origin dev` exit 0; `git rev-parse HEAD` and `git ls-remote origin refs/heads/dev` both returned that exact SHA immediately after push. This final execution-status update is a subsequent evidence-only commit; the orchestrator should verify its final HEAD/remote SHA independently. No rebase/amend/force operation.

Exactly the 13 assigned harness files plus five owned verification evidence files were staged. Other owners' requirements/reviews/round/memory and next-wave files remain untouched/uncommitted by this executor. Window read: commit-window.md granted exclusive quality commit/push after source metadata `10dd848`.

All verification work complete. **Release all QF1 harness ownership, including shared API01Stubs.cs, after this evidence-only commit/push.** API01Stubs is unchanged; planned search work may take over through its own approved dispatch. No quality write/process remains active after return; implementation-review fixes require a new explicit follow-up assignment. No unresolved offline product behavior failure was found in QF1. Windows compile/runtime/MSIX/device/live-session gates remain FAILED/BLOCKED with exact baseline prerequisite evidence; no device acceptance or release performed.

Observed push-banner dependency finding: GitHub reports one moderate Dependabot alert on the default branch, https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/security/dependabot/5 . The banner identifies no package/version; this is an observed external alert for the separately scoped dependency triage, not a finding proven against the complete shipped graph here. Prior limited gRPC advisory evidence is not a repository-wide clean claim. No dependency change attempted.
