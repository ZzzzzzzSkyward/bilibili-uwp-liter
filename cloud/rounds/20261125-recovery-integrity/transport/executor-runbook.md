# Executor integration notes — inactive until review and grant

Read [requirements.md](requirements.md) in full. This file explains local-to-tool data movement; it grants no production execution and contains no automatic publication routine. Review helper hashes, source manifest and every receipt before dependent work. The executor owns staging, tool mutations and final local adoption; root remains read-only for source publication.

Scratch helper modes:

| Command | Effect |
| --- | --- |
| `prepare` | Requires direct dev/tracking refs and HEAD → dev; captures raw owned regular-file fingerprints separately from exact staged blobs, then writes version2 frozen upload JSON/receipts, copied index and tree objects to new external scratch directory. |
| `emit --prepared DIR --offset N --limit 3000` | Revalidates local direct refs/index/tree/manifest/raw owned files, then emits one JSON-escaped Unicode chunk after checking its payload hash. Intended only for internal exec-to-tool transport, without printing source in chat. |
| `reconstruct --commit-json JSON --out RAW` | Pure metadata reconstruction, accepting only canonical SHA equality. Used by immutable historical proof. |
| `verify` | Rechecks direct refs/index/tree/manifest/raw owned-file state and remote receipts; verifies tree/one parent/message and reconstructs server-created commit in scratch. |
| `import` | Rechecks the same working/staged/ref boundary, then imports that verified commit and missing reachable objects without moving any ref/index/worktree. |
| `check` | Fresh old-ref receipt plus complete direct-ref/index/manifest/tree/raw owned-file checks, requiring the exact object already imported. |
| `adopt` | **After actual remote=C only:** locks original index, rechecks exact state and direct refs immediately before `update-ref --no-deref --stdin`, atomically CAS-advances only local dev and origin/dev together, then validates direct refs/raw owned files/index again. No staging or worktree rewrite. |

Stage only from the reviewed fixed list using a subprocess argv array, then read the exact cached names/check. Do not paste a glob or use `git add -A`. The helper rejects cached set inequality rather than silently correcting the index. The message file is exact UTF8 without a terminal LF; the API chooses actual commit author/date and raw serialization, then verification checks the returned exact object.

Version1 preparations are rejected. Version2 binds each manifest working file's own raw hash/bytes and regular-file identity/mode/times independently from its staged blob. CRLF working bytes with LF staged bytes remain supported; edits, replacement (including identical bytes in a new inode), deletion, symlink/directory/FIFO or mode drift reject. Unrelated unstaged files remain allowed. A drift failure requires preserving the files/index/refs and returning for a fresh owned source/test binding; no helper restages or restores them. Actual test-input-to-staged-byte equality is still a separate executor acceptance gate.

The prepared recovery payload is larger than a typical tool stdout budget. Assemble it privately from bounded `emit` chunks. Example integration *after* review/grant, with reviewed constants substituted:

```javascript
// Inside functions.exec; do not text()/notify() chunks or the full source payload.
const prepared = "/absolute/scratch/prepared"; // fixed, reviewed path without shell metacharacters
const expectedPayloadHash = "<prepared payload_sha256>";
const expectedTree = "<prepared tree T>";
let offset = 0, total, joined = "";
while (true) {
  const r = await tools.exec_command({
    cmd: "python3 /workspace/work/recovery-integrity/publication-transport/transport.py emit --prepared " +
         prepared + " --offset " + offset + " --limit 3000",
    max_output_tokens: 8000
  });
  if (r.exit_code !== 0 || r.session_id || !r.output) throw Error("payload read failed");
  const part = JSON.parse(r.output); // truncated/invalid JSON rejects before an API mutation
  if (part.offset !== offset || part.sha256 !== expectedPayloadHash ||
      typeof part.chunk !== "string" || !Number.isSafeInteger(part.total) ||
      part.next_offset !== offset + Array.from(part.chunk).length ||
      (total !== undefined && total !== part.total) || part.next_offset > part.total ||
      (part.next_offset <= offset && offset < part.total)) throw Error("payload chunk mismatch");
  total = part.total;
  joined += part.chunk;
  offset = part.next_offset; // Unicode code-point offset, not JS UTF16 string.length
  if (offset === total) break;
}
const argument = JSON.parse(joined);
if (argument.repository_full_name !== "ZzzzzzzSkyward/bilibili-uwp-liter") throw Error("wrong repo");
// Review prepared receipts/path set first. Only the source executor invokes this mutation.
const result = await tools.mcp__codex_apps__github_create_tree(argument);
if (result.isError) throw Error("tree creation failed");
const sc = result.structuredContent;
const tree = typeof sc?.content === "string" ? JSON.parse(sc.content) : sc;
if (!tree || tree.sha !== expectedTree || tree.truncated !== false) {
  throw Error("tree response unsupported or noncanonical; stop with unreferenced objects only");
}
// Save the actual validated API tree object privately as tree.json; print only SHA/count/status.
text({status: "UNREFERENCED_TREE_VERIFIED", sha: tree.sha});
```

The example decoder matches the actual GET result shape observed in proof. If a creation response uses another shape, stop and inspect sanitized metadata; do not invent a SHA or treat `Action completed` as proof. An immutable GET by a validated returned SHA may supply missing response fields. Save full tool result privately and its decoded API object as structured JSON; the Python verifier consumes the actual decoded Git Data object, never a handwritten approximation. Quoted heredocs or a structured file-write helper must preserve literal source text and avoid shell expansion.

Next operations stay separate and sequential: `create_commit` with exactly prepared message/T/O and no additional parents; immutable GET/verify/import; fresh live old-ref GET and local `check`; **one** explicit `update_ref` call with expected_sha O/force false; independent live new-ref GET; local `adopt`. Do not combine branch publication into a helper/loop or continue after a failed assertion. Every API/write/ref outcome is checked before the dependent step. Persist concise receipts with source/test binding, O/T/C, manifest/preparation/helper hashes, API-observed ref and local-adoption result; no full source payload or credential in chat.

If `update_ref` times out after submission, the next action is a live GET. Remote C means proceed to verify/adopt that same C; remote O means no publication established; any third head means stop. A duplicate source commit or artificial CI dispatch is never a recovery step.
