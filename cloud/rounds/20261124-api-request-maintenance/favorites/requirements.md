# U9 — favorite-folder request fields

Status: **READY FOR REQUIREMENTS REVIEW**. Priority P1; implementation is not authorized by this document alone.
Source: `dev` at `41dc562fba6e45896b0c4ba604f9ec38f4661e73`, 2026-11-24. Owner: `logic_requirements`.
Inputs: this round's `plan.md`; repository/cloud instructions and memory index/run; preceding logic, UX and verification artifacts; API research; actual builders, helper, callers, XAML and existing c2 probes. Evidence: [evidence.md](evidence.md).
Product paths below are relative to `src/BiliLite.UWP/`; other paths are repository-relative. No product/test/build/main documentation or history changes were made for these requirements.

## 1. Observed defect and impact

`Api/User/FavoriteAPI.cs:116–144` defines `FavoriteApi.CreateFavorite` and `EditFavorite` (class spelling differs from the filename).
Both concatenate `ApiHelper.MustParameter(ApiHelper.AndroidKey, true)` immediately with `privacy=...`, without an ampersand.
`Api/ApiHelper.cs:172–180` ends the common form in `ts=<timestamp>`; neither common-helper implementation nor caller supplies a trailing separator.
Consequently both bodies contain `ts=1700000000privacy=0/1`, no separate `privacy`, and a signature over the malformed body (`FavoriteAPI.cs:124,143`). This is a confirmed local construction defect, not proof that a live service rejected a real folder action.

A scratch experiment compiled the complete **actual** `FavoriteAPI.cs`, with the current helper's exact extracted `MustParameter`/`GetSign` method bodies and deterministic dummy platform/settings/hash seams. Eight create/edit × public/private × logged-in/out cases returned **16 contract violations**, exit 1: timestamp contaminated and privacy missing in each case. Text, edit ID, optional access key and signing of the final existing body survived. No endpoint was called; full source/tooling/methodology is in the companion evidence.

The actual callers pass raw title/intro and the public checkbox state (`Dialogs/CreateFavFolderDialog.xaml.cs:29`, `EditFavFolderDialog.xaml.cs:34`). Both XAML files label the checkbox **公开收藏夹**. Thus `isOpen=true → privacy=0`, `false → privacy=1`; the XML parameter comment “是否私密” is ambiguous and must not invert that mapping.

## 2. Goal, non-goals and execution dependency

Goal: construct two valid existing signed forms by separating timestamp/privacy, while preserving all existing request, encoding, ID and credential contracts.
The expected product correction is a separator at each local concatenation; do not redesign shared form/signing infrastructure for this slice.
Non-goals: folder UI lifetime/retry/parser changes, renaming public APIs, new validation rules, modifying add/remove/move/copy/sort/favorite-list requests, migrating endpoints, account/refresh changes, shared helper/transport/cookies, dependencies, manifests, new feature work or release actions.
**No live favorite-folder writes are permitted**, including cleanup writes. All validation uses dummy data and controlled offline request capture. Live service acceptance remains unverified/excluded from scope.

Execution waits for this requirements review PASS, prior-round source/test/documentation ownership release, and explicit executor assignment. Preserve U1/PB1 priority. This file does not alter the preceding reviewed requirements.

## 3. Exact request and data contract

| Property | CreateFavorite | EditFavorite |
| --- | --- | --- |
| Method | POST | POST |
| Base URL | Existing `ApiHelper.API_BASE_URL` + `/x/v3/fav/folder/add` | Existing configured base + `/x/v3/fav/folder/edit` |
| Common fields | Existing `MustParameter(AndroidKey, true)` output, unchanged | Same |
| Feature fields | `privacy`, `title`, `intro` | `privacy`, `title`, `intro`, `media_id` |
| Signature | Existing `GetSign(completedUnsignedBody, AndroidKey)` suffix, appended once | Same |
| Query / cookie metadata | Existing defaults retained; fields stay in body | Same |

The ordinary common-field set is `appkey`, `build`, `mobi_app`, `platform`, `ts`, plus `access_key` only when the existing helper's login condition supplies it. Each field retains exactly its current value; timestamp is an independent decimal timestamp. Keep the helper's `needAccesskey=true` argument and **AndroidKey for both common parameters and signature**. Do not substitute LoginKey, invent credentials, append CSRF, or mutate account state.
The ordinary final field set is exactly common fields + the operation's feature fields + `sign`, each once. Create must not add `media_id`; edit must not rename/drop it. No `ts...privacy` or empty key is acceptable in these bodies.

- `isOpen=true` produces decoded `privacy` exactly `0`; `false` produces exactly `1` on both operations.
- `title` and `intro` are raw caller strings and each is URI-escaped **once**, as today. One form decode must restore all Unicode, spaces, `+`, `&`, `=`, `?`, `%`, newlines and literal percent sequences. Do not trim, decode incoming text, or double-escape.
- Empty intro remains an explicit empty `intro=`; empty title keeps existing builder behavior (UI validates title). Null arguments are outside the textbox caller contract; do not introduce unrelated coercion/validation changes.
- Edit `media_id` is an opaque decimal ID **string**. Preserve exact digits/leading zeroes and values above `2^53`; do not parse through floating point or normalize to another ID. This slice does not define support for arbitrary nondecimal ID syntax.
- Preserve configured base URLs and current body-field order; add only the missing boundary needed for correct form semantics. No host hardcoding or payload-to-query move.

## 4. Signature and error boundary

`ApiHelper.GetSign(string, ApiKeyInfo, string)` (`ApiHelper.cs:78–93`) sorts the raw encoded `&`-separated fields, appends the selected key's secret and hashes; its result includes `&sign=`. It does not repair missing delimiters. Keep this implementation untouched.
The builder must finish **all** feature fields, including separate privacy and edit ID, before calling GetSign, then append that exact suffix once. Do not sign a decoded/re-encoded field map, sign an already signed body, add fields after signing, or reuse a signature from the malformed form.
Offline tests must observe the signing **input and key**, not merely assert a signature-looking substring. A recording signer seam is sufficient for this caller-order contract; it must not be reported as testing the real cryptographic algorithm. The scratch evidence additionally used the actual extracted signing method, but the shared signer is not being changed.
No new network/response/retry path is introduced: errors and credentials continue through current dispatch and caller behavior. `Helpers/Utils.cs:45–48` sends POST directly to `HttpHelper.Post`; `need_cookie` metadata alone is not proof of outgoing WinRT cookies. Do not “fix” POST authentication by flipping this flag or changing global cookie merge semantics.
The correct form does not certify endpoint liveness, session validity, folder persistence or dialog behavior. Do not fabricate a device BR; update the existing U9 source-defect disposition only when implementation evidence exists.

## 5. Minimal expected ownership

| Owner role | Expected files | Boundary |
| --- | --- | --- |
| One API executor | `src/BiliLite.UWP/Api/User/FavoriteAPI.cs` | CreateFavorite/EditFavorite construction only; no shared helper or other builder changes. |
| Same executor | `plan/tests/c2/C2Probe.csproj`, `Program.cs`, `C2Stubs.cs` | Link real FavoriteAPI, add request regressions/minimal recording helper/settings/key seams; preserve original assertions and transport queue behavior. |
| Assigned single documentation owner | `plan/PLAN.md`, `docs/ROADMAP.md`; validation row if needed | Correct source-defect status and record remaining Windows/unverified platform gates; no claim of new device acceptance. |
| Executor/reviewer, assigned later | Stream-local `execution.md` / `final-review.md` | Exact source SHA, commands, pre/post results, diff scope, commit/remote SHA and blocker classification. |

**Why c2:** it already owns favorite-resource parsing and playlist/service/paging behavior (`c2/Program.cs:543–574`), defines compatible ApiModel/RestSharp stubs (`C2Stubs.cs:6–36`), and passed 162 baseline assertions in the verification snapshot. Its current compile list does not include FavoriteAPI, nor does its stub file implement ApiHelper/SettingHelper. This is a concrete coverage extension, not a new probe batch.
No UWP csproj registration, new dependency, production helper class or runner rewrite is needed. Add the source link to c2 and minimal seams required to compile the complete builder. PB1 may alter shared ApiEnvelope consumed by c2; use the current assigned commit, preserve its accepted behavior, and do not silently copy an older parser into the probe.

## 6. Meaningful real-source regression acceptance

The **real FavoriteAPI.cs** must execute. Never copy its body expressions into test stubs or prove the fix using only source-text searches. Existing ApiModel stub gains only request fields if actually needed; existing medialist fake transport remains intact and never sends new requests.
Provide a deterministic common-parameter seam with both ordinary logged-in and logged-out forms ending in `ts=1700000000` **without a trailing ampersand**, matching the actual helper boundary. Record that MustParameter receives AndroidKey and true. The seam must not quietly insert the missing separator itself.
Provide a GetSign recorder that captures every input/key, returns a deterministic `&sign=<dummy>` suffix, and is reset between cases. It must not rebuild or normalize a malformed request. Assert one call per builder and that the returned final body equals the captured complete unsigned body plus the suffix.
Parse the result as form pairs with multiplicity retained and values decoded exactly once. Do not hide duplicate keys using overwriting dictionary assignment. Check field count/set and exact values, independent of field discovery order.

| Case | Required assertion |
| --- | --- |
| Create/edit × isOpen false/true × common forms without/with access key (8 cases) | Correct POST/base path; one exact `ts`, one `privacy` with correct mapping; exact common + operation + signature field set and values. No credential invented/dropped. |
| Reserved/Unicode title and intro, e.g. `收藏 + &= %25 👋`, newline and `?` | One decode reproduces original text exactly; delimiters cannot inject fields; no double escaping or trimming. |
| Empty intro; empty title preservation case | Explicit empty field retained; no new builder validation or malformed separators. |
| Edit high decimal ID and ID with leading zeroes | `media_id` exact, separate; create has none. |
| Configured API base | Same configured host/prefix and exact existing add/edit route; no hardcoded host fallback. |
| Signing observation, each operation/privacy/common-form case | Captured input equals final body before its single sign suffix; already contains separate ts/privacy and every encoded feature field, including edit ID; supplied key is AndroidKey. |
| Existing c2 behavior | All prior favorite/watch-later parse, typed failure, paging, source retry, cursor and single-flight assertions execute and pass unchanged. |

Before changing product code, add the permanent assertions and show a behavioral **red** caused by contaminated timestamp/missing privacy against the real old builder, after test compilation succeeds. A stub compilation failure is not red evidence.
After the local two-boundary fix, run the same assertions unchanged and show **green**. The earlier scratch red is useful corroboration; permanent owning-batch red/green at the executor's exact SHA is still required. Do not modify expectations to obtain green.

## 7. Commands, platform limits and completion

Use the declared Bash interpreter: this host's `/bin/sh` is dash and fails on `BASH_SOURCE`. The quality stream's SDK is already available; no dependency installation is required for the builder test extension.

```bash
UPSTREAM_UNIT_TOOLS=/workspace/work/maintenance-foundation/quality bash plan/tests/c2/run.sh
git diff --check
```

Use the verification artifact's CLI/package-cache environment, or a current byte-identical scratch snapshot if the executor must avoid shared ignored build outputs. Record the source SHA and source integrity; never test an outdated snapshot after the product changes. Baseline c2 was 162 PASS at the input SHA; new count is not prescribed, but every required case and every original assertion must run.
Windows compile gate: `powershell -NoProfile -ExecutionPolicy Bypass -File ./build-release-x64.ps1 -Fast -NoVersionBump -Restore`. Current Linux has no PowerShell/VS/UWP/Windows SDK: **FAILED/BLOCKED — environment**, not passed by net8 probes. No manifest bump/package publication is part of this slice.
Optional later Windows transport inspection must use dummy data and a controlled request receiver without forwarding to Bilibili; inspect form content type/body and existing credential policy. **Do not create/edit live folders as validation.** The offline caller test does not certify outgoing platform cookies or real service persistence.

## 8. Status and review risks

- Requirements: READY FOR REVIEW. Execution: NOT STARTED / gated on PASS and ownership release. Source defect: CONFIRMED AND REPRODUCED, still unfixed at the input SHA.
- Existing whole-suite baseline is FAILED due to separately recorded harness defects; U9 success must not relabel it green. Windows compile/runtime remain environment blocked; live mutation verification is excluded by scope.
- Main risks: reversing public/private because of misleading comments; contaminating common helper output; escaping text twice; truncating IDs; signing before the corrected fields; using a fake helper that repairs the defect. The acceptance matrix targets each.
- Independent final review must confirm only the two owned product boundaries changed, existing assertions remained, real-source red/green evidence matches the final SHA, and all unresolved gates are explicitly carried. No release/master/deployment action is authorized.
