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
