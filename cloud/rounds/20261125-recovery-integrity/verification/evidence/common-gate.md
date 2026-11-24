# Final common gate — PASS

Actual command (once; no selector/deadline change):

```bash
bash plan/tests/run-all.sh --tools-root /workspace/work/maintenance-foundation/quality --output /workspace/work/recovery-integrity/final-verification/gate-20261124T175239Z/aggregate
```

Exit0; finished=true, suite_status=PASS, complete_required=true. Current **51/51 logical, 52/52 executable PASS**; historical baseline50/51 preserved. Exact52 unique executable ids/51 logical ids; every row selected; inventory/prerequisite errors=[]/[]. Status counts `{"BLOCKED": 0, "CANCELLED": 0, "ERROR": 0, "FAIL": 0, "MISSING": 0, "NOT_SELECTED": 0, "PASS": 52, "PENDING": 0, "TIMEOUT": 0}`. Package15 remains a separate unchanged gate and was not rerun. No hidden second recovery invocation: one manifest row executes recovery/run.sh, one actual probe project/program139 checks.

Source root `/workspace/bilibili-uwp-liter`; report provenance `{"dirty": true, "head": "e928bf0e818869c03485f8ef6f31fa47e98ac0f0", "verified": true}`. This truthfully identifies dirty local execution at published storage receipt e928bf0, **not** a clean final recovery commit. Actual HEAD object/blob comparison binds897 unchanged inputs to the immutable published receipt, plus11 modified tracked recovery/tooling inputs and seven new untracked files. Root must bind the eventual identical published tree separately. No final remote success is inferred.

Before/after hashes cover all915 available non-cloud repository inputs (no missing tracked non-cloud path), including every product/test/runner/source-wiring input and all seven new files; full exact copied source is `/workspace/work/recovery-integrity/final-verification/gate-20261124T175239Z/source-snapshot`. Before/after installed exact SDK/runtime/reference/package inputs cover5073 files under quality/dotnet and quality/cache/nuget. Source changes `[]`; tool/package changes `[]`. No test build was active before launch; actual aggregate is sequential. gRPC artifact comparison: 70 before/70 after, changed `['src/BiliLite.gRPC/obj/Debug/netstandard2.0/BiliLite.gRPC.dll', 'src/BiliLite.gRPC/bin/Debug/netstandard2.0/BiliLite.gRPC.dll', 'src/BiliLite.gRPC/obj/Debug/netstandard2.0/BiliLite.gRPC.AssemblyInfo.cs', 'src/BiliLite.gRPC/obj/Debug/netstandard2.0/BiliLite.gRPC.AssemblyInfoInputs.cache']`. Generated wrappers/output hashes are separate in generated-after.json and are not represented as immutable product input or Windows build evidence.

The four derived gRPC changes are explained precisely: replacing only the old077ef35ae194d3ae85aa0621daeac07dc29e29ab informational-version SHA with currente928bf0e818869c03485f8ef6f31fa47e98ac0f0 reproduces the recorded AssemblyInfo before/after hashes. The first serial g1 build regenerated that header/cache and the corresponding obj/bin DLL; all66 other artifacts, including generated protocol sources, stayed identical. All70 artifacts were rehashed after the gate and remained identical to grpc-after.json. No source/proto/tool drift or competing gRPC build occurred. Exact current gRPC outputs and extraction/generated source wrappers are copied under gate scratch generated-snapshot/. This expected Git-revision-derived rebuild requires no second full aggregate.

## Runner controls

Read actual owner's all14 PASS first. Its selector receipt predates one RecommendVM optional-banner correction (current aba526dc929b08fa88ac187bc64399b872c9b6b8ee219036f91a64ee73e83020 vs prior35145c4e8462b4383871b286d6a198d7ef4b40b6c49c7732ddc24760f4a08c68; remaining872 match), so the dispatch explicitly warranted one new control run. Initial gate-20261124T175110Z is hash-only and stopped before any aggregate. Fresh command:

```bash
python3 plan/tests/verify-suite-runner.py --tools-root /workspace/work/maintenance-foundation/quality --output /workspace/work/recovery-integrity/final-verification/controls-current-20261124T1753
```

Exit0, **14/14 PASS**. Its873 selector-input hashes and recovery owner's18 final owned hashes exactly match current source before the actual aggregate; current runner/control source hashes match. This is current executable control evidence rather than reuse of an outdated source receipt. Exact control rows/names/details remain in controls.json.

## Source checks and warnings

Bash -n: c1/c2/recovery run.sh; Python compile(): recovery/prepare-source.py, run-all.py, verify-suite-runner.py; XML parsing: touched c1/c2/playersess/recovery projects, product UWP csproj and DownloadPage/RecommendPage XAML, all PASS. Product csproj contains exactly one explicit Compile include each for Services/DownloadPathCoordinator.cs and Parsing/Recommend/RecommendResponseParser.cs. These are source checks, never a substitute for Windows UWP/XAML compilation.

Compiler warnings were read, retained and not suppressed; exact per-stage lines/counts are in warnings.json. Observed counts `{"CS0067": 14, "CS0108": 12, "CS0162": 1, "CS0168": 1, "CS1701": 8, "CS1998": 5, "CS4014": 2, "CS8981": 10}`. Inherited product warnings include lowercase ApiHelper DTO names (CS8981), existing RelayCommand unused events (CS0067), existing async HttpResults member (CS1998), unchanged RecommendItemModel.timeText unreachable tail (CS0162), and unchanged DownloadVM.RemoveItem catch variable (CS0168). Existing stubs/lifecycle extraction expose CS0108/CS4014; csc Newtonsoft assembly-reference unification is CS1701. Recovery first links the two preexisting model/catch warnings into a permanent batch; their relevant source bodies already exist in HEAD. No new product warning cause was introduced by the reviewed recovery changes. Existing historical warning classes are also recorded in maintenance-foundation/verification/evidence/qf1-results.md; owning recovery evidence explains these additional source exposure warnings. No assertions were changed.

## Artifacts and limits

All raw reports/logs/manifests/snapshots live at `/workspace/work/recovery-integrity/final-verification/gate-20261124T175239Z` or the controls directory above. Complete per-file manifests stay there; concise content bindings below are SHA256.

| Artifact relative to gate scratch | SHA256 |
| --- | --- |
| `aggregate/results.json` | `d18fee55c607410264c870155f83e1ab8a34ea728c6f646ecc212705857b4d10` |
| `aggregate/summary.md` | `0a25d1182d8893fb9ff48b2b237d9bddb7e5a18bc4152088ea10166e45438bcc` |
| `aggregate-console.log` | `9f525f9042deda62db64d8a3dac342e9ad8f7c9f5cdd3890d0c73a5ee6cd72ce` |
| `source-before.json` | `42a24dba9e30d91308ce174f5a66a66fa87177550fced38bcda02d172cd4f91e` |
| `source-after.json` | `42a24dba9e30d91308ce174f5a66a66fa87177550fced38bcda02d172cd4f91e` |
| `tools-before.json` | `66f538a02d410e16fb24e6ac8024606e5112ef1bda1eb5e674c996affcc68267` |
| `tools-after.json` | `66f538a02d410e16fb24e6ac8024606e5112ef1bda1eb5e674c996affcc68267` |
| `grpc-before.json` | `384e9062c5f084a30ed2c2924b2b7dfe0baa6eef2d850b229f315ada80aece27` |
| `grpc-after.json` | `982b1660744c71551363082ef1c5eedc3a9587e4e550974e706e6b544006af44` |
| `grpc-stability.json` | `f2e7144922fa41332fad1f0dda8e9aed5f97cffed430b54e00bb39650122b2e2` |
| `generated-after.json` | `2ab245842dcbbf01410a5c6361bdc6c417712389abc321ac9cb0533f9198caf1` |
| `stability.json` | `7a238372c3ad0c1c07dbbe06596fba49df3dceeb520f108cece4c1b203466112` |
| `head-blob-binding.json` | `46bf413d54b6a73ad8673bb7de941703fc526bf5f719e6ac2efd2da4ee03c287` |
| `head-input-binding.json` | `35bca69a90086e561946d5fa50d230ae2384861420a8b38f0a0fb5d990c268f2` |
| `reused-controls-check.json` | `c3b502a08e4ceddb0d30ad7bcc53d92d8cbf8c5ae2cfae60d7c2da7c46e05da7` |
| `source-checks.json` | `1a9c8d3345c7d7ed8758c8c1d4f392b05b10f94b5207f3f51689a42c803ab75b` |
| `warnings.json` | `f5b7eca8755f93a071ea8a91c73882e97a8f9f5a6577e203e4f1f00bc3eadc2d` |
| `provenance-before.json` | `723084dae35d0560cb6beff279e5962bfa8710342ab9e6d096c7bc9fa043f1ff` |
| `provenance-after.json` | `b300b33a57631558649151d6f377919532211c73d5e51d99384b04a65eca502a` |
| `../controls-current-20261124T1753/controls.json` | `427d42beffae5b83956bd731a96f7474740f3b916812dfefcaf2de171600448e` |
| `../controls-current-20261124T1753/selector-input-hashes.json` | `fa262ffdd8b8da7c003dc909a7870ca8188355ec5b1e8731f165948b99352fe6` |

Windows/VS UWP/XAML compile/rendering, native BackgroundTransfer/FAL/file-lock semantics, real session/media/account actions and hosted CI remain unverified external gates. No real network/account/media write, workflow dispatch, release/identity/permission/settings or source/index/ref mutation was performed. Publication remains separately blocked by normal terminal transport.

Additional gRPC rebuild explanation SHA256: `9ed2336d58850d91a94c492965f700bcbf6c651bc105aa75c3b4e31ffc3b1e89`.
