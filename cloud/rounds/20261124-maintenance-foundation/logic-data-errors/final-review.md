# 实现审查：U1 comment forms and PB1 business-code boundary

- 轮次：`20261124-maintenance-foundation`
- 审查类型：实现
- 审查员：`logic_review`，独立实现审查
- 结论：**PASS — U1 and PB1 implemented and offline verified. Windows compile/session/device gates remain BLOCKED.**
- U1 source/test commit：`5eec54ecd094bdb51ec43b42d9f901d8fae7c993`
- PB1 source/test commit：`edbd8739d894b8ab59400e097eb96a9a0a1ac592`
- Evidence-only follow-up：`10dd84857c748f6638e187d891d6a6649f887bb6`
- Scope：the two immutable diffs against their respective parents, approved logic requirements/review and execution dispatch. This is not a repository-wide runtime, release or next-wave implementation approval.

## 审查范围与证据

Read repository/cloud instructions, memory INDEX and referenced current run/decisions, `implementation-review-dispatch.md`, controlling `logic-data-errors/requirements.md` and `review.md`, execution ownership dispatch, source-stream index and both slice execution records. Inspected full immutable product/test diffs and original/new caller/parser contracts; reviewed the executor's raw red/green logs under `/workspace/work/maintenance-foundation/source-execution/`.

The U1 commit changes only CommentApi, comments project/program, a narrow request-only stub and its execution record. PB1 changes only the three approved product parser files, additive comments/c1 program coverage and source execution metadata. The metadata follow-up changes no product/test source. The combined product diff contains exactly:

- `src/BiliLite.UWP/Api/CommentApi.cs`
- `src/BiliLite.UWP/Parsing/ApiEnvelope.cs`
- `src/BiliLite.UWP/Parsing/Video/VideoEnvelope.cs`
- `src/BiliLite.UWP/Parsing/Comment/WebCommentActionParser.cs`

No shared ApiHelper, cookie/credential/transport service, caller UI, account migration, UWP project/dependency, manifest, asset, proto, live JSON or main documentation changed in either slice. Documentation dispositions are reserved for the later single owner under the dispatch; that queued ownership does not require concurrent reviewer edits.

### U1 behavior and regression review

`CommentApi.cs:83–129` encodes the existing raw CSRF return exactly once for all four actions. Like/ReplyComment/AddComment now append a separate `&csrf=` field; DeleteComment preserves its field and now escapes its value. Empty source token becomes an explicit empty field. The former boolean helper argument was removed locally, without changing the helper contract or cookie state.

Routes, HTTP verbs, body field order, existing leading ampersands, IDs, root/parent, type/action and metadata are otherwise preserved. AddComment still escapes raw text once; ReplyComment still inserts its already escaped message unchanged. Immutable caller inspection confirms `CommentControl.xaml.cs:523,569` supplies escaped replies, while `SendCommentDialog.xaml.cs:38–39` supplies raw new-comment text. No extra decoding/encoding was introduced at callers.

`CommentsProbe.csproj` links the complete real CommentApi. The new `CommentRequestStubs.cs` supplies only enum/metadata/constants/raw-token/WBI dependency seams; no request construction is duplicated. The unused WBI operation throws rather than supplying a false signing success. The raw-token stub matches the existing CookieService behavior, but does not claim to exercise WinRT cookies.

The new tests construct 54 real requests across three tokens (ordinary, reserved/non-ASCII, empty), types 1/17/12, like/unlike, direct/child replies, add and delete. They preserve distinct IDs above 2^53 and exact emoji/Chinese/reserved/newline/literal-percent text. Form parsing retains a list of literal keys, splits before one decode, translates form plus correctly and checks exact multiplicity, count, allowed keys and values. Thus duplicate csrf, injection, token contamination and double-escaped replies cannot be hidden by dictionary overwrite. All original parser/mapper/silence/cursor/source-wiring assertions remain.

### PB1 behavior and regression review

`ApiEnvelope.cs:26–56` introduces one internal `TryReadBusinessCode` in the existing envelope file. It rejects missing/noninteger tokens before conversion, handles BigInteger and UInt64 with explicit Int32 bounds, then safely reads remaining primitive integer representations as Int64 and narrows only after range validation. Invalid input leaves the out value zero and returns false; callers produce the existing Envelope category/stage instead of treating zero as success. It does not coerce strings/floats/bools, catch unrelated exceptions, truncate, clamp or widen BusinessCode.

Immutable search finds exactly the helper declaration and three call sites: ApiEnvelope's shared data/result payload reader (`:77`), VideoEnvelope (`:35`) and WebCommentActionParser (`:45`). No `codeToken.Value<int>` remains under Parsing at the PB1 commit. All four public/feature paths therefore share the same bounded policy.

The specialized-reader diffs replace only unchecked code extraction. Existing message/msg fallback, comment-action rejection fallback, transport/JSON categories, code-rejection precedence and endpoint payload behavior remain intact. ApiEnvelope still requires the selected data/result object; VideoEnvelope returns raw or absent data; comment actions require no data and validate range before DTO deserialization. In-range rejection still carries exact business code/message even when data is absent. Parsing remains UI/log free.

Permanent tests call actual linked readers, including internal VideoEnvelope in the c1 assembly. They cover positive/negative Int32 overflow, both beyond-Int64 literals parsed as BigInteger, missing/null/string/float/bool codes, Int32 extrema/-404, missing-data rejection, zero/empty and each payload policy, message fallbacks, failed/null transport and malformed JSON. The existing season result-envelope consumer is rerun unchanged. There is no copied production code-reader implementation in the repository tests.

## P0（必须修复，阻断）

None found in the reviewed implementation scope.

## P1（必须修复，可本轮闭环）

None found. The previously approved U1 construction defect and PB1 malformed-code exception paths are closed **at the source/offline boundary** by the reviewed commits and independently reproduced red/green evidence below.

Windows compilation and real session/transport/device checks remain explicitly blocked by the host. They must not be relabeled PASS or described as runtime/release readiness. E1 dialog recovery is separate next-wave work and is not claimed fixed by these commits.

## P2（记录到 ROADMAP）

No new P2 defect in the touched source/test structure. No unrelated cleanup or expanded product ownership is needed for this approval. Existing broader maintenance backlog, documentation dispositions and platform follow-ups remain with their explicitly assigned owners.

## 验收标准核对

| Approved requirement | Implementation/evidence | Result |
| --- | --- | --- |
| Singular explicit encoded csrf for all four builders | Exact literal-key multiplicity and decoded values for ordinary/reserved/empty tokens; delete included | PASS |
| Preserve caller text and opaque fields | Raw Add versus escaped Reply preserved; Unicode/emoji/newline/percent text, high IDs, separate root/parent, action/type retained | PASS |
| Limit shared credential blast radius | No helper/cookie/transport/account/UI change; actual request source linked and dependency fakes explicit | PASS |
| Meaningful U1 red/green | New committed assertions fail old actual builder with 174 request failures, then pass unchanged on U1 | PASS — independently rebuilt/reproduced |
| One bounded code reader across four paths | Shared helper with BigInteger/UInt64 guards and three call sites, no remaining unchecked conversion sites from targeted search | PASS |
| Typed failure and preserved rejection/payload rules | All four paths reject malformed/out-of-range code, retain in-range code/message and endpoint-specific data/result/no-data behavior | PASS |
| Meaningful PB1 red/green | New committed tests yield 12 comments and 4 video exceptions on old readers; unchanged tests pass fixed readers | PASS — independently rebuilt/reproduced |
| Original assertion preservation | Test program diffs are additions only: comments +132/-0, c1 +57/-0; original stubs and season1 unchanged | PASS |
| Required affected checks and scope | comments/c1/season1 pass at PB1; immutable diff checks clean; docs and shared ownership respected | PASS |
| Structure/data ownership | Existing API/parser boundaries retained; one internal code policy, no orphan registration or competing product implementation | PASS for touched scope |
| Windows/session/runtime claims | Linux/PowerShell absence documented; no live write, runtime, installed launch or release claim | BLOCKED platform checks retained |

## 测试/构建证据

Reviewer created four isolated scratch snapshots under `/workspace/work/maintenance-foundation/source-review/` from immutable git objects with lazy fetching disabled. Each contains 411 relevant commit-bound source/XAML/project/probe paths. No test ran against moving shared-checkout source, and no shared-checkout bin/obj was generated by this review.

- `u1-green`: exact U1 source/test state.
- `u1-red`: identical U1 permanent tests, replacing only CommentApi with its U1-parent source.
- `pb1-green`: exact PB1 source/test state.
- `pb1-red`: identical PB1 permanent tests, replacing only the three parser files with their PB1-parent sources.

Each run used SDK 8.0.427/runtime 8.0.33, `UPSTREAM_UNIT_TOOLS=/workspace/work/maintenance-foundation/quality`, that SDK prepended to PATH, the existing NuGet cache, a review-local CLI home and `bash plan/tests/<batch>/run.sh` from the named snapshot. Complete reviewer logs are `<snapshot>-<batch>.log` in the review scratch root. The subprocess runner preserved each actual exit; expected red failures were not converted into successful gates.

| Snapshot / command | Exit | Independent observed result |
| --- | --- | --- |
| u1-red / comments | 1 | 174 request-form failures, including missing csrf/contaminated type/message and reserved-token delete failures; compiled successfully |
| u1-green / comments | 0 | **PASS 856 assertions** |
| pb1-red / comments | 1 | **12 exceptions** across data/result/comment action, with OverflowException and InvalidCastException at the specified values; compiled successfully |
| pb1-red / c1 | 1 | **4 exceptions** on the video reader at the same values; compiled successfully |
| pb1-green / comments | 0 | **PASS 917 assertions** |
| pb1-green / c1 | 0 | **PASS 186 assertions** |
| pb1-green / season1 | 0 | **PASS 117 assertions** |

These independently match the executor's raw records and reported totals. Green comments/c1 have no compiler errors or warnings. Season1 has zero compiler errors and two existing CS0067 warnings in unchanged RelayCommand events; they are unrelated to either patch and were not suppressed by the reviewer.

Additional scratch-only `representations/Representations.csproj` links the exact PB1 ApiEnvelope, VideoEnvelope, comment action parser, result and DTO source. Its response seam supplies a JObject directly, without implementing acceptance/conversion. It tests 25 token cases through the helper and all four readers (125 checks): signed/unsigned primitive storage, UInt64 within range/overflow/max, BigInteger zero/Int32 boundaries/±10^100, missing/null, float/string/bool/array/object. Command:

```bash
DOTNET_ROOT=/workspace/work/maintenance-foundation/quality/dotnet \
DOTNET_CLI_HOME=/workspace/work/maintenance-foundation/source-review/cli-representations \
/workspace/work/maintenance-foundation/quality/dotnet/dotnet run \
  --project /workspace/work/maintenance-foundation/source-review/representations/Representations.csproj -c Debug
```

Exit **0**, `REPRESENTATION_CHECKS=125 FAILURES=0`. This directly exercises the UInt64 and in-range BigInteger storage branches that ordinary JSON literals may not produce. The probe remains scratch, not an extra repository test framework or product change.

Both immutable commit `git diff --check` commands produced no whitespace findings. Product/caller and code-reader searches were performed against the named commits, not later implementation edits. The reviewer made no commit/push and no product/test/build/main-doc or prior-artifact mutation; only this owned final review is written to the repository.

### Platform limit and completion meaning

The executor's Windows command `powershell -NoProfile -ExecutionPolicy Bypass -File ./build-release-x64.ps1 -Fast -NoVersionBump -Restore` returned **127**, `powershell: command not found`, before compilation. Reviewer environment recheck remains Linux with neither powershell nor pwsh on PATH. No UWP/XAML/VS/Windows SDK compilation, WinRT cookie-origin/body inspection, authenticated like/unlike/add/direct reply/child reply/delete, expired-session check, installed launch or device test was performed. Preserve their **BLOCKED/FAILED — environment unavailable** records until actual Windows/session evidence exists. Console builds do not certify the new System.Numerics use under the UWP toolchain or any rendered/runtime behavior.

The source/data-structure quality gate is **PASS for the changed scope**: request ownership remains local, wire values retain precision, one bounded typed business-code policy serves all existing relevant readers, specialized payload responsibilities stay separate, and test-only seams are clearly located in the owning batch. No new dead/orphan product file, duplicate product policy or broad abstraction was introduced. This statement does not assert repository-wide structural/runtime completeness; whole-suite harness closure belongs to the separate quality implementation review.
