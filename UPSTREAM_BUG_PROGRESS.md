# Upstream issue assessment progress

## Scope and guardrails

The source catalog contains 757 GitHub issues after excluding pull requests; 477 bug candidates retain their original order across 96 batches. The target app source snapshot is `cfa40a8508c6d6e7401885bf6bb081bc5c90b173`; the sole behavior reference is `/workspace/biliuwp-lite` at `ca2caddc597b3faf05b4dac6dcb226aa0694365b`.

Do not change application code, build or restore the app/solution, generate protocol code, call live application APIs, or run the app. Isolated unit execution is allowed for a concrete source-logic claim. The confirmed #31 source defect and its isolated probe are documented in `work/upstream-bugs/VALIDATED_LOGIC_BUGS.md` and `work/upstream-bugs/unit-tests/batch-002/README.md`.

This is a local catalog and assessment record. Do not alter upstream issues or raw collection caches. Stage only assessment documentation, per-batch disposition ledgers, and unit source/README/output artifacts; do not stage runtime caches or binaries.

## Saved assessment checkpoint

Completed original batch ranges: **001–028, 041–052, and 059–083**. Batches 001–003 remain as historical full reports. For batches 004 onward, retain human prose only for source-supported actionable findings; per-batch `policy-dispositions-*.json` files preserve exact statuses and rationale for every reviewed issue.

Actionable source findings currently retained in human reports:

- Batch 002: #31, wrong request object dispatched during cookie refresh; symptom causality remains unproven.
- Batches 008, 009, 011, 020: #105, #121, #146, #275.
- Batches 026–028: #358, #378, #404.
- Batches 041–043: #598, #611, #625, with the scope and test limits described in each report.
- Batches 080–081: #1235, #1243.

`work/upstream-bugs/assessment-ranges/041-058.md` records #725 and #735 as awaiting independent proof. `work/upstream-bugs/assessment-ranges/077-096.md` records #1216 as awaiting independent proof. These remain unresolved review items.

The next unassessed ranges in original order are 029–040, 053–058, and 084–096. Assessment records for those ranges are being added separately. The user-owned visual assignments remain in `work/upstream-bugs/USER_VISUAL_REVIEW.md` and `user-visual-review.json`.

## Canonical queue state

`candidate-queue.json`, `policy-queue.json`, `logic-validation-queue.json`, and the top summary in `batch-index.md` still reflect an earlier assessment checkpoint. **Canonical queue aggregation is pending** until the remaining ranges and independent reviews are recorded. Use the per-batch disposition ledgers and range notes as the authority for the saved checkpoint; do not use the old queue counts or resume point as current status.

The original full candidate inventory and source snapshot are retained. No GitHub issue was edited, and no app source change was made. This checkpoint did not run app builds, restores, live API calls, runtime checks, or unit tests.
