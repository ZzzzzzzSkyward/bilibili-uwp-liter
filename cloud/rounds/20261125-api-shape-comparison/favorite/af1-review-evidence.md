# AF1 independent implementation evidence

Reviewer `api_favorite_review`, 2026-11-25. Supports [af1-final-review.md](af1-final-review.md). Read the ACTIVE implementation dispatch, accepted AF1 contract/evidence/PASS, exact fix grant, writer execution/receipt/input manifests/diff/logs, frozen residual proof and complete actual changed source/callers. AGENTS/CONTRACT/REPO/memory INDEX and accepted AS3/scheduling inputs were already read in this same owning review session. Earlier AS3 review/evidence remain immutable history.

## Exact scope and preservation

Writer `/workspace/work/api-shape-comparison/favorite-af1-execution/release-manifest.json` matches activation SHA-256 `96a3b3f1f344b42cfc838c706b38a409f460209203f9a78c595072e46289c0bf`, and all six entries match actual bytes. Final owned inputs:

| Path | SHA-256 |
| --- | --- |
| FavoriteDetailVM.cs | `daf13e90f68cff339e4a9dc9fc345187e09beeda5c27f4053383efdd2d7aba26` |
| c2 Program.cs | `cd0a567c33aae46d97d5a1d71767686c2d8af59145ead0f497338beb802e6e99` |
| FavoritePublicationProbe.cs | `0e44931091942ea94ac7ef130f1c8b2cb21085cdcdd9fa18bd0a4593fb36eb2b` |
| Writer receipt.json | `70c198f1fa824d8db7e82d2d92fff5040cfe27cd3b32bafe3dd9777b5f95d20c` |
| Writer final-inputs.json | `d627898ad8a5973a23fb0f1fe0e95b4698d58b3bc74262b1d7c12b8a05b7f9d5` |

Private `verify-and-replay.py` independently verifies:

- All52 final inputs, six release entries, authorities, old proof, private artifacts, unchanged originals and both complete report/log receipts match their recorded hashes. The51 green before/after records match exactly; final52 adds the generated decoder. Compiled/runtime source dependencies and caller/XAML evidence are included. The independently resolved c2/c3 Compile/runtime-reader/script/project set has42 inputs, all bound.
- The complete initial VM snapshot is the accepted AS3-fixed `9e75a062fbee0bcfba9bff2a94aabd398894fdf21a1fe37307dd2028813875c1`, also the prior release's final VM. Old-final-fixture and repaired manifests differ only in this VM. No pre-AS3 regression obtains AF1 red.
- VM prefix/suffix outside the property/flag span match exactly. The entire LoadFavoriteInfo method and all subsequent action/command bytes match, not merely the paging expression. Both obsolete flags are absent from FavoriteDetailVM; similarly named state in MyFollowVideoVM remains outside scope.
- Removing only `await FavoritePublicationProbe.Run();` recovers the exact pre-AF1 Program. C2 project/stubs/script/ActionChecks/PagingProbe and all c3 files retain original hashes. The complete frozen ResidualProbe body appears unchanged after namespace enclosure and Main→Run adaptation; its12 assertions retain their expectations.
- All adjacent AS3 comments/source, original execution/evidence and earlier independent owning review files remain unchanged. The unchanged c2 extractor yields the complete actual HttpResults class exactly, including salvage and legacy methods. No substitute setter, notifier, pager or parser exists in a stub.

Actual source review confirms the caller chain: FavoriteDetailPage binds Videos TwoWay, performs initial LoadFavoriteInfo and calls Search from query submission; refresh uses the actual RelayCommand action. IModules synchronously emits PropertyChanged with the exact Videos name. The corrected setter publishes the new field before that event and returns immediately on a same-reference callback echo. Reference identity, rather than row equality or count, controls replacement. No state is changed by getter reads.

The final fixture observer changes its retained Items only in the Videos callback, detaches the previous collection, attaches the current collection and tracks displayed IDs independently. Its same-reference echo has a depth8 guard and explicit ExcessiveEcho/MaxDepth assertions, so recursion cannot be silently suppressed into success. Property controls cover fresh/repeated null, null→A, same A around repeated reads, distinct same-content B, null clear/detach, repeated null→later C and in-place add/remove/move with row identity. Actual command journeys alternate Search/RefreshCommand four times per family without VM reconstruction. Pending response gates expose reset/detach before publication, and Loading-time Search/Refresh/LoadMore guards cannot issue another request or reset. The command SynchronizationContext tracks active async-void operations and posted continuations, captures faults and uses5-second waits, without sleep or polling.

First-page empty/missing-info controls preserve AS3's direct pre-call rows and additionally verify Search's existing preceding explicit null reset. Both source families cover pending null/HTTP transport, business, envelope and malformed-boolean payload failures, exact notices, null→null retry and later publication. Failed LoadMoreCommand retains accepted collection/row/info/Page without replacement events; same-page retry appends one mapper-filtered row with one Add and zero property replacements. These operations use the real complete VM/base/RelayCommand/API/parser/mapper/decoder; the observer contains no command or paging decision.

## Independent isolated old→fixed execution

Actual command from `/workspace/bilibili-uwp-liter`:

```bash
python3 /workspace/work/api-shape-comparison/favorite-af1-review/verify-and-replay.py
```

Exit0 means binding checks and both expected execution outcomes matched. The script copies all46 repository source/runtime-read inputs into each exclusive `replay-original/` and `replay-repaired/` snapshot. The original snapshot substitutes only the exact complete initial9e75 VM; the repaired uses finaldaf13. The snapshots have identical final fixture/stubs/dependencies and differ only in VM bytes. No main-tree c2/c3 project or author scratch is built or edited.

Each snapshot compiles all27 c2 source files resolved from the unchanged C2Probe project, including local default sources and every explicit actual-source Compile link. Using SDK8.0.427's real csc, all163 net8.0.33 reference DLLs and unchanged Newtonsoft13.0.3, the script writes a compiler response file, output DLL, runtimeconfig and copied dependency solely in private scratch. It then invokes that assembly with the same frozen dotnet runtime. There is no restore, dependency installation or shared-cache write. `independent-runs.json` preserves exact compiler/run argv, exits and log hashes; `independent-tools.json` binds166 compiler/tool/reference/package inputs. Compiler output includes the existing async/event warnings, a benign Newtonsoft net6→net8 reference warning and an ignored `/noconfig` response-file-option warning; no SDK default csc.rsp exists, neither build has errors, and both use the same explicit references.

| Independent snapshot | Compile / run exit | Actual assertion outcome |
| --- | --- | --- |
| Original complete AS3-fixed9e75 | 0 /134 | Residual10 PASS/2 FAIL; added283 PASS/123 desired-safe FAIL. All action563 and AS3 original9/compatibility124/present-empty8/added1070 PASS. Final explicit assertion exception causes134 after every added check; not a compiler, timeout or extraction red. |
| Repaired complete AF1daf13 | 0 /0 | Residual12/12 and added406/406 PASS; action563, AS3 original9/compatibility124/present-empty8/added1070 and original c2 Program451 PASS. |

Independent original log SHA-256 `98f81bf1abd20fba62b797c1337d778235de64620c3febfa0cfe828f26986039`; repaired log `f57d3e948e523fa7575729696545f304133da7fad54797afe052f94e0b3ab127`. `snapshot-inputs.json` SHA-256 `7dbf1600bb96292cf0ef9125b9d8a91fcc4e8c2b9fbdee5d27246bd38a3f4aa7` records every snapshot input before execution; `snapshot-custody-after.json` confirms all46 remain unchanged in each snapshot afterwards. Original failures include the exact stale binding case: model accepts new search/refresh rows while the observer retains initial rows, whereas initial publication and same-reference append positives pass. Added failures expose the explicit new safe null/distinct/reset contract and do not weaken old positives.

## Selected owner receipts and limits

Independently checked immutable owner commands/results:

```bash
bash plan/tests/run-all.sh --tools-root /workspace/work/maintenance-foundation/quality --only batch:c2 --output /workspace/work/api-shape-comparison/favorite-af1-execution/original-red
bash plan/tests/run-all.sh --tools-root /workspace/work/maintenance-foundation/quality --only batch:c2 --only batch:c3 --output /workspace/work/api-shape-comparison/favorite-af1-execution/repaired-green
```

The first runner exits1 with c2 stage134 and the same10/2 residual plus283/123 added outcomes. The second exits0 with c2/c3 stage0 and all final counts above plus c3 117. Both have no prerequisite errors or truncated selected logs. Repaired status is **PARTIAL_PASS**, `complete_required=false`, selected2 logical/2 executable within unchanged51/52 inventory. Exact c3 code/tools/results are unchanged and reused; no duplicate c3 run or full suite occurred. These selected receipts and independent replay do not certify combined integration, hosted CI, UWP/native XAML, real rendering/focus/reordering, service/cookies/session/account, native memory or live/write behavior. AccountP4/P5 hold and U2/credential/proto gates remain.

## Finish and custody

`scope-before.json`/`scope-after.json` and `bindings-before.json`/`bindings-after.json` preserve83 exact stable source/test/tool/evidence inputs including the untracked new fixture, with final25 checks PASS. This does not bind moving unrelated memory/query work or claim whole-tree stability. Sole repository writes are `favorite/af1-final-review.md` and this file. Prior review/history, product/tests/build configuration/main docs/index/refs/caches and author evidence were not modified. All execution/output remained under `/workspace/work/api-shape-comparison/favorite-af1-review/`; no foreign, auth, live or write call occurred. All source/artifact read and private-output write custody is **RELEASED**, with no active build, pending response gate or reviewer write after the final private release receipt.
