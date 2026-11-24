# Recovery post-fix independent evidence

**CHANGES — P2-RF2, residual FA-02 early-exit classification. P1-RF1 and FA-03/04 are CLOSED for the inspected source.** The nonzero-remnant/generation repair is materially correct, but a known zero-length cancelled remnant still produces an empty completed title with no partial/cleanup status. This is within the approved FA-02 amendment, not a new source/schema/framework scope.

## P2-RF2 — zero-length known remnant bypasses classification

Actual DownloadVM lines200–225 capture a path stamp, then skip a missing manifest/media file or a zero-length media file before creating a DownloadedSubItem and adding it to `reads`. The title is still added at273. The publication check at291–300 only examines created episode rows, so it never sets `withheld` or removes that empty title. The status expression at305 consequently publishes an empty string when this known cancelled episode is the title's only episode.

Concrete existing user path: cancel an operation before any media bytes arrive; cancellation succeeds; PermanentDelete fails; the observer retires; refresh completed downloads. With an existing zero-length media file, the new process-local marker is present and CleanupFailures correctly retains one named target, but completed output contains one empty title and no cleanup-pending status. The same mechanism can bypass the classification on the other early file/metadata skips. The amendment §2 expressly requires truthful partial/recovery status even when no completed episode remains and no completed title made solely from these withheld remnants. This finding does not request changing tolerant rules for unrelated old/empty folders.

The bounded source-bound driver is `/workspace/work/recovery-integrity/recovery-final-review/post-fix-empty-remnant/`. It compiles full current DownloadVM/helper/coordinator/RecommendVM and the unchanged real decoder/request/page slices with exact current primitive stubs. It uses ordinary actual LoadDownloading → DeleteItemAsync → actual observer retirement → LoadDownloadedAsync. Operation starts with received0/total10 and a zero-length media file; no manual coordinator/partial-state algorithm or business-rule fake is used. Successful validated cleanup settles the fixture at teardown.

```bash
DOTNET_ROOT=/workspace/work/maintenance-foundation/quality/dotnet \
DOTNET_CLI_HOME=/workspace/work/maintenance-foundation/quality/cache/dotnet-cli \
NUGET_PACKAGES=/workspace/work/maintenance-foundation/quality/cache/nuget \
/workspace/work/maintenance-foundation/quality/dotnet/dotnet run \
  --project /workspace/work/recovery-integrity/recovery-final-review/post-fix-empty-remnant/RetryConcern.csproj \
  -c Debug --nologo
```

Exit1 after successful build:

```text
ZERO_INITIAL_CANCEL received=0;total=10;cancelled=True;titles=1;episodes=0;cleanup=1;status=''
KNOWN_ZERO_REMNANT_STATE_PASS=False
```

Required minimal repair: apply known-partial/current-generation classification to the enumerated exact episode even when an existing media/metadata eligibility rule exits before an episode model exists. Carry that classification through the same final publication recheck. Withhold a title whose only candidate is this known remnant and keep the truthful partial/cleanup status; preserve the retained recovery target, valid same-title/prefix/other-root siblings, generation supersession and all unrelated tolerant scan rules. Original recovery owner can fix within DownloadVM/current coordinator APIs and the existing Program; no page, format, parser, project, runner or stage change is required. Preserve all208 predicates, add initial-zero and missing-media known-remnant cases (including successful cleanup/replacement recovery controls), and retain this identical bounded driver red→green before re-review. The approved FA-02 contract already covers this correction; no native prerequisite prevents it.

| Driver artifact | SHA256 |
| --- | --- |
| `Program.cs` | `53f7ac95059d4ef79c27733cd52335bb48dd6f81f8f5d2ed6a799a3776f6c57a` |
| `RetryConcern.csproj` | `053e79a3af412942fadefddba0fa25aa47ff1337364e5470506e7ddd16a51af2` |
| `inputs.json` | `9510b3764d95d1a4e685ee7f09ba2d30dd44a1580e1807bc949826542c900c1e` |
| `run.log` | `ab91034309dfa39361daa523be6b70ccb953a5f55feacb7a4f446a67fa742111` |

An earlier diagnostic using a nonzero-to-zero primitive file transition also reproduced the same branch; the initial-zero schedule above is the controlling receipt and requires no intervening file mutation. This is a deterministic source/UI-state finding, not a native file-lock or playback claim.

## Closed findings and acceptance fidelity

RF1's current-source per-GUID/source failure registry is checked before any sibling cancellation/storage call in retained, fresh and bulk flows. A requested CTS or local observer retirement no longer proves absence. Actual successful full reconciliation retires only absent owned sources; unchanged/current failed GUIDs keep their attached source and cannot acquire a replacement on rescan. Successful earlier siblings are recorded immediately, the later failure prevents deletion, and target feedback distinguishes partial cancellation. The repaired actual RF1 command, settlement and independent-episode tests cover these continuations.

I independently reran the **original unchanged reviewer RF1 driver**, SHA256 `3ed0d172eadec76bef86f14225144957c656da983e82df19ef91a0e3f2ea9600`, after binding full links to current repository sources and copying the current primitive stubs/exact unchanged wrapper. Scratch: `/workspace/work/recovery-integrity/recovery-final-review/post-fix-rf1/`; command uses the same SDK/cache environment and `dotnet run --project …/RetryConcern.csproj -c Debug --nologo`. Exit0:

```text
FIRST failed_targets=1 deleted=0 token_requested=True transfer_pending=True
RETRY failed_targets=1 confirmation_count=2
RETRY deleted=0 transfer_pending=True cancellation_fault_still_active=True
ACTUAL_SOURCE_CANCELLATION_RETRY_UNSAFE=False
```

RF1 driver `inputs.json` SHA256 `1b878c85671b2723645aa0330f9eb8eec5f39e63ec7ce087f1e7521203ac2490`; log `3866baec9bdd4425d35d77771fccfa4c1346ee472414dd1efdc6a6b332171011`. The original driver settles its operation before exit. This independently closes P1-RF1 without relying only on the executor's green log.

FA-02's nonzero and generation schedules are accepted: markers are exact normalized paths; current helper preparation and every dispatched required transfer contribute to a reference/revision-bound current generation; an old callback, failed/partial preparation, one sibling, pending/faulted/cancelled transfer, disappeared work or unrelated sibling cannot prove current success. Successful validated cleanup resolves matching state under the destructive lease. Fully successful newer generation supersedes the old marker/recovery without granting old snapshots authority. Final recheck rejects a staged model whose evidence changed across file awaits. The residual above is the path which never creates such a model.

FA-03 captures collection and row before the action request, removes only the current owning object, recomputes Ready empty/continuation state, and leaves pending/failure/no-progress request ownership intact. `hadCommittedFeed` preserves refresh-vs-initial failure classification when action removal empties rows during the request. The real dislike/feedback, same-idx replacement, nonfinal/missing/rejected and pending/failed/retry-mode schedules pass. FA-04's total0 guard yields finite0 and preserves ordinary/extreme ulong values through both actual scan and progress callback paths.

Independently verified final Program and Stubs are byte-identical in immutable red-source and green-source. Red contains208 checks/31 genuine failures; green208 checks/zero failures. The first139 outputs PASS on both sides. All99 original static complete Check invocation bodies are present unchanged (loops produce139 executions), and the entire original Recommendations-through-FinalAcceptance method region is byte-for-byte unchanged. Only validated fixture teardown and the additional acceptance region were added. Stub deltas are inert-default FileIO read gating and IProgress callback recording; no failure/generation/classification/feed-state algorithm is copied into a fake. All seven exact-source wrapper slices remain byte-identical on the red/green/current sides; the full current products remain direct project links.

The local selected report is PARTIAL_PASS2/2 with50 NOT_SELECTED and complete_required=false, recovery208/g5335. I reused that exact execution after byte-binding; no full unchanged suite was run for a review label. Raw receipts under `/workspace/work/recovery-integrity/recovery-review-fix/`:

| Artifact | SHA256 |
| --- | --- |
| `red-final.log` | `b910f40f7ac789c1e2724b351d352605f1ed882e2a8b44ec9817c8e971e2e5f4` |
| `red-inputs.json` | `f63d0c600537f4aef53f5c6bae466b9fd83f86d871d11fae579f8999e37a2b9c` |
| `green-inputs.json` | `9fdabe2d5b2c3eb34b1246dade0e636768325123a448c853374fffeef15ba498` |
| `targeted/results.json` | `c9a1fd4a8b905155dd3cfa8f50962c0f531e269a7f3205a6ebb4186b4466cd25` |
| `targeted/logs/batch_recovery.log` | `56cc2dd764c94352e9c6603cdf0961a0a37d3d4e495c6601b127a5b4bc2fe322` |
| `targeted/logs/batch_g5.log` | `78902cb316a7d7033ed3868e015f2adbdf673f6aff0f0e8d125b31fce9a96fd2` |

## Exact current source/test binding and structure

All915 source/test/tooling/wiring inputs were independently hashed and rechecked stable. Relative to the initial full common gate, exactly eight authorized paths changed: storage service/LM02 and these six recovery fix inputs. Sorted indent2/newline current manifest SHA256 is `3cb4d3c1ca57b935a738ab882943f1086c6f280587c56d58c61ccf1667842ff7`, matching root. All six recovery current files match immutable green-source. Machine verification is `post-fix-rf1/binding-summary.json` SHA256 `7ad6d9f2d75bcf0702f793542ead7b2dc56c99129e7c2b06a1e7bd479dc1f4c8`; current full manifest is `post-fix-rf1/current-source.json`.

| Current recovery-owned path | SHA256 |
| --- | --- |
| `src/BiliLite.UWP/Modules/Home/RecommendVM.cs` | `9bf04a115f5961ac3f754eecfba206a212dffd52a65484e6e8f6f098db2da612` |
| `src/BiliLite.UWP/Pages/Home/RecommendPage.xaml` | `b021c4a3e86c4ad73973c319d2c1c4afa26c77f9ff98162a75e2344834f57746` |
| `src/BiliLite.UWP/Pages/Home/RecommendPage.xaml.cs` | `ac8674c49f93f1f25bd8c824f255d6a4f761e72ec3aaa4b3a6e4f03f9806bcd1` |
| `src/BiliLite.UWP/Modules/DownloadVM.cs` | `17ad1184406daa75b2ddbd0f476b631908a728314a0f3dd4dd03863226336740` |
| `src/BiliLite.UWP/Pages/DownloadPage.xaml` | `f09e083e77b62a748bbcc098c815204548396f2bfc2a3b386c9a0f378b7830b5` |
| `src/BiliLite.UWP/Pages/DownloadPage.xaml.cs` | `c80f7267f7db8000c6f97f3dab1bfb37f631291c463a791a9f78db9e2423e56f` |
| `src/BiliLite.UWP/Pages/SeasonDetailPage.xaml.cs` | `b44706b9156259cc57acda3611023b4a9e0ed96f9345b3576ddaac3148804490` |
| `src/BiliLite.UWP/Helpers/DownloadHelper.cs` | `c741f289128875f477545fa1e8d912c66a402ea24a4050def01bb4f1d58ca293` |
| `src/BiliLite.UWP/Services/DownloadPathCoordinator.cs` | `a8516adee00ec48a3a511e8ea7b52b873d3dba77a4ec9aba83e15115da533746` |
| `src/BiliLite.UWP/Parsing/Recommend/RecommendResponseParser.cs` | `fae8ec36361069bbb130557e8b8230b01a0c68dbe81689ada9a9b5da09b57f2d` |
| `src/BiliLite.UWP/BiliLite.UWP.csproj` | `b0ccc591ddc5c1f8fd469e9c22cb735ca121fd2b64be5a0ba536ff59adfe8677` |
| `plan/tests/recovery/RecoveryProbe.csproj` | `d74ee6d14e7e006ab906aabeda859d29da85c2667855113026f3fd66d62ebef0` |
| `plan/tests/recovery/Program.cs` | `8d4733527647442bf807bba78f3513d791b0b7332cc74362831c449189e1bd85` |
| `plan/tests/recovery/Stubs.cs` | `d12fd29832dc36823fd01f77ec723f49ddcfc013c4955cc28bae6d5cf0454b73` |
| `plan/tests/recovery/prepare-source.py` | `307356c8bb9cb0f5804b51718025f4f1a623c0cb232d2791815a1defb4cf9f90` |
| `plan/tests/recovery/run.sh` | `b823095c5efc6d53c0dc98076362532164fd880a736550b03da7af649a8b2451` |
| `plan/tests/run-all.py` | `afc18ae7e9fa6256a0b63e49846d90f2d4d8ae8ebdb32b0e4c753bcd6ecd11f5` |
| `plan/tests/verify-suite-runner.py` | `0f0992d6714718f862bf7a50db70be7d18874cd16eb648f56a1147012cb47d67` |

Touched structure remains acceptable apart from P2-RF2 behavior: evidence belongs in the existing coordinator, creator preparation/detached completion stays in DownloadHelper, GUID observation/cleanup/read publication stays in DownloadVM and feed state stays in RecommendVM. New APIs have actual consumers; no orphan/duplicate helper or test-only algorithm was found. Current-generation state is one record/marker per episode, without disk format changes or transfer-lifetime leases. The feature parser, page wiring, project registration, inventory and seven source slices are unchanged from the accepted source review.

Async command adapters retain catch boundaries; helper transfer observations contain transfer failures and report completion in finally; logical leases and command/scan busy flags release in finally. New product uses existing framework-compatible collection/Task/lock/lambda/property forms and no net8-only product API. Existing UAP19041/min18362 compilation assumptions remain; Linux console/XML/source checks cannot certify native C#/XAML compilation. Warning classes in the bounded reruns remain the documented inherited HttpResults/model/RemoveItem/RelayCommand warnings; none was suppressed.

Runner/control inputs are unchanged, including run-all.sh `6b9cc38288b5bea7e07e50bed43a6c2027923ebcf0acf5c706f33a7dce1fd941`, so the fresh14/14 control receipt `427d42beffae5b83956bd731a96f7474740f3b916812dfefcaf2de171600448e` remains applicable to runner behavior. Current51 logical/52 executable, historical50/51 and separate package15 are preserved. The initial full51/52 PASS is **pre-fix history**; it cannot certify these changed products/tests. Root correctly awaits residual closure before the next complete changed-source gate.

HEAD remains storage receipt `e928bf0e818869c03485f8ef6f31fa47e98ac0f0`, dirty/local/unpublished. Supported publication, remote/local adoption, final documentation and fresh caller audit remain separate root gates. Windows/UWP/XAML/rendering/native BackgroundTransfer/file-lock/restart/session checks remain unavailable; no native/hosted/runtime success is asserted. Reviewer wrote only assigned review/evidence Markdown and scratch drivers, with no source/test/main-doc/index/object/ref/API mutation.
