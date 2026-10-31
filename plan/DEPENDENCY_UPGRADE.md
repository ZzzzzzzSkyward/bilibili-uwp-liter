# Dependency upgrades — open items only

Batch 1 (UI/JSON/logging/WebView2) and batch 2 (protobuf + gRPC toolchain) are shipped, and the
FFmpeg upgrade is done: `FFmpegInteropX` 2.1.0.81200 with `FFmpegInteropX.UWP.FFmpeg` 8.1.2 replaces
the old 2.0.0-pre7 + 5.1.100 pair, and both the compile-only and the packaging build pass with the
same product sources.
## Remaining migrations (each needs its own session)

- **Community Toolkit 7.1.3 → 8.x.** Six `Microsoft.Toolkit.Uwp*` packages split and renamed; types
  move to `CommunityToolkit.WinUI.*` namespaces across many XAML controls.
- **NLog 6.2.2.** Behaviour changes and feature package splits.
- **FFMpegCore 5.1.0 → 5.5.0** (export helper), **SharpCompress 0.39.0 → 0.50.7** (archive extraction;
  avoid the 0.51 beta), **RestSharp 112.1.1-alpha → 112.1.0** (keep the alpha line's API, the
  multipart upload in `SendDynamicVM.UploadImage` uses `RestClient` directly).

- **Not planned:** converting Newtonsoft to `System.Text.Json`; replacing the hand-rolled gRPC
  transport with `Grpc.Net.Client` (no UWP support statement); dropping `SYEngine.uwp` (still the
  FLV fallback) or `NSDanmaku` (already latest).
## Ground rules

- One batch per commit; the UWP build (`build-release-x64.ps1 -Fast -NoVersionBump`) and the gRPC
  build are the gate. A package update is not evidence of a behaviour fix — Bilibili's server
  changes are not addressed by a version bump.
- `NLog.Config` 4.7.15 is likely unnecessary (`NLog.Config` is an NLog namespace, not a package
  requirement); drop it only with a build that proves nothing references the package assets.
