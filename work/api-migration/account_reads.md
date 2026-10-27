# Account/library/live-room retrieval migration

Reference: `/workspace/biliuwp-lite` at `ca2caddc597b3faf05b4dac6dcb226aa0694365b`.

## Scope and progress

- Target API files: `src/BiliLite.UWP/Api/User/UserDetailAPI.cs`, `FavoriteAPI.cs`, `WatchLaterAPI.cs`, `AtAPI.cs`, `Api/AccountApi.cs`, and `Api/Live/LiveRoomAPI.cs`.
- Caller integration: `src/BiliLite.UWP/Modules/User/UserDetail/UserDetailVM.cs`, `UserFollowVM.cs`, `Modules/Detail/FavoriteDetailVM.cs`, and `Modules/Detail/LiveRoomVM.cs`.
- Reference symbols: `Models/Requests/Api/User/UserDetailAPI.cs` (`UserInfo`, `SubmitVideos`, `Followings`); `FavoriteAPI.cs` (`FavoriteInfo`); `WatchLaterAPI.cs` (`Watchlater`); `AtAPI.cs` (`RecommendAt`, `SearchUser`); `AccountApi.cs` (`UserProfile`, `MineProfile`, `Space`, `History`); `LiveRoomAPI.cs` (`RoomRankList`, `RoomLotteryInfo`, `GetBuvid`).
- Shared API reference: `Services/ApiHelper.cs` (`GetWbiSign`, `GetSign`, `MustParameter`), `Models/Requests/Api/BaseApi.cs` (`AppKey`), `Models/Common/ApiConstants.cs`.
- Retrieval migration and caller integration: implemented in the owned files listed above. WBI reads are async to match the shared signer contract.
- No tests/build/restore/network/API calls. Final permitted check: `git diff --check` only.
- No files staged or committed yet. Before staging, request a commit slot from the parent; stage only owned paths and this tracker.

## Grounded decisions

- Keep all login/authentication and mutation methods unchanged, including watch-later add/delete/clear, favorite mutation methods, live gift/danmu/lottery participation, and history deletion.
- WBI methods must await `ApiHelper.GetWbiSign(query)` and assign its complete signed query. The shared helper owner confirmed it will load current keys and return the full signed parameter string.
- Ref `AccountApi` retrieval methods already match target routes and parameter signing; they need no changes.
- Ref `AtAPI` and `WatchLaterAPI.Watchlater` retrieval endpoints and parameters match target; they need no changes.
- `UserDetailAPI.Space`, `UserStat`, `Followers`, `Favlist`, and `FollowingsTag` already match the reference retrieval behavior; no changes. Favorite season retrieval also matches.
- `GitApi.CheckUpdate` is source-configurable through the `RawRepositoryAddress` setting; target has no matching setting UI, so preserve its existing mirror as fallback while honoring a stored address. Keep target-only `FindMoreEntrance` unchanged.
- Ref live rank API supports a contribution rank plus fans rank. Target's three legacy gift-rank tabs and `list`/`next_offset` parsing must be adjusted to the reference-supported contribution/fans behavior.
- Ref lottery retrieval requires BUVID3 in a per-request cookie. Shared helper owner is adding `ApiModel.ExtraCookies` forwarding; caller will obtain BUVID3 with the source `GetBuvid` endpoint.

## Remaining work

- Formatting check: `git diff --check` passed for all tracked owned files; no whitespace diagnostics. The tracker is still untracked and will be included in the staged diff check once the parent grants the commit slot.
- Staging and commit: pending parent commit slot; no files staged.
