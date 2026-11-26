<#
.SYNOPSIS
Read-only external process sampler and operator marker. Targets Windows PowerShell 5.1.
.DESCRIPTION
Prepare metadata outside a NEW run directory. ValidateMetadata never creates outputs.
Collect and Checkpoint require Windows; Mark records operator observations, not UI verification.
See document/VIDEO_MEMORY.md for the metadata contract.
.EXAMPLE
.\tools\measure-video-memory.ps1 -Mode ValidateMetadata -MetadataPath C:\captures\fixture.json
.EXAMPLE
.\tools\measure-video-memory.ps1 -Mode Collect -MetadataPath C:\captures\fixture.json -RunDirectory C:\captures\run1 -TargetProcessId 1234 -ExpectedProcessStartUtc '2026-11-25T12:00:00.0000000Z' -IntervalSeconds 2 -DurationSeconds 2400
.EXAMPLE
.\tools\measure-video-memory.ps1 -Mode Checkpoint -MetadataPath C:\captures\fixture.json -RunDirectory C:\captures\run1 -TargetProcessId 1234 -ExpectedProcessStartUtc '2026-11-25T12:00:00.0000000Z' -Cycle 0 -Phase prewarm
.EXAMPLE
.\tools\measure-video-memory.ps1 -Mode Mark -MetadataPath C:\captures\fixture.json -RunDirectory C:\captures\run1 -TargetProcessId 1234 -ExpectedProcessStartUtc '2026-11-25T12:00:00.0000000Z' -Cycle 1 -Phase settled -Action cycle-complete -CompletedCycles 1 -Status ok -Note 'Operator observed starting view, no video surface/audio.'
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][ValidateSet('Collect','Checkpoint','Mark','ValidateMetadata')][string]$Mode,
    [Parameter(Mandatory=$true)][string]$MetadataPath,
    [string]$RunDirectory,
    [ValidateRange(1,2147483647)][int]$TargetProcessId,
    [string]$ExpectedProcessStartUtc,
    [ValidateRange(0,50)][int]$Cycle = 0,
    [ValidateSet('prewarm','baseline','opened','post-close','settled','idle60','idle300','background','minimized','restored','feed-step')][string]$Phase,
    [ValidateRange(0.1,86400)][double]$IntervalSeconds = 2,
    [ValidateRange(0.1,604800)][double]$DurationSeconds = 3600,
    [ValidateSet('open-request','seek-complete','media-start','close-action','starting-view','cycle-complete','failure','timeout','suspend','resume','observation','run-complete')][string]$Action,
    [ValidateRange(0,50)][int]$CompletedCycles = 0,
    [ValidateSet('ok','failed','timeout','incomplete','active','suspended','unavailable')][string]$Status = 'incomplete',
    [string]$MediaLabel, [string]$MediaPart, [string]$ActualEngine, [string]$ActualQuality,
    [ValidateSet('foreground','background','minimized','restored','unknown')][string]$WindowState = 'unknown',
    [ValidateRange(0,2147483647)][Nullable[int]]$VisibleTabs,
    [ValidateRange(0,2147483647)][Nullable[int]]$ObservedPlayers,
    [string]$Note
)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function ConvertFrom-JsonText {
    param([string]$Json)
    # Core 7.5+ otherwise converts ISO strings to DateTime; 5.1 preserves strings.
    if ((Get-Command ConvertFrom-Json).Parameters.ContainsKey('DateKind')) { return ConvertFrom-Json -InputObject $Json -DateKind String -ErrorAction Stop }
    return ConvertFrom-Json -InputObject $Json -ErrorAction Stop
}
function Get-UtcInstant {
    param([object]$Value)
    if ($Value -isnot [string] -or $Value -notmatch '^\d{4}-\d\d-\d\dT\d\d:\d\d:\d\d(?:\.\d{1,7})?(?:Z|\+00:00)$') { throw 'Expected an explicit UTC ISO8601 string.' }
    return [DateTimeOffset]::Parse($Value, [Globalization.CultureInfo]::InvariantCulture).UtcDateTime
}
function Get-Sha256 {
    param([string]$Path)
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256 -ErrorAction Stop).Hash.ToLowerInvariant()
}
function Assert-Fields {
    param([object]$Value, [string[]]$Fields, [string]$Label)
    if ($null -eq $Value -or $Value -isnot [pscustomobject]) { throw "$Label must be a JSON object." }
    foreach ($name in $Fields) {
        $property=$Value.PSObject.Properties[$name]
        if ($null -eq $property -or $null -eq $property.Value) { throw "$Label.$name is required (use an explicit unavailable observation with reason)." }
    }
}
function Assert-RecordShape {
    param([object]$Value,[string[]]$Fields)
    if ($null -eq $Value -or $Value -isnot [pscustomobject] -or @($Value.PSObject.Properties).Count -ne $Fields.Count) { throw 'Record does not match fixed schema.' }
    foreach ($name in $Fields) { if ($null -eq $Value.PSObject.Properties[$name]) { throw 'Missing record field.' } }
}
function Assert-ScalarString {
    param([object]$Value,[string]$Label)
    # Keep the raw JSON type: a typed [string] parameter would coerce a one-item array.
    if ($Value -isnot [string] -or [string]::IsNullOrWhiteSpace($Value)) { throw "$Label must be a nonempty scalar string." }
}
function Assert-Observation {
    param([object]$Value, [string]$Label)
    if ($Value -is [string]) { if ([string]::IsNullOrWhiteSpace($Value)) { throw "$Label is empty." }; return }
    Assert-Fields $Value @('status','reason') $Label
    Assert-ScalarString $Value.status ($Label+'.status')
    if ($Value.status -ne 'unavailable' -or $Value.reason -isnot [string] -or [string]::IsNullOrWhiteSpace($Value.reason)) { throw "$Label requires unavailable plus a nonempty reason." }
}
function Get-OptionalText {
    param([string]$Value)
    if ([string]::IsNullOrWhiteSpace($Value)) { return $null }
    return $Value
}
function Read-Metadata {
    param([string]$Path)
    $m=ConvertFrom-JsonText (Get-Content -LiteralPath $Path -Raw -Encoding UTF8)
    Assert-Fields $m @('schemaVersion','runId','evidenceLevel','createdUtc','sourceCommit','sourceDirty','artifactPath','artifactSha256','appVersion','configuration','architecture','runtime','package','process','device','fixture','profiler','units','outputs','deviations') 'metadata'
    Assert-ScalarString $m.evidenceLevel 'metadata.evidenceLevel'
    if (($m.schemaVersion -isnot [int] -and $m.schemaVersion -isnot [long]) -or $m.schemaVersion -ne 1 -or $m.runId -isnot [string] -or $m.runId -notmatch '^[A-Za-z0-9][A-Za-z0-9_-]{0,79}$' -or $m.evidenceLevel -cnotin @('E2','E3','E4')) { throw 'Unsupported metadata schema/runId/evidenceLevel.' }
    $null=Get-UtcInstant $m.createdUtc
    if ($m.sourceCommit -isnot [string] -or ($m.sourceCommit -cne 'unknown' -and $m.sourceCommit -notmatch '^[a-fA-F0-9]{40}$')) { throw 'Invalid sourceCommit.' }
    if ($m.sourceDirty -isnot [bool]) { throw 'sourceDirty must be a JSON boolean.' }
    foreach ($name in @('appVersion','configuration','architecture','runtime')) { Assert-Observation $m.$name "metadata.$name" }
    if ($m.artifactPath -isnot [string] -or -not [IO.Path]::IsPathRooted($m.artifactPath) -or $m.artifactSha256 -isnot [string] -or $m.artifactSha256 -notmatch '^[a-fA-F0-9]{64}$') { throw 'Require absolute artifactPath and SHA256.' }
    if ((Get-Sha256 $m.artifactPath) -ne $m.artifactSha256.ToLowerInvariant()) { throw 'Artifact hash mismatch.' }
    Assert-Fields $m.package @('fullName','familyName','appId','executable') 'package'
    foreach ($name in @('fullName','familyName','appId','executable')) { if ($m.package.$name -isnot [string] -or [string]::IsNullOrWhiteSpace($m.package.$name)) { throw "package.$name requires an observed value." } }
    Assert-Fields $m.process @('id','startUtc','path','verificationMethod') 'process'
    if ($m.process.id -isnot [long] -and $m.process.id -isnot [int]) { throw 'process.id must be integer.' }
    if ($m.process.id -lt 1 -or $m.process.id -gt [int]::MaxValue) { throw 'Invalid process.id.' }
    $null=Get-UtcInstant $m.process.startUtc
    foreach ($name in @('path','verificationMethod')) { if ($m.process.$name -isnot [string] -or [string]::IsNullOrWhiteSpace($m.process.$name)) { throw "process.$name requires verified observation." } }
    Assert-Fields $m.device @('osBuild','ram','cpu','gpu','driver','power','display','scaling') 'device'
    foreach ($name in @('osBuild','ram','cpu','gpu','driver','power','display','scaling')) { Assert-Observation $m.device.$name "device.$name" }
    Assert-Fields $m.fixture @('journey','replicate','comparisonGroup','shell','accountLabel','network','mediaListSha256','mediaOrder','requestedEngine','actualEngine','requestedQuality','actualQuality','codec','resolution','frameRate','playbackRate','volume','subtitles','danmaku','autoplay','apiCapture','entryRoute','startingPosition','cacheState','repeatOrder','expectedCycles','playbackSeconds','betweenCloseSeconds','timeoutSeconds','warmupCycles','warmupIdleSeconds') 'fixture'
    Assert-ScalarString $m.fixture.journey 'fixture.journey'
    if ($m.fixture.journey -cnotin @('I','F','S','D','SD','DD','A','M')) { throw 'Unknown journey.' }
    foreach ($name in @('replicate','repeatOrder')) { if ($m.fixture.$name -isnot [int] -and $m.fixture.$name -isnot [long]) { throw "fixture.$name must be integer." }; if ($m.fixture.$name -lt 1) { throw "fixture.$name must be positive." } }
    foreach ($name in @('comparisonGroup','shell','accountLabel','network','requestedEngine','actualEngine','requestedQuality','actualQuality','codec','resolution','frameRate','playbackRate','volume','subtitles','danmaku','autoplay','apiCapture','entryRoute','startingPosition','cacheState')) { Assert-Observation $m.fixture.$name "fixture.$name" }
    if ($m.fixture.mediaListSha256 -isnot [string] -or $m.fixture.mediaListSha256 -notmatch '^[a-fA-F0-9]{64}$' -or $m.fixture.mediaOrder -isnot [array] -or $m.fixture.mediaOrder.Count -ne 50) { throw 'Require the predeclared fifty-item media/step order and its SHA256.' }
    foreach ($item in $m.fixture.mediaOrder) { if ($item -isnot [string] -or [string]::IsNullOrWhiteSpace($item)) { throw 'Each mediaOrder item must be a nonempty operator label.' } }
    # Bind the list without ambiguous platform newlines: UTF8 JSON array, compact, no BOM/newline.
    $listJson=ConvertTo-Json -InputObject @($m.fixture.mediaOrder) -Compress
    $sha=[Security.Cryptography.SHA256]::Create()
    try { $listHash=([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($listJson)))).Replace('-','').ToLowerInvariant() } finally { $sha.Dispose() }
    if ($listHash -ne $m.fixture.mediaListSha256.ToLowerInvariant()) { throw 'mediaOrder SHA256 mismatch (hash compact UTF8 JSON array).' }
    if ($m.fixture.expectedCycles -isnot [array] -or $m.fixture.expectedCycles.Count -ne 50) { throw 'expectedCycles must be integers 1 through 50 in order.' }
    for ($i=0; $i -lt 50; $i++) { if (($m.fixture.expectedCycles[$i] -isnot [int] -and $m.fixture.expectedCycles[$i] -isnot [long]) -or $m.fixture.expectedCycles[$i] -ne ($i+1)) { throw 'expectedCycles must be integers 1 through 50 in order.' } }
    foreach ($name in @('playbackSeconds','betweenCloseSeconds','timeoutSeconds','warmupCycles','warmupIdleSeconds')) {
        $v=$m.fixture.$name
        if (($v -isnot [int] -and $v -isnot [long] -and $v -isnot [double]) -or [double]::IsNaN([double]$v) -or [double]::IsInfinity([double]$v) -or $v -le 0) { throw "fixture.$name must be finite and positive." }
    }
    Assert-Fields $m.profiler @('mode','version') 'profiler'
    Assert-Observation $m.profiler.mode 'profiler.mode'; Assert-Observation $m.profiler.version 'profiler.version'
    Assert-Fields $m.units @('privateBytes','workingSetBytes','handleCount','threadCount','cpuTotalMs') 'units'
    foreach ($name in @('privateBytes','workingSetBytes')) { if ($m.units.$name -isnot [string] -or $m.units.$name -cne 'bytes') { throw "Invalid unit: $name." } }
    foreach ($name in @('handleCount','threadCount')) { if ($m.units.$name -isnot [string] -or $m.units.$name -cne 'count') { throw "Invalid unit: $name." } }
    if ($m.units.cpuTotalMs -isnot [string] -or $m.units.cpuTotalMs -cne 'milliseconds') { throw 'Invalid CPU unit.' }
    Assert-Fields $m.outputs @('samples','checkpoints','events') 'outputs'
    foreach ($name in @('samples','checkpoints','events')) { if ($m.outputs.$name -isnot [string] -or $m.outputs.$name -cne ($name+'.ndjson')) { throw 'Output filenames are fixed relative names.' } }
    if ($m.deviations -isnot [array]) { throw 'deviations must be an array.' }
    foreach ($item in $m.deviations) { if ($item -isnot [string]) { throw 'deviations must contain strings.' } }
    return $m
}

function Read-RequiredClrProperty {
    param([object]$Target, [string]$PropertyName, [type]$ContractType = $null)
    if ($null -eq $Target) { throw [InvalidOperationException]::new('Required CLR target is null: '+$PropertyName) }
    $propertyType=$Target.GetType()
    if ($null -ne $ContractType) { $propertyType=$ContractType }
    $property=$propertyType.GetProperty($PropertyName)
    if ($null -eq $property) { throw [InvalidOperationException]::new('Required CLR property is unavailable: '+$PropertyName) }
    $value=$property.GetValue($Target, $null)
    if ($null -eq $value) { throw [InvalidOperationException]::new('Required CLR property returned null: '+$PropertyName) }
    return ,$value
}
function New-MemorySample {
    param([string]$BoundRunId, [int]$ProcessId, [datetime]$StartUtc, [long]$Sequence)
    $row=[ordered]@{schemaVersion=1; runId=$BoundRunId; sampleSequence=$Sequence; utc=[DateTimeOffset]::UtcNow.ToString('o'); monotonicTicks=[Diagnostics.Stopwatch]::GetTimestamp(); stopwatchFrequency=[Diagnostics.Stopwatch]::Frequency; processId=$ProcessId; processStartUtc=$null; privateBytes=$null; workingSetBytes=$null; handleCount=$null; threadCount=$null; cpuTotalMs=$null; status='ok'; errors=@()}
    $p=$null
    try {
        $p=Get-Process -Id $ProcessId -ErrorAction Stop
        $p.Refresh()
        $actualStart=(Read-RequiredClrProperty $p 'StartTime').ToUniversalTime()
        $row.processStartUtc=$actualStart.ToString('o')
        if ($actualStart.Ticks -ne $StartUtc.Ticks) { $row.status='process-replaced'; $row.errors+= 'Target start time changed.' }
        else {
            foreach ($metric in @('privateBytes','workingSetBytes','handleCount','threadCount','cpuTotalMs')) {
                try {
                    switch ($metric) {
                        'privateBytes' { $raw=Read-RequiredClrProperty $p 'PrivateMemorySize64' }
                        'workingSetBytes' { $raw=Read-RequiredClrProperty $p 'WorkingSet64' }
                        'handleCount' { $raw=Read-RequiredClrProperty $p 'HandleCount' }
                        'threadCount' { $threads=Read-RequiredClrProperty $p 'Threads'; $raw=Read-RequiredClrProperty $threads 'Count' ([System.Collections.ICollection]) }
                        'cpuTotalMs' { $cpu=Read-RequiredClrProperty $p 'TotalProcessorTime'; $raw=Read-RequiredClrProperty $cpu 'TotalMilliseconds' }
                    }
                    if ($metric -eq 'cpuTotalMs') { $value=[double]$raw; if ([double]::IsNaN($value) -or [double]::IsInfinity($value)) { throw 'Nonfinite metric.' } }
                    else { $value=[long]$raw }
                    if ($value -lt 0) { throw 'Negative metric.' }
                    $row[$metric]=$value
                } catch { $row.status='partial'; $row.errors+= ($metric+': '+$_.Exception.GetBaseException().GetType().FullName) }
            }
        }
    } catch { $row.status='process-unavailable'; $row.errors+= $_.Exception.GetBaseException().GetType().FullName }
    finally {
        # Release only this observer's Process wrapper/handles; never close the target process.
        if ($p -is [IDisposable]) {
            try { $p.Dispose() }
            catch { if ($row.status -eq 'ok') { $row.status='partial' }; $row.errors+= ('observerDispose: '+$_.Exception.GetBaseException().GetType().FullName) }
        }
    }
    return $row
}
function Write-NewJsonFile {
    param([string]$Path, [object]$Value)
    $stream=[IO.File]::Open($Path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read)
    $writer=$null
    try { $writer=New-Object IO.StreamWriter($stream,(New-Object Text.UTF8Encoding($false))); $writer.WriteLine(($Value | ConvertTo-Json -Depth 20 -Compress)); $writer.Flush() }
    finally { if ($null -ne $writer) { $writer.Dispose() } else { $stream.Dispose() } }
}
function Open-BoundRun {
    param([object]$Metadata, [string]$MetadataFile, [string]$Directory, [string]$ScriptFile, [string]$ValidatedMetadataHash)
    $directoryPath=[IO.Path]::GetFullPath($Directory)
    $metadataHash=Get-Sha256 $MetadataFile
    if ($metadataHash -ne $ValidatedMetadataHash) { throw 'Metadata changed after validation.' }
    $scriptHash=Get-Sha256 $ScriptFile
    if (-not (Test-Path -LiteralPath $directoryPath)) {
        # New-Item without -Force rejects an existing directory, including a racing creator.
        $null=New-Item -ItemType Directory -Path $directoryPath -ErrorAction Stop
        $destination=Join-Path $directoryPath 'metadata.json'
        $source=[IO.File]::OpenRead([IO.Path]::GetFullPath($MetadataFile)); $target=$null
        try { $target=[IO.File]::Open($destination,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read); $source.CopyTo($target) }
        finally { $source.Dispose(); if ($null -ne $target) { $target.Dispose() } }
        if ((Get-Sha256 $destination) -ne $metadataHash) { throw 'Metadata changed during run initialization; preserve this incomplete directory.' }
        Write-NewJsonFile (Join-Path $directoryPath 'binding.json') ([ordered]@{schemaVersion=1;runId=$Metadata.runId;metadataSha256=$metadataHash;scriptSha256=$scriptHash;scriptVersion='1.0';createdUtc=[DateTimeOffset]::UtcNow.ToString('o');processId=$Metadata.process.id;processStartUtc=$Metadata.process.startUtc;artifactSha256=$Metadata.artifactSha256})
    } else {
        if (-not (Test-Path -LiteralPath $directoryPath -PathType Container)) { throw 'RunDirectory is not a directory.' }
        $binding=ConvertFrom-JsonText (Get-Content -LiteralPath (Join-Path $directoryPath 'binding.json') -Raw -Encoding UTF8)
        Assert-Fields $binding @('schemaVersion','runId','metadataSha256','scriptSha256','processId','processStartUtc','artifactSha256') 'binding'
        Assert-RecordShape $binding @('schemaVersion','runId','metadataSha256','scriptSha256','scriptVersion','createdUtc','processId','processStartUtc','artifactSha256')
        foreach ($field in @('runId','metadataSha256','scriptSha256','scriptVersion','createdUtc','processStartUtc','artifactSha256')) { Assert-ScalarString $binding.$field ('binding.'+$field) }
        foreach ($field in @('schemaVersion','processId')) { if ($binding.$field -isnot [int] -and $binding.$field -isnot [long]) { throw 'Binding integer field has invalid type.' } }
        $null=Get-UtcInstant $binding.createdUtc; $null=Get-UtcInstant $binding.processStartUtc
        if ($binding.scriptVersion -cne '1.0') { throw 'Unsupported binding script version.' }
        if ($binding.schemaVersion -ne 1 -or $binding.runId -cne $Metadata.runId -or $binding.metadataSha256 -ne $metadataHash -or (Get-Sha256 (Join-Path $directoryPath 'metadata.json')) -ne $metadataHash -or $binding.scriptSha256 -ne $scriptHash -or $binding.processId -ne $Metadata.process.id -or $binding.processStartUtc -cne $Metadata.process.startUtc -or $binding.artifactSha256 -cne $Metadata.artifactSha256) { throw 'Run binding changed; use a new directory.' }
    }
    if (Test-Path -LiteralPath (Join-Path $directoryPath 'STOPPED.json')) { Assert-StopRecord (Join-Path $directoryPath 'STOPPED.json'); throw 'Run ended; preserve it and use a new directory.' }
    return $directoryPath
}
function Assert-StopRecord {
    param([string]$Path)
    $record=ConvertFrom-JsonText (Get-Content -LiteralPath $Path -Raw -Encoding UTF8)
    Assert-RecordShape $record @('status','utc','note')
    foreach ($field in @('status','utc','note')) { Assert-ScalarString $record.$field ('terminal.'+$field) }
    $null=Get-UtcInstant $record.utc
    if ($record.status -cnotin @('operator-complete','process-unavailable','process-replaced')) { throw 'Invalid terminal status.' }
}
function Enter-OutputLock {
    param([string]$Path)
    return [IO.File]::Open(($Path+'.lock'),[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
}
function Read-OutputRows {
    param([string]$Path, [object]$Metadata, [string]$Kind)
    $rows=New-Object 'System.Collections.Generic.List[object]'
    if (-not (Test-Path -LiteralPath $Path)) { return ,$rows }
    $stream=[IO.File]::OpenRead($Path)
    try {
        if ($stream.Length -eq 0) { throw 'Empty/partial output stream; preserve this run.' }
        $null=$stream.Seek(-1,[IO.SeekOrigin]::End)
        if ($stream.ReadByte() -ne 10) { throw 'Unterminated output row; preserve this run.' }
    } finally { $stream.Dispose() }
    $previousTicks=[long]-1; $previousUtc=[datetime]::MinValue; $keys=@{}; $previousCycle=-1; $completedEventCount=0
    foreach ($line in [IO.File]::ReadLines($Path)) {
        if ([string]::IsNullOrWhiteSpace($line)) { throw 'Blank/truncated output row; preserve the run.' }
        $r=ConvertFrom-JsonText $line
        $sequenceName='sampleSequence'; if ($Kind -eq 'events') { $sequenceName='sequence' }
        if ($Kind -eq 'events') { Assert-RecordShape $r @('schemaVersion','runId','sequence','utc','monotonicTicks','stopwatchFrequency','cycle','phase','action','completedCycles','mediaLabel','mediaPart','actualEngine','actualQuality','windowState','visibleTabs','observedPlayers','status','note') }
        else {
            $fields=@('schemaVersion','runId','sampleSequence','utc','monotonicTicks','stopwatchFrequency','processId','processStartUtc','privateBytes','workingSetBytes','handleCount','threadCount','cpuTotalMs','status','errors')
            if ($Kind -eq 'checkpoints') { $fields+=@('cycle','phase') }
            Assert-RecordShape $r $fields
        }
        Assert-Fields $r @('schemaVersion','runId',$sequenceName,'utc','monotonicTicks','stopwatchFrequency','status') 'record'
        foreach ($field in @('runId','utc','status')) { Assert-ScalarString $r.$field ('record.'+$field) }
        $instant=Get-UtcInstant $r.utc
        foreach ($field in @('schemaVersion',$sequenceName,'monotonicTicks','stopwatchFrequency')) { if ($r.$field -isnot [int] -and $r.$field -isnot [long]) { throw 'Record integer field has invalid type.' } }
        if ($r.schemaVersion -ne 1 -or $r.runId -cne $Metadata.runId -or $r.$sequenceName -ne ($rows.Count+1) -or $r.monotonicTicks -lt 0 -or $r.monotonicTicks -lt $previousTicks -or $r.stopwatchFrequency -ne [Diagnostics.Stopwatch]::Frequency -or $instant -lt $previousUtc) { throw 'Output record binding/sequence/time changed.' }
        if ($Kind -ne 'events') {
            Assert-ScalarString $r.processStartUtc 'record.processStartUtc'
            foreach ($metric in @('privateBytes','workingSetBytes','handleCount','threadCount','cpuTotalMs')) {
                if ($null -eq $r.PSObject.Properties[$metric]) { throw 'Missing metric field.' }
                $value=$r.$metric
                if ($null -ne $value) {
                    if (($value -isnot [int] -and $value -isnot [long] -and ($metric -ne 'cpuTotalMs' -or $value -isnot [double])) -or [double]::IsNaN([double]$value) -or [double]::IsInfinity([double]$value) -or $value -lt 0) { throw 'Invalid/nonfinite metric.' }
                } elseif ($r.status -eq 'ok') { throw 'Null metric cannot have ok status.' }
            }
            if ($r.errors -isnot [array] -or ($r.status -eq 'partial' -and $r.errors.Count -eq 0) -or ($r.status -eq 'ok' -and $r.errors.Count -ne 0)) { throw 'Invalid metric error contract.' }
            foreach ($errorText in $r.errors) { if ($errorText -isnot [string] -or [string]::IsNullOrWhiteSpace($errorText)) { throw 'Invalid metric error.' } }
            if (($r.processId -isnot [int] -and $r.processId -isnot [long]) -or $r.processId -ne $Metadata.process.id -or $null -eq $r.processStartUtc -or (Get-UtcInstant $r.processStartUtc).Ticks -ne (Get-UtcInstant $Metadata.process.startUtc).Ticks -or $r.status -cnotin @('ok','partial')) { throw 'Previous process failure ends this run.' }
        }
        if ($Kind -in @('checkpoints','events')) {
            Assert-ScalarString $r.phase 'record.phase'
            if (($r.cycle -isnot [int] -and $r.cycle -isnot [long]) -or $r.cycle -lt 0 -or $r.cycle -lt $previousCycle -or $r.cycle -gt 50 -or $r.phase -notin @('prewarm','baseline','opened','post-close','settled','idle60','idle300','background','minimized','restored','feed-step')) { throw 'Out-of-order cycle or invalid phase.' }
            if ($Kind -eq 'checkpoints' -and $rows.Count -gt 0 -and (Get-CheckpointOrder $r.cycle $r.phase) -le (Get-CheckpointOrder $rows[$rows.Count-1].cycle $rows[$rows.Count-1].phase)) { throw 'Out-of-order checkpoint phase.' }
            if ($Kind -eq 'events') {
                Assert-Fields $r @('action','completedCycles','windowState') 'event'
                foreach ($field in @('action','windowState')) { Assert-ScalarString $r.$field ('event.'+$field) }
                foreach ($field in @('mediaLabel','mediaPart','actualEngine','actualQuality','note')) { if ($null -ne $r.$field -and $r.$field -isnot [string]) { throw 'Invalid optional event text.' } }
                foreach ($field in @('visibleTabs','observedPlayers')) { if ($null -ne $r.$field -and (($r.$field -isnot [int] -and $r.$field -isnot [long]) -or $r.$field -lt 0)) { throw 'Invalid optional event count.' } }
                if ($r.windowState -cnotin @('foreground','background','minimized','restored','unknown')) { throw 'Invalid window state.' }
                if ($r.action -notin @('open-request','seek-complete','media-start','close-action','starting-view','cycle-complete','failure','timeout','suspend','resume','observation','run-complete') -or $r.status -notin @('ok','failed','timeout','incomplete','active','suspended','unavailable') -or ($r.completedCycles -isnot [int] -and $r.completedCycles -isnot [long]) -or $r.completedCycles -lt 0 -or $r.completedCycles -gt $r.cycle -or ($rows.Count -gt 0 -and $r.completedCycles -lt $rows[$rows.Count-1].completedCycles)) { throw 'Invalid event status/completeness.' }
                if ($r.action -eq 'cycle-complete' -and ($r.status -ne 'ok' -or $r.cycle -lt 1 -or $r.completedCycles -ne $r.cycle)) { throw 'Invalid completed cycle.' }
                if ($r.action -eq 'cycle-complete') { $completedEventCount++; if ($r.cycle -ne $completedEventCount) { throw 'Missing/duplicate completed cycle event.' } }
                if ($r.action -eq 'run-complete' -and ($completedEventCount -ne 50 -or $r.completedCycles -ne 50 -or $r.cycle -ne 50 -or $r.status -ne 'ok')) { throw 'Incomplete prior run-complete event.' }
            }
            $key=[string]$r.cycle+':'+$r.phase
            if ($Kind -eq 'events') { $key+=':'+$r.action }
            if ($keys.ContainsKey($key)) { throw 'Duplicate output observation.' }; $keys[$key]=$true; $previousCycle=$r.cycle
        }
        $previousTicks=$r.monotonicTicks; $previousUtc=$instant; $rows.Add($r)
    }
    return ,$rows
}
function Write-Record {
    param([string]$Path, [object]$Row)
    # A single owner holds the file-specific lock across read/sequence/write. Flush every row.
    $stream=[IO.File]::Open($Path,[IO.FileMode]::Append,[IO.FileAccess]::Write,[IO.FileShare]::Read)
    $writer=$null
    try { $writer=New-Object IO.StreamWriter($stream,(New-Object Text.UTF8Encoding($false))); $writer.WriteLine(($Row | ConvertTo-Json -Depth 8 -Compress)); $writer.Flush() }
    finally { if ($null -ne $writer) { $writer.Dispose() } else { $stream.Dispose() } }
}
function Assert-NewObservation {
    param([object]$Rows, [int]$NewCycle, [string]$NewPhase, [string]$NewAction)
    foreach ($r in $Rows) {
        if ($r.cycle -gt $NewCycle) { throw 'Cycle moved backwards.' }
        if ($r.cycle -eq $NewCycle -and $r.phase -ceq $NewPhase -and ([string]::IsNullOrEmpty($NewAction) -or $r.action -ceq $NewAction)) { throw 'Duplicate observation.' }
    }
    if ($Rows.Count -gt 0) {
        $last=$Rows[$Rows.Count-1]
        if ([string]::IsNullOrEmpty($NewAction) -and (Get-CheckpointOrder $NewCycle $NewPhase) -le (Get-CheckpointOrder $last.cycle $last.phase)) { throw 'Checkpoint phase moved backwards.' }
        if ([Diagnostics.Stopwatch]::GetTimestamp() -lt $last.monotonicTicks -or [DateTimeOffset]::UtcNow.UtcDateTime -lt (Get-UtcInstant $last.utc)) { throw 'Clock regressed; end this run.' }
    }
}
function Get-CheckpointOrder {
    param([int]$AtCycle,[string]$AtPhase)
    if ($AtCycle -lt 0 -or $AtCycle -gt 50 -or $AtPhase -notin @('prewarm','baseline','opened','post-close','settled','idle60','idle300','background','minimized','restored','feed-step')) { throw 'Invalid checkpoint cycle/phase.' }
    if ($AtPhase -in @('prewarm','baseline')) { if ($AtCycle -ne 0) { throw 'Warm-up checkpoints require cycle zero.' }; if ($AtPhase -eq 'prewarm') { return -2 }; return -1 }
    # Every cycle-50 ordinary/control phase is below 51*20; idle300 follows idle60.
    if ($AtPhase -in @('idle60','idle300')) { if ($AtCycle -ne 50) { throw 'Final idle checkpoints require cycle fifty.' }; if ($AtPhase -eq 'idle60') { return 1021 }; return 1022 }
    $rank=@{'opened'=1;'post-close'=2;'settled'=3;'background'=4;'minimized'=5;'restored'=6;'feed-step'=7}
    return $AtCycle*20+$rank[$AtPhase]
}
function Invoke-MemoryCapture {
    param([object]$Metadata,[string]$Directory,[int]$ProcessId,[datetime]$StartUtc,
        [ValidateSet('Collect','Checkpoint')][string]$CaptureMode,[double]$Cadence,[double]$Duration,
        [int]$AtCycle,[string]$AtPhase)
    $kind='samples'; if ($CaptureMode -eq 'Checkpoint') { $kind='checkpoints'; $null=Get-CheckpointOrder $AtCycle $AtPhase }
    $output=Join-Path $Directory ($kind+'.ndjson'); $lock=$null
    try {
        $lock=Enter-OutputLock $output
        if ($CaptureMode -eq 'Collect' -and (Test-Path -LiteralPath $output)) { throw 'Continuous collection can start only once per fresh run; never resume an interrupted stream.' }
        $rows=Read-OutputRows $output $Metadata $kind
        if ($CaptureMode -eq 'Checkpoint') { Assert-NewObservation $rows $AtCycle $AtPhase '' }
        $timer=[Diagnostics.Stopwatch]::StartNew(); $sequence=[long]$rows.Count; $next=0.0; $previous=$null
        if ($rows.Count -gt 0) { $previous=$rows[$rows.Count-1] }
        do {
            if (Test-Path -LiteralPath (Join-Path $Directory 'STOPPED.json')) { Assert-StopRecord (Join-Path $Directory 'STOPPED.json'); break }
            $sequence++
            $row=New-MemorySample $Metadata.runId $ProcessId $StartUtc $sequence
            if ($CaptureMode -eq 'Checkpoint') { $row.cycle=$AtCycle; $row.phase=$AtPhase }
            if ($null -ne $previous -and ($row.monotonicTicks -lt $previous.monotonicTicks -or (Get-UtcInstant $row.utc) -lt (Get-UtcInstant $previous.utc))) { throw 'Clock regressed; preserve the incomplete run.' }
            Write-Record $output $row
            $previous=[pscustomobject]$row
            if ($row.status -in @('process-unavailable','process-replaced')) {
                Write-NewJsonFile (Join-Path $Directory 'STOPPED.json') ([ordered]@{status=$row.status;utc=[DateTimeOffset]::UtcNow.ToString('o');note='Identity failure; observations preserved, new process requires a new run.'})
                throw 'Target identity failed; inspect the preserved row. Run ended.'
            }
            if ($CaptureMode -eq 'Checkpoint') { Write-Output ($row | ConvertTo-Json -Depth 8 -Compress); break }
            $next+=$Cadence
            # Skip missed slots instead of generating a burst of catch-up observations.
            if ($next -lt $timer.Elapsed.TotalSeconds) { $next=([Math]::Floor($timer.Elapsed.TotalSeconds/$Cadence)+1)*$Cadence }
            if ($next -ge $Duration) { break }
            while ($timer.Elapsed.TotalSeconds -lt $next) { Start-Sleep -Milliseconds ([int][Math]::Min(200,[Math]::Max(1,($next-$timer.Elapsed.TotalSeconds)*1000))) }
        } while ($timer.Elapsed.TotalSeconds -lt $Duration)
        # A duration bounds sampling only. It never proves the operator journey complete.
    } finally { if ($null -ne $lock) { $lock.Dispose(); [IO.File]::Delete($output+'.lock') } }
}

# The executable entry point is deliberately separate from private helpers for controlled tests.
# ValidateSet accepts case variants; store one canonical spelling for duplicate keys/schema.
if (-not [string]::IsNullOrEmpty($Phase)) { $Phase=$Phase.ToLowerInvariant() }
if (-not [string]::IsNullOrEmpty($Action)) { $Action=$Action.ToLowerInvariant() }
$Status=$Status.ToLowerInvariant(); $WindowState=$WindowState.ToLowerInvariant()
$validatedMetadataHash=Get-Sha256 $MetadataPath
$metadata=Read-Metadata $MetadataPath
if ((Get-Sha256 $MetadataPath) -ne $validatedMetadataHash) { throw 'Metadata changed during validation.' }
if ($Mode -eq 'ValidateMetadata') { Write-Output ('Metadata valid: '+$metadata.runId); return }
if ([string]::IsNullOrWhiteSpace($RunDirectory) -or $TargetProcessId -eq 0 -or [string]::IsNullOrWhiteSpace($ExpectedProcessStartUtc)) { throw 'RunDirectory, TargetProcessId and ExpectedProcessStartUtc are required.' }
$expectedStart=Get-UtcInstant $ExpectedProcessStartUtc
if ($TargetProcessId -ne $metadata.process.id -or $expectedStart.Ticks -ne (Get-UtcInstant $metadata.process.startUtc).Ticks) { throw 'Explicit target does not match metadata.' }
if ($Mode -in @('Collect','Checkpoint') -and [Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw 'Windows process measurement required; this host is not Windows.' }
if ($Mode -in @('Checkpoint','Mark') -and [string]::IsNullOrWhiteSpace($Phase)) { throw 'Phase is required.' }
if ($Mode -eq 'Checkpoint') { $null=Get-CheckpointOrder $Cycle $Phase }
if ($Mode -eq 'Mark' -and [string]::IsNullOrWhiteSpace($Action)) { throw 'Action is required.' }
if ($Mode -eq 'Mark' -and $CompletedCycles -gt $Cycle) { throw 'CompletedCycles exceeds attempted cycle.' }
$directory=Open-BoundRun $metadata $MetadataPath $RunDirectory $PSCommandPath $validatedMetadataHash
if ($Mode -in @('Collect','Checkpoint')) {
    Invoke-MemoryCapture $metadata $directory $TargetProcessId $expectedStart $Mode $IntervalSeconds $DurationSeconds $Cycle $Phase
    return
}
$output=Join-Path $directory 'events.ndjson'; $lock=$null
try {
    $lock=Enter-OutputLock $output
    $rows=Read-OutputRows $output $metadata 'events'
    Assert-NewObservation $rows $Cycle $Phase $Action
        if ($rows.Count -gt 0 -and $CompletedCycles -lt $rows[$rows.Count-1].completedCycles) { throw 'CompletedCycles moved backwards.' }
        if ($Action -eq 'cycle-complete' -and ($Status -ne 'ok' -or $Cycle -lt 1 -or $CompletedCycles -ne $Cycle)) { throw 'cycle-complete requires operator status ok and matching completed/attempted cycle.' }
        if ($Action -eq 'cycle-complete') {
            $priorDone=@($rows | Where-Object { $_.action -eq 'cycle-complete' -and $_.status -eq 'ok' })
            if ($priorDone.Count -ne ($Cycle-1)) { throw 'Missing/duplicate completed cycle: preserve failed/incomplete attempts; do not substitute a completion.' }
        }
        if ($Action -eq 'run-complete') {
            $done=@($rows | Where-Object { $_.action -eq 'cycle-complete' -and $_.status -eq 'ok' })
            if ($Cycle -ne 50 -or $CompletedCycles -ne 50 -or $Status -ne 'ok' -or $done.Count -ne 50) { throw 'Incomplete run: require all fifty distinct operator cycle-complete events.' }
        }
        $row=[ordered]@{schemaVersion=1;runId=$metadata.runId;sequence=$rows.Count+1;utc=[DateTimeOffset]::UtcNow.ToString('o');monotonicTicks=[Diagnostics.Stopwatch]::GetTimestamp();stopwatchFrequency=[Diagnostics.Stopwatch]::Frequency;cycle=$Cycle;phase=$Phase;action=$Action;completedCycles=$CompletedCycles;mediaLabel=(Get-OptionalText $MediaLabel);mediaPart=(Get-OptionalText $MediaPart);actualEngine=(Get-OptionalText $ActualEngine);actualQuality=(Get-OptionalText $ActualQuality);windowState=$WindowState;visibleTabs=$VisibleTabs;observedPlayers=$ObservedPlayers;status=$Status;note=(Get-OptionalText $Note)}
        Write-Record $output $row
        if ($Action -eq 'run-complete') { Write-NewJsonFile (Join-Path $directory 'STOPPED.json') ([ordered]@{status='operator-complete';utc=[DateTimeOffset]::UtcNow.ToString('o');note='Operator markers do not verify UI actions or native acceptance.'}) }
        Write-Output ($row | ConvertTo-Json -Depth 8 -Compress)
} finally { if ($null -ne $lock) { $lock.Dispose(); [IO.File]::Delete($output+'.lock') } }
