# Independent amendment re-review

Verdict: **PASS** for original requirements.md together with requirements-amendment.md. Original review.md remains CHANGES as immutable history; its sole P1/R1 is closed by this additive amendment. All original release, first-review release and amendment release hashes independently match current bytes.

The amended scope explicitly permits the same private read-owner admission in GetItems, Refresh and LoadMore, before observable or destructive work, inside the same sole product file. It covers the demonstrated completion Refresh counterexample: every entry rejects while Loading=false notification runs under the held owner; rows/cursor survive; later idle Refresh resets and dispatches. Acquisition before Refresh collection notifications and prevention of command self-rejection are explicit. Existing Loading compatibility and zero-ID idle/public semantics remain stated.

The additional durable matrix requires one-shot Loading=true/CanLoadMore=false/Loading=false nested public/command checks, snapshots of every relevant state/notification/dispatch, idle retry and completion-throw cleanup. Throw-safe release preserves existing propagation policy. The real-VM held-task regression and exact pivot extraction remain mandatory, with bounded awaits and original/GetItems-only baseline sensitivity. No parser, pagination, generation, cancellation, API/auth/write or native scope expansion is authorized.

The exact source/test set is six total paths: one product file plus five tests, as enumerated in the amendment. The first review's prose count of six test paths was a reviewer counting error; its listed paths already described this same set. Existing fixtures/assertions and distinct lifetime fake remain unchanged. Existing api02 supplies actual request-builder control without edits. Selected season1/api02 success is PARTIAL_PASS; complete aggregate and native/Windows/hosted limits retain their original meanings.

No open P0/P1/P2 requirement findings remain. This requirements PASS does not certify an implementation or issue an execution grant. Outputs FROZEN/RELEASED, bound by private amendment/release.json.
