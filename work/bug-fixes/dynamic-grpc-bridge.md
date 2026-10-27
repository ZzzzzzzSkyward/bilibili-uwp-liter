# Dynamics gRPC bridge resume note

## Scope

Port the pinned sibling's app dynamic v2 request definitions and narrow DynAll/DynVideo service contract into the target so later VM integration can replace the broken legacy `dynamic_history` reads. Keep feed DTO/card conversion in a separate integration task.

## Reference behavior

Behavior reference: `/workspace/biliuwp-lite` at `ca2caddc597b3faf05b4dac6dcb226aa0694365b`.

- `BiliLite.Services.GrpcService.GetDynAll` sends `DynAllReq` to `https://grpc.biliapi.net:443/bilibili.app.dynamic.v2.Dynamic/DynAll`, sets `Page`, and when offset is non-null sets `Offset` plus `Refresh.History`.
- `GetDynVideo` sends `DynVideoReq` to the corresponding `/DynVideo` method, always sets `LocalTime = 8`, and for `page > 1` sets `Offset`, `UpdateBaseline`, `Page`, and `Refresh.History`.
- Both methods construct `GrpcBiliUserInfo` from the active account's access key, user ID, and login app key, then parse the matching protobuf reply on successful transport results.

## Target implementation

- The target had no dynamic v2 proto files or generated types. The five-file source import closure was copied from the pinned sibling: `app/archive/middleware/v1/preload.proto`, `app/dynamic/v2/dynamic.proto`, and `dagw/component/avatar/{common/common.proto,v1/avatar.proto,v1/plugin.proto}`. The target already references `Google.Protobuf` 3.29.3 and `Grpc.Tools` 2.69.0, so normal project source generation is available after adding the protobuf items. No `protoc`, build, restore, app run, test, or runtime/correctness validation has been performed.
- Added `BiliLite.Services.GrpcService.GetDynAll(int page = 1, string historyOffset = null)` and `GetDynVideo(int page, string historyOffset, string updateBaseline)`. It uses target account state and the existing `GrpcRequest.SendMessage(..., GrpcBiliUserInfo)` overload, preserving target transport diagnostics and its existing request framing/header behavior.
- The bridge exposes replies only. The sibling `MapToDynamicItemModels` maps a selected subset (video, PGC, and UGC season) into sibling-specific models. The target primary `UserDynamicPage` consumes legacy JSON `DynamicCardModel` and renders through `DynamicParse` into `DynamicItemDisplayModel`; the sibling mapper does not cover the target's full card types, so no content-dropping all-feed mapper is included here.

## Changed paths

- `src/BiliLite.gRPC/BiliLite.gRPC.csproj`
- `src/BiliLite.gRPC/bilibili/app/archive/middleware/v1/preload.proto`
- `src/BiliLite.gRPC/bilibili/app/dynamic/v2/dynamic.proto`
- `src/BiliLite.gRPC/bilibili/dagw/component/avatar/common/common.proto`
- `src/BiliLite.gRPC/bilibili/dagw/component/avatar/v1/avatar.proto`
- `src/BiliLite.gRPC/bilibili/dagw/component/avatar/v1/plugin.proto`
- `src/BiliLite.UWP/Services/GrpcService.cs`
- `src/BiliLite.UWP/BiliLite.UWP.csproj`

## Remaining integration

Consumer VM changes and a mapper from protobuf dynamic items to the target display models remain assigned elsewhere. Once that work lands, verify it preserves page/offset/baseline semantics from the relevant reply list. Only `git diff --check` is permitted in this workspace.
