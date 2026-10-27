# Upstream bug issue collection and triage

## Scope

Collect issues from `ywmoyue/biliuwp-lite` that describe likely bugs rather than requested features. Include both open issues and closed issues, since a fix may have landed upstream without being present in this target repository. Exclude pull requests, feature requests, changelog or documentation reports, and support-only questions. Keep candidates in oldest-to-newest order by issue creation time and split the catalog into sequential batches of no more than five candidates.

For each candidate, record the issue number, title, URL, state, creation date, and close date when applicable. Summarize the reported behavior, the upstream evidence that it is a bug, and whether the corresponding behavior appears present in this target. Record relevant target paths, the pinned-reference evidence, classification, and any uncertainty. Do not label a report as a bug or as missing from the target without source evidence.

This task is issue collection and source triage only. It authorizes reading the upstream GitHub issue tracker. The local sibling checkout at `/workspace/biliuwp-lite`, pinned to `ca2caddc597b3faf05b4dac6dcb226aa0694365b`, remains the sole behavioral source reference. Do not make application-code changes as part of issue collection. Do not build, restore, run, test, validate, generate protocol code, or call application APIs. `git diff --check` is the only permitted check.

## Setup and baseline

- Target: `/workspace/bilibili-uwp-liter`, branch `work`.
- Origin: `https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter.git`.
- Starting HEAD: `cfa40a8508c6d6e7401885bf6bb081bc5c90b173`.
- `git pull --ff-only origin work` reported `Already up to date`; current setup HEAD is unchanged.
- Issue source: `ywmoyue/biliuwp-lite` (`https://github.com/ywmoyue/biliuwp-lite`).
- Reference checkout: `/workspace/biliuwp-lite`, branch `dev`, pinned at `ca2caddc597b3faf05b4dac6dcb226aa0694365b`.
- No `AGENTS.md` was found under `/workspace`.
- No issue candidates have been collected or classified in this setup step. No application source files have been changed.

## Collection status

- Complete snapshot as of 2026-10-27 23:12:11 UTC from the GitHub REST issues collection sorted by `created` ascending, `state=all`, 100 per page.
- Fetched pages 1–11: pages 1–10 returned 100 objects each and page 11 returned 50, for 1,050 API objects. Excluded 293 pull request objects and indexed all 757 issues, open and closed. Creation range: #1 at 2022-12-11T13:07:05Z through #1499 at 2026-10-21T08:15:39Z.
- Classification retains all 757 audit rows: 477 clear bug candidates, 215 feature requests, 20 administrative/documentation, 15 support-only, and 30 ambiguous/deferred. No issue rows were deduplicated.
- `work/upstream-bugs/issues-index.json` is the portable metadata inventory. `collection.md` records the source and pagination. `classification-merged.json` contains metadata plus classification for all 757 issues; `candidate-queue.json` contains the 477 oldest-first candidates; `exclusions-deferred.json` records the remaining 280 reports and reasons.
- `batch-index.md` lists 96 chronological batches of up to five candidates. Batch 001 source assessment for #5, #9, #14, #21, and #22 is recorded in `work/upstream-bugs/batches/001.md`: two are addressed/not applicable (#5, #22), three remain unconfirmed (#9, #14, #21), and none is a confirmed target defect. Batch 002 is next: #24, #27, #28, #31, and #36.
- Candidate target status is pending source assessment except the five entries covered in Batch 001. Candidate queue and batch index statuses match the assessment. No application-code changes were made.
- Raw issue bodies and API responses are stored outside the repository in `/workspace/work/upstream-bugs/`. No application source files were changed. No build, test, restore, run, app API, or runtime validation was performed.
