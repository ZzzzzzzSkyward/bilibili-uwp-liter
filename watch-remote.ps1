<#
.SYNOPSIS
    Block until the watched remote branch moves, then report the new commit.

.DESCRIPTION
    Polls `git ls-remote`, which reads only the remote and leaves local refs and
    the working tree untouched. Exits 0 on the first new commit, 2 when the
    optional timeout elapses with no change, and 3 when the branch disappears.

    The caller decides whether to pull. -Fetch downloads just that ref into
    FETCH_HEAD so the incoming commits can be printed, still without touching
    the checked-out branch.

.EXAMPLE
    .\watch-remote.ps1

.EXAMPLE
    .\watch-remote.ps1 -IntervalSeconds 60 -TimeoutSeconds 3600 -Fetch
#>
param(
    [string]$Remote = 'origin',
    [string]$Branch,
    [int]$IntervalSeconds = 300,
    [int]$TimeoutSeconds = 0,
    [switch]$Fetch
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

function Get-RemoteHead {
    $lines = @(& git ls-remote $Remote "refs/heads/$Branch" 2>$null)
    if ($LASTEXITCODE -ne 0) {
        return [pscustomobject]@{ Ok = $false; Sha = $null }
    }
    foreach ($line in $lines) {
        if ($line -match '^([0-9a-fA-F]{40})\s') {
            return [pscustomobject]@{ Ok = $true; Sha = $Matches[1] }
        }
    }
    return [pscustomobject]@{ Ok = $true; Sha = $null }
}

Push-Location $root
try {
    if (-not $Branch) {
        $Branch = (& git rev-parse --abbrev-ref HEAD).Trim()
        if ($LASTEXITCODE -ne 0 -or -not $Branch) {
            throw "Cannot resolve the current branch; pass -Branch explicitly."
        }
    }

    $baseline = Get-RemoteHead
    if (-not $baseline.Ok) {
        throw "Cannot read $Remote/$Branch`: check the network and the remote name."
    }
    if (-not $baseline.Sha) {
        throw "Remote branch $Remote/$Branch does not exist."
    }

    $limit = if ($TimeoutSeconds -gt 0) { " timeout ${TimeoutSeconds}s" } else { '' }
    Write-Host "Watching $Remote/$Branch at $($baseline.Sha), interval ${IntervalSeconds}s$limit"

    $deadline = if ($TimeoutSeconds -gt 0) { (Get-Date).AddSeconds($TimeoutSeconds) } else { [datetime]::MaxValue }
    $lastWarning = [datetime]::MinValue

    while ($true) {
        if ((Get-Date) -ge $deadline) {
            Write-Host "Timeout: $Remote/$Branch still at $($baseline.Sha)"
            exit 2
        }

        Start-Sleep -Seconds $IntervalSeconds

        $current = Get-RemoteHead
        if (-not $current.Ok) {
            if (((Get-Date) - $lastWarning).TotalSeconds -ge 60) {
                Write-Host "Warning: cannot reach $Remote, retrying"
                $lastWarning = Get-Date
            }
            continue
        }
        if (-not $current.Sha) {
            Write-Host "Branch deleted: $Remote/$Branch"
            exit 3
        }
        if ($current.Sha -eq $baseline.Sha) {
            continue
        }

        Write-Host "Remote moved: $($baseline.Sha) -> $($current.Sha)"
        if ($Fetch) {
            & git fetch --quiet $Remote $Branch
            if ($LASTEXITCODE -eq 0) {
                $incoming = @(& git log --oneline "$($baseline.Sha)..$($current.Sha)" 2>$null)
                if ($incoming.Count -gt 0) {
                    Write-Host 'Incoming commits:'
                    $incoming | ForEach-Object { Write-Host "  $_" }
                }
            }
            else {
                Write-Host 'Warning: fetch failed; the new commits were not listed'
            }
        }
        exit 0
    }
}
finally {
    Pop-Location
}
