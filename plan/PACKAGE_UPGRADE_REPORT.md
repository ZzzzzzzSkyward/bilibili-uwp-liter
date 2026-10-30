# Package upgrade investigation

2026-10-29. My recommendation: keep the current UWP app, take a few small package updates, and give the player and export helper their own upgrade batches. There is useful modernization available, but copying the sibling's entire dependency stack would add a lot of work without a clear improvement for this app.

The best starting points are WinUI 2, Newtonsoft.Json, NLog 5, a stable WebView2 SDK, and the protobuf tools/runtime. The most worthwhile larger investigation is FFmpegInteropX. The separately hosted WebApi also deserves a current .NET runtime if you maintain its deployment.

## What this report is based on

- Target: [ZzzzzzzSkyward/bilibili-uwp-liter, `work`](https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/tree/3092a4194e2726f69bea8267f31f163e8de33949), source commit `3092a4194e2726f69bea8267f31f163e8de33949`. The starting checkout was `e2c7958`; it was fast-forwarded from GitHub and configured to track `origin/work`.
- Sibling: [ywmoyue/biliuwp-lite, `dev`](https://github.com/ywmoyue/biliuwp-lite/tree/ca2caddc597b3faf05b4dac6dcb226aa0694365b), commit `ca2caddc597b3faf05b4dac6dcb226aa0694365b`, dated October 25. This matches the sibling revision in the existing project notes. It was absent locally, so a shallow comparison checkout was fetched into `/workspace/work/package-research/biliuwp-lite`.
- The managed Linux environment is ready for research. Its current HTTP policy reports unrestricted access and enforcement; the inherited proxy and certificate settings were preserved. This is not a Windows build environment setup.
- Three GPT-6-luna subagents researched UI/infrastructure, transport/backend, and media/helper dependencies. Findings were reconciled against source and primary documentation. No applicable `AGENTS.md` was found.
- Only this report is changed in the target repository. No application code, package references, manifests, or build scripts were edited. No installs, restore, builds, tests, linters, format checks, protocol generation, application launch, live Bilibili/backend requests, package archive inspection, or other compatibility validation were run. Online requests were for Git operations and research documentation/metadata.

Versions below are stable releases observed in public NuGet metadata on the report date, unless explicitly described as current project pins. **A published compatible framework asset makes an upgrade plausible; it does not prove that this app works with it.** Effort estimates are rough implementation estimates for someone familiar with the code, not measured results or promises about shipping time.

## Small updates worth taking first

The app is classic UWP/.NET Native: Windows SDK target 19041, minimum 18362, and `Microsoft.NETCore.UniversalWindowsPlatform 6.2.14`. Its [project file][app-project] explicitly lists source files and packages. Keep that platform boundary for an initial maintenance pass.

| Dependency | Target now | Sibling choice | Candidate and practical tradeoff |
|---|---|---|---|
| WinUI 2 / Microsoft.UI.Xaml | 2.8.4 | 2.8.3 | [2.8.7](https://www.nuget.org/packages/Microsoft.UI.Xaml/2.8.7). Same UI product line; low code impact expected. A good first update, measured in hours of implementation. |
| Newtonsoft.Json | 13.0.3 | 13.0.1 | [13.0.4](https://www.nuget.org/packages/Newtonsoft.Json/13.0.4). Small update while preserving existing JSON behavior. Update both the app and WebApi references. |
| NLog | 5.3.4 | 5.2.2 | [5.5.1](https://www.nuget.org/packages/NLog/5.5.1) is the conservative same-major candidate. Latest stable overall is 6.2.2, but there is little reason to combine that migration with routine servicing. |
| WebView2 SDK | 1.0.3079-prerelease | No explicit package; uses WinUI dependencies | [1.0.4314.35](https://www.nuget.org/packages/Microsoft.Web.WebView2/1.0.4314.35) is a stable candidate. First establish whether an explicit SDK pin is needed alongside the chosen WinUI 2 version. Small source scope, but browser initialization/navigation remains a compatibility question. |
| Google.Protobuf | 3.29.3 in app and gRPC library | 3.23.4 | [3.36.2](https://www.nuget.org/packages/Google.Protobuf/3.36.2), updating both references together. Active message serialization/parsing; keep the dependency. |
| Grpc.Tools | 2.69.0 | 2.56.0 | [2.85.0](https://www.nuget.org/packages/Grpc.Tools/2.85.0). Build-time generation, separate from the network transport. Keep `PrivateAssets=all`; coordinate generated-code requirements with the protobuf runtime. Small to medium effort. |
| ZXing.Net | 0.16.9 | 0.16.8 | [0.16.11](https://www.nuget.org/packages/ZXing.Net/0.16.11). Small optional update; QR generation is used in login and video/season pages. |
| OpenCCNET | 1.0.2 | 1.0.2 | [1.1.0](https://www.nuget.org/packages/OpenCCNET/1.1.0). Small optional update around `ChineseConverter`'s two conversion calls; publishes a .NET Standard 2.0 asset. |

These are maintenance candidates, not demonstrated fixes for login, feed rejection, playback, or packaging bugs. Package updates cannot be assumed to fix changes in Bilibili's endpoint behavior.

WebView2 has two moving parts: the SDK referenced by the app and the browser Runtime installed on the user's machine. Updating one does not automatically update the other. Microsoft's [UWP setup guide](https://learn.microsoft.com/en-us/microsoft-edge/webview2/get-started/winui2) says WinUI 2 already brings the WebView2 SDK as a dependency. The target also directly uses CoreWebView2 APIs in `Pages/WebPage.xaml.cs`, so retaining an explicit stable pin can still be reasonable. Avoid deleting the reference solely because it is transitive.

NLog is configured in code by [LogHelper][log-helper]. The `NLog.Config 4.7.15` **package** may be unnecessary; `using NLog.Config` names an API namespace supplied by NLog and does not establish a need for that separate package. Keep the logging configuration behavior. NLog 6 introduces file/archive behavior changes and feature package splits, making it a separate migration with little immediate feature payoff here. See the upstream [NLog 6 changes](https://github.com/NLog/NLog/wiki/NLog-6.0---List-of-major-changes).

## Dependencies that need a real migration decision

**Toolkit 7 to 8 is feasible within UWP.** The target uses six `Microsoft.Toolkit.Uwp*` packages at 7.1.3 across many XAML controls, animations, helpers, and connectivity calls. Current stable UWP controls include [CommunityToolkit.Uwp 8.2.251219](https://www.nuget.org/packages/CommunityToolkit.Uwp.Controls.SettingsControls/8.2.251219). This involves splitting/renaming packages and changing C#/XAML namespaces; the types use `CommunityToolkit.WinUI.*` namespaces even with UWP packages. It is several days of migration work, with uncertain extra work for removed or changed controls. Follow the [v7-to-v8 migration guide](https://github.com/CommunityToolkit/Windows/wiki/Migration-Guide-from-v7-to-v8). My call: adopt individual controls when they solve a concrete UI need, then consider the broader migration. The sibling already mixes older Toolkit 6.1.1 packages with newer UWP ColorPicker 8.1.240916 and SettingsControls 8.0.240109; it has not modernized everything either.

**Keep Newtonsoft instead of converting the app to System.Text.Json.** JSON DOM access through `JObject`/`JToken`, attributes, and deserialization is spread throughout API, account, feed, and local-media code. A switch would mean revisiting behavior across dozens of files, probably weeks of work, for no identified user-visible gain. The [official migration guide](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/migrate-from-newtonsoft) documents the API and behavior differences. Using System.Text.Json in the small Win32 helper does not require converting the UWP app too.

**RestSharp is used for more than a method enum.** Most request descriptors use only its `Method.Get`/`Method.Post`, while the app sends those requests through its own Windows HTTP transport. However, [SendDynamicVM.UploadImage][upload-image] actively uses `RestClient`, `RestRequest`, `AddFile`, and `ExecuteAsync` for multipart uploads. Removing the package requires replacing that upload path and the shared enum usage. The target pin is `112.1.1-alpha.0.4`; stable [112.1.0](https://www.nuget.org/packages/RestSharp/112.1.0) is a closer-line candidate, while [114.1.0](https://www.nuget.org/packages/RestSharp/114.1.0) is the latest stable observed and publishes .NET Standard 2.0 assets. Prefer a focused stable-version change first. Full removal is plausible, roughly one or two days of source work, but has no large immediate payoff. Copying the sibling's Flurl.Http 3.2.4 would add a transport rewrite.

**The old Grpc package is mostly generated-stub baggage.** [GrpcRequest][grpc-request] serializes protobuf messages, frames them, and sends them through a shared `HttpClient`; application calls do not use generated gRPC service clients. The `Grpc 2.46.6` runtime is therefore a removal candidate, but not by deleting its reference alone: the [gRPC project][grpc-project] asks for client-stub generation on every proto. Explicitly switch unused service generation to `GrpcServices="None"`, retain message generation and `Grpc.Tools`, and remove the unused `using Grpc.Core` in `Modules/User/DynamicVM.cs`. Merely omitting the generation option can still generate service code by default.

Replacing transport with [Grpc.Net.Client 2.85.0](https://www.nuget.org/packages/Grpc.Net.Client/2.85.0) is a much larger choice. It would change HTTP/2 handling, metadata, status/trailer handling, error reporting, and fallback integration. A .NET Standard 2.0 asset does not establish legacy UWP support; Microsoft's [platform guidance](https://learn.microsoft.com/en-us/aspnet/core/grpc/supported-platforms?view=aspnetcore-10.0) does not list UWP. The sibling references Grpc.Net.Client 2.55.0 and Client.Web, but its normal request implementation also manually frames HTTP messages. Its package list alone is no reason to replace this transport.

## Playback and export: worthwhile, but keep the two upgrades separate

| Area | Target now | Sibling choice | Recommendation |
|---|---|---|---|
| FFmpegInteropX player | 2.0.0-pre7 + FFmpegInteropX.FFmpegUWP 5.1.100 | 1.0.0 + FFmpegUWP 5.0.0 | Investigate stable [2.1.0.81200](https://www.nuget.org/packages/FFmpegInteropX/2.1.0.81200) as a coordinated package/API migration. Potentially useful newer FFmpeg support, with substantial playback integration work. |
| SYEngine.uwp | 1.0.7 | 1.0.7 | [1.0.7](https://www.nuget.org/packages/SYEngine.uwp/1.0.7) remains the latest stable observed. Keep the FLV fallback while needed. Replacing the engine is a feature project; source already notes its ARM64 limitation. |
| NSDanmaku | 2.2.0 | 2.0.3, plus FrostMaster | Target is already at the latest stable observed, [2.2.0](https://www.nuget.org/packages/NSDanmaku/2.2.0). Keep it. |
| FFMpegCore export wrapper | 5.1.0 | 5.2.0 | [5.5.0](https://www.nuget.org/packages/FFMpegCore/5.5.0) is a reasonable helper update; publishes .NET Standard 2.0 assets. Upgrade its supporting packages together. |
| SharpCompress | 0.39.0 | 0.31.0 | [0.50.7](https://www.nuget.org/packages/SharpCompress/0.50.7) publishes a net48 asset. Used only around archive extraction, but the version jump merits API adaptation work. Avoid the 0.51 beta for this pass. |

FFmpegInteropX's stable line reorganized native packages and moved to C++/WinRT. The observed 2.1 package graph uses `FFmpegInteropX.UWP.Lib 2.1.0` and `FFmpegInteropX.UWP.FFmpeg 8.1.2`. Keeping the old FFmpegUWP package alongside it would not constitute a coherent upgrade. The declared UAP asset floor is 17763, below this app's minimum 18362, so the metadata does not force a higher OS floor; native API/runtime compatibility is still unresolved. Upstream [2.0 release notes](https://github.com/ffmpeginteropx/FFmpegInteropX/releases/tag/v2.0.0) and [2.1 notes](https://github.com/ffmpeginteropx/FFmpegInteropX/releases/tag/v2.1.0) explain the reorganization. Existing integration spans `App.xaml.cs`, `Controls/Player.xaml.cs`, `PlayerControl.xaml.cs`, and live playback, including separate audio/video streams and engine fallback. Budget several days to a couple of weeks, rather than treating this as a quick version bump.

The export helper is a different executable targeting **.NET Framework 4.8**. [MainWindow.xaml.cs][helper-main] uses FFMpegCore, SharpCompress, and System.Text.Json. FFMpegCore 5.5.0 requires at least Instances 3.1.0 and System.Text.Json 9.0.10. A coherent current JSON-family candidate is [System.Text.Json 10.0.14](https://www.nuget.org/packages/System.Text.Json/10.0.14), which publishes net462 assets. Coordinate its Bcl.AsyncInterfaces, Pipelines, Encodings.Web, Buffers, Memory, Unsafe, Tasks.Extensions, and ValueTuple dependencies; do not independently pin every package to an unrelated newest version.

The helper also has a preview `System.Runtime.CompilerServices.Unsafe 7.0.0-preview.2.22152.2`. Latest stable observed is [6.1.2](https://www.nuget.org/packages/System.Runtime.CompilerServices.Unsafe/6.1.2): the smaller number is not an argument for retaining the preview. Its [packages.config][helper-packages], project `HintPath`s, and [App.config binding redirects][helper-config] must agree with the selected package/assembly versions. These files currently hard-code the older paths and JSON/Unsafe assembly identities. Allow roughly one to three days for the helper dependency batch, excluding unknown integration problems.

Updating FFMpegCore does **not** update `ffmpeg.exe`: the helper extracts that executable from its separately bundled `ffmpeg.7z`. That archive was not opened, so this report makes no claim about its actual FFmpeg version or capabilities. The UWP player packages do not update that executable either.

Two small removal candidates: `gfoidl.Base64 1.1.2` has no observed source use, with the app using `Convert.ToBase64String`/`FromBase64String` instead; its [2.0.0](https://www.nuget.org/packages/gfoidl.Base64/2.0.0) targets net7.0 and is a poor fit for the current app. The helper's `System.Drawing.Common 9.0.1` has no observed direct API use, but FFMpegCore or generated resources may still need it. Remove either only when its indirect/generated dependency role is understood; no package graph was restored here.

## WebApi: a separate server upgrade, if you maintain it

The local WebApi project is not just an abandoned-looking folder. App-side `Api/Home/AnimeAPI.cs` and `CinemaAPI.cs` point at a configurable deployed `BiliLiteWebApiBaseUrl`, defaulting to `http://www.xiaotao2333.top:50233/`. That establishes a source dependency on this service shape; it does not establish that this host is currently online or that you control its deployment. No request was sent there.

The target's [WebApi project][webapi-project] is `netcoreapp3.0`; the sibling has moved its target to `net6.0` while retaining similarly old backend dependencies. Both runtime lines are out of support. If maintaining this server, choose **.NET 10 LTS** for the independent upgrade. Microsoft's [support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) lists .NET 10 support through November 14, 2028; .NET 8 and 9 end support on November 10, 2026, making them poor new migration destinations at this report date.

Practical package choices:

- Keep Redis caching: Startup registers it and controllers actively use `IDistributedCache`. Align `Microsoft.Extensions.Caching.StackExchangeRedis 3.1.0` with the [.NET 10 line, currently 10.0.14](https://www.nuget.org/packages/Microsoft.Extensions.Caching.StackExchangeRedis/10.0.14).
- Remove the unused EF Core SQL Server/Tools 3.0.0 references if the source remains as inspected: there is no DbContext, migrations, SQL connection, or EF query usage. Upgrading unused EF would add work without improving the server.
- Keep HtmlAgilityPack and Newtonsoft.Json, which are used. [HtmlAgilityPack 1.13.1](https://www.nuget.org/packages/HtmlAgilityPack/1.13.1) is a current candidate from target 1.11.72; sibling WebApi uses 1.11.17. The current package publishes .NET Standard 2.0 assets. A parser update still cannot repair upstream webpage shape changes automatically.
- `Microsoft.Extensions.Logging.Debug 3.0.0` is likely redundant with the Web SDK/framework defaults; `Microsoft.VisualStudio.Web.CodeGeneration.Design 3.0.0` is scaffolding tooling. Keep explicit references only if the development workflow actually needs them.

This is roughly one to three days of initial source/dependency work, plus separate server integration and deployment effort. Changing the local target framework does not update a deployed service. The full solution release script currently includes WebApi, so separating server maintenance from Windows app packaging could also reduce unrelated build friction later.

## Would adding a whole new package help?

| Option | Benefit versus cost | My call |
|---|---|---|
| CommunityToolkit.Mvvm [8.4.2](https://www.nuget.org/packages/CommunityToolkit.Mvvm/8.4.2) | Offers proper observable objects and async commands, including cancellation. The target has its own `IModules` lifecycle/error behavior and `RelayCommand`; source generators also introduce compiler/tooling requirements. Sibling instead uses PropertyChanged.Fody 4.1.0 and its own view models. | Reasonable for a new screen needing async command state; several days for selective adoption. Do not rewrite existing view models just to reduce boilerplate. Manual toolkit classes can be considered separately from generators. |
| DanmakuFrostMaster.muyan [1.0.2](https://www.nuget.org/packages/DanmakuFrostMaster.muyan/1.0.2) | Sibling supports it through separate controller/model integration. It would add a renderer choice, with additional lifecycle, timing, and mapping work. | Only if an alternate renderer is an actual desired feature. No demonstrated reason to replace working NSDanmaku. |
| Polly / HTTP resilience | Can centralize retry/backoff. But [retry re-executes the operation](https://www.pollydocs.org/strategies/retry.html); blindly wrapping current endpoints can repeat uploads, comments, likes, or other writes and stack retries over existing fallback logic. | Defer until repeated transient failures on specific safe read paths justify it. Package installation alone does not fix endpoint rejection or auth behavior. |
| Sibling's AutoMapper 10.1.1, DI/Hosting 7.x, EF Core Sqlite 3.1.32 | Useful for its service architecture and database-backed features. Porting them here would change startup, object ownership, mappings, or storage, with no requested feature requiring that work. Current releases would also need their own platform review. | Keep direct construction, existing models, and explicit mappings. Introduce a package when a concrete feature needs it. |
| Flurl.Http or Grpc.Net.Client | A more uniform client API can simplify a deliberately redesigned transport. The sibling's older pins do not establish support for the target's cookie handling, manual framing, or UWP runtime. | Low immediate benefit compared with migration cost. Keep existing transport for the first package pass. |

There is also a larger modernization path that does **not** require an immediate WinUI 3 rewrite. Microsoft's current [modern UWP guide](https://learn.microsoft.com/en-us/windows/uwp/dotnet-native/modernize-uwp-apps-with-dotnet) says modern .NET UWP is generally available with Visual Studio 2026, UWP tools, and SDK 26100+. It keeps UWP XAML while moving projects/runtime to modern .NET and Native AOT. This could unlock newer managed libraries, but requires SDK-style project conversion, WinRT component/projection changes, and reflection/trimming work. Its example minimum is 19041; preservation of this app's 18362 floor is unresolved. Existing FFmpeg/SYEngine/JSBridge integration and packaging also need separate investigation. Budget weeks, not a maintenance afternoon.

A full [WinUI 3 / Windows App SDK migration](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/overall-migration-strategy) changes the app model as well as the UI/runtime. Windows API availability, window ownership, pickers, browser integration, and playback/packaging would all need attention. For this personal project I would keep that as a separate product decision. Neither sibling has already done this work for you.

## What I would do next

1. Take small same-line updates: WinUI 2, Newtonsoft.Json, NLog 5, and optional ZXing/OpenCC. Decide the stable WebView2 SDK alongside WinUI, with clear ownership of the explicit reference.
2. Update protobuf/runtime tooling in a focused batch, then remove unused generated service stubs and the legacy Grpc runtime. Stabilize RestSharp separately, retaining image-upload behavior.
3. Upgrade the Win32 export dependency family and binding redirects together. Keep its bundled FFmpeg executable as a separate, explicit decision.
4. Give FFmpegInteropX's stable package family its own player migration. It offers more practical potential than a broad architecture rewrite.
5. Modernize WebApi to .NET 10 if you maintain that service; otherwise document its external dependency rather than changing an undeployed local copy and assuming users benefit.
6. Adopt Toolkit 8 controls, MVVM helpers, alternate renderers, or a modern UWP runtime when a concrete feature or maintenance problem earns their migration cost.

No implementation or compatibility validation is included in this report. These are source-supported choices and tradeoffs for deciding the next work, not claims that the candidate versions have been proven to work in this application.

[app-project]: https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/3092a4194e2726f69bea8267f31f163e8de33949/src/BiliLite.UWP/BiliLite.UWP.csproj
[grpc-project]: https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/3092a4194e2726f69bea8267f31f163e8de33949/src/BiliLite.gRPC/BiliLite.gRPC.csproj
[grpc-request]: https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/3092a4194e2726f69bea8267f31f163e8de33949/src/BiliLite.gRPC/GrpcRequest.cs
[log-helper]: https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/3092a4194e2726f69bea8267f31f163e8de33949/src/BiliLite.UWP/Helpers/LogHelper.cs
[upload-image]: https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/3092a4194e2726f69bea8267f31f163e8de33949/src/BiliLite.UWP/Modules/User/SendDynamic/SendDynamicVM.cs#L76
[helper-main]: https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/3092a4194e2726f69bea8267f31f163e8de33949/src/BiliLite.Win32Tools/MainWindow.xaml.cs
[helper-packages]: https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/3092a4194e2726f69bea8267f31f163e8de33949/src/BiliLite.Win32Tools/packages.config
[helper-config]: https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/3092a4194e2726f69bea8267f31f163e8de33949/src/BiliLite.Win32Tools/App.config
[webapi-project]: https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/3092a4194e2726f69bea8267f31f163e8de33949/src/BiliLite.WebApi/BiliLite.WebApi.csproj
