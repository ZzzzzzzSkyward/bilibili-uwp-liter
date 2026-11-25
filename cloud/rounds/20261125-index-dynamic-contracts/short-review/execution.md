# SR1 — implementation frozen and released

Owner `dynamic_offset_executor`; **READY FOR INDEPENDENT OWNING/FRESH REVIEW**. Selected API02 result remains **PARTIAL_PASS**. Baseline A `7fc3c524fde4139d01e7ecd362647ca771c64ea8`; root's exact SR1 rebind was verified before writing. Fixed deadline remains **2026-11-25 08:29:30 UTC / 16:29:30 Asia/Shanghai**. All four writable source/test files and new evidence are frozen/released; no writer remains active under this grant.

The sole production change is the interpolation operand in `SeasonApi.ShortReview`: `next` becomes `Uri.EscapeDataString(next ?? "")`. The existing default, explicit null and empty still include one empty cursor. Encoding happens once before complete query/URI construction. The complete rest of SeasonApi and its enums remain byte-identical. Configured host, GET/no-cookie, ps20/media/sort/defaults, null body/headers/ExtraCookies, optional access_key value and final append position, and unsigned app/WBI policy stay unchanged. No VM/parser/response/total/error/retry/refresh, auth/token/cookie, global helper or other API method was changed.

The existing API02 project gains only a complete actual SeasonApi link; Program gains only one additive invocation. New `SeasonShortReviewRequestChecks.cs` uses actual SeasonApi → real ApiModel.url → `new Uri` → one standard query decode. Its 19 cursor cases ×2 hosts ×2 login states ×3 media IDs ×3 sorts produce684 requests with five checks each, plus four omitted-next/default-sort controls: **3424 new assertions**. It covers null/empty/default, ordinary/large/leading-zero numeric text, plus/literal-percent, duplicate cursor/control attempts, fragments, JSON text, Unicode, quotes/backslash and CRLF/tab/NUL/control text. Checks retain sole exact cursor, unique controls, exact ordered field set, optional access-key placement, route and no-cookie/no-app/no-WBI policy. It restores its host/login/access fixture state in finally. Existing API01Stubs and enum/type surfaces require no change.

## Exact results and preservation

| Private isolated replay | Observed result |
| --- | --- |
| Same final maintained fixture + old SeasonApi54dabff0 | Existing12+1258+3831+270 = **5371 PASS**; new3424 checks have **486 failures**. Exit134 is the actual unhandled fixture assertion exception: expected old-source red, not a passed gate. |
| Same final maintained fixture + candidate SeasonApi | **8795/8795 PASS**, exit0: all5371 previous API02/FD1 assertions plus3424 new SR1 assertions. |
| Byte-identical original3446 fixture/closure + old SeasonApi54dabff0 |3446 checks, **2958 PASS /488 desired-safe failures**, exit1; **1572 compatibility positives PASS**. |
| Byte-identical original3446 fixture/closure + candidate SeasonApi only |**3446/3446 PASS**, exit0; all488 old failures close, all1572 positives remain green. |

The unchanged original3446 fixture executes complete actual SeasonReviewVM, row models, RelayCommands, full HttpResults and ToInt32 extraction, real SeasonApi and ApiModel. Its successful candidate includes both formerly failing response→actual VM→LoadMoreCommand/retry→builder→final URI assertions. No Next/private lifecycle assignment or copied cursor/VM policy was added. Source delivery and platform seams are unchanged.

Exact commands, environments/cwd, exit codes, counts, complete compiled input/tool/output hashes and excluded raw output paths are in [implementation-evidence/binding.json](implementation-evidence/binding.json), SHA256 `d728fe48570ab1273f63a931b839f1045021cc47b99451da370c1bbf21e07718`. Raw logs, source snapshots, inverse diff, fixtures and compiled outputs remain only under `/workspace/work/index-dynamic-contracts/dynamic/short-review-amendment/`; repository evidence is compact Markdown/JSON metadata only. Existing exact SDK8.0.427/runtime8.0.33/Newtonsoft13.0.3 were reused, with private CLI/build state and the existing owner-private NuGet cache. Only owning API02 and this private fixture compiled; no package upgrade/shared other-batch/full-suite run occurred.

Private inverse checks prove that removing only the additive link and call, and reversing only the accepted interpolation operand, restores every pre-SR1 maintained input byte. FD1's complete builder and270-check fixture remain exact, as do all5101 original API02 checks and the shared stubs/helper. The old/candidate maintained closures differ only in SeasonApi; the original3446 Program and every other original compiled input are byte-identical across old and candidate.

All **417 captured prior FD1 execution/review/evidence, original118/supplement34/G4179 and original SR1 discovery/source/fixture/proof inputs** remain unchanged. They were not rerun or rewritten. Current selected-union hashes are provided for root's separate rebind; current frozen index source still matches the grant. This is scoped input preservation, not a global stable-tree claim while other roles manage metadata. Fresh integrated118/118,34/34,G4179 and complete51logical/52executable validation remain separate gates.

## Frozen four-file release

| Path | SHA256 |
| --- | --- |
| `src/BiliLite.UWP/Api/SeasonApi.cs` | `7c0695b00d529a79dc5f723127d174454c1d179942d76b8e83fc2503e34500a1` |
| `plan/tests/api02/API02.csproj` | `d93bf6c6afa8a9ab0abebc748e3be60259d0c205d74e113e6d7624d10dd6e39b` |
| `plan/tests/api02/Program.cs` | `415f78d0a816189ee3217f3aacc85cf5bb6e61d8f2a70cfb41f760e112e4a12c` |
| `plan/tests/api02/SeasonShortReviewRequestChecks.cs` | `4d262da48bf0b376ef81a3bb13805945f7b3463f75f81d02aa21b8361e23ad21` |

No implementation blocker was found. No Git/index/ref/fetch/pull/stage/commit/push/main-document operation occurred. No server liveness/acceptance, production cursor distribution, native Windows/UWP/XAML/build/startup/device/account/live or hosted CI result is claimed. Independent review, selected-union/final gate and source/docs/archive publication require their separate root grants. [Replacement release manifest](implementation-evidence/release.json) binds this execution report and final evidence.
