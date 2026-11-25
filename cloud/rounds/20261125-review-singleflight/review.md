# Independent requirements review

Verdict: **CHANGES**. Requirements/evidence release and every discovery binding SHA-256 were independently verified. Review inputs include requirements.md, requirements-evidence.md, plan.md, root-review-question.md, complete production SeasonReviewVM/IModules, exact production pivot body/XAML wiring, discovery fixture/proof, and existing season1/api02 integration seams. No product/test/main-document/Git mutations occurred.

## P1 — include command admission in the same owner protocol

The proposed product scope says “limited to GetItems ownership state/guard/cleanup”, but Refresh currently checks only Loading and clears Items/Next before calling GetItems. The required owner remains held while Loading=false notifies synchronous observers. In that notification, RefreshCommand sees false, clears the just-published rows/cursor, then its GetItems is rejected by the held claim. A GetItems-only fix therefore violates required quiet busy-command semantics while appearing single-flight.

Independent private prospective fixture adds only the proposed GetItems claim to a copy of the complete real VM and invokes the actual RefreshCommand from Loading=false notification. Actual output: `COMPLETION_REFRESH before=1 after=0 next= dispatches=1`; exit 0 establishes the counterexample. Full log/source hashes are in review-evidence.json. This is an isolated hypothetical implementation, not a product modification or fixed acceptance result.

Required revision: explicitly permit/require admission checks in existing Refresh and LoadMore against the same private owner state before any command mutation, retaining existing Loading compatibility. Still allow exactly one product file, SeasonReviewVM.cs, with no API/parser/paging/write changes. Extend the durable regression matrix to invoke GetItems, RefreshCommand and LoadMoreCommand from the completion Loading=false notification and assert no dispatch, clear, Next change, notification or owner release by rejected work. The successful owner's rows/opaque cursor must survive; an idle subsequent Refresh must clear/reset and dispatch normally. Also exercise command rejection in Loading=true/CanLoadMore notification callbacks.

## Otherwise acceptable and feasible

Real repeated pivot entry/duplicate append, zero-ID false release, synchronous Loading=true reentry and legacy throwing-transport cleanup are bound to complete actual VM/API/IModules and exact caller extraction. Acquisition-before-notification, zero-ID/busy rejection outside owner-finally, claim-through-completion, explicit failure retry, content/pagination/SR1 retention and boundary exclusions are appropriate. Guard cleanup should remain exception-safe if a completion notification throws; a nested finally is a compatible implementation option.

The exact proposed six test paths plus one product path are feasible: existing season1 has real IModules/RelayCommand, Newtonsoft, transport AsyncFixture and extraction/hash infrastructure. Add only review read seams, GetJson compile seam and throwing review-write seams; keep all existing assertions/fixtures and the distinct lifetime fake unchanged. api02 remains the real builder/route/auth/query control. A new batch/shared runner/stub is unnecessary. New real-VM tests must use held Tasks, bounded waits and baseline sensitivity, including this completion-command case. Native delivery/Windows/device/hosted gates remain unverified.

No additional P0/P1 findings. Re-review the narrow amended requirements before execution. Review outputs FROZEN/RELEASED; private release receipt binds final files.
