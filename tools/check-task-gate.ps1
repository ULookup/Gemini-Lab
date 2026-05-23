param(
    [ValidateSet("write", "review")]
    [string]$Mode = "write"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$taskCardPath = Join-Path $repoRoot "docs/current-task-card.json"

if (-not (Test-Path $taskCardPath)) {
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
exit 0
