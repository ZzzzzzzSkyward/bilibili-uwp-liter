# Requirements-review dispatch

Read `plan.md`, `intake.md`, `cloud/memory/INDEX.md`, applicable repository instructions and all three final requirements artifacts. Review your assigned stream against actual source and the other streams for conflicts. Do not modify requirements or product/tests/build/docs. Own only your stream's `review.md`.

Use `cloud/templates/review.md`; state PASS or CHANGES, actionable P0/P1/P2 findings, exact acceptance, file ownership and environment limits. Cite source/evidence. A document that merely lists defects is insufficient; a passed executable slice must define behavior, dependency/encoding contracts and meaningful red/green verification.

## Logic reviewer

Review `logic-data-errors/requirements.md` and `source-evidence.md`, including both fully specified U1 and PB1. Inspect real callers and existing probe inputs. PASS can approve U1 and PB1 as sequential slices under one exclusive comments-probe owner. U9/search/dialog are triaged follow-ups and need their own detailed requirements/review before execution. Check CSRF multiplicity, raw-vs-encoded message contracts, shared-helper blast radius, noninteger/BigInteger code behavior, payload rules, regression preservation and honest Windows/session status.

## UI/UX reviewer

Review `ui-ux/requirements.md` and `evidence.md`; cross-check shared U1 behavior and acceptance with the logic stream. Verify core user need coverage, existing import/export/bulk operations, device-acceptance policy, failure/draft/retry behavior, accessibility and scale matrix, and whether observed source gaps versus runtime hypotheses are labeled honestly. This pass approves the maintenance policy and U1 UX acceptance; other UI work remains a separate scoped follow-up.

## Quality reviewer

Review `verification/requirements.md` and `evidence/baseline.md`, including current actual-source inputs and scratch setup. Inspect failing test harnesses and determine the narrow QF1 repair ownership and preserved behavioral intent. Do not approve weakening a stale source-text assertion merely to get green; require current single-flight/pending-tail behavior evidence and a negative control, or explicitly keep that part FAILED. Check complete denominators and later masked single-file stages, Bash invocation, SDK/reference packs, original-asset availability and Windows boundaries. QF2 runner/CI changes must have a concrete owned shape before implementation; no workflow publication or dependency migration is implied by a plan.

## Cross-stream decisions supplied by orchestrator

- Use Bash to execute scripts that declare Bash; the documented `sh` invocation is a demonstrated portability failure on this host. Do not force code changes to preserve that invocation at the cost of actual semantics.
- First source executor owns U1 and then PB1 sequentially, with per-slice evidence and commits; comments tests/docs are shared and cannot have concurrent writers.
- Quality executor may run independently only with disjoint probe harness ownership. It must not modify comments/c1/season1 while the source executor owns them.
- Main documentation (`plan/PLAN.md`, `docs/ROADMAP.md`, `plan/VALIDATION.md`, `docs/DEVELOPMENT.md`) gets one later documentation owner so execution does not race.
- No freshly passed Windows compile/runtime/device check exists. New behavior can be implemented and offline verified with that gate explicitly FAILED/BLOCKED, never described release-ready.
