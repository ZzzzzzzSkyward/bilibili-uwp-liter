# Review origin/dev 63e1765651eb1275f8731be5c45cf8f9427f0a6c

Reviewed all new source changes since `456d920de1e248ff2578c98192f379082829297b`, with Luna reviewers and static source inspection only. No tests, builds, linters, application execution, or runtime validation. The previous superseded-open source overwrite is fixed in source and is not repeated.

## Successful engine fallback drops the failed attempt's player without disposing it

Location: `src/BiliLite.UWP/Controls/Player.xaml.cs:356-377` (`PlayDashUseNative`, local player ownership and failure return).

Trigger: native DASH source creation returns null or throws before `Accept`, then `ChangeQuality` successfully falls back to FFmpeg. The new local `MediaPlayer` has already been registered with `HoldSessionPlayer`, but is never published to `_playerVideo`. The caller reuses the same ticket for the fallback (`Controls/PlayerControl.xaml.cs:2589-2611`). FFmpeg's `Accept` clears all held leases (`Services/PlayerSessionState.cs:126-131`), including the failed native player's lease, without disposing them. `ClosePlay` cannot clean that player because it only knows the published field. Before this change, the native player was in that field and fallback cleanup could dispose it. The single-FLV SYEngine-to-FFmpeg fallback has the same ownership pattern.

Each such fallback leaves an abandoned player without deterministic cleanup, allowing native playback resources to accumulate until runtime cleanup catches up and adding memory pressure during repeated opens. This is a concrete disposal gap relevant to BR-21, not evidence that it explains the maintainer's reported growth (no measurements were taken here).

Suggested fix: scope held resources to an individual engine attempt, or add an operation that disposes/removes the failed attempt's leases while keeping the ticket current. Only transfer the successful attempt's resources on `Accept`. Calling the current `Release` alone would invalidate the ticket and prevent the intended fallback. A failed-native/successful-FFmpeg ownership case should assert that the abandoned player is disposed exactly once and the accepted source remains live.

## Dispositions (2026-11-02, dispatcher)

The engine-fallback finding above is closed in `0b70113`. Everything below is source + probe evidence; nothing
was run on a device, and this pass established no measurement for BR-21.

- The defect was reproduced structurally, not just described: `plan/tests/playersess` grew 95 → 161 checks, and
  **23 of the new assertions fail against the pre-fix sources** (a no-op `Abandon` body plus the pre-fix call
  sites). The failing set names the exact consequences the finding listed — the abandoned player is not disposed,
  the accepted fallback is not the only live resource, and a later close does not clean up.
- Fix shape follows the suggestion that keeps the fallback usable: `PlayerSessionState.Abandon(id)` drains only
  the resources held by the failed attempt, disposes them once, and leaves the ticket `Pending` and current;
  `Release(id)` remains the terminal operation that invalidates the ticket. `Accept`'s lease clear is unchanged,
  so the accepted attempt's own resources still transfer to the player field. A stale-ticket `Abandon` and an
  `Abandon` after `Accept` are no-ops, so a superseded open is still released exactly once by `Invalidate`.
- Call sites: seven catch paths across the native/FFmpeg/SYEngine openers plus the native null-source return —
  exactly the eight spots that can return failure before `Accept`. `PlayerControl` needed no change; the probe
  asserts its two same-ticket fallbacks still pass `session: session`.
- On BR-21 wording: this removes one concrete undisposed-resource path. It is not evidence about the maintainer's
  measured growth, and no measurement was taken here.
