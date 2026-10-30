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
| `3b95659` | `3665bdb` | Eight async helpers took `out` parameters (CS1988), so the UWP tree could not compile | Fixed in `058c899` with `DynamicApiSourceResult`; scan shows no `async` method with `out` left in the tree. |
| `168ee8e` | `765be46` | A failed gRPC append cleared `_historyOffset`/`_updateBaseline`, so the footer retry sent `null` into protobuf string properties and that page could never recover | Fixed in `261317f`: `ClearGrpcCursorOnRestart` keeps the last good cursor for appends and clears only on a page-1 restart or a row-replacing source switch. `plan/tests/g4` now executes the retry (`offset=hist-1`); verified by reverting the fix and watching the assertion fail. |
| `7dbdcbe` | `15af573` | (1) `OnHolding` returned early when the player was no longer `Playing`, so a hold released after playback ended never closed the held 3x overlay; (2) `sync-work.ps1` could report success after three rejected pushes because the third round left the loop after a rebase without pushing | (1) `14995ab`: the `Playing` check now gates only `Started`; `Completed`/`Canceled` always clear the hold and close the overlay, and `Player_PlayMediaEnded`/`SetPlayItem` release it too. (2) the script tracks `$pushed`, reports "本地提交仍未发布" and exits 1 when no round succeeded. |
| `df790a4` (same review) | `5e36ec2` | Confirmed resolved: playlist continuation guards; nothing else found in the loaded-danmaku list | No action. |

## Standing review notes

- Reviews are static source inspection only: no build, no launch, no API call. A finding marked here
  as fixed means "fixed in source", not "verified on device".
- The reviewer re-reads earlier findings every round, so a row stays useful for a long time.
- New findings become plan entries: append a row here when a review is acted on, and add the
  corresponding item to `plan/PLAN.md` when the finding is deliberately deferred.
- Known reviewer-blind spots (state them when a batch relies on them): UWP XAML/visual layout,
  server behaviour, timing under a real device, and anything that needs an account.

## Probe maintenance (2026-10-30, after API-SMART-01)

The selector batches rewrote internals that several probes pinned as source text, so the suite was
reconciled: `g3` 34, `g5` 44 (was 65), `smart1` 160, everything else unchanged, all green.
`g4` has since grown to 75 by executing the BR-16 quiet-failure contract (`LoadMoreFailureStaysQuiet`) and
the append-retry cursor contract (`AppendFailureKeepsTheGrpcCursor`).

- `g3`/`g4`/`g5` now compile the selector sources with a probe-local in-memory store and stub
  `SettingHelper`/`ApiHelper`/`HttpResults.code`, because the home VM references them.
- Retired assertions that pinned removed internals (`useRestFallback`, `replaceListOnRestPage`,
  `spaceRestMode` and the `stage=rest-fallback` log line). The behaviour they guarded is now covered
  by executed assertions (REST called once, rows published/replaced, cursor not taken from the
  response offset) plus the selector probe.
- Kept: every executed assertion over real product code, plus the csproj compile checks.

The lesson: source-text assertions survive refactors badly. Prefer executed assertions over real
product types, and text checks only for wiring that cannot run outside UWP.
