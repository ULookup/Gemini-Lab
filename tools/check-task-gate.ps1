param(
    [ValidateSet("write", "review")]
    [string]$Mode = "write"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$taskCardPath = Join-Path $repoRoot "docs/current-task-card.json"

if (-not (Test-Path -LiteralPath $taskCardPath)) {
    Write-Error "Task gate failed: missing docs/current-task-card.json"
    exit 1
}

try {
    $raw = Get-Content -LiteralPath $taskCardPath -Raw -Encoding UTF8
    $task = $raw | ConvertFrom-Json
}
catch {
    Write-Error "Task gate failed: current-task-card.json is not valid JSON."
    exit 1
}

$errors = New-Object System.Collections.Generic.List[string]

function Test-NonEmptyString($value) {
    return ($null -ne $value) -and ($value -is [string]) -and (-not [string]::IsNullOrWhiteSpace($value))
}

function Test-NonEmptyArray($value) {
    if ($null -eq $value) { return $false }
    if ($value -is [string]) { return $false }
    return @($value).Count -gt 0
}

function Test-PathMatchesAny([string]$pathValue, [string[]]$patterns) {
    foreach ($pattern in $patterns) {
        if ($pathValue -match $pattern) { return $true }
    }
    return $false
}

$allowedStatus = @("planned", "approved", "executing", "done")

if (-not (Test-NonEmptyString $task.status) -or ($allowedStatus -notcontains $task.status)) {
    $errors.Add("status must be one of: planned, approved, executing, done")
}

if (-not (Test-NonEmptyString $task.task_source)) {
    $errors.Add("task_source must be a non-empty string")
}

if (-not (Test-NonEmptyString $task.source_excerpt)) {
    $errors.Add("source_excerpt must be a non-empty string")
}

if (-not (Test-NonEmptyArray $task.scope_do)) {
    $errors.Add("scope_do must contain at least one item")
}

if (-not (Test-NonEmptyArray $task.scope_not_do)) {
    $errors.Add("scope_not_do must contain at least one item")
}

if (-not (Test-NonEmptyArray $task.completion_criteria)) {
    $errors.Add("completion_criteria must contain at least one item")
}

if (-not (Test-NonEmptyArray $task.direct_files)) {
    $errors.Add("direct_files must contain at least one item")
}

if (-not ($task.PSObject.Properties.Name -contains "approved")) {
    $errors.Add("approved field is missing")
}
elseif ($task.approved -ne $true -and $Mode -eq "write") {
    $errors.Add("approved must be true before write operations")
}

if (-not ($task.PSObject.Properties.Name -contains "scene_play_parity_required")) {
    $errors.Add("scene_play_parity_required field is missing; every task must declare Scene/Play parity scope")
}
elseif ($task.scene_play_parity_required -isnot [bool]) {
    $errors.Add("scene_play_parity_required must be a boolean")
}

if (-not ($task.PSObject.Properties.Name -contains "scene_visual_contracts")) {
    $errors.Add("scene_visual_contracts field is missing; use [] for non-visual tasks")
}

if (-not ($task.PSObject.Properties.Name -contains "runtime_visual_files")) {
    $errors.Add("runtime_visual_files field is missing; use [] when no runtime visual code is in scope")
}

$directFiles = @($task.direct_files | ForEach-Object { [string]$_ })
$visualPatterns = @(
    "\.unity$",
    "^Assets/_Project/Art/",
    "^Assets/_Project/Scripts/(Modules|UI)/",
    "^Assets/_Project/Scripts/Editor/(SceneBootstrap|Tools)/"
)
$isVisualTask = $false
foreach ($directFile in $directFiles) {
    if (Test-PathMatchesAny $directFile $visualPatterns) {
        $isVisualTask = $true
        break
    }
}

if ($isVisualTask -and $task.scene_play_parity_required -ne $true) {
    $errors.Add("visual task detected from direct_files; scene_play_parity_required must be true")
}

if ($task.scene_play_parity_required -eq $true) {
    if (-not (Test-NonEmptyArray $task.scene_visual_contracts)) {
        $errors.Add("scene_play_parity_required=true requires non-empty scene_visual_contracts")
    }

    $runtimeDirectFiles = @($directFiles | Where-Object {
        $_ -match "^Assets/_Project/Scripts/(Core|Modules|UI)/"
    })
    if ($runtimeDirectFiles.Count -gt 0 -and -not (Test-NonEmptyArray $task.runtime_visual_files)) {
        $errors.Add("runtime code is in direct_files; declare runtime_visual_files for the visual contract scan")
    }
}

if ($errors.Count -eq 0 -and $task.scene_play_parity_required -eq $true) {
    $sceneChecker = Join-Path $repoRoot "tools/check-scene-visual-contract.ps1"
    $runtimeChecker = Join-Path $repoRoot "tools/check-runtime-visual-contract.ps1"

    if (-not (Test-Path -LiteralPath $sceneChecker)) {
        $errors.Add("missing Scene visual contract checker: tools/check-scene-visual-contract.ps1")
    }
    else {
        $sceneOutput = & $sceneChecker -TaskCardPath $taskCardPath -Mode $Mode 2>&1
        $sceneExitCode = $LASTEXITCODE
        foreach ($outputLine in $sceneOutput) { Write-Host $outputLine }
        if ($sceneExitCode -ne 0) {
            $errors.Add("Scene visual contract checker failed")
        }
    }

    if (-not (Test-Path -LiteralPath $runtimeChecker)) {
        $errors.Add("missing runtime visual contract checker: tools/check-runtime-visual-contract.ps1")
    }
    else {
        $runtimeOutput = & $runtimeChecker -TaskCardPath $taskCardPath -Mode $Mode 2>&1
        $runtimeExitCode = $LASTEXITCODE
        foreach ($outputLine in $runtimeOutput) { Write-Host $outputLine }
        if ($runtimeExitCode -ne 0) {
            $errors.Add("runtime visual contract checker failed")
        }
    }
}

if ($Mode -eq "write" -and $task.status -notin @("approved", "executing", "done")) {
    $errors.Add("status must be approved/executing/done before write operations")
}

if ($errors.Count -gt 0) {
    Write-Host "[TaskGate] FAILED" -ForegroundColor Red
    foreach ($err in $errors) {
        Write-Host " - $err" -ForegroundColor Yellow
    }
    exit 1
}

Write-Host "[TaskGate] PASSED" -ForegroundColor Green
Write-Host " status   : $($task.status)"
Write-Host " source   : $($task.task_source)"
Write-Host " approved : $($task.approved)"
Write-Host " parity   : $($task.scene_play_parity_required)"
exit 0
