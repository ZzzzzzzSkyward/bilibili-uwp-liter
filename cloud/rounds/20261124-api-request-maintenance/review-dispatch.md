# Independent requirements review — next wave

All three next-wave requirements owners have finished. Read this round's `plan.md`, all three requirements/evidence pairs, applicable repository/cloud instructions and memory index. The preceding round is executing U1/PB1 and QF1 under `execution-dispatch.md`; no reviewer may modify their files or confuse intermediate edits with a stable baseline.

Use the bound original source/evidence and approved preceding contracts when inspecting shared files under active implementation. The unchanged FavoriteAPI/SearchAPI/SendCommentDialog sources can be inspected directly. Execution still waits for requirements PASS and release of any overlapping owner, plus U1/PB1 dependency completion for comment recovery. Review findings go only to the assigned stream-local `review.md`, with PASS/CHANGES, actionable severity and exact acceptance/ownership.

| Reviewer | Exclusive output | Focus |
| --- | --- | --- |
| `logic_review` (`gpt-6.1-sol` / `xhigh`) | `favorites/review.md` | U9 exact timestamp/privacy/form fields, checkbox meaning, raw text encoding, IDs, configured URL, current Android key/signing input and minimal real-source c2 test seam. Preserve all existing assertions; no global helper/credential changes or live folder writes. |
| `quality_review` (same model/effort) | `search/review.md` | U5/U8 direct/configured API versus area/proxy cookie identity, no invented BUVID, complete seven-builder coverage and app order/filter/signing inputs; exact source seam/fixtures and test ownership disjoint from active quality work until release. |
| `ux_review` (same model/effort) | `comment-recovery/review.md` | E1/UX-02 synchronous event cancellation plus owned async Task contract, typed success, draft/busy/retry state, logical abandon vs network rollback, duplicate/cancel/Closed/late completion races, truthful uncertainty text, actual-source probe and necessary Windows boundary. Verify cited official UWP API evidence and do not invent a device BR. |

Review current requirements, not an imagined larger refactor. If a needed detail or source-verified contract is missing, request a concrete bounded revision; do not grant shared file ownership implicitly. Documentation remains one later owner. No product/test/build/main-doc edits, no commits/pushes or new live-service writes from a reviewer. Report only artifact path/status in chat.
