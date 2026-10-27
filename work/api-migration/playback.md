# Playback and video-detail API migration

## Step 1: Read API endpoints

Reference: `/workspace/biliuwp-lite/src/BiliLite.UWP/Models/Requests/Api/PlayerAPI.cs` symbols `VideoPlayUrl`, `SeasonPlayUrl`, `SeasonAndroidPlayUrl`, `LivePlayUrl`, `InteractionEdgeInfo`, `GetPlayerInfo`, and `SegDanmaku`; `/workspace/biliuwp-lite/src/BiliLite.UWP/Models/Requests/Api/VideoAPI.cs` symbols `Detail`, `DetailWebInterface`, `RelatesWebInterface`, `HasLike`, `HasCoin`, `HasFav`, `Tags`, `GetMediaList`, and `GetMediaListNotLogin`.

Changes in target `src/BiliLite.UWP/Api/PlayerAPI.cs` and `src/BiliLite.UWP/Api/VideoAPI.cs`:

- Switched regular video playback retrieval to `/x/player/playurl`, included the logged-in `mid`, and requested sibling's fixed `fnval=4048`; kept existing method arguments and target headers/proxy compatibility.
- Season playback now always requests the sibling's web `fnval=12240` or Android `fnval=4048`; proxy requests add the sibling's referer header. The existing older live-play API remains for target fallback compatibility; its v2 helper now requests cookies.
- Changed interaction edge ID to the sibling's `int` type. Player-info retrieval is now async and awaits `ApiHelper.GetWbiSign` per the helper owner's contract. Danmaku segment retrieval uses `/x/v2/dm/list/seg.so` and Android-signed request parameters.
- Updated Android detail URL to explicit `/x/v2/view`; added the sibling's web detail, related-video, like/coin/favorite status, tags, and media-list read APIs.
- Action APIs were left unchanged.

Format check: `git diff --check` passed.

Resumable status: API endpoint changes are complete. Next: commit this step after parent grants the shared-index slot; then integrate web video detail reads and player/gRPC retrieval behavior.

## Step 2: Video detail and playback metadata reads

Reference: `/workspace/biliuwp-lite/src/BiliLite.UWP/ViewModels/Video/VideoDetailPageViewModel.cs` symbols `LoadVideoDetail`, `LoadVideoTags`, `GetAttentionUp`, and `GetUserVideoStates`; `/workspace/biliuwp-lite/src/BiliLite.UWP/Modules/Player/PlayerVM.cs` symbols `GetPlayInfo` and `PlayerInfoViewPoint`; `/workspace/biliuwp-lite/src/BiliLite.UWP/Modules/Player/InteractionVideoVM.cs` symbols `GetNodes`, `InteractionEdgeInfoChoiceModel`, and `InteractionEdgeInfoQuestionModel`.

Changes in target `src/BiliLite.UWP/Modules/Detail/VideoDetailVM.cs`, `src/BiliLite.UWP/Modules/Player/PlayerVM.cs`, `src/BiliLite.UWP/Modules/Player/InteractionVideoVM.cs`, and the interaction caller in `src/BiliLite.UWP/Controls/PlayerControl.xaml.cs`:

- Video detail now tries the web detail and related-video reads, falls back to the existing app/proxy detail path, and fills the web response's related videos and short link. It also loads the sibling's tags and, for web detail, the uploader follow state plus login-only like/coin/favorite state reads.
- `PlayerVM.GetPlayInfo` now awaits the async WBI-signed PlayerAPI request. The `PlayerInfo` response model includes sibling `view_points` data.
- Interaction edge IDs use `int` like the sibling API/model, including the control's `ChangedNode` caller.
- Danmaku control has no direct change in this step: its existing `LoadDanmaku` call still reaches the same PlayerVM public method. Protocol/parser integration is recorded in Step 3.

Format check: `git diff --check` passed.

Resumable status: detail/player metadata integration is complete. Next: commit this step after parent grants the shared-index slot; then migrate gRPC playback and danmaku protocols.

## Step 3: gRPC playback and danmaku protocols

Reference: `/workspace/biliuwp-lite/src/BiliLite.gRPC/Api/PlayURL.cs`, `/workspace/biliuwp-lite/src/BiliLite.gRPC/bilibili/app/playurl/v1/playurl.proto`, `/workspace/biliuwp-lite/src/BiliLite.gRPC/bilibili/tv/interfaces/dm/v1/dm.proto`, and `/workspace/biliuwp-lite/src/BiliLite.UWP/Modules/Player/Playurl/BiliPlayUrlRequest.cs` symbol `ParseGrpc`.

Changes in target `src/BiliLite.gRPC/Api/PlayURL.cs`, `src/BiliLite.gRPC/GrpcRequest.cs`, `src/BiliLite.gRPC/Api/GrpcBiliUserInfo.cs`, `src/BiliLite.gRPC/BiliLite.gRPC.csproj`, the two copied protocol files, `src/BiliLite.UWP/Modules/Player/Playurl/BiliPayUrlRequest.cs`, and `src/BiliLite.UWP/Modules/Player/PlayerVM.cs`:

- Added the sibling's current PlayURL request/reply proto and TV danmaku reply proto, with project entries. Both source protos are self-contained.
- Switched playback gRPC request construction and parsing to `Bilibili.App.Playurl.V1`, including its `Aid` field, boolean `Fourk`, and unsigned/64-bit field conversions. The target's existing string access-key transport remains available; the new user-info overload supplies access key while retaining compatibility.
- Player playback gRPC requests now pass the target's configured `LoginKey`, access key, and user ID. Danmaku segment parsing now uses the new TV interface reply type.
- The target control already routes danmaku reads through the same `PlayerVM.GetDanmaku` method, so no further control read-path change was needed. The sibling's FrostMaster-specific reader is not used by this target.

Format check: `git diff --check` passed.

Resumable status: protocol and parser migration is complete. Next: commit this step after parent grants the shared-index slot. No proto generation, builds, tests, restores, or API requests were run.
