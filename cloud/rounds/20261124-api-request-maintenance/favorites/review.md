# 审查：U9 favorite-folder request requirements

- 轮次：`20261124-api-request-maintenance`
- 审查类型：需求
- 审查员：`logic_review`，独立需求审查
- 结论：**PASS**
- 批准范围：`favorites/requirements.md` 的 CreateFavorite/EditFavorite 本地字段边界修正及既有 c2 批次的真实 builder 回归。执行仍须等待先前 owner 释放、明确文件派发和提交窗口；不批准共享 helper、凭据、其他收藏操作或线上写入。
- 输入基线：`41dc562fba6e45896b0c4ba604f9ec38f4661e73`；审查时 HEAD `3317305f5c606cd4ca33f21a611c23f15a18dada`。被审 FavoriteAPI、ApiHelper、c2 程序/项目/stub 与原始基线逐字节一致；先前轮次的活动实现不作为稳定新基线。

## 审查范围与证据

Read `AGENTS.md`, `cloud/CONTRACT.md`, `cloud/REPO.md`, memory INDEX and referenced decisions/run record, this round's plan/review dispatch, all three final requirements/evidence pairs, preceding reviewed logic/UI contracts and verification baseline, and preceding `execution-dispatch.md` for active ownership. This review owns only this file and preserves the prior review artifacts.

Inspected the complete actual `src/BiliLite.UWP/Api/User/FavoriteAPI.cs`, current ApiHelper parameter/signing methods, create/edit dialog callers and checkbox XAML, `plan/tests/c2/C2Probe.csproj`, C2Stubs and Program, plus the favorites scratch project/program/stubs/extracted helper. No external API research or live call is necessary to establish a missing local delimiter.

Source findings supporting approval:

- `FavoriteAPI.cs:122,141` directly concatenate `privacy=` after MustParameter; `ApiHelper.cs:179` ends at the timestamp. Both methods subsequently call GetSign (`FavoriteAPI.cs:124,143`), so they currently sign a malformed field boundary. A leading `&` at each local feature fragment fixes the identified construction defect without modifying shared behavior.
- `Dialogs/CreateFavFolderDialog.xaml.cs:29` and `EditFavFolderDialog.xaml.cs:34` pass raw textbox strings and `checkPrivacy.IsChecked.Value`. Both dialog XAML files at `:8` label that checkbox **公开收藏夹**. The reviewed mapping `isOpen=true → privacy=0`, false → 1 matches the active callers despite the ambiguous XML parameter comment. Reversing it would be a regression.
- Current text escaping is inside both real builders. The reviewed raw-input/one-escape/one-decode contract preserves Unicode, literal percent text, delimiters and newlines without changing caller files. Empty strings remain valid builder inputs; existing UI title validation and null behavior are outside this correction.
- Edit media_id is interpolated as the supplied string; the slice preserves exact decimal digits, leading zeroes and values above 2^53. It neither introduces numeric conversion nor promises arbitrary nondecimal identifier syntax.
- API_BASE_URL is configured, and both operations use it with their existing `/x/v3/fav/folder/add` or `/edit` suffix. ApiModel method/body/query/cookie defaults remain intact. Tests must use a nondefault base/prefix fixture to expose accidental host hardcoding.
- MustParameter receives **AndroidKey and true**; GetSign receives the complete unsigned body and **AndroidKey**. `ApiHelper.cs:78–93` sorts raw encoded fields and returns an `&sign=` suffix; it cannot repair the missing separator. A recording seam for input/key/call count is sufficient to prove the unchanged caller contract, provided it never reconstructs the request. It is not cryptographic verification.
- c2 already links real favorite/watch-later parsers, MediaListService and paging rules. Its ApiModel and RestSharp stubs support the needed request shape. It currently does not link FavoriteAPI or define ApiHelper/SettingHelper. Adding the complete real builder link and thin recording settings/key/helper seams is a concrete, small extension; it does not require copying product expressions into tests or changing the existing fake transport queue.

Source integrity checks: all five SHA-256 values in favorites `evidence.md` match the current FavoriteAPI, ApiHelper, extracted helper, scratch Program and Stubs. Independently compared exact balanced method text for GetSign and MustParameter against `ExtractedApiHelper.cs`: **2/2 EXACT**. Independently compared FavoriteAPI, ApiHelper, c2 project, C2Stubs and c2 Program with the quality baseline snapshot: **5/5 MATCH**.

Cross-stream and active-owner check:

- Search owns a later SearchAPI/api02/shared API01Stubs extension; favorites uses c2 and has no shared test-writer overlap with that proposed scope. Neither stream may change ApiHelper or credential policy. Different existing key choices remain deliberate: favorites AndroidKey versus app search LoginKey.
- Comment recovery depends on preceding U1/PB1 and comments ownership release; this review grants none of those files. Preceding PB1 may change ApiEnvelope consumed by c2. Future c2 evidence must run the actual assigned post-PB1 source, retaining its accepted range behavior; the old baseline replay below cannot certify that later source.
- Prior QF1 owns g3/LM05/credential/session harnesses. U9 must not edit those or commit through an occupied shared index. The later documentation owner alone updates PLAN/ROADMAP/VALIDATION/DEVELOPMENT. Requirements §5 correctly keeps docs outside the API executor's automatic grant.
- Both other next-wave contracts preserve their own network/UI limits. This PASS authorizes U9 only; it is not a review approval for search or comment recovery.

## P0（必须修复，阻断）

None in the reviewed U9 requirements.

## P1（必须修复，可本轮闭环）

No unresolved requirements finding. The **existing U9 product P1 remains unfixed at the bound input**, and implementation acceptance must include all of the following already specified gates:

1. New permanent real-builder assertions compile successfully, then fail because timestamp/privacy are malformed before the source correction. Compilation failure or a source-text match is insufficient red evidence.
2. Correct both local boundaries only, preserving privacy direction, configured destination, raw text/ID semantics, complete common fields and the current Android signing input/key. No shared helper or authentication change is approved.
3. Keep the same permanent assertions for green. Preserve all original c2 assertions and fake transport behavior. A signer recorder must see the complete encoded unsigned form exactly once, and final body must equal that input plus its exact single returned suffix.
4. Await explicit ownership release/assignment, use the actual assigned source SHA for permanent red/green, and record per-command exits, commit/remote SHA and blocked platform gates. No live create/edit/delete/cleanup request is permitted.

## P2（记录到 ROADMAP）

No new P2 requirement defect. The misleading `isOpen` XML description and existing folder-dialog response/lifetime behavior are outside the two-boundary correction. This review does not reopen those behaviors or grant cleanup ownership. Any later work needs a separate scoped evidence/requirements decision; it must not be slipped into U9 to make the patch broader.

## 验收标准核对

| Contract | Required verification | Review |
| --- | --- | --- |
| Operation/mode matrix | Create/edit × public/private × common parameters with/without access key: 8 cases, real FavoriteAPI | PASS — exact matrix specified |
| Form keys and timestamp | Exact operation/common/sign field set; every expected key once; ts independent decimal fixture; privacy 0/1; create lacks media_id; no empty key | PASS — multiplicity preserved, no dictionary overwrite hiding duplicates |
| Helper boundary | Deterministic common form ends in `ts=1700000000`, no trailing ampersand; MustParameter records AndroidKey and true | PASS — seam cannot repair the product defect |
| Text | Reserved/Unicode/emoji/newline/percent sequences decode once to original input; empty intro/title remain explicit and unchanged | PASS — no trim, decode or double escape |
| ID | High decimal and leading-zero edit IDs exact, separate from other fields | PASS — no precision loss or normalization |
| Destination/metadata | Configured API host/prefix and add/edit route retained; POST/body/default query and cookie metadata unchanged | PASS — no endpoint/auth migration |
| Signing | One GetSign call with AndroidKey and all final unsigned fields, before suffix; exact raw encoded input and returned suffix preserved | PASS — no field added after signing; recorder scope declared honestly |
| Regression ownership | c2 project/program/C2Stubs only plus real FavoriteAPI; original 162 checks preserved; no new batch/dependency/runner/production helper | PASS — suitable existing compile seam |
| Evidence lifecycle | Permanent old-product red and unchanged green at executor SHA; c2 Bash runner plus diff check; final independent review | PASS — scratch corroboration does not replace committed-test red/green |
| Platform and writes | Windows compile explicit BLOCKED/FAILED here; dummy-only controlled transport if later used; no live favorite writes or release claim | PASS — boundaries clear |

## 测试/构建证据

Independently replayed existing scratch executables using `/workspace/work/maintenance-foundation/quality/dotnet/dotnet` from the repository root. **This is a replay of previously compiled binaries, not a fresh build.** Inspected their bound sources/projects and verified the source integrity noted above.

| Executable argument to scratch dotnet | Exit | Observed result |
| --- | --- | --- |
| `/workspace/work/maintenance-foundation/favorites/request-boundary/bin/Debug/net8.0/FavoriteBoundaryProbe.dll` | 1 | `CONTRACT_VIOLATIONS=16`: eight cases each have ts `1700000000privacy=0/1` and absent privacy; exact text/edit ID/conditional dummy access-key/sign-final-body controls pass. |
| `/workspace/work/maintenance-foundation/quality/baseline/plan/tests/c2/bin/Debug/net8.0/C2Probe.dll` | 0 | `PASS 162 assertions`; existing parser, typed failure, retry, cursor, paging and single-flight behavior intact on that original snapshot. |

The reproducer links the whole original FavoriteAPI and exact extracted helper bodies. Its MD5/time/settings/network-free seams are declared, and the scratch program makes no Request or HttpClient call. A valid-looking signature is only a passing control: the replay signs the malformed current form successfully, demonstrating why independent field/input assertions are essential.

Execution must use `UPSTREAM_UNIT_TOOLS=/workspace/work/maintenance-foundation/quality bash plan/tests/c2/run.sh` with the existing SDK/cache environment, then `git diff --check`. Existing Bash scripts must not be invoked through this host's dash as `sh`. The executor must rebuild/test its actual current source rather than treat this immutable baseline replay as final evidence.

Required Windows compile remains `powershell -NoProfile -ExecutionPolicy Bypass -File ./build-release-x64.ps1 -Fast -NoVersionBump -Restore`. Linux lacks PowerShell/VS-UWP/Windows SDK/runtime; no UWP compile, real outgoing WinRT cookie observation, folder persistence, installed-app smoke or device validation ran in this review. They remain **FAILED/BLOCKED or unverified**, and live mutation validation is explicitly excluded from this slice. The whole-suite baseline is separately FAILED and cannot become green because c2 passes.

Reviewer made no product/test/build/main-doc/helper/credential/asset/branch/history change, no commit/push and no live-service write. Only this review file is owned and written.
