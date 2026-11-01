# Review origin/work 399807da3a10f7d44c6868c027b4829e270dd736

Reviewed the complete source range since `70c1e2f29a903033baed756a6ca9711b0e065ff5` and read the current `bug_report.md`. The Watch Later identity and unknown-total fixes address BR-22/BR-23. Two worthwhile issues remain below. Source inspection with Luna reviewers only; no tests, builds, scripts, or application execution.

## BR-24 remains incomplete after switching parts by keyboard

`src/BiliLite.UWP/Controls/PlayerControl.xaml.cs:596–598` calls `SetPlayItem` directly using `EpisodeList.SelectedIndex`. That method updates `CurrentPlayIndex` at line 1114 but never updates the player's `EpisodeList.SelectedIndex`. The page's `ChangeEpisodeEvent` handler only synchronizes its separate `listEpisode` control (`Pages/VideoDetailPage.xaml.cs:901–908`).

Concrete case: start at P1 of a two-part video with another collection/playlist row after it, then press X. P2 plays, but the player's selected index is still 0. The new sequence preview correctly uses `CurrentPlayIndex` and advertises the next outer video. Clicking that now-visible next button at `PlayerControl.xaml.cs:2719–2725` still takes the stale “another part remains” branch and replays P2. Repeated keyboard-next also keeps replaying P2 rather than reaching the next video.

The keyboard selection drift predates this increment; the new outer-next button and target preview expose it as an incomplete shared-navigation fix. Suggested fix: synchronize the player's selected index with the actual playing index, guarding `SelectionChanged` to prevent duplicate playback. Base next/previous boundary decisions on the same current index used by the sequence state, and route part changes through one consistent path.

## SDK fallback is configured after the restore that needs it

`build-release-x64.ps1:163–183` discovers the standalone dotnet SDK and sets `MSBuildSDKsPath` for `-Fast -SkipProjectReferences`. But adding the documented `-Restore` switch runs solution restore at lines 149–158 before that setup. On the machine this fallback targets—VS MSBuild cannot resolve `Microsoft.NET.Sdk`, while a standalone dotnet SDK is installed—the solution restore still evaluates the SDK-style `BiliLite.gRPC` project and fails before reaching the fallback. Package changes therefore cannot be restored using this combination of switches.

Suggested fix: move SDK discovery and environment setup before the optional restore, keeping the build-only `BuildProjectReferences=false` argument separate. Alternatively, explicitly restore through the compatible dotnet SDK before invoking VS MSBuild. The SDK path must be available to both evaluations.
