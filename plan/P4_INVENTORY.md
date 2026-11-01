# P4 inventory — REST response → application model

Read-only survey of the four families left in `plan/PLAN.md` item 4. Target boundaries are the ones the
dynamic migration already proved:

| Stage | Migrated reference |
| --- | --- |
| Request builder | `src/BiliLite.UWP/Api/User/WebDynamicFeedApi.cs:16` `CreateFeedRequest` (synchronous, no signing) |
| Transport | `ApiModel.Request()` → `src/BiliLite.UWP/Helpers/Utils.cs:36` → `HttpHelper.Get/GetWithWebCookie/Post` |
| Envelope | `src/BiliLite.UWP/Parsing/ApiEnvelope.cs:12` `Read(HttpResults)` → `ApiParseResult<JObject>` (`data`-only) |
| Parser | `src/BiliLite.UWP/Parsing/Dynamic/WebDynamicFeedParser.cs:30` `Parse` — shape checks, own cursor, no UI |
| Mapper | `src/BiliLite.UWP/Parsing/Dynamic/WebDynamicFeedMapper.cs` + scalars `WireScalars.cs` |
| Result type | `src/BiliLite.UWP/Parsing/ApiParseResult.cs:28` + `ApiParseFailure` (`Transport/Envelope/Rejected/Payload`) |
| Publisher | `Modules/Home/DynamicVM.cs:886`, `Modules/User/DynamicVM.cs:567` — select source, publish, own the message |

Everything below still decodes with one of four `HttpResults` helpers
(`src/BiliLite.UWP/Helpers/HttpHelper.cs`): `GetJson<T>` :553, `GetJObject` :576 (**shows a toast on
failure** :596), `GetData<T>` :602 (`ApiDataModel<T>`), `GetResult<T>` :614 (`ApiResultModel<T>`).
`ApiDataModel<T>`/`ApiResultModel<T>` live in `src/BiliLite.UWP/Models/ApiModel.cs:3,34` — two envelope
models, neither of which is a parse result. `GetJObject` returns `{}` for an unparsable body, so "no JSON"
is indistinguishable from "empty object" at every call site that uses it.

Notation: transport is `Request()` = `ApiModel.Request()` (`Helpers/Utils.cs:36`) unless stated.

---

## 1. Comments

Publisher is the control itself: `Controls/CommentControl.xaml.cs` owns paging, cursors and the UI, and the
hosts only hand it a `LoadCommentInfo` (`Controls/CommentControl.xaml.cs:715`).

| Endpoint | Builder | Transport | Envelope | Parse site | Model | Publisher | Probe |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `x/v2/reply/wbi/main` (cursor) | `Api/CommentApi.cs:75` `CommentV2` — **async** builder (WBI sign inside) | `Request()` | `data` (`replies`, `top_replies`, `upper.top`, `cursor.pagination_reply.next_offset`) | `Controls/CommentControl.xaml.cs:168-176` — a throwaway `re.GetData<object>()` at :171 only to read `code<0`; real decode `JsonConvert.DeserializeObject<dataCommentModel>(re.results)` :183 | `dataCommentModel` `CommentControl.xaml.cs:733-757`; `CommentModel` :759-1023 | `GetComment` :148 → `ls_new.ItemsSource` :214-236; hosts: `Pages/VideoDetailPage.xaml.cs:420`, `Pages/SeasonDetailPage.xaml.cs:232,263,360,385,407`, `Pages/UserInfoPage.xaml.cs:101`, `Pages/User/DynamicDetailPage.xaml.cs:70`, `Pages/Home/UserDynamicPage.xaml.cs:151` | none (see §6) |
| `x/v2/reply` (legacy) | `Api/CommentApi.cs:61` `Comment` | `Request()` | `data` | fallback pick :174-176, same deserialize :183 | same | same | none |
| `x/v2/reply/reply` | `Api/CommentApi.cs:98` `Reply` | `Request()` | `data.replies` | `CommentControl.xaml.cs:301-330` (deserialize :304) | `CommentModel` | `GetReply` :290 → `data.replies.Add` :317 | none |
| `x/v2/reply/action` (like) | `Api/CommentApi.cs:112` `Like` | `Request()` | `code`/`message` only | `JObject.Parse(re.results)` :362 | `CommentModel.action/like` | `doLike` :345 | none |
| `x/v2/reply/add` (reply / @reply) | `Api/CommentApi.cs:126` `ReplyComment` | `Request()` | `code`/`message` | `JObject.Parse` :520 and :594 | — | `ReplyComment` :498, `ReplyAt` :557 | none |
| `x/v2/reply/del` | `Api/CommentApi.cs:138` `DeleteComment` | `Request()` | `code`/`message` | `JObject.Parse` :663 | — | `DeletComment` :619 | none |
| `x/v2/reply/add` (root comment) | `Api/CommentApi.cs:150` `AddComment` | `Request()` | `data` via `GetData<object>` | `Dialogs/SendCommentDialog.xaml.cs:39-41` (checks `data.code`) | — | `SendCommentDialog` | none |
| `x/v2/reply/main` (older cursor variant) | `Api/CommentApi.cs:32` `Comment_New` | — | — | — | — | **no caller** | — |

UI work inside the parse path: `CommentControl.xaml.cs:186` (`Utils.ShowMessageToast("解析评论失败", …)` in
the deserialize catch), :276 "全部加载完了", :282 评论区已关闭, :288/292 error toasts, :351/366/388 action
toasts. Cursor/page state (`_page`, `cursor`, `isCursorApiResponse`) is interleaved with the decode.

Field-contract traps to carry into the parser/mapper:

- `dataCommentModel` is envelope **and** payload in one type (`code`, `message`, `data`, `page`, `acount`,
  `count`, `num`, `size`, `cursor`, `hots`, `replies`, `upper`, `top`, `top_replies`) — a body without
  `code` decodes as `code = 0`, i.e. missing business code reads as success. This is exactly what
  `ApiEnvelope` refuses to do.
- `replies`, `top_replies`, `upper.top` and `cursor` are all independently nullable; only `replies == null`
  is normalised (:193-196). Pinned-reply dedupe is UI logic on `rpid` (:196-236) and belongs to the mapper
  or the VM, not the parser.
- `CommentModel.rpid` is `long` (:790) — fine, but `data.rpid` arrives as a number while `rpid_str`
  exists; keep the numeric ID only where the action calls need it.
- `CommentModel` doubles as wire DTO and view model (`INotifyPropertyChanged` :911, `Visibility showTop`
  :1003, `replyText`, `loadpage`, `showReplyMore`, `showLoading`) — a source DTO + mapper is required
  before the parse can leave the control.
- Two endpoints feed the same model and the same cursor slot (`cursor`) with different cursor shapes
  (`pagination_reply.next_offset` vs `pn`); the legacy branch sets `isCursorApiResponse = false` and keeps
  the old `NextOffset`, which is the source-bound-cursor rule P4 must preserve explicitly.
- `CommentItem` (`CommentControl.xaml.cs:1024-1098`) is a second, attribute-based comment DTO with a
  `ToCommentModel()` converter — **no caller anywhere**; its converter also does
  `c.replies.Append(i.ToCommentModel())` (:1090), discarding the result.

---

## 2. Video detail

Publisher is `Modules/Detail/VideoDetailVM.cs` (`VideoInfo` property :81); the page binds it and starts
the chain at `Pages/VideoDetailPage.xaml.cs:426`.

| Endpoint | Builder | Transport | Envelope | Parse site | Model | Publisher | Probe |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `x/web-interface/view` | `Api/VideoAPI.cs:19` `DetailWebInterface` | `Request()` | `data` | `VideoDetailVM.cs:159,164` `GetJson<ApiDataModel<VideoDetailModel>>` | `VideoDetailModel` :751-877 (+ `VideoUgcSeason` `Models/VideoCollections.cs:10`) | `VideoInfo` :81 → `VideoDetailPage` | `c1` (model only), `stab` (source text) |
| `x/web-interface/archive/related` | `Api/VideoAPI.cs:31` `RelatesWebInterface` | `Request()` | `data` (array) | `VideoDetailVM.cs:160,168` | `VideoDetailRelatesModel` :1068 | merged into `data.data.relates` :172 | `c1` (source text) |
| `app/x/v2/view` (signed app) | `Api/VideoAPI.cs:7` `Detail` | `Request()` | `data` | `VideoDetailVM.cs:183-187` | same | same | none |
| `biliproxy.iill.moe/app.php` (third-party proxy) | `Api/VideoAPI.cs:98` `DetailProxy` | `Request()` | `data` (proxied) | `VideoDetailVM.cs:190-194` | same | same | none |
| `x/tag/archive/tags` | `Api/VideoAPI.cs:192` `Tags` | `Request()` | `data` (array) | `LoadVideoTags` :286-291 | `VideoDetailTagModel` :1062 | `VideoInfo.tag` :294 | none |
| `x/relation/stat` | `Api/VideoAPI.cs:87` `RelationStat` | `Request()` | `data.follower` | `LoadOwnerFans` :258-264 | `VideoDetailOwnerExtModel.fans` | `VideoInfo.owner_ext` | none |
| `x/relation` (attribute) | `Api/User/FollowAPI.cs:115` `GetAttention` | `Request()` | `data.attribute` | `GetAttentionUp` :318-326 | `VideoDetailReqUserModel.attention` | same | none |
| `archive/has/like` | `Api/VideoAPI.cs:45` `HasLike` | `Request()` | `data` (int) | `GetUserVideoStates` :344-358 | `req_user.like` | same | none |
| `archive/coins` | `Api/VideoAPI.cs:59` `HasCoin` | `Request()` | `data.multiply` | :344,361-368 | `req_user.coin` | same | none |
| `fav/video/favoured` | `Api/VideoAPI.cs:73` `HasFav` | `Request()` | `data.favoured` | :344,369-376 | `req_user.favorite` | same | none |
| `x/relation/modify` (follow) | `Api/VideoAPI.cs:180` `Attention` | `Request()` | `data` | :682-690 | `req_user.attention` | `DoAttentionUP` | none |
| `view/like`, `view/dislike` | `Api/VideoAPI.cs:118,135` | `Request()` | `data` | :395-405, :448-458 | `req_user.like/dislike` | `DoLike`/`DoDislike` | none |
| `view/like/triple` | `Api/VideoAPI.cs:151` `Triple` | `Request()` | `data` | :506-516 | `req_user` | `DoTriple` | none |
| `view/coin/add` | `Api/VideoAPI.cs:163` `Coin` | `Request()` | `data` | :555-565 | `req_user.coin` | `DoCoin` | none |
| `fav/folder/created/list` (which folders hold it) | `Api/User/FavoriteAPI.cs:42` `MyCreatedFavorite` | `Request()` | `data.list` | :127-133 (`GetJson<ApiDataModel<JObject>>` then `data["list"]` re-deserialize) | `FavoriteItemModel` | `MyFavorite` :110 → `LoadFavorite` :119 | none |
| `medialist/gateway/coll/resource/deal` (favorite) | `Api/User/FavoriteAPI.cs:58` `AddFavorite` | `Request()` | `data` | :608-620 | `req_user.favorite` | `DoFavorite` | none |
| `PlayerAPI.VideoPlayUrl` (download URL) | `Api/PlayerAPI.cs` | `Request()` | `data.durl[0].url` | :720-730 | — | download URL helper | none |

Field-contract traps:

- Five sources are folded into one `VideoDetailModel` by the VM (web view + relates; app view fallback;
  proxy fallback). The fallback chain is decided on `data == null || !data.success` (:181) — i.e. on a
  *model* being null, not on a parse result. `relates` failure nulls the whole page (:173-176).
- `short_link` is synthesised in the VM (:167), `owner_ext` and `req_user` are defaulted/rewritten
  (:205-213) — pure mapping, belongs in the mapper with a documented default.
- **`VideoDetailModel.relates` setter dereferences `value` without a null check**
  (`:821-822` `set { _relates = value.Where(...) }`) → a wire `relates: null` throws inside
  deserialization. The current code only survives because the app path always sets it. This must be fixed
  as part of the migration, not carried over.
- `aid`/`cid`/`mid` are `string` on `VideoDetailModel` (:751-...), `VideoDetailPagesModel(:1077)`,
  `VideoDetailOwnerModel(:898)`; `VideoUgcSeasonSectionEpisode.Aid` is a string with an `Arc` fallback —
  keep IDs as strings, numeric conversion only where the endpoint contract requires it.
- `req_user` is local UI state living inside the wire model; the parser must not own it.
- `data.success`/`data.code` are the only envelope check today; `GetJson` returns `default` (null) on a
  parse failure and the code cannot tell that from a rejected envelope.
- `VideoDetailModel` and `VideoUgcSeason` live in `Modules/`/`Models/`, not `Models/Api/`.

---

## 3. Season detail

Publisher `Modules/Detail/SeasonDetailVM.cs` (`Detail` :30, `Episodes` :63, `Previews` :79); page
`Pages/SeasonDetailPage.xaml.cs` builds `PlayInfos` and the comment hosts.

| Endpoint | Builder | Transport | Envelope | Parse site | Model | Publisher | Probe |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `pgc/view/web/ep/list` (only live read) | `Api/SeasonApi.cs:28` `EpList` | `Request()` | **`result`** | `SeasonDetailVM.cs:100-105` `GetJson<ApiResultModel<SeasonDetailModel>>`; section merge :172-183; split :186-189 | `SeasonDetailModel` :300-451, `SeasonDetailSectionItemModel` :475, `SeasonDetailEpisodeModel` :482, `SeasonDetailModel_Web` :452 | `Detail` :184, `Episodes`/`Previews` :186-189 | none |
| `bangumi.bilibili.com/view/web_api/season` | `Api/SeasonApi.cs:47` `DetailWeb` | `Request()` | hand-rolled (`code` + `result` read by indexer) | `SeasonDetailVM.cs:213-232` `GetWebSeasonDetail` — `GetJObject`, `data["code"].ToInt32()`, `data["result"]["episodes"].ToString()` re-deserialize | `SeasonDetailModel_Web` | **dead**: only caller is the commented block at :132 | none |
| `pgc/view/v2/app/season` (signed) | `Api/SeasonApi.cs:9/12` `Detail` | — | — | commented at :121 | — | **no caller** | — |
| `pgc/view/web/ep/list` alias | `Api/SeasonApi.cs:44` `Detail_Web` | — | — | — | — | **no caller** (pure alias of `EpList`) | — |

Field-contract traps:

- `data.result.total != 0` (:148) gates the *episode list itself*; the `try` body under it is entirely
  commented out (:150-164), so the branch only "exists". `Episodes`/`Previews` are derived from
  `data.result.episodes.Where(...)` (:186-188) with no null guard — `result.episodes: null` NREs.
- `section[].episodes` is inserted into `result.episodes` with `section_type = 1` (:172-183), and
  `IsPreview` is computed as `section_type != 0` (:562). 正片/预告 is a *mapper* rule, currently applied by
  mutating the wire model.
- `SeasonDetailModel` still carries commented-out `JArray modules` (:320) and a `convert(SeasonDetailModel_Web)`
  (:404-408) used only by the dead web path; `SeasonDetailModel_Web` re-declares `actor`/`staff` as
  `string` (:452-456) to dodge a shape difference — the exact "two shapes, one model" problem P4 removes.
- `season_id` is `int` on `SeasonDetailSeasonItemModel` (:629) while `id` is `long` on the section model
  (:478) — inconsistent width; keep the wire width in the DTO and normalise in the mapper.
- No `result`-envelope counterpart to `ApiEnvelope` exists: `ApiEnvelope.Read` only accepts `data`. A
  `result` reader (or a parameterised envelope selector) is the first shared piece this family needs.
- `DoFollow` (:243) is a fully commented-out body — dead, delete with the migration.

---

## 4. Playlists (remote medialist, video-list continuations)

Two source kinds sit behind one directory model (`Models/PlayDirectory.cs`,
`Models/MediaListResources.cs`). Sync/parse already lives in `MediaListPagingState.Append`
(`Models/MediaListResources.cs:266`) — that part is clean; the network path is not.

| Endpoint | Builder | Transport | Envelope | Parse site | Model | Publisher | Probe |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `x/v2/medialist/resource/list` (signed) | `Api/VideoAPI.cs:203` `GetMediaList` | `Request()` | `data` (`media_list`, `has_more`, `total`) | `Services/MediaListService.cs:79` `GetData<MediaListResources>()`; failure toast `ReportFailure` :96-102 | `MediaListResources`/`MediaListItem` `Models/MediaListResources.cs:12-46`; paging `:174-283` | `Pages/VideoDetailPage.xaml.cs:899-905` (`LoadMediaListPage`), rows :686/891, load-all :824 | `c2` (service + paging), `c3` (paging/route), `ux3`/`ux2*` (source text) |
| `x/v2/medialist/resource/list` (unsigned retry) | `Api/VideoAPI.cs:215` `GetMediaListNotLogin` | `Request()` | `data` | same `RequestAsync` :62-87 | same | same | `c2` |
| `bilibili://music/playlist/playpage/{id}` link | — (pure model) `Models/MediaListResources.cs:94-158` `MediaListRoute.TryParse` | — | — | — | `MediaListRoute` | `Helpers/MessageCenter.cs:526-552` `OpenMediaListPlaylist` (pre-fetches page 1 via `MediaListService`) | `c3` |
| `x/v3/fav/resource/list` (favorites as a queue) | `Api/User/FavoriteAPI.cs:168` `FavoriteInfo` | `Request()` | `data` (`medias`, `has_more`) | `Modules/Detail/FavoriteDetailVM.cs:127-131` | `FavoriteDetailModel` (in `FavoriteDetailVM.cs`) | `Pages/User/FavoriteDetailPage.xaml.cs:158-184` builds an in-memory `VideoPlaylist` | `c1`/`c3` (in-memory queue only) |
| `x/v2/history/toview` (watch later queue) | `Api/User/WatchLaterAPI.cs:16` `Watchlater` | `Request()` | `data.list` | `Modules/User/WatchLaterVM.cs:114-120` (`GetJson<ApiDataModel<JObject>>` + `data["list"]` re-deserialize) | `WatchlaterItemModel` | `Pages/WatchlaterPage.xaml.cs:37-73` | none |
| `ugc_season` (embedded in video detail, no second request) | — | — | part of `data` of `x/web-interface/view` | `VideoDetailVM.cs:164` (same decode) | `VideoUgcSeason`/`...Section`/`...Episode` `Models/VideoCollections.cs:10-114` | `Pages/VideoDetailPage.xaml.cs:450-540`; order `Models/VideoCollections.cs:129-160` | `c1`, `c3` |
| `pages[]` (分P, embedded) | — | — | `data.pages` | `VideoDetailVM.cs:164` | `VideoDetailPagesModel` :1077 | `PlayDirectoryBuilder.FromParts`; `VideoDetailPage` | `c1` |

Field-contract traps:

- `MediaListService` shows toasts (`:100` `Utils.ShowMessageToast`) and logs inside the decode path, and
  returns `null` for every failure category — transport, rejection and "no media_list" are collapsed. The
  service also owns *source selection* (signed → unsigned retry, :44-58), which is the VM's job under the
  P4 contract; it does distinguish "request failed" from "response had no media_list" via the private
  `MediaListReply` (:107-112) — that distinction is precisely what an `ApiParseResult<T>` carries, so the
  wrapper should be replaced, not preserved.
- Cursor is `oid = last row's aid` (`MediaListPagingState.LastAid`, `:216`/`:230`); `has_more` is stored on
  the state, `total` only when the server reported it (`HasKnownTotal` :206). Keep all three source-bound.
- `MediaListItem` uses `[JsonProperty("media_list")]`/`[JsonProperty("has_more")]` on the container but
  PascalCase `Name` on the upper object (`:43-46`) — the wire name there is `name` and works only because
  Newtonsoft is case-insensitive by default. Make wire names explicit.
- `MediaListPagingState.Append` dedupes by resource ID and returns the added count (:266-...); the
  `mediaListLoadMoreButton`/footer texts are derived from the state — that derivation is fine, the toast is
  not.
- `FavoriteDetailVM` re-deserializes `data` into a full model after checking `success`; the same response
  also feeds `FavoriteInfo` (title) — parser + mapper, no change to `FavoriteDetailPage`'s in-memory queue.

---

## 5. Account responses (login state, `Modules/User/Account`, `Services/CookieService` callers)

Three decoders coexist: `ApiEnvelope`-style is **absent**; `CookieService`/`AccessKeyRenewal`/
`PasswordLoginSession` hand-roll `JObject.Parse` + `code`; `Account`/`LoginVM` use `GetJson`/`GetData`/
`GetJObject`. `CookieService` even gates on the *transport* code: `response?.code == 200`
(`Services/CookieService.cs:235`) while everything else checks `results.status`.

| Endpoint | Builder | Transport | Envelope | Parse site | Model | Publisher | Probe |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `oauth2/getKey` | `Api/AccountApi.cs:16` | `Request()` | `data.hash`/`data.key` | `Modules/User/Account.cs:38-40` (`GetJObject` + indexer) | none (RSA input) | `EncryptedPassword` :30 | `AUTH-01b` (builder only) |
| `oauth2/login` (v2) | `Api/AccountApi.cs:49` | `Request()` | `code`/`ts`/`data.access_token…` | `Account.cs:65-67` `GetJson<LoginV2Model>` | `LoginV2Model` | `LoginV2` :60 | none |
| `x/passport-login/oauth2/login` (v3) | `Api/AccountApi.cs:68` | `Request()` | `data` (`status`, `token_info`, `sso`, `cookie_info`, `url`) | `Account.cs:136-140`, `:215-219` `GetData<LoginDataV3Model>` | `LoginDataV3Model` | `LoginV3` :131, `Login2023` :210 | `AUTH-01b` (source-text/shape) |
| v3 reuse from the dialog | same builder `:68` | `Request()` | `data` | `Modules/User/LoginVM.cs:559-565` `GetData<LoginResultModel>` → `HandleLoginResult` :679 | `LoginResultModel` | `LoginVM` | `AUTH-01b` LoginVM probe |
| v3 reuse from the **debug panel** | `Api/AccountApi.cs:100` `Login2023` | `Request()` | `data` | `Pages/SettingPage.xaml.cs:1390,1406` `GetData<LoginResultModel>` | `LoginResultModel` | `SettingPage.DoLogin` :1380 | none |
| `passport-login/country` | `Api/AccountApi.cs:148` | `Request()` | `data.list` | `LoginVM.cs:305-311` (`GetJson<ApiDataModel<JObject>>` + `data["list"]` re-deserialize) | `CountryItemModel` | `LoginVM.Countries` | none |
| `passport-login/sms/send` | `Api/AccountApi.cs:159`, `:171` | `Request()` | `data` | `LoginVM.cs:368-371`, `:415-418` `GetJson<ApiDataModel<SMSResultModel>>` | `SMSResultModel` | `LoginVM` | none |
| `passport-login/login/sms` | `Api/AccountApi.cs:182` | `Request()` | `data` | `LoginVM.cs:480-484` `GetData<LoginResultModel>` | `LoginResultModel` | `LoginVM` → `HandleLoginResult` | none |
| `safecenter/captcha/pre` | `Api/AccountApi.cs:195` | `Request()` | hand-rolled | `LoginVM.cs:813-816` (`GetJObject` + `body["data"]` re-deserialize) | `GeetestRequestModel` | `LoginVM` | none |
| `safecenter/user/info` | `Api/AccountApi.cs:206` | `Request()` | hand-rolled | `LoginVM.cs:850-854` (`body["code"]`, `body["data"]["account_info"]["hide_tel"]`) | — | password secondary flow | none |
| `safecenter/common/sms/send` | `Api/AccountApi.cs:217` | `Request()` | hand-rolled | `LoginVM.cs:906-909` | — | same | none |
| `safecenter/login/tel/verify` | `Api/AccountApi.cs:234` | `Request()` | hand-rolled | `LoginVM.cs:934-938` | — | same | none |
| `web/exchange_cookie` | `Api/AccountApi.cs:252` | isolated `HttpClient` (`Services/IsolatedPasswordLoginTransport.cs`) | hand-rolled | `Services/PasswordLoginSession.cs:37-41` | `PasswordLoginSession` | `Account.CommitCookieLogin` :365 → `FinishCookieLoginAsync` :372 | `AUTH-01b` (70+67 assertions) |
| `login/app/third` + confirm (cookie→access key) | `Api/AccountApi.cs:263`, `:279` | isolated `HttpClient` | hand-rolled | `Services/IsolatedPasswordLoginTransport.cs:114-133` | `PasswordLoginSession` | same | `AUTH-01b` |
| `web/cookie/info` | `Api/AccountApi.cs:295` | `CookieService.send` (injected `api.Request()`) | hand-rolled, **transport `code==200`** (:235) | `Services/CookieService.cs:235-260` | `CookieTimestamp`, `needsCookieRefresh` | `CookieService.NeedRefreshCookieAsync` :206; `ReloginRequired` event | `AUTH1`, `API-01`, `AUTH-03` |
| `correspond/1/{path}` | `Api/AccountApi.cs:307` | `CookieService.send` | — | consumed as the refresh CSRF by the refresh chain (`CookieService.cs:346-…`) | refresh CSRF | `CookieService.RefreshCookieAsync` :321 | `AUTH2`/`AUTH3` |
| `web/cookie/refresh` | `Api/AccountApi.cs:325` | `CookieService.send` | hand-rolled | `CookieService.cs:403-410` | `newToken` | same | `AUTH1`/`API-01` |
| `web/confirm/refresh` | `Api/AccountApi.cs:344` | `CookieService.send` | hand-rolled | `CookieService.cs:424-430` | — | same | `AUTH1`/`API-01` |
| `oauth2/refresh_token` | `Api/AccountApi.cs:515` | `Request()` via `Account.cs:768` | hand-rolled | `Services/AccessKeyRenewal.cs:58-105` (`JObject.Parse`, `code`, `data.token_info/cookie_info`) | `LoginTokenInfo`,`LoginCookieInfo` | `AccessKeyRenewal.RunAsync` → `SaveLogin` | `AUTH-03` |
| `account/myinfo` | `Api/AccountApi.cs:393` | `Request()` | `data` | `Account.cs:554-563` (`GetJObject` + `code` + `DeserializeObject<MyProfileModel>(obj["data"])`) | `MyProfileModel` | `GetProfile` :539 | `AUTH-01b` (profile clear only) |
| `account/mine` + `app/x/v2/space` (two responses, one model) | `Api/AccountApi.cs:410`, `:426` | `Request()` ×2 | `data` ×2 | `Account.cs:580-623` — indexer reads across two bodies (:608-622) | `HomeUserCardModel` :775 | `Modules/Home/HomeVM.cs:174` `LoginUserCard` | none |
| `passport-login/web/qrcode/generate` | `Api/AccountApi.cs:465` | `Request()` | `data` | `Account.cs:640-643` `GetData<QRAuthInfo>` | `QRAuthInfo` :808 | `Account.GetQRAuthInfo` :636 → `LoginVM.cs:619` | none |
| `passport-login/web/qrcode/poll` | `Api/AccountApi.cs:482` | `Request()` | `data` | `Account.cs:688-691` `GetData<LoginDataV3Model>` | `LoginDataV3Model` | `PollQRAuthInfo` :684 → `LoginVM.cs:668` | none |
| `x/oauth2/info` (login state) | `Api/AccountApi.cs:499` | `Request()` | hand-rolled | `Account.CheckLoginState` :735-745 (`GetJObject`, `code==0`) | bool | `Pages/HomePage.xaml.cs:63` | none |
| `x/web-interface/nav` (WBI keys) | `Api/AccountApi.cs:533` `WbiToken` | `Request()` | hand-rolled | `Services/WbiKeyService.cs:66-68` | `WbiKey` | `WbiKeyService` | `API-01` |
| `oauth2/login` (2023 app variant) | `Api/AccountApi.cs:100` `Login2023` | `Request()` | `data` | `SettingPage.xaml.cs:1393,1409` | `LoginResultModel` | debug panel | none |
| `api/login/sso` | `Api/AccountApi.cs:360` `SSO` | — | — | — | — | **no caller** (commented `Account.cs:77,342`) | — |
| `captcha` | `Api/AccountApi.cs:377` | — | — | — | — | **no caller** | — |
| `web/key` (2023) | `Api/AccountApi.cs:32` `GetKey2023` | — | — | — | — | **no caller** | — |

Field-contract traps:

- **Two login-state notions**: `Account.CheckLoginState()` = `x/oauth2/info` code 0; `CookieService` =
  `cookie/info` with `code`/`-101`/`-111` plus `NeedsRelogin`. Nothing today reconciles them, and
  `cookie/info` is the only one that can say "session dead". A parser per endpoint plus one service-level
  verdict is the shape to target; do not merge the two endpoints.
- `CookieService` checks `response?.code == 200` (:235) as "transport ok" while `HttpResults.code` is an
  HTTP status only on the WebView path — this is the one place the transport/body split is already explicit,
  and `ApiEnvelope.Read`'s `status` check cannot be substituted blindly.
- `Account.Login2023` (:210) actually calls `accountApi.LoginV3` (:215) — dead duplicate of `LoginV3`; the
  `Login2023` builder (`AccountApi.cs:100`) is a *different* request (app-key headers, `seccode|jordan`)
  used only by the debug panel. Names must be separated before any rename pass.
- `GetHomeUserCard` merges two responses with unchecked indexer chains (:608-622) and swallows every
  exception to `null`; `pendant` falling back to `AppHelper.TRANSPARENT_IMAGE` is a presentation default
  that belongs in the mapper.
- `AccessKeyRenewal` is already the correct shape (injected `send`, explicit failure categories, no UI) and
  is fully probed (`AUTH-03`) — treat it as the template for hand-rolled cookie paths, not as work to redo.
- Large IDs: `mid`/`uid` are carried as `string` in `MyProfileModel`/`HomeUserCardModel` but as `long` in
  `CommentMemberFansDetailModel.uid` and `LoginTokenInfo.mid` — pick per endpoint contract.

---

## 6. Probe coverage (`plan/tests/`)

| Family | Covered today | Gap |
| --- | --- | --- |
| Comments | `cm1` (rich-text rendering, `CommentControl` source text only) | **no behavioral probe** for `CommentV2`/`Comment` fallback, cursor, pinned dedupe, reply paging, action decoding |
| Video detail | `c1` (model deserialization incl. `ugc_season`, source text), `stab` (page source text) | no probe for the web→app→proxy fallback chain, relates merge, `req_user` states, tags/fans, `relates: null` |
| Season detail | `ux3`/`stab` (page source text only) | **no probe** for `ep/list` `result` envelope, section merge, 正片/预告 split, empty/`total==0`, null `episodes` |
| Playlists | `c2` (service retry + paging, recorded transport), `c3` (route, paging, order), `ux2*` (directory source text) | no probe for the toast-in-service failure categories; favorites/watch-later list decoding uncovered |
| Account | `AUTH1`, `AUTH2`, `AUTH3`, `API-01`, `API-02`, `AUTH-01b` (cookie chain, exchange, `Account` adapter, `WbiKeyService`) | no probe for `LoginV2/V3/GetProfile/GetHomeUserCard/QR/CheckLoginState` decoding; `LoginVM` covered only via `AUTH01BLoginVM` lifecycle |

Pattern for new probes: mirror `plan/tests/g4` — compile the real `Parsing/` sources plus a stubbed
`HttpResults` factory (`RestEnvelope(...)`), assert each `ApiParseFailure` category. `g3`/`g4` show the
one-test-per-category shape to copy.

---

## 7. Suggested migration order

Order is by "one endpoint pair, one commit, smallest blast radius, probes already exist".

| # | Family / step | Why here | Risk & blockers |
| --- | --- | --- | --- |
| 1 | **Season detail** (`EpList` only) — **done in `ed9f248`** | Single live endpoint, single prompt VM, `result` envelope is the one shared piece every other family will need; nothing else depends on it | Shipped as planned: `ApiEnvelope.ReadResult`, `Parsing/Season/`, `Models/Api/Season/Web/`, the dead fallback chain deleted, `plan/tests/season1` (109 assertions) added first and grown until it pins each finding |
| 2 | **Playlists** (medialist pair + favorites/watch-later decode) | `c2`/`c3` already pin paging and retry, so a parser can be asserted before/after; the service is the cleanest "toast in the parse path" removal | Moves toast + retry decision from `MediaListService` into `VideoDetailPage`/`MessageCenter`; two page call sites and the link route must keep their single-flight and `lastAid` cursor. Do **not** touch `MediaListPagingState.Append` (already correct and probed) |
| 3 | **Video detail** (`view` + `related` first, then the four state reads) | Largest fan-in, but `c1` pins the model; splitting `view`/`related` from the action endpoints keeps each commit reviewable | `relates` setter NRE must be fixed or the DTO mapper must bypass it; the proxy fallback (`biliproxy.iill.moe`) is a third-party transport and needs an explicit decision (keep as a fallback source, or delete — it is only reachable when the signed app endpoint fails); `req_user`/`owner_ext` defaults must move to the mapper with tests |
| 4 | **Account** | Highest risk: credentials, `CookieService` single-flight, the exchanged-session commit path and `AUTH-01b`'s 136 assertions | Do **not** re-plumb the cookie chain (`CookieService`, `AccessKeyRenewal`, `PasswordLoginSession`, `IsolatedPasswordLoginTransport`) in the same commit as plain response decoding. Start with the read-only/profile endpoints (`myinfo`, `mine`+`space`, `oauth2/info`, QR) and only then the login responses. Separate `Account.Login2023` from `AccountApi.Login2023` naming before any rename |
| 5 | **Comments** | Last on purpose: the publisher is a `UserControl` that owns paging, cursor and toasts, so the VM work is part of the migration, and there is no probe to fall back on | Requires a `CommentModel` source DTO + mapper extraction (view fields: `showTop`, `replyText`, `loadpage`, `showLoading`, `showReplies`), a decision on where `_page`/`cursor` live, and two source-bound cursors (offset string vs `pn`). `CommentItem` and `Comment_New` delete with it |

Cross-family item, **done in `ed9f248`**: `HttpResults.TryGetJObject` is the silent reader and
`ApiEnvelope` uses it; `GetJObject` keeps its toast for callers that have not migrated yet, so a family
that still parses inline must keep going through `GetJObject` until it is migrated.

---

## 8. Files an executor will own per family

**Season detail**
`src/BiliLite.UWP/Api/SeasonApi.cs`, `src/BiliLite.UWP/Modules/Detail/SeasonDetailVM.cs`,
`src/BiliLite.UWP/Parsing/ApiEnvelope.cs` (add `result` selection) or a new
`src/BiliLite.UWP/Parsing/` reader, new `src/BiliLite.UWP/Models/Api/Season/` + `Parsing/Season/`,
`src/BiliLite.UWP/BiliLite.UWP.csproj`, new `plan/tests/` probe project.

**Playlists**
`src/BiliLite.UWP/Api/VideoAPI.cs` (medialist pair only), `src/BiliLite.UWP/Services/MediaListService.cs`,
`src/BiliLite.UWP/Models/MediaListResources.cs`, `src/BiliLite.UWP/Pages/VideoDetailPage.xaml.cs`
(load paths + toast), `src/BiliLite.UWP/Helpers/MessageCenter.cs` (link route only),
`src/BiliLite.UWP/Modules/Detail/FavoriteDetailVM.cs`, `src/BiliLite.UWP/Modules/User/WatchLaterVM.cs`,
new `Models/Api/MediaList/` + `Parsing/MediaList/`, `BiliLite.UWP.csproj`.

**Video detail**
`src/BiliLite.UWP/Api/VideoAPI.cs`, `src/BiliLite.UWP/Api/User/FollowAPI.cs` (`GetAttention` only),
`src/BiliLite.UWP/Api/User/FavoriteAPI.cs` (`MyCreatedFavorite`, `AddFavorite`),
`src/BiliLite.UWP/Modules/Detail/VideoDetailVM.cs` (VM + the model classes at the bottom of the file),
`src/BiliLite.UWP/Models/VideoCollections.cs`, `src/BiliLite.UWP/Pages/VideoDetailPage.xaml.cs` (only where
the fallback/`UgcSeason` wiring changes), new `Models/Api/Video/` + `Parsing/Video/`, `BiliLite.UWP.csproj`,
`plan/tests/c1` (may need regenerating — `run.sh` extracts the model region from the VM file).

**Account**
`src/BiliLite.UWP/Api/AccountApi.cs`, `src/BiliLite.UWP/Modules/User/Account.cs`,
`src/BiliLite.UWP/Modules/User/LoginVM.cs`, `src/BiliLite.UWP/Services/CookieService.cs` (decode block only),
`src/BiliLite.UWP/Services/AccessKeyRenewal.cs`, `src/BiliLite.UWP/Services/PasswordLoginSession.cs`,
`src/BiliLite.UWP/Services/IsolatedPasswordLoginTransport.cs`, `src/BiliLite.UWP/Pages/SettingPage.xaml.cs`
(debug login only), new `Models/Api/Account/` + `Parsing/Account/`, `BiliLite.UWP.csproj`.

**Comments**
`src/BiliLite.UWP/Api/CommentApi.cs`, `src/BiliLite.UWP/Controls/CommentControl.xaml.cs` (+ `.xaml` if the
binding surface changes), `src/BiliLite.UWP/Dialogs/SendCommentDialog.xaml.cs`,
new `Models/Api/Comment/` + `Parsing/Comment/`, `BiliLite.UWP.csproj`, new probe project.

Shared-enabling file every one of the four touches: `src/BiliLite.UWP/Helpers/HttpHelper.cs` (`GetJObject`
toast) — change it once, in the first family commit, and note it there.

---

## 9. Not worth migrating

- `Services/AccessKeyRenewal.cs` + `Services/PasswordLoginSession.cs` + `Services/IsolatedPasswordLoginTransport.cs`
  — already parse-by-hand with injected transport, explicit categories and probe coverage (`AUTH-01b`,
  `AUTH-03`). Only the `JObject.Parse` duplication could be shared; a rewrite buys nothing.
- `Models/MediaListResources.cs:75-159` (`MediaListQueue`, `MediaListRoute`) and
  `MediaListPagingState.Append` — pure, probed, no transport or UI.
- `Models/PlayDirectory.cs`, `Models/WatchSequence.cs`, `Models/VideoCollections.cs:129-160` — pure builders
  over already-parsed models; P4 must not touch them.
- `Api/PlayerAPI.cs` playurl/danmaku endpoints — different contract (protobuf/segment), not part of item 4.
