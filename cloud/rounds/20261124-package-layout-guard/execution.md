# Package-layout guard execution

Status: **COMMITTED / PUSHED — OFFLINE PASS**. This round's commit-window.md grants the exclusive global index after verified search metadata `01c503f0b878e4949ec0aa61ef441ee2950fea03`. Already verified source/probe and both manifest hashes remain identical; no retesting needed or performed. Other owners' work remains preserved. Implementation review and Windows gates remain pending/BLOCKED.

Owned implementation files:

- `build-release-x64.ps1`: exactly three added lines inside Test-FullPackageLayout: explanatory comment, root-key existence check and uncompressed Int64 length `-ge 64KB` rejection. Existing diagnostic priority, reader/caller and all other script bytes unchanged.
- New `plan/tests/package-layout-probe.ps1`: PS5.1-compatible syntax/APIs and UTF-8 BOM; default/override source/scratch paths; parser-validated exact top-level two-function extraction; inert .NET ZIP fixtures; 15 mandatory cases; exact diagnostics, Int64/uncompressed boundary and cleanup checks; required 0/1/2 exits. No full-script execution or copied validation algorithm.

Permanent actual-source red: **exit 1, CASES=15 PASSED=10 FAILED=5** (five intended root false accepts; old controls PASS). Scoped source repair, unchanged permanent assertions: **exit 0, CASES=15 PASSED=15 FAILED=0**. Default source selection also PASS. Loader/parser/duplicate/nested/setup negative controls return 2; a top-level sentinel is not executed; space/bracket source/scratch paths work. Full-source/probe syntax checks and `git diff --check` PASS. Both manifests byte unchanged, reader AST hash unchanged, all bytes outside the scoped function unchanged.

Exact commands/exits, all case outcomes, source/function/probe/manifest hashes, loader controls, cleanup/inventory and platform limits: [implementation-evidence.md](implementation-evidence.md). Raw logs/source snapshots only in `/workspace/work/maintenance-foundation/package-layout/execution/`. Adds one separate named PowerShell gate with 15 internal archive cases, without altering the historical 50-logical/51-stage inventory or QF2 runner/CI scope.

Implementation commit: `8f559681c700453e681b28bd295ad56b2cdcf132` (`fix: reject missing and oversized package root launcher`). `git push origin dev` exit 0; immediate `git rev-parse HEAD` and `git ls-remote origin refs/heads/dev` both returned that exact SHA. Exactly build-release-x64.ps1, plan/tests/package-layout-probe.ps1, this execution record and implementation-evidence.md were staged; no other owner/round file. This final status update is a subsequent evidence-only commit, whose final HEAD/remote SHA the orchestrator verifies independently. No history rewriting or additional tests/source changes.

Stable verified SHA-256 remains build script `479656b5e95d1af66d8262d453ea8b4db6735571431a0f038c52bd054a10ef10`, permanent probe `0d39001c69b9594b3a0ddd9c2f1d4098a5dcbc49e751daeb0e8255311595d14a`; both manifest hashes remain unchanged as recorded in implementation-evidence.md. No main docs/manifest/version/asset/release/master/live action.

**Release both package-guard files and the global commit/push window after this evidence-only commit/push.** No executor process/write remains active after return. Implementation review may assign bounded fixes through a new follow-up; Windows gates remain BLOCKED.

PowerShell Core **7.7.0 / Linux** actual-function fixture behavior is verified. Windows PowerShell 5.1 execution, full release/UWP build, real signed/installable package, installed launch/converter/device behavior remain **FAILED/BLOCKED — Windows environment unavailable**. The full release script was never run or dot-sourced. Zero-byte launcher acceptance remains the explicitly approved limited-predicate characterization.
