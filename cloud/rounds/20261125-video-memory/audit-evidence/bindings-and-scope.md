# Audit bindings, controls and retained boundaries

Read-only audit of HEAD7356b94893377558191b0d70576a52bae704c1f9, frozen player/season source and19 combined source/fixture paths. Tooling T1–T3 was concurrently owned and deliberately excluded from stability/testing claims. No owner byte/index/ref/package/settings/main-document changes or unchanged aggregate rerun.

## Exact scope binding

All19 before/after SHA256 comparisons pass. Player10 paths match `resources/implementation-evidence/bindings.json`; season2 product+7 fixture hashes match `managed/implementation-evidence/bindings.md`.

- `src/BiliLite.UWP/Controls/PlayerControl.xaml.cs`: `8682d1505752a2dfd99e835259d45429cf78ee3e358caf39553b1375a7d4a3cc`.
- `src/BiliLite.UWP/Modules/Detail/SeasonDetailVM.cs`: `615a4c04bb0f4077ff63976e993f013a455b6fcf55a77fc798cef0a9d5acc596`.
- `src/BiliLite.UWP/Pages/SeasonDetailPage.xaml.cs`: `c91ceea2e58aa8318311841eaf22ef473711710c6222289baa6bed9b79bf4f5d`.

The original Program.cs files are independently byte-identical after removing only the known player dispatch/additive season return (UTF8 BOM normalized). Thus all original240 player and141 season assertions remain. The three unchanged player history fixture hashes match owning receipts. No fixture resets `_disposed`/`_isClosed` to make a terminal owner reusable; fresh positive owners are new objects. Product lifetime/settings flags are extracted from the real source. Track coordinator is the complete actual method with only its documented fixture parameter-type substitution.

## Independent extraction check

Compiled a package-free private Roslyn syntax-span validator. It independently parses both actual player source roots and both actual season source roots, then compares every manifest method's entire declaration span/hash to the owning extraction. Result: original38 + fixed39 =77 complete player/graphics/coordinator methods match;18 complete season/page/shell method/class spans match on each side (36 total). The page field slice is separately receipt-bound. There is no truncation at an inner brace in these actual inputs. Original/final source are not replaced by reconstructed implementations.

Command: `/workspace/work/maintenance-foundation/quality/dotnet/dotnet run --project /workspace/work/video-memory/fresh-audit/extraction-check/Check.csproj`, exit0. This is a new bounded fidelity check, not a repeated owning/full suite.

The owning missing/duplicate-anchor controls bind the same extractor bytes and fail with exit1; source red is behavioral, not a missing-source/compiler error. Reviewed existing raw logs: player original fails25 of39 lifetime groups/all five graphics groups and final passes39/five with241 aggregate checks (original240 plus graphics dispatch). Season original141 PASS/additive297 failures, fixed141 PASS/additive497 PASS. These unchanged valid executions were reused, not rerun. Selected reports remain PARTIAL_PASS, complete_required=false.

## New counterexample/control commands

`python3 /workspace/work/video-memory/fresh-audit/run-new-controls.py`, exit0. Three bounded probes intentionally confirm unsafe actual-source outcomes plus control observations; zero native memory statistics are collected. They are discovery counterexamples, not desired-safe acceptance tests. Their later owning regressions must assert safe results and genuinely fail on this source.

- `build-proof.py`: setup0/proof0; driver SHA256 `e773fdc90ec452599cb8f13f8d567833c466acdd41ab019f6dfcaf3a7f6d568f`; log `/workspace/work/video-memory/fresh-audit/interaction-proof/run.log`, SHA256 `83c1fa03f361777fa46220fce4b0952dd61b56fb7b7b4200cd8434f0cfac3447`.
- `danmaku-proof.py`: setup0/proof0; driver SHA256 `b50715bd31a84565c5af9f6b013a1f88881d778244d28cdd52ed434e40d84fd6`; log `/workspace/work/video-memory/fresh-audit/interaction-proof/node-run.log`, SHA256 `fc88f04bf68c0b47e4e5315f8f0c2ab2c4f7de800a145036bf04d43a7063ad99`.
- `ended-proof.py`: setup0/proof0; driver SHA256 `6f7ae8f82c4f5c64c57bf006859f8f8fc89790fcf4e1c20f37039eb2e269e5f0`; log `/workspace/work/video-memory/fresh-audit/ended-proof/run.log`, SHA256 `4b53cfdd2e70ec0e93e55ac4ec0b25cdcd18e821707ee58958be2687d17bb873`.

FA1 compiles the full unmodified InteractionVideoVM and IModules, replacing the owner's deliberately inert interaction boundary with the real child implementation; transport and Windows brush/color are boundary doubles. FA2 executes the real changed-node/load chain with a representational pre-existing segment/pool, distinct cid data, an actually pending old request and settled completion; no private terminal flag is changed. FA3 inserts the complete original ended-handler method instead of the inert fixture stub; actual public replacement/close are executed around a delayed report boundary. All gates settle and modeled timers are stopped.

## Scope conclusions beyond findings

- Terminal/live/fresh public control ownership and generation-before-producer/after-await placement prevent original R1/R2 ticket/timer resurrection. Same-reference SetPlayItem detaches interaction VM correctly. Manual subtitle/manifest guards, picker identity captured before picker await, local source fallback session checks and local shared-model obsolete-failure successful-no-op coordinator result preserve newer owners. Current local failures still roll back; stale finally cannot re-enable a replacement's flags.
- Three settings loaders still refresh values every load, register one logical anonymous handler per loader and suppress persistence/effects during programmatic refresh. Live ratio/rate/volume/lane/subtitle refresh remain present. Internal settings-control event cycles alone are not external roots.
- Season full VM keeps one binding identity, localizes mapping until accepted and rejects obsolete metadata/decode/episode success/fault/finally. Its notification-to-notification ownership checks stop the demonstrated reentrant VM invalidation/close. Page terminal entry/navigation/Init checks and active error boundary are coherent. Actual title handlers/MediaID setter are synchronous ordinary writes; no new supported season reentrant suffix failure was found. Original fallback/order/last-good/empty/null assertions remain intact.
- G1's format→target→layout→geometry ownership, late style acquisition and style→geometry→layout→target→format release are valid for the inspected1.26.0 WinMD contract. Drawing closes before save; both awaited save/source consumers finish before stream/temp release. Returned Grid/Image/BitmapImage and borrowed shared device remain usable. Complete actual cue/settings callers reach the renderer during watched playback. Stub counters establish deterministic release/order and output contracts only. No pixels/native bytes/finalization/cache claim.
- MainPage ordinary tab open constructs a fresh MyFrame/page; actual close notifies frame then detaches/removes tab. No-tab duplicate types use a new frame; Back disables outgoing cache and clears/removes the outgoing route. Multiwindow constructs a fresh frame. Retained history/visual unload/minimize intentionally keep a live owner. Source cache policy does not certify unavailable native XAML reuse/delivery.
- Final synchronous position save and existing history drain/tail behavior remain required. FA3 revokes only the obsolete ended suffix; no history cancellation/storage rewrite is justified. `_activeInstance` is a bounded single slot, not50 accumulating entries. Normal Unloaded detaches CoreWindow/SMTC and stops focus; Dispose stops subtitle/history/danmaku. Finite task snapshots and current-video danmaku growth are not proved permanent roots.
- Preserve R4's conditional native-dispose-throw teardown abort classification, adaptive/local source/stream ownership uncertainty, display balance/SMTC background policy, optional diagnostic capture and scalar history/cache boundaries. Inspected NSDanmaku2.2.0 package/nuspec does not establish an independently rooted native renderer event/thread; no ClearAll/global destroy rewrite is prescribed. Exact package lower bounds remain distinct from an actual Windows restore.

## Structure/user-journey assessment

Product responsibilities are still in existing page/VM/control ownership boundaries, with private guards and no new global registry/dependency/stage. Fixture/extractor/graphics files have clear roles; existing monolithic PlayerControl predates this scope and is not grounds for an unmeasured rewrite. The three findings are ownership gaps within those boundaries, not a reason to rebuild the player architecture.

Read README, docs/FEATURES.md, ROADMAP1.1/1.4 and BR-21; traced actual tab/no-tab/multiwindow/play-directory/season/local public routes. Primary users need ordinary video/season/local playback with subtitles/danmaku, quality/track changes, continuation, history, error retry, and close/reopen while minimized/background/history views remain intentional. Current repairs address interrupted closes and deterministic per-cue cleanup; FA2/FA3 can affect normal interaction/next playback, so the journey is not fully accepted yet. No separate ordinary-feature redesign is supported by this audit.

Linux source proofs do not decode fifty videos or identify Windows private-commit/GPU/WinRT/codec memory. Actual warmed50 same/distinct-video close versus minimize, subtitles/danmaku controls, heap roots and idle recovery remain BLOCKED/external per the accepted measurement protocol. A missing runtime does not negate available source findings.

Private `/workspace/work/video-memory/fresh-audit/before.json` SHA256 `f11bfc2350febdc2ed7b769381ce03e2b2409d404f50bfc529ff0983294b88c0`.

Private `/workspace/work/video-memory/fresh-audit/integrity.json` SHA256 `45dcfe1ef6c61b7ccb8b53a3ae47b915388f2b69e2686fe4ff560e8d5c615f75`.

Private `/workspace/work/video-memory/fresh-audit/new-control-results.json` SHA256 `1d9277a7c3cf6b0d82683622e78ec2eaf925ded2bd73984a842f4fe6dbdd4fe9`.

Private `/workspace/work/video-memory/fresh-audit/extraction-check/run.log` SHA256 `6ab3c53bf3a16cff751ff11331a01ed5db6971b72ac83fa7254728334fcad822`.
