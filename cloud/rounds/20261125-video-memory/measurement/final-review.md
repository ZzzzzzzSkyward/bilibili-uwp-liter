# External measurement tooling implementation review

Status: **PASS — RELEASED TO ROOT**, 2026-11-25, after the narrow T1–T3 re-review. No open P0/P1/P2 findings in this component. Final tool SHA-256 `668b5c06ff519d6f9500a9e087889e5504099cd84c0fb0fcf6bade4f2011474f`; source HEAD remains `7356b94893377558191b0d70576a52bae704c1f9`. [Independent evidence](implementation-review-evidence.md) binds the frozen files, original/final same-fixture results and proof limits.

## Findings resolved

| Finding | Independently verified correction | Status |
| --- | --- | --- |
| T1, P1: final idle checkpoints sorted before settled50 | Idle60/idle300 now rank 1021/1022, after all seven permitted cycle-50 ordinary/control ranks 1001–1007. The complete actual capture-function journey records 16 ordered checkpoints. Duplicate/backward rejection remains, including an isolated idle300→idle60 sequence without an earlier idle60. | **CLOSED** |
| T2, P2: required scalar strings accepted JSON arrays | Raw `[object]` scalar-string checks precede comparisons/membership/coercion in metadata, stored process/checkpoint/event rows, run bindings and terminal records. Single-element arrays are rejected with prior bytes unchanged, no append and no new process read where applicable. Valid scalars, supported arrays, explicit unavailable objects, nullable observations and true zero/empty-thread values remain supported. | **CLOSED** |
| T3, P2: marker helper count overstated | Current execution/support receipts report the measured **55 public calls / 52 accepted synthetic rows**. The marker helper is byte-identical to the prior release; no dummy call was added. Counts are derived by forwarding actual public invocations. | **CLOSED** |

I independently ran the same frozen **106-check** fixture against original and corrected tools with the same metadata input: **38 failures / exit 1** on original `6b1b67a7…`, **zero failures / exit 0** on final `668b5c06…`, with identical check names/order and 14→16 journey rows. My original counterexamples also now pass their corrected expectations.

The revised isolated tooling entry independently passes **46 outer invocations**, all original **21 controlled groups**, **106 revision checks**, and the actual **55/52** marker count. All four PowerShell files parse on Core 7.7. All eight released files, nine review-start inputs and 1003 bound raw artifacts matched their recorded hashes. Original getter/UTC/observer/writer/lock helpers and public entry are byte-identical; the tested ordering/type validators and terminal validation contain the narrow corrections.

## Accepted behavior and remaining gates

The delivered four-mode external tool and scoped tests have coherent file/API responsibilities. Current integer-byte/CPU values, private commit versus working set, artifact/tool/metadata/PID/start/run binding, MR1/MR2 getter/null/true-zero behavior, explicit native-host rejection, observer disposal, lock ownership, partial/output/fatal-identity preservation, marker ordering/completeness and unknown-metric rejection remain accepted within the tested dependencies. No production registry/GC/cache purge/target launch or kill, synthetic public collection bypass, optional summary or aggregate inventory change was added.

This PASS covers the external tooling implementation. **Windows PowerShell 5.1 execution, harmless actual Windows PID sampling, UWP playback, fifty-video E2 trends, managed/native/GPU attribution, build/device acceptance and BR-21 measurement remain BLOCKED/UNVERIFIED.** Synthetic CLR values/operator markers do not establish native bytes or playback observations. Whole-tree source integration and publication remain root’s separate gates; this review grants neither itself.
