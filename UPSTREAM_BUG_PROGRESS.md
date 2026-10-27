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

Issue inventory and candidate comparison are pending. Add accepted candidates below in creation-date order, retaining open and closed reports and grouping each sequential set into batches of at most five.
