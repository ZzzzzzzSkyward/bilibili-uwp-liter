# API-SMART-01 — Adaptive dynamic API selection

## Summary

Add a fully automatic selector that remembers recent gRPC and legacy results across restarts and predicts which source should handle the next request.

Prioritize reliability, then latency. Use a small deterministic scoring model; no trained model, new dependency, background probes, or settings UI.

Append this feature as the next batch in `plan/PLAN.md` and add its entry to `plan/REMAINING.md` during implementation. This planning turn changes no files.

## Coverage and integration

Cover the existing paired paths:

| Operation | gRPC | Legacy |
|---|---|---|
| Home video feed | `DynVideo` | `DyanmicNew` / `HistoryDynamic` |
| User all feed | `DynAll` | `DyanmicNew(All)` |
| User video feed | `DynVideo` | `DyanmicNew(Video)` |
| Space feed | `DynSpace` | `SpaceHistory` |
| Dynamic detail | `DynDetail` | `DynamicDetail` |
| Reposts | `RepostList` | `DynamicRepost` |

Keep these six histories separate. Home and user video feeds share an RPC but use different conversion and legacy pagination paths.

Exclude playback, submission search, article/season/topic feeds, and write operations from v1.

Integrate selection in the existing VMs, where both sources and their conversion rules are known. Keep `GrpcService` as the gRPC request layer; do not move UI models or pagination into the selector.

## Selector behavior

**History and prediction**

- Key each bucket by operation, signed-in account ID or anonymous state, login app key, and legacy Android app key. Capture this context before awaiting a request.
- Retain the latest eight scored attempts per source, discard samples older than 24 hours, and weight recent samples using a 30-minute half-life.
- Estimate reliability as `(1 + weighted successes) / (2 + weighted attempts)`. Record successful request duration through response decoding, excluding UI rendering.
- With no history, preserve gRPC-first behavior.
- Prefer a challenger when its reliability estimate exceeds the current preference by at least `0.10`. When estimates differ by less than `0.10`, switch for speed only after both sources have three successful measurements and the challenger’s weighted latency is at least 20% lower. Otherwise retain the preference.
- Keep thresholds as named constants in the selector.

**Failures and recovery**

- An explicit endpoint rejection, including `-663`, temporarily excludes that source. Repeated failed recovery attempts use cooldowns of 2, 5, 15, then 30 minutes.
- Two consecutive transport, timeout, HTTP 5xx, or malformed-response failures within two minutes trigger a one-minute cooldown.
- A successful attempt clears that source’s failure streak and cooldown.
- Check recovery through real user requests only. At a new-feed or detail boundary, an untried or unused alternative becomes eligible for one trial after five minutes and after its cooldown expires. Reserve trials atomically so concurrent pages do not all probe it.
- Try at most two sources for one logical request. If the preferred source fails, try the alternative once, even when its history is poor. If both fail, retain the existing error presentation; never loop.

**Pagination**

Choose the source on initial load or refresh, then pin it for that feed’s lifetime. Score subsequent page requests, but do not change source merely because another source’s score improves.

On failure, keep the existing source-switch behavior:

- Home gRPC → legacy: continue using the last published dynamic ID.
- User feed, space, and repost gRPC → legacy: restart legacy pagination and replace rows on the first successful replacement page.
- Legacy → gRPC: restart at gRPC page one and replace rows after success.
- Never transfer opaque cursors between sources. Preserve displayed rows if replacement fails, and discard results from superseded or closed requests.

## Interfaces and persistence

Add a small shared `DynamicApiSelector` with:

- `Choose(context, operation)` returning the first source, decision reason, and optional recovery-trial reservation.
- `Record(attempt, outcome, elapsed)` recording exactly one result against the captured context.
- An injectable clock and storage adapter so selection remains independent of UWP and networking.

Add structured failure metadata to `GrpcResult`, preserving HTTP status, gRPC status, Bilibili status, and transport-failure kind. Preserve it through a typed exception from dynamic `GrpcService` methods. Keep existing method signatures and readable error messages; do not classify failures by parsing message strings.

For legacy requests, inspect HTTP status and the JSON business code separately.

Outcome rules:

- Successful decoding and a valid response envelope count as success; valid empty lists also count.
- Deleted/private/not-found items, login requirements, cancellation, superseded requests, and local mapping/rendering failures do not lower endpoint availability.
- Endpoint rejection, transport failure, and malformed response count as failures. Preserve unknown business errors for diagnostics without inventing a permanent “unsupported” classification.
- Delay recording until the outcome is classified, avoiding a transport success followed by a second failure record for the same attempt.

Persist a versioned snapshot in app-local `dynamic-api-history.v1.json`. Bound it to 32 operation/context buckets, evicting the least recently used. Store timestamps, outcomes, durations, preference, and cooldown state; exclude credentials, item IDs, and cursors.

Load lazily, serialize writes, and coalesce updates over two seconds. Missing, incompatible, or unreadable history starts fresh; storage failure leaves selection working in memory. Log source, reason, scores, and fallback outcome through existing diagnostics.

## Acceptance scenarios and delivery

Under the standing restriction, use source inspection only—no tests, builds, application execution, package inspection, or validation commands.

Implementation must account for:

- Cold start, persistent legacy preference after repeated gRPC rejection, and recovery to gRPC.
- Symmetric fallback when legacy fails first.
- Valid empty pages and content-specific failures leaving availability unaffected.
- Refresh preserving learned history while resetting pagination.
- Independent histories across operations, accounts, and app-key configurations.
- Concurrent recovery attempts, account changes during requests, and closed-page continuations.
- Expired or unreadable history returning to the default order.
- Source changes replacing rows correctly without cursor mixing or duplicates.

Implement in three sequential batches: structured outcomes and selector; VM integration and pagination; persistence, diagnostics, and planning-index updates. Use GPT-6-luna for detailed implementation work. No new UI or API experimentation is required.
