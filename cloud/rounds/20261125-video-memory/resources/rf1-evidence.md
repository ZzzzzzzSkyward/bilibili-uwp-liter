# RF1 evidence — no-item Update Danmaku

Status: **RELEASED for independent RF1 requirements review**. Normative amendment: [rf1-requirements.md](rf1-requirements.md). Source frozen284be; no product/test/tooling/main-doc/index/ref or active reviewer scratch mutation. This author performed source reads, complete-method scratch copies and hash checks only; no original audit command/project or unchanged suite was rerun.

## Source ownership and actual UI precondition

- `PlayerControl.xaml.cs:264` declares `CurrentPlayItem` as an auto-property with no initializer. Complete constructor388–427 creates services/timers/pivot state and never assigns a play item. Complete public initialization later selects an item through SetPlayItem. Thus null is a real initial state, not a fabricated native callback or terminal-flag reset.
- Constructor removes Node/LocalTracks pivot items; persistent DanmakuPivot remains. `TopBtnMore_Click:3579` uses a null-safe item test and opens SplitView. `PlayerControl.xaml:245` Update Danmaku is enabled by default and wired directly to `DanmuSettingUpdateDanmaku_Click:4070`. That complete handler awaits SetDanmaku(true), with no intervening ownership rule.
- **Source-supported exposed page:** `SeasonDetailPage.xaml:11–22` contains the player in `detail` without a Loading/Loaded visibility or disabled ancestor; its374 progress ring and365 centered error border do not establish whole-player input suppression. Initial `InitSeasonDetail:174–195` awaits the real VM; Detail null leaves public player initialization uncalled. `InitializePlayInfo:198–200` also returns for NothingPlay. Initial pending/failed/unavailable season can therefore leave the enabled no-item control. No page/UI redesign is required to make the control boundary safe.
- **Qualification to the original broad page paragraph:** `VideoDetailPage.xaml:79` does bind its whole content ancestor to `videoDetailVM.Loaded`, whose default is false (`VideoDetailVM:63`). The player is instantiated by XAML, but source alone does not establish an enabled before-first-load Video click while that ancestor is hidden. RF1 does not rely on that route or claim no loading ancestor exists for Video. The Season route plus the public control's actual default suffice; Windows pointer delivery remains unexecuted.

## Exact failure and helper choice

Current `IsPlaybackWorkCurrent:371–374` checks terminal/generation/reference only, so two null references match. Complete SetDanmaku2428+ accepts that result, then2433 dereferences `loadingItem.LocalPlayInfo` in the new initial/lease precheck **outside** try. The task faults and the actual async-void click posts NullReferenceException. Old8682 had its corresponding null dereference inside the existing try/catch, which contained the click exception. Neither a broad catch nor a fake initial media object is the desired ownership fix.

The helper's43 actual call sites (plus declaration) were reviewed/catalogued in private `current-work-callers.json`: quality/local-open/info, node/switch/current checks, subtitles, imports/pickers, engine/ended callbacks, danmaku tick/load/settled/error/finally and accepted replacement continuations. They authorize work **for an item**; none deliberately needs null to represent an active owner. A null-false predicate therefore directly expresses the accepted ownership contract and avoids the same no-owner dereference in other consumers without changing any non-null truth condition. An explicit early SetDanmaku guard is an acceptable narrower equivalent; do not add cid/manifest/media-availability validity checks unrelated to null ownership.

A helper change also makes null-owner error/finally publications ineligible. This is intended: absent media has no update result to publish. It does not suppress a legitimate current-item load/error or revive a disposed owner. Existing local/manifest items can have different address/id shapes and remain authorized solely by their live item/generation/reference, as before.

## Reused independently executed safe UI comparison

Fresh evidence: [empty-refresh.md](../residual-audit-evidence/empty-refresh.md). Its initial two-state log/hash paragraph is historical; the auditor subsequently captured a three-state same-fixture comparison in `/workspace/work/video-memory/fresh-residual-audit/comparison.json`. This author checked that comparator's exact source/input/log hashes and copied its receipt/log text to exclusive scratch, without invoking its driver or touching its active project.

| Same fixture state | Old8682 | Frozen284be |
| --- | --- | --- |
| Current item | Task complete,2 requests,0 unhandled errors | Same active positive. |
| Constructor-empty | Task complete,0 requests,0 unhandled errors | Task complete,0 requests,**1 NullReferenceException**. |
| Actual terminal close | Task complete,0 requests,0 unhandled errors | Same terminal control. |
| Desired-safe executable exit |0|**1**, SAFETY failures=1. |

Authoritative three-state log bindings:

- final284be `final.log`: `c3f78d6b35f35c92aa3e0262d795e614b91b108df882f68b148053e446c1ada1`.
- old8682 `old8682.log`: `884426873e4f8cccf00cdfbf806bbbebe84dad16ebb18a214897933335ffac2b`.
- unchanged actual click `RefreshEntry.g.cs`: `095f2597148f5646e346eace1005245b514cfd4eb9665babc09e864bd48f554c`.
- shared three-state driver `Main.cs`: `6af3c7c796da87284519420b838115c044ac37b717e63571cc40610a3d495d6c`.

Recorded comparator command: `python3 /workspace/work/video-memory/fresh-residual-audit/run-empty-comparison.py`; it enforces old8682 subprocess0 / final284be subprocess1 and identical fixture bytes, invoking `/workspace/work/maintenance-foundation/quality/dotnet/dotnet run --project /workspace/work/video-memory/fresh-residual-audit/empty-refresh/Check.csproj` after source-specific extraction. The wrapper's0 denotes the expected red/control pair, not repaired-source PASS. No repaired RF1 source or green run exists yet. Future acceptance must run unchanged final owning assertions on284be and repaired source; the no-unauthorized-publication contract is stronger than the old source's mere exception containment.

The fixture runs complete actual click and SetDanmaku/helper/load methods, with existing UI/network/media boundaries. Its synchronized ControlledContext records async-void completion and posted exceptions; the null public item assignment removes the fixture's synthetic initial item to match the product default, without changing terminal/generation flags. Actual Windows UI, XAML visibility delivery, network session and native memory were not executed.

## Binding, custody and retained limits

Exclusive `/workspace/work/video-memory/resources-rf1-requirements/` contains `input-bindings.json` (26 current frozen fixture/read-only dependency comparisons plus exact284be Control and page/XAML/contracts), `current-work-callers.json`, six complete inspected method copies and `reviewed-comparison.json` (exact comparator and raw logs). All26 comparisons passed. The comparator sources/input hashes and authoritative raw logs matched when read. No claim of stability for another reviewer's evolving Markdown/scratch is made; this author retained a self-contained read snapshot.

Only RF1 requirements/evidence are new outputs. Prior resource/residual/G1/M1 contracts/proofs remain frozen; all58 residual/39 lifetime/five graphics/original240 and season/tooling/API assertions remain required. No global player, native release, cache, SMTC/display, decoder, renderer or transport change is justified. Windows native/PID/private-commit/working-set/GPU/pixel/50-video evidence remains external. Custody released for independent RF1 review; root waits active reader release before any same-owner code grant.
