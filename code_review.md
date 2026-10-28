# Review of `315f626`

Read `bug_report.md` after pulling `origin/work`. Compared source with sibling `ywmoyue/biliuwp-lite` at `ca2caddc597b3faf05b4dac6dcb226aa0694365b`. Three GPT-6-luna reviewers assisted. No application code changed; no API requests, builds, tests, restore, or runtime validation. The current user instruction allows format checks only and overrides the report's build/verification instructions.

## BR-01 — Approve, reuse the existing card template

Map `DynMedialist` to `major.medialist`, add its `MapCardType`/`ParseType2024` cases, and supply cover, title, subtitle, ID and source URI to the existing `OneRowTemplate`. A new template is unnecessary: `Controls/Dynamic/DynamicItemDataTemplateSelector.cs:45-54` already handles MediaList.

Distinguish medialists from UGC collections in `DynamicParse.cs:357-369`: use the sibling's `收藏夹` tag, not `合集`. Otherwise `Pages/Home/UserDynamicPage.xaml.cs:132-139` sends a video-comment request with the missing UGC archive AID. Keep medialist comments on the dynamic ID, matching the sibling's legacy medialist routing (`Models/Common/UserDynamic/UserDynamicParseOneRowInfoActionsMap.cs:146-163`). Preserve UGC collection behavior.

Drop explicit AD/banner cards. Use a compact unsupported placeholder for COURSES_SEASON, COURSES_BATCH, COMMON_SQUARE, APPLET and other unmapped families; remove JSON dumps in both conversion paths (`Modules/User/DynamicVM.cs:693,904-918`). Keep an unmapped repost origin as a placeholder so its parent remains readable. Filter before generic unsupported-type normalization. Use the server's `HasMore` and offsets even when filtering reduces the visible count; an empty mapped page does not establish end-of-feed.

## BR-02 — Reject the empty-card diagnosis; approve one small mapping fix

Music already uses `OneRowTemplate`. Its title and cover come through the article accessors (`Modules/Home/DynamicVM.cs:303-320,371-382`); `GetCommentId` supplies the song ID (`Helpers/DynamicGrpcMapper.cs:341-364`), and `DynamicParse.cs:280-294` constructs the song link. Comments already use Song. The shared DTO is awkward but works; no dedicated music DTO or Article reclassification is needed.

The actual mismatch is `DynMusic.Label1` being stored in `article.desc` (`DynamicGrpcMapper.cs:519-528`), while the displayed subtitle reads `article.label` through `viewtext` (`Modules/Home/DynamicVM.cs:429-440`). Map the display label there instead, retaining song navigation/comments. This fixes the fabricated `播放:0` when a source label is available.

## BR-03 — Approve option 3

Mark only the synthetic follow-season cards and hide their author header and interaction row. Keep cover/title/subtitle and navigation through `FollowListItem.Url`. Do not request per-card details, invent a Season comment type, or treat season IDs as video IDs.

This matches the sibling's `CUSTOM_SEASON` cards (`ViewModels/UserDynamic/UserDynamicAllViewModel.cs:259-264`, `Controls/Dynamic/DynamicV2Template.xaml:745-779`). Ordinary PGC dynamics must retain their author and comment actions. The current card body already has a source URL; the missing episode AID does not make season navigation impossible.

## BR-04 — Approve, including the skipped-refresh fix

Use a request generation plus captured tab, checked before publishing items, cursors, `CanLoadMore`, failure state or errors. No per-tab cache is needed.

Crucially, `Refresh()` currently returns while `Loading` (`Modules/User/DynamicVM.cs:131-145`). A tab switch changes the type but never starts its request. Increment the generation and queue the latest requested tab refresh even while loading; after the old request finishes, discard its result and load the latest selection. Use captured request values after awaits, including the Season/Video branch at `:464`. Keep loading completion tied to the owning request so an older completion cannot end a newer load.

## BR-05 — Approve (a) and (b); skip (c) as cleanup

Register `LoadMoreBottomOffset` with `typeof(MyListView)` (`Controls/MyListView.cs:25-26`). Add the short-viewport load trigger after population/layout for every paginated MyListView, using `CanLoadMore` and the VM's existing in-flight/failure state. Trigger again after a completed page when the viewport remains unfilled; stop when the server ends pagination or a request fails.

The sibling grid supplies the no-scroll loading pattern (`Controls/MyAdaptiveGridView.cs`), but its list has the same owner mismatch. Making `CanLoadMore` a dependency property is optional only if needed to implement the trigger; compiled `x:Bind` already sets the CLR property. Consistency alone is not worth a separate change.

## BR-06 — Reject the speculative endpoint fallback

Accept absent optional chrome for now. The sibling maps these same fields from `/x/v2/space` (`ViewModels/User/UserDetailViewModel.cs:121-161`); it supplies no evidence that a web-card fallback is needed or would repair the alleged omission. Missing fields are not proof that a live account's badges are mapped incorrectly. No extra request or fabricated status values.

## BR-07 — Defer; no code change

The sibling subtitle model has the same four fields (`Modules/Player/PlayerVM.cs:343-349`) and uses label heuristics. It does not establish an `ai_type`/`is_ai` contract. Keep the current heuristic and document unmarked AI tracks as a limitation. This does not claim the endpoint never returns such metadata; its name and meaning remain unestablished by the permitted reference.

## BR-08 — Approve login/error handling; reject empty-success shortcut and REST fallback

The new report's measurements were supplied by the other environment, not reproduced here. They describe rejected requests, not successful empty feeds. Source confirms that `GrpcRequest.cs:108-129` logs HTTP status but ignores it when deciding success and discards server status metadata. Preserve HTTP failure and any exposed `grpc-status`, `grpc-message` and `bili-status-code` when constructing errors; do not replace rejection with successful emptiness.

The project targets `netstandard2.0`. Neither checkout supplies a compatible trailer-reading implementation, so a patch relying directly on `HttpResponseMessage.TrailingHeaders` is not approved. A transport replacement solely to retrieve unavailable trailers is deferred; retain a clear unknown-response failure when metadata cannot be obtained. The current source cannot establish the exact server cause from body length alone.

Approve a login prompt before requesting these personalized feeds when the app has no access key; cancellation should leave a login-required state rather than a transport error. A nonempty but rejected key still needs the error/retry path. This is the chosen feed UX, not a claim that source proves the endpoint's entire authentication contract.

Reject returning `Array.Empty<byte>()` merely because HTTP is 200 or the body is short. Even a parsed empty `DynAllReply` hits `DynamicList == null` and throws in `Modules/User/DynamicVM.cs:428-432`. A valid list with zero entries is already handled; a missing response list is not equivalent.

Keep gRPC failure retryable through Refresh, with a visible load-failed/login-required state when no items exist. The existing catch already logs and toasts (`:593-599`), and Refresh resets `lastPageFailed`; the page's `count=0` log does not mean silent success. Retain already loaded items and cursors on a failed load-more. Do not add REST fallback: the sibling does not establish interchangeable gRPC offsets/baselines and legacy REST history cursors.
