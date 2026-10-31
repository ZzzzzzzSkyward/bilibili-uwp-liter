# Review origin/work aec8dd2493e1434f2dc107ee279299ae11a5db81

Reviewed the new ZIP export and capture importer since `33c254252f7fdf0c688756ac0424232776df3a94`, and read the current `bug_report.md`. Two worthwhile issues; source inspection only, with no tests, builds, or application execution.

## 1. Overwriting an existing ZIP leaves its old tail behind

`src/BiliLite.UWP/Pages/SettingPage.xaml.cs:1126-1127` opens the selected destination with `ReadWrite` and creates a ZIP without truncating the stream. If the user chooses an existing, larger ZIP and the new bundle is smaller, bytes beyond the new archive remain, including potentially the previous ZIP's central directory. Readers can reject the bundle or read stale archive metadata even though the UI reports success.

Suggested fix: set the destination stream's size to zero and seek to position zero before constructing `ZipArchive`, so replacing a file writes exactly one complete new archive.

## 2. An unpacked exported bundle cannot be imported from its root

`tools/import-capture.ps1:36-46` handles ZIP inputs by selecting their `api/` subfolder, but folder inputs keep the bundle root. The exporter puts NDJSON files under `api/`, so passing an already-unpacked bundle directory (an advertised input) finds no root-level NDJSON and exits with `no *.ndjson capture found` instead of producing fixtures.

Suggested fix: normalize both ZIP and directory inputs through the same folder-selection step: use `<bundle root>/api` when present, otherwise keep the root for flat capture folders.
