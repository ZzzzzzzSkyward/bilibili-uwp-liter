# Dependency upgrades — open items only

Shipped: batch 1 (UI/JSON/logging/WebView2), batch 2 (protobuf + gRPC toolchain), FFmpegInteropX
2.1.0.81200 with `FFmpegInteropX.UWP.FFmpeg` 8.1.2, the maintainer's 2026-10-31 bump (OpenCCNET
1.1.0, ZXing.Net 0.16.11, RestSharp 114.1.0 — the only direct `RestClient` use is the multipart upload
in `SendDynamicVM`, and it compiles unchanged), and the unused-package removal (`gfoidl.Base64`,
`Microsoft.Toolkit.Uwp.UI.Media`, `NLog.Config` 4.7.15 — NLog 5 configures XML in code, so the NLog 4
package only shadowed assemblies).

## Remaining migrations (each needs its own session)

- **Community Toolkit 7.1.3 → 8.x.** Six `Microsoft.Toolkit.Uwp*` packages split and renamed; types
  move to `CommunityToolkit.WinUI.*` namespaces across many XAML controls.
- **NLog 5.5.1 → 6.x.** Behaviour changes and feature package splits.
- **`BiliLite.Win32Tools` export tool**: FFMpegCore 5.1.0 → 5.5.0 and SharpCompress 0.39.0 → 0.50.7
  (avoid the 0.51 beta). Both are `packages.config` `HintPath` references, so the upgrade also rewrites
  the `packages/` layout the export tool loads at runtime.

- **Not planned:** converting Newtonsoft to `System.Text.Json`; replacing the hand-rolled gRPC
  transport with `Grpc.Net.Client` (no UWP support statement); dropping `SYEngine.uwp` (still the
  FLV fallback) or `NSDanmaku` (already latest).

## Ground rules

- One batch per commit; the UWP build (`build-release-x64.ps1 -Fast -NoVersionBump`) and the gRPC
  build are the gate. A package update is not evidence of a behaviour fix.
- A package is removed only after a grep of every namespace it exports; a package that compiles but
  is never referenced is still a shipped assembly.
