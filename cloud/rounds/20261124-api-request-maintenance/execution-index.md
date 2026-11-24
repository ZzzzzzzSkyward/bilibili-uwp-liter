# Request/recovery execution index

Status: **U9 + E1 IMPLEMENTED, COMMITTED/PUSHED, OFFLINE VERIFIED; independent implementation review pending; Windows/session gates BLOCKED.**

| Slice | Artifact | Stable source/test commit | Genuine permanent red → unchanged green |
| --- | --- | --- | --- |
| U9 | [favorites/execution.md](favorites/execution.md) | `7ff548b24dc70a335c6d0d44c4ebc86a984ce96c` | 64 behavioral request/signing-boundary failures → c2 PASS 451, including all original 162 assertions. |
| E1 / UX-02 | [comment-recovery/execution.md](comment-recovery/execution.md) | `1c7f6e4aacf3c0fa4bb7f7d70851308405b995cd` | 153 real-dialog checks / 109 failures → 153/0 and owning comments PASS 1070, including all prior 917 assertions. |

Each commit was pushed to dev and immediate local HEAD/remote branch equality verified before the next step. History remains append-only; no force/rebase/master/release operation. A final evidence-only commit introduces this index and stable E1 hash without changing source/test behavior. Every source-executor-owned product/test/evidence file is committed; other owners' in-flight work remains preserved/unstaged.

Source/test ownership for U9 c2/FavoriteAPI and E1 comments/dialog files is now **RELEASED**. This executor has finished its first shared-index commit/push window after the evidence-only push. Search may proceed only under its separately granted orchestrator commit-window artifact; no competing file is staged. Main documentation remains reserved for its later owner.

Both artifacts carry reproducible commands, raw scratch log locations, fidelity/decoder policies and limits. `git diff --check` passed. Windows UWP compile remains BLOCKED (required command exit 127, powershell absent); actual XAML compilation/routing/rendering/focus/keyboard/WinRT cookies and any authorized session outcomes remain unverified. No live favorites/comment writes, runtime/device acceptance or release-ready claim. The green foundation baseline remains immutable evidence for its recorded SHA; new affected-batch passes do not claim a new whole-suite run.
