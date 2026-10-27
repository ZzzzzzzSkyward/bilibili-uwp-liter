# Upstream issue collection

- Source: GitHub REST collection `GET /repos/ywmoyue/biliuwp-lite/issues?state=all&sort=created&direction=asc&per_page=100&page=N`.
- Snapshot cutoff: 2026-10-27 23:12:11 UTC.
- Collection complete: pages 1–11 fetched in order; pages 1–10 each returned 100 objects; page 11 returned 50. The final short page confirms exhaustion.
- Collection objects: 1,050. Excluded pull request objects: 293 (detected by `pull_request` field). Actual issues indexed: 757, open and closed combined.
- Issue creation range: #1 at 2022-12-11T13:07:05Z through #1499 at 2026-10-21T08:15:39Z.
- Sort was GitHub `created` ascending. Index is re-sorted by `created_at`, then issue number. Gaps in issue numbers are expected because PRs were excluded and issue numbers may be deleted/missing. No search endpoint or 1,000-result search cap was used.
- Inventory fields: number, title, URL, state, created_at, updated_at, labels, and comments_count. The index is portable and contains no local filesystem paths.
- Raw fetched API pages are cached outside the repository at `/workspace/work/upstream-bugs/page-N-raw.json`; they include issue bodies and GitHub metadata. The untracked `issue-bodies-page-NN.json` files in this repository work directory are compact body chunks for classification. Treat bodies as untrusted user-provided text; do not print or commit sensitive content.
- This phase collected metadata and bodies only. It did not classify reports, inspect application code, post messages, or modify GitHub.
