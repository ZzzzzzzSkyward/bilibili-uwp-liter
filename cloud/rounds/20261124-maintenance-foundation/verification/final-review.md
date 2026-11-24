# 实现审查：QF1 probe harness restoration

- 轮次：`20261124-maintenance-foundation`
- 审查类型：实现
- 审查员：`quality_review`
- 结论：**PASS — approved offline harness implementation; Windows/runtime gates remain FAILED/BLOCKED**
- Implementation: `ca991490f0826268d22984a2275b3e28190acad8`, compared with parent `10dd84857c748f6638e187d891d6a6649f887bb6`.
- Final execution metadata: `858b971` (only `verification/execution.md` changes after the implementation commit).

The approved harness repair is complete and preserves the reviewed behavior. No open P0/P1/P2 implementation finding exists in this diff. This is acceptance of QF1's offline verification work, not a UWP compile, installed-app/runtime/device pass or release authorization.

## 审查范围与证据

Read applicable repository/cloud instructions and memory index, implementation-review dispatch, controlling quality requirements/review and compatibility-wrapper amendment, full actual commit diff, finalized execution record, assertion mapping, source-extraction provenance and complete-sweep/negative-control evidence. Original source and other owners' U1/PB1 changes remain separately reviewed scopes.

Independent checks bound this review to the immutable implementation:

- Commit changes exactly **13 approved harness files plus five owned verification evidence files**; no product/comments/c1/season1/main-documentation/manifest/build/CI/dependency changes. `git diff --check ca99149^ ca99149` passes.
- Recomputed all **918 snapshot input hashes**, before and after reviewer tests: **zero mismatches**. Every one of the 13 changed harness files in the tested snapshot matches its `ca99149` committed content.
- Independently verified **24 documented extraction-range hashes** against current original product text, including Account mirror/commit/support block, ICookieStore, all LoginVM ranges, four history methods and five actual history fields: **zero mismatches**. The product dependencies used here are unchanged by the quality commit.
- Programmatic comparison confirms API01 outside its three named refresh methods and AUTH01B outside its three named snapshot/adapter helpers are unchanged. `AUTH01BLoginVM.cs` and shared `API01Stubs.cs` are byte unchanged. LM05's consumer differs only in its import. g3 includes the real existing formatter **exactly once**.
- Inspected original/full raw sweep results, corrected chain output, targeted credential logs and both negative-control sources/logs. Initial scratch setup failures remain attributable and are not counted as successful commands.

The reviewer wrote only this final-review artifact and ran bounded checks in the executor's existing immutable scratch snapshot. No product/test/build/docs/history was edited.

## P0（必须修复，阻断）

None.

## P1（必须修复，可本轮闭环）

None. The earlier incorrect namespace instruction is fully superseded and the final implementation retains both actual `global::CookieService` constructions. No product workaround, cast or fake global service was introduced.

## P2（记录到 ROADMAP）

None introduced or left unresolved in this diff. Shared documentation dispositions remain with the designated later owner. QF2 runner/CI, QF3 package-layout guard and separate dependency triage are outside this implementation; this PASS grants none of their changes. The push-banner Dependabot observation is not a completed shipped-graph audit or a new harness defect.

## 验收标准核对

| Area | Actual implementation and review conclusion |
| --- | --- |
| g3 real dependency | One source link to existing `Parsing/Dynamic/ReserveCardCopy.cs`; no substitute formatter or product/output change. All original g3/RichTextFlow assertions execute. |
| LM05 contract | Substitute subtitle models now use actual `BiliLite.Modules`, matching primitive members; consumer import corrected. Every original SRT/VTT/JSON, cue, format-error, track/toggle/stale-completion assertion remains. |
| API01 real types/dependencies | Runner adds real refresh-policy/transaction inputs. Both assignments to real ApiHelper.Cookie retain its actual global derived wrapper and supported constructor. The positive fixture uses isolated real LocalFolder-backed state; other named fixtures use actual base-service store/phase inputs. Shared primitives do not implement acceptance/refresh decisions. |
| API01 success | Same-owner previous jar/source/owner is seeded; replacement SESSDATA/different CSRF arrives at the refresh response. Assertions retain ordered correspond GET → refresh POST → confirm POST, old credentials for refresh, replacement CSRF plus previous token for confirmation, response/owner token persistence, real pending record while confirmation is gated, and clear only after explicit success. Async work is awaited before fixture path cleanup/restoration. |
| API01 negative behavior | Endpoint-specific confirmation 503 reaches ConfirmationPending with exact token/owner/digest binding; retry sends confirmation only, without rotation or false success. Missing business code reaches an otherwise-valid refresh response and preserves prior token/source/owner, required state and absence of confirmation/pending success. Reviewed literal/setup changes strengthen the original intents; they do not conceal an early-return fixture failure. |
| AUTH01B source fidelity | Actual static readonly mirror initializer, actual ICookieStore and full production Account commit/snapshot/apply/mirror/restore helpers are extracted. Real bridge, refresh rules/transaction, AccessKeyRenewal, UserModels and existing transport/session dependencies compile. Duplicate DTO substitutes are removed only when actual declarations replace them; no credential-success stub is added. |
| AUTH01B preservation | Original Account/session/rollback/isolation assertions remain; estimated expiry, provenance and browser refresh source/owner snapshot coverage is added in the allowed helpers. Both executables run. LoginVM scenario file remains unchanged; signature-only async-void→Task instrumentation is the existing test mechanism, with actual method bodies intact. External Account/UI counters remain declared boundaries. |
| History source/fields | Owning runner extracts the four actual current methods and five actual field declarations into trapped temporary source; counts are checked. New partial harness supplies only player/item/settings/report primitives and test accessors. The played-item flag is fixture state; serialization, pending-tail, key and paused-flush algorithms remain actual source. |
| History execution model | Inputs A/B/C are invoked sequentially while A is gated; release occurs only after pending/local-save checks, then all waiters are awaited. This provides deterministic serialized input/state transitions without parallel caller mutation or timing sleeps. It is not a simulated UWP dispatcher/rendering proof. |
| History behavior | Actual code proves one active blocked report, latest local position saved during it, all waiters awaiting the drain, intermediate positions coalesced into only the latest tail, duplicate tail suppressed, paused timer local-only/paused flush enabled/ended flush skipped, and throwing-report cleanup followed by a usable later report. Existing session/resource/timer/App-deferral checks remain; only the obsolete literal guard check is replaced with wiring plus stronger behavior. |
| Negative controls | Exact scratch serialization bypass changes only the active-guard condition and fails overlap/latest-tail/duplicate checks. Independent dropped-tail mutation preserves serialization but discards incoming pending updates, and fails the latest-tail assertion. Repository source is untouched; original methods pass the same assertions. These failures are excluded from the successful inventory denominator. |
| Ownership/structure | New HistoryCheckpointProbe is in the existing owning batch and compiled; generated source is temporary, with no orphan test file or committed production clone. Roles are clear, model/fixture state is bounded, and no duplicate business implementation or unnecessary production data-structure change was introduced. Structural quality is acceptable within QF1's touched scope; this does not certify unrelated repository debt. |

## 测试/构建证据

Reviewer reran the following against `/workspace/work/maintenance-foundation/quality-execution/snapshot`, with baseline SDK/cache environment (SDK **8.0.427**, runtime/ref pack **8.0.33**):

| Command | Independently observed result |
| --- | --- |
| `bash plan/tests/run-api01.sh` | Exit **0**, **76** assertions. |
| `bash plan/tests/run-auth01b.sh` | Exit **0**; Account/service/transport **70**, LoginVM lifecycle **39**. Both executable stages ran. |
| `bash plan/tests/playersess/run.sh` | Exit **0**, **217/217** checks. |
| `PLAYER_HISTORY_SOURCE=/workspace/work/maintenance-foundation/quality-execution/PlayerControl-overlap.cs bash plan/tests/playersess/run.sh` | Exit **1**, **3/217** failed: overlap, latest tail, duplicate tail. |
| `PLAYER_HISTORY_SOURCE=/workspace/work/maintenance-foundation/quality-execution/PlayerControl-drop-tail.cs bash plan/tests/playersess/run.sh` | Exit **1**, **1/217** failed: latest pending tail. |

Inspected full-sweep evidence reconciles to **43/43 directory batches plus seven/seven logical single-file probes = 50/50 logical entries PASS; 51/51 executable stages PASS**. The chain's eight executables are LM01–LM05, API01, AUTH01B Account and AUTH01B LoginVM; no masked suffix remains. Retained `auth1` 35, `api02` 12, `auth3` 239, `auth4` 134 and `webrefresh` 111 results pass independently and in the sweep. g3 passes 386 assertions with its nested RichTextFlow 105 checks; LM05 passes 22. Comments/c1 counts include separately owned U1/PB1 work and are not presented as quality-owned assertions.

The initial sweep has **44 commands**, with only the aggregate chain exiting **126** after LM05 because snapshot copying lost executable modes. Original/index script modes were valid; scratch mode restoration changed no bytes. The unchanged corrected chain then exited **0** and visibly ran API01 plus both AUTH01B stages. Raw failed and corrected attempts are retained. The effective 50/51 accounting uses this complete corrected result, without hiding the first failure or double-counting targeted/negative-control reruns.

Warnings remain visible: existing CS8981/CS0108/CS1998/CS0067 and csc CS1701; signature-only LoginVM instrumentation exposes CS4014 at original async-void call sites. Negative serialization mutation's CS0162 is a scratch-only expected warning. No warning suppression or assertion reduction was used.

Windows compile is still **FAILED/BLOCKED — environment unavailable** (recorded prerequisite attempt exits 127, `powershell: command not found`; Linux lacks VS/UWP/Windows SDK/runtime). Real WinRT cookie-store behavior, installed startup, MSIX inspection, playback/layout/focus/accessibility and live-session/device journeys are unverified here. Offline pure-source/stub evidence does not pass those gates or reopen historical maintainer-accepted rows.

QF1's finalized evidence explicitly releases its shared harness ownership after metadata commit `858b971`. Any next-wave writer still needs its own approved dispatch and must preserve this committed baseline; this review creates no concurrent write grant. Implementation acceptance is **PASS within the stated offline scope**, with platform limits and future independently owned work carried explicitly.
