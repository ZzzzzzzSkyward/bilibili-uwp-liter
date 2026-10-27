# Bug 1: AI subtitle auto-selection option

## Diagnosis

The player setting is labeled “默认打开AI字幕”, stored as `PlayerAutoOpenAISubtitle`, and defaults to `false`. It controls initial automatic selection of the first subtitle track when the track label identifies it as AI/automatic-generated. Regular subtitles remain eligible for default selection.

Both downloaded/local and API subtitle menus checked for `自动` or uppercase `AI`, but did not recognize lowercase `ai`. A lowercase-labeled AI track therefore bypassed the disabled-by-default option and was passed to `SetSubTitle`.

## Change

In `src/BiliLite.UWP/Controls/PlayerControl.xaml.cs`, both initial-selection predicates now also recognize lowercase `ai`, matching the pinned sibling implementation. No settings UI or subtitle renderer changes are needed.

## Status

Implemented and committed as part of bug 1. Only `git diff --check` was used for verification; no build, tests, restore, run, or API calls.

## Diagnostic breadcrumb

Both downloaded/local and API initial-selection paths now log the setting value, candidate track label, and whether the initial result was `none` or that track. The log does not include subtitle contents.
