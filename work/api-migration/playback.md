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
