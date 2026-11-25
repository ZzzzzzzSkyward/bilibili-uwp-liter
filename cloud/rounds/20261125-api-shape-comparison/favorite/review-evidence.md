# AS3 independent review evidence

Reviewer: `api_favorite_review`, 2026-11-25. Supports [final-review.md](final-review.md). ACTIVE authority is [implementation-review-dispatch.md](../implementation-review-dispatch.md); the writer is frozen/released. Other memory/query work is disjoint. This evidence binds only the stable favorite scope and its actual dependencies, never a moving whole-tree state.

## Exact released inputs and independent checks

Writer manifest `/workspace/work/api-shape-comparison/favorite-execution/release-manifest.json` hashes to the activation's exact `edf80766e5a29bca60d2c2e7c0143a284191ac8bec2166e7929134118b194125`. All nine entries match actual files, including untracked new fixture and writer evidence. Relevant hashes:

| Input | SHA-256 |
| --- | --- |
| Current FavoriteDetailVM.cs | `9e75a062fbee0bcfba9bff2a94aabd398894fdf21a1fe37307dd2028813875c1` |
| Original FavoriteDetailVM.cs snapshot | `2a0e667cd6d6bb066df581e7ae27a2277ab4c529c3a65db388940b8e06af7e1b` |
| Final FavoritePagingProbe.cs, both runs | `62a7a3bf7b5a21fc9fec76ec688d9babcad5e5105c5712034d56b117ef1bfc0c` |
| Final c2 Program.cs | `3528fca2da57400e7f5b5ba8f8496a745848b3e1493247bcbcfc23a7277a857f` |
| Original c2 Program.cs, recovered exactly without dispatch | `04b62fe038a3888a57b8828e4ab29a272f4511cf7027f6c3a219405adeed2d4b` |
| Writer receipt.json | `482d4ff982d32cd0739d485502479de71dd50e0365bd06181fef3e4e0e35529f` |
| Writer final-inputs.json / final-source-manifest.json | `899f5fccfa77581da38e449edd69f181ef5bc16b60c93ce0cbfb1505a5cba91f` |

Read AGENTS, CONTRACT, REPO, memory INDEX; accepted requirements/binding, AS3 amendment/final PASS/evidence, execution grant, scheduling review/adoption, writer execution, all receipts/manifests and complete final original-red/current logs. No Git startup/ref operation was appropriate under this frozen local review grant, which expressly preserves concurrent authorized changes.

Independently inspected full `FavoriteDetailVM.cs`, `FavoriteAPI.cs`, favorite DTO/parser/mapper, ApiEnvelope/ApiParseResult, MediaListFailureText/IModules, full HttpResults decoder, actual request dispatch/ApiModel/signing context, and all actual favorite callers. Navigation through FavoritePage, UserInfoPage and MessageCenter supplies Id/Type (default11 for folder links); FavoriteDetailPage sets Page1/empty Keyword and awaits the VM. Search/Refresh reset first-page state, LoadMore uses the current Page, the XAML binds the paging/loading/empty flags, and PlayAll awaits the same method while ShowLoadMore. These are static wiring observations, not native execution claims.

Private verifier: `/workspace/work/api-shape-comparison/favorite-review/verify-bindings.py`. Actual review command:

```bash
python3 /workspace/work/api-shape-comparison/favorite-review/verify-bindings.py
```

Final independent binding pass is exit0. `binding-check-before.json` and `binding-check-after.json` record the checks and parsed report outcomes; `scope-before.json`/`scope-after.json` bind88 exact stable inputs/artifacts. Checks include:

- All released entries, requirement/grant hashes, frozen original proofs, private snapshot/manifests and report/log hashes match; historical draft and final-v2 receipts remain preserved.
- The writer's47 before/after runnable/tool/package records match exactly across green. Final-inputs contains49 records: those47 plus the generated decoder and reporting Python wrapper. All49 current hashes match. The final fixture-old versus green manifests differ only in the VM and three permitted adjacent comment files.
- All40 initial repository input snapshots match their initial hashes; all six final own source/test snapshots match current bytes. Original project/stubs/run/ActionChecks and c3 files retain exact initial/final hashes. Removing one `await FavoritePagingProbe.Run();` line recovers original Program exactly.
- Independently resolved41 distinct c2/c3 Compile/source-text/script/project inputs; all are bound in final-inputs, including the generated decoder. Explicit Compile links use the full actual favorite VM/API/DTO/parser/mapper/IModules; there is no copied VM, pager or parser. C3's preexisting decoder stub and source-text wiring checks are accurately limited to compatibility coverage.
- The complete c2 generated HttpResults source exactly equals the current production class extracted with the unchanged c2/run.sh recipe, including salvage and legacy methods. The unchanged c2 delivery/platform/ApiHelper primitives record requests and canned responses; no paging or has_more algorithm is supplied by a stub. Signature/cookie fields are offline request-policy checks, not actual authentication acceptance.
- Every changed line in the three adjacent DTO/parser/mapper files is a comment line; noncomment content is unchanged. No parser tolerance change accompanies the new VM behavior.
- Each of the three frozen original Programs appears in the final fixture with its complete body unchanged after only namespace enclosure and Main→public Run adaptation; every original assertion/expected result remains.

The verifier's first private draft assumed final-source held all repository snapshots; it actually holds the six own final paths. A second draft checked generated decoder membership in the47 pre-run records instead of final49. Both were reviewer bookkeeping assumptions, corrected by inspecting the actual manifests; no tested source, assertion or author evidence changed. Draft artifacts remain private, separate from the final successful checks.

## Semantics checked against actual source and tests

The sixteen-row source × flag × decoded-row table is exercised at first/append and equal/unequal counts:64 scenarios ×8 checks =512 added assertions. Normal bool authority applies only after accepted mapping/publication, uses mapper-filtered row count and does not manufacture false for absent/null. Both sources' first-empty branches publish info/ownership/collect fields then retain the pre-call Videos reference/content and Page1. Missing/null first info rejects rows/ownership publication. Append ignores incoming info, preserves the accepted collection/row/info objects and appends decoded string IDs in order.

The72 added optional-row/ownership checks cover absent/null/all-null medias, missing/null first info and empty first-page self/collect outcomes. The468 typed-failure/retry checks cover both families and both first/append pages: null/HTTP transport, business, missing-code/JSON/data envelopes, payload data/media/info shapes and number/string/object/array malformed booleans. They assert failure category, unchanged accepted state/Page, existing exact notice/completion, actual source request and same-page retry publication. Another18 exception/progression assertions cover logged exceptions, same-page retry and subsequent terminal requests. This totals1070 added assertions, distinct from the three preserved original proof bodies.

The new VM expression implements precisely the accepted table: normal present true+N>0 → ON, present false or N=0 → OFF; normal absent/null and Type21 → exact COUNT. First-empty/missing-info and failure returns precede it. There is no unconditional append-empty return, `GetValueOrDefault`, count comparison change, new state/service or request change. The complete original action methods and all original assertions remain intact.

## Actual immutable test receipts reused

The owner ran these commands with SDK8.0.427, runtime/ref8.0.33 and existing cached Newtonsoft13.0.3, all exact tool/package hashes checked independently. Shared prefix:

```bash
bash plan/tests/run-all.sh --tools-root /workspace/work/maintenance-foundation/quality
```

| Arguments / private report | Actual result |
| --- | --- |
| `--only batch:c2 --output /workspace/work/api-shape-comparison/favorite-execution/original-red-v2` | Runner exit1; selected c2 exit134 after explicit final assertion exception. Original6 failures/9; compatibility124 PASS; present-empty6 PASS/2 failures; added1055 PASS/15 expected changed-behavior failures; unchanged actions563 PASS. No prerequisite errors; log not truncated. |
| `--only batch:c2 --only batch:c3 --output /workspace/work/api-shape-comparison/favorite-execution/repaired-green` | Runner exit0; both selected stages exit0. Original9/9, compatibility124/124, present-empty8/8, added1070/1070; actions563, original c2 Program451, c3 117 PASS. `PARTIAL_PASS`, `complete_required=false`, logical/executable pass2/2, inventory51/52. No prerequisite errors; neither log truncated. |

The old failures are the intended actual behavior: false/count42 keeps offering more/increments, true/count1 stops, and false append/count42 advances; present true/false empty append with prior1/count42 advances to Page3. Fixed outcomes are respectively terminal/Page unchanged, nonterminal/increment once, and terminal/Page2 for the two empty controls. All124 absent/null/Type21/first-empty/missing-info compatibility expectations stay green on both sources. The final fixture hash is identical, source snapshots are genuine, and the logs include compiler warnings followed by all executed assertion output; this is not a compile-only red.

Author final original-red c2 log SHA-256 `d98aa4af3a3b838b6177f4947ec2ba1b027d4e3bfd204f1372c328aa852bcc6e`; fixed c2 log `58e452ad9d8fe5d959677c0b4f881d40949b5ab0a8a694793baf2936f25cff00`; fixed c3 log `8895452266b1240336ecca241b353883c5341a4e14fdc359eeed82dfcd3590e1`. Their results JSON hashes are bound in the checked writer receipt.

No c2/c3 or full-suite rerun occurred in this review: exact relevant bytes, full extraction, final fixture, tools/packages, successful report and meaningful old-red evidence all remained valid after independent inspection. Scheduling instructions require avoiding duplicate checks solely for a new label. There was no concurrent same-project build from this reviewer and no cache writes.

## Finish and custody

Review verdict PASS is limited to AS3 source and its selected offline compatibility evidence. No whole-tree/full-suite, hosted CI, Windows/UWP/MSIX/device/native, live Bilibili, cookie/session/account or write claim is made. AccountP4/P5 hold, U2 gates and protected paths stay in force. Root owns fresh audits, the one stable combined51/52 gate and later exact publication.

After writing the two review files, the final verifier rechecks the exact88-record scope unchanged, including new untracked fixture bytes and all frozen author artifacts. Repository mutation is limited to `favorite/final-review.md` and this file; all private artifacts stay under `/workspace/work/api-shape-comparison/favorite-review/`. No product/test/tooling/main-doc/index/ref/cache mutation, foreign execution or network/auth/live/write request occurred. All review read/write custody is **RELEASED**; no reviewer build or pending write remains after the final release receipt.
