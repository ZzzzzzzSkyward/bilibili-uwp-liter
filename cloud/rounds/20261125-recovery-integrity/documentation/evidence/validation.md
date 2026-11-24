# Documentation validation — LOCAL READY

Owner `round_documentation_executor`, `gpt-6.1-sol/high`; docs-only [grant](../../documentation-dispatch.md).
Validation performed 2026-11-24 20:19–20:22 UTC (round date 2026-11-25 Asia/Shanghai).
This is a documentation verification receipt, not independent documentation acceptance or publication permission.

## Actual commands and reference checks

```bash
bash plan/tests/run-all.sh --help
bash plan/tests/run-all.sh --list
python3 /workspace/work/recovery-integrity/documentation/validate.py
git diff --check
```

Commands exited0 after correcting a private validation-script property-name assumption to the actual source
`DownloadedStatus`/`CleanupFailures`/`RetryCleanupCommand`; this was a checker setup error, not a product failure.
No product/probe/control/package/build/hosted suite was rerun. Help/list only inspect supported arguments and inventory.
The first private baseline-capture command used Git's quoted non-ASCII paths and failed before capturing any data;
it was corrected to NUL-delimited `git ls-tree -rz --name-only`, with no repository mutation.

- All **126 local Markdown links in the twelve owned main documents** resolve, including Markdown heading fragments. Both new PLAN/VALIDATION anchor targets are checked against their actual headings. Execution/evidence links are checked after creation as well.
- Live `--list`: **52 unique executable IDs**, **44 directory batches**, **8 legacy executables /7 legacy logical items**, **51 logical total**; `batch:recovery` occurs once. g1 has its one root runner and the other43 directory entries use `run.sh`.
- All43 directory csproj files parse and target `net8.0`; stab is shell-only. Help exposes the documented only/tools/output/timeouts/repo-root options, and the listed recovery/LM05/AUTH01B selector IDs are real. No selector execution was needed for docs.
- UWP XML parses. `Parsing/Recommend/RecommendResponseParser.cs` and `Services/DownloadPathCoordinator.cs` exist, declare the named classes and each have exactly one Compile entry. Existing favorite/detail/history/library/recommend/download/dialog/helper paths exist. Actual XAML/member names are checked for recommendation StatusText/RetryCommand/AutoLoad and download DownloadedStatus/CleanupFailures/RetryCleanupCommand. These are static references, with no XAML/native compilation claim.
- `git diff --check` passes. The exact main-doc set is the granted twelve paths; no source/test/build/workflow/identity/protected-data/global-instruction semantics were edited. README/AGENTS/REPO/index edits are reusable guidance, with detailed one-time SHA/results in PLAN/ROADMAP/REVIEW_LOG/current round.

## Immutable source and scope binding

Accepted source C: `7fe7a5ff417d0bc2437b5dc3f0507543f744da9b`.
The final executed [915-input manifest](../../verification/evidence/final-common-gate.md) SHA256 is
`0a80ac1cf14e76c9274564a999a88522c23460be6a85e077e093d5e28cd55639`.
Every corresponding **committed C blob** was read via `git cat-file --batch` and SHA256-compared:915/915 exact.
Current working bytes differ from that executed manifest only at the **eleven authorized non-cloud Markdown paths**;
cloud/REPO.md is the twelfth owned main doc and lies outside that915-input manifest. No other key differs.

A separate initial/current manifest checks **889 tracked non-cloud/non-Markdown files**, including product/test/tooling,
build/workflow/assets: all889 unchanged. The915 check additionally protects runnable/help Markdown and all excluded
main documentation outside this grant. This binds docs to the exact accepted complete51/52 execution without rerunning it.
The [source publication receipt](../../recovery/publication.md) separately binds the published22 changed runnable paths.

HEAD, direct dev and origin/dev remain C; index is empty. Existing root/cloud metadata and the authorized recovery
execution/publication receipts remain dirty continuation payload. No whole-worktree clean claim is made.
This executor performed no checkout/pull/stash/reset/clean, staging/object/ref/commit/push/API write, source fix or test run.
Root retains actual-remote/hosted/archive closure and maintenance-plan/quality/memory ownership.

Private checked artifacts: `/workspace/work/recovery-integrity/documentation/` contains `validate.py`,
`validation.json`, `links.json`, `help.txt`, `list.txt`, `runnable-before.json` and `status-before.txt`.
Their raw hashes are recorded by execution.md; no logs/generated/binary artifact is added to the repository.

## Preserved evidence boundaries

Historical50/51 and early51/52/139/208/312 receipts remain their original snapshots. Current source acceptance is
[owning PASS](../../recovery/final-review.md), [fresh PASS](../../final-audit.md),
[boundary PASS](../../boundaries/final-review.md), storage PASS plus final FA-01 closure, and final348/common51/52.
All RF1/RF2/FA-01–09/CR1 are independently CLOSED in that bounded source scope, not native/runtime certification.
No numeric satisfaction decision is made by the documentation executor.

Hosted status comes only from root's actual2026-11-25 04:11 Asia/Shanghai observation supplied in the grant:
only old169b2b0/manual run45434541348, queued/null conclusion; no verified7fe7a5f run/result.
Old-head eventual PASS cannot certify changed runnable bytes/current inventory. Windows/VS/UWP/XAML/native login,
BackgroundTransfer/FAL/file-lock/restart/session/signing-store/device and measurement/identity/account-hold/parked
prerequisites remain explicit. Historical maintainer accepted rows are preserved; new native scenarios are unrun.

## DR1 correction and refreshed static binding

Under `documentation-revision-dispatch.md`, only AGENTS.md, docs/DEVELOPMENT.md and plan/PLAN.md were
corrected from the previously reviewed payload. The four named sync/environment locations now identify
sync-dev.ps1 as local-maintainer guidance and exclude cloud agents from its rebase/stash behavior.
Cloud agents explicitly follow the applicable serialized grant, ordinary fetch, granted fast-forward-only
integration and nonforce push, preserving concurrent work rather than automatically stashing/rewriting it.

At2026-11-24 20:28:42 UTC, refreshed private `dr1-binding.json` verifies exactly those3 main-doc deltas,
9 other accepted main documents unchanged, and both frozen plans unchanged:
current continuation `ee966e86b1b0889c9dccbd242aa353372d3fbb4ccadbc0c8ab8ba27521cf2984`,
old foundation plan `50191998f71550466956068878604f7bf87682b46c4d51992a9964194ae99402`.
Root alone corrects DR2/quality assessment. No plan payload, source, script, CONTRACT, index or ref was edited here.

The unchanged private validator exited0:136 local links resolve across main/owning/review documentation
(126 in main docs); static CLI/XML/type/XAML references remain valid, whitespace passes, all915 committed C
inputs equal executed bytes, current915 delta remains exactly11 authorized Markdown keys and all889
non-Markdown tracked inputs are unchanged. HEAD/dev/tracking remain C and index empty. Only read-only help/list
were refreshed; no source suite/control/package/build/native/hosted run occurred. Revised hashes and raw
receipt hashes are frozen in execution.md for the same independent reviewer, with no staging/publication grant.
