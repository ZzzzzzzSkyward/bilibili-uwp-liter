# G1 independent implementation review

**PASS — FROZEN / RELEASED.** Reviewer `memory_graphics_review`, `gpt-6.1-sol` / `xhigh`. No actionable P0/P1/P2 finding remains in the reviewed G1 implementation. This is scoped source/fixture acceptance; root still owns common integration, residual review and publication.

Bound final PlayerControl SHA256: `8682d1505752a2dfd99e835259d45429cf78ee3e358caf39553b1375a7d4a3cc`. The complete actual renderer is `GenerateSubtitleItem` at2038–2118; extraction-method SHA256 is `5789d03300d297cb7bc6115ee0622417b5773960544b3b0899ec2bb8167fbcb7`. Exact inputs, independent red→green commands/results and limits are in [implementation-review-evidence.md](implementation-review-evidence.md).

## Source and ownership

The final method implements the accepted G1 scope. Format acquisition precedes its setters inside the new using scope. Target, layout and geometry use nested scopes at the original allocation points. The nullable style local is assigned only after Clear, before its DashStyle setter; its enclosing finally runs after drawing, save/load and output work. This covers successful acquisition followed by setter failure without hoisting style creation or changing successful render operations.

Existing drawing-session close → awaited PNG save → awaited BitmapImage source load → stream close → output assembly is preserved. On completion the owned temporaries release in style→geometry→layout→target→format order. The shared device remains borrowed/open, and the returned Grid/Image/BitmapImage is neither closed nor cleared. All five owned scopes conservatively outlive both awaits and output assembly. No early-disposal assumption, catch/swallowed error, renderer algorithm, dependency, GC/cache policy or image-ownership change appears in G1.

The final method's noncomment lexical tokens match the accepted private scoped prototype. Both actual caller methods (`SubtitleTimer_Tick`, `UpdateSubtitle`) are byte-identical to the original method bodies. Their existing generation/reference publication checks remain intact. G1 is confined to the renderer; R1/R2/R3/RR1 behavior in the same full source has a separate resource reviewer.

The prior API evidence remains applicable: actual Win2D1.26.0 UAP WinMD/projected documentation supports close for format/layout/geometry/style and inherited CanvasBitmap close for the render target. NSDanmaku establishes a transitive lower bound, not an observed Windows resolved version. The UWP project/dependency bytes are unchanged. Shared-device and returned-image contracts were preserved, including the documented drawing close and awaited stream-consumption ordering.

## Independent unchanged-fixture proof

The reviewer extracted the complete renderer afresh from original and final actual source, verified exact method equality against generated output and release bindings, then copied the delivered three graphics fixture/stub/reference files unchanged into exclusive scratch. Identical isolated projects, entry point, SDK/runtime and fixture bytes ran against each source method:

- Original source: exit1 after50 complete renders,50 undisposed objects of **each** of the five owned types,50 retained/read output graphs and0 device closes. All five graphics groups report desired-safe failure; this is a behavioral failure, not compilation failure.
- Final source: exit0, all five graphics groups PASS,0 undisposed objects of each type, exactly-once reverse acquired cleanup,50 returned graphs usable after cleanup and0 shared-device closes.
- Pending save/source gates keep stream/all5 temporaries open and output pending, with drawing already closed before save. Identical operation/parameter/output-contract records match the complete original reference over50 varied cue/setting inputs.
- All23 acquisition/setter/measurement/drawing/stream/save/load/output fault points and both genuinely suspended save/source completion failures execute. Final cleanup passes and `ReferenceEquals` verifies the original injected exception object. Constructor/factory faults are distinguished from acquired-object setter failures.

Delivered fixture source, extractor and project wiring were inspected. The baseline reference is the exact original full renderer. Missing and ambiguous renderer anchors reject independently with exit1. Model resource cleanup failures are accumulated and each graphics group fails when its cleanup receipt is nonempty; the passing final receipt does not hide those failures.

The original Program.cs is byte-exact after removing only additive dispatch statements and their blank line. All historical history fixtures remain byte-exact, preserving the original240 assertions. The executor's hash-bound full owning-stage log passes241 aggregate checks (240 existing plus1 graphics dispatch); this reviewer reran only the focused graphics slice. `plan/tests/run-all.sh` is byte-identical to the baseline: no aggregate stage/inventory change was introduced, and51 logical/52 executable stages remain the common gate. Scoped diff whitespace check passes. No full suite was rerun here.

## Limits and release

G1 remains E1/P2 deterministic ownership hardening. Registry/counter evidence is not native retention, working set, private commit, RAM/GPU reduction or pixel equivalence. Upstream destructor cleanup and WinRT wrapper/finalizer/cache/driver timing remain unmeasured. The drawing-close fault double closes its own model state before throwing; native Close/Dispose failure before release, cleanup errors masking another error and R4 are not asserted solved.

Actual Windows UWP build/resolved Win2D assets, final-package rendering checks and the accepted warmed50-cycle subtitles-off/on/idle native memory/GPU protocol remain FAILED/BLOCKED on Linux. BR-21 attribution remains open to those measurements. This PASS does not replace them.

Reviewer writes: only this file, `implementation-review-evidence.md`, and private `/workspace/work/video-memory/graphics-final-review/`. Product/tests/dependencies/tooling/main docs/index/Git refs were not edited; disjoint tooling activity is outside this frozen component claim. Released for the root's next coordinated review/integration step.
