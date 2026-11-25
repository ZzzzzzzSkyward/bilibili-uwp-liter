# Fresh IG1 audit — exact binding, wiring and ownership proof

Final Control SHA256 `262e0719cf4589ebd9e493937f377bcc95d43e092962aad30dbfc137275bcb89`; baselineb382 `b3823bdb9fe15e7f90465a78446f6480fc81c8c92832de6f72d1762621a0a3b7`; HEAD7356b948 unchanged. All46 released fixture/read-only source/tooling inputs plus Control (47 paths) independently hash-match before/after. Compiler/runtime/ref-pack/Newtonsoft DLL hashes independently match the owning receipt. Full map is private before.json, matching resources/ig1-implementation-evidence/bindings.json.

## Source and maintained wiring

The original ux1d checks read source text and require inline direct Next=false in Show and inline Next=true in Clear; all checks are honestly labeled source. Source inspection and independent Roslyn AST checks establish executable assignments, not comment/string/dead-code matches. Show directly sets false within its BottomBtnNext!=null branch. Clear first removes its pending reason, then its real !_localTrackSwitching branch directly sets true; its else directly retains false. Both methods keep the first terminal return, targets/host/accessibility/labels/ApplyNextLabel. No current-item check, helper invocation, selector write, new field/API/policy or redundant call was introduced.

The live truth table remains: Show disables with or without a current switch; Clear enables only without a current switch. Matching current switch completion still runs the unchanged eligibility helper and cannot release a pending transition. A stale A finally still cannot enable or roll back B. The transition methods now affect only the Next restriction they own; selector state stays owned by item/switch paths.

Independent Roslyn parses all56 complete method/property spans for each baseline/final manifest and verifies hashes. The baseline execution originally read the repository path while it containedb382; audit validation correctly uses its immutable capturedb382 source for those historical control spans, rather than treating that now-current path as old bytes. Independent exact-span masking proves every character outside the two transition declarations is identical. First terminal guards/direct conditions are inspected as syntax. All other current helper/RF1/item/switch/dispose/renderer/history body hashes remain unchanged.

## Independent isolated replay

`python3 /workspace/work/video-memory/fresh-ig1-audit/replay.py`, exit0. Private full-source snapshots only; no main playersess or ux1d project build. The unchanged ux1d fixture is compiled/run on each private source snapshot. Existing full-method PF1 Case groups are selected through a private reflection entry, resetting only the fixture failure counter; no product flag/ownership algorithm is replaced. Sixteen real A/B current-switch/replacement outcome groups plus terminal and real-selector groups run on each side. A further private actual-method control verifies empty-owner pending labels/host/Next and terminal quiet behavior; it does not add a current-item prerequisite.

- `baseline/pf1` exit0, expected0; source `b3823bdb9fe15e7f90465a78446f6480fc81c8c92832de6f72d1762621a0a3b7`; log SHA256 `9890456b2a2ba0f2d8633978e530ebae2a33104b873f22f7942232908a909167`; same fixture `True`.
- `baseline/ux1d` exit1, expected1; source `b3823bdb9fe15e7f90465a78446f6480fc81c8c92832de6f72d1762621a0a3b7`; log SHA256 `3296315f77d40ab1471d05180ae70c36665d171e5ade7f7273c1c56300da91ce`; same fixture `True`.
- `final/pf1` exit0, expected0; source `262e0719cf4589ebd9e493937f377bcc95d43e092962aad30dbfc137275bcb89`; log SHA256 `9890456b2a2ba0f2d8633978e530ebae2a33104b873f22f7942232908a909167`; same fixture `True`.
- `final/ux1d` exit0, expected0; source `262e0719cf4589ebd9e493937f377bcc95d43e092962aad30dbfc137275bcb89`; log SHA256 `46a15d3d120a5550b8b33380301ca8cd7aef5747c05f842d4d184c1fc152fe34`; same fixture `True`.

Baseline ux1d fails after54 successful checks at the maintained pending-disable source check. Final passes all107 assertions, including the later Clear inline-enable check. The two logs byte-match the released owning ux1d logs. Both sides pass all18 actual-source PF1 groups plus the empty-owner/terminal/label control; PF1 output bytes are identical. The real B held switch, transition Show/Clear, current failure/fault/cancel recovery and stale A finally/model cases keep both disable reasons. All gates/timers settle through existing deterministic context observation. No runtime/native defect is inferred from the source-wiring failure.

## Reused evidence and immutable failure

Owning selected command/log/report hashes independently match the released bindings. Baseline two-stage selection is FAIL; final is PARTIAL_PASS, complete_required=false. Both sides retain66/66 residual groups (58 plus RF1 eight),39 lifetime,five graphics and original240 assertions (existing aggregate241). Every test/stub/extractor/project/runner/assertion is byte-identical; original assertions are not weakened. No full suite or separate graphics gate was rerun by this audit.

The first full aggregate remains immutable FAIL:51 executable PASS/1 FAIL;50/51 logical PASS; sole ux1d failure. Results SHA256 `aee9bff87ec5fd891b40b6a89f4289d4e1b298a9f5d4504fe5f7b3be34426a17`, ux1d log `3296315f77d40ab1471d05180ae70c36665d171e5ade7f7273c1c56300da91ce` independently match. New partial success does not relabel or replace that full failure. A later new stable complete gate remains root's separately authorized work.

Current-work null guard retains body hash `7ee4a330af7d08f19425de06dbe5ff66a62cbd90e6f3cd54bb8600eaab3b4adf`; G1 renderer retains `5789d03300d297cb7bc6115ee0622417b5773960544b3b0899ec2bb8167fbcb7`. HistoryKey/Checkpoint/Drain/Flush, item/switch/dispose and every remaining product span are exact. Thus prior RF1, FA1/FA2/FA3, terminal/live/fresh and G1/history acceptance carries forward by relevant byte identity plus current owning positives. No blanket whole-file equality or extra graphics/native claim is made. Season/tooling/API inputs and separate reviews are not rewritten or recertified by this correction.

## Structure, journey, limits and release

The two direct public-boundary assignments preserve maintained wiring and current reason ownership without a new framework, stale enabled snapshot, selector rewrite or new media precondition. Labels and normal live empty-owner transition presentation remain functional. Ordinary current/stale switch, pending/settled transition, terminal and no-item/RF1 behavior remain coherent; no additional supported source/structure/UX gap was found in this exact scope. Existing page/VM/renderer/history responsibilities and51 logical/52 executable inventory remain intact.

Native Windows/UWP/PS5.1/UI/cache/dispatcher/media/decoder/WinRT/SMTC/display/graphics/pixels/RAM/GPU/service/account/session/hosted/fifty watched-video gates remain external and unverified. R4/stream/global/background/cache/finite-work dispositions are unchanged. This source-wiring correction proves no native memory improvement or user RAM reproduction.

Full audit read custody is released. Only assigned new Markdown and exclusive private scratch were written; no source/test/main-doc/index/ref/cache/settings/Git/publication action. The authoritative stop/freeze/push-start remains08:29:30UTC/16:29:30Asia/Shanghai; this audit does not reset or extend it. Prior evidence remains preserved.

Private `/workspace/work/video-memory/fresh-ig1-audit/before.json` SHA256 `32e7f3b44b3a3665700e8cd19cccc78bfe428f9ea7eac81b5aa948d28ac3fa06`.

Private `/workspace/work/video-memory/fresh-ig1-audit/comparison.json` SHA256 `fc6fac9beb1901b0155d39aa3fd4a705f2792e61428352d03dbd1882ce538d19`.

Private `/workspace/work/video-memory/fresh-ig1-audit/integrity.json` SHA256 `cac1181c645b9a04c6481ccb99b80c6d50ac3deca87b608f2e3dd5b2bff9f853`.

Private `/workspace/work/video-memory/fresh-ig1-audit/replay.py` SHA256 `99a1e2bdce1c9eab9d02fb4edbcdfa1c2f45c9f9ccf41da241a58f309fed9faf`.

Private `/workspace/work/video-memory/fresh-ig1-audit/extraction-check/run.log` SHA256 `13bd3b808a4c69d56688550dc86e035704be18defd587f85e0b7ac3acdba8097`.
