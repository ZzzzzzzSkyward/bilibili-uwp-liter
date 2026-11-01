# Collection and playlist navigation — remaining work

Updated 2026-11-01 for `dev`. The planned flow shipped through P2a; everything finished is one line in
`git log`. This file keeps only what is still open plus the invariants later batches must not break.

## Shipped

| Batch | Commits | Result |
| --- | --- | --- |
| P0 identity and counts | `67b9eae` | Watch Later resolves the clicked aid inside the filtered queue; an unknown remote total stays unknown |
| P1a one sequence state | `399807d`, `6c9b39c` | `WatchSequence*` decides next/previous/completion for parts and outer rows; the player's part index and its selection list cannot disagree |
| P1b 目录 drawer | `e9e5d2a`, `18c519d` | one drawer inside the player (full screen included), stable row keys, markers built from the actually playing aid, locate stays in the browsed section |
| Copy | `d905726`, `f68b705` | labels state the fact, icons carry their own word (`bug_report.md` BR-31) |
| P1c boundaries and paging | `1dd359c` | `自动连播` gates both boundaries, `PlayTargetStart` fixes the resume policy, one single-flight page request shared by footer, boundary and drawer |
| P1d transition UX | `da62afc` | pending target, retry-and-play-next, ticket-based rejection of late results, drawer keyboard/accessibility |
| P2a search and identity | `4db379c` | activation resolves a row's `Key` into the source order; the drawer searches the rows it already holds |

## Invariants later batches must keep

- The page owns every decision; `PlayerControl` forwards events and hosts the drawer; `PlayDirectoryControl`
  renders and raises events. Neither control plays, pages, or changes the continuation source.
- A row's identity is `(Key, kind)`; `Ordinal` is display only. Paging appends to a stable collection.
- One paging channel (`MediaListPageLoader`) — a second request never starts while one is in flight.
- One pending transition at a time; a result is accepted only for the video id, media list id and
  generation it started under.
- Copy follows BR-31: no `（...）`, no `：`-suffix explanation, no `·`-joined restatement, no icon
  repeating the adjacent label.

## Open

| Batch | Work | Done when |
| --- | --- | --- |
| P2b long-list cost | `加载全部` with visible progress and cancel over the same channel; the drawer's row list is the only scroller, bounded so the footer stays reachable at 200+ rows, and a page landing appends without rebuilding the list or jumping the scroll position | later items are reachable without a hidden page tab, and a long list stays cheap |
| Season and entry points | adapt `Pages/SeasonDetailPage.xaml(.cs)` through its existing episode/preview state so the same 目录 entry works without mixing 正片 and 预告; carry readable titles from Watch Later/Favorites/launcher into queue headers; consider a progressive start for Favorites instead of its eager `全部播放` | the directory entry behaves the same for seasons, and a queue header says what the user is browsing |
| BR-32 | cancel the saved advance when the continuation source changes (or execute its saved source/row) | a retry can never advance a list the user switched away from |

## Not claimed

- BR-21 (memory growth) is still unmeasured. Bounded lists and virtualization are design constraints,
  not evidence that this feature caused the growth.
- Interactive-video nodes, local-media tracks and A-B playback keep their own precedence; nothing here
  changes them.
