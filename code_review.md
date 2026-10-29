# Review — `96de3fc673eb5cd03194156657fcfdf97f32e82d`

Read `bug_report.md` and reviewed all source changes since `6ae19816fbeda3d1e14ee01364c772799a665828` (review commit `96c26cd`), using three GPT-6-luna reviewers. Static source inspection only; no tests, builds, application execution, package inspection, or other validation.

## Repost fallback appends the first page again

`src/BiliLite.UWP/Modules/User/DynamicRepostVM.cs:106-112,127,145-155`: after a successful gRPC page, a failure on a later page switches `useRestFallback` on, but `restOffset` is still empty. `LoadRestPage()` therefore requests REST page one and blindly appends it to the gRPC items already displayed. When the sources contain the same reposts, those entries appear twice; loading more REST pages can repeat more of the previously loaded list. A later gRPC page that maps to no usable cards takes the same path.

Keep the cursors separate, but reconcile the displayed list when changing sources. The simplest fix is to replace the list with the first successful REST page on fallback and continue with its offset. If keeping the already loaded rows is preferred, deduplicate REST additions by `DynamicID` while still advancing the REST cursor when a whole page overlaps.

The earlier space-feed timezone finding is fixed by `LocalTime = 8`. No other worthwhile issue found in the home-feed fallback, collection model/detail UI, audio detachment, WebView close handling, or Share subscription cleanup. The bug report still describes the earlier packaging/device round; its reported results were not rerun here.
