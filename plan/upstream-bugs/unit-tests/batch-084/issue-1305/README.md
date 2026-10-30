# Issue 1305: report only items that actually played

Run `bash plan/upstream-bugs/unit-tests/batch-084/issue-1305/run-played-item-marker.sh` from the repository root. It extracts the actual logical-item reset block in `SetPlayItem` and the actual `Playing` state block from `PlayerControl`, then checks same-item refresh, changes to season/episode/CID, and pause versus playing transitions.

The companion issue-105 probe extracts and executes the actual `Dispose` method. It verifies that an already-played paused item has its item and position captured before synchronous player/timer cleanup and is reported afterward, while a never-played item with restored progress retains its saved resume position but is not reported. Natural-end state remains excluded from history. Both probes use local boundaries and do not compile or run the application.
