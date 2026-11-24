# Yield index while external hosted run is queued

The actual corrected workflow_dispatch run 45434541348 exists on reviewed source/workflow head 169b2b0 and remains queued with no jobs assigned at the last read. This is PENDING, not PASS, failed-job evidence or proof of disabled Actions/outage.

If still nonterminal, existing runner_executor should checkpoint the exact current event/head/status/no-job observation in corrected-hosted-execution.md, leave its actual outcome explicitly PENDING, and finish **PENDING HOSTED — INDEX RELEASED** for independent documentation publication. Do not create a receipt commit/push solely to keep polling. Release the global index/source ownership; do not mutate source/workflow/settings or dispatch again. Main orchestrator will continue reading the same actual run and can request a final result receipt later.

If the run becomes terminal before this handoff, record its actual result and finish normally, releasing the index. No pending result may be promoted to passing or permanently failed. This is coordination of necessary independent work while an external runner is unallocated, not weaker hosted acceptance or cancellation of the run.
