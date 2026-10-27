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

## Read the errors

- `error CS####` lines are real compiler errors. Fix those first.
- `error MSB4181: the "CompileXaml" task returned false but did not log an error` and `XamlCompiler error WMC####` are usually cascades of the C# errors in the same output. They disappear once the compile errors are fixed; do not treat them as separate defects.
- A successful run prints `-> Z:\BiliLite.UWP_<version>_x64_Test\..._x64.msix` and `-> Z:\BiliLite.Packages_<version>_x64_Test\..._x64.msix`, followed by a `生成产物:` list.
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
- Check the produced files on disk (`ls -l` on the two `.msix` paths) and report their sizes, not just the exit code. The filenames must carry the version the build just bumped to; a package still at the previous version means the run used `-NoVersionBump` by mistake.

## Commit and push

- `git add -A`, then commit the source fix. The build's manifest bump is part of the build, so it belongs in that commit; say so in the commit message.
- Push to `work` (`git push origin work`), which tracks `origin/work`. `master` is a separate ancestor branch; do not push build fixes there.
- In the summary, state the exact error that was fixed, the change, the verification command, and the emitted `.msix` paths with their version.
