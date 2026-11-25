# G1 implementation review evidence

Status: **PASS / FROZEN / RELEASED**. Decision: [final-review.md](final-review.md). Reviewer scratch: `/workspace/work/video-memory/graphics-final-review/`. Existing requirements/API evidence remains [review-evidence.md](review-evidence.md); frozen execution inputs are under `../resources/implementation-evidence/`, with `../resources/execution.md`.

## Exact bindings and preservation

| Input | SHA256 |
| --- | --- |
| Accepted requirements.md | `fe8c2514db39a06181a8ea7aeafa2223239cb394918c757ca9828eecc7847263` |
| Accepted design review.md | `26b6fdaf2ee911cb50a48a4c24730dc1d292c38b9ea3543717a55b92ec934253` |
| Original full PlayerControl | `18fac7cc1afcdf38f5896364f1606dd05446358965f3aae51cff00a7929b1e4f` |
| Final full PlayerControl | `8682d1505752a2dfd99e835259d45429cf78ee3e358caf39553b1375a7d4a3cc` |
| Original extracted complete renderer | `948c9ff7b27e6f8f35b4782dc472d1296ed57560dd3ab74c1b149ee0e4c2a71a` |
| Final extracted complete renderer | `5789d03300d297cb7bc6115ee0622417b5773960544b3b0899ec2bb8167fbcb7` |
| Original ActualScoped.g.cs wrapper | `f56764942a319e7500f9648218502fe91e58dd1c8160dfce0d94361b1fbb70c4` |
| Final ActualScoped.g.cs wrapper | `0795d31aca6923411e8e0bd76bec38b8b2d495c9e8068a1aa64a2be13fc45277` |
| SubtitleGraphicsProbe.cs | `a6fb7a1dc157691ead5cc147d336e3b695c821b7ef5bbf134091b4d223580764` |
| SubtitleGraphicsStubs.cs | `47d6260a14bad0dec2ce8fe4f16342b22734a46f392ca8f9f3d096126d1df754` |
| SubtitleGraphicsBaseline.cs | `d7844c564cba44289294913062eacd09ee54512fdc33bbe9533a079e797e0bb7` |
| extract-player-lifetime.py | `ff94a2f3aa1dcaf2d1851f644b32fdde2a1f49314cdf0cad404253a2fa360b98` |
| Independent original probe.log | `4b7183ee577d267222d3993fd60ae2026a155966a3a95d8d88077ca078b6fb2b` |
| Independent final probe.log | `09b7b9fa83e227ddc15f66cd91a7d7ac6c448eef4559634afcd1962a8b24fc4d` |

The implementation extractor hashes the signature-starting body through the complete closing brace, excluding the first line's indentation and trailing newline. This differs from the earlier discovery convention (`8775d4…`) without changing the original renderer's content. `audit.py` validates this directly against the same baseline Git blob `7356b94893377558191b0d70576a52bae704c1f9` and exact renderer copies.

`audit-bindings.json` records checks for all released changed/test/dependency inputs and exact SDK/compiler/runtime/ref hashes, every execution evidence file, generated-method/output equality and historical preservation. Fresh extractor output has the same method entries and generated hashes as the release for both source versions. Complete original/final method bodies are saved as `original-actual-method.cs.txt` and `final-actual-method.cs.txt`.

The final renderer's noncomment lexical tokens equal the accepted private `ActualScoped.cs` prototype. Both callers' entire method bodies compare byte-exact to baseline. Program.cs compares byte-exact after removal of only its additive lifetime/graphics dispatch block. HistoryCheckpointProbe.cs, HistoryStorageProbe.cs, HistoryStorageStubs.cs, aggregate run-all.sh and the full UWP csproj compare byte-exact to the same baseline. This verifies original assertions/inventory/dependency preservation without treating unrelated concurrent owners' files as a stable whole tree.

The executor's original/final owning-stage log hashes independently match `behavior-summary.json`: original `2e75634dbe13ea66cf3ac7b988635423f4c3a38a7cf772548c2e97f9fc4e6da3`, final `dea896bc251de6e2442e0cb553070dec25feef94348c03c82e89a50e576e5219`. Original logs fail desired-safe behavior after execution; final logs show241 aggregate checks passed. These are inspected executor receipts, separate from the reviewer's graphics-only execution.

## Source review and API boundary

Actual renderer locations: PlayerControl:2038–2118; format scope/setters2060–2068; target/layout/geometry2073–2079; drawing/style2085–2092; stream/awaits2095–2099; output2100–2108; style finally2110–2113. The actual complete methods and scoped diff were inspected, not merely string assertions about Dispose presence.

Format property setters occur after its scoped constructor returns; a failed setter cannot lose the acquired format. The style assignment follows Clear and precedes its DashStyle setter, so no successful acquisition is stranded by that setter. A factory/constructor which never returns does not acquire a value for the caller's cleanup. Enclosing scopes clean up earlier successful acquisitions if a later acquisition/render fails. Ordinary C# using/finally constructs remain compatible with the unchanged UWP project.

The owned-resource dependency sequence and async boundaries are unchanged apart from deterministic release. Style lives past the drawing session; target and stream live through awaited save; stream lives through awaited source load; all5 new owned scopes conservatively enclose output assembly. Existing stream/drawing scopes close before outer cleanup. Returned BitmapImage source data is not the CanvasRenderTarget object, and borrowed shared-device ownership is unchanged. Both callers still apply the original generation/subtitle-reference checks before publication.

Previously verified actual package WinMD hash remains `28e0741c97829b1dd71e9531d5172c2d8de6ff3375040a4c0303a3e01dcb7cc2`. Its raw IClosable/projected Dispose evidence covers format, style, layout, geometry and inherited render-target close. Exact upstream commit `36026efcb3fcd07af1d7a919fdf4201a07f05a20` documents shared-device borrowing and drawing close before SaveAsync; native Close implementations release their own device references, and ResourceWrapper's destructor also cleans up. Official BitmapSource.SetSourceAsync documentation supports closing the input stream after awaited loading while retaining the BitmapImage. The UWP dependency graph is unchanged; actual Windows resolution is still unobserved.

## Independent identical-assertion red→green

The delivered graphics assertions/stubs/reference were copied **unchanged** to `shared/`. Each mode compiles the fresh complete actual renderer extraction, those same shared files and a common one-line entry calling `GraphicsLifetimeProbe.RunCases()`. The two isolated project files are byte-identical, SHA256 `13ef81f3682ab42a9cef2bb5c121c26b93d3d09ebd81502b36f8af4c9788190e`. No private prototype, source rewrite, caller emulation or copied rendering algorithm is compiled as the changed method. `SubtitleGraphicsBaseline.cs` is the exact original actual method and serves as the successful-operation reference.

| Unchanged assertion group | Actual original | Actual final |
| --- | --- | --- |
|50 renders and retained/read returned outputs | completes50, outputs50, device closes0;50 undisposed per owned type; desired-clean FAIL | completes50, outputs50, device closes0;0 undisposed each; PASS |
|Pending save/source and disposal order | awaits/order execute, missing acquired cleanup FAIL | stream/all5 open at both pending gates; drawing already closed; return awaits source; reverse cleanup/count PASS |
|23 faults | all23 execute;77 accumulated unclosed model resources; cleanup FAIL | all23 execute;0 unclosed; exactly-once acquired cleanup and original exception-reference PASS |
|2 suspended completion faults | both genuinely suspend; missing owned cleanup FAIL | both suspend with stream/all5 open; original exception-reference and final cleanup PASS |
|50 baseline/final operation/parameter/output traces | all50 comparisons execute; absent owned cleanup FAIL | all50 operation/checkpoint/parameter/output-contract comparisons plus cleanup PASS |
|Process exit |1 after all groups; no compilation failure |0, all5 groups PASS |

The23 points cover color conversion, format construction/size/alignment/bold, measurement, target/layout/geometry/drawing/style acquisition, style setter, Clear/stroke/fill, drawing-close error, stream construction, save start/completion, source start/completion and output width/child insertion. Resource registration occurs after the injected constructor entry fault, so only returned acquisitions must close. Fault assertions compare the thrown object by `ReferenceEquals` with the injected instance.

`Clean` verifies reverse acquisition trace for all acquired owned types, exactly one release of every registered resource, and zero shared-device releases plus a later independent device consumer. Successful/faulted methods run before those checks. `CheckCleanup` accumulates failures only so the baseline can finish all cases; `RunCases` compares each stage's before/after count and reports FAIL when any were accumulated. Final has no such failures. The50 success outputs remain retained and are reread after all calls; output Width/Stretch/source/Tag/child structure are checked.

Trace comparisons preserve padding/newline normalization; measure/font/weight/alignment; target/layout dimensions and DPI; transparent Clear; border color/4f/Solid; fill color; PNG/1.0; source load; and output graph contract. The sequence varies font18–24,96/144DPI, all3 alignments, regular/bold and CJK/multiline/Latin text. Model parameter equality is not pixel/encoding/font-shaping equality.

The exact extraction guard was also tested independently with a missing renderer signature and a duplicated complete renderer. Both fail with exit1 and `expected exactly one anchor`, before compilation. Logs are `missing-extractor.log` and `ambiguous-extractor.log`. Scoped `git diff --check -- src/BiliLite.UWP/Controls/PlayerControl.xaml.cs plan/tests/playersess` exits0.

## Commands and scope

```bash
# cwd: /workspace/bilibili-uwp-liter; all outputs in reviewer scratch
python3 plan/tests/playersess/extract-player-lifetime.py --source /workspace/work/video-memory/player-fix/original/src/BiliLite.UWP/Controls/PlayerControl.xaml.cs --out /workspace/work/video-memory/graphics-final-review/original-extraction
python3 plan/tests/playersess/extract-player-lifetime.py --source /workspace/bilibili-uwp-liter/src/BiliLite.UWP/Controls/PlayerControl.xaml.cs --out /workspace/work/video-memory/graphics-final-review/final-extraction
python3 /workspace/work/video-memory/graphics-final-review/audit.py
# Run in each of graphics-final-review/original and graphics-final-review/final:
/workspace/work/maintenance-foundation/quality/dotnet/dotnet restore Graphics.csproj --configfile NuGet.Config
/workspace/work/maintenance-foundation/quality/dotnet/dotnet run --project Graphics.csproj --no-restore
```

Both restores exit0. Original run exits1, final run exits0. Logs, exact projects and generated bodies remain in their mode directories; `comparison.json` binds output and source/project hashes. SDK8.0.427, runtime/ref8.0.33 and compiler bytes match the executor's reviewed receipts. No full owning stage or aggregate rerun was performed by this reviewer; the separate original240/241 receipts are hash-verified and source preservation is checked directly.

Fixture resource registries intentionally retain model objects for disposal inspection; counters are not managed survivor or native allocation statistics. Measurement/drawing/PNG/source/XAML boundaries are doubles. The drawing Dispose fault marks its own state closed before throwing, so native disposal failure before cleanup and exception masking by a failing native cleanup remain unverified, as required. There is no new GC/cache policy, production collection or native leak claim.

The released capabilities receipt records Linux, absent Windows PowerShell/MSBuild/native UWP substrate, missing UWP assets/lockfile, and the failed-to-launch Windows build command. Resolved Win2D assets, Windows compilation/final-package rendering, pixels/native RAM/GPU/private commit and the warmed subtitles-off/on50-cycle/idle protocol remain FAILED/BLOCKED. Root's common stable gate and later publication remain separate. Reviewer changed no product/test/dependency/tooling/main-doc/index/Git-ref input.
