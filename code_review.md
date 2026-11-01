# Review origin/dev 456d920de1e248ff2578c98192f379082829297b

Reviewed all new source changes since `094cdb03aa075556d196017dd0c316ef09c46683`, with Luna reviewers and static source inspection only. No tests, builds, linters, application execution, or runtime validation.

## A superseded multi-segment open can still replace the selected video's source

Location: `src/BiliLite.UWP/Controls/Player.xaml.cs:1400-1406` (`PlayVideoUseSYEngine`); the local-file branch has the same ordering at `1392-1396`.

Trigger: open multi-segment video A through SYEngine, then select B while A is awaiting `SaveAndGetFileUriAsync` (or local file/clip creation). `SetPlayItem` invalidates A's ticket and closes its player; B can then install its own player in the shared `_playerVideo` field. When A resumes, it assigns A's source to that shared field **before** the new stale-session check runs. The check returns only after B's player has already been changed, so the selected video can play A's media or fail with B's metadata still displayed.

This is an incomplete fix to the existing asynchronous-open race: the added session guard does not protect the write it is meant to prevent. Keep the in-progress player/source in local variables, check the ticket immediately after the awaits and before any shared-field assignment, and publish/bind them only while the ticket is current. Release abandoned resources without touching the replacement player. The `playersess` probe currently counts callback guards; a case that resumes A after B has installed its player would cover this ordering.
