# Build tooling — the `CS2012` file-in-use retry

`build-release-x64.ps1` has carried a `CS2012` retry since the 1.0.9.12 packaging work. This note
records what actually holds the file, because the previous explanation ("MSBuild node reuse") was a
guess and the fixed `Start-Sleep 3` it justified failed twice in one session.

## Symptom

```
BiliLiter.exe(…,intermediatexaml): error CS2012: Cannot open '...obj\x64\Release\intermediatexaml\BiliLiter.exe'
  for writing -- 'The process cannot access the file because it is being used by another process.'
```

## Measured, not guessed

A Restart Manager probe (`rstrtmgr.dll` → `RmRegisterResources` / `RmGetList`) polled the path every
200 ms during a real `-Fast` build on this machine:

```
[22:14:07.492] holder pid=21588 app='csc'
[22:14:07.964] holder pid=22176 app='MSBuild.exe'  cmd=… BiliLite.UWP.csproj -t:Build … -p:UseSharedCompilation=false
```

and, during a later build, `pid=10492 app='MSBuild.exe'` alone. When no build runs the same probe
reports nothing and an exclusive `File.Open` succeeds.

So two processes inside **one** build hold the same path: the `csc.exe` child that is writing it, and
the parent `MSBuild.exe`, which opened the pass-1 assembly earlier in the XAML two-pass compile.

## Why it breaks and why a sleep alone does not fix it

- `-p:UseSharedCompilation=false` makes the compiler a normal `csc.exe` child instead of the
  `VBCSCompiler` server, so the write happens in a separate process while the parent still maps the
  previous copy of the same file.
- `intermediatexaml\BiliLiter.exe` is written by the XAML pre-compile pass and read by the markup
  pass; any second consumer of the project (a re-run of the script, a visual Studio build, the second
  packaging project) widens that window from "briefly" to "guaranteed".
- A fixed `Start-Sleep` cannot close a handle that belongs to a process which is still alive; it only
  works by accident when the holder happened to be exiting.

## What the script does now

1. `Get-FileHolders` asks Restart Manager who has the locked path (no `handle.exe`, no elevation).
2. The warning names the holders instead of assuming `BiliLiter.exe` and a product-wide kill list.
3. `Wait-FileReleased` waits up to `-LockWaitSeconds` (default 90) for the handles to disappear and an
   exclusive open to succeed.
4. Only if that times out does it kill the holders **that are build tools** (`MSBuild`, `csc`,
   `VBCSCompiler`, `XamlCompiler`, `MSBuildTaskHost`), then waits again.
5. `-LockRetryDelay` (default 3 s) pauses before the retry, because antivirus/indexing can attach to a
   4 MB image the moment the last handle drops.
6. The retry is still `-t:Rebuild`, never `Build`: a half-written `intermediatexaml` gets packaged
   into the MSIX otherwise and the installed app stops at the splash screen (BR-15).

## Verification

- `build-release-x64.ps1 -Fast -NoVersionBump` → `快速编译完成`, 0 errors, with the new probe and wait
  path compiled into the script.
- The probe itself was exercised three ways: it named `csc`/`MSBuild.exe` mid-build, it named a sole
  `MSBuild.exe` during another build, and it reported `no holders` on an idle tree where an exclusive
  open succeeded.

## If it ever fails twice in a row

The race is inside the UWP XAML targets, not in this script. Serialize the two consumers: run
`build-release-x64.ps1 -Fast -NoVersionBump` first (app project only), then the full packaging run.
