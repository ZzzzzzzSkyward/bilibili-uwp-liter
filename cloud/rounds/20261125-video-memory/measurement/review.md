# Independent measurement requirements review

Status: **PASS — RELEASED TO ROOT**, 2026-11-25, after the narrow MR1/MR2 re-review. No open P0/P1/P2 findings. Source baseline remains `7356b94893377558191b0d70576a52bae704c1f9`; the frozen revised document/prototype hashes and independent results are bound in [review-evidence.md](review-evidence.md).

## Previous findings resolved

| Finding | Verified correction | Result |
| --- | --- | --- |
| MR1/R1, P1: failed CLR getter became zero/ok | Both executable forms now share the same reflection-based getter/checkpoint core. Getter exceptions and raw null fail before conversion; the failed field stays null with a field-specific error and partial status. Successful fields, exact >32-bit values and real zeros, including empty-thread count zero, are preserved. Identity failures/replacement/exit still write the fatal row and stop; output failure throws. | **CLOSED**. The same throwing CLR getter still reproduces the original false zero, while the revised cores pass it under default and strict/Stop settings. |
| MR2/R2, P2: standalone rejected required prewarm | Both phase sets contain the eleven published phases, including prewarm. Invalid phases fail before process access/output. The actual standalone interface accepts each valid phase far enough to reach the explicit Linux host rejection without sample output. | **CLOSED**. Twelve standalone phase/host guards passed. |

I independently extracted both cores from the actual revised protocol/prototype, verified exact equality to each other and the released tested cores, and ran **43 synthetic controls, 12 actual standalone phase/host guards and six parser checks**, all PASS. The synthetic process-like dependency never calls a real process API. All 14 revision-release hashes, 15 current-source hashes and 28 unchanged reused artifacts matched; no unchanged source/owning/full-suite rerun was needed.

## Accepted scope and limits

The fifty-cycle fixture, same/distinct/rendering/idle/feed/active/background controls, actual attempted/completed-cycle interpretation, warm-up/settled/idle/repeat analysis, artifact/PID/start-time binding, private-commit versus working-set distinction, managed/native/GPU attribution limits and no-new-production-GC rule remain accepted. WPR syntax and configuration/recording ownership guards retain the previously checked official-documentation basis. Optional Python analysis remains optional; no production census registry, extra aggregate stage or global background-player rewrite is required.

Windows PowerShell 5.1, actual UWP playback/process values, VS managed attribution, WPR/native tracing and GPU validation remain **BLOCKED/UNVERIFIED** on this Linux host. Source-session/fake-resource and deliberate-mutant controls remain bounded regression-seam evidence, not fifty Windows videos, a measured RAM improvement or a repaired product defect. BR-21 and native/device acceptance are not closed by this PASS.

This releases the measurement requirements/protocol review to root. It does not open a source/tooling/publication window: root still applies the all-stream PASS/release gate and grants exact executable ownership separately.
