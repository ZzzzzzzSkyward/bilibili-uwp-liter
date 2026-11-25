# FA2 — pending old segment suppresses the current interaction node load

Supported actual-source counterexample, P2 playback ownership/correctness. Frozen PlayerControl SHA256 `8682d1505752a2dfd99e835259d45429cf78ee3e358caf39553b1375a7d4a3cc`.

Actual route: `DanmuTimer_Tick:1638` requests the next segment through `LoadDanmaku`; while that request is pending, the user selects an interaction node through `NodeList_SelectionChanged` → `ChangedNode:2155`. The new implementation advances `_playItemGeneration`/invalidates tickets and changes cid, but leaves `loadingDanmaku`, `danmakuLoadedSegment` and `danmakuPool` owned by the former node. After GetNodes completes, `SetDanmaku` → `LoadDanmaku(1)` sees the old `loadingDanmaku=true` and returns at2520 without issuing any current-cid request or clearing the old pool. Its finally resets the flag, but cannot restore the skipped load. Old request completion is correctly rejected by the new generation guard; its finally cannot release the current generation. The node's initial segment remains marked loaded from the previous node. The normal timer checks `danmakuLoadedSegment.Contains(segIndex)` and therefore does not retry current segment1, and can render the old pool (same PlayInfo reference).

Bounded proof compiles complete actual ChangedNode/SetDanmaku/LoadDanmaku/SetQuality and the real interaction VM; UI/HTTP/media boundaries are explicit doubles. Seeded list/pool represent an already watched node. Hold old cid1 segment2, complete the actual node metadata request to cid2002, then release the old segment. Exact output:

```text
NODE after transition cid=2002 requests=1:2 poolHasOldCue=True loadedSegment1=True
NODE after old completion requests=1:2 poolHasOldCue=True poolHasStaleResponse=False loadedSegment1=True
```

The old response is properly rejected (negative control), yet no cid2002 request exists and the previous-node cue/list remain. This is an accepted-work gap, not a memory-root or Windows timing claim. The global runner is not involved.

Required amendment: transfer/reset node-owned danmaku loading/segment/pool state when advancing the node operation, so the new node can load segment1 while obsolete requests remain pending and their finalizers cannot clear the replacement's lock. Preserve the existing current-generation guard and live/manual segmented updates. Add a real-method pending-old-segment→node-change case with distinct cid/cue payloads, a current-node success control, and old success/fault completion controls. Do not make the fix a synthetic SetDanmaku stub.

Commands: `python3 /workspace/work/video-memory/fresh-audit/danmaku-proof.py`, then `/workspace/work/maintenance-foundation/quality/dotnet/dotnet run --project /workspace/work/video-memory/fresh-audit/interaction-proof/Proof.csproj` (both exit0 confirming the counterexample). Full orchestrator: `python3 /workspace/work/video-memory/fresh-audit/run-new-controls.py`.

Private raw node log SHA256 `fc88f04bf68c0b47e4e5315f8f0c2ab2c4f7de800a145036bf04d43a7063ad99`. Actual DanmuTimer_Tick is read as the real producer/consumer; the fixture runs the complete load/node chain, not a native dispatcher timer or decoded video. All gates settle, and all modeled timers are stopped.
