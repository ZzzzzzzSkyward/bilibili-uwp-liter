# Runnable Windows fifty-cycle protocol

This is a collection procedure, not native results. The MR1/MR2 correction is released by its author for independent re-review: the inline and standalone forms share the same checked-CLR-getter core and eleven-phase contract, including prewarm. Native execution is BLOCKED on the current Linux host. Read [requirements.md](requirements.md) for fixture/schema/acceptance; [evidence.md](evidence.md) records actual tools and source experiments. No installed package name, app ID, executable path or PID is assumed below.

## 1. Preflight and bind the real target

Use the actual installed, launchable **Packages** delivery artifact, preserving its hash/version and matching source/build record. Do not rebuild or install just to take a baseline, change identities, assume an app-project MSIX launches, or mix Debug/Release/architecture across comparison runs. The approved compile command, when independently needed and available, is `powershell -NoProfile -ExecutionPolicy Bypass -File .\build-release-x64.ps1 -Fast -NoVersionBump -Restore`; it does not create the installable delivery artifact. This protocol does not require a build change.

Run these read-only commands in Windows PowerShell 5.1 and save their output in the fresh run directory:

```powershell
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
$PSVersionTable
Get-CimInstance Win32_OperatingSystem | Select-Object Caption,Version,BuildNumber,TotalVisibleMemorySize
Get-CimInstance Win32_Processor | Select-Object Name,NumberOfLogicalProcessors
Get-CimInstance Win32_VideoController | Select-Object Name,DriverVersion
Get-Command Get-AppxPackage,Get-AppxPackageManifest,Get-Process -ErrorAction Stop
Get-AppxPackage | Select-Object Name,PackageFullName,PackageFamilyName,InstallLocation
Get-CimInstance Win32_Process | Select-Object ProcessId,Name,ExecutablePath
```

Select the package by its **observed exact** PackageFullName and the running app process by the package/application/Task Manager Details relationship; record the selection method. Never choose `ApplicationFrameHost`, a short-lived package launcher or an unrelated process merely because its title/name looks plausible. The executable is observed, not derived by guessing a name from the repository. The variables below mean values taken from that real inventory:

```powershell
$selectedPackage = @(Get-AppxPackage | Where-Object { $_.PackageFullName -eq $observedPackageFullName })
if ($selectedPackage.Count -ne 1) { throw 'Package binding is ambiguous or absent.' }
$installedManifest = $selectedPackage[0] | Get-AppxPackageManifest
$installedManifest.Package.Applications.Application | Select-Object Id,Executable,EntryPoint
$targetProcess = Get-Process -Id $observedProcessId -ErrorAction Stop
$targetProcess | Select-Object Id,ProcessName,StartTime,Path
$boundStartUtc = $targetProcess.StartTime.ToUniversalTime()
Get-FileHash -Algorithm SHA256 -LiteralPath $observedDeliveryArtifactPath
```

Resolve the observed process path under that package's InstallLocation and compare the manifest application/executable plus runtime-loaded executable as applicable. Record the selected manifest App Id, package full/family names, PID, exact start UTC and path. If path/module/manifest access is denied, preserve the exact error and use a permitted elevated read-only inspection or matching Visual Studio/Task Manager process details; do not label an unverified binding as verified. If no launchable app/runtime/device exists, stop native collection as BLOCKED while retaining source investigation.

Record the fixed fixture metadata listed in requirements, including actual runtime/configuration, engine, display size/scaling, account label, media list hash, capture switches and power state. Default API response shape/sample capture is off, but use the same recorded setting for every compared run; ordinary log writing remains on. Start with no video tabs. Avoid opening Settings/diagnostics during the timed sequence. Use its existing issue marker before warm-up and its export after collection if log correlation is needed.

## 2. Executable process-checkpoint fallback

The following independent Windows PowerShell 5.1-compatible block requires only `Get-Process` and .NET process properties. It needs no localized performance-counter names or app code. Set `$observedProcessId` from the verified binding above and `$runDirectory` to a dedicated **existing fresh** output directory with `metadata.json`. It never calls GC, trims memory, changes process state or launches playback. The scratch standalone version with explicit parameters is `/workspace/work/video-memory/measurement/Measure-Checkpoint.ps1`; portable PowerShell parsing passed, Windows execution remains untested.

```powershell
# Run once in the measurement shell after binding the actual process.
$targetProcessId = [int]$observedProcessId
$expectedStartUtc = $boundStartUtc # retain the verified start; do not silently rebind a reused PID
$runId = [Guid]::NewGuid().ToString('N') # also put this exact value in metadata.json
$checkpointSequence = 0
if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw 'Windows required.' }
if (-not (Test-Path -LiteralPath $runDirectory -PathType Container)) { throw 'Missing dedicated run directory.' }
if (Test-Path -LiteralPath (Join-Path $runDirectory 'checkpoints.ndjson')) { throw 'Use a fresh run directory.' }

function Read-RequiredClrProperty {
    param([object]$Target, [string]$PropertyName, [type]$ContractType = $null)
    if ($null -eq $Target) { throw [InvalidOperationException]::new('Required CLR target is null: '+$PropertyName) }
    $propertyType=$Target.GetType()
    if ($null -ne $ContractType) { $propertyType=$ContractType }
    $property=$propertyType.GetProperty($PropertyName)
    if ($null -eq $property) { throw [InvalidOperationException]::new('Required CLR property is unavailable: '+$PropertyName) }
    # Reflection invokes the CLR getter; PowerShell adapted-property reads can hide a thrown getter as null.
    $value=$property.GetValue($Target, $null)
    if ($null -eq $value) { throw [InvalidOperationException]::new('Required CLR property returned null: '+$PropertyName) }
    return ,$value # preserve an empty collection as a value, rather than pipeline-enumerating it away
}

function Save-MemoryCheckpoint {
    param(
        [ValidateRange(0,50)][int]$Cycle,
        [ValidateSet('prewarm','baseline','opened','post-close','settled','idle60','idle300','background','minimized','restored','feed-step')][string]$Phase
    )
    $script:checkpointSequence++
    $row = [ordered]@{
        schemaVersion=1; runId=$runId; sampleSequence=$script:checkpointSequence
        utc=[DateTimeOffset]::UtcNow.ToString('o')
        monotonicTicks=[Diagnostics.Stopwatch]::GetTimestamp()
        stopwatchFrequency=[Diagnostics.Stopwatch]::Frequency
        cycle=$Cycle; phase=$Phase; processId=$targetProcessId; processStartUtc=$null
        privateBytes=$null; workingSetBytes=$null; handleCount=$null; threadCount=$null; cpuTotalMs=$null
        status='ok'; errors=@()
    }
    $fatal=$false
    try {
        $p=Get-Process -Id $targetProcessId -ErrorAction Stop
        $p.Refresh()
        $actualStartUtc=(Read-RequiredClrProperty $p 'StartTime').ToUniversalTime()
        $row.processStartUtc=$actualStartUtc.ToString('o')
        if ($actualStartUtc.Ticks -ne $expectedStartUtc.Ticks) {
            $row.status='process-replaced'; $row.errors+= 'Target start time changed.'; $fatal=$true
        } else {
            foreach ($metric in @('privateBytes','workingSetBytes','handleCount','threadCount','cpuTotalMs')) {
                try {
                    switch ($metric) {
                        'privateBytes' { $raw=Read-RequiredClrProperty $p 'PrivateMemorySize64' }
                        'workingSetBytes' { $raw=Read-RequiredClrProperty $p 'WorkingSet64' }
                        'handleCount' { $raw=Read-RequiredClrProperty $p 'HandleCount' }
                        'threadCount' {
                            $threads=Read-RequiredClrProperty $p 'Threads'
                            $raw=Read-RequiredClrProperty $threads 'Count' ([System.Collections.ICollection])
                        }
                        'cpuTotalMs' {
                            $cpuTime=Read-RequiredClrProperty $p 'TotalProcessorTime'
                            $raw=Read-RequiredClrProperty $cpuTime 'TotalMilliseconds'
                        }
                    }
                    # Getter exceptions/null have already failed this field, before any numeric cast.
                    if ($metric -eq 'cpuTotalMs') { $row[$metric]=[double]$raw }
                    else { $row[$metric]=[int64]$raw }
                } catch {
                    $row.status='partial'
                    $row.errors+= ($metric+': '+$_.Exception.GetBaseException().GetType().FullName)
                }
            }
        }
    } catch { $row.status='process-unavailable'; $row.errors+= $_.Exception.GetBaseException().GetType().FullName; $fatal=$true }
    $line=$row | ConvertTo-Json -Depth 6 -Compress
    $outputPath=Join-Path $runDirectory 'checkpoints.ndjson'
    $writer=New-Object -TypeName System.IO.StreamWriter -ArgumentList @($outputPath,$true,(New-Object System.Text.UTF8Encoding($false))) -ErrorAction Stop
    try { $writer.WriteLine($line) } finally { $writer.Dispose() }
    Write-Output $line
    if ($fatal) { throw 'Measurement stopped; inspect the preserved identity/error row and begin a new run.' }
}

Save-MemoryCheckpoint 0 'prewarm'
# Perform five uncounted warm-up cycles and sixty seconds foreground idle.
Save-MemoryCheckpoint 0 'baseline'
# During cycle 5: sample after thirty seconds actual playback, immediately after close,
# then after ten seconds in the starting view; do the same for cycles 10,25,50.
Save-MemoryCheckpoint 5 'opened'
Save-MemoryCheckpoint 5 'post-close'
Save-MemoryCheckpoint 5 'settled'
# Replace 5 with the actual 10/25/50 checkpoint at the corresponding action.
# After closing cycle 50 and its settled sample, remain foreground with no media:
# Save-MemoryCheckpoint 50 'idle60'
# Save-MemoryCheckpoint 50 'idle300'
```

The shared helper invokes CLR getters through reflection and rejects null before a numeric cast; getter exceptions become null plus a field-specific error and partial status, while genuine zero/empty thread collections remain valid. This avoids PowerShell adapted-property access hiding a throwing getter as null and converting it to zero. The standalone interface accepts the same eleven phases as the inline function, including prewarm; invalid phases fail before process access/output. Portable controlled checks of both exact extracted cores passed; this does not validate Windows PowerShell 5.1 or native collection.

Calls are placed at the observed actions, **not executed consecutively as if they operated the UI**. Measure the sixty-/three-hundred-second idle from the final close; leave the app foreground, using a second shell/monitor for commands if needed. This block captures sparse checkpoints. A reviewed repository collector should add two-second continuous sampling and explicit action markers under the requirements schema, with independent sampler output/read-only metadata. Sparse fallback reports its lower resolution. Record failed field reads as null+error, never fill them with zero.

## 3. Perform exactly one journey per fresh-process run

Prepare a list before timed collection: one reference media/part repeated fifty times for S, or fifty distinct fixed media/part entries for D. Choose a stable entry route that does not page a growing feed/search list; use an existing playlist/history/local preparation route and keep its initial model/list depth identical. Record the route because a retained fifty-item playlist is deliberate input. Avoid changes to account, network/proxy, viewport, engine, quality and subtitle/danmaku settings. Disable autoplay next-item. Select media long enough for the timed segment. Reopening a video may restore history: seek/reset to the recorded position and mark seek completion before starting the playback stopwatch.

1. Start a fresh observed process, bind identity and start checkpoint recording. Collect `prewarm` with no player.
2. Open/watch thirty actual seconds/ordinary-close the reference video five times, waiting ten seconds after each close. Wait sixty seconds in the exact starting view; collect `baseline` (warm0).
3. For cycles 1–50: record cycle/media/open request; allow up to sixty seconds for actual playback. Record media-start and actual engine/quality. Watch thirty seconds after seek/reset completes; sample `opened` at cycles 5/10/25/50. Close through the actual tab close/back path that removes the video owner. Observe the tab/player surface gone and audio stopped; record close action and return to starting view. Sample `post-close` at those checkpoints, wait ten seconds, and sample `settled`. Record `completedCycles`; failed/time-out cycles remain failed attempts. Do not quietly replace the input and still call it the same fixture.
4. After cycle 50, remain foreground on the starting view. Collect idle60 and idle300, count remaining tabs/player surfaces, and record any unsettled history/loading/error state. Do not minimize, navigate Settings or force collection during this idle.
5. Stop measurement; record final completeness/status, then use the existing log/diagnostic export and retain session ID. Hash outputs/tool/artifact, write deviations and source/tool binding. A crash/restart/PID reuse ends this run as incomplete.

For I, keep the same visible shell idle for the elapsed duration of the matched completed S/D run; take corresponding elapsed checkpoints. For F, use fifty predeclared scroll/page steps with the same time intervals, no media, and record depth/model counts when observable. For SD/DD, repeat the exact S/D list/timings with danmaku visible; **hidden danmaku in current source still loads segments**, so call the S/D control rendering-off rather than data-free. Optional local media with no danmaku tracks is a different-media control, never a direct same-network substitute.

For A, keep one video intentionally open and playing while switching to a fixed non-player view for a matched run duration, then explicitly close it and take idle points. Retention of its owner/media before close is the positive control. M minimizes one active player for sixty seconds, restores and verifies the same playback owner; repeat fifty transitions if investigating that case. Record visibility, suspend/resume and existing GC log markers. Minimize does not mean close; existing suspension work includes forced collection and changes the experiment. Do not use M results to explain S/D without further evidence.

Screen I/F/S/D/SD/DD once; run A for owner interpretation and M for a suspected background case. Repeat the suspicious journey and closest control three times with fresh processes, same recorded disk-cache policy and alternating run order. Report individual raw points, early/late slopes, idle behavior, repeat medians/ranges and deviations. No numerical leak verdict from an arbitrary MB limit. A reproducible late positive private-byte slope is reason to attribute; rising working set by itself is insufficient.

## 4. Attribute managed, native/media and GPU retention

Do a separate, explicitly intrusive diagnostic repeat on the **same source/artifact configuration and fixture** so profiler overhead/pauses/collections do not contaminate the uninstrumented process trend. Rebind the new PID/start time. Preserve profile session files rather than screenshots. Record any profiler-driven collection and whether a snapshot shows live retained objects or allocated-but-uncollected objects.

Visual Studio 2022 with UWP workload/appropriate Windows SDK is the preferred managed-root path. Select the actual installed application/project target or attach to the verified running app; use the supported managed or mixed debugger mode for that actual runtime. In Diagnostic Tools enable Memory Usage/heap profiling as available; native/mixed profiling may require restarting the session. Capture matched warm0, settled5/10/25/50 and idle300 snapshots (and a matched active-owner snapshot for A). Compare object type counts/retained bytes and inspect Paths to Root with external/framework filters visible when needed. Record specific old closed VideoDetail/Season/LocalPlayer/PlayerControl/VM/session/task/timer/event roots and the owner that outlives them. Count actual MediaPlayer/FFmpeg/danmaku objects only if exposed. Do not add inclusive sizes or label all objects awaiting an outstanding network response permanent leaks.

If managed snapshot/attach is unsupported, preserve VS version/runtime/target/exact error and set managed attribution unavailable. Current x64 source config disables .NET Native, but the effective installed binary still needs verification. A differently configured diagnostic build is separate corroboration, never a silently equivalent Release measurement. Do not assume `dotnet-gcdump`, `dotnet-counters`, SOS or a .NET8 probe can attach to this UWP runtime.

For native allocation attribution, first inspect available local WPR/WPA tooling without starting or cancelling anyone else's recording:

```powershell
Get-Command wpr.exe,wpa.exe -ErrorAction Continue
wpr.exe -status
wpr.exe -profiles
wpr.exe -help heap
wpr.exe -help start
```

WPR command-line exists on supported Windows versions; WPA/graph analysis may require Windows Performance Toolkit from the ADK. If absent or permission denied, save the error and mark the specific attribution unavailable. Do not invent a profile/provider from another machine. With a verified target executable leaf, package full name and manifest app ID, elevated diagnostic collection can use the documented Store-app HeapTracingConfig form **before launching the measured process**:

```powershell
$heapBinding = @('-HeapTracingConfig', $observedExecutableLeaf, $observedPackageFullName, $observedManifestAppId)
wpr.exe @heapBinding                 # save the preexisting configuration
# Continue only with the Heap profile present and no unrelated recording active.
wpr.exe @heapBinding enable
# Restart/launch the same target through its normal route, then verify its new PID/start.
wpr.exe -start Heap -filemode
# Perform the separately marked fixture; monitor available disk space/trace size.
wpr.exe -stop (Join-Path $runDirectory 'native-heap.etl')
# Restore the saved preexisting configuration; if it was disabled before this investigation:
wpr.exe @heapBinding disable
```

Keep the trace/configuration cleanup in the operator's `try/finally` workflow: stop only the recording started by this run and restore only settings changed by this run, including failure/cancellation. The package field is **full name**, not family-name AUMID. Do not blindly disable a preexisting diagnostic setting or issue `wpr -cancel` against an unrelated recording. This procedure is documented, not executed here. Heap tracing adds overhead and can produce large files; use the full fifty-cycle diagnostic run if practical, otherwise a predeclared shorter reproduction window whose outstanding-allocation evidence is bounded to allocations observed in that trace. A trace started mid-session cannot reconstruct all prior allocations.

Open ETL in WPA, verify target PID/start, provider/event loss and symbols, then compare outstanding native allocation counts/bytes and allocation stacks across matched intervals and after idle. Record module versions and matching app/native PDB availability; public symbols alone do not name every FFmpeg/SYEngine allocation. Heap tracing does not cover all VirtualAlloc/custom allocators/media/GPU memory. If private bytes rise but the heap does not explain them, discover the local VAlloc/resource profiles and repeat only supported collection, preserving its configuration/command. Do not compute “native = private bytes − managed heap”; these categories/time bases overlap or leave gaps.

For GPU/media surfaces use the locally available Task Manager dedicated/shared GPU process counters or an actually enumerated GPU/media WPR profile with the required provider, marking tool/units/PID/support. Graph/page/surface counts may require another supported graphics tool. If unavailable, record null+reason and keep GPU attribution unresolved. CPU private bytes, normal heap traces and shared GPU memory do not sum into a complete device RAM account. OS handle/thread curves are supporting signals; identify handle types/native stacks before attributing growth to a page or player.

## 5. Minimal result table

Each run reports warm0/opened/closed/settled/idle points at 0/5/10/25/50; exact elapsed time and attempted/completed counts; private bytes/working set/handles/threads/CPU; retained root/native allocation/GPU data only when actually collected; count/status of tabs, active players and pending operations where observable. Include source/artifact/process/tool hashes, repeat summaries and failures.

Conclusion choices are bounded: no reproduced growth under this fixture; reproducible process trend awaiting attribution; attributed old-owner/native/GPU retention; or before/after improvement for an independently repaired case. Unsupported tools or incomplete runs remain explicit, not zero values or a passing Windows gate. No source-only test closes BR-21 or the global background-player measurement gate.
