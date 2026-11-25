# AS3 amendment re-review evidence

Independent reviewer `api_requirements_review`, 2026-11-25. Supports [as3-review.md](as3-review.md). Scope is solely the P1 empty/fallback precedence correction; initial source selection, AS1/AS2 and initial source counterexamples remain accepted under [review-evidence.md](review-evidence.md). No old suite, owning batch, original fixture or clone metadata query was rerun for this Markdown amendment.

## Reviewed correction and actual source

Read ACTIVE re-review/revision dispatches, root's conservative decision, amended requirements/evidence/matrix and contracts/as3-amendment. Reviewed all sixteen source × valid flag × decoded-row table rows, each with first/append outcomes, against the complete current production `FavoriteDetailVM.LoadFavoriteInfo` (`src/BiliLite.UWP/Modules/Detail/FavoriteDetailVM.cs:118–183`), FavoriteAPI168–188, typed nullable FavoriteResourceListDto, strict FavoriteResourceListParser and null-row-filtering mapper.

Actual precedence and review result:

| Boundary | Required amended outcome | Independent check |
| --- | --- | --- |
| Normal first nonempty, bool true/false | Publish info/replace rows; ON/OFF respectively | Existing desired-safe AS3 first-page cases remain unchanged; new table explicit. |
| Normal first nonempty, absent/null | Publish info/replace rows; exact COUNT | Existing source common count branch; new requirement does not alter it. |
| Both sources/all valid flags, first empty with info | Existing FIRST-EMPTY before count decision | Actual VM147–158 publishes info then returns before Videos assignment; all eight flag/source controls pass with old Videos reference retained. |
| Both sources, first missing info | Reject publication; retain prior rows/info/Page1, hide Load more, Nothing=false, Loading=false, notice | Actual VM139–145; both source controls pass. |
| Normal append, bool true with rows | Append rows, retain accepted info; ON | Existing current-source true/count-equal desired-safe case remains required; table defines flag priority without count override. |
| Normal append, bool false with any rows; bool true empty | Retain/append accepted rows and info, OFF/Page unchanged | Original nonempty-terminal red remains; both new true/false empty desired-safe controls fail actual old source precisely on paging outcome. |
| Normal absent/null append, empty or not | Exact `Videos.Count != FavoriteInfo.media_count` after append | Both equal/unequal-count controls pass; empty prior1/count42 remains true/Page3, not terminal. |
| Type21 append, any valid flag, empty or not | Exact same COUNT; no synthetic flag authority | All four flags × empty/nonempty × equality cases pass unchanged. |
| Failed/malformed append | Preserve accepted rows/info/Page; current categorized notice and completion state; no paging decision | Source parser returns before mapping/publication. Prior reviewer actual-source transport503/rejected/missing-data/string-bool controls remain frozen and green. Final requirements retain explicit same-Page retry/progression controls. |

N is decoded new rows after null-row filtering, not raw array length. Absent/null remain nullable; malformed present bool remains typed Payload even for Type21, as today. Append always ignores incoming info; it does not replace last accepted totals or owner fields. First empty does not clear a preexisting Videos reference, matching the current method. No broad empty early return is allowed before the preserved COUNT append branches. This directly closes the first review's contradictory promises without changing either original flag-defect acceptance or the root-selected compatibility behavior.

## Hash and preservation checks

Private re-review directory: `/workspace/work/api-shape-comparison/requirements-review/as3-rereview/`.

`input-hash-check.json` records **38/38 exact matches** against the author's `contracts/as3-boundary-manifest.json`: five initial files,31 supplemental inputs and two result logs. Independently rechecked the original fourteen source/fixture entries against the earlier review's own `source-hash-check.json`, all36 complete production/stub Compile input records against earlier `linked-source-manifest.json`, and all twelve baseline source snapshots. Every comparison matched.

The two new Programs were copied byte-identically into exclusive reviewer scratch with their project files. Complete production Compile references remain linked to real current main sources; the complete previously verified `HttpResults.actual.cs` is read from its released frozen path. No author output/bin/obj/log was touched. Expected Program hashes:

| Program | SHA-256 |
| --- | --- |
| as3-boundary-controls | `5fb14709db756735f06d2c7361d4fbc72bf0211ace818a14397ad2bd7e71f037` |
| as3-present-empty-repro | `b813ecda508bb58251157eeaa0597e5734c490fbcbc5f48791e492149d291856` |

Original Programs remain `c0e4afcf0346c18ff17860edd0649b02ec3dad180ed2c283fd19593cfcf19405` (query) and `1a76dffdafe67a074209293e8413ed004d545636ae5f4cd5326fd21eecf72169` (favorite). Original author baseline logs currently match their frozen manifest hashes `af8623e53bcac0479f2894c99c6baa369d93cf0966507ded448b64b4de1d7153` and `4e4970b48840c0a51ee532abd82126a3377cf843f1c006d1c6b39197e4eacb7f`. Their exact assertion-outcome sequences match the initial independent reviewer replay logs: query25 checks/15 failures and favorite9 checks/6 failures. Comparison ignores only build-warning lines with different scratch paths; it does not change assertions or outcomes. Private `initial-log-preservation.json` records both author/reviewer log hashes and equality. No original proof was rerun or relabeled green.

Initial review.md/review-evidence.md and all three initial reviewer result logs were hashed before this re-review and checked unchanged after. The original current-source fixture and baseline evidence therefore remain separate from these additional fixtures and the final future implementation proof.

## Supplemental independent replay

Reviewed the fixture sources before executing them. The boundary Program selects only unchanged normal absent/null and Type21 count paths for append controls, plus both sources' all-flag first-empty and missing-info cases. Counts:24 append cases ×4 checks =96, eight first-empty cases ×3 =24, two missing-info cases ×2 =4; total124. The separate desired-safe Program selects only normal present true/false empty append, four checks each, so the new behavior cannot be hidden among compatibility controls.

The fixtures execute full production FavoriteDetailVM→FavoriteApi→transport recorder→FavoriteResourceListParser→mapper→publication. The existing c2 ApiHelper base-url primitive is configured to `https://api.bilibili.com`; no network dispatch is used and no production paging/parser algorithm is replaced. This resolves the author's documented initial unseeded-host harness error by configuring the primitive seam; it does not weaken route assertions. The actual keyword still passes the real FavoriteApi encoder (`A+B&literal%2B`→`A%2BB%26literal%252B`).

From exclusive re-review scratch, used existing SDK8.0.427/runtime8.0.33 and earlier reviewer-local cached Newtonsoft13.0.3:

```bash
export DOTNET_ROOT=/workspace/work/maintenance-foundation/quality/dotnet
export DOTNET_CLI_HOME=/workspace/work/api-shape-comparison/requirements-review/as3-rereview/cli-home
export NUGET_PACKAGES=/workspace/work/api-shape-comparison/requirements-review/nuget
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
"$DOTNET_ROOT/dotnet" restore as3-boundary-controls/BoundaryControls.csproj --source /workspace/work/api-shape-comparison/requirements-review/as3-rereview/empty-source
"$DOTNET_ROOT/dotnet" run --project as3-boundary-controls/BoundaryControls.csproj --no-restore
"$DOTNET_ROOT/dotnet" restore as3-present-empty-repro/PresentEmpty.csproj --source /workspace/work/api-shape-comparison/requirements-review/as3-rereview/empty-source
"$DOTNET_ROOT/dotnet" run --project as3-present-empty-repro/PresentEmpty.csproj --no-restore
```

| Independent output | Result |
| --- | --- |
| `as3-boundary-controls/independent-result.log` | Exit0: `unchanged-green controls: passed=124 failed=0` |
| `as3-present-empty-repro/independent-result.log` | Exit1: `present-empty desired-safe: passed=6 failed=2`. Each old-source normal present-bool empty append returns ShowLoadMore=true/Page3 instead of required false/Page2. Row/info identity, exact endpoint/pn/encoded keyword and successful completion controls all PASS. |

No repaired-source run or implementation PASS is claimed. Later executor must retain all original six AS3 failures, both supplemental desired-safe failures and all124 compatibility assertions unchanged against original and repaired source, plus the requirement's full final table/error/retry/ID/optional-payload/action controls and existing c2/c3 assertions. AS1/AS2 final real-source red→green and all shared regressions remain required. Fixed51logical/52executable aggregate remains the final integrated gate; no new batch or denominator is authorized.

## Custody

All supplemental manifest entries and original reviewer history/source bindings are unchanged at finish. No source/test/tooling/main-doc/index/ref or frozen cache mutation occurred. Sole new repository outputs: `as3-review.md`, `as3-review-evidence.md`. Initial CHANGES artifacts stay intact. No repeated clone metadata queries, foreign execution, Bilibili calls or whole-suite run occurred.

All amendment/artifact/source/fixture read custody is **RELEASED**; reviewer private subdirectory is frozen. Revised requirements PASS is not an immediate executor/code/index/ref/publication grant. Memory safe integration and root baseline/exact file ownership grants remain first. Account/live/proto holds and native/session/live-service verification limits remain unchanged.
