# Connected Git Data publication — requirements ready for independent review

Status: **REVISED REQUIREMENTS / 158 LOCAL CHECKS PASS / READY FOR RE-REVIEW; NOT A PUBLICATION GRANT.** [Review repair evidence](review-repair-evidence.md) records genuine red→green for P1-TR1/P1-TR2 and the P2-TR1 prose correction. Only the assigned source executor may execute this protocol after independent `transport_review` PASS and an explicit revised owned publication window. Root never stages, commits or publishes on that executor's behalf. Existing recovery `commit-window.md` authorizes only normal git transport and therefore does not itself activate this fallback.

## Scope and supported boundary

Normal terminal transport currently fails credential read/HTTP401; connected GitHub GET succeeds. Preserve all current product/test/root work. This requirements/proof task performs no source/index/ref/configuration/credential or remote writes. All local object/ref mutation tests use a separate scratch repository. Network reads do not establish write permission.

Callable connected tools expose `create_tree(base_tree_sha, tree_elements)`, `create_blob(content, encoding)`, `create_commit(message, tree_sha, parent_sha, additional_parent_shas)` and `update_ref(branch_name, sha, expected_sha, force)`. `create_commit` exposes **no author/date/signature controls**: do not create a local commit first or predict its SHA. Use the server-created commit, reconstruct its exact canonical bytes, verify its object hash, import it locally while unreferenced, and only then publish its branch ref.

Official Git Data docs support nested tree entries with **either** UTF8 `content` **or** blob `sha`; use inline staged text here, avoiding unnecessary per-file blob writes. The returned root tree SHA must equal local `git write-tree`. If payload/response limits prevent inline transfer, stop before publication and record the missing capability; do not switch to Contents API or per-file commits. No branch/test commit/CI dispatch is permitted as a transport probe.

Helpers are local-only at `/workspace/work/recovery-integrity/publication-transport/transport.py`; they have no network code, stage no paths, create no commits and publish no remote refs. Their `import` command writes verified Git objects; `adopt` uses an index lock and a two-ref CAS transaction. Those production commands are executor-only, after a revised grant. Helpers and scratch data are not product changes or source commit inputs.

## Fixed ownership and preconditions

Recovery manifest: `/workspace/work/recovery-integrity/publication-transport/recovery-manifest.json`, **22 exact current source/test/execution/evidence paths**, SHA256 `8a241dc4334a551e37822dac463878a5fa026e88c727df401e2382a31d4a87f7`. It enumerates **11 product/project paths + five recovery probe files + two aggregate/control files + execution.md + three implementation-evidence files = 22**. It excludes root orchestration, requirements/reviews, every commit-window file, main documentation and other owners' paths. `DownloadDialog` has no change and is absent. Read the manifest, not a glob. Any required ownership or exact path change requires an updated owned manifest and independent review before staging; do not auto-expand it from status.

1. Source executor exclusively owns the shared index/publication window. Other owners may retain unrelated unstaged edits; nobody may stage or move local refs during the window. Freeze owned source and evidence first, bind actual tests to staged canonical bytes, and finish required source gates/review evidence. No post-preparation owned edits or contradictory prepublication success statements.
2. Start from **HEAD pointing directly and symbolically to refs/heads/dev, with dev and origin/dev themselves direct nonsymbolic refs**, both O. Reject aliases even if their resolved SHAs equal O; no permitted ref may redirect adoption into master or any other target. A fresh connected GET `git/ref/heads/dev` must return exact `ref`, `object.type=commit`, SHA O. At proof time O is `e928bf0e818869c03485f8ef6f31fa47e98ac0f0`; always re-read at execution. Confirm repository identity and old commit/tree exist locally. Do not fetch/reset/stash/rebase to force equality.
3. Executor alone stages **exact explicit argv paths** from the reviewed manifest, checks cached path-set equality and `git diff --cached --check`. Helpers reject additional/missing staged paths, deletions, conflicts, nonregular modes, split indexes, non-SHA1 repositories, unsupported ambient Git redirections, invalid UTF8, binary/control bytes, unsafe paths and individual blobs over 1MiB. Modes `100644` and `100755` are preserved. Unknown/binary paths require a separately reviewed protocol; never silently omit them.
4. Only exact staged blob objects are upload sources (`git cat-file blob <index-SHA>`). Working-tree reads, re-encoding, newline normalization and clean/smudge filters must not supply upload content. A copied index and scratch object directory run `write-tree`, preserving the real index and object database during prepare/verify. Capture the raw real-index fingerprint, owner-manifest fingerprint, base tree, canonical staged root tree T, message and each blob/mode/size/hash. **Separately capture every manifest working path's raw bytes SHA256, byte count, regular-file mode/device/inode/link count and nanosecond mtime/ctime.** Read through no-follow directory/file descriptors, reject symlink ancestors/endpoints, missing/replaced/nonregular files and avoid blocking on a FIFO. Working files over 2MiB fail conservatively. Compare raw working bytes against their own captured baseline, preserving intentional CRLF-working/LF-staged differences; unrelated paths are outside this comparison.
5. Preparation format is now **version 2**; earlier preparations without raw-file receipts fail closed. `verify`, `import`, `check`, `emit` and `adopt` share manifest/index/direct-ref/staged-tree/**raw owned-file** validation. `prepare` checks raw files both before and after reading staged inputs. Adoption checks raw files/direct refs again immediately before its no-dereference CAS and verifies them afterward. Reject any changed index, manifest, local ref, owned raw-file state or staged root without repairing, staging or rewriting it. Before publication, drift requires a fresh reviewed source/test binding and new preparation directory. After remote publication, drift is an explicit adoption blocker, never a reason to overwrite a working file or move a protected ref. Actual tested-input-to-staged-byte equivalence remains the executor's explicit gate; a stable raw fingerprint does not establish it by itself.

## Required sequential procedure

Use a new scratch output directory, an exact UTF8 message file with no final newline, and sanitized JSON API receipts. `O` is the freshly checked old SHA. Every API invocation must check `isError`, decode its actual API payload, validate required fields/types/values, and stop on absent/ambiguous/truncated results. `Action completed` alone is insufficient. Never infer successful mutation from a tool name or a pending response.

```bash
python3 /workspace/work/recovery-integrity/publication-transport/transport.py prepare \
  --repo /workspace/bilibili-uwp-liter --out /absolute/new/scratch/prepared \
  --old "$O" --manifest /workspace/work/recovery-integrity/publication-transport/recovery-manifest.json \
  --message-file /absolute/scratch/message.txt --remote-ref /absolute/scratch/old-ref.json
```

1. **Prepare.** Validate source ownership/staged test bindings, local/tracking/remote old SHA and the prepared manifest/T/blob receipts. Freeze helper and preparation hashes in the executor's evidence. `create-tree.json` contains only the expected repository, base old tree and changed text blob paths/modes/content. No upload has occurred yet.
2. **Create an unreferenced tree.** Executor invokes connected `create_tree` using that exact structured JSON argument. Validate returned SHA=T and `truncated=false`; GET immutable `git/trees/T` if the creation result lacks full required metadata. A matching canonical root hash proves all paths, unchanged base entries, blob bytes and modes, not merely file names. A differing SHA rejects; no ref changes.
3. **Create one unreferenced commit.** Connected `create_commit` uses the prepared message, tree T, `parent_sha=O`, and `additional_parent_shas=[]`. Validate creation result and GET immutable `git/commits/C`. Require SHA C, exact tree T, exactly one ordered parent O and exact message. An API error or ambiguous response stops; inspect immutable receipts before deciding whether any object was created. Never make a replacement commit solely to obtain a convenient SHA.
4. **Reconstruct and verify before branch publication.** Fresh GET old ref remains O; save actual tree and Git Data commit metadata locally. Run:

```bash
python3 /workspace/work/recovery-integrity/publication-transport/transport.py verify \
  --prepared /absolute/scratch/prepared --remote-ref /absolute/scratch/old-ref.json \
  --remote-tree /absolute/scratch/tree.json --remote-commit /absolute/scratch/commit.json
python3 /workspace/work/recovery-integrity/publication-transport/transport.py import \
  --prepared /absolute/scratch/prepared --remote-ref /absolute/scratch/old-ref.json
```

The verifier reconstructs the canonical `commit <byte-count>\0<body>` SHA1, writes the exact commit only into its scratch object database, and requires equality to C. Import then packs precisely C's objects not reachable from O into the real local object database, without moving refs or changing the index/worktree. It verifies exact raw commit bytes, T and O ancestry locally. Missing objects/metadata, hash mismatch, unsupported headers/signature/serialization or any import failure **abort before remote publication**. Unreferenced objects can remain harmlessly; do not delete/rewrite user objects.

5. **Final read and publication gate.** GET the live old ref again, then run helper `check --prepared ... --remote-ref ...`. Require `READY_FOR_OWNER_REMOTE_UPDATE` with O,C after all local-ref/index/tree/import checks. The source executor then invokes exactly:

```json
{"repository_full_name":"ZzzzzzzSkyward/bilibili-uwp-liter","branch_name":"dev","sha":"C","expected_sha":"O","force":false}
```

There is no force=true, create_ref, merge, branch creation, terminal credential bypass or alternate API write. Check the actual `update_ref` result and independently GET live `git/ref/heads/dev`, requiring exact ref/type/SHA C. A timeout, ambiguous response, rejected expected SHA or different actual remote SHA requires observation/recovery below; do not retry blindly.

6. **Adopt the same commit locally.** Only after the independently observed actual remote is C:

```bash
python3 /workspace/work/recovery-integrity/publication-transport/transport.py adopt \
  --prepared /absolute/scratch/prepared --remote-ref /absolute/scratch/published-ref.json
```

The helper obtains `.git/index.lock` exclusively, revalidates the original index fingerprint/T, imported raw commit/parent, raw owned working fingerprints and **direct** local O refs, then uses one `git update-ref --no-deref --stdin` transaction with explicit old O to advance `refs/heads/dev` and `refs/remotes/origin/dev` to C together. The no-dereference option prevents a last-moment symbolic alias from redirecting an allowed update into master or another target. It writes neither index nor worktree, runs no checkout/reset/rebase and cannot discard unrelated edits. Post-checks require HEAD → dev directly, direct dev/tracking=C, byte-identical index, unchanged raw owned files and empty staged diff. Keep unrelated dirty files. Record exact actual remote observation and helper outcomes; this is API-observed tracking adoption, **not a successful git fetch/push**. `FETCH_HEAD` and terminal transport remain unchanged/unverified.

Keep complete source/test/evidence stable before prepare. Root can independently record final SHA/remote observations in its own artifacts. If the source owner needs a final committed receipt, use at most the already allowed separately frozen evidence-only manifest under an explicit window; repeat the reviewed protocol from the new exact O, and avoid a postcommit receipt loop.

## Exact commit reconstruction support

Git Data `author.date`/`committer.date` normalize unsigned original timezone offsets, and API messages may omit an original terminal LF. The helper tries a bounded set of quarter-hour offsets through ±14h and exact message/no-final-LF or one-final-LF encodings. It accepts **only canonical hash equality to C**, not a plausible author/date. The default server-created UTC representation is included. Other offsets/encoding/extra headers safely fail before a ref write. This is a lossless verified reconstruction when accepted, not a promise to reconstruct every historical Git object.

For signed objects, GET `verification.payload` preserves original signed header/message bytes and `verification.signature` supplies signature text. The helper supports ordinary LF PGP `gpgsig` insertion after the supported tree/parent/author/committer headers, retaining the exact continuation-line/trailing-newline form that matches C. It rejects missing payload/signature halves, unknown/extra header order, SSH/non-PGP signatures, malformed metadata or unmatched hashes. Existing signed merge metadata is tested for reconstruction; production verification independently insists on exactly one parent. No private key, credentials, signature fabrication or auth-file reads are involved. Signature *validity* is not re-certified by hash verification; exact server object identity is.

## Atomicity, limits and recovery

The remote branch publication point is one normal ref update. Tool metadata promises mismatched `expected_sha` rejection; official REST PATCH has SHA/force fields and does **not** document a server-side old-SHA CAS parameter. Do not claim an independently proven server-side lease implementation or a globally atomic remote/local transaction. With C's sole parent O and `force=false`, a competing append-only head R distinct from O/C cannot be an ancestor of C, so GitHub's fast-forward rule rejects overwriting it, including a race after the GET. These safety claims assume the repository's mandated append-only branch model. Concurrent force/rewind/delete operations are outside that model; no such operation is authorized here. If review requires stronger atomic expected-old semantics than the tool contract plus this graph restriction, request a documented capability in this artifact and leave publication blocked.

| Observed point | Required response |
| --- | --- |
| Before any branch update; raw owned file/type/identity, symbolic ref, content/ref/index/hash/auth/permission/tool limit fails | Preserve source/index/local refs and all receipts; mark blocked. Unreferenced remote/local objects are not publication. No cleanup ref write or fallback Contents API. |
| `update_ref` rejected/timeout; live remote remains O | Not published. Preserve C/T and preparation; inspect rejection. Retry only with a fresh exact unchanged preflight and explicit owner window; never recreate C or force. |
| Ambiguous update response; live remote is C | Published. Do not repeat remote mutation. Verify C/T/raw object/index and adopt C locally. |
| Live remote is some R other than O/C | Stop; read actual graph and owner changes. No force/reset/rebase/local tracking substitution or automatic new parent. New reconciliation requires a reviewed owner dispatch. |
| Remote C but local adoption fails (lock/index/ref drift) | Report **remote published / local adoption blocked**, not synchronized. Preserve index/worktree; inspect read-only, recover using the same verified C only after original equivalence/CAS preconditions are restored or explicitly reviewed. Never roll remote back. |
| Local adoption succeeded; remote moves afterward | Report the observed publication equality and later divergence truthfully; new remote work is not overwritten. No fabricated persistent synchronization guarantee. |

Actual write permission, branch protections/rulesets, account identity selected by the server, payload-size limits and CI behavior are **unproved** until the authorized operation returns evidence. Authentication is the existing connected service; terminal credentials are neither read nor repaired. A refusal is an external transport/permission blocker, not a reason to weaken source gates or change settings. Actual pushed-SHA hosted jobs still need their normal independent observation; this protocol adds no artificial dispatch.

Acceptance: independent review PASS; all scratch proofs PASS; exact reviewed manifest/staged test/tree/hash checks; one-parent server object imported before remote branch update; explicit expected SHA and `force=false`; actual remote C; same local/tracking C by transactional CAS; unchanged index/unrelated working bytes; truthful receipts and original native/CI/auth limits. No production use is authorized by this document alone.
