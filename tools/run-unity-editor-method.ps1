param(
    [Parameter(Mandatory = $true)]
    [string]$ExecuteMethod,

    [string]$ProjectPath,

    [string]$UnityPath,

    [string]$LogName,

    [string[]]$AdditionalArgs = @(),

    [int]$TimeoutSeconds = 300,

    [int]$StartupLogTimeoutSeconds = 90,

    [switch]$NoQuit,

    [switch]$UseGraphics,

    [switch]$KeepStaleLock,

    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

function Resolve-RepoRoot {
    return (Split-Path -Parent $PSScriptRoot)
}

function Test-UnityExecutable([string]$Path) {
    return -not [string]::IsNullOrWhiteSpace($Path) -and (Test-Path -LiteralPath $Path -PathType Leaf)
}

function Join-UnityExe([string]$EditorRoot) {
    if ([string]::IsNullOrWhiteSpace($EditorRoot)) {
        return $null
    }

    $candidate = Join-Path $EditorRoot "Editor\Unity.exe"
    if (Test-UnityExecutable $candidate) {
        return $candidate
    }

    return $null
}

function Resolve-UnityExecutable([string]$ExplicitPath) {
    if (Test-UnityExecutable $ExplicitPath) {
        return (Resolve-Path -LiteralPath $ExplicitPath).Path
    }

    if (-not [string]::IsNullOrWhiteSpace($env:UNITY_EDITOR) -and (Test-UnityExecutable $env:UNITY_EDITOR)) {
        return (Resolve-Path -LiteralPath $env:UNITY_EDITOR).Path
    }

    $registryKeys = @(
        "HKLM:\SOFTWARE\Unity Technologies\Installer\Unity 2022.3.62f3c1",
        "HKCU:\SOFTWARE\Unity Technologies\Installer\Unity 2022.3.62f3c1"
    )

    foreach ($key in $registryKeys) {
        $item = Get-ItemProperty -LiteralPath $key -ErrorAction SilentlyContinue
        if ($null -eq $item) {
            continue
        }

        $location = $item."Location x64"
        $exe = Join-UnityExe $location
        if (Test-UnityExecutable $exe) {
            return (Resolve-Path -LiteralPath $exe).Path
        }
    }

    $commonEditorRoots = @(
        "C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1",
        "D:\unity\2022.3.62f3c1",
        "D:\unity\Editor\2022.3.62f3c1",
        "D:\unity\编辑器\2022.3.62f3c1"
    )

    foreach ($root in $commonEditorRoots) {
        $exe = Join-UnityExe $root
        if (Test-UnityExecutable $exe) {
            return (Resolve-Path -LiteralPath $exe).Path
        }
    }

    throw "Unity.exe was not found. Pass -UnityPath explicitly or install Unity 2022.3.62f3c1."
}

function Write-RunnerStatus([string]$Message) {
    $line = "[UnityRunner] $Message"
    Write-Host $line
    if (-not [string]::IsNullOrWhiteSpace($script:runnerLogPath)) {
        Add-Content -LiteralPath $script:runnerLogPath -Encoding UTF8 -Value $line
    }
}

function Stop-StartedUnityProcess([System.Diagnostics.Process]$Process, [string]$Reason) {
    if ($null -eq $Process -or $Process.HasExited) {
        return
    }

    Write-RunnerStatus "Stopping Unity PID $($Process.Id): $Reason"
    try {
        Stop-Process -Id $Process.Id -Force -ErrorAction Stop
        $Process.WaitForExit(10000) | Out-Null
    }
    catch {
        Write-RunnerStatus "Failed to stop Unity PID $($Process.Id): $($_.Exception.Message)"
    }
}

function Remove-StaleUnityLock([string]$ResolvedProjectPath) {
    $lockPath = Join-Path $ResolvedProjectPath "Temp\UnityLockfile"
    if (-not (Test-Path -LiteralPath $lockPath -PathType Leaf)) {
        return
    }

    $unityProcesses = @(Get-Process Unity -ErrorAction SilentlyContinue)
    if ($unityProcesses.Count -gt 0) {
        $ids = ($unityProcesses | Select-Object -ExpandProperty Id) -join ", "
        throw "Temp\UnityLockfile exists and Unity process(es) are running: $ids. Close Unity or stop the stale process before running batchmode."
    }

    if ($KeepStaleLock) {
        throw "Temp\UnityLockfile exists but no Unity process is running. Re-run without -KeepStaleLock to remove the stale lock."
    }

    Remove-Item -LiteralPath $lockPath -Force
    Write-RunnerStatus "Removed stale lock: $lockPath"
}

if ($TimeoutSeconds -le 0) {
    throw "-TimeoutSeconds must be greater than 0."
}

if ($StartupLogTimeoutSeconds -le 0) {
    throw "-StartupLogTimeoutSeconds must be greater than 0."
}

$repoRoot = Resolve-RepoRoot
if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = $repoRoot
}

$ProjectPath = (Resolve-Path -LiteralPath $ProjectPath).Path
$unityExe = Resolve-UnityExecutable $UnityPath

$logDir = Join-Path $repoRoot "Logs\UnityBatchmode"
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

if ([string]::IsNullOrWhiteSpace($LogName)) {
    $safeMethod = $ExecuteMethod -replace "[^A-Za-z0-9_.-]", "_"
    $LogName = "$safeMethod.log"
}

$logPath = Join-Path $logDir $LogName
$script:runnerLogPath = "$logPath.runner.log"

if (Test-Path -LiteralPath $script:runnerLogPath) {
    Remove-Item -LiteralPath $script:runnerLogPath -Force
}

$arguments = @(
    "-batchmode"
)

if (-not $UseGraphics) {
    $arguments += "-nographics"
}

$arguments += @(
    "-projectPath", $ProjectPath,
    "-executeMethod", $ExecuteMethod,
    "-logFile", $logPath
)

if (-not $NoQuit) {
    $arguments += "-quit"
}

if ($AdditionalArgs.Count -gt 0) {
    $arguments += $AdditionalArgs
}

Write-RunnerStatus "Unity       : $unityExe"
Write-RunnerStatus "ProjectPath : $ProjectPath"
Write-RunnerStatus "Method      : $ExecuteMethod"
Write-RunnerStatus "Log         : $logPath"
Write-RunnerStatus "RunnerLog   : $script:runnerLogPath"
Write-RunnerStatus "Timeout     : ${TimeoutSeconds}s total, ${StartupLogTimeoutSeconds}s startup log"
Write-RunnerStatus "Arguments   : $($arguments -join ' ')"

if ($DryRun) {
    exit 0
}

Remove-StaleUnityLock $ProjectPath

if (Test-Path -LiteralPath $logPath) {
    Remove-Item -LiteralPath $logPath -Force
}

$process = Start-Process -FilePath $unityExe -ArgumentList $arguments -PassThru -WindowStyle Hidden
$start = Get-Date
$logSeen = $false
Write-RunnerStatus "Started Unity PID $($process.Id)."

try {
    while (-not $process.HasExited) {
        Start-Sleep -Seconds 2
        $elapsed = [int]((Get-Date) - $start).TotalSeconds

        if (-not $logSeen -and (Test-Path -LiteralPath $logPath -PathType Leaf)) {
            $logSeen = $true
            $size = (Get-Item -LiteralPath $logPath).Length
            Write-RunnerStatus "Unity log created after ${elapsed}s (${size} bytes)."
        }

        if (-not $logSeen -and $elapsed -ge $StartupLogTimeoutSeconds) {
            Stop-StartedUnityProcess $process "startup log was not created within ${StartupLogTimeoutSeconds}s"
            Write-Error "Unity startup timed out before creating log: $logPath"
            exit 124
        }

        if ($elapsed -ge $TimeoutSeconds) {
            Stop-StartedUnityProcess $process "total timeout ${TimeoutSeconds}s exceeded"
            Write-Error "Unity batchmode timed out after ${TimeoutSeconds}s. See runner log: $script:runnerLogPath"
            exit 124
        }
    }
}
finally {
    if ($null -ne $process) {
        $process.Refresh()
    }
}

$exitCode = $process.ExitCode
Write-RunnerStatus "Unity exited with code $exitCode."

if ($exitCode -ne 0) {
    Write-Error "Unity batchmode failed with exit code $exitCode. See log: $logPath"
    exit $exitCode
}

if (-not (Test-Path -LiteralPath $logPath -PathType Leaf)) {
    Write-Error "Unity exited with code 0 but did not create log: $logPath"
    exit 125
}

Write-RunnerStatus "Completed successfully."
exit 0
