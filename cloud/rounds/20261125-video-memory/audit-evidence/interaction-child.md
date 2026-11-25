# Fresh audit finding FA1 — bound interaction child survives terminal close

Status: supported residual source counterexample; no native RAM/root attribution. This is a read-only audit of frozen PlayerControl `8682d150…`, not a source patch.

Actual `GetPlayerInfo` creates `InteractionVideoVM`, binds both `NodeList.DataContext` and `InteractionChoices.DataContext`, then awaits `requestedInteraction.GetNodes()` (PlayerControl:2799–2804). Actual `InteractionVideoVM.GetNodes` awaits transport/decode, then assigns `Info`, `List`, `Select` and finally `Loading` without ownership invalidation (InteractionVideoVM:59–92). Actual terminal `Dispose` invalidates parent work but does not invalidate/detach this child (PlayerControl:4184–4225). The parent guard after the child await rejects the online/title suffix only after child publication already happened.

The normal supported entry is InitializePlayInfo→SetPlayItem→GetPlayerInfo for an interactive video; close the tab while the initial node response is pending, then allow it to complete. Exact real `GetPlayerInfo`/`Dispose` and the full unmodified real InteractionVideoVM/IModules were compiled in private scratch. Three close repetitions each report `childBoundBeforeCompletion=True childBoundAfter=True childLoaded=True lateNotifications=Info,List,Select,Loading onlineCalls=0`. The active control publishes normally (`onlineCalls=1`); actual same-reference/other item replacement detaches the child and rejects its suffix, establishing a meaningful boundary control. The fixture does not infer that a bound finite child is an immortal global root.

The owning fixture currently substitutes `InteractionVideoVM.GetNodes` with `NextGate?.Task ?? Task.CompletedTask` and `Select = new Element()` (PlayerLifetimeStubs:177–183), so its continuation controls cannot observe these real child notifications. No product flag reset/copy of the algorithm is used in this proof. Existing stub boundary is useful for caller work but insufficient for this child-publication claim.

Proposed requirement amendment: terminal close must revoke the interaction child's view ownership before any close callbacks/await. Detach both bound DataContexts and the old child/item identity (or use a separately reviewed child invalidation boundary); finite transport completion may remain finite and publish only to a detached old VM if that is the accepted policy. Add a complete actual child method/full VM close-pending test plus active and live-replacement controls. Preserve live node navigation and same-reference replacement. A generation-aware child is necessary if a supported concurrent same-VM node operation is also included; this audit has not yet used that broader path for its verdict.

Reproduction command:

```bash
python3 /workspace/work/video-memory/fresh-audit/build-proof.py
/workspace/work/maintenance-foundation/quality/dotnet/dotnet run --project /workspace/work/video-memory/fresh-audit/interaction-proof/Proof.csproj
```

Both exit0; the last line confirms the counterexample plus controls. The private source scaffolding is derived from the final owning fixture and changes only the interaction child to the full actual product class, transport/UI type boundaries, graph-version stub type, namespace import and private test entry. No existing suite rerun or owner edit.

Exact source input SHA256:

- `src/BiliLite.UWP/Controls/PlayerControl.xaml.cs`: `8682d1505752a2dfd99e835259d45429cf78ee3e358caf39553b1375a7d4a3cc`
- `src/BiliLite.UWP/Modules/Player/InteractionVideoVM.cs`: `9d58001e73b2cc000213d1428fca4262b3748aa7ac8d7a2430a6aa66289f8569`
- `src/BiliLite.UWP/Modules/IModules.cs`: `cef5334b540d7e74513d59b513e3d7671fce98e9be62b7d50bad471df14ed3c6`
- `plan/tests/playersess/PlayerLifetimeStubs.cs`: `a5ec7f174766507264e0e4ff369ebb504040d8bc5ac21a3de644270d8b9ededd`
- `plan/tests/playersess/extract-player-lifetime.py`: `ff94a2f3aa1dcaf2d1851f644b32fdde2a1f49314cdf0cad404253a2fa360b98`

Raw log SHA256: `83c1fa03f361777fa46220fce4b0952dd61b56fb7b7b4200cd8434f0cfac3447`. The driver/input/generated manifest and raw log remain under `/workspace/work/video-memory/fresh-audit/interaction-proof`.
