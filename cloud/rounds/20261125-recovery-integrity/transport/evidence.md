# Publication transport proof evidence

Status: **158 LOCAL CHECKS PASS (original35 + review123) / READY FOR INDEPENDENT RE-REVIEW / NO REMOTE WRITES.** Main requirements are [requirements.md](requirements.md); genuine regression red→green and the fixed review boundaries are in [review-repair-evidence.md](review-repair-evidence.md). No publication, permission success, native/CI PASS or terminal auth recovery is claimed.

Read `AGENTS.md`, cloud contract/memory INDEX and relevant recovery run/pitfalls, root `remote-verification.md`, execution dispatch, recovery commit-window/execution and actual deferred tool metadata. The proof-specific prohibition on source/index/ref writes takes precedence over routine startup fetch/checkout/pull mutations; none was performed here. Only `transport/**` Markdown and `/workspace/work/recovery-integrity/publication-transport/**` were written. No credentials/auth files/process memory/settings were inspected or modified.

## Read-only live facts

Connected GET `https://api.github.com/repos/ZzzzzzzSkyward/bilibili-uwp-liter/git/ref/heads/dev` returned `isError=false`, `ref=refs/heads/dev`, `object.type=commit`, SHA `e928bf0e818869c03485f8ef6f31fa47e98ac0f0`. Independent local reads at the beginning/end showed HEAD/dev/origin/dev equal that SHA and an empty cached name list. Existing root/recovery edits were retained. The live read is point-in-time proof, not a future execution preflight or write-permission test.

GET immutable Git Data commit `e928bf0e818869c03485f8ef6f31fa47e98ac0f0` returned tree `53bc841c54f3daa6bf94ee053c011a46cdaa418b`, sole parent `bdba9ebcddb3de146c4fdf92919c5a3c57eaf90f`, verification `unsigned`, null payload/signature, normalized UTC dates. Its actual original local canonical bytes have `+0800` identities and a terminal message LF. Reconstruction used the **API metadata alone**, found the exact matching canonical SHA, and subsequently byte-compared equal to the immutable local commit (288 bytes). No local commit was made.

An independent existing signed sample was selected read-only from `github/docs` main, then fetched by immutable Git Data SHA `fa03f00dcd9fdcb13e6964774118b2d4df4dba95`: `verification.reason=valid`, PGP signature and exact payload present. It has two parents and `-0800` headers. Payload+signature reconstruction with the original terminal signature newline produced **exact SHA equality** (1159 bytes); removing that newline produced a different SHA and was not accepted. The production verifier separately rejects any multi-parent commit; this merge sample tests only lossless historical object reconstruction. Both exact raw commits were imported with `git hash-object -w -t commit --stdin` into separate scratch `immutable-objects.git`, with no refs.

Tool metadata is saved at `.../tool-metadata.json`. Observed creation tool inputs have no author/date/signature controls; `update_ref` exposes expected_sha/force and advertises mismatch rejection. This is a supported interface contract, not proof of backend CAS implementation or repository write access. No creation/update tool was invoked during this proof.

## Official documentation

Applied `read-docs` skill. Firecrawl failed for exhausted credits; no documentation was supplied by that service. Official pages were instead retrieved directly, read and saved as `commits.html`, `trees.html`, `refs.html`, `blobs.html` (the content negotiation returned Markdown). They establish:

- [Git commits](https://docs.github.com/en/rest/git/commits?apiVersion=2022-11-28): creating an object does not itself move a branch; author defaults to authenticated user/current date; committer defaults from author; verification exposes signature and signed payload.
- [Git trees](https://docs.github.com/en/rest/git/trees?apiVersion=2022-11-28): nested entries, explicit modes, either content or SHA, `base_tree` preservation and root SHA/truncated metadata. No base tree would delete omitted files, so it is mandatory here.
- [Git refs](https://docs.github.com/en/rest/git/refs?apiVersion=2022-11-28): normal update with force false enforces fast-forward; body has no expected-old field. The distinction from wrapper `expected_sha` and the append-only race argument are explicit in requirements.
- [Git blobs](https://docs.github.com/en/rest/git/blobs?apiVersion=2022-11-28): UTF8/base64 uploads exist, but this text-only proposal uses documented inline tree contents. No live blob upload was needed.

The supplied version query currently serves docs that mention `X-GitHub-Api-Version: 2026-03-10`; the connected tool exposes no header/version parameter. Required listed fields are documented on the retrieved page; no version-override claim is made.

## Reproducible scratch checks

```bash
python3 /workspace/work/recovery-integrity/publication-transport/proof.py
```

Exit 0; final receipt `{"status":"PASS","checks":158}`. All original35 checks remain, plus123 review regressions; full case names/counts are in `.../proof-results.json`. The added suite fails46 checks against the frozen original helper, retaining77 controls, then passes123/123 after the repair. All fixture staging/config/ref/object writes occur under temporary scratch repositories and are removed at test end; immutable-object imports remain in separate scratch `immutable-objects.git`. The real source repository is used only for a final immutable `cat-file` byte comparison.

The checks cover exact staged LF bytes despite CRLF working bytes; executable mode; bounded payload chunks; source-index/read preservation; unreferenced canonical commit verification/import; wrong remote/ref/tree/message/extra parent/truncated response/changed index/manifest rejection; invalid UTF8/NUL/symlink/deletion/unexpected path rejection; two-ref local CAS failure leaving the other ref unchanged; exact successful local adoption with unchanged raw index, empty cached diff, unrelated dirty text and original CRLF working bytes; clean lock removal; unsigned and signed immutable metadata reconstruction and separate object imports; corrupt/unsupported signature and unsupported signed-header rejection.

Current helper SHA256: `f72a4873ff232296c9826877e6d261eaff6dd0d16683f732465b974f22d8e5b0` (`transport.py`). Proof script SHA256: `8cef00bcdcd9d57d861f75d514985bef6ad96546719cd00ce327808ef7e51229`. Combined results SHA256: `78fd50a7f1e90523868d2aca505185ff008d776376d1f39ac388bf5a9f63808c`. Fixed recovery manifest SHA256 remains `8a241dc4334a551e37822dac463878a5fa026e88c727df401e2382a31d4a87f7`. Further baseline/regression hashes are in the repair evidence. Reviewer should re-read/check these exact artifacts; scratch helpers are not committed product files. At initial proof the manifest totaled approximately365KB of working files (largest≈52KB); actual staged payload is generated only by the source executor after its explicit window. No source payload was emitted into chat.

Remaining gates: independent transport review; explicit source-owner fallback publication grant; exact staged canonical test bindings/live old ref; real server-created tree/commit response and exact prepublication import; actual nonforce expected-SHA branch update; local CAS adoption; independent final source review/full current51/52 gate/hosted observations as already assigned. Lack of write permission or unsupported metadata is a safe stop, not permission to reconnect/change credentials or fabricate success.
