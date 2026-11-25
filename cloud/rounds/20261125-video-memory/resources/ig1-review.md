# IG1 transition wiring correction-design review — PASS

**PASS / RELEASED. No P0/P1/P2 design correction requested.** The proposed two-method correction is compatible with accepted PF1 ownership and the unchanged maintained ux1d source checks. Read custody is released. Root may grant the same player owner this exact correction only after every verifier/publisher/reader release. No implementation, test/assertion edit or publication authority is issued here.

Frozen Control: `b3823bdb9fe15e7f90465a78446f6480fc81c8c92832de6f72d1762621a0a3b7`. The first actual combined gate remains **FAIL:51/52 executable and50/51 logical PASS**, sole failed stage ux1d. Its report/log must remain immutable. Runtime ownership controls passed; this is maintained source-wiring compatibility, not a newly observed native/runtime defect.

## Exact approved direction

Only `ShowTransitionPending` and `ClearTransitionPending` in PlayerControl.xaml.cs may change:

1. **Show:** retain the first terminal guard, transition/target assignments, null-safe label/host/accessibility work and ApplyNextLabel. Replace its eligibility-helper call with a real null-safe **direct** `BottomBtnNext.IsEnabled = false;` assignment. Pending transition owns that disable regardless of current switch state.
2. **Clear:** retain the first terminal guard, transition/target clearing, overlay collapse and ApplyNextLabel. Replace its helper call with a null-safe executable conditional: direct `BottomBtnNext.IsEnabled = true;` only when no current local-track switch owns the restriction; otherwise explicitly retain/assign false. Clear has already removed its pending reason before evaluating current switch eligibility. No unconditional re-enable or saved old IsEnabled snapshot.
3. Neither transition method needs to rewrite local selector enabled state. The existing UpdatePlaybackControlEligibility helper and real item/track paths keep their current behavior and remain unchanged. No new helper/field/eligibility framework, current-item requirement or UI policy is needed; pending state may legitimately precede item initialization.

The direct statements must execute on their ordinary valid branches. A comment/string/unreachable branch satisfying Contains, a test change, or an extra helper call plus redundant selector rewrites is not this correction.

For a live control, the resulting Next truth table remains:

| Current switch | Show pending | Clear pending |
| --- | --- | --- |
| false |disabled|enabled|
| true |disabled|disabled|

Terminal entry returns before state or UI writes. When a matching current switch finishes, its unchanged helper still consults `_transitionPending`, so completion cannot release a still-pending transition. When transition clears during a switch, only that transition reason is removed. Obsolete A's guarded finally/shared-model behavior is untouched. Labels, visibility, directory behavior and pending keyboard/click policy remain unchanged.

## Required later verification

Preserve every existing assertion/fixture. Capture currentb382 before editing and bind new full source plus exact two changed method spans. Run unchanged owning playersess and unchanged ux1d as the directly affected selected gate; ux1d must genuinely change from its preserved failure to green, including the later Clear inline-true check that the first failure prevented from executing. Existing full actual-method PF1 tests already exercise Show/Clear interleaved with B's held switch, current settlement, labels, stale A and terminal entry; no new test or assertion weakening is required by this design.

Retain all66 residual groups (58 plus RF1 eight),39 lifetime, five graphics and original240 checks. Rebind unchanged G1/history spans and all source outside these two methods. Selected success remains partial; root owns a new complete stable51/52 gate after review/release, justified by the actual source correction. The first FAIL receipt is not relabeled or overwritten.

This design review ran no tests or executable prototype and changed no source/test/main-document/index/ref/cache. Exact failed-state source/assertion/report/log bindings and reasoning are in [ig1-review-evidence.md](ig1-review-evidence.md). All prior native/build/Windows/crash/RAM/GPU/service/session/hosted limits and protected constraints remain unchanged.
