# Review origin/work 0ecf3212c5e04f1f4912b1b96d02555f73322f3f

Read the current `bug_report.md`. The ZIP truncation and unpacked-bundle import fixes address the earlier findings. One new issue in topic-feed error handling; source inspection only, with no tests, builds, or application execution.

## Malformed topic cards lose the error and retry feedback

`src/BiliLite.UWP/Modules/User/DynamicVM.cs:690-694` only logs and returns when `TryDeserialize` fails. With a valid envelope/cards array containing an incompatible card value, the parser catches the `JsonException` and this branch bypasses the outer catch, which previously called `PublishFeedError`. On an initial topic request, the spinner stops with an empty list, no visible explanation and `lastPageFailed` still false; on an append, the old rows remain without failure/retry feedback.

The adjacent parser-failure branch also discards `parsed.Message` unless the failure is `Rejected`. For an HTTP-200 response with a malformed `cards` field, it passes `results.message` (normally empty) to `PublishFeedError`, leaving the error text blank rather than using the parser's explanation.

Suggested fix: route both failures through `PublishFeedError` after the current-generation guard. Use a concrete message such as `动态卡片解析失败` for deserialization failure and the non-empty `parsed.Message` (with a generic fallback) for parser failure. This restores the existing stopped-paging, error display and retry-state handling without changing the loaded rows.
