# Catalog and discovery API migration

## Scope and reference

Behavioral reference: `/workspace/biliuwp-lite` at `ca2caddc597b3faf05b4dac6dcb226aa0694365b`.

Owned request sources compared against:

- `Models/Requests/Api/SearchAPI.cs`: `Search`, `WebSearchVideo`, `WebSearchAnime`, `WebSearchMovie`, `WebSearchUser`, `WebSearchLive`, `WebSearchArticle`, `WebSearchTopic`, and `SearchSuggest`.
- `Models/Requests/Api/RankAPI.cs`: `Rank`.
- `Models/Requests/Api/RegionAPI.cs`: `Regions`, `RegionDynamic`, `RegionChildDynamic`, `RegionChildList`.
- `Models/Requests/Api/SeasonApi.cs`: `Detail`, `EpList`; preserved target review action methods and legacy `DetailWeb` API.
- `Models/Requests/Api/SeasonIndexAPI.cs`: `Condition`, `Result`.
- `Models/Requests/Api/Home/AnimeAPI.cs`: `BangumiHome`, `GuochuangHome`, `Timeline`, `AnimeFallMore`.
- `Models/Requests/Api/Home/CinemaAPI.cs`: `CinemaHome`, `CinemaFallMore`.
- `Models/Requests/Api/Home/HotApi.cs`: `Popular`.
- `Models/Requests/Api/Home/LiveAPI.cs`: `LiveHome`.
- `Models/Requests/Api/Live/LiveAreaAPI.cs`: `LiveAreaList`, `LiveAreaRoomList`.
- `Models/Requests/Api/Live/LiveRecommendAPI.cs`: `LiveRoomList`.
- `Models/Requests/Api/Live/LiveCenterAPI.cs`: `FollowLive`, `FollowUnLive`, `History`, `SignInfo`.
- Shared signing/cookie contract coordinated with `Services/ApiHelper.cs`, `Models/Common/ApiConstants.cs`, `Models/Requests/Api/BaseApi.cs`, and reference `Models/Requests/ApiModel.cs`.

## Step 1 — source-backed read behavior

Implemented in target `src/BiliLite.UWP/Api/` while retaining the target `BiliLite.Api` public namespaces and existing callable members:

- Search web types use `/x/web-interface/wbi/search/type`, asynchronous complete-query WBI signing, and the reference `buvid3=temp` extra cookie; app search uses `ApiHelper.LoginKey`. Added the reference `SearchSuggest` endpoint.
- Ranking uses WBI signing and enables cookies when logged in.
- Region mobile APIs use explicit `/x/v2/...` paths and configured `LoginKey` signing.
- Season detail uses `/pgc/view/v2/app/season`, supports season/episode identifiers through `SeasonIdType`, and adds the reference web episode-list request. Kept `Detail_Web` as an alias and retained `DetailWeb` for target compatibility. Review action implementations remain unchanged.
- Anime and cinema requests use the reference-configurable web API base setting and its reference default.
- Popular, live-home, live-area, recommendation, and read-only live-center requests use the reference login key where the reference used `BaseApi.AppKey`; popular/live-home requests normalize `mobi_app=iphone` before signing. Live history uses the explicit `/x/v2/history/liveList` path. `DoSign` remains unchanged as an action.

Required async call-site updates are in `src/BiliLite.UWP/Modules/SearchVM.cs`, `src/BiliLite.UWP/Modules/RankVM.cs`, and `src/BiliLite.UWP/Modules/Detail/SeasonDetailVM.cs`.

## Current status

- Implementation: complete for the owned catalog/discovery APIs and their async callers.
- Formatting: owned-path `git diff --check` passed; no tests, builds, restores, API calls, or correctness validation were run.
- Remaining: commit this file and only the explicit owned paths under the reserved commit slot.
