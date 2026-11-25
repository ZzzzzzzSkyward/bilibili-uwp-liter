# Requirements discovery evidence

Managed reproduction: `/workspace/work/review-singleflight/discovery/setup-proof.py`, fixture `/workspace/work/review-singleflight/discovery/repro/ShortReview.csproj`; receipt `/workspace/work/review-singleflight/discovery/proof-binding.json`; output `/workspace/work/review-singleflight/discovery/proof.log`. Command: `python3 /workspace/work/review-singleflight/discovery/setup-proof.py`; exit 0. Its receipt binds every fixture source, setup script, proof log and production caller source with SHA-256. SDK 8.0.427, runtime 8.0.33 at `/workspace/work/maintenance-foundation/quality/dotnet`.

The fixture copies the existing read-only prior full API/VM fixture into the new private discovery namespace, then overwrites SeasonReviewVM, IModules and SeasonApi with current working-tree files. It does not edit or build the original fixture, product tree, shared tests, Git/index/refs. `Caller.cs` contains the exact production `pivot_SelectionChanged` method body and signature, extracted between its method anchor and the next method. Native Pivot/SelectionChangedEventArgs are stand-ins; the real VM and request builder execute behind an injected Request transport, with real fixture HttpResults implementation. The page XAML explicitly wires SelectionChanged to this event, index 3 is the review predicate, and the page initializes MediaID from season detail.

Observed checks, all seven exit-0 assertions establish current defects/controls:

1. First extracted real pivot entry dispatches one request and leaves Loading true/Items empty while held.
2. Leave/reenter before response dispatches a second request through the same exact caller.
3. Completing the held response appends the same response twice and releases Loading.
4. A MediaID-zero direct call dispatches nothing but sets Loading false while the original task is still incomplete.
5. Restoring ID and executing LoadMoreCommand during that held request dispatches a second request because the zero-ID invocation cleared Loading.
6. A synchronous PropertyChanged listener on Loading=true directly reenters GetItems and produces two requests.
7. Existing throwing transport is caught, emits a toast and releases Loading; this is a preservation control.

`SUMMARY observed_checks=7 failures=0`. This proof intentionally asserts the observed old behavior; it is not a passing implementation acceptance suite. It demonstrates a real managed caller/VM overlap, not actual Windows event delivery. Discovery did not alter parsing/paging, auth/write paths or any product/test/main-document file. No Windows/device/hosted result inferred.

Requirements scope and checks are in requirements.md. Private release receipt contains exact final output hashes and relinquishes ownership; no further requirements edits after release without a new grant.
