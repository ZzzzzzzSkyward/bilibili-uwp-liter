# Building and fixing build failures (x64 Release)

## Run the script

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build-release-x64.ps1
```

- Use the `.ps1` directly. `build-release-x64.bat` is the double-click wrapper and ends with `pause`, so it blocks non-interactive runs.
- The script locates MSBuild through `vswhere`, restores both `packages.config` and `PackageReference` projects, then builds `Release|x64` with `-p:AppxPackageDir=Z:\`. Do not call MSBuild yourself; the script already handles the restore and output directory.
- The output directory must exist (`Z:\` by default, override with `-OutputDir`).
- Every run bumps the version on its own: before building, the script raises the revision of both `src/BiliLite.UWP/Package.appxmanifest` and `src/BiliLite.Packages/Package.appxmanifest` (for example `1.0.9.3` to `1.0.9.4`), so each build ships a higher, installable version. Do not pass a flag to get this, and do not hand-edit the manifests to bump them; a sideloaded package at an unchanged version silently replaces the installed app instead of upgrading it.
- Commit the bumped manifests together with the change that triggered the build. `-NoVersionBump` is an escape hatch for a throwaway rebuild at the current version (for example, re-running a failed build while diagnosing); it must not be the default in a release or verification run.
- A full run takes about two minutes. Let it finish and read the result once, rather than polling.

### `CS2012` on `BiliLiter.exe`

`obj\x64\Release\intermediatexaml\BiliLiter.exe` is held open by a compiler process that is still exiting, so the next build fails with `CS2012` followed by `MSB4181 CompileXaml returned false` - a lock, not a source error. `-nodeReuse:false` and `-p:UseSharedCompilation=false` reduce it but do not remove it: the locker seen here was a spawned `csc`/`XamlCompiler`, and a fixed `Start-Sleep 3` retry was still too early, so the retry failed with the same error against a different process id.

The script now extracts the locked path from the `CS2012` message, kills `MSBuild`/`VBCSCompiler`/`csc`, waits until the file can be opened with `FileShare.None` (up to 30 s, polling every 500 ms), and only then retries with **`-t:Rebuild`** - at most twice. A fixed sleep is a guess; the exclusive-open probe is the actual precondition, and the rebuild is what keeps a half-written `intermediatexaml` from being packaged (a `Build` retry can produce a package that dies at the splash screen).

If a run still fails, rerun with `-NoVersionBump` so the failed attempt does not burn a revision.

### The MSIX layout that actually boots

The intended layout is a small root `BiliLiter.exe` launcher (historically 8 KB), with the managed
app at `entrypoint/BiliLiter.exe`. Deliver only
`BiliLite.Packages_<version>_x64_Test/BiliLite.Packages_<version>_x64.msix`.
The app project's `BiliLite.UWP_*.msix` uses its own directory and lacks the required entrypoint layout;
installing it historically exited at the splash screen before application logging. The former
same-name collision explanation and UWP-package retry/recheck do not describe the current script.

The script filters discovered MSIX paths to Packages deliverables, selects the first one and validates it.
It rejects missing `entrypoint/BiliLiter.exe`, missing `BiliLite.Win32Tools/BiliLite.Win32Tools.exe`,
missing manifest `fullTrustProcess`, missing root `BiliLiter.exe`, or a root entry whose **uncompressed
Int64 ZIP length is >=65,536 bytes**. Exactly 65,535 is allowed; there is no minimum-size, PE/signature
or bootability check. A zero-byte fixture can satisfy the limited root predicate.
A layout failure throws; there is no UWP-package retry. On success it prints `交付包` and removes only
guarded UWP byproduct folders, preserving delivery packages, symbols, certificates and dependencies.

The separate source-function archive probe is:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ./plan/tests/package-layout-probe.ps1
```

It runs 15 inert archive cases without executing the release script or changing versions. Exit 0 means
all cases passed, 1 means an assertion failed, 2 means setup/parser/extraction/cleanup failure.
`pwsh -NoProfile -File ./plan/tests/package-layout-probe.ps1` is the PowerShell Core equivalent.
This optional gate is separate from the 43 directory + seven logical legacy probes (50 logical /
51 executable stages). The complete offline command is `bash plan/tests/run-all.sh --tools-root /absolute/path/to/probe-tools`
on Linux with Bash >=4/Python >=3.9; see [DEVELOPMENT.md](../docs/DEVELOPMENT.md#6-探针离线行为测试) for exact SDK setup,
selectors, reports and partial-vs-complete results. It cannot replace the Windows release gates.
Fixtures do not establish a valid signed MSIX, installed launch or converter behavior. Before an
authorized release, run the actual Windows UWP/XAML compile, full version-bumping build, inspect the
selected real Packages archive, install and launch it, then complete affected device journeys.

Verify an installed package before blaming the source:

```powershell
$p = (Get-AppxPackage -Name BilibiliUWPLiter).InstallLocation
Get-ChildItem $p -Recurse -Filter *.exe | Select-Object FullName, Length
```

`<loc>\BiliLiter.exe` = 8192 with `<loc>\entrypoint\BiliLiter.exe` = ~4.28 MB is the good shape. A 4.28 MB exe at the root with no `entrypoint/` cannot start this app.

### Signing and installation certificate prerequisites

Before an authorized release, inspect the certificate actually selected by
`PackageCertificateThumbprint` in both the UWP csproj and Packages wapproj in the target Windows
certificate store. Check its validity interval, accessible signing private key and publisher match
against the package manifest. Verify the emitted MSIX signature and ensure the certificate supplied
to installers matches that actual signer. Do not assume the tracked `BiliLite.UWP.cer` is the active
signing certificate or that its dates describe the selected Windows-store certificate.

Without the Windows store/private-key and actual-package evidence, active signing readiness is
BLOCKED; a repository certificate inspection alone is not a current signing failure reproduction.
Reconciliation of certificate material, selected thumbprints, keys or package identity belongs to a
maintainer's authorized release preparation. See [ROADMAP](../docs/ROADMAP.md) for the recorded
tracked-certificate discrepancy; do not replace protected signing material as a documentation fix.

### Fast compile loop

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build-release-x64.ps1 -Fast -NoVersionBump
```

- `-Fast` builds only `src/BiliLite.UWP/BiliLite.UWP.csproj` and skips MSIX packaging, which takes the run to about a minute (measured 54-58 s on this machine versus roughly 120 s for the full pipeline). It still runs the C# compiler, the XAML compiler, and PRI generation, so it catches `CS####`, `WMC####`, and `CompileXaml` failures.
- `-Fast` skips the NuGet restore; add `-Restore` when packages changed. NuGet restore, packaging, and the WebApi/ApiTester/Win32Tools projects are only covered by the plain run.
- Keep `-NoVersionBump` here: a compile loop must not burn a package revision per iteration.
- A green fast build is not a release check. Run the plain script once before publishing.

## Watch for a new push

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\watch-remote.ps1 -Fetch
```

`watch-remote.ps1` blocks until the watched remote branch moves, then exits, which makes it usable as a "wake me on the next push" gate. It polls `git ls-remote`, so it never touches local refs or the working tree; `-Fetch` downloads just that ref into `FETCH_HEAD` to print the incoming commits, still without changing the checked-out branch.

- Defaults: remote `origin`, the current branch, a 5 min interval, and no timeout. `-IntervalSeconds`, `-TimeoutSeconds`, `-Remote` and `-Branch` override them. 5 min keeps a long hand-off gate cheap; lower `-IntervalSeconds` when the remote is expected to answer within seconds.
- Exit codes: `0` a new commit appeared, `2` the timeout expired with no change, `3` the branch was deleted.
- Output: the baseline SHA in the first line, then silence until the SHA changes. Unreachable remotes retry instead of exiting and warn at most once a minute.
- Pull and rebuild only after it reports a change; the script does not pull on its own.

## Encoding: keeping the Chinese output readable

Windows PowerShell 5.1 writes standard output in the console code page (936/GBK on this machine), so the Chinese lines become mojibake the moment MSYS/git-bash, `grep` or a log reader treats them as UTF-8. Two layers fix it:

1. **In the script** (already done): `build-release-x64.ps1` sets `[Console]::OutputEncoding`, `[Console]::InputEncoding` and `$OutputEncoding` to a BOM-less `UTF8Encoding` before writing anything, inside a `try` because a host without a console handle throws. Its own `Write-Host` text and the MSBuild output it forwards are then UTF-8, so `... | grep 快速编译完成` works with no decoder in between.
2. **When calling PowerShell ad hoc**, do the same in the command instead of post-processing:

```powershell
powershell -NoProfile -Command "[Console]::OutputEncoding=[Text.Encoding]::UTF8; & .\build-release-x64.ps1 -Fast -NoVersionBump"
```

Reading output that was already written in GBK needs a decoder, not a code change:

```bash
iconv -f GB18030 -t UTF-8 -c build.log | grep -a "error"
```

`-c` is not optional: one invalid byte aborts `iconv`, the pipe closes, and the producing PowerShell process dies mid-build - which is how an earlier packaging run appeared to "fail" while its log was truncated. Related rules learned the hard way: grep an ASCII marker (`error`, `msix`, `CS2012`) rather than a Chinese literal when the encoding is unknown; write logs to a stable Windows path (`H:/tmp/build.log`) because the MSYS `/tmp` does not survive between tool calls; and never trust a pipeline's exit status when the last stage is `grep`/`iconv` (`${PIPESTATUS[0]}` or a marker file instead).

`pwsh` (PowerShell 7) defaults to UTF-8 and needs none of this; the wrapper above is only for `powershell.exe`.

## Read the errors

- `error CS####` lines are real compiler errors. Fix those first.
- `error MSB4181: the "CompileXaml" task returned false but did not log an error` and `XamlCompiler error WMC####` are usually cascades of the C# errors in the same output. They disappear once the compile errors are fixed; do not treat them as separate defects.
- A successful full run prints the Packages-only `生成产物:` list, validates its layout, then prints `交付包:`. Never treat an intermediate output line as final success.
- Warnings that are expected in this tree and are not failures:
  - `WMC1500` `DropShadowPanel` is deprecated
  - `WMC1506` OneWay bindings without change notification
  - `PRI257` resources found for `en-us,zh-cn` but not the default language
  - `NETSDK1138` `netcoreapp3.0` is out of support (`BiliLite.WebApi`)
- The script writes its progress messages in Chinese; through a pipe they can appear as mojibake. Trust the exit code and the `error` lines, not the progress text.
- If the build fails with errors that do not match the current sources, rerun with `-Clean` to rebuild instead of reusing stale `obj`/`bin` output.

## Fix at the source

- Reproduce with the script first, then change the smallest thing that removes the cause. Do not suppress the diagnostic or edit generated files.
- Keep every caller consistent with the fix; if a signature or accessibility changes, update all call sites in the same commit.
- One recurring class of failure here: `CS0122` "`X` is inaccessible due to its protection level" when a derived class in the same file calls a helper on its base class. Widen the base member to `protected` (or `protected static` for a static helper) and leave helpers that only the base class uses as `private`. This is what `Modules/Player/Playurl/BiliPayUrlRequest.cs` needed: `BiliVideoPlayUrlRequest` and `BiliSeasonPlayUrlRequest` call `NewRequestId` and `LogPlayurl` on `BiliPlayUrlRequest`.

## Verify

- Re-run the same command. A real verification is the full pipeline: NuGet restore, C# compile, XAML compile, PRI generation, and MSIX packaging.
- Check the selected Packages delivery file on disk (`ls -l` on its `.msix` path) and report their sizes, not just the exit code. The filenames must carry the version the build just bumped to; a package still at the previous version means the run used `-NoVersionBump` by mistake.

## Commit and push

- Commit the source fix together with the manifest bump the build made; say so in the commit message. Do not `git add -A` blindly: large binary assets (fonts, `ffmpeg.7z`, splash images) must never be rewritten or dropped by a stray add.
- Push to `dev` (`git push origin dev`), which tracks `origin/dev`. `master` is a separate ancestor branch; do not push build fixes there.
- In the summary, state the exact error that was fixed, the change, the verification command, and the emitted `.msix` paths with their version.
