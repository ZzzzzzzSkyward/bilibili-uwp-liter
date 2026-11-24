# Independent publication transport review

Status: **PASS — P1-TR1 / P1-TR2 / P2-TR1 CLOSED after independent re-review.** No open P0/P1/P2 transport finding. This approves the bounded protocol and exact reviewed scratch artifacts; it grants no production staging/object import/ref/API write. Root must still issue the exact source executor's publication window after source acceptance, with the reviewed final manifest and test binding.

## Re-review closure

Read `review-repair-evidence.md`, revised requirements/evidence/runbook, root's review-iteration plan, the baseline-to-current helper/proof diff and the full new regression suite. Independently reproduced both sides: the frozen original helper exits 1 with **123 checks, 46 failures / 77 preserved controls**; the repaired combined proof exits 0 with **158 PASS = original35 + review123**. The original35 assertion bodies remain unchanged. Temporary red/regression staging/object/ref fixtures were confined to scratch and removed afterward. Recomputed hashes match the released repair evidence.

- **P1-TR1 CLOSED:** `oldrefs` now requires HEAD's immediate symbolic target dev, independently rejects symbolic dev/tracking refs, and checks resolved old-SHA equality. Common validation covers prepare/verify/import/check/emit/adopt. Adoption repeats validation at its mutation boundary and uses `update-ref --no-deref --stdin`, preserving one two-ref old-O CAS transaction. Tests cover both aliases at every phase, exact unrelated-ref/index/worktree preservation on rejection, direct-ref success and aliases injected immediately at the CAS command. The race cases prove master cannot be advanced through either allowed destination; they do not claim a global concurrent-ref lock.
- **P1-TR2 CLOSED:** version2 preparations record each owned working file's raw hash/size and regular-file mode/device/inode/link-count/mtime/ctime independently of staged bytes. No-follow descriptor reads, before/after identity checks and nonblocking nonregular rejection prevent the reported replacements and FIFO hang. Every dependent command validates these receipts; adoption validates again immediately before and after its CAS. Version1 fails closed. Source/test/evidence drift, missing/symlink/directory/FIFO/new-inode/mode replacements reject; CRLF-working/LF-staged and unrelated dirty-file controls succeed. Actual tested-input-to-staged-byte equivalence remains an explicit executor gate, as documented.
- **P2-TR1 CLOSED:** requirements now correctly account for 11 product/project + five recovery probe + two aggregate/control + one execution + three evidence paths =22; the manifest bytes/hash are unchanged.

Accepted current SHA256 bindings under `/workspace/work/recovery-integrity/publication-transport/`:

| Artifact | SHA256 |
| --- | --- |
| `transport.py` | `f72a4873ff232296c9826877e6d261eaff6dd0d16683f732465b974f22d8e5b0` |
| `proof.py` | `8cef00bcdcd9d57d861f75d514985bef6ad96546719cd00ce327808ef7e51229` |
| `review_regressions.py` | `c491186102bd26989b3b2c6d354833b515d88e1a57f55bad775faa25f5be7f6b` |
| `review-red-results.json` | `6744919bfb60dcd418dc70b8b6f935fd313b08c0f44e10ee9e2b1820e1ec28f7` |
| `review-regression-results.json` | `637b8bc604097c992dec2506d0c015607dca454ea16cfaf74d32e51d831a1fa7` |
| `proof-results.json` | `78fd50a7f1e90523868d2aca505185ff008d776376d1f39ac388bf5a9f63808c` |
| `recovery-manifest.json` | `8a241dc4334a551e37822dac463878a5fa026e88c727df401e2382a31d4a87f7` |

Read-only local re-review checks still show direct dev/tracking and HEAD → dev, all at `e928bf0e818869c03485f8ef6f31fa47e98ac0f0`, empty cached path list, and unchanged raw index SHA256 `adb455081ae826fe8d866a7f2c2878846e841d0ff92e49cc5342ef530afa56e5`. Fresh connected GET of `git/ref/heads/dev` returned isError=false, exact ref/type and the same SHA. This is a point-in-time read, not a future publication preflight. No production source/index/ref/object/API write was performed by this review.

Remaining limits: source owners must honor the exclusive frozen-source window; raw worktree files have no global Git lock. Checks are boundary observations, and drift after remote publication must be reported without destructive repair or rollback. API write access, real creation-response/metadata compatibility, server identity/rules/payload limits, actual nonforce publication/readback/local adoption, terminal auth, CI and Windows/native gates remain unproved. The append-only/single-parent fast-forward safety argument and wrapper expected-SHA contract remain correctly distinguished from a documented backend CAS or global remote/local transaction. A changed ownership path set needs the separately reviewed manifest/window already required by the protocol.

## Original findings and evidence — historical, now closed

The sections below retain the original CHANGES evidence and actionable repair requirements. Their old helper hashes and line numbers identify the reviewed baseline, not the accepted repaired helper above.

### P1-TR1 — symbolic local refs can move an unauthorized branch — CLOSED

`transport.py:67-72` checks resolved SHA equality without requiring `refs/heads/dev` and `refs/remotes/origin/dev` to be direct refs. `adopt` at lines 301-303 uses default dereferencing `git update-ref`. A tracking ref can therefore alias another branch while passing every old-SHA check.

Independent scratch reproduction: initialize dev at O, create scratch master at O, set scratch `refs/remotes/origin/dev` symbolically to `refs/heads/master`, then prepare/verify/import/adopt the normal one-parent C. The helper returned `ADOPTED_LOCAL_REFS`; **master also advanced to C**, and the tracking alias remained. No production ref was involved. This violates the exact two-ref scope and protected branch rule. The actual source refs are currently direct, which does not make accepting an unsupported state safe.

Required fix: reject symbolic dev/tracking refs in every shared state validation, preserve the required HEAD → dev symbolic relationship, and make the final transaction incapable of following either allowed ref into another target (for example, explicit no-dereference updates combined with direct-ref validation). Retain old-O checks and all-or-nothing behavior. Add scratch cases for dev and tracking aliases, asserting rejection and byte/target identity of every unrelated ref, plus a direct-ref success case. Revalidate this boundary immediately before adoption.

### P1-TR2 — owned working source/evidence drift is not rejected — CLOSED

`prepare` records staged blobs/index/tree but no fingerprint of the owned working files. `load_prepared` at lines 215-225 only rechecks the manifest file, refs, raw index and copied-index tree. Consequently an owned source, test or execution-evidence file can change after preparation while `verify`, `import`, `check` and `adopt` still accept the preparation.

Independent scratch reproduction: prepare and verify/import staged `A.cs`, then overwrite working `A.cs` without staging it. `check` still returned `READY_FOR_OWNER_REMOTE_UPDATE`, and subsequent scratch adoption succeeded. The frozen staged blob remains intact, but the helper has not established the required unchanged owned-source/test/evidence binding. The dispatch explicitly requires rejecting changed source; a written instruction to freeze it is not that check.

Required fix: bind every owned manifest working path to its preparation-time raw bytes and regular-file state, and revalidate before every dependent publication check and local adoption, or integrate an equivalent explicit frozen-source receipt gate that the helper verifies. Compare working files against their own captured baseline: preserve the intentional CRLF-working/LF-staged support and unrelated unstaged root edits. Reject missing/replaced/changed owned files without repairing/staging them. Add scratch regressions for owned source/test/evidence drift, deletion/type replacement and unchanged filtered-working success; assert no ref/index/worktree mutation on rejection. Keep actual test-input-to-staged-byte verification an explicit executor gate.

### P2-TR1 — fix manifest inventory prose — CLOSED

The reviewed 22-path manifest contains **11 product/project paths + five recovery probe paths + two aggregate/control paths + execution.md + three evidence paths**. Requirements currently call the first group 13 while also counting the two tooling paths separately. Correct that sentence; the actual fixed manifest hash/path set remains authoritative.

### Original independent evidence and accepted design

- Read AGENTS/cloud contract/repository parameters, relevant recovery memory/pitfalls, transport dispatch/review dispatch, remote verification, recovery conditional window/source execution ownership, all released transport documents and both scratch Python files. No startup fetch/checkout/pull was run because this review expressly prohibits source/index/ref mutation.
- Re-ran `python3 /workspace/work/recovery-integrity/publication-transport/proof.py`: exit 0, **35 checks PASS**. All its mutation fixtures and the additional reproductions above were confined to scratch repositories; additional temporary fixtures were removed. Existing proof coverage does not include the two failing cases above.
- Confirmed current callable GitHub tool metadata independently: create_commit has message/tree/parent controls but no author/date/signature controls; update_ref exposes expected_sha/force and promises expected-SHA mismatch rejection.
- Independently GET-read actual dev and both immutable proof commits through connected GitHub. Actual dev remains `e928bf0e818869c03485f8ef6f31fa47e98ac0f0`; local HEAD/dev/origin/dev equal that O, cached path list empty, both local refs direct. Unsigned e928bf0 has tree `53bc841c54f3daa6bf94ee053c011a46cdaa418b`, sole parent bdba9ebcddb3de146c4fdf92919c5a3c57eaf90f, and unsigned/null verification payload/signature. The independent github/docs sample `fa03f00dcd9fdcb13e6964774118b2d4df4dba95` has PGP payload/signature and two parents; production verification correctly rejects multiple parents despite supporting this historical reconstruction test.
- Read the saved official [commit](https://docs.github.com/en/rest/git/commits?apiVersion=2022-11-28), [tree](https://docs.github.com/en/rest/git/trees?apiVersion=2022-11-28), [ref](https://docs.github.com/en/rest/git/refs?apiVersion=2022-11-28) and [blob](https://docs.github.com/en/rest/git/blobs?apiVersion=2022-11-28) documentation under the read-docs skill, reusing the already retrieved evidence. Tree base/content/modes, server-selected identity, verification payload/signature and nonforce fast-forward semantics support the proposed design. REST does not document expected-old CAS; requirements correctly distinguish the wrapper contract and append-only single-parent race protection from a proven backend lease/global transaction.
- Exact staged blob UTF8 upload plus root-tree equality, bounded hash-equal canonical commit reconstruction, unreferenced verification/import before branch mutation, checked command exits, one-parent O, one force=false expected-O update, actual-ref readback, and two-ref old-O adoption are appropriate. Unknown encodings/headers/signatures/metadata/payload limits must stop before publication. Preserve the documented read-only ambiguous-outcome reconciliation; no blind commit recreation/ref retry/rollback.

Reviewed scratch SHA256: helper `d0f1ee8cc1233deb9051ac9ee46c90791f2dc80f6e7a22699a87bcdee83f238e`; proof `b28beef7975f5dd3d3e7a2323fc85e135d01853eeeca607cdfcc30ac7f99e86d`; results `9fe2829f00012f055e3a1e57dd0c6e9a055b012b07c3184effa0ed2073246bfa`; manifest `8a241dc4334a551e37822dac463878a5fa026e88c727df401e2382a31d4a87f7`.

This review writes only this owned Markdown artifact in the main repository. It creates no production commit/index/ref/remote write, extracts no credentials and changes no platform settings. API write permission, selected server identity, repository rules, real creation-response compatibility, actual payload limits, terminal auth, hosted CI and Windows/native gates remain unproved. Source implementation verdicts and a later executor publication grant remain separate.
