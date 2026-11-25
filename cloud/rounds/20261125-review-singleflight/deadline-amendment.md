# Deadline exact-literal static amendment

Status: STATIC PREPARATION PASS; v2 has not been executed or activated. No Git/index/ref mutation occurred. Sole repository output for this amendment is this report. Original/v1 scripts and first static report/review remain unchanged.

Prepared private v2: `/workspace/work/review-singleflight/deadline/timed_push_v2_exact_literals.py`; SHA256 **`086649cdad64e14d0176d8fc3f59fb7fc668f8c0203ae0f1591c0983bbbdada8`**. Evidence `/workspace/work/review-singleflight/deadline/static-v2.json` includes baseline mode/blob proof and exact substitutions; `/workspace/work/review-singleflight/deadline/static-v2.diff` holds the narrow diff. `git diff --no-index` returned its expected difference status1 after saving the diff; static preparation itself succeeded.

The original SHA256 remains `99d8eacaf1d38de7b6317a846cc6fe39690cd01b1a31c6ef867da1650edd03bc`; v1 remains `b8f4f8be1d6ead09450e415460d254cb378e0cc628dc211bdb596b04e3585d3c`. Syntax compiled via `compile()` without executing either script or writing bytecode. Function AST comparison confirms only `validate_inputs` changed; local/remote/ref/manifest checks, absolute wait, push and receipt control flow are unchanged.

The amendment permits exactly these future literal payload paths, each bound to regular mode100644:

- `src/BiliLite.UWP/Modules/Season/SeasonReviewVM.cs`
- `plan/tests/season1/Season1Probe.csproj`
- `plan/tests/season1/Season1Stubs.cs`
- `plan/tests/season1/SeasonReviewOwnershipProbe.cs`
- `plan/tests/season1/extract-lifetime.py`
- `plan/tests/season1/Program.cs`
- `bug_report.md`
- `plan/PLAN.md`
- `plan/VALIDATION.md`
- `docs/ROADMAP.md`

Read-only `git ls-tree HEAD` confirms all nine existing literal paths are regular100644 blobs, including `extract-lifetime.py`; none is100755 in this baseline. `SeasonReviewOwnershipProbe.cs` is absent from baseline and its permitted new-file mode is explicitly100644. Thus symlink mode120000 and any executable-mode drift are rejected. Final actual modes/blobs/hashes are still independently checked against the reviewed manifest and commit.

Current-round metadata and the three existing named cloud-memory metadata paths retain their exact v1 policy: plain `.md`/`.json`, regular100644 only. No broad source/test/main-document or cloud-memory prefix was added. Unchanged/nonexistent paths must be excluded from the eventual actual payload; eligibility is not a requirement to include a path.

Fixed deadline09:22:09UTC /17:22:09AsiaShanghai, parent86e0e56e67c05aadc34500ee5f65bdcdce3a5277, protected actual-server master7ac3146808bd1410dcccf2ec566ea09e2900cc0e and absent local master map remain hard-bound. Every timing/publication guard is retained. This amendment grants neither implementation nor staging/commit/activation/push. Await independent amended-requirements and implementation/doc acceptance, independently reviewed final exact manifest, writer release and separate root exact final transaction grant. Private v2 and this report are now frozen/released for independent static review.
