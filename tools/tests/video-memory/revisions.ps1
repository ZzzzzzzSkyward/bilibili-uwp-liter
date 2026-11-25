param([Parameter(Mandatory=$true)][string]$Tool,[Parameter(Mandatory=$true)][string]$MetadataPath,[Parameter(Mandatory=$true)][string]$OutputDirectory)
Set-StrictMode -Version 2.0
$ErrorActionPreference='Stop'
$null=New-Item -ItemType Directory -Path $OutputDirectory -ErrorAction Stop
$tokens=$null; $errors=$null
$ast=[Management.Automation.Language.Parser]::ParseFile($Tool,[ref]$tokens,[ref]$errors)
if($errors.Count) { throw ($errors | Out-String) }
foreach($function in $ast.FindAll({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst]},$false)) { Invoke-Expression $function.Extent.Text }
Add-Type @'
using System;
using System.Collections;
public sealed class RevisionSyntheticProcess : IDisposable {
 public void Refresh() {} public void Dispose() {}
 public DateTime StartTime { get { return new DateTime(2026,11,25,12,0,0,DateTimeKind.Utc); } }
 public long PrivateMemorySize64 { get { return 4294967297L; } }
 public long WorkingSet64 { get { return 0L; } }
 public int HandleCount { get { return 0; } }
 public ICollection Threads { get { return new int[0]; } }
 public TimeSpan TotalProcessorTime { get { return TimeSpan.Zero; } }
}
'@
$script:fake=New-Object RevisionSyntheticProcess; $script:processCalls=0
function Get-Process { param([int]$Id) if($Id -ne 424242) { throw 'Only synthetic PID allowed.' }; $script:processCalls++; return $script:fake }
$metadata=Read-Metadata $MetadataPath
$start=Get-UtcInstant $metadata.process.startUtc
$script:checks=New-Object 'System.Collections.Generic.List[object]'
function Record { param([string]$Name,[bool]$Passed,[object]$Detail) $script:checks.Add([pscustomobject]@{name=$Name;passed=$Passed;detail=$Detail}) }
function Fresh { param([string]$Name) $p=Join-Path $OutputDirectory $Name; $null=New-Item -ItemType Directory -Path $p; return $p }
function Attempt { param([scriptblock]$Operation) try { $null=& $Operation; return $false } catch { return $true } }

# T1: one actual capture stream, not separate per-phase directories.
$journey=Fresh 'journey'; $steps=@(@(0,'prewarm'),@(0,'baseline'))
foreach($cycle in @(5,10,25,50)) { foreach($phase in @('opened','post-close','settled')) { $steps+=,@($cycle,$phase) } }
$steps+=,@(50,'idle60'); $steps+=,@(50,'idle300'); $failures=@()
foreach($step in $steps) { if(Attempt { Invoke-MemoryCapture $metadata $journey 424242 $start 'Checkpoint' 2 10 $step[0] $step[1] }) { $failures+=($step[0].ToString()+':'+$step[1]) } }
$path=Join-Path $journey 'checkpoints.ndjson'; $rows=Read-OutputRows $path $metadata 'checkpoints'
$expected=@(foreach($step in $steps) { $step[0].ToString()+':'+$step[1] })
$actual=@(foreach($row in $rows) { $row.cycle.ToString()+':'+$row.phase })
$exactSequence=($actual -join '|') -ceq ($expected -join '|')
Record 'T1-full-same-run-sixteen-checkpoints' ($rows.Count -eq 16 -and $failures.Count -eq 0 -and $exactSequence) @{rows=$rows.Count;failures=$failures;exactSequence=$exactSequence;orders=@{settled50=(Get-CheckpointOrder 50 'settled');idle60=(Get-CheckpointOrder 50 'idle60');idle300=(Get-CheckpointOrder 50 'idle300')}}
$before=Get-Sha256 $path; $calls=$script:processCalls
$rejected=Attempt { Invoke-MemoryCapture $metadata $journey 424242 $start 'Checkpoint' 2 10 50 'idle60' }
Record 'T1-idle60-after-idle300-refused-preserves-output' ($rejected -and $before -eq (Get-Sha256 $path) -and $calls -eq $script:processCalls -and -not (Test-Path -LiteralPath ($path+'.lock'))) @{rejected=$rejected;processReads=$script:processCalls-$calls}
# Isolate backward ordering from duplicate rejection: this stream has no earlier idle60.
$reverse=Fresh 'idle300-before-idle60'; $null=Invoke-MemoryCapture $metadata $reverse 424242 $start 'Checkpoint' 2 10 50 'idle300'
$reversePath=Join-Path $reverse 'checkpoints.ndjson'; $before=Get-Sha256 $reversePath; $calls=$script:processCalls
$rejected=Attempt { Invoke-MemoryCapture $metadata $reverse 424242 $start 'Checkpoint' 2 10 50 'idle60' }
Record 'T1-idle60-backward-without-prior-duplicate' ($rejected -and $before -eq (Get-Sha256 $reversePath) -and $calls -eq $script:processCalls -and -not (Test-Path -LiteralPath ($reversePath+'.lock'))) @{rejected=$rejected;processReads=$script:processCalls-$calls}
foreach($phase in @('opened','post-close','settled','background','minimized','restored','feed-step')) {
 $dir=Fresh ('control-'+$phase); $failed=Attempt { Invoke-MemoryCapture $metadata $dir 424242 $start 'Checkpoint' 2 10 50 $phase; Invoke-MemoryCapture $metadata $dir 424242 $start 'Checkpoint' 2 10 50 'idle60'; Invoke-MemoryCapture $metadata $dir 424242 $start 'Checkpoint' 2 10 50 'idle300' }
 $count=([IO.File]::ReadAllLines((Join-Path $dir 'checkpoints.ndjson'))).Count
 Record ('T1-idle-after-cycle50-'+$phase) (-not $failed -and $count -eq 3) @{rows=$count;rejected=$failed}
}

# T2: actual public metadata validator; wrap every scalar-string leaf, retaining supported arrays.
function StringPaths { param([object]$Value,[string[]]$Trail=@())
 foreach($property in $Value.PSObject.Properties) {
  if($property.Value -is [string]) { ,@($Trail+$property.Name) }
  elseif($property.Value -is [pscustomobject]) { StringPaths $property.Value @($Trail+$property.Name) }
 }
}
$metadataPaths=@(StringPaths $metadata)
foreach($parts in $metadataPaths) {
 $candidate=ConvertFrom-JsonText (Get-Content -LiteralPath $MetadataPath -Raw -Encoding UTF8)
 $parent=$candidate; for($i=0;$i -lt $parts.Count-1;$i++) { $parent=$parent.($parts[$i]) }
 $leaf=$parts[$parts.Count-1]; $parent.$leaf=@($parent.$leaf)
 $file=Join-Path $OutputDirectory ('metadata-'+($parts -join '-')+'.json'); Write-NewJsonFile $file $candidate
 $rejected=Attempt { & $Tool -Mode ValidateMetadata -MetadataPath $file }
 Record ('T2-metadata-scalar-'+($parts -join '.')) $rejected @{rejected=$rejected}
}

# Process/checkpoint replay plus actual append: no new read or byte change on rejection.
foreach($kind in @('samples','checkpoints')) {
 $fields=@('runId','utc','processStartUtc','status'); if($kind -eq 'checkpoints') { $fields+='phase' }
 foreach($field in $fields) {
  $dir=Fresh ($kind+'-'+$field); $file=Join-Path $dir ($kind+'.ndjson')
  $row=New-MemorySample $metadata.runId 424242 $start 1
  if($kind -eq 'checkpoints') { $row.cycle=0; $row.phase='prewarm' }
  $row[$field]=@($row[$field]); Write-Record $file $row; $before=Get-Sha256 $file; $calls=$script:processCalls
  if($kind -eq 'checkpoints') { $rejected=Attempt { Invoke-MemoryCapture $metadata $dir 424242 $start 'Checkpoint' 2 10 0 'baseline' } }
  else { $rejected=Attempt { Read-OutputRows $file $metadata $kind } }
  Record ('T2-'+$kind+'-scalar-'+$field) ($rejected -and $before -eq (Get-Sha256 $file) -and $calls -eq $script:processCalls) @{rejected=$rejected;processReads=$script:processCalls-$calls;unchanged=($before -eq (Get-Sha256 $file))}
 }
}

# Real public Mark initializes a binding and replays the mutated stored event before append.
$common=@{Mode='Mark';MetadataPath=$MetadataPath;TargetProcessId=424242;ExpectedProcessStartUtc=$metadata.process.startUtc}
foreach($field in @('runId','utc','phase','action','status','windowState','mediaLabel','mediaPart','actualEngine','actualQuality','note')) {
 $dir=Join-Path $OutputDirectory ('event-'+$field)
 $null=& $Tool @common -RunDirectory $dir -Cycle 0 -Phase prewarm -Action observation -Status incomplete -MediaLabel 'synthetic' -MediaPart 'part' -ActualEngine 'synthetic' -ActualQuality 'synthetic' -Note 'synthetic'
 $file=Join-Path $dir 'events.ndjson'; $row=ConvertFrom-JsonText ([IO.File]::ReadAllText($file)); $row.$field=@($row.$field)
 [IO.File]::WriteAllText($file,($row | ConvertTo-Json -Depth 8 -Compress)+[Environment]::NewLine,(New-Object Text.UTF8Encoding($false)))
 $before=Get-Sha256 $file; $calls=$script:processCalls
 $rejected=Attempt { & $Tool @common -RunDirectory $dir -Cycle 1 -Phase opened -Action open-request -CompletedCycles 0 -Status incomplete }
 Record ('T2-event-scalar-'+$field) ($rejected -and $before -eq (Get-Sha256 $file) -and $calls -eq $script:processCalls) @{rejected=$rejected;unchanged=($before -eq (Get-Sha256 $file));processReads=$script:processCalls-$calls}
}

# Stored run bindings and terminal records are typed before comparison/early stop.
foreach($field in @('runId','metadataSha256','scriptSha256','scriptVersion','createdUtc','processStartUtc','artifactSha256')) {
 $dir=Join-Path $OutputDirectory ('binding-'+$field); $null=& $Tool @common -RunDirectory $dir -Cycle 0 -Phase prewarm -Action observation
 $file=Join-Path $dir 'binding.json'; $row=ConvertFrom-JsonText ([IO.File]::ReadAllText($file)); $row.$field=@($row.$field)
 [IO.File]::WriteAllText($file,($row | ConvertTo-Json -Compress)+[Environment]::NewLine,(New-Object Text.UTF8Encoding($false)))
 $events=Join-Path $dir 'events.ndjson'; $before=Get-Sha256 $events; $bindingBefore=Get-Sha256 $file
 $rejected=Attempt { & $Tool @common -RunDirectory $dir -Cycle 1 -Phase opened -Action open-request }
 Record ('T2-binding-scalar-'+$field) ($rejected -and $before -eq (Get-Sha256 $events) -and $bindingBefore -eq (Get-Sha256 $file)) @{rejected=$rejected;unchanged=($before -eq (Get-Sha256 $events))}
}
foreach($field in @('status','utc','note')) {
 $dir=Fresh ('terminal-'+$field); $file=Join-Path $dir 'STOPPED.json'
 $row=[ordered]@{status='operator-complete';utc=[DateTimeOffset]::UtcNow.ToString('o');note='synthetic terminal'}; $row[$field]=@($row[$field]); Write-NewJsonFile $file $row
 $before=Get-Sha256 $file; $calls=$script:processCalls
 $rejected=Attempt { Invoke-MemoryCapture $metadata $dir 424242 $start 'Collect' 2 10 0 '' }
 Record ('T2-terminal-scalar-'+$field) ($rejected -and $before -eq (Get-Sha256 $file) -and $calls -eq $script:processCalls -and -not (Test-Path -LiteralPath (Join-Path $dir 'samples.ndjson'))) @{rejected=$rejected;processReads=$script:processCalls-$calls}
}
$valid=Fresh 'valid-terminal'; Write-NewJsonFile (Join-Path $valid 'STOPPED.json') @{status='operator-complete';utc=[DateTimeOffset]::UtcNow.ToString('o');note='synthetic'}
$calls=$script:processCalls; $failed=Attempt { Invoke-MemoryCapture $metadata $valid 424242 $start 'Collect' 2 10 0 '' }
Record 'T2-valid-scalar-terminal-stops-without-sample' (-not $failed -and $calls -eq $script:processCalls -and -not (Test-Path -LiteralPath (Join-Path $valid 'samples.ndjson'))) @{rejected=$failed}

$failedChecks=@($script:checks | Where-Object { -not $_.passed })
[ordered]@{status=$(if($failedChecks.Count) {'FAIL'}else{'PASS'});toolSha256=(Get-Sha256 $Tool);checks=$script:checks;failedCount=$failedChecks.Count;synthetic=$true;native='BLOCKED/UNVERIFIED'} | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'revision-results.json') -Encoding UTF8
Write-Output ('Revision checks: '+$script:checks.Count+', failures: '+$failedChecks.Count)
if($failedChecks.Count) { exit 1 }
