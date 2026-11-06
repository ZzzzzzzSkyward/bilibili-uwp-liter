# Review origin/dev cfbe5c2353a85ac76444824a3bfde8e7be1f4983

Reviewed all new source changes since `2fdcb8a58f6b35274346019e343f1a7ebb012a70`, including Luna reviews of dynamic cards and the player. Static source inspection only; no tests, builds, linters, application execution, or runtime validation. The previous sequential page-reload ordering finding is fixed.

## Images after the ninth tile are still laid out outside the grid's height

Location: `src/BiliLite.UWP/Controls/Dynamic/DynamicImageGrid.xaml.cs:86-88`; `src/BiliLite.UWP/Controls/Dynamic/DynamicImageGridPanel.cs:26-37`.

Trigger: display a dynamic with 12 valid images. The layout calculates nine visible tiles and puts `+3` on the ninth, but `Rebuild()` binds all 12 tiles. The panel then arranges every child while reporting only the nine-tile height. At width 480, that height is 480, but images 10–12 start at Y=486. Those images are outside the space reserved for the grid, so they can overlap following content or be clipped; the promised collapsed images also retain visual elements and image loads.

Suggested fix: bind only the first `layout.VisibleCount` tiles, while computing overflow from the original count. Keep each retained image's full `AllImages` list and original index so the viewer can still open all images. Make the panel arrange only the tiles it budgets space for.

## Paging during an earlier-page reload can consume the wrong checkpoint

Location: `src/BiliLite.UWP/Services/DynamicFeedWindow.cs:230-231`; caller at `src/BiliLite.UWP/Modules/User/DynamicVM.cs:1572`, `:1588`, and `:1600`.

Trigger: after loading 12 pages, start reloading evicted page 2. While that request is pending, scroll down and let page 13 load, with the scroll anchor away from page 3. Normal paging can evict page 3 because `LoadMore()` and `ReloadEarlier()` use separate in-flight guards. When the page-2 response returns, `TryTakeCheckpoint()` removes the now-last checkpoint, page 3, and inserts page-2 items with page-3 cursors and page number. Page 3 loses its reload checkpoint, page 2 keeps its stale one, and subsequent reloads can leave missing history or stop on duplicate items.

Suggested fix: coordinate normal paging and earlier-page reloads so eviction cannot change the checkpoint between peek and take, or verify that the requested checkpoint is still the adjacent one before accepting its response. If it changed, retain both checkpoints and retry from the new window edge. Do not consume whichever checkpoint happens to be last after the await.

## About-page source and release links use the wrong repository name

Location: `src/BiliLite.UWP/Assets/Text/info.md:7` and `:11`.

Trigger: open Settings → About and click the source or Releases link. Both now pass through the new link handler, but their targets use `ZzzzzzzSkyward/biliuwp-liter`; origin and `README.md:30` identify this project as `ZzzzzzzSkyward/bilibili-uwp-liter`. The app therefore directs users looking for its source or installation packages to a different repository address.

Suggested fix: use `https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter` and its `/releases` URL for both the displayed source address and link destinations. This mismatch is established from repository text; no network link check was performed.

## Dispositions (2026-11-06, dispatcher, review `a27125d`)

All three findings were correct; none is contested.

1. **Tiles past the ninth were laid out outside the grid's height.** `DynamicImageGrid.Rebuild` computed the layout from
   the full count (correct for columns and `+N`) and then bound every tile, while `DynamicImageGridPanel` measured and
   arranged `Children.Count` children and returned a height for nine. With twelve images the last three were arranged
   below the reported height. Fixed in `1410710`: the control hands the panel only `layout.VisibleCount` tiles
   (`GetRange(0, VisibleCount)`), the badge is placed on `VisibleCount - 1` from the full-count layout, and the panel
   loops over `Math.Min(Children.Count, layout.VisibleCount)` so a future over-binding cannot escape the budget either.
   The dropped tiles keep their `ImagesInfo` entries, so the viewer still opens the full set at the right index.
2. **Paging during an earlier-page reload consumed the wrong checkpoint.** `ReloadEarlier` peeked, awaited, then took
   "whatever was last", which `LoadMore` may have replaced in the meantime, and reinserted the page under another page's
   number and cursors. Fixed in `bdc837d`: `TryTakeCheckpoint(expected)` removes only the checkpoint whose page number and
   before-cursor match the request; `ReturnCheckpoint` puts it back in page order (appending would make it the newest
   eviction and jump the queue). The VM now takes before the request and returns it on every early exit — wrong
   generation, empty page, all-duplicates, or exception — so a failed reload keeps both checkpoints.
3. **About links used the wrong repository name.** Correct, and my mistake: `info.md` pointed at
   `ZzzzzzzSkyward/biliuwp-liter` while `origin` and `README.md` say `bilibili-uwp-liter`. Fixed in `9b3dda2`.

Probe coverage: `g5` 202 → 207 (grid wiring), `g3` 366 → 380 (checkpoint identity, ordered return, idempotent return).
Compiled into `BiliLite.Packages_1.0.9.51_x64.msix`; no device run, per the maintainer's 2026-11-04 default-pass rule.
