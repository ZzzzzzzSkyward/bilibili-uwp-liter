# Collection and playlist navigation — UI/UX plan

Updated 2026-10-31. Source reviewed: `46ece663f4629c398c93e9dc9ec7cf52a80c99ff` on `origin/work`.
Read the current `bug_report.md`, including BR-21. This is a source-based feature review and an open
implementation plan. No application execution, tests, builds, package inspection, or device validation
were performed. Existing C1/C2/C3 behavior remains the baseline; this plan does not reopen accepted
fixes or claim a measured memory problem.

## Outcome

While watching, users should be able to see what list they are following, see what plays next, switch
to another episode, and browse the complete available list without leaving full screen. A video part
(`分P`), a video within an uploader's collection (`合集`), and a row in an external playback queue
(`播放列表`) must stay distinguishable.

Start with ordinary UGC video details, uploader collections, remote medialists, and the queues passed
by Watch Later and Favorites. Keep the existing season/preview player working; give it the same list
entry in a later compatibility pass. Interactive-video nodes and local-media tracks keep their own
controls. No player-engine rewrite, account-wide queue syncing, or new playlist endpoint is needed.

## Current behavior and worthwhile gaps

File references below are relative to `src/BiliLite.UWP/` and refer to the reviewed source.

| Area | Source evidence | User effect / planning decision |
| --- | --- | --- |
| Collection and playlist continuation already works | `Pages/VideoDetailPage.xaml.cs:1012–1109`; `Models/VideoCollections.cs:124–160` | Parts finish first; explicit collection selection makes the collection the continuation source; otherwise the opening queue wins, with collection fallback when no queue exists. Preserve section order, queue order, independent remote cursors, and existing failed-page retry. |
| Manual next only means the next part | `Controls/PlayerControl.xaml.cs:1096–1105`, `2673–2675`, `550–578` | On the last or only part the next button disappears, and the keyboard shortcut says “最后一P”, even when the collection or playlist has another video. Automatic completion can continue where manual next cannot. |
| The full outer list disappears in theater/full-screen modes | `Pages/VideoDetailPage.xaml.cs:846–873`; `Controls/PlayerControl.xaml.cs:925–945`, `2615–2621` | The detail column is hidden; the player's “选集” opens only `PlayInfos` for the current video. A single-part collection video may have no useful list button at all. |
| The continuation source is implicit | `Pages/VideoDetailPage.xaml.cs:480–498`, `990–1037` | Clicking a collection row changes future continuation; clicking a queue row changes it back. Merely inspecting the UI does not reveal this distinction. Selecting the already-current collection row returns before changing the source. |
| Long lists lack a way back to the current item | `Pages/VideoDetailPage.xaml.cs:367–465`, `522–600`; `Pages/VideoDetailPage.xaml:9–75` | Collection rows have metadata and current selection, but no ordinal/count/search/current-item action. Collection content is rebuilt after a video change. Remote lists have loaded-count text, while in-memory queues lack a corresponding summary. |
| Unknown remote totals can look complete | `Models/MediaListResources.cs:239–245`, `277–279` | `Append` substitutes loaded count for an absent total, so a page with more results can display “已加载 20 / 20”. Keep unknown total distinct from loaded count. |
| Selection during a transition has no acknowledgement | `Pages/VideoDetailPage.xaml.cs:480–485`, `990–995` | Selection events return while details load, after the control may already have moved its highlight. Add an explicit pending state and prevent conflicting playback activation. |
| A filtered Watch Later queue uses the wrong starting index | `Pages/User/WatchlaterPage.xaml.cs:37–58`; `Pages/VideoDetailPage.xaml.cs:236–243` | The queue removes “已失效视频”, then uses the clicked row's index in the original list. An invalid row before the clicked video can open a different video or produce an out-of-range index. Resolve the clicked aid within the filtered queue. |
| Keyboard browsing needs to stop starting playback accidentally | `Pages/VideoDetailPage.xaml.cs:893–900`; collection/queue `SelectionChanged` handlers above | The part grid consumes every preview key; collection/queue selection also starts playback. Separate keyboard focus/browsing from activation rather than simply removing key suppression. |
| Playback mode labels describe different scopes poorly | `Controls/PlayerControl.xaml:392–400`; `.xaml.cs:2805–2882` | “列表循环” loops the current `PlayInfos`, not the outer playlist; the “自动播放下一P” setting is checked between parts but not before the outer completion event. Specify the scope before exposing these controls beside the queue. |

## Proposed watching flow

### One list entry in every viewing mode

Use one player action, **目录**, visible whenever there is a collection, a queue, multiple parts, or a
season episode list. Its compact caption can show `合集 · 7/24` or `播放列表 · 第7项`; its accessible
name says “打开播放目录”. A single unrelated one-part video does not need an empty drawer.

In a normal wide window, this opens/selects the existing detail-side list area. In full window, full
screen, or a narrow layout, it opens an overlay drawer inside the player. Use the same backing state
and row templates in both hosts; host separate view instances rather than attaching one XAML element
to two parents. Opening a list never stops playback or changes full-screen mode.

Show only applicable scopes: `当前视频分P`, `合集`, and `播放列表`. A season page can supply `正片` and
`预告/花絮` without treating them as UGC collection rows. The drawer opens on the active outer source,
or parts when there is no outer source. Switching its scope only browses; it does not change playback
or the continuation source.

The header shows the source title, current position, total/loaded status, and **定位正在播放**. Keep a
small visible line above the transport controls: `连播来源：合集《…》 · 接下来：…`. When both a queue
and a collection exist, expose `按合集连播` / `按播放列表连播`; choosing a source containing the current
video does not restart it. If the viewed source does not contain it, choose a row to start that source
rather than guessing a position. Browsing alone never switches the source.

Example of the remote-playlist drawer while playing a multipart video:

```text
目录                                             关闭
[当前视频分P]  [合集]  [播放列表 · 连播中]
课程播放列表                 第7项 · 共120项
[搜索已加载的20项…]             [定位正在播放]
  06  上一讲                       12:30
▶ 07  当前讲        正在播放 P2/3   18:40
  08  下一讲                       15:20
  …
[加载更多]                   已加载20 / 共120项
接下来：P3 总结
[上一P] [下一P]        [下一个视频（跳过剩余分P）]
```

### Predictable next and previous

Keep an explicit distinction between advancing within a video and skipping to another video.

| Action | Behavior |
| --- | --- |
| Primary next, button or existing next shortcut | Next part if one remains; otherwise next video in the active outer source; otherwise a clear end state. Tooltip/accessible name names the target: `下一P：…`, `下一个视频：…`, or `下一集：…` on a season page. |
| `下一个视频` in the source header/next-button menu | Skip remaining parts and advance the active collection/queue immediately. Hide this action when there is no outer sequence. |
| Previous | Previous part when available; otherwise previous outer row. This is sequence navigation, not navigation through browsing history. Mirror the existing previous shortcuts and expose a touch-accessible action in the drawer. |
| Pick a row | Activate that row by stable identity; keep the outer queue intact. Picking a collection/queue row explicitly chooses that continuation source. Picking a part keeps the current outer source. |
| End of source | Show `已到合集末尾` / `已到播放列表末尾`, retain the current item, and offer `查看完整目录` and `重播当前视频`. Do not silently switch to the other list or to recommendations. |

Manual next is available even with automatic continuation off. Rename the automatic setting to
`自动连播（分P及下一视频）` and apply it before automatic advancement at either boundary. Preserve an
existing false `AUTO_NEXT` preference as false; this intentionally closes the current mismatch at
the final part. Do not gate manual activation on that setting. Keep `单集循环` scoped to the current
part/episode, and rename the existing list-loop mode to `本视频分P循环` on UGC pages. Adding a loop over
an entire remote playlist is deferred; it must not be implied by the present label. A-B playback and
interactive-node choices retain their existing precedence.

Specify start position rather than inheriting it accidentally: direct opening or a normal row click
can keep the existing resume behavior, with `从头播放` as an explicit row action. Moving to another
video with Next or automatic sequential continuation starts at its first playable part at 0; Previous
can resume the previous video's saved position. Moving between parts starts the chosen part at 0.
Keep this policy visible in implementation acceptance because `InitializeVideo` currently restores
the target video's saved part (`Pages/VideoDetailPage.xaml.cs:336–347`).

### A full list users can actually explore

- Collection rows retain section headings, cover, title, author and duration, and add ordinal plus a
  textual `正在播放` marker. Show both video position and part position, e.g. `合集 7/24 · P2/3`.
  Keep keyboard focus, pending selection, and actually playing row visually distinct.
- Fixed in-memory queues show `共 N 个可播放视频`. Favorites' `全部播放` currently reads all pages before
  creating this queue (`Pages/User/FavoriteDetailPage.xaml.cs:146–181`); do not describe it as a
  pageable medialist or silently substitute a Favorites id for a medialist id.
- Remote queues show `第7项 · 已加载20项 · 共120项` only when the server supplied a reliable total.
  Otherwise show `第7项 · 已加载20项 · 总数未知`; only mark `已全部加载` when paging is exhausted.
  Retain a known server total when a later page omits it, keeping counts honest if items disappear.
- Add search over **已加载条目** and a collection-section selector. Keep source order and stable item
  identity when filtering. Search results never become a reordered autoplay queue. A blank result
  says `已加载条目中没有结果`; it must not claim an unloaded remote item does not exist.
- Put `加载更多` / `重试加载` in the list footer, reachable after the last loaded row; retain it with
  empty search results when more pages exist. Display a spinner and disable duplicate page requests.
  An explicit P2 action, `加载全部目录`, may fetch sequentially with progress and Cancel. Start with the
  existing page size, reuse the same cursor path, and stop on end, failure, cancellation, or no cursor
  progress. Opening the directory never starts an unbounded download.
- Locate and reveal the current row on first opening and on **定位正在播放**. Preserve scroll position,
  filter and source tab while browsing; do not force-scroll on every automatic transition. When the
  current row is outside the viewport/filter, retain its summary and offer a locate action that clears
  the filter explicitly. Avoid rebuilding every collection row on each episode change.

## Loading, failure, and input behavior

One page-owned navigation operation determines the next target, whether requested by a button,
shortcut, row activation, or completion. During a switch, show `正在切换到…`, allow browsing, and disable
further playback activations until the operation completes. A pending row is not yet the playing row.
On failure retain the last committed identity/source/queue position and offer retry; do not highlight
a failed target as playing. Late results from a closed page or replaced context must be ignored.

Use one in-flight page load per queue/cursor, shared by footer loading and boundary continuation.
Do not let an automatic boundary request race a manual “加载更多”. Footer retry only loads rows and
does not unexpectedly start playback. A separate `重试并播放下一项` retries the pending continuation,
then advances exactly once if the original current item/source is still active. Keep the end/loading/
failed states visible in the drawer and player; a toast pointing to a hidden page tab is insufficient.

For keyboard users, Tab reaches directory/next/actions; arrow keys move list focus without playback,
Enter/Space activates, and Escape closes the drawer before it exits full screen. Restore focus to the
opener. Suppress player shortcuts while focus is in the search field or browsing the list. Retain
single-click/tap row activation with a practical 44px minimum target. Announce current row, position,
loading and failure through automation properties; use text/icon as well as highlight color.

## Implementation batches

Keep each batch usable on its own. This change is planning only; these batches describe later code work.

| Priority / batch | Concrete work and locations | Completion criterion |
| --- | --- | --- |
| P0 — correct identity and counts | Resolve Watch Later's initial row after filtering by aid; check missing/empty targets before indexing. Preserve remote known/unknown totals in `Models/MediaListResources.cs`. | An invalid row before the clicked item cannot change what opens; missing totals never render a false complete count. |
| P1a — shared navigation state | Add a small page-owned sequence state/controller near the detail VMs, with current aid/cid, stable row identity, active source, pending target and load state. Wrap existing collection order and queue helpers. Expose awaitable commands and a next-target preview to `PlayerControl`; keep queue fetching in the page/service layer. | Button, shortcut and completion resolve the same next target; a one-part collection video can advance manually. |
| P1b — directory in all modes | Extract reusable list presentation from `Pages/VideoDetailPage.xaml(.cs)`, bind separate normal/overlay hosts to the same state, and connect the player directory button. Add source name, position, locate-current and explicit source action. | Full collection/loaded queue is reachable without exiting full screen; list browsing alone never changes playback or source. |
| P1c — transition and boundary UX | Route row activation, next/previous and completion through the shared commands. Apply the automatic setting at both boundaries, use explicit resume policy, loading/failure/end states, single-flight page loading and accessible focus behavior. | No silently ignored activation, double advance, misleading playing highlight, or hidden retry instruction. |
| P2 — long-list browsing | Add loaded-item search, section selection, footer paging, cancellable sequential load-all, and bounded-height grouped/virtualized presentation. Keep rows/scroll stable across item changes. | Users can reach later items and recover from failure without losing their place; displaying a collection need not realize every row. No memory improvement is claimed without measurement. |
| P2 — season compatibility and entry points | Adapt `Pages/SeasonDetailPage.xaml(.cs)` through its existing episode/preview state; carry readable titles from Watch Later/Favorites/launcher into queue headers. Consider progressive Favorites start separately from its current eager `全部播放` path. | The same directory entry works for seasons without mixing preview order, UGC collection order, Favorites ids, or medialist cursors. |

The state/controller should describe the active sequence rather than turn every collection into a
second copy of `PlayInfos`. Keep `PlayInfos` as the playable parts/season episodes, existing queue
rows as video-level targets, and source-specific paging state separate. Stable identity must survive
search, appended pages, and display-host changes; display indices are derived values. UI highlight
synchronization must not trigger another playback command. Reuse existing services and theme tokens.

Coordinate late-result handling with the separate player session-ownership work in `PLAN.md`; this
feature needs page-level request identity and load guards, not a broad engine/resource refactor.
BR-21 remains an unmeasured memory report. Progressive lists and virtualization are sensible design
constraints, not evidence that this feature caused the reported growth.

## Future acceptance scenarios — not executed in this review

1. Open a one-part video in a multi-section collection, enter full screen, open the directory, locate
   the current row and manually advance across a section boundary without leaving full screen.
2. Play P2 of a three-part video: primary next goes to P3; `下一个视频` skips directly to the next outer
   row; the primary next at P3 resolves that same outer row. Its target preview matches the action.
3. Enter through a playlist, browse its video's collection, and close the drawer: continuation still
   follows the playlist. Explicitly choose the collection, then switch back using the visible source
   action without restarting the currently playing video or destroying either list.
4. Reach the last loaded remote row while more pages exist: display loading, append once, and play
   the first new target. Simultaneous footer activation shares the request. On failure the current
   video/rows/cursor remain; “重试加载” does not play, while “重试并播放下一项” advances once.
5. Read a remote page without `total_count`, then one with it, then another without it. Counts remain
   truthful, and a final page, duplicate-only page, cancellation or error cannot cause endless loading.
6. Filter a long list, play a result and clear search: source order/position are unchanged. Open/close
   the drawer in each viewing mode, retain browsing position, and locate a current row outside search.
7. Click a valid Watch Later item with invalid entries before it, including the last valid item; the
   same aid opens. Empty/all-invalid queues provide an empty state instead of indexing into no rows.
8. Turn automatic continuation off at both an intermediate part and final part. Playback stops in
   each case, while manual next still works. Single-part repeat and part-list repeat keep their stated
   scope and do not silently repeat or escape into an outer queue.
9. Use only keyboard input to browse, search, activate, locate current and dismiss; focus movement
   never starts a new video. Rapid input during loading gets visible feedback; failure or page close
   cannot commit a stale target. Repeat with touch and both light/dark themes.
10. A standalone video, canonical season with previews, interactive video, and local-media manifest
    retain their relevant controls. Directory availability does not expose an empty or unrelated list.
