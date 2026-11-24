# 审查：Verification and engineering quality requirements

- 轮次：`20261124-maintenance-foundation`
- 审查类型：需求
- 审查员：`quality_review`（独立审查）
- 结论：**PASS — amended requirements; prior API01 namespace finding withdrawn**
- Original product/test evidence: `dev`, `41dc562fba6e45896b0c4ba604f9ec38f4661e73`; superseding-correction review HEAD `10dd84857c748f6638e187d891d6a6649f887bb6`.

**PASS for the specified QF1 harness repair requirements and maintenance policy, as corrected by [amendment-review.md](amendment-review.md). My earlier P1-02 namespace finding was factually incorrect and is withdrawn.** Actual ApiHelper declares the global compatibility wrapper and its Cookie field uses that derived type; preserve/revert the original global-wrapper constructions. Controlling requirements `:120`, execution dispatch `:30` and plan `:54` now record this correction. All valid dependency, fixture, LM05, session and regression requirements remain. QF2 stays PLAN ONLY; this PASS grants no aggregate-runner/CI/dependency migration, product change, shared documentation write, release or fresh Windows/device acceptance.

## Re-review closure evidence

Read the revised `verification/requirements.md`, `maintenance-plan.md`, execution dispatch, [ownership-amendment.md](ownership-amendment.md), [ownership-conflict.md](ownership-conflict.md), independent PASS [amendment-review.md](amendment-review.md) and plan's recorded correction. Earlier metadata-only review statements refer to HEAD `3317305`; current HEAD also includes separately owned U1/PB1 commits, which are not implementation-reviewed here. Source inspection of **both** ApiHelper's header and the namespaced service corrects the earlier incomplete type search.

| Prior finding | Re-review disposition and exact evidence |
| --- | --- |
| P1-01 | **CLOSED.** Revised requirements `:51,105` and current execution dispatch include `LM05.cs`'s using correction with all fixtures/assertions retained. g3 links the real formatter. |
| P1-02 | **WITHDRAWN factual namespace finding; valid fixture/dependency requirements retained.** Actual `ApiHelper.cs:15–23` defines global `CookieService : BiliLite.Services.CookieService`; `:28` types ApiHelper.Cookie as that wrapper. Original `API01.cs:147,232` constructions are correct. Independent `amendment-review.md` PASS and current requirements `:120`/dispatch `:30` require preserving/reverting them, using the wrapper's supported constructor plus isolated real LocalFolder-backed state. The other named fixtures may use the actual base service when not assigning the derived field. `plan.md:54` supersedes the erroneous earlier instruction. |
| P1-03 | **CLOSED.** Requirements `:58–70,107` make QF2 PLAN ONLY and require a separately reviewed concrete runner/stage/workflow specification. The canonical draft matches that boundary and the 50-logical/51-executable denominator. No workflow or aggregate interface is granted. |
| P2 documentation ownership | **CLOSED for this dispatch.** Requirements `:96`, canonical plan ownership and execution dispatch reserve all shared main docs for a later single owner. Source and quality owners remain disjoint, commits serialized through the source-first / explicit quality commit-window arrangement. |
| P2 synthesis drift | **CLOSED.** Current plan uses actual supported minimum Windows window plus 800×600/1280×720 samples, includes the full UX matrix by reference, and retains QF3's P1-before-release priority. |

The reviewed stronger refresh fixture is acceptable as deliberate added protection: installing distinguishable replacement cookies makes confirmation use replacement CSRF plus previous token, matching existing real `webrefresh` coverage. It must not be reported as proof the old empty-store fixture was invalid: settings-to-jar fallback can populate its CSRF. Revised per-assertion mapping remains mandatory and all unrelated assertions stay intact. QF1c's exact extraction/serialized-context/negative-control shape is sufficient; inability to keep it faithful leaves the item FAILED, never green through a looser source assertion.

The corrected execution dispatch aligns with requirements §4.1–4.3 and the independent amendment PASS. It rejects unassigned `AUTH01BLoginVM.cs` scenario changes, excludes comments/c1/season1 from quality writes, and retains source/snapshot plus serialized commit-window coordination. The retained executor log `quality-execution/logs/compile-run-api01.sh.log` reports **CS0266 at API01.cs:161,232** after the erroneous dispatched substitutions; that confirms the review error, not a baseline product defect. Current in-flight API01 edits still require the assigned executor's restoration, actual compile/regression evidence and implementation review. No new test-pass claim is made by this requirements correction.

The initial detailed findings below are retained as the review history. Their re-review status above controls; no new implementation/test pass is claimed.

## 审查范围与证据

Read `AGENTS.md`, `cloud/CONTRACT.md`, `cloud/REPO.md`, `cloud/memory/INDEX.md` and its relevant decisions/run entries, round `plan.md`, `intake.md`, `review-dispatch.md`, all three final requirements artifacts, verification `evidence/baseline.md`, and the subsequently supplied draft `maintenance-plan.md`. Cross-checked the actual compile lists, runners, stub/model namespaces, credential fixture setup and assertions, Account extraction inputs, player checkpoint/drain implementation, and package-layout guard. This review owns only this file.

Independent read-only verification during review:

- `git branch --show-current` / `git rev-parse HEAD`: `dev` / the SHA above. `git status --short` showed concurrent cloud round/memory work; no product/test changes were present.
- Python inventory over current `plan/tests/`: **43 directories, 42 directory projects, 42 directory `run.sh` files**. `g1` has its root runner; `stab` has no project.
- Recomputed SHA-256 for every entry in scratch `quality/logs/source-manifest.json`, against both original checkout and `quality/baseline/`: **697 entries; zero mismatches in either location**. Checked all **197 asset links** for existing original targets, matching size and actual symlink resolution: **zero invalid links**. Symlinks are an analysis-use convention, not filesystem-enforced read-only permissions; no asset mutation was observed.
- Read raw `results.json`, `batch-g3.txt`, `batch-playersess.txt`, `batch-single-files.txt`, `api01-independent.txt`, `auth01b-independent.txt`, `batch-cm1-corrected-snapshot.txt`, `shell-after-sdk.txt`, `windows-build.txt`, `sdk-info.txt` and `sdk-integrity.txt` under `/workspace/work/maintenance-foundation/quality/logs/`. Inspected `run-baseline.py`: it runs commands sequentially, captures exit/output and continues; it does not rewrite source inputs.
- Source commands included `nl -ba`/`sed` on the exact files cited below and `rg -n '^namespace BiliLite\\.Modules'` across all LM05 compile inputs. No new test run or claimed implementation pass was manufactured in this review.

## P0（必须修复，阻断）

None in these quality requirements. U1 remains the logic stream's existing P0; this is not a second U1 implementation assignment.

## P1（必须修复，可本轮闭环）

### P1-01 — CLOSED on re-review: LM05 consumer namespace ownership

Evidence: `verification/requirements.md:101` restricts QF1a to `plan/tests/g3/G3Probe.csproj` and `plan/tests/LM05Stubs.cs`. The latter declares `BiliLite.Modules.Player` at line 5, while actual `SubtitleTrackParser.cs:7,18` consumes `BiliLite.Modules.SubtitleModel`; actual model declarations are in `Modules/Player/PlayerVM.cs:319–334` within `BiliLite.Modules`. **`plan/tests/LM05.cs:7` also imports the old `BiliLite.Modules.Player` namespace.** No other LM05 compile input declares that namespace. Moving the stubs to the real namespace alone leaves this using directive unresolved, so the named two-file slice is incomplete.

Required revision: explicitly include `plan/tests/LM05.cs` for the import correction only, alongside `LM05Stubs.cs`; preserve every assertion and fixture. Do not invent a compatibility model or empty namespace merely to keep an obsolete using directive compiling. Keep product code unchanged. QF1a's exact proposed test ownership should therefore be:

- `plan/tests/g3/G3Probe.csproj`: link existing real `Parsing/Dynamic/ReserveCardCopy.cs`.
- `plan/tests/LM05Stubs.cs`: align only the substitute subtitle-model namespace/member contract.
- `plan/tests/LM05.cs`: align the using directive, with no assertion reduction.

Acceptance: record the existing compile-error red evidence; after repair, unchanged g3 behavioral assertions and all LM01–LM05 assertions execute and pass. The aggregate may then expose API01/AUTH01B failures; report them, and keep whole-suite status FAILED until resolved. This remains disjoint from U1/PB1's comments/c1/season1 ownership.

### P1-02 — WITHDRAWN namespace finding; superseding compatibility contract

The earlier claim that the two `global::CookieService` constructions were unresolved was wrong: it missed the real compatibility class at the top of linked `ApiHelper.cs`. The erroneous namespace-only amendment is explicitly withdrawn and must not be implemented. Preserve/revert both original wrapper constructions wherever assigning `ApiHelper.Cookie`; no product field-type edit, fake wrapper or cast workaround is authorized.

The independently approved replacement contract preserves the real constructor boundary. The wrapper exposes arguments through `log` (`ApiHelper.cs:18–22`); the base additionally exposes state-store/phase inputs (`Services/CookieService.cs:103–118`). Its wrapper-assigned fixture therefore uses a case-isolated real LocalFolder-backed state store through the existing platform path substitute; other named refresh fixtures may use the base's actual pure-service arguments. Isolation persists until all awaited work completes, then restores/cleans path and pending files.

All other reviewed ownership/behavior remains: actual refresh policy/transaction/bridge and mirror initializer, named same-owner replacement fixtures, replacement CSRF plus previous token, provenance/rollback/pending-state checks, per-assertion mappings, WBI/raw-CSRF/session/LoginVM guards, and all unaffected assertions. The original empty-store fixture is not declared invalid solely from constructor inspection; stronger replacement-jar coverage is deliberate added protection. See the independent amendment review for exact source/compiler evidence and retained regression gates.

Acceptance: independent API01 and both AUTH01B executables pass, then the aggregate chain passes; `auth1`/`api02` pass whenever shared `API01Stubs.cs` changes, and affected real credential regressions (`auth3`, `auth4`, `webrefresh`) remain green when their contracts are brought into the repaired harness. Record newly exposed failures honestly. These are harness repairs, not an account-parser migration or fresh WinRT/session validation.

### P1-03 — CLOSED on re-review: QF2 plan-only boundary

Evidence: `verification/requirements.md:56,59,63,66,103` calls QF2 ready after QF1 but offers “a single owned entry point **or** documented inventory command,” a generic separate docs/CI owner, and a future CI increment without an owned file list/interface. Current `run.sh` has `set -e` and embeds LM01–LM05 compilation; `run-auth01b.sh` similarly places LoginVM after the Account executable. Merely invoking the three existing top-level scripts independently cannot enumerate every later masked stage.

Required revision: either mark QF2 explicitly **PLAN ONLY — separate requirements/review required before runner/workflow implementation**, or provide a concrete bounded shape now. A ready shape must name the entry-point path, existing runners it may change, how each of LM01–LM05 and both AUTH01B stages can be selected/reported independently, exact 50-logical/51-executable denominator handling, prerequisite/timeout/error statuses, aggregate exit contract, and output/log location. Name the CI workflow path, trigger/permissions/toolchain/timeout/artifact contract and a single owner only if CI is included in the reviewed implementation. Verify failure propagation with an injected failing scratch stage and a missing required stage; both must fail the aggregate while later stages still report. Keep SDK/dependency migration, release publishing and Windows runner provisioning outside this grant.

Acceptance: this revision removes ambiguity about what PASS authorizes. A plan-only QF2 is sufficient to unblock the narrower QF1 requirements; actual QF2 implementation must return for its own requirements review. Publishing a workflow or changing dependencies is not authorized merely because CI is recommended here.

## P2（记录到 ROADMAP）

- Preserve QF3's confirmed root-launcher guard gap as a separately owned build item: current `build-release-x64.ps1:426–432` checks entrypoint/companion/declaration but omits root launcher existence/size. Good/bad fixtures are useful; a real Windows build and installed launch remain release requirements. Do not fold this into QF1's test-only ownership.
- Reconcile documentation ownership in synthesis. `verification/requirements.md:92` proposes feature ownership of `plan/PLAN.md`; dispatch assigns all shared PLAN/ROADMAP/VALIDATION/DEVELOPMENT writes to one later documentation owner. The dispatch decision takes precedence. Record one exact writer before any executor starts; this reviewer grants none of those files.
- Correct synthesis-only drift before promoting `maintenance-plan.md` from draft: its UX row at line 49 invents a `320/480/800+` width matrix, while UX requirements explicitly use the actual minimum usable window plus **800×600/1280×720 effective-pixel samples** and do not add an unsupported mobile target (`ui-ux/requirements.md:43,99`). Preserve the reviewed supported-window contract. Its QF3 row at line 51 says P2; quality requirements say **P1 before the next release**. Keep that conditional release priority visible even though the current Windows gate is blocked. The draft's QF2 row correctly says detailed implementation scope is still required, supporting the plan-only resolution in P1-03.

## 验收标准核对

| Contract | Review judgment / required result |
| --- | --- |
| QF1-G3 | Correct diagnosis: real `DisplayModel.cs:73` calls existing formatter omitted from g3. Compile the real dependency; retain behavioral tests. |
| QF1-LM | Correct diagnosis and behavioral goals; ownership needs P1-01. Existing SRT/VTT/JSON, cue boundaries, formatting failures, grant/file handles and stale subtitle/danmaku completion scenarios remain. |
| QF1-API/AUTH | Real dependency/extraction compile drift confirmed; preserve method bodies and state decisions. Use the corrected actual global-wrapper contract and reviewed fixture boundaries. Both AUTH01B stages must run; compiling Account alone is insufficient. |
| QF1-SESSION | **Accept the specified invariant, reject a string-only repair.** `playersess/Program.cs:450` demands an obsolete literal, while real `PlayerControl.xaml.cs:3455–3474,3486–3514` saves locally before awaiting, coalesces latest pending state, serializes a drain and clears in finally. This does not demonstrate runtime overlap. |
| Session replacement coverage | Use exact extracted current checkpoint/drain/flush methods or an equally direct existing seam; do not copy/reimplement the algorithm. On one serialized execution context, gate report A, invoke B then C, assert maximum active reports = 1 and local C save before A is released; release A and assert only latest C tail is sent. Requeue identical item/position during its report and assert no duplicate tail. Assert paused ordinary checkpoint does not send, paused flush can send, and a thrown report clears the guard so a later report proceeds. Retain existing timer, session/resource and App-deferral wiring checks. |
| Session negative control | In a scratch mutation of the **extracted real source**, bypass serialization and demonstrate the overlap case fails. Prefer also dropping pending-tail retention and observing the latest-tail assertion fail. Original actual methods must pass the same assertions. Changing only the failed literal or asserting the helper's presence is insufficient. If bounded direct testing is infeasible, leave QF1-SESSION **FAILED**, preserving its old evidence, rather than marking the batch green. No player product refactor is authorized. |
| Cross-stream U1 | Logic and UX agree: actual builder, exactly one encoded raw-token `csrf`, message raw for AddComment/already escaped for ReplyComment, exact IDs/type/action, no shared `GetCSRF(bool)` change. Logic's detailed missing-token contract and multiplicity checks control. Existing comments parser coverage alone proves none of this request behavior. Quality checks must not race the exclusive comments owner. |
| Cross-stream PB1 | Logic specifies all four reader paths, Int32 bounds and beyond-Int64/BigInteger, typed Envelope failure, preserved in-range rejection and payload policy. U1 then PB1 share comments files and must be sequential. No QF1 repair may change comments/c1/season1 while that source executor owns them. |
| QF2/QF5 | Inventory, Bash, exact SDK/ref-pack inputs, truthful status and cadence are appropriate. Implementation shape requires P1-03; cadence recommendations do not schedule automations or publish CI. |
| QF3/QF4 | Correctly separates offline fixtures/advisory query from Windows compile, package, compatibility and runtime gates. No upgrade, restore migration, signing/version edit, asset retrieval or release is implied. |

## 测试/构建证据

Accepted baseline evidence is **41/43 directory batches PASS; g3 compile FAILED; playersess 1/203 FAILED**. The raw first helper run has 44 entries (43 batches plus aggregate chain), 40 zero exits and four nonzero exits because its first cm1 attempt lacked an analysis-snapshot asset. The preserved corrected-snapshot cm1 log passes 71 unchanged assertions, so the curated 41/43 result is reconciled rather than inflated.

Single-file logical total is **7: four PASS (LM01–LM04), three compile FAILED (LM05/API01/AUTH01B)**. API01 and AUTH01B were invoked independently after the earlier chain failure. AUTH01B contains two executable stages: Account-cookie compilation failed and **LoginVM remains unexecuted**. Across the inventory this means 50 logical entries / 51 executable stages, not 50 fresh runtime passes.

Scratch SDK integrity evidence records Microsoft exact-version SHA-512 match, SDK **8.0.427**, runtime/ref pack **8.0.33**, and SDK-internal Newtonsoft.Json. The source and asset consistency checks above make the reproduced failure inputs current and available. Script LF is separate from interpreter selection: `sh plan/tests/firstrun/run.sh` under dash fails with `Bad substitution`/MSB1009, while the Bash baseline succeeds. Use Bash as dispatched; do not rewrite intended Bash semantics to preserve an incorrect `sh` command.

Windows compile attempt is **FAILED/BLOCKED: exit 127, `powershell: command not found`**; UWP compilation never started. Full build, real MSIX inspection, installed startup, rendering/focus/Narrator, playback, cookie-manager behavior and authenticated write/device journeys remain unavailable here. No new Windows/session/device PASS exists. Existing maintainer-accepted historical rows stay accepted under repository policy; that policy is not fresh evidence for changed behavior.

**Final disposition: PASS for corrected requirements; no open P0/P1 requirement findings.** The compatibility-wrapper amendment supersedes every earlier namespace-change instruction. QF1 implementation is not accepted here: the executor must restore the valid constructions, supply independent API01/both AUTH01B stages/chain and assigned dependent gates, then receive actual diff/evidence review. Session negative controls, aggregate denominators, shared-stub release and commit-window coordination remain required. The historical suite baseline is FAILED and Windows/runtime gates remain FAILED/BLOCKED until new evidence exists.
