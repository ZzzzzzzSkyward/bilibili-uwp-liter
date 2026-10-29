# Dynamic V2 feed mapping

## Source and scope

The sole behavior reference is `/workspace/biliuwp-lite` at `ca2caddc597b3faf05b4dac6dcb226aa0694365b`. Its `UserDynamicAllViewModel` calls `GrpcService.GetDynAll` for the combined feed and `GetDynVideo` for video and season tabs. Its video grid mapper handles AV, PGC, and UGC-season cards. The sibling filters banner cards from the combined feed and renders other card families with its Dynamic V2 views.

## Target change

`Helpers/DynamicGrpcMapper.cs` adapts protocol cards to the target app's existing models. The home grid maps AV, PGC, and UGC-season cards into its current models; UGC-season retains protocol type 4310 and the archive AID used by the sibling's detail navigation. The primary dynamic feed maps word, forward, draw, AV, PGC, UGC-season, article, music, live, and live-recommendation cards into the target's existing `DynamicCardModel2024` renderer. It carries author identity and presentation metadata, dynamic IDs, interaction counts and like state, rich description nodes, PGC archive AID, draw images, and repost origins. Banners are filtered like the sibling; source card families without a safe target card mapping remain visible through the target's existing `Other` card.

`Modules/User/DynamicVM.cs` routes the All tab to `DynAll`, Video and Season to `DynVideo`, and Article to the already-present sibling-matched `ArticleV2` wrapper and its `items`/`offset` response. It retains the gRPC history offset, video update baseline, page number, and server `HasMore`. A failed page request disables further automatic loading until explicit refresh resets the failure and cursor state. Space and Topic modes retain their existing target API behavior.

Small target compatibility changes preserve PGC archive AID for comment/detail actions, use the source author time label in the home grid, route article cards through their source jump URL, and provide the full draw image list to each image action.

## Remaining scope and validation

Space and Topic still use the existing target path. Common-square, course, and other protocol card families without a faithful current renderer are represented by the target's unsupported-card view. No new feed endpoints were added. Only `git diff --check` was run; no build, restore, tests, app run, runtime request, or protocol generation was performed.
