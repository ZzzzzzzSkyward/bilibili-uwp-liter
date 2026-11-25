# Player implementation review — CHANGES

**CHANGES — PF1/P1 must be repaired. No P0 or additional P2 implementation finding.** Reviewed frozen PlayerControl SHA256 `8682d1505752a2dfd99e835259d45429cf78ee3e358caf39553b1375a7d4a3cc` and the ten released source/test paths. Review custody is released to root for a precise repair grant to the same PlayerControl writer. Requirements remain accepted; no source, test, tooling, main-document, index or Git-ref edit was made by this reviewer.

## PF1 — P1: replacement inherits disabled local-track controls permanently

**Source:** `PlayerControl.xaml.cs:1724` and `:2309` (`SetPlayItem` / `SwitchLocalTrackAsync` finally). Start of the track switch disables LocalVideoTrackSelector, LocalAudioTrackSelector and BottomBtnNext at2241–2243. Replacement advances `_playItemGeneration` and clears `_localTrackSwitching`, but does not restore those controls. The new generation check in the old switch's finally now correctly prevents its writes. Consequently **no owner restores enabled state** after a replacement when B has no track switch of its own. Actual UpdateLocalTrackSelectors only assigns options/selections and never restores IsEnabled.

**Concrete reproduction:** on one live control, start a manifest audio-track switch with the engine open pending; initialize a distinct manifest item B through complete actual InitializePlayInfo→SetPlayItem while leaving the old open pending; finish the old open with failure. Both new track selectors stay disabled after B's successful open and after old work completely settles. The control's Next button also remains disabled in this source path. The dropped old work no longer rolls back B, but the user cannot select B's local tracks or use that still-disabled Next button.

Independent targeted fixture executes the complete original/final control/callback/coordinator methods and the complete unchanged actual UpdateLocalTrackSelectors. It does not reset a product flag or implement a replacement UI policy in a stub. Same assertions:

```text
original: track-start ui=False,False,False
original: replacement-ready ui=False,False,False opens=2
original: old-switch-settled ui=True,True,True opens=2       [exit0]
final:    track-start ui=False,False,False
final:    replacement-ready ui=False,False,False opens=2
final:    old-switch-settled ui=False,False,False opens=2    [exit1]
ASSERT_REPLACEMENT_TRACK_UI_ENABLED_FAILED
```

Commands/boundaries/hashes/logs: `/workspace/work/video-memory/player-final-review/track-ui/{commands.json,binding.json,original.log,final.log}`. This is a genuine source regression, not Windows/native inference. The released TrackClose/TrackSharedModel tests only assert that obsolete finally produces no UI writes; they omit B's enabled-state positive control, so all39 existing groups can PASS despite this regression.

**Required correction:** make the accepted replacement operation own the selector/Next enabled state, restoring the normal usable state when it supersedes an old switch. Keep old completion/finally unable to re-enable or otherwise mutate B while B's own switch is pending. Do not remove the generation guard or resume obsolete rollback; do not enable controls on terminal Dispose. Respect any legitimate current transition/eligibility state rather than unconditionally enabling everything from a stale callback.

**Acceptance:** add an unchanged-baseline/final actual-method regression covering distinct and same-reference manifest replacement while A's switch is pending. With no B switch, both selectors return to their normal enabled state after B is ready and remain usable after A succeeds/fails/faults/cancels. With B's own switch pending, A's late completion leaves B's disabled controls/flag intact; B's completion alone restores them. Keep shared LocalPlayInfo model protection, stale no-open/no-error/no-publication, close controls, all original240 checks and all39 lifetime groups. Bind new final bytes and repeat the directly affected owning/local-media checks only as justified; root retains the later common integration gate.

## Verified implementation and evidence outside PF1

- Independently validated all ten released code/test hashes, declared fixture/dependency/tool/history hashes, exact original source snapshot and original Program preservation after removing only the added dispatch lines. All original240 assertions and existing three history fixture files remain intact. No new aggregate stage or product helper/project/dependency change was introduced.
- Replayed the unchanged released owning stage in an isolated private snapshot. Original source exits1 with25/39 player groups failing through intended assertions; final source exits0 with39/39 and241 aggregate checks (original240 plus graphics dispatch). Both extraction manifests independently match complete actual source bodies: original38/final39 methods; the only coordinator substitution is its fixture parameter type. This is source behavior, not a full UWP build.
- The monotonic Dispose latch precedes teardown; closed public Initialize/Loaded/manual/import/engine/opened paths reject; Unloaded still detaches. Live unload/reload and fresh owner with identical content work through complete actual public/quality/info/subtitle chains. New generations reject stale info/quality and detach an old interaction VM on item replacement, including same-reference replacement. Captured item/generation plus requested interaction VM checks occur after the relevant info/node/online awaits. Existing session Begin/Hold/Accept/Abandon ownership semantics remain unchanged.
- Picker generations are captured before selection; late selected/canceled results cannot import into B. Local coordinator callbacks suppress obsolete failure/fault rollback against a shared mutable LocalPlayInfo while current failures retain the existing rollback path. PF1 concerns the associated current UI ownership, not the need to preserve those stale-work checks.
- All three actual settings loaders still run refresh assignments, register their handlers once, suppress refresh-triggered persistence/toasts, and preserve normal user handler bodies. Final fixture runs50 loads with external persisted values changing and validates all-three refresh, handler counts and one subsequent subtitle/volume/danmaku persistence effect. Ratio/rate/autoplay and history save/timer/drain bodies retain their source behavior. Test-only GC/root models remain confined to reachability assertions; no production registry or forced GC was added.
- The recorded ten-stage selection receipt was hash-checked:10/10 PASS, `PARTIAL_PASS`, `complete_required=false`; it was not rerun. The independent owning run includes shared graphics controls, but G1 API/ownership/native limitations and its final verdict remain with the separate graphics reviewer. No graphics source change is requested by PF1.

## Scope and limits

Read actual residual producer/consumer paths and child VM source, public callers/cache preconditions and the final test files; no whole-tree stability is asserted while disjoint tooling work continues. This review does not extend the earlier Video initial-prefix proof into universal caller safety. Separate residual audit remains required by dispatch. Season's own no-late model/title/review/attempted-player/comment/QR acceptance is not replaced by the lower-level player guard.

R1/R2 source holes and R3 owned amplification show meaningful improvement, but PF1 prevents implementation PASS. R4 teardown-fault isolation, native/adaptive/local-stream/display/SMTC policy and new graphics discovery beyond accepted G1 remain separately gated. Windows UWP compile/device/playback/cache/decoder/graphics/pixels/RAM/GPU/handles remain unavailable/unverified. Neither the successful source fixture nor the modeled timers/resources establishes50 actual watched videos, native retention or memory savings. No full suite, publication or native leak-fix claim is made.
