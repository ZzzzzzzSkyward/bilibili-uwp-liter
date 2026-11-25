# AF2 fresh audit evidence — frozen/released

Independent reader `api_fresh_audit`, 2026-11-25. Read ACTIVE implementation-review/fix grants, amended requirements and accepted amendment review, full final source and owner receipts/raw reports. This audit preserves previous AS1–AS3/AF1 audits and unsafe AF2 discovery history. No shared c2/c3 build, main source/test/Git/cache mutation or live delivery occurred.

## Actual complete source and supported journeys

Inspected full MyFollowVideoVM5007c7536241ef521d37ff19cccaa18fe9e684f82fba63bed85bd94fe50a4ebb, IModules, RelayCommand, FavoriteAPI folder/append/sort builders, ApiDataModel, full actual HttpResults legacy/salvage decoder, exact Utils.DeserializeJson Task.Run method, both generated wrappers and c2 project/stubs/runner/fixtures. FavoritePage.xaml80 binds created folders TwoWay; its refresh command, initial selected-tab load, create/edit/delete completion Refresh calls and collection reorder→SortMyFavorite use these actual VM methods. Dialogs and writes were not executed against a service.

The property has a pure getter and one identity-based notifier; an identical reference is a no-op, a distinct reference is stored before the real inherited notification. Two obsolete flags are gone. CollectFavorite's unconditional setter remains separate and byte-identical. The binding observer replaces its retained collection only inside the real MyFavorite event, detaches/subscribes CollectionChanged there, and asserts retained IDs and event order. Its bounded same-reference callback echo records excess depth as failure; it does not poll the model to obtain replacement rows.

LoadFavorite now completes each currently selected created/default/explicit-bool/collected read before any response-derived assignment/Insert/HasMore/Page commit. Initial status/GetJson/success handling, Task.Run decode, exact indexed CLR-null branch tests, null-created fallback and existing accepted default/collected/null-row/cast semantics remain. Success commits created reference before default Insert0, then flag/Page, then collected reference. LoadCreateList decodes list and flag before Add, then preserves row order and success flag/Page; rejected late metadata cannot leave a retry duplicate. Null append list still fails. No parser/DTO/endpoint/auth/tolerance/dedup/rollback/single-flight policy was added. Arbitrary throwing or different-reference reentrant subscribers during commit remain outside the accepted response-validation guarantee.

Direct load retains accepted M/C on precommit failure; Refresh's existing preceding null resets remain visible and retained on failure; append preserves references/rows/Page and its incoming Loading owner. Tests distinguish these phases, including late missing-default/bad-flag/collected-decode failures, no response event before commit, exact same-pn retry and valid accepted duplicates/null rows. Create/edit completion uses the same repeated Refresh method; no native-dialog acceptance follows.

## Independent full c2 replay on final extended supports

Exclusive scratch `/workspace/work/api-shape-comparison/fresh-af2-audit/`. `prepare.py` exit0 copies39 full Compile/runtime-read/caller files; runs the two exact extraction blocks from the copied current run.sh against copied complete HttpHelper/Utils; and changes only private package configuration to reference the already cached Newtonsoft13.0.3 DLL directly. Package sources are cleared and CLI homes/obj/bin are exclusive. Actual c2 Program/Main and every existing action/AS3/AF1/AF2 fixture run unchanged. No private test implementation replaces the VM, notifier, default insertion, decoder, helper or pager.

The two private trees have identical final fixtures/project/stubs/extractor/runtime-read inputs; only the complete MyFollowVideoVM differs. Original is byte-exact98a1ef21f38816cb020eb7512fdd4c05fbfb0abd168c7f9e729edf23f59bcabe; repaired is byte-exact5007c7536241ef521d37ff19cccaa18fe9e684f82fba63bed85bd94fe50a4ebb. All earlier accepted detail/query code remains in its current repaired state.

Commands ran from `/workspace`, with DOTNET_ROOT=/workspace/work/maintenance-foundation/quality/dotnet, individual private DOTNET_CLI_HOME, telemetry/no-logo/first-time-experience settings disabled, exact SDK8.0.427/runtime/ref8.0.33:

```bash
/workspace/work/maintenance-foundation/quality/dotnet/dotnet run --project /workspace/work/api-shape-comparison/fresh-af2-audit/original/plan/tests/c2/C2Probe.csproj -c Debug --configfile /workspace/work/api-shape-comparison/fresh-af2-audit/NuGet.Config
# exit134 after final explicit behavioral assertion exception; original.log
/workspace/work/maintenance-foundation/quality/dotnet/dotnet run --project /workspace/work/api-shape-comparison/fresh-af2-audit/repaired/plan/tests/c2/C2Probe.csproj -c Debug --configfile /workspace/work/api-shape-comparison/fresh-af2-audit/NuGet.Config
# exit0; repaired.log
python3 /workspace/work/api-shape-comparison/fresh-af2-audit/verify-extractor-private.py
# exit0; exact emitted helper +9 missing/duplicate/incomplete-boundary rejection cases PASS
```

| Identical final AF2 group | Original98a1 | Repaired5007 |
| --- | --- | --- |
| Original13 binding checks |11PASS/2FAIL|13PASS/0FAIL|
| Safe37 metadata/retry checks |17PASS/20FAIL|37PASS/0FAIL|
| Added573 identity/phase/compatibility checks |467PASS/106FAIL|573PASS/0FAIL|

Both executions compile and execute real code. Original134 occurs after every AF2 case; it is not a compiler/extractor/timeout failure. Current flags/optional branches/salvage/native-warning text are not reclassified as new failures. Repaired initial→RefreshCommand retains updated default-first created rows through notifications; collected remains separate. The full extended harness also independently passes original c2 Program451, action563, AS3 original9/compatibility124/present-empty8/added1070, and AF1 residual12/added406. Those fixtures are unchanged, rather than reused solely by old input hash. Owner's raw c3 report still shows117PASS with exact current bound dependencies. Owner c2+c3 aggregate result is PARTIAL_PASS/complete_required=false; this audit adds no stage or full-suite claim.

The original13 and safe37 assertion bodies are byte-identical after namespace/entrypoint adaptation. The seven unsafe historical controls/log are hash-preserved as discovery evidence, not fixed-green expectations. Real Task.Run/async-void operation and continuation ownership is deterministically settled with fault capture and5-second bounds; no sleeps or notification-bypassing observer polling establishes success.

## Exact preservation and bindings

`python3 …/verify.py` exit0,35 preservation/binding checks PASS. It rehashes final6 own paths,6 authorities,6 frozen proof artifacts,2 unsafe historical files,5 AF1 release artifacts,9 private receipts/scripts,7 original fixture/c3 files,58 final Compile/runtime/helper/decoder/tool entries,9 private release entries and all old/current report files. Release manifest equals ACTIVE43e75f47cb3eddb894e0d8d028e5c74170376de46f1257cfd1c6ac6656ff2c32. All54 query source/fixture/dependency inputs and4728 compiler/runtime/ref/package entries remain exact, so unchanged AS1/AS2 results remain reusable; no query rerun is inferred.

Compared complete old/new source outside the three granted spans: constructor, Loading/HasMore/Page ownership, CollectFavorite setter, LoadMore, Refresh, DelFavorite, SortMyFavorite and FavoriteItemModel are byte-identical. Existing Program is exact after deleting one additive dispatch; all stub bytes/methods are exact after reverting only Utils's partial modifier; project is exact after deleting two actual Compile entries; old runner/HttpResults extractor is exact after deleting the added helper block. Generated helper equals the complete actual Task.Run method and both generated old/current dependencies match. Independent10 extractor controls PASS; result receipt equals the owner's exact extractor receipt. No duplicate notification owner, obsolete flag or orphan source was added.

Owner old/current56-input sets differ only in MyFollowVM; green before/after manifests match. Final58-entry manifest adds the two separately generated dependencies; neither count is a test denominator. This reader's39 Compile/runtime/caller files match before→after. AS3 adjacent DTO/parser/mapper hashes and AF1 detail VM/fixture remain exact.

Private `audit-binding.json` SHA256 `30673b33b449b05e24c83b77667109675705ad42d7c33409e91dd80906046d39` binds scripts/manifests/projects/fixtures/full sources/generated outputs/raw logs/verification. Original log SHA256 `173bc3ada0111b29d8c92419aa5fc146b650f6a6a75f1ab2bfe422243f4a9fc6`; repaired log `b38ce079111d1655ae18bce4937d2d7a847d3ad9bdbe20993abf4e916c5f2ce6`; verification-results `85bcb55ea4b2a7201cf933da273a6802e45ba1ca98f819a43388a695a699992f`.

All source/test/build/private read custody is RELEASED; outputs frozen and no own process/gate remains. No full suite or source/main-doc/index/ref/Git/cache/publication mutation or authenticated/live/write call. Native FavoritePage rendering/dialog/reorder, UWP/Windows build, legacy server schema/liveness, cookies/account/session/write/native-player acceptance remains unverified; AccountP4/P5hold/U2/proto gates and fixed checkpoint/deadline are unchanged.
