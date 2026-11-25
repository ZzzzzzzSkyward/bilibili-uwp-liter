# Stable combined gate evidence — FAIL / FROZEN / RELEASED

Authority: top ACTIVE activation in [final-verification-dispatch.md](../../final-verification-dispatch.md). Independent verifier `memory_integration_executor`,gpt-6.1-sol/high. All executable owners/reviewers were released; FI1/FD1 remains requirements-only. The actual full command in [execution.md](../execution.md) ran **once**,2026-11-25T04:18:21.451949+00:00→04:20:42.465212+00:00,141.026s. No selector/budget/inventory/assertion change or retry.

## Verdict and sole failure

Aggregate exit1; finished=true, suite_status=FAIL, complete_required=false. **50/51 logical,51/52 executable PASS**. Exact fixed52 rows/51 distinct logical IDs, all selected/available/required, fixed order/argv/paths; both AUTH01B rows PASS. Inventory/prerequisite errors are empty. Status counts: PASS51,FAIL1; all MISSING/BLOCKED/TIMEOUT/ERROR/CANCELLED/NOT_SELECTED/PENDING zero. Logs are untruncated.

`batch:ux1d`: command `bash plan/tests/ux1d/run.sh`, compiled and exited1 after54 `ok` assertions and one `FAIL`:

> source: the next button does not stay enabled-but-dead while pending

Frozen `plan/tests/ux1d/Program.cs:247–248` requires `show.Contains("BottomBtnNext.IsEnabled = false;")` inside actual ShowTransitionPending. The actual source now invokes UpdatePlaybackControlEligibility there. Its subsequent clear check at249–252 also requires an inline true assignment, while actual ClearTransitionPending invokes the common eligibility helper. The first failure stops that stage's remaining assertions. This is a supported integration failure requiring owner disposition; current source correctness is not inferred by deleting/relaxing the assertion. No source/test repair or extra selected/full run was made by this verifier.

Exact failed log `/workspace/work/video-memory/final-verification/combined-final/aggregate/logs/batch_ux1d.log`, SHA256 `3296315f77d40ab1471d05180ae70c36665d171e5ade7f7273c1c56300da91ce`. Actual failed row, counts and exit are retained in failure-summary.json/validation.json; exact Control and unchanged UX1D inputs are in source-snapshot/. Root must route this to the proper owner; combined source publication is blocked by this FAIL.

## Stable source, review and tool identity

All **935** available tracked/new non-cloud repository inputs were freshly NUL-inventoried and copied before launch; no tracked input was missing. source-before.json equals source-after.json and final recheck935/935. This supersedes the moving934-file preparation candidate. Includes all20 new runnable inputs, all product/test/tool/build/workflow/default Compile and explicit linked/runtime-read inputs, AF2 folder fixture/actual Task.Run helper extraction/c2 project/stub/run changes and current RF1 extractor. Compile/import inventory and runtime source-read lines are separately retained; no standalone extractor replay was used.

Truthful HEAD/tracking/dev `7356b94893377558191b0d70576a52bae704c1f9`; report dirty=true/verified=true. Direct Git blob-format comparison binds892 unchanged HEAD inputs,23 modified tracked inputs and20 new inputs. source-delta-from-head.json and owner-source-delta.json bind all43 changed/new runnable files: query5,favorite12,player12,season9,tooling5. All67 fresh final owner-byte checks match, including Controlb382,FavoriteDetailVMdaf13 and MyFollowVideoVM5007. Final owning/fresh/graphics/season/tooling review inputs match before/after. Index SHA, local refs and HEAD remain exact. This is an uncommitted local working tree receipt, not a published source SHA or source-publication authority.

All **5073** quality/dotnet and quality/cache/nuget files equal their before/after manifests and accepted recovery tools manifest. Actual runner resolves SDK8.0.427/runtime/ref8.0.33; Newtonsoft13.0.3 remains exact. All **732** PowerShell installation files also match current targeted-check receipt and before/after manifests. No manual cache replacement, dependency/source installation or Git mutation occurred.

Separate current [interpreter-checks](interpreter-checks.md) sampler46/21/106/55calls52rows and package15 artifacts were hash-validated and reused; no rerun. Runner14 actual controls receipt `427d42beffae5b83956bd731a96f7474740f3b916812dfefcaf2de171600448e`, all14PASS, unchanged three runner sources and exact quality5073 support the documented control reuse. No absent historic Python/OS/PowerShell baseline is invented or extended to every host library. No separate gate is added to52.

## Passing regressions and actual generated inputs

Raw per-stage logs/regressions.json retain query original12/search1258 plus3831; c2 original451/action563/AS3 original9/compatibility124/present-empty8/added1070; AF1 residual12/added406; AF2 original13/safe37/added573; c3117. Player passes original240 plus graphics dispatch=241,39 lifetime,five graphics,prior58 residual and8 actual-click/helper RF1 groups. Four absent50-click groups have zero exception/work/state deltas. Season passes original141/additive497, actual close-gate50+50 late effects zero. Recovery remains one stage/program348/failures0 with22 exact inputs/16 slices; g5335 passes. None of these replaces UX1D or changes the full gate status.

All1263 ignored generated/bin/obj outputs were manifested separately before/after; actual after bytes are copied in generated-snapshot/. A read-only observer and existing evidence-copy option retained **all nine actual ephemeral playersess files**, including `ActualHistoryCheckpoint.cs` SHA256 `e1b7e6459f7503c61c52aecdf8b4c7b531e44f2223f7bb61410a07f5d7002fe5`. Existing cleanup still ran. The current extraction manifest binds56 actual method/property bodies and current Control. Full graphics generated input remains `0795d31aca6923411e8e0bd76bec38b8b2d495c9e8068a1aa64a2be13fc45277`. C2 actual complete HttpResults/helper outputs match accepted hashes `6df7b868…`/`98c845e0…`; source body inclusion was checked without another extraction/run. Season/recovery actual generated sources and manifests are retained.

Of70 gRPC outputs,66 remain unchanged; only AssemblyInfo/header cache and obj/bin DLL change. The accepted prior actual header copy matches this run's before hash; replacing solely informational Git versione928bf0 with current7356b948 reproduces the after header bytes. Exact derivation proof is grpc-derived-explanation.json. Generated changes reflect build outputs, not source/tool drift or Windows compilation.

178 static source checks pass: Bash syntax, Python AST and XML product/probe project/XAML/manifest parsing; scoped git diff --check passes. Warning counts retained without suppression: CS0108=12,CS0067=16,CS8981=6,CS0414=2,CS1701=8,CS1998=4,CS0162=1,CS0168=1,CS4014=2. CS0414 observations are the season fixture's changedFlag and extracted page browsedDirectoryKind fields; these diagnostics do not establish a Windows defect or repaired inherited warning. All exact lines remain in warnings.json. Static checks do not certify XAML/binding/native execution.

## Immutable receipt inventory

Private root `/workspace/work/video-memory/final-verification/combined-final/` contains raw logs/reports, exact source/generated snapshots, command/environment overrides, owner/review/manifests, source/protected/head identity and failed-stage evidence. Hashes are SHA256:

| Artifact | SHA256 |
| --- | --- |
| aggregate/results.json | aee9bff87ec5fd891b40b6a89f4289d4e1b298a9f5d4504fe5f7b3be34426a17 |
| aggregate/summary.md | 2aa852b89d45fb0934ba3f7c872f0c2f58916a8be929e35597ec87d0e7a34186 |
| aggregate-console.log | 565525ba3a0b41060a1fa68b65e6dfeaaa7f4248a8a07d1286abf4d6725677c9 |
| source-before.json / source-after.json | 5bd46d9f8d3becc2a0f4c76331c7dcba75715eb4267c6e6afdfd3fed0e50ba48 |
| tools-before.json / tools-after.json | 66f538a02d410e16fb24e6ac8024606e5112ef1bda1eb5e674c996affcc68267 |
| powershell-before.json / powershell-after.json | 47a72d2ef3b44514a360ce2ba59c6d649df3099a7744a79b21979d8e27baf6d5 |
| stability.json | c0c73fdf5503d4cc076921be19d8064acd2c7ca221ceccc67adfaf9e1508e3c1 |
| head-input-binding.json | dfe51ceb1bacff9386a345b52201fb43748d41c630ea2c8961e762e20da9e138 |
| validation.json | 8610146deb966d56d1569bb3f274370d86a9dc6db92813576dbfef7c63e73fd3 |
| generated-binding.json | 3e6c5b81e5c0f8eafbdf3f3bef8923dfb20827e22802ffdbc0102cf340e0ba85 |
| warnings.json | e0a284d4b4f51ebc4b96fc7fef2318e7c8589c11a35d503fdea88b8762e40a6d |
| artifact-hashes.json | 6f62a45db1fc776bc4541a2a15ee7a0fd3dd9c718e467330cfbe7b55245c2f24 |

All verifier custody **FROZEN/RELEASED TO ROOT WITH FAIL**. No repair, assertion/selection weakening, rerun, source/test/main-doc/index/ref/publication change or extra authority is implied. The fixed checkpoint/deadline were not changed. Existing Windows build exit127 evidence remains; native VS/UWP/XAML/PS5.1/PID/decoded50video/heap/RAM/GPU/WinRT/service/session/signing/install/startup and actual pushed-SHA hosted CI remain unverified. BR-21 is still native-unmeasured.
