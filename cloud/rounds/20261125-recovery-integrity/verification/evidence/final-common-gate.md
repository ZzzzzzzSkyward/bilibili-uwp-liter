# Final changed-source common gate — LOCAL COMPLETE PASS / FROZEN

The sole actual full command ran once without selectors or budget changes:

```bash
bash plan/tests/run-all.sh --tools-root /workspace/work/maintenance-foundation/quality --output /workspace/work/recovery-integrity/final-verification/final-closed-20261124T2325/aggregate
```

Exit0; finished=true, suite_status=PASS, complete_required=true. **51/51 logical and52/52 executable PASS**, all52 required rows selected/available/exit0, exact manifest order/argv/paths,52 distinct executable ids/51 logical ids including both AUTH01B members. There are no missing, extra, failed, blocked, error, timeout, cancelled, pending, unknown, skipped or truncated rows. Inventory/prerequisite errors are empty. Current44 directory batches (43 net8 projects+shell stab) plus seven legacy logical/eight executable stages retain historical50/51 separately. Fixed inventory SHA256 `84730c2d99a5823a7fdc13d2b297c673c82d8d866158cbf1da6172dbe868b7c7`. Real report times: 2026-11-24T20:02:41.267971+00:00→2026-11-24T20:04:05.245770+00:00, 83.992 seconds. Directory label is not execution-time evidence.

## Exact executed source and provenance

Source root `/workspace/bilibili-uwp-liter`; branch dev; HEAD/tracking `e928bf0e818869c03485f8ef6f31fa47e98ac0f0`; report dirty=true/verified=true. **This is local dirty recovery source, not a clean or published recovery tree.** All915 non-cloud repository inputs, including every actual product/test/tooling/wiring input and seven untracked files, match the independently accepted manifest **`0a80ac1cf14e76c9274564a999a88522c23460be6a85e077e093d5e28cd55639`**. No tracked non-cloud input is missing. Before/after source dictionaries are equal. The exact915-file copy at `/workspace/work/recovery-integrity/final-verification/final-closed-20261124T2325/source-snapshot/` was checked file-by-file before launch and again after completion.

HEAD blob binding:893 unchanged published inputs,15 modified tracked inputs, seven new inputs. Full path/hash and HEAD comparison inventories are `source-before.json`, `source-after.json`, `head-input-binding.json`; these distinguish recovery bytes from observed HEAD. All5073 installed SDK/runtime/ref/package files under quality/dotnet and quality/cache/nuget are stable before/after and on final recheck, tool manifest **`66f538a02d410e16fb24e6ac8024606e5112ef1bda1eb5e674c996affcc68267`**. Report resolves SDK8.0.427 and runtime/ref8.0.33. Index bytes and all local refs were unchanged. No source/test/main-doc/config/index/object/ref/credential/settings/publication action occurred.

## Separate unchanged gates and generated inputs

Existing14/14 runner controls are reused by exact receipt **`427d42beffae5b83956bd731a96f7474740f3b916812dfefcaf2de171600448e`** and unchanged run-all.py/run-all.sh/verify-suite-runner.py plus all5073 tool/package bindings. Their873-input selector receipt has seven later product/test differences, explicitly listed in `reused-gates.json`; those are the reviewed repairs exercised by this complete run, not runner/control behavior changes. All three runner/control sources match the fresh receipt's source snapshot and recorded hashes. No unnecessary control rerun occurred.

Package15 remains separate/excluded. Exact unchanged build script `479656b5e95d1af66d8262d453ea8b4db6735571431a0f038c52bd054a10ef10` and probe `0d39001c69b9594b3a0ddd9c2f1d4098a5dcbc49e751daeb0e8255311595d14a` bind the [accepted package gate](../../../20261124-package-layout-guard/implementation-evidence.md); no package/full-release rerun occurred.

Recovery ran one permanent executable: **checks348/failures0**, all tracked native deliveries and singleton observers settled. Its actual22-input/16-slice receipt SHA256 `f1e306f328e6d971212bc35bc5d6a73fcbacabe26af45ccfe385bc9d31d8c77b` and generated wrapper `e18d89b0d51f054fe9350e32996bac7693830017a27b31ca9d34888141c1fe21` exactly match the reviewed fixed-source artifacts. All70 gRPC derived files were unchanged; their informational header already records current e928bf0. The old pre-fix gate's077→e928 rebuild is historical only. All1261 separately recorded generated outputs stayed stable on final recheck; actual wrappers/receipts and gRPC outputs are copied into generated-snapshot/.

## Source checks and retained warnings

177 checks passed: Bash syntax for all probe shell inputs, Python AST parsing for all probe Python inputs, XML parsing of product csproj/XAML/manifests and probe csproj, and exactly one explicit UWP Compile include each for DownloadPathCoordinator/RecommendResponseParser. Scoped git diff --check passed. These checks do not establish Windows compilation or binding/runtime behavior.

Retained compiler diagnostic counts: `{"CS0067": 8, "CS0162": 1, "CS0168": 1, "CS1701": 8, "CS1998": 4, "CS4014": 2}`. Full per-stage lines and comparison with the historical gate are in warnings.json/warning-comparison.json. No new warning code appeared. Existing CS1998 HttpResults async member, CS0162 model tail, CS0168 RemoveItem catch variable, CS0067 RelayCommand events, CS1701 Newtonsoft reference unification and CS4014 LoginVM probe calls remain. Recovery additionally exposes the unchanged accepted primitive ComboBox.Loaded/SelectionChanged event warnings; both were already present in the final owning348 build receipt. Lower counts in unchanged projects reflect this incremental invocation and do not claim that inherited diagnostics were fixed. No warning suppression or assertion change occurred.

A private precheck initially interpreted Git's quoted non-ASCII ls-files output as literal paths. It stopped before the command was launched; using NUL-delimited ls-files/ls-tree corrected only verifier path decoding. The initial script is retained as execute-precheck-only.py. There was exactly one actual aggregate run and no product/tool change.

## Immutable receipts and release

All raw logs/reports/manifests/copied sources are private under `/workspace/work/recovery-integrity/final-verification/final-closed-20261124T2325`. `artifact-hashes.json` binds the complete concise raw receipt inventory; key SHA256 bindings follow.

| Artifact | SHA256 |
| --- | --- |
| `aggregate/results.json` | `b33d402fdf65518ea47805463f82d097bec9e638694750e6031b698baa1b549d` |
| `aggregate/summary.md` | `0a25d1182d8893fb9ff48b2b237d9bddb7e5a18bc4152088ea10166e45438bcc` |
| `aggregate-console.log` | `7d514eb5990f137c35ad15a7b7163c6ab38a33dd155b7743de515332e52dc8e0` |
| `source-before.json` | `0a80ac1cf14e76c9274564a999a88522c23460be6a85e077e093d5e28cd55639` |
| `source-after.json` | `0a80ac1cf14e76c9274564a999a88522c23460be6a85e077e093d5e28cd55639` |
| `tools-before.json` | `66f538a02d410e16fb24e6ac8024606e5112ef1bda1eb5e674c996affcc68267` |
| `tools-after.json` | `66f538a02d410e16fb24e6ac8024606e5112ef1bda1eb5e674c996affcc68267` |
| `stability.json` | `e4241697c2d82c80476cd6f320174b4815d32f5809647a9ff5dc8906682655e1` |
| `head-input-binding.json` | `e365fef9c3ec0026c00f290b97466ce89e85735a2fd17564a502709031fcd1cc` |
| `reused-gates.json` | `8f6604639e20ec841742311191f418eb69da5ae07e18811b62f29b217f739d55` |
| `source-checks.json` | `8dd76a11670f9e69e3c223c82d0f89a06f71dbcfd0fbb62ca09b43c09f50632e` |
| `warnings.json` | `86249145b702d0d5047193674f6d96c1b1d99f0e813fe6db1b6b3cedca3cf4d5` |
| `recovery-generated-binding.json` | `1e4bb822c3b936c734e37849b85811b8aeea57b4b54c0055855dfaa985d39369` |
| `final-freeze-binding.json` | `19e2c710efc44645fa506c3a628634155b02a34363a15b7053efb21a0873a3ff` |
| `publication-candidate-binding.json` | `cd50b9ac9a7370795b89e9b7d18c8fa4648dc19e273f06f8a4b41196da30820e` |
| `artifact-hashes.json` | `8de2a7314b32d98c16d0d7095ae166b8d1bfbfd951265e58848ab72b794ce943` |

The inactive30-path publication candidate is separately bound in publication-candidate-binding.json:22 runnable files equal the exact executed source, plus eight owning evidence files. This binding grants no staging/publication. Root must verify the eventual staged/published canonical runnable files against this source manifest. Later main-document-only edits require an explicit allowed-path/byte diff proving every product/test/tooling/wiring byte remains identical; they do not justify rerunning this unchanged product suite.

Ownership released **LOCAL COMPLETE PASS/FROZEN**. The initial pre-fix common gate in [common-gate.md](common-gate.md) remains historical evidence for its own source snapshot. Native Windows/VS/UWP/XAML/login/BackgroundTransfer/file-lock/restart/session/media/device/signing and actual pushed-SHA hosted CI remain unverified external gates. No native/hosted/release/final remote PASS is inferred.
