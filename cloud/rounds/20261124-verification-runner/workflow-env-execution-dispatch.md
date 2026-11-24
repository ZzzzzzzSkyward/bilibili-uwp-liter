# Approved environment repair continuation

workflow-env-review.md is PASS; existing runner_executor retains exclusive owned source/workflow/evidence/index scope. Implement only the exact environment relocation in workflow-env-correction.md, preserving every job/action/pin/permission/value/budget/report field. The separate P1 metadata/source fix and its local red/green/new full gate take precedence.

After the source correction is committed/tested and the exact configuration repair is published/synchronized, finalize review-fix-execution.md (and config evidence) and finish **READY FOR CORE RE-REVIEW** if that verdict is pending. Do not poll/wait in an agent turn or dispatch before the independent core P1 closure. Actual automatic push-run observation is read-only and may be recorded meanwhile.

Before the one authorized corrected-head manual request, check for an actual exact-head run. If the valid workflow's normal push already produced it, observe that run rather than create a duplicate manual job. Otherwise the single ref=dev dispatch after source PASS remains authorized. This narrows unnecessary writes; it does not weaken actual hosted-result acceptance. No settings/identity/permission/branch/release change.

GitHub's actual expression-validation/result is authoritative. Record and supersede earlier HTTP422/no-run findings accurately; do not label configuration errors as outages or hosted PASS from local lint. No unchanged full-suite repeat for metadata or the trigger/env-only delta. All CI/source follow-ups remain artifact-based.
