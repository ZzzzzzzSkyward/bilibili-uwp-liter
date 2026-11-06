# Review origin/dev 09ee4801aa06a2332ed2da0338217f25296be4b9

Reviewed all new source changes since `492714ddbd3db39c7518f09252c1596b688ce524`, with Luna reviews of dynamic cards and live-room loading. Static source inspection only; no tests, builds, linters, application execution, or runtime validation. The feed-window epoch fix addresses the stale-checkpoint finding.

## Live-room parse-failure guards still dereference null in their error branches

Location: `src/BiliLite.UWP/Modules/Detail/LiveRoomVM.cs:740` and `:765`; same pattern at `:812`/`:818`, `:856`/`:879`, and `:908`/`:915`.

Trigger: a successful HTTP response fails deserialization, so `GetData<T>()` returns null (`Helpers/HttpHelper.cs:619-628`). For example, `HasParsedData(data, "anchor")` logs the failure and returns false, but its `else` then evaluates `data.message`. The SuperChat, wallet, and nested bag-list branches do the same. They still throw the null-reference exception that the new guard was meant to avoid, and their local catches show `ex.Message` to the user. The outer loader's new stage catch does not receive those locally caught exceptions.

Suggested fix: separate null handling from a parsed API error: return/skip the optional step immediately when `HasParsedData` is false, then check `data.success` and read `data.message` only on a non-null model. Apply this to each listed helper.

BR-53: this leaves a concrete source path to the reported null-reference toast, despite the new diagnostics and guard. It does not establish which response caused the maintainer's actual report; the new per-step parse log can identify that response.

## Legacy unknown cards still lack the web command required by the new fallback

Location: `src/BiliLite.UWP/Controls/Dynamic/DisplayModel.cs:682`; missing assignment in `src/BiliLite.UWP/Modules/User/DynamicVM.cs:1956-1965`.

Trigger: REST feed/detail returns a type that `ConvertToDisplay()` maps to `Other` without throwing. That branch replaces the initially populated model with a fresh one containing `DetailByIdCommand` but no `WebCommand`. The new `CanOpenOriginal` predicate requires `WebCommand`, and both the fallback button and shared header's web action use it. Thus this ordinary legacy unsupported card has no working web escape; the fix only supplies the command for the modern unsupported branch and the legacy exception branch. This also affects the REST fallback reached when native detail cannot render a modern unknown type.

Suggested fix: assign `WebCommand = WebCommand` in the ordinary legacy `Other` return too, or preserve the initialized model's command fields when changing it into a placeholder. Keep the new web route and ID check.
||||||| parent of 2f874e1 (Return before reading a failed live response and give legacy unknown cards the web command)
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

## Dispositions (2026-11-06, dispatcher, review `8d496eb`)

Both findings were correct, and both were mine from the previous two batches.

1. **The live-room guards still dereferenced null in their error branches.** `HasParsedData(data, step) && data.success`
   short-circuits the condition but leaves the `else` to read `data.message`, which throws exactly the exception the guard
   was added to prevent. Fixed by splitting the two cases: `if (!HasParsedData(...)) return;` first, then
   `if (data.success) ... else <parsed error>`. Applied to SuperChat, wallet, bag-gifts, nested bag-list, anchor and
   gift-config; the nested gift-room branch already returned early. `g5` pins that no `HasParsedData(...) && ...` remains.
2. **The ordinary legacy unsupported branch had no web command.** `ConvertToDisplay` builds a fresh `Other` model for a
   type the legacy parser does not know, and it only carried `DetailByIdCommand`; with `CanOpenOriginal` requiring
   `WebCommand`, that card had no escape. Fixed by assigning `WebCommand = WebCommand` there too, so all three
   unsupported paths (modern, legacy parseable, legacy exception) offer the web route.

BR-53 is unchanged in status: the guards now cannot throw, and each failing step logs its own name. The device report still
needs that log line to name the response, which the maintainer has not sent yet.

Compiled into `BiliLite.Packages_1.0.9.57_x64.msix`; no device run, per the 2026-11-04 default-pass rule.
