# Independent follow-up requirements review evidence

Reviewer `api_followup_review`; 2026-11-25 UTC. Private scratch **W** is `/workspace/work/api-shape-comparison/followup-review/`. Read AGENTS/CONTRACT/REPO, memory INDEX and current API/timeboxed records, fixed deadline, ACTIVE review dispatch, author requirements/evidence/release, actual APIs/VMs/page/XAML/transport, G4 closure and E5/E6 pinned sources/callers. Repository HEAD remained `7356b94893377558191b0d70576a52bae704c1f9`; concurrent authorized edits were preserved. No fetch/checkout/pull/index/ref mutation was performed by this reviewer.

## Exact input custody

Before replay, independently verified **every** author binding entry:25 current product/caller source hashes,17 sibling source hashes, all author artifact hashes and6 compiler/runtime/JSON tool inputs. Author binding SHA256 is `9096bc1ed204467a13b2e73ee5e03bf377ded53edac101557b8cfab2ec8eb7fe`. All25 repository inputs still matched at final check (`source_stable=true`). W/input-binding.json records extra page/grid/requirements/dispatch inputs; W/review-binding.json records source/tool/custody results and every listed replay artifact hash.

| Actual repository input | SHA256 |
| --- | --- |
| `src/BiliLite.UWP/Modules/Season/SeasonIndexVM.cs` | `e7eef5dd3c69852f9b17c676122c4c1bec19eae729e49fb2e4c2ad02b19e14c3` |
| `src/BiliLite.UWP/Api/User/WebDynamicFeedApi.cs` | `775ac5004bde97d6a5d02e16d52792af1272ee4d4901f0402a59e297f9ea2efb` |
| `src/BiliLite.UWP/Pages/Bangumi/AnimeIndexPage.xaml.cs` | `89e2d7b0e9aa4d827db38cb54ca5945473ce3781545a70932f5c1dceb579d8cc` |
| `src/BiliLite.UWP/Api/SeasonIndexAPI.cs` | `2f52e361f3f7635f33cdff670be5530f1e4465ea08c2311b4b31e1abe6987f0b` |
| `src/BiliLite.UWP/Api/ApiHelper.cs` | `dfb07f377db95023aebce4c68101c6d334444722cf79b07d7d39a6126ba302d9` |
| `src/BiliLite.UWP/Parsing/Dynamic/WebDynamicFeedParser.cs` | `af784f82b10658104bf44eb4504f5f7425aaf621ea9bf82200d02e41163e2ae7` |

All sibling caches remain clean at the released exact pins: S1 `217fb54f51f6764176319bbdedab2a7522d75401`, S2 `d60a9477e7b3a3d1bfe7570e43ab9370803a8a6b`, S3 `4789508a93f3a39b7d3c896273309db892a9cc8b`. S1 real pgc_index controller loads conditions before queryData and passes named page/pagesize filters to data-specific models. S1 dynamics actual String offset request/model/controller and named query map corroborate opaque source-owned tokens. S2 season data/result families remain distinct; S3 integer annotation/type1/generic fallback do not override current code. No foreign source/script/build/test/dependency was executed or installed.

## Independent unchanged-fixture replay

Copied author complete repro/controls source trees into W without bin/obj; verified identical Program/Stubs/project/real complete source and exact full-class HttpResults extraction. Each command used existing SDK8.0.427/runtime8.0.33 `/workspace/work/maintenance-foundation/quality/dotnet/dotnet`, private DOTNET_CLI_HOME and private NUGET_PACKAGES. Only private projects generated obj/bin. Newtonsoft13.0.3 is the already present hash-bound local reference; no dependency install was needed.

```bash
/workspace/work/maintenance-foundation/quality/dotnet/dotnet run --project /workspace/work/api-shape-comparison/followup-review/repro/Followup.csproj -c Debug
# exit1; CHECKS=118 FAILURES=29
# groups: PGC-initial-red2, PGC-owner-red3, dynamic-roundtrip-red18, dynamic-controls-red6

/workspace/work/maintenance-foundation/quality/dotnet/dotnet run --project /workspace/work/api-shape-comparison/followup-review/controls/plan/tests/g4/G4Probe.csproj -c Debug
# exit0; PASS179 assertions
```

All89 current positive assertions pass. Replayed receipt hash exactly matches the author's original receipt, independently confirming all observed tokens/counts. G4 Program/Stubs remain byte-identical to the main current fixture; its local private project uses only existing copied gRPC/protobuf/JSON references and full real Home VM/parser/mapper/rules. User VM checks remain source-text checks, not full User runtime or WinRT acceptance. These selected private checks are not a full aggregate gate.

| W artifact | SHA256 |
| --- | --- |
| `repro/Program.cs` | `fd3ac1f728b26c07e4cd4a47c6882d936095558d85630ead75e254e2718c7e6f` |
| `repro/Stubs.cs` | `6b67ae0af9500c16d979620aa41acb9773dd55bc371fbca4c1a22aa2f70bc201` |
| `repro/HttpResults.actual.cs` | `8aa4c8734aed85517df1dc3863a479fc076a262e775a7c657e45e9f8b9bc6a93` |
| `repro/bin/Debug/net8.0/receipt.json` | `c7279ac8188488a57ded0352ab73a3afc01ec6943f88dd45337cbf5dfaa23854` |
| `repro/run.log` | `a8d45602561f956f5bd4e2fe01428e312529492c01813e96a15f60a768b51c52` |
| `controls/run.log` | `d15228ca4ba251ac4a9d43ba7e0e1074b725eff07fa9c5f40eec49afedbfd45e` |

## Additional FI1 pending-conditions counterexample

W/guards is a separate copy retaining **all real author product sources unchanged** plus the complete real AnimeIndexPage.xaml.cs. Extra Platform.cs replaces only Windows ComboBox/selection/navigation/base-page/navigation-service types and inert InitializeComponent; it does not implement or patch navigation, selection, conditions, result, request construction, signer, parsing or paging. Reflection invokes the actual page overrides/handler and obtains its actual private VM. No generated x:Bind/rendering is claimed. A synchronous PropertyChanged subscriber invokes the actual selection handler with a selected item to model that declared delivery boundary.

```bash
/workspace/work/maintenance-foundation/quality/dotnet/dotnet run --project /workspace/work/api-shape-comparison/followup-review/guards/Followup.csproj -c Debug
# exit1; CHECKS=7 FAILURES=1 (6 active controls pass)
# OBSERVED condition_dispatches_while_held=2 ConditionsLoading=True Loading=False
# OBSERVED after_first_condition_response second_condition_still_pending=True ConditionsLoading=False
```

Controls prove: actual original page starts conditions with valid Parameter/busy Loading; Refresh is blocked while that initial condition request is held; actual original navigation misses its first result; idle LoadMore with absent Conditions/Result sends nothing; the prospective idle state reaches the real conditions wait; with one conditions response, synchronous actual selection stays guarded and navigation starts exactly one result. The **one desired-safe failure** is the real Refresh command sending a second conditions request while initial conditions remain held. Both replies are explicitly drained. Setting the existing public Loading=false is an explicit prospective-initial-state branch, **not a repaired source, repaired-green claim or manipulation of the unchanged118 fixture**.

Source chain for the contradiction: SeasonIndexVM constructor45, LoadConditions88/137/152, Refresh230–241; page navigation39–43 and selection63; XAML refresh75. MyAdaptiveGridView63–69 executes LoadMore on a false Loading dependency-property change when short; IModules33–35 invokes PropertyChanged synchronously; LoadMore246–254 checks result availability; result state165 is currently published before Loading175 becomes true. Requirements must explicitly cover these existing guard/notification boundaries without introducing an unrelated refresh architecture.

| W artifact | SHA256 |
| --- | --- |
| `guards/Program.cs` | `4fa2c4f826c7e333813354b2fcd1a07e6369b03042d5d5a0d16c3e1dc51b4926` |
| `guards/Platform.cs` | `6cc92fd3bc513949551f1ecd429dd67016f557c02ac3346d0cc428e58e0c0c39` |
| `guards/Followup.csproj` | `81d345c8d661694d49c2bd6d8e1c469aefd34a0e8d14ef6cd3e8d4e86e4bc02a` |
| `guards/run.log` | `91deb2449e7647d38381112d050c467821f01a7fc12155036876dee3c52593a9` |

## FD1 transport and cursor trace

WebDynamicFeedApi15–29 appends the current raw offset into its one cookie GET all-feed route. ApiHelper236–278 ApiModel.url concatenates; Utils35–49 chooses GetWithWebCookie; HttpHelper167–242 prepares cookies then passes the same URL to Get; HttpHelper116 constructs Uri for GetAsync. No later named-value encoding exists. Independent final Uri/query decode reproduces exact plus/percent/ampersand/fragment/duplicate-field failures, preserves null/empty and current CRLF controls, and keeps no-sign/type/timezone/host/method unchanged.

Home DynamicVM180–245 commits polymerOffset only after successful parse/map/publication and clears other sources. User DynamicVM620–665 uses its articleOffset;933–1017 uses polymerOffset for all/video, checks generation and commits only after success;1630–1649 replays the checkpoint's own source/page/cursor. Neither legacy IDs nor gRPC historyOffset/updateBaseline enters this builder. Current typed envelope/items/optional boolean has_more and accepted cursor text remain unchanged. G4 supplies179 positive source-bound paging/failure/retry controls. Declared-only SpaceHistoryV2 has no shipped caller and stays excluded.

## Release and limitations

All reviewer outputs/private scratch are FROZEN/RELEASED for root, author revision and archive readers. No product/test/main-doc/index/ref/cache mutation, shared project/full-suite build, live/auth/write request, foreign execution or native/server/hosted acceptance occurred. Native XAML timing/rendering remains unmeasured; the extra guard fixture proves the synchronous actual managed handlers under declared platform delivery primitives. Old→repaired unchanged-fixture replay and fresh final hashes are future execution/review gates. Later paths require current combined source/docs/archive release and fresh safe dev binding. The fixed checkpoint07:41:29UTC and hard deadline08:51:29UTC were not shifted.
