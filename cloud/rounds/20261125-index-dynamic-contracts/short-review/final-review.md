# SR1 independent implementation review — PASS

Reviewer `dynamic_offset_review`, 2026-11-25 UTC. **PASS; FROZEN / RELEASED.** No P0/P1/P2 change is required. This is the owning SR1 implementation review and selected API02 **PARTIAL_PASS**. Fresh selected-union, complete-suite, source publication and native/hosted gates remain separate. Fixed deadline stays **08:29:30 UTC / 16:29:30 Asia/Shanghai**.

[Exact binding](implementation-review-evidence/binding.json), SHA256 `4de70ae4bd7d81f81a68aec3bde75c82ae53bf8fea51f2a522c2a9210e58d774`, records the approved requirements/current release, all source/fixture/tool/output hashes, actual command arguments/cwd/environment, counts and exits. Complete raw logs, source diff, snapshots and compiled outputs are private under `/workspace/work/index-dynamic-contracts/dynamic-review/short-review-amendment/`; repository outputs are Markdown/JSON metadata only.

## Source and fixture findings

The entire SeasonApi file differs from immutable old `54dabff0...` only at the approved ShortReview operand: `next` becomes `Uri.EscapeDataString(next ?? "")`. The signature/default remains unchanged; omitted, null and empty next still include one empty cursor. The accepted string is escaped once before completed query/URI construction. Every other method and both enums are byte-identical. Configured host, GET/no-cookie, null body/headers/ExtraCookies, media_id, ps20, sort/default0, optional access_key value and final append placement, and no-app/no-WBI policy remain exact. There is no new parser, VM, auth/token, helper, signing or transport policy.

Independent inverse-byte checks restore every pre-SR1 maintained input hash by removing only the additive real SeasonApi project link, additive Program invocation and accepted operand edit. Existing FD1 builder, link, invocation and 270 assertions remain exact, as do all 5,101 original API02 assertions and API01Stubs. Complete actual SeasonApi/ApiModel compile without shared stub or enum changes. The new request-check class is invoked by the existing batch, with no orphan/dead code or misplaced responsibility in the reviewed surface.

The new fixture exercises 19 cursor cases ×2 hosts ×2 login states ×3 media IDs ×3 sorts: **684 actual requests**, five final-URI checks each, plus four omitted-next/default-sort checks = **3,424**. It uses the complete real builder, actual ApiModel.url, `new Uri` and exactly one standard query decode. Checks preserve the single exact opaque cursor, unique controls, ordered field set, access-key placement, no fragment, route and signing/cookie policy. Cases cover numeric/leading-zero text, plus/literal percent, separators/duplicate fields, fragment, JSON-shaped text, Unicode and controls. Host/login/access fixture state is restored in `finally`; no encoding or query policy was copied into stubs.

## Fresh independent replay

| Private replay of the same final fixture | Observed result |
| --- | --- |
| Maintained API02 + immutable old SeasonApi | Prior **5,371 PASS**; new 3,424 have **486 failures**. Exit **134**, actual unhandled assertion exception; expected old-source red. |
| Maintained API02 + candidate SeasonApi | **8,795 PASS**, exit **0**. |
| Byte-identical original3446 + immutable old SeasonApi | **2,958 PASS / 488 desired-safe failures**, exit **1**; all **1,572 compatibility checks PASS**. |
| Byte-identical original3446 + candidate SeasonApi | **3,446 PASS / 0 failures**, exit **0**; all **1,572 compatibility checks PASS**. |

Old and candidate private3446 closures differ only in the whole actual SeasonApi file; the other eighteen inputs, including the original Program, remain exact. The full actual HttpResults class and actual ToInt32 extraction independently match current product bodies. Complete SeasonReviewVM/row models, helper/signers, RelayCommand and platform-delivery primitives execute. The response supplies Next; real LoadMoreCommand, failed append/retry and RefreshCommand reach the actual builder/final URI. No private Next or lifecycle flag is assigned and no miniature VM/parser/request policy substitutes for production logic. The old fixture's 488 exact failed labels match frozen executor evidence: 486 matrix failures and two actual VM append/retry failures. Existing total/terminal, rows/content decoding, retained cursor, retry and refresh behavior stays unchanged.

The maintained fixture's **3,424** and original fixture's **3,446** are intentionally distinct: each has the same 3,420-check request matrix, while the former adds four default controls and the latter adds 26 default/actual-VM controls. No result was relabeled or assertion weakened.

## Preservation and release

Before/after checks retain **417 named prior FD1 execution/review/history, original118/supplement34/G4179 and original SR1 discovery/source/proof inputs**, plus **272 relevant protected/contract/main-document input hashes**. The current **11-path selected union**, including final index source `34536344...`, SeasonIndexProbe `0a74f449...`, FD1 builder `e10fecee...` and all current season1 tests, remains exactly bound. These scoped checks do not establish a new integrated or whole-tree gate. Historical118/34/G4179 were preserved and not rerun for this review. Six exact SDK8.0.427/runtime8.0.33/Newtonsoft13.0.3 tool/reference hashes were reused; all builds/cache/CLI state were private.

| Frozen SR1 file | SHA256 |
| --- | --- |
| `src/BiliLite.UWP/Api/SeasonApi.cs` | `7c0695b00d529a79dc5f723127d174454c1d179942d76b8e83fc2503e34500a1` |
| `plan/tests/api02/API02.csproj` | `d93bf6c6afa8a9ab0abebc748e3be60259d0c205d74e113e6d7624d10dd6e39b` |
| `plan/tests/api02/Program.cs` | `415f78d0a816189ee3217f3aacc85cf5bb6e61d8f2a70cfb41f760e112e4a12c` |
| `plan/tests/api02/SeasonShortReviewRequestChecks.cs` | `4d262da48bf0b376ef81a3bb13805945f7b3463f75f81d02aa21b8361e23ad21` |

No repository source/test/main-document, Git/index/ref, shared-suite/project, dependency, external cache, account/auth/settings or live-service mutation occurred. Synthetic request evidence does not establish server acceptance/liveness or production cursor frequency. Windows/UWP/XAML/build/startup/device/account/live and pushed-SHA hosted results remain unverified here.

All review commands ended. The four SR1 candidate files, current11-path read custody, prior evidence, this verdict, review metadata and private scratch are **frozen and released**. No reviewer writer remains active. [Release metadata](implementation-review-evidence/release.json) binds the final report and evidence for the separately granted fresh/full/publication gates.
