# FD1 — implementation frozen and released

Status: **IMPLEMENTATION FROZEN; CURRENT EVIDENCE RELEASED FOR REVIEW METADATA REBIND**, selected API02 **PARTIAL_PASS**. Owner `dynamic_offset_executor`. Baseline `7fc3c524fde4139d01e7ecd362647ca771c64ea8`; latest fixed deadline remains 2026-11-25 08:29:30 UTC. This owner has stopped writing and releases all four owned product/test files, this artifact, evidence and private scratch for independent review/integration.

The sole product change is `System.Uri.EscapeDataString(offset)` at the existing nonnull offset interpolation in `src/BiliLite.UWP/Api/User/WebDynamicFeedApi.cs`. Null still omits offset and empty still emits one empty offset. The literal host, route, GET, cookie mode, type/page/timezone/features and absence of app/WBI signing retain their exact existing behavior. No parser, mapper, cursor owner, signer, shared stub, API policy or foreign product file changed.

The maintained API02 project links that actual product builder; `Program.cs` gains one additive invocation. New `DynamicFeedRequestChecks.cs` contributes 270 assertions across all/video/article and 18 offset cases: null, empty, plain/numeric text, plus, literal percent escapes, ampersand/duplicate-field injection, fragment, Unicode, quotes/backslash, CRLF and tab/NUL/control text. Every case constructs the real `ApiModel.url` URI, decodes its query once and verifies exact offset, unique unchanged control fields, fixed field set, cookie GET and no fragment. API01Stubs and the real ApiHelper/ApiModel remain unchanged; no query policy was copied into a facade.

## Exact evidence and results

Private scratch: `/workspace/work/index-dynamic-contracts/dynamic/`. Complete source/fixture/tool hashes, actual command arguments/environment/cwd/exits and immutable-source comparison are in [evidence/binding.json](evidence/binding.json), SHA256 `000fe1f247106d397e57c7e13d4dca8e32e40b0d231e339fd4eff9bebf453a40`. Full logs and generated original118 receipts remain in excluded private scratch at the exact paths/hashes bound there; adjacent receipt JSON files are compact custody/count summaries. [relocation.json](evidence/relocation.json) records byte verification and relocation. Exact initial execution/binding metadata is preserved under [evidence-history/initial-executor](evidence-history/initial-executor/), explicitly marked superseded for raw-repository paths. All tests compile isolated private trees and use existing SDK8.0.427/runtime8.0.33, unchanged cached Newtonsoft13.0.3 and private CLI/NuGet cache state; no shared obj/bin or other project/suite ran.

| Replay | Result |
| --- | --- |
| Same final maintained fixture + old-A builder | Existing12 original,1258 search,3831 query assertions all pass; new270 has30 failures. Exit134 is the fixture's unhandled assertion exception; this is expected old-source red, not success. |
| Same final maintained fixture + candidate builder | All12+1258+3831+270 = **5371 assertions PASS**, exit0. |
| Byte-identical original118 fixture + immutable old-A closure |118 checks,29 failures: FI1 initial2/owner3, FD1 offset18/control6; exit1. |
| Byte-identical original118 fixture + candidate FD1 builder only |118 checks,**113 PASS/5 foreign FI1 failures**, exit1. All89 former positives stay green and all24 FD1 desired-safe failures become green. |

Original118 Program/Stubs/project/complete HttpResults and all other compiled product sources remain byte-identical; only the candidate owned builder differs. Its SeasonIndexVM is immutable old-A SHA256 `e7eef5dd3c69852f9b17c676122c4c1bec19eae729e49fb2e4c2ad02b19e14c3`, never FI1's moving source. The five FI1 failures are **unimplemented foreign scope**, not passed or waived. The fixture actively retains typed envelope/items/boolean failures, exact large numeric scalar cursor text, legacy encoded/signed offset and parser positives. Fresh integrated118/118 and34/34 await the separate frozen-union review gate.

All original private discovery artifacts, including the original118 fixture/log/receipt and G4 fixture/project/source manifest/log, still match their historical bound hashes. Existing G4's179 input bytes and current owning source inputs remain unchanged; this owner did not rerun G4. Its prior179PASS is historical compatibility evidence, not a new full/union gate. Unchanged parser/mapper/VM/source-bound cursor inputs are bound for later compatibility replay.

Preservation checks prove removing only the additive Program call/project link restores the exact original source bytes, and reversing only the encoding expression restores the original product bytes. All other original maintained project/test/product inputs remain exact. No old assertion or fixture was weakened, no dependency/runner/inventory changed, and no Git/index/ref/fetch/pull/stage/commit/push operation occurred.

## Frozen ownership and limits

| Owned file | SHA256 |
| --- | --- |
| `src/BiliLite.UWP/Api/User/WebDynamicFeedApi.cs` | `e10feceedf3369e1ba11b8ba353805cdd9212db400a46f5300e3ddaa8d8b91c9` |
| `plan/tests/api02/API02.csproj` | `ab8237d1d135c1b3d35e3c4ce1c8342c8f5f3163f31ac64dc16e10cd163db41a` |
| `plan/tests/api02/Program.cs` | `e5d50a678cd5f4ef8474b5275644b1aabcace3eabaeecfbe5083dc84005657b4` |
| `plan/tests/api02/DynamicFeedRequestChecks.cs` | `5f8df3e270dedb59a50afb18bae44ecf9a02128c50c9af877d6fa4d4aee88f4d` |

No new server acceptance or offset distribution is claimed. Complete-suite, native UWP/Windows build/XAML/startup/device/account/live and hosted CI gates remain unverified by this work. No current implementation blocker was found; independent review and separate integrated gates remain pending. Source publication belongs only to a later explicit owner grant.

## Metadata-only evidence packaging correction

The four owner-generated repository `.log` copies were removed only after checking byte identity against their excluded private originals. The two full generated receipt JSON files were compacted in place into private-path/hash/count summaries; their exact original bytes remain private and no assertion/result was changed. The first execution and binding metadata were preserved byte-for-byte before this revision; there was no first executor release.json. [evidence/release.json](evidence/release.json) freezes this replacement current evidence. Initial independent PASS/review metadata remains read-only and unchanged by this owner; its separate metadata rebind is pending. No code/test/fixture/tool change, test/build rerun, Git operation or acceptance relabeling occurred. All four frozen source/test hashes still match.
