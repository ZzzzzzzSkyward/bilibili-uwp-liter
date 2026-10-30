# Review — `8b8d436c9648f306cd185b93c17d7d58ca53ebf5`

Read the current `bug_report.md` and reviewed all source changes since `797dc3a75addda681e11e6dc16d03796cb8592cd` (completed review `ecc90db6`), with a GPT-6-luna reviewer. Static source inspection only; no tests, builds, script/application execution, or other validation. The bug report's earlier device/build results were not rerun here.

## Moving the probe runners breaks their repository-root paths

`plan/tests/run.sh:3`, `run-api01.sh:3`, `run-auth01b.sh:3`, and `run-g1.sh:5` still ascend three directories from the script directory. Moving from `work/improvements/tests/` to `plan/tests/` removed a directory level, so these now resolve the repository's parent (`/workspace` here). Running these existing entry points cannot find the application sources or G1 project. Use `../..` from these four script directories.

The opposite adjustment breaks several upstream-bug runners: `plan/upstream-bugs/unit-tests/batch-003/issue-378/run.sh:3` now ascends four directories, landing in `plan/` and looking for `plan/src/...`. Moving `work/upstream-bugs/` to `plan/upstream-bugs/` did not change their nesting depth, so retain five parent traversals. The same introduced error affects batch-003 issue-404, batch-008 issue-105, batch-041 issue-598, fix-batch-002 issues 275 and 358, batch-084 issue-1305, and batch-085 issue-1311. Restore the removed parent traversal in those root calculations.

No application source changed in this increment. The prior detail-selection finding is unchanged; this review does not treat the planning move as fixing it.
