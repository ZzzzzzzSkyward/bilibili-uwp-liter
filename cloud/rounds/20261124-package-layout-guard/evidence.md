# Actual-function package-layout evidence

Date: 2026-11-24. Source HEAD at intake: `10dd84857c748f6638e187d891d6a6649f887bb6`. The build script and two versioned manifests were read/hashed and left unchanged; unrelated owners have active source/test/cloud edits, so no whole-worktree-clean claim is made.

## Tool installation and boundaries

Reused the Cloud Environment runtime skill and Packages guidance read earlier in this task. `/usr/local/bin/cloud-agent-packages` is absent. `powershell` and `pwsh` were absent from PATH; a user-level tool was installed solely under `/workspace/work/maintenance-foundation/package-layout/powershell/`.

Official metadata: `https://api.github.com/repos/PowerShell/PowerShell/releases/latest` returned stable **v7.7.0**, published `2026-11-10T20:18:47Z`.
Official archive: `https://github.com/PowerShell/PowerShell/releases/download/v7.7.0/powershell-7.7.0-linux-x64.tar.gz`.
Download exit 0; archive bytes **76881682**. SHA-256 exactly matches the digest on that official release asset:

```text
c89d9c682d2c403dfe726f3df948e359675235a5bd6fab535776de69729ca0cb
```

Archive extraction exit 0; `$PSVersionTable` exit 0: PSVersion **7.7.0**, PSEdition **Core**, Platform **Unix**, Debian GNU/Linux 13. No system/home installation, repository dependency change, Windows toolchain or release-build execution occurred. Raw metadata/install/version logs are in assigned scratch `logs/`.

## Exact source seam

Scratch runner: `/workspace/work/maintenance-foundation/package-layout/run-fixtures.ps1`.
Fixture generator/inventory: `create-fixtures.py`, `fixtures.json`, and `fixtures/` in the same scratch root.
The runner calls the PowerShell Parser on `build-release-x64.ps1`, checks parse errors and exactly one definition for each expected function, takes FunctionDefinitionAst extents, and defines only those exact function texts. It never invokes/dot-sources the complete build script. Source functions are `Read-AppxPackageLayout` at lines 407–424 and `Test-FullPackageLayout` at 426–433.
It loads the ZIP assembly, invokes the actual reader/predicate against inert ZIPs named .msix, and compares outputs/exceptions to independent expected outcomes. Python only creates fixture archives; it does not implement the predicate. No package is installed or executable content run.

Command:

```bash
/workspace/work/maintenance-foundation/package-layout/powershell/pwsh -NoLogo -NoProfile -File /workspace/work/maintenance-foundation/package-layout/run-fixtures.ps1
```

Result: **exit 1**, `EXTRACTED=2 BUILD_SCRIPT_PARSE_ONLY=YES`; `ROOT_LENGTH_TYPE=System.Int64 ROOT_LENGTH=65535`; **CASES=15 PASSED=10 FAILED=5**. Complete output is retained in scratch `logs/red-fixtures.txt`.

| Case group | Observed output | Count / interpretation |
| --- | --- | --- |
| Root 8,192; root 65,535 | ACCEPTED | 2 passing structural controls. |
| Zero-byte root | ACCEPTED | 1 passing characterization of the explicitly bounded existence/upper-bound predicate, not a real launcher. |
| Missing canonical root; nested-only root | ACCEPTED | 2 red failures: required missing-root rejection is absent. |
| Root 65,536; 65,537; 4 MiB | ACCEPTED | 3 red failures: required size rejection is absent. |
| Missing entrypoint | `缺少 entrypoint/BiliLiter.exe` | 1 unchanged rejection control. |
| Missing companion | `缺少 BiliLite.Win32Tools` | 1 unchanged rejection control. |
| Missing manifest; manifest missing declaration | `清单里没有 fullTrustProcess 扩展` | 2 unchanged rejection controls. |
| Missing root and entrypoint together | `缺少 entrypoint/BiliLiter.exe` | 1 old diagnostic-priority control. |
| Corrupt archive; missing archive path | THREW MethodInvocationException | 2 unchanged reader error controls; no successful fallback. |

The highly compressed root has **4,194,304 uncompressed bytes versus 4,084 compressed bytes**. The actual reader stores ZipArchiveEntry.Length, so the repair must compare the uncompressed Int64 value. The existing guard currently ignores that root metadata entirely.
All 15 source invocations completed; the nonzero aggregate is from the five intended root false accepts, not parse/setup failure. No green implementation was simulated or applied. The permanent probe still needs its own original-source red run followed by unchanged-assertion green evidence after independent requirements PASS.

## Platform limits and preservation

PowerShell Core fixture execution does not establish Windows PowerShell 5.1 compatibility, a UWP build, real package layout/contents/signature, installed startup, converter operation or playback. Those Windows checks remain FAILED/BLOCKED — environment unavailable. The historical `powershell: command not found` build result remains historical; the new local Core tool is used only for safe function fixtures and does not make the full Windows pipeline available.
The scratch `logs/source-integrity.json` captures SHA-256 for build-release-x64.ps1 and both Package.appxmanifest files. Recheck after fixture execution found no change. No sparse settings/excluded blobs, product/tests/build/main docs, manifests, assets, branch or git history were modified by this requirements owner.
