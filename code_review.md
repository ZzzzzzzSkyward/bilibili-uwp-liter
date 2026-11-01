# Review origin/dev 503ceae2c47d8201f6fdabb324b26e0df0d83ae6

Reviewed the complete source range after `c8dbf82afce078450ce7abd7a3676a203be675b7`. Static inspection with Luna reviewers only; no builds, tests, linters, or application execution. BR-32 is already tracked in `bug_report.md` and is not repeated here.

## Load all never stops when a successful page makes no progress

Location: `src/BiliLite.UWP/Models/PlayDirectory.cs:420-426` (`PlayDirectoryLoadAll.RunAsync`).

Trigger: click “加载全部” and receive a page containing only already-loaded IDs (or no usable rows), with `has_more=true`. `MediaListPagingState.Append` keeps the old `LastAid` when no rows are added (`Models/MediaListResources.cs:294-302`), and the loader still reports success. The runner checks only success and whether more pages exist, so it requests the same cursor again. If that cursor keeps producing the same response, loading continues indefinitely with an unchanged count until the user cancels, wasting requests and device resources. The existing automatic-next loop has an attempt limit; this new loop has no equivalent progress check.

Suggested fix: compare the cursor and loaded count before and after each page. Stop the run when neither advances, preserve the loaded rows, and leave a practical retry path. A bounded allowance for consecutive stalled pages is also reasonable; do not mark the list exhausted merely because one page stalled.
