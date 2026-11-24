# Stage-aware runner requirements review

Reviewer: `logic_review`, `gpt-6.1-sol` / `xhigh`, independent from requirements author. Read repo/cloud instructions, memory index, this round's plan/requirements/evidence, actual legacy/default/selector boundaries, official workflow source/pins and current comments/package integration ownership. Own only `review.md`.

The author labels CORE ready and final integration blocked until comments runner is finalized/released. Review concrete core behavior now; identify actionable scope/protocol/runtime/CI issues rather than inventing requirements. Do not grant full execution while the current-stage manifest/denominator/invocation contract is unresolved. Re-review final integration after the comments owner releases its runner. Confirm core requirements already state the correct binding gate, preserve original default behavior and full stage independence, bounded truthful outputs and failure/missing/timeout/cancellation propagation.

No product/test/workflow/requirements edit or commit. Source/quality ownership is still active in other rounds; inspect immutable inputs where needed, do not run destructive controls in shared checkout. All conclusions/PASS-or-CHANGES scope go in this review file. Final chat artifact/status only.
