param([Parameter(Mandatory=$true)][string]$Tool,[Parameter(Mandatory=$true)][string]$MetadataPath,[Parameter(Mandatory=$true)][string]$RunDirectory)
Set-StrictMode -Version 2.0
$ErrorActionPreference='Stop'
# Real public Mark entry; no native dependency replacement or host-guard changes.
$common=@{Mode='Mark';MetadataPath=$MetadataPath;RunDirectory=$RunDirectory;TargetProcessId=424242;ExpectedProcessStartUtc='2026-11-25T12:00:00.0000000Z'}
$null=& $Tool @common -Cycle 0 -Phase prewarm -Action observation -Status incomplete -Note 'SYNTHETIC events; no UI/native observation.'
$failed=$false
try { $null=& $Tool @common -Cycle 2 -Phase settled -Action cycle-complete -CompletedCycles 2 -Status ok } catch { $failed=$true }
if(-not $failed) { throw 'Skipped cycle accepted.' }
for($i=1;$i -le 50;$i++) {
    $null=& $Tool @common -Cycle $i -Phase settled -Action cycle-complete -CompletedCycles $i -Status ok -VisibleTabs 0 -ObservedPlayers 0 -Note 'SYNTHETIC operator assertion only.'
    if($i -eq 1) {
        $failed=$false
        try { $null=& $Tool @common -Cycle 1 -Phase opened -Action cycle-complete -CompletedCycles 1 -Status ok } catch { $failed=$true }
        if(-not $failed) { throw 'Duplicate completion in another phase accepted.' }
    }
}
$null=& $Tool @common -Cycle 50 -Phase idle300 -Action run-complete -CompletedCycles 50 -Status ok -Note 'SYNTHETIC complete marker sequence only.'
if(-not (Test-Path -LiteralPath (Join-Path $RunDirectory 'STOPPED.json'))) { throw 'Missing terminal operator marker.' }
$prior=[IO.File]::ReadAllText((Join-Path $RunDirectory 'events.ndjson'))
$failed=$false
try { $null=& $Tool @common -Cycle 50 -Phase idle300 -Action observation -CompletedCycles 50 -Status ok } catch { $failed=$true }
if(-not $failed -or $prior -cne [IO.File]::ReadAllText((Join-Path $RunDirectory 'events.ndjson'))) { throw 'Append after terminal marker.' }
Write-Output 'PASS complete synthetic marker sequence; skip/duplicate/terminal protections.'
