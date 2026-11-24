# Hosted-validation amendment execution — trigger published; one dispatch rejected

Owner: runner_executor. Read the exact amendment, independent PASS and execution dispatch. Startup fetch/pull succeeded at synchronized `b59ae0d60dd40a03fa191c08ca870b99a9384247`. Existing dirty main-document/orchestrator files belonged to other owners and were not staged.

## Exact approved change and validation

Published only additive no-input `workflow_dispatch:` under `on` through GitHub contents update-file, using current blob lease `6b6a98cad3c52434fc217bc09c4038ac46919719`. Publication commit **`636484d3507384ce48cac8a3b32766c497494041`**, resulting workflow blob `5164de4292e04acab65cb7627e9ee7457a957766`. Pulled --ff-only and verified local HEAD/origin/dev/ls-remote equal this SHA; repository API again confirmed actual default branch `dev`.

The complete diff is exactly one inserted line; removing it yields byte-identical old workflow. New workflow SHA-256 **`d05b81e7011de2f33fc0f02dfc11f2a02743c563e34f59f7e3d55a0fcf3dd491`**. Every existing job/action/tool/permission/branch/timeout/report/artifact byte is unchanged. YAML parsing and combined job Bash syntax pass; those checks do not validate GitHub expression contexts. All 869 bound source/test/runner input hashes remain unchanged. Existing local 11/11 controls, 50/51 aggregate and default chain results remain valid and were not repeated for this trigger-only change.

Reused current official docs already retrieved for the independent review, following read-docs: manual execution requires workflow_dispatch on the default branch; inputs are optional; endpoint requires ref; a no-details successful response would be 204 and would not itself establish CI PASS. No dispatch-specific connector tool was callable, so the expressly approved existing authenticated gh API fallback was used, without credential/identity changes.

## Exactly one dispatch and actual result

Pre-dispatch exact-head run query returned `total_count=0`, so no existing real hosted run was ignored. At **2026-11-24T15:57:49.571614Z** sent one request, completed **15:57:51.049789Z**:

```http
POST /repos/ZzzzzzzSkyward/bilibili-uwp-liter/actions/workflows/429242571/dispatches
{"ref":"dev"}
```

The structured JSON body was supplied to `gh api --method POST ... --input -` via subprocess stdin. No inputs, extra ref, token output or alternate identity. **One write attempt only.** Response **HTTP 422 Unprocessable Entity**, gh exit **1**:

```json
{
  "message": "Invalid Argument - failed to parse workflow: (Line: 19, Col: 28): Unrecognized named-value: 'runner'. Located at position 1 within expression: runner.temp, (Line: 20, Col: 27): Unrecognized named-value: 'runner'. Located at position 1 within expression: runner.temp, (Line: 21, Col: 20): Unrecognized named-value: 'runner'. Located at position 1 within expression: runner.temp, (Line: 22, Col: 24): Unrecognized named-value: 'runner'. Located at position 1 within expression: runner.temp, (Line: 23, Col: 23): Unrecognized named-value: 'runner'. Located at position 1 within expression: runner.temp, (Line: 24, Col: 23): Unrecognized named-value: 'runner'. Located at position 1 within expression: runner.temp, (Line: 25, Col: 22): Unrecognized named-value: 'runner'. Located at position 1 within expression: runner.temp",
  "documentation_url": "https://docs.github.com/rest/actions/workflows#create-a-workflow-dispatch-event",
  "status": "422"
}
```

**Hosted CI: FAILED TO START / BLOCKED by an actual workflow expression-validation defect.** This supersedes the earlier unexplained empty-run observations as the newest concrete evidence. GitHub rejects the seven existing job-level `env` values referencing `runner.temp` (published file lines 19–25), before creating any run/job/artifact. There is no run URL, runner, terminal job conclusion or artifact to claim. A post-request exact-head run query again returned total_count=0. This is not a dispatch permission/enablement diagnosis and not an external-service outage; underlying job configuration needs repair.

The trigger-only amendment expressly preserves all other workflow fields and permits one dispatch. Therefore no job/environment repair, second dispatch, settings/permissions/account/identity change, receipt experiment or alternative-client retry was performed. The existing original implementation reviewer must receive this concrete failure before approving the core workflow. A separately reviewed/owned correction can relocate runtime-derived path environment setup to a GitHub-valid context while preserving the exact paths, pins, read-only job, budgets and artifacts; it must validate GitHub's actual syntax, then obtain a newly authorized hosted validation attempt. Do not count local YAML/Bash parsing as GitHub-expression validation.

Raw bounded request/result and pre/post run receipts: `/workspace/work/maintenance-foundation/runner-execution/manual-dispatch-receipt.json`, `manual-pre-dispatch-runs.json`, `manual-post-dispatch-runs.json`; prepared bytes and equivalence receipt are siblings. Only sanitized service status/body is copied here, no credentials.

## Release

The additive trigger is published; the single bounded dispatch is conclusively rejected. This continuation's **workflow/evidence ownership and global index/push window are RELEASED** after its truthful receipt publication. Final metadata changes only this round's execution evidence. Source/test/runner/default behavior remains preserved; no complete local rerun is justified. Existing Windows/UWP/XAML/MSIX/device/session gates remain BLOCKED independently. No product/main-doc/release/master/live-service write occurred.
