# Bounded hosted-validation fallback amendment

Status: READY FOR INDEPENDENT REVIEW; no workflow change authorized until PASS.

Observed after publication commit `79ba0b04fab9c3b79345e212b6cd1aa6a0229fdf`: local/origin/GitHub refs match; GitHub lists workflow ID `429242571`, state `active`, correct owned path/name. Push-run query for that SHA returned no runs; check-runs returned total_count 0. Repository Actions-permissions inspection returned 403 `Resource not accessible by integration`. These facts do not prove Actions is disabled or why no run is visible. Original runnable source tests and full 50/51 local gate passed; hosted CI has no PASS evidence yet.

Execution completion supplies the already-performed step-1 receipt: ordinary git push `00d28afc1fc56657c36d67c2f35869632a653a64` also has no runs/checks at the recorded 15:52 UTC observation. Current synchronized evidence tip is `b59ae0d60dd40a03fa191c08ca870b99a9384247`. **Do not repeat step 1 or create further event-test receipts.** Only the exact additive manual-entry/one-dispatch proposal below remains for review. See execution.md and execution-evidence/ci.md for full actual service evidence.

## Exact fallback and limits

1. The runner owner may publish one truthful owned execution receipt through its already authorized git commit/push path after the workflow exists, retaining unchanged tested code/workflow bytes. Query that resulting dev SHA for a normal push run; this can establish an event without altering repository settings. No repeated empty commits/polling loop.
2. If no hosted run is observable, approve only an additive **`workflow_dispatch:` with no inputs** in the existing owned `.github/workflows/offline-probes.yml`, alongside its unchanged dev-push/dev-PR triggers. Preserve exact job, action/tool pins, read-only contents permission, no secrets/live writes/release/deploy, timeouts, reports and artifact limits. This supplies a normal manual maintenance/verification entry, not an Actions enablement or permission change.
3. After publishing that concrete one-field change and synchronizing dev, request **one** workflow dispatch for `ref=dev` using an already authorized GitHub identity/API. Read current official dispatch syntax/API guidance before the call; use structured JSON, not shell-interpolated data or token printing. Observe the actual workflow_dispatch run, exact head SHA, job/conclusion/artifacts. No local/YAML result substitutes for hosted PASS.
4. If dispatch/visibility is explicitly denied or an external setting prevents execution, record the actual status/error and next action. Do not enable Actions, alter repository/token/branch permissions, install a connection, escalate identity, rewrite history or ask an unneeded confirmation. A policy auto-review rejection may not be bypassed via another route. Normal unavailable connector capability may use the already-authorized gh API fallback; all evidence stays file-based.

No new product/runner/test/default behavior is requested. The same runner_executor owns the existing workflow/receipt; all other source/main-doc ownership stays unchanged. The later docs owner may add the actual manual trigger only after publication, with truthful verification status. Future actual workflow CI results and any service blocker belong in execution evidence; Windows/runtime gates remain BLOCKED.

Reviewer owns only `hosted-validation-review.md`, PASS/CHANGES with actual docs/source/permission boundaries; no implementation or account-setting writes.
