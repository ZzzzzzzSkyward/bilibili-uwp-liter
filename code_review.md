# Review — `765be4644868e6f38d482ebe9061bd4727b796ce`

Read the current `bug_report.md`, including BR-16, and reviewed all source changes since `709ecf8f7f732c2075d22397419669613408c9b4` (completed review `2a1515e8`), using GPT-6-luna reviewers. Static source inspection only; no tests, builds, script/application execution, or other validation. The report's device/build observations were not rerun here.

## A failed gRPC load-more page leaves every retry without its cursor

`src/BiliLite.UWP/Modules/Home/DynamicVM.cs:93-101` now keeps the source pinned after a failed append and sets `_lastPageFailed` so the footer can retry. However, `RequestGrpcSourceAsync()` has already cleared `_historyOffset` and `_updateBaseline` on failure (`170-171`, also `178-179` for an unusable response). Let gRPC page 1 succeed, then let page 2 fail once and click load more again. `LoadMore()` retries `_page + 1` on the same source with those fields now null. `Services/GrpcService.cs:93-97` assigns them to the protobuf request's string properties, which reject null before sending; even an empty-string workaround would lose the cursor needed to resume that page. Retrying cannot recover without refreshing the feed.

Preserve the last successful gRPC cursor/baseline when an append fails. Clear them only when actually restarting page 1 or switching sources, or capture and restore the request cursor for retries. This keeps the existing rows and silent failure behavior requested in BR-16 while allowing a transient failure to recover.

The raw gRPC rejection is no longer selected for the user-facing toast, and failed append requests no longer replace existing rows with another source's first page. No other worthwhile issue found in the home-feed change or CS2012 holder lookup/retry script.
