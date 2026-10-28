# Integration baseline

Observed at target `3a4fe5533ad54e81159a7d0e0a6a5e1371886c25`; behavior reference sibling pinned at `ca2caddc597b3faf05b4dac6dcb226aa0694365b`. No `AGENTS.md` files were found under `/workspace`.

## Target boundaries

- **Local playback:** `Pages/LocalPlayerPage.xaml.cs` receives the page-local playlist wrapper (`PlayInfos`, `Index`) and passes those `PlayInfo` objects to the existing `PlayerControl`. `PlayInfo` and the current per-item `LocalPlayInfo` live in `Controls/PlayerControl.xaml.cs`. The latter has legacy `Info`, subtitle dictionary, danmaku path, and quality properties. `Pages/DownloadPage.xaml.cs` constructs them for downloaded videos. Keep this path intact for LM-01.
- **Player media models:** `Modules/Player/Playurl/BiliPlayUrlInfo.cs` defines the existing playback URL and DASH track structures (`BiliDashPlayUrlInfo`, `BiliDashItem`). These are the future LM-03 boundary; LM-01 remains independent.
- **Subtitles and danmaku:** `Controls/PlayerControl.xaml.cs` currently parses Bilibili subtitle JSON, schedules/render cues, and loads danmaku paths into its existing renderer. No separate subtitle track parser or portable local-media domain layer exists in target. SRT/VTT must remain unsupported until adapters and parser/rendering tests exist.
- **API/auth:** endpoint classes are under `Api/`; account and player endpoints are `AccountApi.cs` and `PlayerAPI.cs`. `ApiHelper` supplies shared signing/WBI and cookie-facing helpers; `HttpHelper` reads WebView cookies. There is no extracted cookie or WBI service layer yet.
- **gRPC:** `BiliLite.gRPC` contains request/header code and nine existing protobuf definitions; `Services/GrpcService.cs` in UWP calls it. Keep generated/proto scope incremental.
- **Project constraints:** UWP project uses explicit `<Compile Include=...>` entries, so new C# source files require project entries. Target platform version is 10.0.19041, minimum 10.0.18362; no `LangVersion` override was found. Newtonsoft.Json 13.0.3 is already referenced.

## Pinned sibling comparison

The sibling local playback retains a compatible `LocalPlayInfo` with legacy `Info`, `Subtitles`, `DanmakuPath`, and `Quality`, and adds separate video/audio track collections plus current selections. It uses different shared model locations and types. LM-01 defines a stable manifest contract in the target; later adapters should map into the target's own `BiliPlayUrlInfo`/`BiliDashItem` boundary rather than copying sibling internal models.
