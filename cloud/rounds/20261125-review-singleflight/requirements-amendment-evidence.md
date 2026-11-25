# Addendum evidence

Inputs: immutable original requirements/evidence, `review.md`, `review-evidence.json`, `root-review-question.md`, and `requirements-amendment-dispatch.md`. The independent reviewer bound a private GetItems-only prospective guard and the actual RefreshCommand in the completion Loading=false callback. Its recorded output `COMPLETION_REFRESH before=1 after=0 next= dispatches=1` establishes a row/cursor mutation despite only one dispatch. This addendum relies on that existing bound evidence, not a new claimed fixed implementation.

The concern follows the current real code: Loading setter assigns `_loading` then synchronously invokes PropertyChanged; Refresh only checks Loading then clears Items and Next before GetItems. Holding a new private claim during completion therefore requires the command's own shared admission check before its destructive preparation. LoadMore must use the same owner check for consistent quiet rejection.

This amendment expands no product path and changes no product/test/main-doc/Git state. It explicitly reconciles the review's counting typo as one product plus five tests. It requires throw-safe owner release without changing notification exception policy and adds bounded completion-command/retry checks. No new Windows/device/hosted assertion is made.

The private `/workspace/work/review-singleflight/discovery/amendment/release.json` binds every final additive output and the original frozen inputs; status FROZEN/RELEASED relinquishes amendment ownership for independent review.
