# Recovery/FA-01 source publication — VERIFIED

Normal configured Git publication completed exit0 under [the exclusive grant](../source-publication-grant.md). **Actual server dev = local dev = tracking origin/dev = `7fe7a5ff417d0bc2437b5dc3f0507543f744da9b`.** Commit C has exactly one parent O and the exact verified staged tree T:

| Identity | SHA |
| --- | --- |
| C / published dev | `7fe7a5ff417d0bc2437b5dc3f0507543f744da9b` |
| O / sole parent | `e928bf0e818869c03485f8ef6f31fa47e98ac0f0` |
| T / staged and committed root tree | `30b9e7d997e25e7a4b04c3c0dab1d9a0eabbf614` |
| Exact30-path manifest SHA256 | `ccbd59364ea3cd9291a645341e37382b6801f2f0e794c8082a74ef915273b76d` |
| Executed915 source manifest SHA256 | `0a80ac1cf14e76c9274564a999a88522c23460be6a85e077e093d5e28cd55639` |
| Final complete51/52 results SHA256 | `b33d402fdf65518ea47805463f82d097bec9e638694750e6031b698baa1b549d` |

Fresh normal ls-remote/fetch and direct symbolic/ref checks established HEAD→dev and local dev/tracking/actual server all O before staging, with empty index. All30 raw payload hashes matched independent reviewed binding; all22 runnable inputs matched the final executed source. Only the30 explicit manifest argv paths were staged. Cached path-set equality, regular modes, no conflict/deletion/extra, cached whitespace and each canonical staged blob's exact raw bytes/hash passed. Staged root T was captured before the one conventional append-only commit `fix(maintenance): harden download recovery and login lifecycle`. Actual C has only parent O/tree T and exactly those30 changed paths; every committed payload blob matches the frozen raw source/evidence bytes.

```bash
git fetch origin refs/heads/dev:refs/remotes/origin/dev
git add -- <the exact30 individually passed manifest argv paths>
git diff --cached --check -- <the same30 argv paths>
git write-tree
git commit --file /workspace/work/recovery-integrity/source-publication/commit-message.txt
git push origin HEAD:refs/heads/dev
git ls-remote origin refs/heads/dev
```

Every command exit was checked. Push exit0 was followed by an independent normal actual ls-remote read returning C; direct local dev/tracking origin/dev and HEAD matched C. Index empty; all30 payload paths were initially clean before these authorized receipt edits. All22 runnable inputs remain source-clean; all915 current and **committed C blobs** are byte-identical to executed inputs. The92 unrelated metadata paths observed before publication retained their exact bytes/status. No worktree rewrite/stash/reset/rebase/amend/force/credential-setting/API route/workflow dispatch occurred; unrelated root dirt was preserved. This is source publication, not a whole-repository clean claim.

Private immutable evidence: `/workspace/work/recovery-integrity/source-publication/`. `preflight.json`, `staged.json`, `commit.json`, `publication.json` and checked `commands.jsonl` bind actual staged/committed/raw inputs, tree/parent, network exits and refs. Publication receipt JSON SHA256 `f1e20625c6540cf4a29e090bed921da49bcaa7ed95aa7fcff5049221a9faf8d2`; preflight `ca2a225a721d8d9cfd031e41a0f3228632488fdfb7e8722b92a0dd295084b342`; staged `63c0b71e6532d8b04ecd4db90828b9efb7e5c82e86d7214a7313e91b5adeec90`; commit `3e620ba6cc90004a7f02f1425f9a7f2ec23c87f6737f99328d7d6f14c2488416`; command log `51db0d0f6cc0e1edd0c0d9d90ac04f60f44a0cc7cc6907609db90a7f36a13438`. Final release binds these two evidence edits externally, avoiding a self-receipt commit loop.

Final local complete51 logical/52 executable PASS (complete_required=true), recovery348/g5335 and exact reused14 controls/package15 evidence apply to the identical published source bytes. No redundant suite was run for immutable publication or these evidence-only edits. Actual pushed-C hosted CI and Windows/VS/UWP/XAML/native login/cancellation/BackgroundTransfer/file-lock/restart/session/signing/device outcomes remain unverified. Main documentation, cloud archive and final global/ref/clean closure remain separately owned.

Only this receipt and recovery/execution.md's current publication status were changed after verified push. They remain unstaged/frozen for the separately reviewed cloud archive; no additional receipt commit is authorized or created. Sole publisher releases index/ref/source custody.
