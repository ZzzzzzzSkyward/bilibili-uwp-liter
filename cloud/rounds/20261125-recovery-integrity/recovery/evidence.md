# Recommendation and download recovery evidence

Status: REVISED / READY FOR INDEPENDENT RE-REVIEW of P1-RV1. No implementation grant or green claim. Owner `recovery_requirements`; product/test/build/main-document/index/history read-only. Baseline HEAD: `077ef35ae194d3ae85aa0621daeac07dc29e29ab`. Findings support the bounded implementation in [requirements.md](requirements.md), not a device failure report or an implementation PASS.

## Inputs and source scope

Read AGENTS.md; cloud/{CONTRACT,README,REPO}.md; memory INDEX and relevant decisions/pitfalls; this round plan/intake-observations/discovery-coordination; prior foundation UX requirements/evidence and canonical maintenance-plan; relevant acceptance policy and probe inventory. Root owns the recorded clean fetch→checkout dev→pull startup; this specialist did not repeat shared branch/index mutations.

Inspected actual RecommendVM, RecommendPage XAML/code, MyAdaptiveGridView, RecommendAPI, IModules, RelayCommand, Utils.ToInt32 and HttpResults decoding; DownloadVM, DownloadPage XAML/code, DownloadHelper folder/GUID creation, DownloadDialog creation/close callbacks, App initialization and VideoDetail/SeasonDetail/SettingPage callers. Commands were `rg --files`, bounded `rg -n`, `nl -ba … | sed -n …`, exact file reads and the local fixture commands below. No external request was required.

Source anchors:

| Boundary | Current source evidence |
| --- | --- |
| Empty/failure automatic command | RecommendPage.xaml:145 hardcodes CanLoadMore=True. MyAdaptiveGridView.cs:63–70 invokes the command after Loading=false with zero scroll extent; :86–90 invokes on bottom scroll. RecommendVM.cs:155–177 dereferences Items.LastOrDefault().idx before null/empty/Loading guards, and commits last_index before success. |
| Recommendation refresh/parsing | RecommendVM.cs:50–122 uses ToInt32 on code, maps then publishes, with transient toast only; :179–189 nulls Items/Banner before the request. Actual Utils.ToInt32:138–149 maps a nonnumeric object string to0. A successful empty items array remains empty; LoadMore then dereferences its null last row. |
| Feedback selection | RecommendPage.xaml:21 exposes reason rows for any reason-bearing three-point model; code:156–160 forwards that model. RecommendVM.cs:203–207 ignores Feedback's returned ApiModel and requests the earlier Dislike model. RecommendAPI's real builders distinguish reason_id and feedback_id. |
| Completed-library scan | DownloadVM.cs:108–116 clears rows and awaits root/disk/enumeration outside any top-level catch/finally; :252 clears busy only after successful completion. :232–235 tolerates individual title errors. RefreshDownloaded:95–103 refuses requests while the busy flag remains true. |
| Cancellation ownership | DownloadVM.cs:431 replaces cts on every scan, :445–457 allocates new per-CID tokens, :459–462 reuses old operation/observer entries, :507–515 binds the first token to AttachAsync, :536–538 always RemoveItems on completion. DownloadDialog.xaml.cs:214,230 invokes rescans after add/close; App.xaml.cs:360 invokes startup scan. |
| Actual folder/key scope | DownloadHelper.cs:38–39 creates video/season parent then episode folder; :211–219 persists CID and that episode-folder Path per GUID. DownloadVM.cs:481–495 groups by CID and stores the group key in DownloadingItem.EpisodeID. Delete methods use this Path, not the parent. |
| Season caller mismatch | SeasonDetailPage.xaml.cs:732 compares DownloadingItem.EpisodeID to item.id (season EPID), while the actual grouped key is item.cid. :736 correctly compares completed-subitem EpisodeID to EPID. VideoDetailPage.xaml.cs:2218 correctly compares current row key to CID. |
| Task effects/error surface | DownloadVM.cs:655–669,695–714 confirms cancellation only, cancels token, PermanentDeletes episode folder and swallows exceptions. RemoveItem:584–619 can remove the final row before cleanup failure. PauseItem/ResumeItem:624–648 also swallow errors. |
| Existing completed-delete behavior | DownloadPage.xaml.cs:284–303,344–362 names target and permanent deletion; declined confirmation returns; removal follows storage success; failures toast/log. Preserve this behavior. |

## Real-source local reproducer

Scratch root `/workspace/work/recovery-integrity/recovery/` contains `RecoveryDiscovery.csproj`, frozen source copies/extractions, Program.cs, Stubs.cs, `verify-source.py`, `source-sha256.json`, `red-baseline.log` and `red-final.log`. Scratch only; no raw files/logs/build output are proposed for commit.

Real code compiled:

- Full unchanged RecommendVM, RecommendAPI, IModules, RelayCommand, ReturnModel and API DTO model files.
- Exact unchanged full HttpResults class and ApiModel request class in namespace wrappers; exact Utils.ToInt32 body. Thus response acceptance is actual production decoding, including the existing embedded-object salvage policy, rather than a stricter fake.
- Exact DownloadVM methods: RefreshDownloaded, LoadDownloaded, LoadDownloading, Handel, DownloadProgress, GetProgress, RemoveItem, DeleteItem, DeleteAll, PauseItem, ResumeItem, StartAll and PauseAll; actual DownloadingItem/DownloadingSubItem/DownloadedItem/DownloadedSubItem models. Exact DownloadSaveInfo/DownloadSaveEpisodeInfo/DownloadSubtitleInfo/DownloadGUIDInfo classes from DownloadHelper.
- Exact unchanged season in-progress `if` block in a wrapper with primitive item id/cid and a real current DownloadingItem fixture.

The scaffold supplies only omitted VM fields/caller wrappers and dependencies: settings/log/dialog/toast recorders; API required-parameter/signature stand-ins; fake Request delivery; Windows storage/bitmap/stream surfaces; controllable BackgroundDownloader current-operation list and primitive AttachAsync.AsTask wrapper. Request bodies/parameters, VM state, conditional selection, actual observer token passing and RemoveItem behavior execute in real source. Fake GetDownloadFolder injects root failure; disk metadata helper is a no-op for this root-failure fixture, so this run does **not** establish a disk or child-error green behavior. A capture SynchronizationContext observes exceptions escaping real async-void without crashing the harness.

The cancellation-race case explicitly schedules real RemoveItem within the failing storage primitive after Cancel; this is an allowed order of actual Handel.finally and delete continuation, not an observed Windows schedule. The repeated-scan case invokes real LoadDownloading twice while the same primitive observer remains pending, invokes real DeleteItem and inspects the token passed by real Handel. It proves detached token identity, not an OS-specific file-lock claim. The feedback fixture supplies a supported reason-bearing `type=feedback` card; it proves wrong branch request selection, not today's live menu payload.

Commands and observed outputs:

```bash
python3 /workspace/work/recovery-integrity/recovery/verify-source.py
# SOURCE_BINDING_PASS 12 source hashes; 6 full files; 25 exact unchanged class/method bodies

/workspace/work/maintenance-foundation/quality/dotnet/dotnet run \
  --project /workspace/work/recovery-integrity/recovery/RecoveryDiscovery.csproj -c Debug \
  > /workspace/work/recovery-integrity/recovery/red-baseline.log 2>&1
# exit 1 (compiled successfully; expected failing assertions)

/workspace/work/maintenance-foundation/quality/dotnet/dotnet run --no-build \
  --project /workspace/work/recovery-integrity/recovery/RecoveryDiscovery.csproj -c Debug \
  > /workspace/work/recovery-integrity/recovery/red-final.log 2>&1
# exit 1; same 17 checks / 13 failures
```

`dotnet --info` confirms SDK8.0.427/runtime8.0.33; project pins Newtonsoft13.0.3 from the prior quality cache. No dependency download was required. Source verification checks whole-file SHA256s, normalized full-file identity and unique exact source-block membership; it fails on baseline drift. The scratch generation helpers are retained as investigation history; use the frozen fixture plus verify-source.py to reproduce, rather than rerunning incremental generator scripts.

Authoritative assertion output (`red-final.log`):

```text
RED initial failure followed by real LoadMore is exception-safe; actual=ArgumentNullException
RED successful empty followed by LoadMore is exception-safe; actual=NullReferenceException
RED failed append accepts each later explicit retry at idx10; actual requests=1
PASS failed append retains existing rows
RED failed refresh retains known-good collection; actual null=True
RED nonnumeric business code cannot publish success; actual items=1
PASS declining task deletion leaves token and folder intact
RED task confirmation names target and permanent partial-file deletion; actual=取消任务:确定要取消任务吗?
PASS confirmed cancellation deletes only episode folder
PASS batch continues after first folder failure
RED mixed batch reports one truthful summary and logs failure; actual toasts=0,logs=0
RED cancellation completion before delete failure retains recovery target or actionable feedback; rows=0,toasts=0
RED root scan failure is caught, clears busy and preserves prior rows; uncaught=1,busy=True,rows=0
RED repeated transfer scan cancels attached observer token, not a replacement; actual attached=False,map=True
RED failed pause is logged and visibly reported; actual toasts=0,logs=0
RED feedback branch sends real Feedback builder result; actual=stub=1&goto=av&id=123&mid=1&reason_id=91&rid=2&tag_id=3
RED season caller recognizes current CID task when episode id differs; actual state=0
CHECKS=17 FAILURES=13
```

Key baseline hashes (all12 are in source-sha256.json):

| Source | SHA256 |
| --- | --- |
| Modules/Home/RecommendVM.cs | `456e8f396a984eef0983f61927e1585b7004314e0e040695a2082fe98a250342` |
| Modules/DownloadVM.cs | `e0342ab3aea7ec3d894d9d432d13bebb79b47a1db00f9aa95ea2435f8c894357` |
| Helpers/HttpHelper.cs | `b12e457a75a133e9298fb77ad982b33986c2f95da2562b4f031b1fd692424d13` |

## Existing probes, preserved behavior and ruled-out hypotheses

Search `rg -n 'RecommendVM|DownloadVM|RecommendPage|DownloadPage|MyAdaptiveGridView' plan/tests --glob '*.cs' --glob '*.csproj'` finds recommendation XAML title-tooltip checks in g5, and no direct compilation of either VM. Inspected g5 project/stubs/assertions, d1/d2, localdir and relevant aggregate-runner manifest/control totals. d1/d2 are danmaku tests; localdir owns local-player directory building/wiring rather than task/file operations. A dedicated recovery batch is justified in requirements, with explicit current inventory51/52 and a single tooling/documentation owner; no stage was added during discovery.

Existing recommendation protection baseline:

```bash
bash plan/tests/run-all.sh --tools-root /workspace/work/maintenance-foundation/quality \
  --only batch:g5 --output /workspace/work/recovery-integrity/recovery/g5-baseline
# PARTIAL_PASS; 1 logical/1 executable PASS; complete_required=false
# Current/historical inventory is still 50 logical/51 executable during discovery.
```

Evidence files: scratch `g5-baseline/results.json`, `summary.md`, `logs/batch_g5.log`. This is not a full-suite PASS. No unchanged full-suite run was needed at requirements stage.

Ruled out or narrowed:

- Append failure already keeps committed rows. Preserve that passing assertion; the genuine defect is retry/cursor consumption and missing persistent state. Refresh failure is the path that discards known-good rows.
- Null/empty LoadMore is not merely an impossible direct call: actual adaptive-grid callbacks and footer command reach it after initial load completes. The harness proves source exception behavior; actual XAML event timing remains a native check.
- Bulk start/pause/delete already exist. No new bulk feature or universal undo is required.
- Completed-download warnings/error handling already exist and are materially better than task cancellation. They are preservation targets, not invented missing functionality.
- Task Delete's Path comes from the episode folder. No parent-video/season deletion bug was found. The CID-valued property name EpisodeID is confusing but stable; fix the incorrect season caller comparison rather than migrate the persisted schema/keys.
- Both task operations and storage deletion already have per-target batch continuation. Preserve continuation and add accurate reporting; do not convert mixed failure to all-or-nothing rollback.
- No evidence here supports a global transfer scheduler/protocol replacement, account work, real file-lock timing guarantee, new navigation-cancellation framework or broad accessibility/layout refactor.

## Limits and safe next steps

`command -v powershell` and `command -v pwsh` return no paths; .NET reports Linux/no installed workloads. No Windows/UWP build, XAML compiler, rendered state/focus/Narrator check, native BackgroundTransfer/file-lock experiment, live request or real user-file mutation occurred. Earlier accepted device rows remain accepted under repository policy; these local reds are deterministic source diagnoses rather than fabricated BR entries.

Reviewer can reproduce the frozen actual-source red now, inspect the exact scopes/contracts and then authorize an executor after PASS. Requirements specify immutable publication, explicit retry/empty/error states, current CTS/GUID ownership, truthful irreversible file effects, retained cleanup targets and stale-generation protection. The new tests must preserve existing assertion bodies and bind green to actual source; updated runner controls and final integrated gate are separately owned. Windows follow-up remains explicit and cannot be claimed passed by these fixtures.

## P1-RV1 revision checkpoint

Reopened requirements for the specific shared creation/cleanup boundary identified by independent review, under `recovery/revision-dispatch.md`; no source edit grant. Actual DownloadHelper:34–59 launches async-void DownloadVideo and can finish while :199 CreateFileAsync is pending, before operation/GUID metadata publication at :200–220. StartAsync at :224 lasts for media transfer, so awaiting that whole method under a preparation gate would be an incorrect lifetime change. Root permits narrowly owned preparation/path coordination in DownloadHelper and minimal DownloadDialog wiring, plus safe coordination of the already owned DownloadPage permanent-delete handlers. A point-in-time VM scan is insufficient; the stable revised contract and actual-creator race evidence are recorded below. Existing17-check/13-red/four-control evidence remains authoritative and unchanged.

### Stable P1-RV1 revision / source-bound receipt

Read [review.md](review.md), [revision-dispatch.md](revision-dispatch.md) including root's final bounded-revision clarification, and the root [inventory-decision.md](../inventory-decision.md). The reviewer established the pre-GUID failure independently; that and the original13 reds are sufficient to authorize this contract revision. No coordinator or permanent probe was implemented. Existing scratch was extended only to confirm allowed actual-source race orders; implementation/green remains assigned to the future executor after re-review.

Independently reran the reviewer's receipt:

```bash
/workspace/work/maintenance-foundation/quality/dotnet/dotnet run \
  --project /workspace/work/recovery-integrity/recovery-review-creation/CreationReview.csproj -c Debug
# exit 0
# AFTER_ACTUAL_ADDDOWNLOAD returned=true pending_video=True current_operations=0
# AFTER_PLATFORM_FILE_COMPLETION current_operations=1 active_path=/videos/哔哩哔哩下载/av1/cid1/video.m4s
```

`sha256sum` of the reviewer's full DownloadHelper copy and the actual repository file both equals `c065e3310d5fe7398b8bd6b04fe7b10778c280615959ccbb032ef172994b9e86`. Actual AddDownload awaits auxiliary preparation/SaveInfo but does not await the private async-void video preparation. StartAsync's awaited completion is inside that same private method. Converting it to Task and simply awaiting the entire method would therefore fix pre-GUID ordering by incorrectly blocking for transfer lifetime; the revised contract explicitly splits those phases.

Additional bounded scratch: `/workspace/work/recovery-integrity/recovery/creator-races/CreatorRaces.csproj`. It compiles full unchanged actual DownloadHelper/IModules/ReturnModel plus exact unchanged DownloadVM LoadDownloading, Handel, DownloadProgress, GetProgress, RemoveItem, DeleteItem, DeleteAll and two item-model bodies. The scaffold supplies fields/command adapter and a no-op completed-library refresh outside the observed race. Only primitive storage/dialog/settings/transport/BackgroundTransfer surfaces are substituted. No path-reservation algorithm, gate or ownership business rule was added to the fake or to product code.

```bash
python3 /workspace/work/recovery-integrity/recovery/creator-races/verify-source.py
# CREATOR_SOURCE_BINDING_PASS 4 source hashes; full actual DownloadHelper/IModules/ReturnModel;
# 9 exact DownloadVM method/model bodies

/workspace/work/maintenance-foundation/quality/dotnet/dotnet run \
  --project /workspace/work/recovery-integrity/recovery/creator-races/CreatorRaces.csproj -c Debug \
  > /workspace/work/recovery-integrity/recovery/creator-races/red.log 2>&1
# exit 1; compiled successfully; checks=6 failures=5
```

The assertions produce:

```text
RED actual AddDownload awaits child preparation until GUID publication; actual completed=True
RED same-path pre-GUID creator excludes cleanup; current_new_guids=0,delete_during_pending_creator=True
PASS media-transfer lifetime is outside AddDownload completion; start remains pending
RED creator cannot enter after cleanup lookup begins; actual active_new_guid_at_delete=True
RED creator cannot mutate same path during pending DeleteAsync; actual new_guid_active=True
RED new same-CID GUID attached during confirmation is excluded from old cancellation; actual new_token_signaled=True
CREATOR_RACES checks=6 failures=5
```

Race scheduling is explicit: hold actual helper video CreateFileAsync before GUID publication; hold actual delete's folder lookup then admit the actual creator; hold actual DeleteAsync then admit the creator; hold actual confirmation, publish a new same-CID operation via actual AddDownload, attach via actual LoadDownloading, then accept the old command. These primitive schedules prove missing source exclusion and cancellation-token reach. They do not simulate Windows folder-lock semantics or establish that a real Windows delete would succeed in these orders. The pending StartAsync control protects the intended lifetime boundary rather than claiming preparation is already correct.

Rechecked the original source bindings and original frozen assertion program:

```bash
python3 /workspace/work/recovery-integrity/recovery/verify-source.py
# SOURCE_BINDING_PASS 12 source hashes; 6 full files; 25 exact unchanged class/method bodies
/workspace/work/maintenance-foundation/quality/dotnet/dotnet run --no-build \
  --project /workspace/work/recovery-integrity/recovery/RecoveryDiscovery.csproj -c Debug \
  > /workspace/work/recovery-integrity/recovery/revision-original-red.log 2>&1
# exit 1; unchanged 17 checks / 13 genuine reds / four preserved controls
```

These are two scratch experiments, not two new permanent executable stages. Root still approves **one** recovery program/stage; current inventory becomes51 logical/52 executable only when that program is integrated by the designated executor. The original13 reds/four controls and all existing probe assertions remain mandatory. The new five race/preparation reds and transfer-lifetime control provide additional source evidence; the actual creator/coordinator/page cases must be integrated into that one program without inventing a fake ownership algorithm.

### Exact repair contract and boundary closure

Revised requirements §3 D5 now makes creator admission precede the first parent/episode mutation for the captured episode path, holds ownership through all awaited child preparation/operation+GUID/manifest publication or settled failure, and requires StartAsync dispatch before release while detaching its completion lifetime. The public AddDownload Task signature, filenames, metadata formats/keys, headers/group/network policies and optional auxiliary tolerance stay compatible. Required preparation failures flow to the existing DownloadDialog catch; already published siblings remain discoverable with truthful partial results. Minimal caller compatibility wiring is permitted if needed, not a dialog/transfer rewrite.

The same product coordinator serves coherent pre-confirmation snapshot and confirmed/retried destructive ownership. No lease is held across the dialog. The final lease spans generation/owner reconciliation, cancellation and awaited lookup/deletion; a new creator cannot enter the check/act gap. Pending creator, changed admitted-creation revision, unknown metadata or failed reconciliation fails that target closed before cancellation. The creation revision also catches a new creator whose operation has already finished/failed before the confirmation resumes. Actual GUID observers own stable independent cancellation sources, so adding a same-CID GUID does not enlarge an older confirmation's cancellation reach. Different episode paths remain usable and every failure releases logical ownership. A typed busy result may safely refuse same-path preparation before mutation; no global transfer scheduler or media-lifetime gate is introduced.

Independently searched all actual creation/destructive entry points:

```text
rg -n 'new BackgroundDownloader|CreateDownload\(|\.DeleteAsync\(StorageDeleteOption.PermanentDelete\)' src/BiliLite.UWP --glob '*.cs'
Helpers/DownloadHelper.cs:175,200       sole BackgroundDownloader/CreateDownload creator
Modules/DownloadVM.cs:665,708          task single/bulk permanent delete
Pages/DownloadPage.xaml.cs:296,355     completed episode/title permanent delete
```

Both completed-delete handlers require the same safe coordination: episode scope for one episode, subtree scope for the whole video/season directory. The latter must exclude a new child absent from its historical Episodes list. Their current confirmation, declined no-mutation, storage-error retention and success/list-removal behavior remain preserved; neither confirmation authorizes cancelling an overlapping running task, so such conflicts fail closed without cancellation. Inspected DownloadPage's other named delete buttons (:496,:511): they only remove selection-list entries. GenerateButton (:451–454) immediately reports undeveloped and returns. OutputFile's SRT conversion is not another PermanentDelete bypass; no import/export redesign is added.

Root-approved future product scope now explicitly includes actual DownloadHelper preparation/path coordination, a tiny feature coordinator with UWP Compile entry, both already owned DownloadPage destructive handlers and only necessary DownloadDialog compatibility wiring. Requirement acceptance8–11 covers all three reviewer race orders (lookup and delete separately), cleanup retry, no GUID yet, new same-CID GUID during confirmation, distinct-path progress, descendant scope, failed/unknown reconciliation, failure release and transfer lifetime outside ownership. Permanent seam must compile actual AddDownload/GetDownloadFolder/auxiliary helpers/SaveInfo/new video-preparation and detached observer methods plus actual coordinator and actual page deletion methods, using exact current-source extraction only if full linkage is impractical.

Product/tests/build/main-doc/index/history remain untouched; scoped `git diff --name-only -- src plan/tests` is empty and HEAD remains `077ef35ae194d3ae85aa0621daeac07dc29e29ab`. Windows/native limitations from the original evidence remain unchanged. The proposed shared registry protects cooperating in-process application paths; it makes no claim to exclude arbitrary external programs or pass native file-handle/rendering tests. Stable revised requirements are ready for the same independent reviewer; no further discovery or prototype is required to start that review.
