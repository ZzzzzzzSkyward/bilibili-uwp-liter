# FD1 independent implementation review — PASS

Reviewer `dynamic_offset_review`, 2026-11-25 UTC. **PASS for the frozen FD1 implementation and its owning gate; selected validation remains PARTIAL_PASS.** No P0/P1/P2 finding requires a change. Reviewed baseline A `7fc3c524fde4139d01e7ecd362647ca771c64ea8`, the exact accepted follow-up requirements/FD1 review, current execution grant, released implementation and immutable original evidence. The fixed deadline remains 08:29:30 UTC / 16:29:30 Asia/Shanghai.

The complete independent binding is [review-evidence/binding.json](review-evidence/binding.json), SHA256 `4b6c4611e1aef1244720ec718b848c1c435024c13b46bbc6fa4d2bca9ceb33a6`. It records actual commands, private cwd/environment, input/tool/source hashes, log hashes, exits and receipts. Private scratch is `/workspace/work/index-dynamic-contracts/dynamic-review/`.

## Source and maintained fixture findings

The sole product edit applies `System.Uri.EscapeDataString(offset)` exactly once at the existing nonnull offset value interpolation. It retains null omission, empty inclusion, the literal host/route, GET/cookies, page/type/timezone/features and the unsigned web family. It neither interprets offsets numerically nor changes any parser, transport, signer or cursor owner. The API layer still owns request construction; the new check class remains in the existing API02 batch. No dead code, orphan file or responsibility duplication was introduced within this reviewed surface.

The exact inverse diff, privately preserved at `/workspace/work/index-dynamic-contracts/dynamic-review/evidence-hygiene/raw/source-diff.patch` (hash bound in the review metadata), independently proves that removing only the additive Program invocation/project link and reversing only the encoding expression restores their baseline byte hashes. The new fixture is additive and the project links the complete real builder. API01Stubs is byte-identical and contains no offset encoding/query decoding policy; the real `ApiHelper.ApiModel.url` still concatenates baseUrl, `?` and parameter. The original 12 API02, 1,258 search and 3,831 query assertions and their source inputs remain unchanged.

All 18 offset cases run for all/video/article through the actual builder, actual `ApiModel.url`, `new Uri(...)` and one standard query decode. The 270 checks assert an exact sole offset, unique unchanged controls, the fixed field set, cookie GET route and no fragment/signatures. Cases include null/empty, numeric text, plus, literal percent escapes, ampersand/duplicate-field injection, fragment, Unicode, quote/backslash, CRLF and tab/NUL/control text. The literal `%2B` round-trips as `%2B`; it is not decoded into a plus early. These synthetic URI-boundary results make no service acceptance or live offset-distribution claim.

## Fresh independent replay

| Exact private replay | Observed result |
| --- | --- |
| Same final maintained fixture + immutable old-A builder | Original 5,101 assertions pass; new 270 checks have 30 failures. Exit **134**, with the actual unhandled fixture assertion exception. Expected old-source red, not a passed gate. |
| Same final maintained fixture + candidate FD1 builder | **12 + 1,258 + 3,831 + 270 = 5,371 PASS**, exit **0**. |
| Byte-identical original118 fixture + immutable old-A closure | **118 checks / 29 failures**, exit **1**: FI1 initial2/owner3, FD1 offset18/control6. |
| Byte-identical original118 fixture + candidate FD1 only | **113 PASS / 5 foreign FI1 failures**, exit **1**: FI1 initial2/owner3 only. |

Full actual logs and the two original118 receipts remain private under `/workspace/work/index-dynamic-contracts/dynamic-review/`, with verified preservation copies in `evidence-hygiene/raw/`. [review-evidence/](review-evidence/) contains concise command/hash/result and receipt-reference metadata only. Assertion-by-assertion receipt comparison verifies **all 89 original positives retained and all 24 FD1 old failures closed**. Both independently generated original118 receipts equal the frozen executor receipts. The five FI1 failures remain unimplemented foreign scope in this isolated replay; none is passed or waived.

Original118 Program, Stubs, project, complete actual HttpResults extraction, global.json and all complete product copies are unchanged except the candidate builder. Both original118 closures use immutable SeasonIndexVM SHA256 `e7eef5dd3c69852f9b17c676122c4c1bec19eae729e49fb2e4c2ad02b19e14c3`. No moving FI1 source was read or copied. Actual parser checks retain typed envelope/items/boolean failures and exact large numeric scalar text; the real legacy encoded/signed request remains an active positive control.

## Preservation, custody and remaining gates

Before/after bindings retain the four released candidate hashes below, 43 unchanged contract/test inputs, six exact tool/reference binaries, 206 protected product byte inputs and 25 main-document inputs. Original discovery artifacts and G4 project/fixture/reference/source manifest/report remain at their historical hashes; executor initial metadata and raw proofs retain their exact historical bytes at the corrected history/private locations bound here; the 16 supplemental artifacts also match their frozen binding. Parser/mapper/Home/User VM/source-bound cursor, helper, signer/auth/cookie and protected asset/proto/manifest/identity/served-JSON bytes were not changed. G4's **179 PASS** is reused solely through its exact unchanged input/tool/report binding; it was not rerun or represented as fresh union/full-suite validation.

| Released candidate path | SHA256 |
| --- | --- |
| `src/BiliLite.UWP/Api/User/WebDynamicFeedApi.cs` | `e10feceedf3369e1ba11b8ba353805cdd9212db400a46f5300e3ddaa8d8b91c9` |
| `plan/tests/api02/API02.csproj` | `ab8237d1d135c1b3d35e3c4ce1c8342c8f5f3163f31ac64dc16e10cd163db41a` |
| `plan/tests/api02/Program.cs` | `e5d50a678cd5f4ef8474b5275644b1aabcace3eabaeecfbe5083dc84005657b4` |
| `plan/tests/api02/DynamicFeedRequestChecks.cs` | `5f8df3e270dedb59a50afb18bae44ecf9a02128c50c9af877d6fa4d4aee88f4d` |

All builds used private source trees and private CLI/NuGet/build state with SDK8.0.427/runtime8.0.33 and cached Newtonsoft13.0.3. No source/test/main-document/Git/ref/index/shared-project/full-suite operation occurred. This review does not establish whole-tree stability while FI1 moves. Fresh frozen-union118/118, supplement34/34, the complete51/52 suite and native/Windows/UWP/XAML/device/account/live/hosted gates remain separate.

**Frozen and released:** this final verdict, review evidence and private scratch, plus all read custody over the four candidate files and old evidence. No reviewer writer remains active. FD1 is ready for the separately granted integration/publication process; this review grants no source or Git mutation.


## Metadata-only evidence relocation and rebind

The exact first PASS report, binding and release are preserved under [review-history/](review-history/). Their historical raw repository links are explicitly superseded. Four raw logs and the source diff were removed from the repository only after private byte-identity verification; full receipts were preserved privately and their repository JSON replaced with concise path/hash/count summaries. [review-evidence/relocation.json](review-evidence/relocation.json) records every artifact.

Before this replacement release, the executor froze its separately corrected current evidence. Its current metadata/release hashes are bound here. Independent comparison confirms identical four code/test hashes, source/fixture/tool bindings, original commands/exits/counts, full raw log/receipt hashes and substantive execution assessment. Only status/storage/path metadata changed. The first independent 5,371 PASS and original118 113 PASS/5 foreign FI1 results retain exactly the same proof identity and acceptance; no test or build reran. Current review outputs are Markdown/JSON metadata only. All current/historical review artifacts and read/write custody are again frozen/released.
