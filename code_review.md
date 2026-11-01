# Review origin/work d9057260d20515aff76aa62c99aa54d53b388e02

Reviewed all new source since `18c519d9821508ea732ec1adc2317edf41e836fc` and reread `bug_report.md`. The icon-only mute toast and percentage-only overlays fit the requested copy policy. One meaningful behavior mismatch accompanies the changed setting label. Source inspection with Luna only; no tests, builds, scripts, or application execution.

## “自动连播” off still advances to the next collection/playlist video

`src/BiliLite.UWP/Controls/PlayerControl.xaml:392` and `Pages/SettingPage.xaml:271` rename the setting from automatic continuation to the next **part** to the broader “自动连播”. Both still control `AUTO_NEXT`. But `PlayerControl.xaml.cs:3083–3093` raises `AllMediaEndEvent` at the final part without checking `PlayerSettingAutoNext.IsOn`; the check only occurs when another part remains, at lines 3094–3104. `Pages/VideoDetailPage.xaml.cs:1020–1023` then advances the outer sequence without checking the preference.

Concrete trigger: switch “自动连播” off and finish a one-part video with another collection/playlist row after it, or finish a multipart video's final part. The app still selects/loads the next video despite the disabled automatic-continuation control. The part-only condition predates this increment; widening the label now gives the user a different promise.

Suggested fix: honor `AUTO_NEXT` before automatic outer-source advancement as well as between parts. Keep the gate specific to automatic completion, so the next button, shortcuts and explicit directory selection still work when automatic continuation is off. If this increment intentionally changes wording only, retain a short part-scoped label such as “分P自动续播” until the broader behavior is implemented.
