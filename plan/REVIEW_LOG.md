# Review log — cached reviewer outcomes

Each row is a reviewer commit on `origin/work`, what it found, and what happened. Read this before
re-opening an area: it records findings that are already fixed so they are not re-litigated, and
findings that were deliberately not fixed so they are not silently dropped.

| Review commit | Reviewed head | Finding | Disposition |
| --- | --- | --- | --- |
| `22f3a63` | `f84ab1b` | Retry counter counted successes; `/confirm/refresh` never sent; `cookie_timestamp` zeroed before refresh | All three fixed — the first two in the API-01 work, the residual (counter reset + timestamp lifetime) in `7f167d2`. Probe `plan/tests/api02`. |
| `d68fe0a` | `d4c6345` | (1) Draw/photo posts sent the dynamic id as the comment oid; (2) a single unreachable gRPC detail card suppressed the REST fallback | Both fixed in `6ae1981`: `GetCommentId` gained the `Draw` case, and the detail branch requires `display.Type != Other` before publishing. Probe `plan/tests/g3`. |
| `96c26cd` | `6ae1981` | `DynSpaceReq` did not send `local_time`, so space responses used a UTC day boundary while the other dynamic requests send 8 | Fixed in `4fca416`; `plan/tests/g4` now pins all three `local_time` assignments. Review explicitly confirmed both earlier detail findings as addressed and found nothing in D2/G5 wiring. |
| `bf03273` | `96de3fc` | After a gRPC repost page, switching to REST appended REST page 1 to the already-loaded gRPC rows, duplicating entries; a later gRPC page mapping to zero cards took the same path | Fixed in `9fb3d16` by replacing the list with the first REST page on the switch, as the reviewer preferred over per-id deduplication. Probe `plan/tests/g5`. |
| `465aa55` | `1dcf688` | Closing the detail tab while an automatic next-page request was pending resumed `AdvancePlaylistAsync`, selected a row and called `InitializeVideo` on a disposed page (`dispRequest` was null) | Fixed in `18f71dc` + `db1b348`: `_isClosed` latch set first in `ClosePage()`, reset on navigation entry (the page is `NavigationCacheMode.Enabled`), and checked after every playlist/detail await. Probe `plan/tests/stab`. |
| `df790a4` | `5e36ec2` | `BiliLite.UWP.csproj` compiled `Services\DynamicApiSelector.cs`, which was absent from that tree — the entry landed in the danmaku commit, the file one commit later | Resolved by `797dc3a`: tree `HEAD` contains both the file and the entry. No code change needed. |
| `df790a4` (same review) | `5e36ec2` | Confirmed resolved: playlist continuation guards; nothing else found in the loaded-danmaku list | No action. |

## Standing review notes

- Reviews are static source inspection only: no build, no launch, no API call. A finding marked here
  as fixed means "fixed in source", not "verified on device".
- The reviewer re-reads earlier findings every round, so a row stays useful for a long time.
- New findings become plan entries: append a row here when a review is acted on, and add the
  corresponding item to `plan/REMAINING.md` when the finding is deliberately deferred.
- Known reviewer-blind spots (state them when a batch relies on them): UWP XAML/visual layout,
  server behaviour, timing under a real device, and anything that needs an account.
