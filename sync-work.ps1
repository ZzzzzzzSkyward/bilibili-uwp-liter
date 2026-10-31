<#
.SYNOPSIS
    拉取并推送当前分支，带 TLS 重试，且不把维护者的包清单改动混进提交。

.DESCRIPTION
    GitHub 的 TLS 在这个网络里会间歇性中断（`unexpected eof while reading`、连接重置、超时），
    单独一次 `git push` 经常直接失败。这个脚本把日常同步做成一件事：

    1. fetch（失败按 -RetrySeconds 重试，识别 TLS/连接类错误）
    2. 分支落后就 rebase 到远端；冲突时中止 rebase 并保留现场，不猜
    3. push（被拒说明远端又动了，重新 fetch+rebase 再来一次）
    4. 两个 Package.appxmanifest 一直是本地改动：进入前 stash（仅这两个路径），结束时 pop 回来

    退出码：0 已同步，1 失败（原因已打印），2 有需要人工处理的冲突。

.PARAMETER NoPush
    只拉取，不推送（只想看别人改了什么时用）。

.PARAMETER NoPull
    只推送，不拉取。

.PARAMETER Attempts
    每个网络动作的重试次数，默认 12 次 × 15 秒 ≈ 3 分钟。

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File sync-work.ps1
#>
param(
    [string]$Remote = 'origin',
    [string]$Branch,
    [switch]$NoPush,
    [switch]$NoPull,
    [int]$Attempts = 12,
    [int]$RetrySeconds = 15
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $root
try {
    try {
        $utf8 = New-Object System.Text.UTF8Encoding($false)
        [Console]::OutputEncoding = $utf8
        [Console]::InputEncoding = $utf8
        $OutputEncoding = $utf8
    }
    catch { }

    # 这两个清单是维护者自己升版本号留下的常驻改动，绝不能进提交，也不能被 rebase 打断
    $protected = @(
        'src/BiliLite.UWP/Package.appxmanifest',
        'src/BiliLite.Packages/Package.appxmanifest'
    )
    $networkFailure = 'TLS|unexpected eof|Connection reset|connection reset|timed out|Failed to connect|Could not resolve host|RPC failed|early EOF|SSL_ERROR'

    function Invoke-Git {
        param([string[]]$Arguments, [switch]$AllowNetworkRetry, [switch]$Quiet)
        $attempt = 0
        # git 会往 stderr 写正常进度（"Rebasing (1/4)" 等）。在 PowerShell 5.1 里
        # 一旦把 stderr 并进管道，$ErrorActionPreference='Stop' 就会把它当成异常；
        # 这里临时降级，只看退出码判断成败。
        $ErrorActionPreference = 'Continue'
        while ($true) {
            $attempt++
            $out = & git @Arguments 2>&1 | ForEach-Object { "$_" }
            $code = $LASTEXITCODE
            if ($code -eq 0) {
                if (-not $Quiet -and $out) { $out | ForEach-Object { Write-Host $_ } }
                return @{ Code = 0; Output = $out }
            }
            $text = ($out | Out-String)
            if ($AllowNetworkRetry -and $attempt -lt $Attempts -and $text -match $networkFailure) {
                Write-Warning ("第 $attempt 次失败（{0}），{1}s 后重试：git {2}" -f ($text.Trim().Split("`n")[0]), $RetrySeconds, ($Arguments -join ' '))
                Start-Sleep -Seconds $RetrySeconds
                continue
            }
            return @{ Code = $code; Output = $out }
        }
    }

    function Get-BranchName {
        if ($Branch) { return $Branch }
        $name = (& git rev-parse --abbrev-ref HEAD 2>$null)
        if ($LASTEXITCODE -ne 0 -or -not $name -or $name -eq 'HEAD') {
            throw '当前不是在一个具名分支上，无法同步。'
        }
        return $name.Trim()
    }

    function Get-AheadBehind {
        $counts = (& git rev-list --left-right --count "HEAD...$Remote/$Branch" 2>$null)
        if ($LASTEXITCODE -ne 0) { return @{ Ahead = 0; Behind = 0 } }
        $parts = ($counts -split '\s+') | Where-Object { $_ -ne '' }
        return @{ Ahead = [int]$parts[0]; Behind = [int]$parts[1] }
    }

    $Branch = Get-BranchName
    Write-Host "仓库    : $root"
    Write-Host "分支    : $Branch → $Remote/$Branch"

    $stashed = $false
    $dirty = @(& git status --porcelain -- $protected 2>$null) | Where-Object { $_ }
    if ($dirty.Count -gt 0) {
        $stashArgs = @('stash', 'push', '--quiet', '-m', 'sync-work: protected manifests', '--') + $protected
        $stash = Invoke-Git -Arguments $stashArgs
        if ($stash.Code -ne 0) { throw "无法暂存包清单改动：$($stash.Output -join ' ')"; }
        $stashed = $true
        Write-Host '暂存    : 两个 Package.appxmanifest 已暂存，结束时自动恢复'
    }

    try {
        if (-not $NoPull) {
            $fetch = Invoke-Git -Arguments @('fetch', $Remote, $Branch, '--quiet') -AllowNetworkRetry
            if ($fetch.Code -ne 0) {
                # fetch 失败不该挡住推送：推的是本地已有提交，远端拒绝了再处理
                Write-Warning "拉取失败，跳过 rebase，直接尝试推送：$(($fetch.Output | Select-Object -Last 1))"
            }
            $state = Get-AheadBehind
            if ($fetch.Code -eq 0) {
                if ($state.Behind -gt 0) {
                    Write-Host "拉取    : 落后 $($state.Behind) 个提交，rebase 到 $Remote/$Branch"
                    $rebase = Invoke-Git -Arguments @('rebase', "$Remote/$Branch")
                    if ($rebase.Code -ne 0) {
                        & git rebase --abort 2>$null | Out-Null
                        Write-Host ''
                        Write-Host 'rebase 冲突，已中止并保留工作区。先手动解决再运行本脚本。' -ForegroundColor Red
                        exit 2
                    }
                }
                else {
                    Write-Host '拉取    : 已是最新'
                }
            }
        }

        if (-not $NoPush) {
            $state = Get-AheadBehind
            if ($state.Ahead -eq 0) {
                Write-Host '推送    : 没有本地提交需要推送'
            }
            else {
                $pushed = $false
                for ($round = 1; $round -le 3; $round++) {
                    Write-Host "推送    : 尝试推送 $($state.Ahead) 个提交（第 $round 轮）"
                    $push = Invoke-Git -Arguments @('push', $Remote, $Branch) -AllowNetworkRetry
                    if ($push.Code -eq 0) { $pushed = $true; break }

                    $text = ($push.Output | Out-String)
                    if ($text -match 'non-fast-forward|fetch first|rejected') {
                        Write-Warning '远端又前进了，重新 fetch + rebase 后再推。'
                        $fetch = Invoke-Git -Arguments @('fetch', $Remote, $Branch, '--quiet') -AllowNetworkRetry
                        if ($fetch.Code -ne 0) { Write-Error "拉取失败：$($fetch.Output -join ' ')"; exit 1 }
                        $rebase = Invoke-Git -Arguments @('rebase', "$Remote/$Branch")
                        if ($rebase.Code -ne 0) {
                            & git rebase --abort 2>$null | Out-Null
                            Write-Host 'rebase 冲突，已中止并保留工作区。' -ForegroundColor Red
                            exit 2
                        }
                        $state = Get-AheadBehind
                        if ($state.Ahead -eq 0) { $pushed = $true; break }
                        if ($round -eq 3) {
                            Write-Host "连续三轮被远端抢先，本地还有 $($state.Ahead) 个提交没有推送。稍后重跑本脚本。" -ForegroundColor Red
                            exit 1
                        }
                        continue
                    }

                    Write-Host ''
                    Write-Host "推送失败：$(($push.Output | Select-Object -Last 1))" -ForegroundColor Red
                    exit 1
                }
                if (-not $pushed) {
                    Write-Host '推送没有成功，本地提交仍未发布。' -ForegroundColor Red
                    exit 1
                }
            }
        }
    }
    finally {
        if ($stashed) {
            $pop = & git stash pop --quiet 2>&1
            if ($LASTEXITCODE -ne 0) {
                Write-Warning "包清单改动未能自动恢复，仍在 stash 里（git stash list 可见）：$($pop -join ' ')"
            }
            else {
                Write-Host '恢复    : 两个 Package.appxmanifest 的本地改动已回到工作区'
            }
        }
    }

    Write-Host ''
    & git status -sb | Select-Object -First 3 | ForEach-Object { Write-Host $_ }
    & git log --oneline -1 | ForEach-Object { Write-Host "HEAD    : $_" }
}
finally {
    Pop-Location
}
