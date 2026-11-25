# Addendum: shared admission and completion commands

Status: READY FOR INDEPENDENT RE-REVIEW. Additive revision for review.md P1/R1; original requirements.md, requirements-evidence.md and first CHANGES review remain immutable. Where the original product-scope sentence limits edits to GetItems, this addendum explicitly supersedes that limit as follows. All other exclusions/acceptance remain effective.

## R1 resolution

The one permitted product file remains `src/BiliLite.UWP/Modules/Season/SeasonReviewVM.cs`. Changes may cover a private per-instance read-owner state/admission helper, GetItems, Refresh, LoadMore, and narrowly necessary private read core/cleanup. The public entry and both existing commands must reject against the **same private owner state**, as well as retain the existing public Loading compatibility check. Refresh must check this state before `Items.Clear()` or `Next=""`; LoadMore must check before any delegation. Rejected work returns quietly without notifications, dispatch, mutations or cleanup/release. Do not change Like/Dislike/SendShortReview/API/parsing/paging semantics.

Acquisition must precede observable work for an admitted request. The accepted owner's claim spans Loading=true, CanLoadMore and completion Loading=false notifications. A completion callback therefore sees Loading=false but the private claim still held. GetItems, RefreshCommand and LoadMoreCommand invoked there must all return quietly; Refresh must not clear successfully published rows or the opaque Next cursor. Once the original method has fully completed, an ordinary idle Refresh must still clear rows/reset Next and dispatch normally. Existing idle Loading=true behavior must also continue to block commands/public entry even if no private owner was taken.

If Refresh's preparation is factored into a common admitted core, claim before its collection/reset notifications and avoid rejecting the command's own delegated read. If it remains outside that core, guard before mutations and ensure synchronous collection-change reentry cannot cause it to reset an admitted owner's cursor or rows. This is the same-instance synchronous notification boundary already in scope, not cross-thread synchronization or generation/cancellation redesign.

Cleanup must always release the private claim even if a synchronous completion notification throws. For example, an inner try setting Loading=false enclosed by an outer finally releasing the private flag meets this property. A notification exception may propagate as it did previously; do not invent toast/swallowing policy. The required invariant is that, after the throwing listener is detached, the next normal invocation can acquire and dispatch. A rejected invocation must never execute this cleanup.

The zero-ID public GetItems check remains before entering an owner's finally. It cannot release another owner or issue a Loading notification. Retain existing idle Refresh reset behavior; do not add a policy that discards an accepted response because MediaID changes while held.

## Additional durable assertions

In the real-VM season1 probe, invoke each public/command entry from one-shot synchronous listeners for Loading=true, CanLoadMore=false and Loading=false. Snapshot rows, Next, CanLoadMore, Loading notification count and dispatch count around the nested call; reject with no mutation/extra notification/dispatch. For completion listeners the owner's successful row and opaque cursor must survive all three entries. After original completion, call idle Refresh and verify its existing reset plus one new request; release it and verify normal ownership release.

Add a one-shot throwing Loading=false listener: observe the original call's existing fault behavior, detach the listener, assert Loading=false and a subsequent call dispatches exactly once and completes. Use bounded awaits; no hanging callback loop. Also check synchronous Refresh collection notification reentry if the implementation leaves preparation outside its common admitted core. Preserve baseline sensitivity: the reviewed GetItems-only prospective guard must fail the completion Refresh no-clear assertion; original unguarded source must fail duplicate/zero-ID cases.

## Exact path count correction

The original list is **six total code/test paths: one product and five test paths**, not six tests plus one product. No expansion is authorized:

1. `src/BiliLite.UWP/Modules/Season/SeasonReviewVM.cs`
2. `plan/tests/season1/Season1Probe.csproj`
3. `plan/tests/season1/Season1Stubs.cs`
4. `plan/tests/season1/SeasonReviewOwnershipProbe.cs`
5. `plan/tests/season1/extract-lifetime.py`
6. `plan/tests/season1/Program.cs`

No new runner/batch/shared stub, no API02 edits. Existing api02 remains the real builder control. Verification commands, fixed deadline and all Windows/native/device/hosted limits remain as originally written. This document is a requirements amendment, not an execution grant.
