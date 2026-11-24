# Source execution index — U1 then PB1

Status: **IMPLEMENTED + OFFLINE VERIFIED; implementation review pending; Windows/session gates BLOCKED.**

| Slice | Evidence | Source/test commit | Verification |
| --- | --- | --- | --- |
| U1 | [u1/execution.md](u1/execution.md) | `5eec54ecd094bdb51ec43b42d9f901d8fae7c993`, pushed and exact remote verified before PB1 started | Genuine old-builder red: 174 request failures; same new assertions green: comments 856 assertions. |
| PB1 | [pb1/execution.md](pb1/execution.md) | `edbd8739d894b8ab59400e097eb96a9a0a1ac592`, pushed and exact remote verified | Genuine old-reader red: 16 exceptions across four readers; unchanged assertions green: comments 917, c1 186, season1 117. |

Both slices preserve prior assertions and scoped ownership. `git diff --check` passes. SDK 8.0.427/runtime 8.0.33 is the quality stream's installed scratch toolchain; scripts use Bash, with its SDK also prepended to PATH for c1/season1. Full logs remain in `/workspace/work/maintenance-foundation/source-execution/`, with decisive outputs and exact reproducible commands in each slice artifact.

Windows compile command fails before build with exit 127 (`powershell: command not found`) on this Linux host. Controlled WinRT form/cookie inspection and authenticated like/unlike/add/direct reply/child reply/delete/error-session journeys require the Windows/consenting-session environment. No runtime, device, account, UI or release-ready claim follows from offline probes. Main docs stay reserved for their assigned later owner. No shared/main docs or quality-owned file was staged by this executor; other owners' work remains preserved.

Source execution is complete and owns no further product/test mutation. Independent reviewers may inspect the two immutable source/test commits above; quality may begin its immutable/full-suite verification and await the orchestrator's separate shared-index commit-window artifact. Final source-owned metadata commit records only these observed stable SHAs; no uncommitted source-executor file remains.
