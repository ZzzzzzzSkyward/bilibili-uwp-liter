# AF2 independent implementation evidence

Reviewer `api_favorite_review`, 2026-11-25. Supports [af2-final-review.md](af2-final-review.md). Read ACTIVE implementation/fix grants, amended requirements/amendment/independent PASS, evidence, full writer receipt/input/diff/logs, original13/safe37 proofs and unsafe historical controls. AGENTS/CONTRACT/REPO/memory INDEX and prior AS3/AF1/scheduling material were already read in this same owning-review session. Prior review/evidence remain immutable history.

## Binding and exact source scope

Writer release manifest SHA-256 matches activation `43e75f47cb3eddb894e0d8d028e5c74170376de46f1257cfd1c6ac6656ff2c32`. All nine manifest entries match actual bytes, including the six granted source/test paths and new fixture. Key hashes:

| Input | SHA-256 |
| --- | --- |
| Final MyFollowVideoVM.cs | `5007c7536241ef521d37ff19cccaa18fe9e684f82fba63bed85bd94fe50a4ebb` |
| Original complete MyFollowVideoVM snapshot | `98a1ef21f38816cb020eb7512fdd4c05fbfb0abd168c7f9e729edf23f59bcabe` |
| Final FavoriteFolderPublicationProbe.cs | `e37b1a8f1a382170f003ed75a6a3eb110ddd7e333943e3452692ae2bca67d6b2` |
| Final C2Probe.csproj | `fe09ccfc868ce2709c8b3b8926447ee9ab7c41d69fa2b050771bec7ab1325eb2` |
| Final C2Stubs.cs | `d0857739e563bdcc4cd49157400eb2a2bc039a00c592f2d23dbce07536e97e7b` |
| Final run.sh | `1c84653ae991c49892a7af3dc2e6b33ba40727d9f071ab4d7033b44bdc57d27b` |
| Writer receipt.json | `b744b0a6aa8c614b19e49178d6f6ac9c1be1dc1eacf9712faac130edf82318a1` |
| Writer final-inputs.json | `dac938bae90457d0ae261087005657dc4163f8f0c77de9655d11cf9079ef711c` |

Private `verify-and-replay.py` independently verifies all58 current final inputs,56 green before/after records, authorities, reports/logs, original/final snapshots, frozen13/37 proofs and seven unsafe historical controls. Original-final-fixture versus repaired inputs differ only in MyFollowVideoVM. Final58 adds both generated actual helper and decoder to the56 pre/post runnable/read/tool/package records. All46 independently resolved c2/c3 Compile/runtime-source-reader/script/project inputs are included. No moving unrelated memory/query/player scope is claimed.

I inspected complete actual MyFollowVideoVM/FavoriteItemModel, base notifier/RelayCommand, FavoriteAPI, full HttpResults and ApiDataModel legacy decoder, exact Utils.DeserializeJson Task.Run body, current project/stubs/extractor and full551-line fixture. The verifier elides only MyFavorite/flags, LoadFavorite and LoadCreateList and proves every remaining product byte identical to original. Thus CollectFavorite's unconditional setter, DelFavorite, SortMyFavorite, Refresh, LoadMore and item notifications are unchanged. The earlier AF1 VM and fixtures/receipts also match their preserved hashes.

The setter stores before the inherited synchronous MyFavorite event and uses reference identity, not row equality. Its getter mutates no state; same-reference echo terminates and later distinct changes publish. Response reads now precede any response-derived reference/Insert/Add/success flag/Page commit. Direct failure retains existing created/collected references and row identities; Refresh retains its explicit preceding null resets; both finish Page1/HasMore=false/Loading=false with existing notice. Append fails without Add at the same Page and leaves incoming Loading unchanged, as before; valid retry adds only the accepted row and advances once. Existing skipped CLR-null branches remain skipped, JSON-null tokens retain their original failure/accepted-result distinctions, null-created decode still coalesces, accepted null defaults/collected results remain accepted, and null append lists still fail. Current bool/int/float/string cast acceptance and legacy missing-code/salvage rules remain; no typed parser, auth or request migration occurs.

Success order is explicitly observed: created-reference event at Page1 with the decoded created list, default Insert0 collection event, true HasMore event, collected assignment at Page2. Append retains source row order, existing accepted duplicates/nulls and collected identity. The guarantee ends at validated response commit; no rollback across arbitrary throwing/reentrant observers is claimed.

## Harness and observer integrity

Removing the one new Program dispatch recovers original Program exactly. Reverting only `partial` recovers the complete old Utils stub; removing the two Compile entries recovers the old project; removing only the added helper block recovers the entire old runner, including original HttpResults extraction. All earlier action/AS3/AF1 fixture files and c3 files match their old bytes. Their assertions run unchanged on this extended harness; old whole-input receipts cannot imply current harness identity because these three support files changed.

The emitted helper equals the complete actual Utils method, including its Task.Run/lambda/JsonConvert body. The decoder equals the full actual final HttpResults class with salvage/legacy methods. Neither the observer nor stubs implement notification, folder parsing, insertion, paging or commit policy. The observer retains its own collection only in MyFavorite callbacks and attaches/detaches CollectionChanged there; assertions use retained IDs, exact references, event counts and order. A depth8 echo guard records excessive recursion as failure. Null/getter/identity/distinct-equal-content/later-reference tests, stale detach, in-place insert/move/remove and actual item notifications are included. CollectFavorite's unchanged unconditional setter is independently exercised.

Real repeated RefreshCommand/Refresh cycles use deterministic response gates and a SynchronizationContext tracking active async-void operations, posted continuations and escaped faults, with5-second limits and no sleep/polling. Pending reset/detach, Loading guard, direct/fresh/Refresh transport/business/decoder/late-default/flag/collected failures, malformed/null/missing-list branch cases, valid retry and append under both incoming Loading values exercise the accepted phase table. Optional branches, null results/rows, serialized lists, legacy code/salvage, supported flag casts, accepted duplicates and actual request order provide compatibility controls.

Static FavoritePage traces confirm initial pivot LoadFavorite; create/edit/delete completion calls Refresh; refresh button uses RefreshCommand; load-more uses LoadMoreCommand; drag completion calls sorting; MyFavorite is TwoWay and collected is OneWay. Creation/edit/delete dialogs and server writes were not executed. The sorting assertion executes the real builder/VM with canned transport only.

## Independent actual-source replay and extractor controls

Actual command from the repository:

```bash
python3 /workspace/work/api-shape-comparison/favorite-af2-review/verify-and-replay.py
```

Exit0 confirms checks and both expected run exits. The script copies all52 repository source/runtime-read inputs into exclusive original/repaired snapshots, substituting only the complete original98a1 VM in the former. Both snapshots use the same final fixture/project/stubs/generated bodies and differ only in that VM. It resolves all30 c2 source files from the unchanged current project/default local sources and compiles complete actual production methods with SDK8.0.427's csc,163 net8.0.33 reference DLLs and existing Newtonsoft13.0.3. Runtime is8.0.33. All167 compiler/reference/runtime/package inputs are hashed before and after; both manifests match. All52 snapshot inputs per run also remain unchanged after execution. No main c2 build, NuGet restore, dependency install or shared-cache write occurs.

| Independent snapshot | Compile/run exit | Observed outcomes |
| --- | --- | --- |
| Original98a1 | 0/134 | AF2 original11 PASS/2 FAIL; safe-boundary17 PASS/20 FAIL; added467 PASS/106 FAIL. Final explicit assertion exception after all checks causes134. |
| Repaired5007 | 0/0 | Original13/13, safe37/37, added573/573; original c2 Program451 completes. |

Both independently rerun the existing action563, AS3 original9/compatibility124/present-empty8/added1070 and AF1 residual12/added406 without changing their assertions; every one passes. Compiler logs contain only expected async/event warnings and the Newtonsoft net6→net8 reference warning, no errors. The original red is actual decoded behavior, not compilation, timeout or extraction failure. Original13 and safe37 assertion bodies are embedded exactly after namespace/Main→Run adaptation. The seven unsafe observations remain separate unchanged discovery history; they are not fixed-green invariants.

Independent original log SHA-256 `24f21cecceea2f35daf9a6951f3971a329926bf43cf81ed15e7cd2a19817a387`; fixed log `fde08c9d13ffe03b094664089868329f7b444e376512d0f22a3c34eb04f20d02`. `independent-runs.json` records exact compiler/run argv and outcomes; `snapshot-inputs.json` hash is `6a5982b77ef47553a2c7df7f9680c90199238aff6a14e205c5c30afefe25a9dc`.

The same review script separately executes the actual new extractor block against private source copies. **10/10 PASS**: positive complete-body equality and rejection of missing declaration, duplicate declaration, spaced duplicate declaration, missing/duplicate next boundary, missing opening brace, missing lambda/method closure and truncated source. `independent-extractor.json` hash `e331b1ed01f647c6674d0c4458295cf071095eba04c7a12d25884862c4b1bc1e` contains individual exits/stderr; author extractor artifacts were neither executed in place nor rewritten.

## Owner selected receipts and finish

Exact checked owner commands:

```bash
bash plan/tests/run-all.sh --tools-root /workspace/work/maintenance-foundation/quality --only batch:c2 --output /workspace/work/api-shape-comparison/favorite-af2-execution/original-red
bash plan/tests/run-all.sh --tools-root /workspace/work/maintenance-foundation/quality --only batch:c2 --only batch:c3 --output /workspace/work/api-shape-comparison/favorite-af2-execution/repaired-green
```

Old runner exits1/c2 stage134 with the same binding2/metadata20/added106 failures; fixed runner exits0/both stages0, all c2 counts above plus c3 117. Neither has prerequisite errors or truncated selected logs. Fixed result is **PARTIAL_PASS**, `complete_required=false`, selected2/2 in the unchanged51logical/52executable inventory. C3's exact unchanged source/tool/result binding is reused; no duplicate c3/full-suite run occurred. Earlier AS1/AS2 accepted scope is untouched; its live/server claims are not expanded.

Final `scope-before.json`/`scope-after.json` bind101 exact stable source/test/tool/artifact inputs; `bindings-before.json`/`bindings-after.json` finish with29 checks PASS and no difference. Sole repository writes are `favorite/af2-final-review.md` and this file; all other output is under exclusive `/workspace/work/api-shape-comparison/favorite-af2-review/`. No code/test/main-doc/index/ref/cache mutation, foreign execution, live/auth/write call or schedule/deadline operation occurred. No native/UWP/build/render/dialog/server/session/account/write/liveU2 or native-memory pass is inferred. AccountP4/P5 hold and U2/credential/proto gates remain. FI1/FD1 follow-up and final combined51/52/publication remain root-owned. All read/build/write custody is **RELEASED**, with no pending response/async command, build or write after the final private release receipt.
