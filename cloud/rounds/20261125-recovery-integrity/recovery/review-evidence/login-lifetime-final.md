# Independent owning FA-09 final implementation review — PASS

**PASS — FA-09/P2 CLOSED. No open actionable scoped P0/P1/P2.** The owning show attempt now releases its flag on every completion path; a nonowner cannot release another invocation’s held show. RF1/RF2, FA-02–08 and P2-CR1 acceptance from [caller-final.md](caller-final.md) remains valid by unchanged product/source bindings. This is final source closure for the granted recovery scope, not the deferred complete gate, publication, native or hosted certification.

Read the granted [final source dispatch](../../login-lifetime-final-review-dispatch.md), current checkpoint, approved [lifecycle amendment](../login-lifetime-amendment.md)/[independent PASS](../login-lifetime-review.md), tiny execution grant/current [caller-fix receipt](../implementation-evidence/caller-fix.md), actual full helper/diff, final Program additions, complete extractor and unchanged Stubs/project. Previous caller evidence remains historical and was not rewritten.

## Independent exact binding

Read/hash-only verification, without executing extraction, builds, probes or controls:

```bash
python3 /workspace/work/recovery-integrity/recovery-final-review/login-lifetime-final/verify.py
```

Exit0; private `binding-summary.json` SHA256 `069d909db4a935a2092b6e177c53fc65987aee841d9295df7bbdb45e18f5f8af`; verifier `6c42d3fb8bb53450f4b3bd63c11deb15a68b7591be9d0da3764877a91216cd8e`.

- All915 workspace inputs match final manifest **`0a80ac1cf14e76c9274564a999a88522c23460be6a85e077e093d5e28cd55639`**. Only Utils, recovery Program and extractor differ from accepted312 manifest30c64c85…. Stubs/project and every other product remain byte-identical. Rehashed all915 inputs in each baseline, old-source and fixed-source snapshot against its exact expected manifest, plus the retained immutable caller312 snapshot.
- Program/Stubs/project/extractor are identical across old/fixed/workspace. Old/fixed roots differ only in the reviewed Utils product body. Rehashed all28 receipt artifacts and artifact manifest `2a10c7bc694a2a300502c0d9db4b811b5ff9728148baf0e7a01e455ed956bb92`.
- Preserved every complete Check invocation in order: original208 has139 including declaration, frozen312 has182, final348 has191. All original208 acceptance method bodies are identical; prior nonlogin caller acceptance, empty-admission, skipped-candidate and Reset method bodies are also identical. Original312 predicate names/results are an exact ordered subsequence of both new logs and all PASS. The existing login loops/invocations are unchanged; new healthy-owner controls precede the old terminal fault without reordering prior predicates.
- Independently verified each actual complete slice against each emitted wrapper and its file/body receipt:22 inputs/16 unique slices. The old receipt/wrapper is identical to frozen312; fixed differs only by the actual complete helper. All other15 slices, original seven, field, pins and wrapper context are unchanged.
- All5073 installed tool/package hashes match the prior accepted manifest. Runner/control/package inputs and current51 logical/52 executable inventory remain unchanged. No label-only full suite, controls, package or concern rerun occurred.

| Final input | SHA256 |
| --- | --- |
| Utils.cs | `9db1e9c608e60ccd73e041ce3ebf5ba75cc205059714c9a22e35214c4c645952` |
| recovery Program.cs | `0a35d1b69b77415626802fcf2fda1731832ba3cc7f906a57281b9043f0581623` |
| recovery prepare-source.py | `61332f46ffbb893bb437578ba2073a0022b880261dcfc4c4c394e68f1eb19aeb` |
| unchanged Stubs.cs | `c3b05e52ff18ea6eeb31336a1157e99f2e611958700e6c79ebe38237a17f7c99` |
| unchanged RecoveryProbe.csproj | `4aca047cd916873269a82c4d351e9d514c613bfdd6be251659a0cf62f789dec2` |

## Source and seam correctness

The entire Utils delta is exactly the reviewed branch-local try/finally expansion around `await login.ShowAsync()`. LoginDialog construction remains before the claim, dialogShowing=true remains before invocation, the declaration/signature and current-account bool tail are untouched, and every other Utils byte is preserved. Only an invocation entering the original `if (!dialogShowing)` branch runs this finally. A suppressed second/third call retains the existing immediate current-account result, does not wait, construct a dialog or release the owner. There is no new global owner/lock, account short circuit, credential policy or cross-thread serialization claim.

Success and ordinary user cancellation naturally release ownership and keep true/false. A synchronous delivery throw or awaited fault runs finally then propagates the same exception; a cancelled task similarly preserves cancellation/token. The recommendation caller’s existing error boundary still contains failure without issuing an action or changing its initiating row. After settlement a later logged-out action can show again. No caller product changed, so pre-login/post-ack collection/object and current feed-state authority remain exactly the accepted implementation.

The extractor’s only delta is the helper marker’s two **literal complete-body** hashes and its corresponding membership check. Old `8e682965e4aebf1e2cb5781a688fa771f2d5388d67ff4dd790398f2aae6a1a68` and fixed `760b21cc33e11fe2471ab787e383f701e81bb68de6f211256145937664c7ddb1` independently match the inspected actual bodies. Normalizing precisely that allowance back to the old literal/check reproduces the entire prior extractor byte-for-byte. There is no arbitrary/default/environment bypass, computed self-approval, fallback or stored implementation. Both wrappers uniquely contain their actual complete helper and retain actual full Utils file/body hashes. The private field digest stays `6fe051d489bef42079775defed73f7dad1a421d1af4dddb509ff67e7d1b3917a`.

## Genuine same-fixture semantic result and natural teardown

Reused the source owner’s exact accepted runs after independent binding. Both compile successfully; no pin rejection or setup failure is counted as red. Raw old log has348 checks/21 failures; fixed348/348 PASS. Added36 comprise21 old semantic failures and15 passing controls; all prior312 pass on both. The three decisive immediate retry failures are FA-09 itself; the18 downstream unavailability assertions are openly disclosed consequences of the same poisoned old helper, not18 new findings.

The existing terminal fault is immediately followed by `LoginRetryAction(vm, rows, row, ...)` on that same VM, collection and object. There is no intervening Reset/CallerDefaults, count reset, replacement, reflection or product-state write. Its available successful primitive delivery and held real action acknowledgement produce old `shows=0;requests=0;account=False;row_retained=True` versus fixed `shows=1;requests=1;account=True;row_retained=False`; acknowledgement is settled in both. The fixed request retains the actual feedback_id91 builder and only the initiating row is removed after acknowledgement.

Healthy held-success and normal-cancel schedules first establish that the owner stays pending, second and third nonowners return false immediately with no extra native show, and the natural owner result is preserved. Direct helper cases then cover synchronous throw, held asynchronous fault with original exception identity, and held cancellation with the known token. Both held exceptional schedules repeat the nonowner suppression before settlement. Every outcome is followed by a real later logged-out recommendation action with native show/request/held-ack/row observations; all six fixed retry traces show one show, one request, account true and only the initiating row removed.

Each held owner is awaited; allocated native gates are completed/faulted/cancelled and explicitly awaited even if old poison prevented admission. Each acknowledgement gate is completed and its action awaited. Existing synchronization-context/singleton observer teardown remains PASS, and the final tracked-delivery check passes. Old poisoned state is disclosed by DOWNSTREAM_UNAVAILABLE output; it is never reset to fabricate green. Fixed later-action observations demonstrate natural release and reuse after every outcome. Only primitive delivery/account inputs are restored. The unchanged Stubs provide native timing/outcomes/counts; actual helper and full caller own lifecycle/auth/request/row decisions.

Controlling private artifacts under `/workspace/work/recovery-integrity/login-lifetime-fix/`:

| Artifact | SHA256 |
| --- | --- |
| old-final.log | `207ea53c274a2b60d4505231c2e6b34d8e0bfad359e00ff53476ea4cd05591c4` |
| targeted-final/logs/batch_recovery.log | `d6978cd5908757e5d7fd3787be89fe3ae41d1f9e65d1d5cb1533d533566cf36f` |
| targeted-final/logs/batch_g5.log | `770c3b67a7c23c981ad1417e1c2196f20a337355d2b86196bdc97ca84ca07ace` |
| targeted-final/results.json | `bf8469d0d58a6e7937f3ba713ef7bd341ddd3cae4039e359834e7c84c5d8bc17` |
| old exact wrapper | `2ca7a42f4aed3a26fc0a482deb9284d0f0d1cbcdecf90d0f6a658df1a7be6118` |
| fixed exact wrapper | `e18d89b0d51f054fe9350e32996bac7693830017a27b31ca9d34888141c1fe21` |

## Structure and remaining gates

The repair is coherent and minimal: lifecycle stays in the existing helper, additive schedules stay in the same recovery program, and only the owned source-pin allowance expands. No orphan/duplicate production helper, schema/stage/project expansion or net8-only product API was introduced. Branch-local try/finally around await is compatible with the existing UWP source language/framework assumptions. Test-only cancellation/task inspection APIs do not enter product code. Existing warning causes remain CS1998/CS0168/CS0162/CS0067 without suppression.

Current evidence is selected **PARTIAL_PASS**, recovery348 andg5335,2/2 stages PASS,50 NOT_SELECTED, complete_required=false. Immutable source provenance correctly remains null HEAD/dirty/verified=false; real dev/HEADe928bf0 is dirty/local/unpublished and cached index is empty. Root may now proceed through both source closures to the separately granted complete changed-source51/52 and later reviewed exact30-path publication/docs/ref/clean-tree work. Unchanged14 controls/package15 remain separate reusable bindings. Windows/VS/UWP/XAML, native login/cancellation/BackgroundTransfer/file locks/restart/session/media/device/signing and actual pushed-SHA hosted CI remain unverified. No source/test/main-doc/index/object/ref/API mutation was made by this reviewer.
