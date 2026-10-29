# Review — `d4c63459af705eb26a063f4659cd23074960d776`

Read `bug_report.md` and reviewed all new source changes since completed review `22f3a63` (source boundary `f84ab1be7fc4237b1e05d2f7af10447880dce05a`), with three GPT-6-luna reviewers. Source inspection only; no tests, builds, application execution, package inspection, or other validation.

## Photo details now send the wrong comment target

The new gRPC success path at `src/BiliLite.UWP/Modules/User/DynamicVM.cs:948-953` exposes the existing mapper's missing photo comment-ID mapping to the detail page. `DynamicGrpcMapper.GetCommentId` handles video/article/season/music but not Draw (`Helpers/DynamicGrpcMapper.cs:345-365`). A Draw card therefore gets `Extend.DynIdStr` as `basic.rid_str` at lines 304-313, even though the response carries the separate photo object ID in `DynDraw.Id` (already copied into `major.draw.id` at lines 479-483).

`ConvertToDisplay2024` passes that value through as ReplyID (`DynamicVM.cs:1278`), and `Pages/User/DynamicDetailPage.xaml.cs:38-44` selects the Photo comment endpoint (type 11) without replacing the ID. For a photo post whose dynamic ID differs from its photo object ID, opening comments from the newly migrated detail sends the dynamic ID as the photo-comment oid, so the correct comment thread cannot load. The previous REST detail converter preserved `desc.rid_str` (`DynamicVM.cs:1013`).

Add the Draw case to GetCommentId using `dynamic.DynDraw.Id`, so photo cards receive the photo object ID while DynamicID remains the dynamic ID. This also repairs the same mapping for feed cards; no detail-page-specific routing rewrite is needed.

## A one-card placeholder suppresses REST detail fallback

At `src/BiliLite.UWP/Modules/User/DynamicVM.cs:949-953`, `detailCards.Count == 1` is treated as a successful usable detail. But `DynamicGrpcMapper.MapUserCard` retains unhandled non-ad/banner types as a single card with `DYNAMIC_TYPE_UNSUPPORTED` (`Helpers/DynamicGrpcMapper.cs:286-315,646-663`). `ConvertToDisplay2024` returns a Type=Other placeholder for these cards (`DynamicVM.cs:1255-1264`); it also catches conversion failures and returns another Other placeholder (`:1380-1388`). Neither result throws into the fallback catch.

Consequently, an unsupported gRPC detail or a card the display converter cannot render shows “无法显示此动态” / “动态加载失败” and returns before trying the existing REST detail. This defeats the new path's intended recovery precisely when gRPC supplies a card the app cannot display.

Convert into a local display model first and accept it only when it is usable, including rejecting the converter's Other/failure placeholders. Otherwise continue into the existing REST code. Keep genuine supported content or deliberate deleted-content states distinct from conversion failure; a broad mapper redesign is unnecessary.

No worthwhile issue found in the cookie timestamp/consecutive-failure changes or the archive-search migration. The current bug report still describes the earlier packaging/device round; those results were not rerun here.
