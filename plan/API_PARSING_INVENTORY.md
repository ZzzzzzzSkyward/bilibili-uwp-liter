# API parsing inventory (plan item 5, batch P0)

Read-only inventory of the dynamic-feed response paths, produced before any parser is moved.
Purpose per `plan/PLAN.md` § 5: **each active response path has a named owner, and no source or
paging cursor is omitted.** Destinations (`Parsing/...`) are proposals; nothing has moved yet.

## Envelope and result types in play

| Type | Role today | Note |
| --- | --- | --- |
| `ApiModel` (`Api/ApiHelper.cs:303`) | request description: method, url, parameter, body, headers, cookie flag | built synchronously; `Request()` returns `HttpResults` |
| `HttpResults` (`Helpers/HttpHelper.cs:493`) | transport result: `code` (HTTP status), `status`, `message`, `results`, `RequestId`, `Method`, `Endpoint` | `GetJson<T>` / `GetJObject` / `GetData<T>` / `GetResult<T>` decode and **can show a toast** |
| `ApiDataModel<T>` (`Models/ApiModel.cs:3`) | `{code,message,msg,data}` envelope | `success` is `code == 0` |
| `ApiResultModel<T>` (`Models/ApiModel.cs:34`) | `{code,message,msg,result}` envelope | same shape, different payload key |
| `ReturnModel<T>` (`Models/ReturnModel.cs:3`) | UI-facing `{success,message,data}` | not a wire envelope |
| `DynamicApiOutcome` + `DynamicApiSourceResult` (`Services/DynamicApiSelector.cs`, `Services/DynamicApiSelection.cs`) | source-selection verdict per attempt | outcome classes: success / transport / rejected / malformed / ignored |
| `ApiParseResult<T>` | **proposed** (P1) | parse verdict only: value, or failure class + stage + field path |

`ApiDataModel<T>`, `ApiResultModel<T>`, `ReturnModel<T>`, `DynamicApiSourceResult` and the proposed
`ApiParseResult<T>` are five different concerns; the plan forbids treating them as interchangeable.

## Active response paths

Legend: **cursor** = the paging state that page N+1 must send; each source keeps its own.

### 1. 关注动态 — 全部 / 视频 (the surface behind BR-17 and BR-22)

| Source | Builder | Wire shape | Cursor | Decoder today | Mapper today | Caller |
| --- | --- | --- | --- | --- | --- | --- |
| web polymer | `DynamicAPI.ArticleV2(page, offset, "all"\|"video")` → `x/polymer/web-dynamic/v1/feed/all` (cookie) | `{code,message,data:{items[],offset,has_more}}` | `data.offset` (`polymerOffset`) | inline in `Modules/User/DynamicVM.TryUserFeedPolymerAsync` (envelope, items, has_more, offset) | `DynamicGrpcMapper.MapToUserFeedCards` → `ConvertToDisplay2024` | `Modules/User/DynamicVM` |
| gRPC | `GrpcService.GetDynVideo(page, historyOffset, updateBaseline)` → `DynVideo` | `DynVideoReply{list[],history_offset,update_baseline}` | `history_offset` + `update_baseline` (`historyOffset`, `updateBaseline`) | protobuf decoder | `DynamicGrpcMapper.MapToUserCards` | same VM |
| legacy REST | `DyanmicNew(type, offset)` → `dynamic_new`; `HistoryDynamic(id, type)` → `dynamic_history` | `{code,message,data:{cards[]}}`, `dynamic_history` adds `next_offset` | legacy dynamic id (`historyOffset` reused as the legacy cursor) | `JsonConvert.DeserializeObject<List<DynamicCardModel>>` | legacy card model (not the mapper) | same VM |

### 2. 首页视频动态 (video-only tab)

| Source | Builder | Wire shape | Cursor | Decoder today | Mapper today | Caller |
| --- | --- | --- | --- | --- | --- | --- |
| web polymer | `ArticleV2(page, offset, "video")` | as § 1 | `data.offset` (`_polymerOffset`) | inline in `Modules/Home/DynamicVM.RequestPolymerSourceAsync` | `DynamicGrpcMapper.MapToHomeVideoItems` | `Modules/Home/DynamicVM` |
| gRPC | `GrpcService.GetDynVideo` | as § 1 | `history_offset`/`update_baseline` (`_historyOffset`, `_updateBaseline`) | protobuf | `DynamicGrpcMapper.MapToHomeItems` | same VM |
| legacy REST | `DyanmicNew` / `HistoryDynamic` (`UserDynamicType.Video`) | as § 1 | last card `desc.dynamic_id` (`_restCursor`) | `DeserializeObject<ObservableCollection<DynamicItemModel>>` | legacy model | same VM |

### 3. 专栏 / 图文 tab (关注动态 的 `Article` 分类)

| Source | Builder | Wire shape | Cursor | Decoder / mapper | Caller |
| --- | --- | --- | --- | --- | --- |
| web polymer | `ArticleV2(page, offset, "article")` | as § 1 | `data.offset` (`articleOffset`) | inline in the same method that serves § 1 (`TryUserFeedPolymerAsync`) | `Modules/User/DynamicVM` |
| web nav | `Article(updateBaseline, "article")` → `x/polymer/web-dynamic/v1/feed/nav` | `{code,data:{update_num,...}}` | `update_baseline` (update-count badge only, not paging) | `DeserializeObject<DynamicCardModel>` on `data` | same VM |

Note: § 1 和 § 3 share one request path; only `type` and the cursor field differ. A parser must keep
the two cursors separate — `polymerOffset` (all/video) and `articleOffset` (article).

### 4. UP 主空间动态

| Source | Builder | Wire shape | Cursor | Decoder | Mapper | Caller |
| --- | --- | --- | --- | --- | --- | --- |
| web polymer | `DynamicAPI.SpaceHistoryV2(mid, offset)` → `x/polymer/web-dynamic/v1/feed/space` (cookie) | `{code,data:{items[],offset,has_more}}` | `data.offset` (`spaceOffset`) | protobuf-free JSON, decoded through the same feed shape as § 1 | `MapToUserFeedCards`/`MapToUserCards` | `Modules/User/DynamicVM` (space leg) |
| gRPC | `GrpcService.GetDynSpace(mid, "space", page, offset)` → `DynSpace` | `DynSpaceRsp{list[],offset}` | `offset` + page | protobuf | `MapToUserCards` | same VM |
| legacy REST | `DynamicAPI.SpaceHistory(mid, dynamic_id)` → `dynamic_svr/space_history` | `{code,data:{cards[],has_more}}` | `offset_dynamic_id` | `DeserializeObject<List<DynamicCardModel>>` | legacy model | same VM |

### 5. 动态详情

| Source | Builder | Wire shape | Decoder | Mapper | Caller |
| --- | --- | --- | --- | --- | --- |
| gRPC | `GrpcService.GetDynDetail(id)` → `DynDetail` | `DynDetailReply{item}` | protobuf | `DynamicGrpcMapper.MapToUserCards` (single card) | `Modules/User/DynamicVM` detail leg |
| legacy REST | `DynamicAPI.DynamicDetail(id)` → `get_dynamic_detail` | `{code,data:{card}}` | `DeserializeObject<DynamicCardModel>` | legacy model | same VM |

### 6. 转发列表

| Source | Builder | Wire shape | Cursor | Decoder | Mapper | Caller |
| --- | --- | --- | --- | --- | --- | --- |
| gRPC | `GrpcService.GetDynRepostList(id, offset)` → `RepostList` | `RepostListRsp{list[],has_more,offset}` | `offset` | protobuf | `DynamicRepostMapper.ToDisplayList` | `Modules/User/DynamicRepostVM` |
| legacy REST | `DynamicAPI.DynamicRepost(id, offset)` → `dynamic_repost/repost_detail` (cookie) | `{code,data:{items[],has_more,offset}}` | `offset` | `DeserializeObject` of the card model per item | `DynamicRepostMapper` | same VM |

### 7. 话题 / 推荐

`DynamicAPI.RecommendTopic()` (`topic_svr/get_rcmd_topic`, appkey-signed) feeds the topic page; the
topic's own dynamic list reuses § 1/§ 4 through the same VMs. No separate parser is needed — record it
as a caller, not a source.

### 8. Write endpoints (no response model, envelope only)

`CreateDynamicPhoto`, `CreateDynamicText`, `RepostDynamic`, `Like`, `Delete`, `UploadImage`
(`Api/User/DynamicAPI.cs`): all answer `{code,message,data:{dynamic_id|...}}`. They only need the
envelope check, and they must not gain a payload parser.

## Duplicated rules that the first migration batch should absorb

| Rule | Duplicated in | Target |
| --- | --- | --- |
| `response == null → transport`, `!status → HTTP status class`, missing `code → malformed` | `Services/DynamicApiSelection.ClassifyLegacy`, plus inline in both polymer legs | keep `ClassifyLegacy` as the single transport classifier; the parser reports `Transport`/`Envelope` and the VM maps through `ClassifyLegacy` |
| `code != 0 → business rejection` | `ClassifyLegacy` + both polymer legs | parser reports `Rejected` with the code; VM maps via `DynamicApiOutcomeClassifier.FromBilibiliCode` |
| `data`/`items` shape checks and `has_more`/`offset` reads | `Modules/User/DynamicVM.TryUserFeedPolymerAsync`, `Modules/Home/DynamicVM.RequestPolymerSourceAsync` | one `WebDynamicFeedParser` |
| "mapping threw ⇒ this leg did not get a page" | both polymer legs (`stage=feed-polymer-failed`) | stays in the VM (mapping is a mapper concern), but the page object it maps comes from the parser |

## P1/P2 first slice (next implementation batch)

1. `Parsing/ApiParseResult.cs` — value or `{Transport, Envelope, Rejected, Payload}` failure with stage,
   field path, business code and message.
2. `Parsing/Dynamic/WebDynamicFeedParser.cs` — decode once, check envelope, check `data.items` is an
   array, read `offset`/`has_more`, return `WebDynamicFeedPage`. No toasts, no UI, no source choice.
3. Both polymer legs switch to the parser; `DynamicGrpcMapper.MapToUserFeedCards` /
   `MapToHomeVideoItems` stay the mappers (P2 moves them out of the gRPC-named file).
4. Probes: g3/g4 keep their current contracts (polymer fallback, cursors, quiet pages, refresh) and
   gain parser cases — business rejection, malformed envelope, valid empty page, non-array `items`.
