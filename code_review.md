# Review origin/dev 284a7145d7ce656b8935ca934ad85cd8dba0b8c9

Reviewed all new source changes since `09ee4801aa06a2332ed2da0338217f25296be4b9`, with Luna reviews of the player-list migration and live-room/dynamic-card fixes. Static source inspection only; no tests, builds, linters, application execution, or runtime validation.

## Local playback loses its episode picker without getting the replacement directory

Location: `src/BiliLite.UWP/Controls/PlayerControl.xaml.cs:1049-1053` (removal of the shared list population) and `src/BiliLite.UWP/Controls/PlayerControl.xaml:713-722` (the directory is now the only list button). Missing integration: `src/BiliLite.UWP/Pages/LocalPlayerPage.xaml.cs:85-86`.

Trigger: open a downloaded video/season containing multiple episodes, or a local-media collection containing multiple items. `DownloadPage.OpenPlayer` builds a `PlayInfos` entry for every downloaded episode, and `OpenCollectionItemAsync` similarly supplies the collection playlist. Both open `LocalPlayerPage`, which still only calls `InitializePlayInfo` and never subscribes to `DirectoryRequested`/`DirectoryRowRequested`, calls `UpdateDirectoryInfo`, or supplies a directory model.

Effect: removing `EpisodeList` and `BottomBtnList` removes the working in-player picker for these playlists. The replacement directory button remains collapsed on this page. Users can advance sequentially, but cannot directly select another item from inside the player; they must return to the downloads/library selection and reopen playback. The directory migration covers `VideoDetailPage` and `SeasonDetailPage`, but misses this third caller.

Suggested fix: connect `LocalPlayerPage` to the existing directory drawer before removing its list access. Build rows from its supplied `PlayInfos`, report directory availability, route activated rows to `ChangePlayIndex(index, PlayTargetStart.Resume)`, and refresh the current-row highlight on `ChangeEpisodeEvent`. This preserves the chosen single-list design for local playback too.

## Previous findings and BR-53

The previous live-room parse-failure error-branch dereferences and the ordinary legacy unsupported-card's missing `WebCommand` are fixed in this range; they should not be reported again. BR-53's actual failing response still cannot be identified from this source change. The added step/stage log line is needed to establish the cause of the maintainer's report; these guards alone do not prove that every possible null-reference path is gone.

## Dispositions (2026-11-07, dispatcher, review `dae62b7`)

The finding is correct; not contested. `35f6905` removed the shared list population and the `EpisodeList`/`BottomBtnList`
entries, and `LocalPlayerPage` — the third caller, fed by `DownloadPage.OpenPlayer` and `OpenCollectionItemAsync` — only
called `InitializePlayInfo`. Nothing subscribed the directory events, reported availability or supplied a model, so the
replacement directory button stayed collapsed and a multi-episode download had no in-player picker.

Fixed in `9c8920f`: the page keeps the supplied `LocalPlayInfo`, builds `PlayDirectoryBuilder.FromParts` rows from every
`PlayInfo` (title, cid, duration/1000), reports availability only above one item with the model caption, opens and refreshes
the drawer on request, switches Parts rows through `ChangePlayIndex(index, PlayTargetStart.Resume)`, ignores the current row,
refreshes the open drawer from `ChangeEpisodeEvent` behind an index|count signature, and releases the three handlers on
navigate-from/close. `plan/tests/localdir` (50 assertions; 28 of its 32 page anchors fail against the pre-change body).

Compiled into `BiliLite.Packages_1.0.9.58_x64.msix` together with BR-56; no device run, per the 2026-11-04 default-pass rule.

BR-53 keeps its open status: the failing response is still unnamed until the maintainer sends the `step=`/`stage=` log line.
