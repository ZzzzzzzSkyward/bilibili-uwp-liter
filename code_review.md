# Review origin/dev 4db379c8abfd7bf259968145ddfdf92463a6d1c2

Reviewed the complete source range after `1dd359c065501bc5f22343beec7d8198c85fa4c9`, using the inherited empty review marker `78f89509745f8e419943d1c109de8e9a26ac0a8d`. Static inspection only; no builds, tests, linters, or application execution.

## Retry can advance the wrong continuation source

Location: `src/BiliLite.UWP/Pages/VideoDetailPage.xaml.cs:1346-1348` (`ResumeInterruptedAdvanceAsync`), with source switching at `1751-1785`.

Trigger: play the last loaded row of a remote playlist, where the current video also belongs to a collection. Press Next and let the next-page request fail. Switch to “按合集连播”, then browse the playlist segment and press “重试”.

The interrupted step saves its source and row, but retry forwards only `SkipRemainingParts` and `Manual` into `AdvanceSequenceAsync`. Switching sources changes neither the video/list identity nor its generation, so `DecideResume` accepts the old record. The advance is then recomputed against the collection: pressing playlist Retry unexpectedly starts the collection's next video, rather than finishing the requested playlist step or merely loading rows.

Suggested fix: cancel the saved advance when the user changes continuation source, or verify and execute its saved source/row before resuming. Also guard source switching while an advance is pending, or explicitly invalidate that advance, so a page response cannot redirect an in-flight step. Keep ordinary directory browsing available.
