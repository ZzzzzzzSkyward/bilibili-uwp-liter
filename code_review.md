# Review origin/dev 284a7145d7ce656b8935ca934ad85cd8dba0b8c9

Reviewed all new source changes since `09ee4801aa06a2332ed2da0338217f25296be4b9`, with Luna reviews of the player-list migration and live-room/dynamic-card fixes. Static source inspection only; no tests, builds, linters, application execution, or runtime validation.

## Local playback loses its episode picker without getting the replacement directory

Location: `src/BiliLite.UWP/Controls/PlayerControl.xaml.cs:1049-1053` (removal of the shared list population) and `src/BiliLite.UWP/Controls/PlayerControl.xaml:713-722` (the directory is now the only list button). Missing integration: `src/BiliLite.UWP/Pages/LocalPlayerPage.xaml.cs:85-86`.

Trigger: open a downloaded video/season containing multiple episodes, or a local-media collection containing multiple items. `DownloadPage.OpenPlayer` builds a `PlayInfos` entry for every downloaded episode, and `OpenCollectionItemAsync` similarly supplies the collection playlist. Both open `LocalPlayerPage`, which still only calls `InitializePlayInfo` and never subscribes to `DirectoryRequested`/`DirectoryRowRequested`, calls `UpdateDirectoryInfo`, or supplies a directory model.

Effect: removing `EpisodeList` and `BottomBtnList` removes the working in-player picker for these playlists. The replacement directory button remains collapsed on this page. Users can advance sequentially, but cannot directly select another item from inside the player; they must return to the downloads/library selection and reopen playback. The directory migration covers `VideoDetailPage` and `SeasonDetailPage`, but misses this third caller.

Suggested fix: connect `LocalPlayerPage` to the existing directory drawer before removing its list access. Build rows from its supplied `PlayInfos`, report directory availability, route activated rows to `ChangePlayIndex(index, PlayTargetStart.Resume)`, and refresh the current-row highlight on `ChangeEpisodeEvent`. This preserves the chosen single-list design for local playback too.

## Previous findings and BR-53

The previous live-room parse-failure error-branch dereferences and the ordinary legacy unsupported-card's missing `WebCommand` are fixed in this range; they should not be reported again. BR-53's actual failing response still cannot be identified from this source change. The added step/stage log line is needed to establish the cause of the maintainer's report; these guards alone do not prove that every possible null-reference path is gone.
