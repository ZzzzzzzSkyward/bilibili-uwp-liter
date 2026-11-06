# Review origin/dev 492714ddbd3db39c7518f09252c1596b688ce524

Reviewed all new source changes since `cfbe5c2353a85ac76444824a3bfde8e7be1f4983`, with Luna and static source inspection only. No tests, builds, linters, application execution, or runtime validation.

Correction to review `a27125d`: its overlapping-paging checkpoint example overlooked `GetDynamicItems()` incrementing `requestGeneration` (`DynamicVM.cs:550`). The reload already discarded that response at `IsCurrentRequest` before consuming a checkpoint, so the claimed wrong-page metadata scenario was not established. The image-grid overflow and About-link findings are fixed in source.

## A discarded reload restores an old checkpoint into a refreshed feed

Location: `src/BiliLite.UWP/Modules/User/DynamicVM.cs:1580-1584`; the exception path at `:1619` also restores without checking whether the window was reset.

Trigger: after enough paging to evict pages, start “重新加载”, then refresh or switch feed category while that request is pending. `Refresh()` advances the generation and resets the shared `feedWindow` (`:227-245`). When the old response returns, the generation guard correctly discards its page, but the new `ReturnCheckpoint(held)` puts its old page number and cursor back into that reset window. A later feed publication updates the earlier-page hint from this contaminated checkpoint list. Reloading through that hint then replays an old cursor using the current feed kind/category; this can fail, insert stale history before the refreshed first page, or keep retrying a checkpoint whose rows are already present.

Suggested fix: give the feed window an epoch that changes on reset, capture it when reserving a checkpoint, and restore the checkpoint only if that epoch still matches, including in the catch path. Ordinary paging can change the request generation without resetting the window, so distinguish that case from refresh/category resets. Old-window checkpoints should be discarded after a reset.

## “打开原文” routes unsupported cards back through the same unsupported renderer

Location: `src/BiliLite.UWP/Controls/Dynamic/DynamicTemplate.xaml:148`.

Trigger: a dynamic has a type unsupported by both the modern and legacy converters. The new button invokes `DetailByIdCommand`, which opens the native `DynamicDetailPage` (`DynamicVM.cs:418-431`). Its gRPC detail attempt rejects an `Other` result and falls back to REST; when REST also has an unknown type, `ConvertToDisplay()` returns another `Other` model with the same ID and `DetailByIdCommand` (`:1955-1964`). The page uses the same template, so users get another unsupported placeholder with the same “打开原文” button instead of a usable original. Repeating the action repeats the same detail request/navigation.

Suggested fix: route this fallback action to the existing web-opening path using `DynamicUrl`, so a type the client cannot render can be viewed on its original page. Base the button's availability on that web command and the ID, and provide the command for both modern and legacy unsupported models. Keep native detail navigation for cards it can render.
||||||| parent of acbad3f (Guard the restored checkpoint by window epoch and open unknown cards on the web)
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

## Dispositions (2026-11-06, dispatcher, review `52a29f1`)

Both findings were correct. The review also withdrew part of `a27125d` (the checkpoint race was already covered by
`requestGeneration`); the identity-checked take added there stays, because it makes the contract explicit and the new
finding below showed the restore path still needed a guard.

1. **A discarded reload restored an old checkpoint into a refreshed feed.** Correct: the generation guard dropped the
   response, but `ReturnCheckpoint` still pushed the old page number and cursor into the window `Refresh()` had just
   reset. Fixed by giving `DynamicFeedWindow` an `Epoch` that increments in `Reset()`, capturing it when the checkpoint is
   taken, and restoring only when the epoch still matches (`ReturnCheckpoint(checkpoint, epoch)`); the catch path uses the
   same captured epoch. Ordinary paging changes `requestGeneration` but not the epoch, so a plain failed reload still
   puts its checkpoint back. `g3` 380 → 386.
2. **“打开原文” routed unsupported cards back through the same renderer.** Correct: `DetailByIdCommand` opens the native
   detail page, whose gRPC attempt rejects `Other`, falls back to REST, gets `Other` again, and renders the same
   placeholder. The action now uses `WebCommand` with `DynamicUrl`, and `CanOpenOriginal` requires that web command, so a
   card the client cannot render opens on `t.bilibili.com`. Both unsupported branches (modern and the legacy parse
   failure) provide the command. `g5` 207 → 208, `g3` pins that a native-detail command alone no longer shows the button.

Compiled into `BiliLite.Packages_1.0.9.53_x64.msix`; no device run, per the maintainer's 2026-11-04 default-pass rule.
