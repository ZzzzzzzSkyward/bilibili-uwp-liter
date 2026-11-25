# Preparation-only reuse bindings — FROZEN / RELEASED

No tests or extractors were executed. Private inspection script, full manifests and exact comparisons: `/workspace/work/video-memory/final-verification/preparation-20261125/`. `reuse-validation.json` SHA256 `98677483465a265d9f9cbb5f6cf3d4ab553b71aaaf80ed023ad3d0de60c79468`; `supplemental-bindings.json` resolves the intentionally replaced review Markdown inputs. These are current read/hash observations while player source is moving, not a final whole-tree gate.

## Exact unchanged quality inputs

All **5073** files currently under quality/dotnet and quality/cache/nuget equal the accepted recovery final tools-before.json byte for byte; no missing/added/changed files. Current and prior tool manifest SHA256 `66f538a02d410e16fb24e6ac8024606e5112ef1bda1eb5e674c996affcc68267`. Prior path `/workspace/work/recovery-integrity/final-verification/final-closed-20261124T2325/tools-before.json`; current `tools-current.json` in preparation scratch. Installed directory inventory is SDK8.0.427/runtime8.0.33/ref8.0.33.

| Relevant quality-relative file | SHA256 |
| --- | --- |
| dotnet/dotnet | db40da462b13ec4c07df7a75c4b0395223d360b8dc9a477d9e31fb71759b05f4 |
| dotnet/sdk/8.0.427/dotnet.dll | b5632df862202dd108e89026f11afd049df73b5962c5a51f0c35e02b271a9e4e |
| dotnet/sdk/8.0.427/Roslyn/bincore/csc.dll | 2b9a5bbe002b353e44c78506e1bccbbc55dcdf2338abb11a68a5056329cb7334 |
| dotnet/shared/Microsoft.NETCore.App/8.0.33/System.Private.CoreLib.dll | 98fec20c823887e41d4fbd2d2e900175e5b1ba7d671e29704c9ce4632dcc5432 |
| dotnet/packs/Microsoft.NETCore.App.Ref/8.0.33/ref/net8.0/System.Runtime.dll | 20b0357b92db5aea8c402747d1c39ae5f91fa061df91625f20542285659638f2 |
| cache/nuget/newtonsoft.json/13.0.3/lib/net6.0/Newtonsoft.Json.dll | 22c649f75fce5be7c7ccda8880473b634ef69ecf33f5d1ab8ad892caf47d5a07 |

## Separate runner14 and package15

The actual14-control receipt `/workspace/work/recovery-integrity/final-verification/controls-current-20261124T1753/controls.json` hashes `427d42beffae5b83956bd731a96f7474740f3b916812dfefcaf2de171600448e`; all14 rows are PASS. All three relevant runner sources remain exact:

| Repository input | SHA256 |
| --- | --- |
| plan/tests/run-all.py | afc18ae7e9fa6256a0b63e49846d90f2d4d8ae8ebdb32b0e4c753bcd6ecd11f5 |
| plan/tests/run-all.sh | 6b9cc38288b5bea7e07e50bed43a6c2027923ebcf0acf5c706f33a7dce1fd941 |
| plan/tests/verify-suite-runner.py | 0f0992d6714718f862bf7a50db70be7d18874cd16eb648f56a1147012cb47d67 |
| build-release-x64.ps1 | 479656b5e95d1af66d8262d453ea8b4db6735571431a0f038c52bd054a10ef10 |
| plan/tests/package-layout-probe.ps1 | 0d39001c69b9594b3a0ddd9c2f1d4098a5dcbc49e751daeb0e8255311595d14a |

The873-input historical selector receipt has26 candidate source differences today; full list/hash `runner-selector-differences-candidate.json` / `96bdb10e866fef8967717fd8e05300947ef76c057d1a16aaa19c3b55f753935c`. Those are later product/fixture repairs, including player/season/API and prior recovery edits, not changes to runner controls. Nineteen new runnable files are separately inventoried. Neither old selector/full-suite success nor count equality proves the new combined source. The unchanged runner contract is reusable; the final actual combined51/52 run must exercise its current source.

Package receipt [implementation-evidence.md](../../../20261124-package-layout-guard/implementation-evidence.md) and raw `/workspace/work/maintenance-foundation/package-layout/execution/final-integrity.json` bind the exact build/probe above and both unchanged manifests (`ee64a3b24c7014a5b3682afb7f63025ed84dc056d6f8bfb35229715da9aba921`, `2e655dd7b626ba17e53f6594783d29213ef13823476c8f3567ec3ed5a94d59ab`). Inspected raw logs under execution/logs: permanent-red-final ends CASES15/PASSED10/FAILED5; permanent-green-final and default-source-green end CASES15/PASSED15/FAILED0. Accepted exits are1/0/0. Exact log/integrity hashes are retained in reuse-validation.json. This supports the real-function archive predicate on Core, not a signed/installable executable or Windows startup. No package fixture rerun occurred.

## Corrected sampler receipt

[Final review](../../measurement/final-review.md) is PASS/released, final tool `668b5c06ff519d6f9500a9e087889e5504099cd84c0fb0fcf6bade4f2011474f`. All8 released implementation/evidence files match [release-binding.json](../../measurement/implementation-evidence/release-binding.json), SHA256 `d87315d5c6a9019cd60a732441a991ad1612b7419512da9a5c39033460b6cec6`. All1003 raw artifact bindings and all14 correction artifact bindings match. The two differences among9 reviewed-input paths are expected: old CHANGES review/evidence were replaced by final PASS review/evidence. Archived initial-changes-review.md and initial-changes-evidence.md under `/workspace/work/video-memory/tooling-final-review/t1-t3/` exactly match their original recorded hashes `e903be02…` / `c79d1ab0…`. Current PASS review/evidence hashes are `c7caf0ab97cb78e5048ab1b7bd79e10ac2713854ea423742f92c2a3c5334763d` / `16420a7e05a5c360284458cf0116ea14cd5e91c811c069ccbec92e762e38bdd0`. This is review progression, not stale executable evidence.

Actual supported results `/workspace/work/video-memory/tooling-fix/t1-t3/final-supported/results.json` hash `c5cb6161972021782255bb752f04aaf77eed6f74456907ca20a8fb8894f3d7c2`, statusPASS. All46 actual recorded command labels/exits match the expected byInvocation receipt: accepted calls exit0 and deliberate malformed/native-host rejections exit1. All original21 controlled groups PASS;106 revision checks;55 actual public helper calls/52 accepted synthetic rows. Both original/final106-check result digests and same fixture `62d788ea675302f7f915e787d51f7e5e14817413e1a42df4bd7baa4055116701` match; original exit1/38 failures/14 rows versus corrected exit0/zero failures/16 rows, with T1 idle ranks1021/1022 after settled50 and all cycle50 control phases. No prior56-call arithmetic error is repeated.

Current runnable hashes: run.py `20d38a64ba753c25663497e0304dcd70e752a7450266ff6a307736cfffc8de44`; controlled.ps1 `26031002342388927e02d216955109a02efeb648f072ea05b60b7db4b48ffd93`; markers.ps1 `9b3371eb53083c583ff6493457bbaebfaad96a08f0e3d7eec108eab9591e4ccb`; revisions.ps1 as above. Native process values remain synthetic; no Windows PID/UWP fifty-video/native byte result is inferred. Tests remain external to aggregate52.

### Interpreter provenance boundary

Accepted package and sampler receipts name `/workspace/work/maintenance-foundation/package-layout/powershell/pwsh`, Core7.7.0/Linux, but contain **no historical interpreter-binary or installation SHA256**. The searches of accepted receipts/private binding manifests found no prior baseline. Consequently exact old-to-current interpreter-byte equality is **unproved**, even though path/version provenance and executable test/result bindings are retained. Do not label that missing evidence as an executable regression or silently claim full interpreter identity.

Current pwsh binary SHA256 `3f27efac38d345f9a39dbb68c7ca519bcab35884a4f12e36ce0e456f876d275a`; all732 installed PowerShell files are captured in `powershell-current.json`, SHA256 `47a72d2ef3b44514a360ce2ba59c6d649df3099a7744a79b21979d8e27baf6d5`. Its bundled runtimeconfig is net11.0/Microsoft.NETCore.App11.0.0, separate from the product probes' .NET8 tools. Current Python/Bash/awk/Git path/realpath/binary hashes are in `interpreters-current.json`, SHA256 `79da86b39ffcf46fa30523e8c4a7d85d9ec587359145edf3fc3f88683857701e`. No interpreter was invoked for tests/version probing here. Root should explicitly disposition the absent historical interpreter hash before asserting byte-exact reuse; preparation authority does not allow repairing provenance by an unrequested test rerun.

## Historical complete reports and remaining gate

Accepted recovery final full report `/workspace/work/recovery-integrity/final-verification/final-closed-20261124T2325/aggregate/results.json` hash `b33d402fdf65518ea47805463f82d097bec9e638694750e6031b698baa1b549d` covers915 local recovery inputs and51/52; its manifest `0a80ac1cf14e76c9274564a999a88522c23460be6a85e077e093d5e28cd55639` is not the current candidate934 or a future final freeze. Earlier gate-20261124T175239Z report `d18fee55c607410264c870155f83e1ab8a34ea728c6f646ecc212705857b4d10` predates recovery final closure. Foundation qf1-results.md's50/51 is historical. No old full report is relabeled as combined memory/API acceptance.

Fresh final before/after manifests, exact owner/fresh-review closure, root activation, actual combined51/52 report and eventual immutable source blob binding are still required. Source-only PASS never establishes native Windows/VS/XAML/PS5.1/process50playback/RAM/GPU/WinRT/service/session/signing/startup. Preparation custody **released**; no test/publication authority exercised.
