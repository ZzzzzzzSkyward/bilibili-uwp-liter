# FA3 — old ended/history continuation advances the replacement video

Supported actual-source counterexample, P1 stale live-owner action. This adds concrete proof beyond the previously recorded generic native callback/state-mutation candidate. It does not alter history storage or infer native event roots.

`PlayerControl.Player_PlayMediaEnded:3927–4006` is invoked while itemA is current, then awaits `playerHelper.ReportHistory(CurrentPlayItem, Player.Duration):3942`. `PlayerVM.ReportHistory:61–87` really awaits an HTTP request. If the user selects another video/part while that report is pending, existing live `InitializePlayInfo`/SetPlayItem accepts B and advances `_playItemGeneration`. When A's report settles, the unguarded remainder reads B's CurrentPlayIndex/PlayInfos and emits AllMediaEndEvent, changes B's play index, or invokes Player.Play depending on B's play mode. This does not require an obsolete native publisher to emit an event: the event handler was current on entry and becomes obsolete at its own await.

Actual complete consumers: `VideoDetailPage.player_AllMediaEndEvent:1319–1323` calls `AdvanceSequenceAsync(false,false)`; its1385–1415 guards page close, detail-loading, pending transition and AUTO_NEXT, then computes the **current** sequence step. It receives no old generation/item identity. On a fully initialized still-open B with autoplay enabled, the stale end therefore advances B's current sequence. The page's closed guard prevents this particular caller from advancing a closed page, which is a useful boundary but does not protect live replacement. Single/list loop branches can directly call the player after close as well.

Private proof compiles the entire real Player_PlayMediaEnded body, plus complete real InitializePlayInfo/SetPlayItem/Dispose and the accepted real guard/session dependencies. The history boundary is delayed, the live replacement enters through the real public initializer, and all tasks settle. Exact output:

```text
END close=False replace=False callbackCount=1 observedCurrentTitle=Video
END close=False replace=True callbackCount=1 observedCurrentTitle=B
END close=True replace=False callbackCount=1 observedCurrentTitle=Video
```

The active control proves one normal end notification. Replacement proves the old end callback observes B. Close proves terminal completion is not quiet at the control boundary; no claim is made that the actual closed Video page bypasses its own guard. The owning lifetime fixture replaces Player_PlayMediaEnded with an empty stub, so its existing opened/engine tests do not cover this await boundary.

Required amendment: capture terminal/item/generation ownership before the history report, reject an obsolete suffix after the await and before any loop/play/index/AllMediaEnd effect. Keep the actual final history report/local synchronous save; do not await close indefinitely, cancel shared history, discard pending checkpoint tails, or redesign SMTC/native publisher ownership. Add complete-method active end, close-pending end, distinct and same-reference live replacement, report failure and relevant loop/next-mode controls. If native callbacks are broadened beyond this concrete source boundary, require separate proof.

Commands: `python3 /workspace/work/video-memory/fresh-audit/ended-proof.py`, then `/workspace/work/maintenance-foundation/quality/dotnet/dotnet run --project /workspace/work/video-memory/fresh-audit/ended-proof/Proof.csproj` (both exit0 confirming the counterexample). Full orchestrator: `python3 /workspace/work/video-memory/fresh-audit/run-new-controls.py`.

Raw log SHA256 `4b53cfdd2e70ec0e93e55ac4ec0b25cdcd18e821707ee58958be2687d17bb873`. Extracted method is verbatim from frozen full source; unchanged PlayerVM source SHA256 `7e0e0653c244759539ab8b0defebf0dd00eacb9758dd652913836096fc3aa3d6`. Network/media/UI behavior is doubled; actual Windows event scheduling/playback/private bytes are unmeasured.
