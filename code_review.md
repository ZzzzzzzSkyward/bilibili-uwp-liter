# Review — `92e544d9df785a2e3d2bac5da9e1dfe1fc5377b0`

Read the current `bug_report.md`, including BR-17 through BR-21, and reviewed all new source changes since `365ea8da0d62df51032fb7dd7c0298e78cffeda1` (completed review `216b1907`), using GPT-6-luna reviewers. Static source inspection only; no tests, builds, application/script execution, live API calls, or other validation. The report's device observations were not rerun here.

## A legacy-first fallback can still pin the feed to the source that cannot paginate

`src/BiliLite.UWP/Modules/Home/DynamicVM.cs:156` and `Modules/User/DynamicVM.cs:842` order the fallback chain as Polymer → Legacy → gRPC when the selector prefers Legacy. If Polymer has a temporary failure and `dynamic_new` succeeds, the legacy success immediately pins that source and gRPC is never attempted, even if it has recovered and could paginate. The report already establishes what happens next on Legacy: the all-types response lacks a usable offset, and home-video `dynamic_history` returns 404. These feeds can therefore stop after the first screen despite an available paginating source. The new append policy cannot switch away from that pinned source afterward.

Keep the deprecated Legacy source last in both chains. The selector may determine whether gRPC or Polymer is tried first, but the remaining paginating candidate should be attempted before accepting the known first-page-only legacy fallback. This preserves the useful 20-row last resort without allowing it to hide a working full feed.

No other worthwhile issue found in the web-feed mapping or the player-overlay/button changes. BR-21 remains a reported memory-growth symptom without measurements; this static pass establishes no specific new leak.
