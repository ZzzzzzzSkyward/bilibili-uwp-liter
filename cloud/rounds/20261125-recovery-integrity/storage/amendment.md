# Storage amendment — FA-01

Status: **ready for independent amendment review; requirements only**. Controlling dispatch: [review-iteration-plan.md](../review-iteration-plan.md). This adds one bounded registry-read requirement to the already accepted S1–S4 [requirements](requirements.md); all original contracts/assertions remain. Evidence: [amendment-evidence.md](amendment-evidence.md). No executor source grant is implied before independent PASS and root's exact ownership window.

## Required behavior

`ReadRegistryUnderGateAsync` must validate the complete deserialized entry collection before returning it to any caller. An entry is structurally unusable if it is null, or if **Token, CollectionId or RootPath is null, empty or whitespace-only**. Token is the reopen/grant identity; CollectionId plus RootPath is the existing replacement identity. No additional identity format, canonicalization, duplicate detection or schema rule is introduced. In particular, do not require GUID-shaped tokens, trim/rewrite strings, require a currently existing folder/grant/asset, or validate display metadata as identity.

Perform this validation inside the existing guarded read's try/catch, after its current null defaults and before it returns. A malformed entry anywhere in the list fails the whole read as `LocalMediaLibraryException` with Stage=`registry-read`, through the existing exception path; no `NullReferenceException`, unusable row, partial list, silently filtered entry or successful import may escape. Use the existing `unknown` collection identity for a failed registry read rather than relying on a malformed row. A valid earlier entry does not excuse malformed later entries: reopen and import receive the same validated snapshot as list.

Preserve these compatibility and transaction rules:

- Missing registry file, deserialized null registry, missing/null `Entries`, and an empty collection retain the existing empty-library defaults. File name, JSON property names/defaults and all public signatures stay unchanged. Title/ItemCount metadata defaults remain readable when required identity is usable.
- A syntactically complete identity with a stale grant, moved folder, malformed manifest or missing media remains a saved row for repair. Listing performs no new FAL or asset lookup. Reopen keeps its existing `access-restore`, `manifest-read`, `asset-resolution` and validation behavior; restoring an asset or reimporting a valid folder still works. A blank public reopen token keeps its existing argument precondition, and an unknown token in a valid registry remains `access-restore`.
- A rejected read leaves committed registry bytes, temporary-file state and every existing grant unchanged. Import must fail before provisional grant/staging/retirement. Do not rewrite, migrate, repair or discard malformed disk data automatically. Valid bytes supplied later by the fixture/user can be read again.
- Keep the current common gate and unconditional release in each caller. A read rejection cannot hold the gate, poison a later request or alter S3/S4 admission/acquisition lifetimes. No new gate, retry loop, cancellation API or persistence framework is needed.

The actual page refresh catches `LocalMediaLibraryException`, so the service repair makes malformed-entry failures reach its existing log/toast path without changing DownloadPage. Existing visible saved rows are retained by that catch. No page ownership transfer or broad exception catch is requested.

## Exact future ownership

After amendment PASS and root's grant, **the original storage executor alone** may change:

| Path | Bounded purpose |
| --- | --- |
| `src/BiliLite.UWP/Services/LocalMedia/LocalMediaLibraryService.cs` | Entry validation in the existing guarded read; a small private validation helper only if useful. |
| `plan/tests/LM02.cs` | Add the acceptance cases below to the existing whole-source executable; preserve every original/S1–S4 assertion. |
| `plan/tests/LocalMediaStorageStubs.cs` | Only a necessary inert primitive observation hook, if existing create/write/replace/remove/count/contains facilities cannot prove no side effects. No substitute acceptance algorithm. |
| Storage execution/evidence artifacts | Exact red/green inputs, logs and source binding under the executor's assigned evidence window. |

No history-helper/playersess, product page, XAML, project, runner/inventory, main documentation, index/ref or unrelated source change belongs to this amendment. Existing stubs already expose enough primitives for the expected implementation; unchanged stubs need no edit.

## Deterministic real-source acceptance

Extend LM02 against the **whole actual service**, using the existing primitive storage/FAL substitutes and exact installed toolchain. Before fixing source, retain the current service bytes/hash and run the final added test bytes against those bytes. Require a real nonzero red before green; the prior green suite does not cover FA-01. Do not weaken or remove assertions to make a red run finish; aggregate new failures if needed so all cases are observed.

1. Seed one valid saved entry and its real fixture grant, then introduce each malformed entry: null, `{}`, and each required field independently missing/null/empty/whitespace while the other two remain usable. Include malformed entries before and after the valid entry, especially a valid-token reopen with a malformed trailing entry. For each fixture, invoke list, reopen with that valid token, and a valid import. Each must produce typed `registry-read`; list must expose no unusable/partial rows. Assert the **exact original bytes** and grant identity/count after each rejected call, plus zero staging/replace/retirement activity. An existing grant alone is insufficient to prove no provisional grant was added and removed: observe primitive access activity if necessary.
2. After each operation kind rejects a read, restore known valid bytes without resetting/replacing the service or its static gate. Bound completion and prove list and valid-token reopen succeed; perform a distinct valid import and prove old/new rows and reachable grants survive. This proves release and later retry rather than merely observing the exception. Await/drain every initiated operation before resetting fixture state; event gates, if used, release in finally.
3. Preserve null-root/missing/null/empty Entries defaults, absent registry file, non-GUID usable tokens and absent display metadata. Keep the existing malformed-JSON typed-read, unknown/stale-token, broken-asset retention/repair, duplicate reimport and all transaction/interleaving checks. Add explicit identity-complete/broken-asset control if needed to distinguish structural validation from asset validation.
4. Re-run the scratch exact `RefreshSavedCollectionsAsync` body, bound to the current page method, against repaired service bytes: refresh Task completes through one typed failure log/toast, retains seeded visible rows, and leaves bytes/grants unchanged. This is source-call composition evidence, not a permanent new runner stage or a rendered-page claim.

Run `UPSTREAM_UNIT_TOOLS=/workspace/work/maintenance-foundation/quality bash plan/tests/run.sh --stage LM02`. Run unchanged LM03 only if the shared stub changes. Selected PASS is partial evidence. Root's integrated owner reruns the full common gate once storage and recovery changes stabilize: current **51 logical/52 executable** inventory stays unchanged, all original checks remain, and separate14 controls/package15 rules remain distinct. Do not rerun unchanged controls without an input change.

Independent fresh_audit reviews this amendment before execution, then closes FA-01 against exact repaired hashes, new red/green predicates and preserved original assertions. This small source repair does not require unavailable native tools. Native UWP compile/rendering, FAL/file replacement/restart semantics and hosted CI remain separate unverified gates; no new native guarantee is claimed.
