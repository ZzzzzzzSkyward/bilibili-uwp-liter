# Deadline publisher static preparation

Status: STATIC PREPARATION PASS; not activated, no grant/manifest supplied, and no Git/index/ref mutation performed. This report is the publisher’s sole repository output under the preparation grant. Product/test/main-document files were not written.

## Fixed bindings

- Deadline: **2026-11-25 09:22:09 UTC / 17:22:09 Asia/Shanghai**, epoch `1795598529000000000` ns. The interval is never restarted.
- Required sole parent and current local/tracking dev: `86e0e56e67c05aadc34500ee5f65bdcdce3a5277`.
- Protected actual server master: `7ac3146808bd1410dcccf2ec566ea09e2900cc0e`; both `refs/heads/master` and `refs/remotes/origin/master` remain absent locally.
- Current worktree contains root-owned metadata changes; this is expected during preparation. The final transaction cannot run until the exact reviewed commit exists and index/worktree are globally clean.

## Prepared private artifact

- Original retained unchanged: `/workspace/work/deadline-checkpoint/timed_push_with_docs.py`; SHA256 `99d8eacaf1d38de7b6317a846cc6fe39690cd01b1a31c6ef867da1650edd03bc` confirmed before and after preparation.
- Prepared version: `/workspace/work/review-singleflight/deadline/timed_push_v1_archive_only.py`; SHA256 `b8f4f8be1d6ead09450e415460d254cb378e0cc628dc211bdb596b04e3585d3c`.
- Static evidence: `/workspace/work/review-singleflight/deadline/static-v1.json`; exact original/prepared diff: `static-v1.diff`; read-only current actual server proof: `static-remote-refs.txt`, all under the same private directory.
- Python syntax compiled with `compile()` without executing the script or writing bytecode. Exact narrow substitutions and deadline epoch were asserted. No test files or probe runs were created.

The v1 payload policy permits only mode `100644` plain `.md`/`.json` files in `cloud/rounds/20261125-review-singleflight/` and the specifically named metadata paths `cloud/memory/INDEX.md`, `cloud/memory/deadline-extension-20261125.md`, `cloud/memory/runs/20261125-review-singleflight.md`. No source, tests, prior-round namespace, broad cloud-memory prefix, or main-document path is permitted. These are preparation bounds, not an exact final transaction grant. Final actual paths, blobs, modes, SHA256 hashes and sole checkpoint commit must still be independently reviewed and bound in the private manifest/root grant. If the implementation is accepted, a versioned script with separately granted exact source/test/main-document literals is required; never grant a broad source prefix.

All original publication guards remain: expected sole parent; dev branch; local master map; unchanged tracking parent; empty index and globally clean worktree; exact changed-path manifest; per-file mode/blob/content hashes; actual server dev/master proof; only nonforce `git push origin HEAD:refs/heads/dev`; activation strictly before T−40; absolute wall-clock wait; final remote/local preflight at T−30; no push before target; fail closed at scheduling delay >=1 second. The fixed parent/master/absent-local-master bindings were additionally enforced in input validation. The original script remains intact.

## Proposed freeze and publication timeline

1. **09:12:09 UTC (T−10m):** freeze new scope; choose reviewed implementation checkpoint or archive-only fallback, preserving explicit unfinished status.
2. **By 09:17:00 UTC:** all writers freeze/release; root provides exact candidate file ownership and final archive content; implementation/doc reviewers finish or select archive-only.
3. **By 09:19:00 UTC:** independently review exact final content and candidate manifest; only after an explicit exact transaction grant may the publisher stage only the named paths and create one conventional checkpoint commit with the fixed sole parent.
4. **By 09:20:30 UTC:** review actual commit’s exact path/mode/blob/hash manifest, confirm global clean state, bind checkpoint SHA and manifest SHA in final private root grant; publisher waits for that separate exact grant.
5. **Target launch no later than 09:21:15 UTC (T−54s):** activate only the reviewed script version with the exact private grant; script rejects launch at/after **09:21:29 UTC (T−40s)**. Do not wait until the hard boundary.
6. **09:21:39 UTC (T−30s):** script independently repeats actual server and local preflight; any drift fails closed and preserves the commit.
7. **09:22:09 UTC:** previously authorized script alone starts the safe nonforce dev push. No post-cutoff implementation. Observe terminal receipt/server refs; report verified publication or preserved blocked checkpoint honestly.

No stage/commit/push/activation is authorized by this preparation report. Await the separate root exact final transaction grant and independently reviewed final manifest.
