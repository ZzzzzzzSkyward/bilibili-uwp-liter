# External contract controls at final source

Implementation HEAD: `f1963e0ed59816188dac487ed30ff8ddddbc7e64`. Command:

```bash
UPSTREAM_UNIT_TOOLS=/workspace/work/maintenance-foundation/quality python3 plan/tests/verify-suite-runner.py --output /workspace/work/maintenance-foundation/runner-execution/contract-final-sha
```

Exit: **0**; **11/11 controls PASS**, separately labeled from the legacy totals. Report tree contains only bounded diagnostics, JSON/Markdown metadata and SHA-256 input identities; temporary source/fixture/cache trees stayed outside it and fixtures were removed.

| Control | Status | Seconds |
| --- | --- | --- |
| failure-propagation | PASS | 0.245 |
| missing-required-stage | PASS | 0.23 |
| stage-timeout-cleanup | PASS | 6.891 |
| active-suite-deadline | PASS | 7.755 |
| sigint-finalization | PASS | 5.874 |
| sigterm-finalization | PASS | 5.901 |
| bounded-log | PASS | 0.38 |
| interfaces-and-preflight | PASS | 0.614 |
| inventory-drift | PASS | 0.207 |
| completion-marker-protocol | PASS | 0.184 |
| real-selector-independence | PASS | 4.055 |

Failure control: exit 7/FAIL then later PASS; missing control: MISSING then later PASS. Every report retains 50 logical/51 executable baseline/current and selected 2/2. Stage deadline uses 1 second and continues; active suite deadline uses 30-second stage versus 2-second suite, yields TIMEOUT/suite_budget then BLOCKED/suite_budget, never launches the suffix. SIGINT/SIGTERM are readiness-coordinated to the aggregate and yield active CANCELLED with exact signal reasons, unstarted BLOCKED/cancelled and 130/143. Each process control verifies the spawned child no longer exists and waits past its planned delayed-marker write; no marker survives. Bounded output emits >2 MiB and exits 7; retained head/tail <=1 MiB preserves that exit.

Missing and wrong exact SDK cases BLOCK affected stages without launching fixture commands; independent shell-only stab still passes. Unknown/empty IDs, bad timeout values, invalid list combinations and invalid legacy selectors return 2. --list/--help work without the SDK. Output preservation/report-path cases retain sentinels and exit 2. Inventory drift fails overall while selected known commands complete. A zero-exit selector without completion marker becomes ERROR/protocol. Actual AUTH LoginVM selected twice is deduplicated and yields PARTIAL_PASS, 1/1 selection, AUTH logical PARTIAL, logical_pass=0, 50 NOT_SELECTED, complete_required=false.

Real selector controls copy every available tracked source/test input byte-for-byte into external temporary scratch and retain input hash metadata. Scratch-only #error in LM01 makes the original default chain fail before the real LM05 suffix; LM05 selection still runs **22** real checks and completes. Scratch-only #error in AUTH01B makes default Account compilation fail before the real LoginVM suffix; LoginVM selection still runs **39** real lifecycle checks and completes. Explicit control assertions now check absence of suffix business output, not only absent markers. Prefix files are restored and every copied input hash is rechecked. No original product assertions/stubs/scenarios were modified.
