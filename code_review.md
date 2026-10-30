# Review — `d8757eba8f3179bca152622e6e51da1819aac508`

Read the current `bug_report.md` and reviewed all source changes since `8b8d436c9648f306cd185b93c17d7d58ca53ebf5` (completed review `8baf1194`), using GPT-6-luna reviewers. The remote advanced during review; both the detail-load fix and the newly arrived runner-path fix are included. Static source inspection only; no tests, builds, script/application execution, or other validation.

## The runner-path fix leaves six reported entry points broken

The main `plan/tests/` wrappers and upstream batches 002/003 are corrected. The following runners still calculate their repository root with only four parent traversals at line 4:

- `plan/upstream-bugs/unit-tests/batch-008/issue-105/run-dispose-stop-order.sh`
- `plan/upstream-bugs/unit-tests/batch-041/issue-598/run-natural-end-report.sh`
- `plan/upstream-bugs/unit-tests/fix-batch-002/issue-275/run.sh`
- `plan/upstream-bugs/unit-tests/fix-batch-002/issue-358/run.sh`
- `plan/upstream-bugs/unit-tests/batch-084/issue-1305/run-played-item-marker.sh`
- `plan/upstream-bugs/unit-tests/batch-085/issue-1311/run-subtitle-language-label.sh`

Each script directory is five levels below the repository root, so these paths land in `plan/` and attempt to read nonexistent `plan/src/...` files. Running any of these existing entry points fails before reaching its probe. Restore `../../../../..` in their root calculations, as already done for batch-003. This is the remaining part of the previous migration finding.

The detail-selection loading flag finding is resolved: rejected concurrent calls no longer overwrite `_id`, and the active request releases the flag in `finally`. No other worthwhile issue found in that change.
