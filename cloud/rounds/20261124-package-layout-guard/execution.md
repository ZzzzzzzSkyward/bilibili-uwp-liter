# Package-layout guard execution

Status: **OFFLINE PASS — COMMIT WINDOW GRANTED**. This round's commit-window.md grants the exclusive global index after verified search metadata `01c503f0b878e4949ec0aa61ef441ee2950fea03`. Already verified source/probe and both manifest hashes remain identical; no retesting needed or performed. Other owners' work remains preserved. Implementation review and Windows gates remain pending/BLOCKED.

Owned implementation files:

- `build-release-x64.ps1`: exactly three added lines inside Test-FullPackageLayout: explanatory comment, root-key existence check and uncompressed Int64 length `-ge 64KB` rejection. Existing diagnostic priority, reader/caller and all other script bytes unchanged.
- New `plan/tests/package-layout-probe.ps1`: PS5.1-compatible syntax/APIs and UTF-8 BOM; default/override source/scratch paths; parser-validated exact top-level two-function extraction; inert .NET ZIP fixtures; 15 mandatory cases; exact diagnostics, Int64/uncompressed boundary and cleanup checks; required 0/1/2 exits. No full-script execution or copied validation algorithm.

Permanent actual-source red: **exit 1, CASES=15 PASSED=10 FAILED=5** (five intended root false accepts; old controls PASS). Scoped source repair, unchanged permanent assertions: **exit 0, CASES=15 PASSED=15 FAILED=0**. Default source selection also PASS. Loader/parser/duplicate/nested/setup negative controls return 2; a top-level sentinel is not executed; space/bracket source/scratch paths work. Full-source/probe syntax checks and `git diff --check` PASS. Both manifests byte unchanged, reader AST hash unchanged, all bytes outside the scoped function unchanged.

Exact commands/exits, all case outcomes, source/function/probe/manifest hashes, loader controls, cleanup/inventory and platform limits: [implementation-evidence.md](implementation-evidence.md). Raw logs/source snapshots only in `/workspace/work/maintenance-foundation/package-layout/execution/`. Adds one separate named PowerShell gate with 15 internal archive cases, without altering the historical 50-logical/51-stage inventory or QF2 runner/CI scope.

Commit/local/remote SHA: publication pending in the granted window. Stage exactly build-release-x64.ps1, plan/tests/package-layout-probe.ps1, this execution record and implementation-evidence.md; no other round/owner file. No main docs/manifest/version/asset/release/master/live action. Stable verified source/probe hashes are recorded in implementation-evidence.md and matched again before staging.

PowerShell Core **7.7.0 / Linux** actual-function fixture behavior is verified. Windows PowerShell 5.1 execution, full release/UWP build, real signed/installable package, installed launch/converter/device behavior remain **FAILED/BLOCKED — Windows environment unavailable**. The full release script was never run or dot-sourced. Zero-byte launcher acceptance remains the explicitly approved limited-predicate characterization.
