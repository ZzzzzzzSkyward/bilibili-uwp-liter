# Review origin/dev 59e8175cfb3dc9e4b8ecf522c7fac88c6a393930

Reviewed all new source changes since `33debce49b772dbfcf98fda3bd48a4f0954d2b9c`, with Luna reviews of playback checkpoints and the card/UI changes. Static source inspection only; no tests, builds, linters, application execution, or runtime validation.

## A pending history request discards the pause and exit checkpoints (BR-60)

Location: `src/BiliLite.UWP/Controls/PlayerControl.xaml.cs:3390`, before the local save at line 3400; pause calls the same method at line 3426 and the awaited app flush calls it at line 3431.

Trigger: a 30-second checkpoint has saved its position and is still awaiting `ReportHistory` at line 3406. While that POST is pending, seek substantially forward, pause, and close the app before the request finishes. The progress slider updates `Player.Position` directly (`PlayerControl.xaml:606`), without independently saving it.

Effect: `_historyCheckpointRunning` makes both the pause save and the final flush return before reading or saving the new position. `OnExiting`/`OnSuspending` can therefore complete their awaited flush immediately, without waiting for the pending report or recording a final checkpoint. Reopening with resume enabled uses the pre-seek tick position, potentially many minutes behind the point the user deliberately selected. Without a seek, the lost progress is the playback interval since the pending tick. The usual page-disposal save does not cover closing the application, the journey BR-60 is fixing.

Suggested fix: save the current item's local position before suppressing overlapping network requests, so a pending POST cannot block a local-only pause or exit save. Track the pending report as a task and coalesce a requested final report behind it; let the lifecycle flush await that work and report the latest captured item/position rather than returning on the busy flag. Keep ordinary timer ticks from issuing overlapping reports.

The previous duplicate reservation panel finding is resolved by the ownership handoff after text-to-reserve promotion and is not repeated. This range provides no new evidence identifying BR-21's memory-growth cause or BR-53's failing interface response.
