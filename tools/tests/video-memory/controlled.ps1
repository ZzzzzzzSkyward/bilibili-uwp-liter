param([Parameter(Mandatory=$true)][string]$Tool,[Parameter(Mandatory=$true)][string]$OutputDirectory)
Set-StrictMode -Version 2.0
$ErrorActionPreference='Stop'
$tokens=$null; $errors=$null
$ast=[Management.Automation.Language.Parser]::ParseFile($Tool,[ref]$tokens,[ref]$errors)
if ($errors.Count -ne 0) { throw ($errors | Out-String) }
# Execute actual private function AST extents only. The production entry/host guard is untouched.
foreach ($function in $ast.FindAll({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst]},$false)) { Invoke-Expression $function.Extent.Text }
Add-Type @'
using System;
using System.Collections;
public sealed class SyntheticMemoryProcess : IDisposable {
  public string Mode = "ok";
  public int DisposeCalls;
  public void Dispose() { DisposeCalls++; }
  public DateTime Start = new DateTime(2026,11,25,12,0,0,DateTimeKind.Utc);
  public void Refresh() { if(Mode=="exit") throw new InvalidOperationException("synthetic exit"); }
  public DateTime StartTime { get { if(Mode=="identity-throw") throw new UnauthorizedAccessException("synthetic identity"); return Mode=="replaced" ? Start.AddSeconds(1) : Start; } }
  public long PrivateMemorySize64 { get { return Mode=="zero" ? 0 : 4294967297L; } }
  public object WorkingSet64 { get { if(Mode=="throw") throw new UnauthorizedAccessException("synthetic getter"); if(Mode=="null") return null; return Mode=="zero" ? 0L : 9876543210L; } }
  public int HandleCount { get { return Mode=="zero" ? 0 : 73; } }
  public ICollection Threads { get { return Mode=="zero" ? new int[0] : new int[3]; } }
  public TimeSpan TotalProcessorTime { get { return Mode=="zero" ? TimeSpan.Zero : TimeSpan.FromMilliseconds(1234.5); } }
}
'@
$script:fake=New-Object SyntheticMemoryProcess
$script:processCalls=0
function Get-Process { param([int]$Id) $script:processCalls++; if($Id -ne 424242 -or $script:fake.Mode -eq 'missing') { throw 'Synthetic process unavailable.' }; return $script:fake }
$results=New-Object 'System.Collections.Generic.List[object]'
function Check { param([string]$Name,[scriptblock]$Test) & $Test; $results.Add([pscustomobject]@{name=$Name;passed=$true}) }
function Require { param([bool]$Condition,[string]$Message) if(-not $Condition) { throw $Message } }
$start=$script:fake.Start
Check 'exact-large-integers-and-schema' {
    $script:fake.Mode='ok'; $r=New-MemorySample 'synthetic' 424242 $start 1
    Require ($r.privateBytes -eq 4294967297L -and $r.workingSetBytes -eq 9876543210L -and $r.status -eq 'ok' -and $r.errors.Count -eq 0) 'large values'
    $expected=@('schemaVersion','runId','sampleSequence','utc','monotonicTicks','stopwatchFrequency','processId','processStartUtc','privateBytes','workingSetBytes','handleCount','threadCount','cpuTotalMs','status','errors')
    Require (@(Compare-Object @($r.Keys) $expected).Count -eq 0 -and $r.monotonicTicks -gt 0 -and $r.stopwatchFrequency -gt 0 -and $r.processId -eq 424242) 'schema'
    $json=$r | ConvertTo-Json -Compress; $roundtrip=ConvertFrom-JsonText $json
    Require ($roundtrip.privateBytes -eq 4294967297L -and $roundtrip.workingSetBytes -eq 9876543210L) 'roundtrip'
    $null=Get-UtcInstant $roundtrip.utc; $null=Get-UtcInstant $roundtrip.processStartUtc
}
Check 'true-zero-empty-threads' {
    $script:fake.Mode='zero'; $r=New-MemorySample 'synthetic' 424242 $start 2
    foreach($field in @('privateBytes','workingSetBytes','handleCount','threadCount','cpuTotalMs')) { Require ($null -ne $r[$field] -and $r[$field] -eq 0) $field }
    Require ($r.status -eq 'ok' -and $r.errors.Count -eq 0) 'zero contract'
}
foreach($strict in @($false,$true)) {
    foreach($mode in @('throw','null')) {
        $script:fake.Mode=$mode
        if($strict) { Set-StrictMode -Version 2.0; $ErrorActionPreference='Stop' } else { Set-StrictMode -Off; $ErrorActionPreference='Continue' }
        $r=New-MemorySample 'synthetic' 424242 $start 3
        Set-StrictMode -Version 2.0; $ErrorActionPreference='Stop'
        Require ($null -eq $r.workingSetBytes -and $r.status -eq 'partial' -and $r.errors.Count -eq 1 -and $r.errors[0] -like 'workingSetBytes: *') 'getter failure contract'
        Require ($r.privateBytes -eq 4294967297L -and $r.handleCount -eq 73 -and $r.threadCount -eq 3 -and $r.cpuTotalMs -eq 1234.5) 'successful fields preserved'
        if($mode -eq 'throw') { Require ($r.errors[0] -eq 'workingSetBytes: System.UnauthorizedAccessException') 'base exception type' }
        $results.Add([pscustomobject]@{name="getter-$mode-strict-$strict";passed=$true})
    }
}
foreach($mode in @('replaced','exit','identity-throw','missing')) {
    $script:fake.Mode=$mode; $r=New-MemorySample 'synthetic' 424242 $start 4
    $expected='process-unavailable'; if($mode -eq 'replaced') { $expected='process-replaced' }
    Require ($r.status -eq $expected -and $r.errors.Count -eq 1) 'fatal status'
    foreach($field in @('privateBytes','workingSetBytes','handleCount','threadCount','cpuTotalMs')) { Require ($null -eq $r[$field]) 'fatal null metrics' }
    if($mode -eq 'identity-throw') { Require ($null -eq $r.processStartUtc) 'identity null' }
    $path=Join-Path $OutputDirectory ($mode+'.ndjson'); Write-Record $path $r
    Require (([IO.File]::ReadAllLines($path)).Count -eq 1) 'fatal row preserved'
    $results.Add([pscustomobject]@{name="fatal-$mode";passed=$true})
}
$script:fake.Mode='ok'
Check 'observer-wrapper-disposed-on-success-and-failure' {
    foreach($mode in @('ok','throw','identity-throw','exit')) {
        $script:fake.Mode=$mode; $before=$script:fake.DisposeCalls
        $null=New-MemorySample 'synthetic' 424242 $start 1
        Require ($script:fake.DisposeCalls -eq ($before+1)) 'observer wrapper not disposed exactly once'
    }
    $script:fake.Mode='ok'
}
Check 'partial-output-preserved-before-write-failure' {
    $path=Join-Path $OutputDirectory 'partial.ndjson'
    Write-Record $path (New-MemorySample 'synthetic' 424242 $start 1)
    $before=Get-Sha256 $path
    $occupied=Join-Path $OutputDirectory 'occupied'; $null=New-Item -ItemType Directory -Path $occupied
    $failed=$false; try { Write-Record $occupied (New-MemorySample 'synthetic' 424242 $start 2) } catch { $failed=$true }
    Require ($failed -and (Get-Sha256 $path) -eq $before) 'write failure preserves prior observations'
}
Check 'exclusive-output-lock' {
    $path=Join-Path $OutputDirectory 'locked.ndjson'; $owner=Enter-OutputLock $path
    try { $failed=$false; try { $other=Enter-OutputLock $path; $other.Dispose() } catch { $failed=$true }; Require $failed 'second writer must fail' }
    finally { $owner.Dispose(); Remove-Item -LiteralPath ($path+'.lock') }
}
Check 'create-new-no-overwrite' {
    $path=Join-Path $OutputDirectory 'fresh.json'; Write-NewJsonFile $path @{value=1}; $before=Get-Sha256 $path
    $failed=$false; try { Write-NewJsonFile $path @{value=2} } catch { $failed=$true }
    Require ($failed -and (Get-Sha256 $path) -eq $before) 'createNew overwrote existing data'
}
Check 'empty-and-unterminated-streams-refuse-append' {
    $meta=[pscustomobject]@{runId='synthetic';process=[pscustomobject]@{id=424242;startUtc=$start.ToString('o')}}
    foreach($variant in @('empty','complete-json-no-newline')) {
        $path=Join-Path $OutputDirectory ($variant+'.ndjson')
        $content=''; if($variant -ne 'empty') { $content=(New-MemorySample 'synthetic' 424242 $start 1) | ConvertTo-Json -Compress }
        [IO.File]::WriteAllText($path,$content,(New-Object Text.UTF8Encoding($false)))
        $before=Get-Sha256 $path; $failed=$false
        try { $null=Read-OutputRows $path $meta 'samples' } catch { $failed=$true }
        Require ($failed -and (Get-Sha256 $path) -eq $before) 'unterminated observations cannot concatenate with new output'
    }
}
Check 'phase-order-and-duplicate-guards' {
    $r=[pscustomobject]@{cycle=5;phase='settled';utc=[DateTimeOffset]::UtcNow.AddSeconds(-1).ToString('o');monotonicTicks=0}
    $rows=New-Object 'System.Collections.Generic.List[object]'; $rows.Add($r)
    foreach($phase in @('opened','post-close','settled')) { $failed=$false; try { Assert-NewObservation $rows 5 $phase '' } catch { $failed=$true }; Require $failed 'backwards/duplicate phase' }
    Assert-NewObservation $rows 10 'opened' ''
}
Check 'bad-record-bindings-order-types-and-null-contract' {
    $meta=[pscustomobject]@{runId='synthetic';process=[pscustomobject]@{id=424242;startUtc=$start.ToString('o')}}
    foreach($mutant in @('run','pid','start','sequence','negative','nonfinite','false-zero-null','time','unit-type','duplicate','missing-field')) {
        $r=New-MemorySample 'synthetic' 424242 $start 1
        switch($mutant) {
            'run' { $r.runId='other' }; 'pid' { $r.processId=1 }; 'start' { $r.processStartUtc=$start.AddSeconds(1).ToString('o') }
            'sequence' { $r.sampleSequence=2 }; 'negative' { $r.privateBytes=-1 }; 'nonfinite' { $r.cpuTotalMs=[double]::NaN }
            'false-zero-null' { $r.privateBytes=$null }; 'time' { $r.monotonicTicks=-1 }; 'unit-type' { $r.privateBytes='1 MB' }
            'missing-field' { $r.Remove('threadCount') }
        }
        $path=Join-Path $OutputDirectory ($mutant+'.ndjson'); Write-Record $path $r
        if($mutant -eq 'duplicate') { Write-Record $path $r }
        $failed=$false; try { $null=Read-OutputRows $path $meta 'samples' } catch { $failed=$true }; Require $failed $mutant
    }
}
Check 'actual-capture-loop-continuous-and-no-resume' {
    $script:fake.Mode='ok'
    $meta=[pscustomobject]@{runId='synthetic';process=[pscustomobject]@{id=424242;startUtc=$start.ToString('o')}}
    $directory=Join-Path $OutputDirectory 'capture'; $null=New-Item -ItemType Directory -Path $directory
    Invoke-MemoryCapture $meta $directory 424242 $start 'Collect' 0.1 0.25 0 ''
    $rows=Read-OutputRows (Join-Path $directory 'samples.ndjson') $meta 'samples'
    Require ($rows.Count -ge 2 -and $rows.Count -le 3) 'bounded continuous samples'
    $before=Get-Sha256 (Join-Path $directory 'samples.ndjson')
    $failed=$false; try { Invoke-MemoryCapture $meta $directory 424242 $start 'Collect' 0.1 0.25 0 '' } catch { $failed=$true }
    Require ($failed -and (Get-Sha256 (Join-Path $directory 'samples.ndjson')) -eq $before) 'resume rejected, bytes preserved'
    Require (-not (Test-Path -LiteralPath (Join-Path $directory 'samples.ndjson.lock'))) 'lock released'
}
Check 'actual-capture-fatal-preservation-and-no-further-process-read' {
    $meta=[pscustomobject]@{runId='synthetic';process=[pscustomobject]@{id=424242;startUtc=$start.ToString('o')}}
    foreach($mode in @('replaced','identity-throw','exit')) {
        $directory=Join-Path $OutputDirectory ('capture-'+$mode); $null=New-Item -ItemType Directory -Path $directory
        $script:fake.Mode='ok'; $null=Invoke-MemoryCapture $meta $directory 424242 $start 'Checkpoint' 2 10 0 'prewarm'
        $script:fake.Mode=$mode
        $failed=$false; try { $null=Invoke-MemoryCapture $meta $directory 424242 $start 'Checkpoint' 2 10 0 'baseline' } catch { $failed=$true }
        Require $failed 'fatal capture must throw'
        $path=Join-Path $directory 'checkpoints.ndjson'; $lines=[IO.File]::ReadAllLines($path)
        Require ($lines.Count -eq 2 -and (ConvertFrom-JsonText $lines[0]).status -eq 'ok' -and (ConvertFrom-JsonText $lines[1]).status -like 'process-*') 'prior/fatal observations preserved'
        Require (Test-Path -LiteralPath (Join-Path $directory 'STOPPED.json')) 'terminal run marker'
        Require (-not (Test-Path -LiteralPath ($path+'.lock'))) 'fatal lock released'
        $before=$script:processCalls
        try { $null=Invoke-MemoryCapture $meta $directory 424242 $start 'Checkpoint' 2 10 5 'opened' } catch { }
        Require ($script:processCalls -eq $before -and ([IO.File]::ReadAllLines($path)).Count -eq 2) 'no target reads/append after fatal run'
    }
    $script:fake.Mode='ok'
}
Check 'all-eleven-phases-and-invalid-before-dependency' {
    $meta=[pscustomobject]@{runId='synthetic';process=[pscustomobject]@{id=424242;startUtc=$start.ToString('o')}}
    foreach($phase in @('prewarm','baseline','opened','post-close','settled','idle60','idle300','background','minimized','restored','feed-step')) {
        $directory=Join-Path $OutputDirectory ('phase-'+$phase); $null=New-Item -ItemType Directory -Path $directory
        $cycle=5; if($phase -in @('prewarm','baseline')) { $cycle=0 }; if($phase -in @('idle60','idle300')) { $cycle=50 }
        $null=Invoke-MemoryCapture $meta $directory 424242 $start 'Checkpoint' 2 10 $cycle $phase
        $r=ConvertFrom-JsonText ([IO.File]::ReadAllText((Join-Path $directory 'checkpoints.ndjson')))
        Require ($r.phase -eq $phase -and $r.cycle -eq $cycle -and $r.status -eq 'ok') 'phase record'
    }
    $before=$script:processCalls; $failed=$false
    try { $null=Invoke-MemoryCapture $meta $OutputDirectory 424242 $start 'Checkpoint' 2 10 5 'invalid' } catch { $failed=$true }
    Require ($failed -and $script:processCalls -eq $before) 'invalid phase before target/output'
}
Check 'actual-capture-output-failure-preserves-prior-and-releases-lock' {
    $meta=[pscustomobject]@{runId='synthetic';process=[pscustomobject]@{id=424242;startUtc=$start.ToString('o')}}
    $directory=Join-Path $OutputDirectory 'capture-writefail'; $null=New-Item -ItemType Directory -Path $directory
    $path=Join-Path $directory 'samples.ndjson'; $null=New-Item -ItemType Directory -Path $path
    $failed=$false; try { Invoke-MemoryCapture $meta $directory 424242 $start 'Collect' 2 10 0 '' } catch { $failed=$true }
    Require ($failed -and (Test-Path -LiteralPath $path -PathType Container) -and -not (Test-Path -LiteralPath ($path+'.lock'))) 'occupied output preserved and lock released'
}
$results | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'controlled-results.json') -Encoding UTF8
Write-Output ('PASS '+$results.Count+' controlled groups; all process objects and values are synthetic.')
