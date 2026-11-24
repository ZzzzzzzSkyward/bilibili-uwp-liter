# Transport review repair — ready for independent re-review

Status: **P1-TR1 / P1-TR2 / P2-TR1 REPAIRS IMPLEMENTED IN SCRATCH; 158 CHECKS PASS; NO PRODUCTION PUBLICATION.** Original [review.md](review.md) remains the immutable CHANGES verdict until its independent owner re-reviews. Ownership/prohibitions come from root's `review-iteration-plan.md` transport section. No main-repository source/index/ref/remote/settings/history writes were performed; only owned transport Markdown and separate scratch helpers/fixtures changed.

## Genuine red before repair

Frozen original files: `/workspace/work/recovery-integrity/publication-transport/review-baseline/transport.py` SHA256 `d0f1ee8cc1233deb9051ac9ee46c90791f2dc80f6e7a22699a87bcdee83f238e`; original `proof.py` SHA256 `b28beef7975f5dd3d3e7a2323fc85e135d01853eeeca607cdfcc30ac7f99e86d`. The original35 assertions and fixture controls remain in the current combined proof.

New functional cases were run against that original helper **before helper mutation**:

```bash
python3 /workspace/work/recovery-integrity/publication-transport/review_regressions.py \
  --helper /workspace/work/recovery-integrity/publication-transport/review-baseline/transport.py \
  --output /workspace/work/recovery-integrity/publication-transport/review-red-results.json
```

Exit1, **123 checks: 46 failures / 77 preserved controls**, bound to the original helper SHA above. Failures reproduce tracking-alias acceptance at prepare/verify/import/check/emit/adopt; owned source/test/evidence drift acceptance; missing/symlink/directory/FIFO/new-inode/mode replacement acceptance; and both dev/tracking aliases injected precisely at adoption's CAS moving master. Rejection-preservation assertions also fail where baseline adoption incorrectly advances refs. Dev aliases already rejected by several baseline paths and direct-ref/filtered-working controls are retained rather than relabeled red.

## P1-TR1 repair

Every shared ref check requires `symbolic-ref --no-recurse HEAD == refs/heads/dev`, rejects any symbolic dev/tracking ref using checked `symbolic-ref --quiet --no-recurse`, then independently requires old-SHA equality. All dependent operations, including payload emission, use that common check. Adoption revalidates immediately before mutation and uses **`git update-ref --no-deref --stdin`**, so an intervening alias cannot redirect an allowed destination into another branch. Both old-O ref updates remain one CAS transaction. Post-adoption direct refs and HEAD are checked again.

Scratch cases cover aliases at prepare and each dependent phase for **both** local refs, byte/target preservation of unrelated master/unrelated refs on rejection, unchanged index/worktree, lock cleanup, direct-ref success, and last-moment alias injection at the actual `update-ref --stdin` boundary. In that race the repaired command may safely reject or update only its explicitly named destination; the tests insist master and every unrelated ref/index/file remain unchanged. No production alias or ref was created.

## P1-TR2 repair

Preparation schema **version2** captures every fixed-manifest working file independently of the staged upload blob: raw SHA256/byte count, regular-file mode/device/inode/link count and nanosecond mtime/ctime. File reads traverse no-follow directory descriptors and use no-follow/nonblocking final opens; no symlink target is read, and FIFOs reject without hanging. Descriptor and named-path identities are checked before/after reading. Preparation checks all raw receipts twice; common state validation rechecks them for verify/import/check/emit/adopt. Adoption checks again immediately before its CAS and afterward. Version1 preparations cannot bypass the new gate.

Raw working files are compared against their own baseline, **not** staged normalized bytes. Tests preserve the original CRLF-working/LF-staged success and unrelated dirty text. Source, test and execution-evidence files each have separate post-prepare drift cases through verify/import/check/emit/adopt, with rejection and exact no-ref/index/worktree-change checks. Missing files, symlink/directory/FIFO replacement, same-byte replacement while retaining the original inode elsewhere, and executable-mode drift reject at check and adopt. No repair/staging/restoration is performed. The executor's actual tested-input-to-staged-byte binding remains mandatory; these raw fingerprints establish stability only.

## P2-TR1 and final verification

Manifest prose is corrected to **11 product/project + five recovery probe + two aggregate/control + one execution + three evidence =22**. Actual fixed paths/hash are unchanged.

```bash
python3 /workspace/work/recovery-integrity/publication-transport/review_regressions.py
python3 /workspace/work/recovery-integrity/publication-transport/proof.py
```

Standalone review suite: exit0, **123/123 PASS**. Combined proof: exit0, **158 PASS = original35 + review123**. No original assertion was removed/weakened. Reconstructing/importing both existing immutable remote unsigned/PGP objects remains part of the original35 proof; no additional network request or remote mutation was needed for these repairs. All staging/object/ref/race fixtures remain under separate temporary scratch repositories and are deleted by test teardown.

| Artifact under `/workspace/work/recovery-integrity/publication-transport/` | SHA256 |
| --- | --- |
| `transport.py` | `f72a4873ff232296c9826877e6d261eaff6dd0d16683f732465b974f22d8e5b0` |
| `proof.py` | `8cef00bcdcd9d57d861f75d514985bef6ad96546719cd00ce327808ef7e51229` |
| `review_regressions.py` | `c491186102bd26989b3b2c6d354833b515d88e1a57f55bad775faa25f5be7f6b` |
| `review-red-results.json` | `6744919bfb60dcd418dc70b8b6f935fd313b08c0f44e10ee9e2b1820e1ec28f7` |
| `review-regression-results.json` | `637b8bc604097c992dec2506d0c015607dca454ea16cfaf74d32e51d831a1fa7` |
| `proof-results.json` | `78fd50a7f1e90523868d2aca505185ff008d776376d1f39ac388bf5a9f63808c` |
| `recovery-manifest.json` | `8a241dc4334a551e37822dac463878a5fa026e88c727df401e2382a31d4a87f7` |

Remaining publication limits are unchanged: independent transport PASS plus root's explicit exact final source window **after source acceptance**, actual connected write permission/response/identity/rules/size evidence, real server-created object verification/import, checked expected-O/force=false update and actual readback/local adoption. Terminal auth, CI and native gates remain separate. Worktree edits are not globally locked by Git; source owners must honor the exclusive frozen-source window, and the helper checks the captured state at each dependent boundary. Drift after remote publication is reported as adoption blocked, never repaired by destructive writes or a ref rollback.
