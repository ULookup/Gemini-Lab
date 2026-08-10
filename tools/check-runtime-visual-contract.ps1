param(
    [Parameter(Mandatory = $true)]
    [string]$TaskCardPath,
    [ValidateSet("write", "review")]
    [string]$Mode = "review"
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $PSScriptRoot

if (-not (Test-Path -LiteralPath $TaskCardPath)) {
    Write-Host "[RuntimeVisualGate] FAILED" -ForegroundColor Red
    Write-Host " - missing task card: $TaskCardPath" -ForegroundColor Yellow
    exit 1
}

try {
    $task = (Get-Content -LiteralPath $TaskCardPath -Raw -Encoding UTF8) | ConvertFrom-Json
}
catch {
    Write-Host "[RuntimeVisualGate] FAILED" -ForegroundColor Red
    Write-Host " - task card JSON could not be parsed" -ForegroundColor Yellow
    exit 1
}

if ($task.scene_play_parity_required -ne $true) {
    Write-Host "[RuntimeVisualGate] SKIPPED (scene_play_parity_required=false)" -ForegroundColor DarkYellow
    exit 0
}

$errors = New-Object System.Collections.Generic.List[string]
$runtimeFiles = @($task.runtime_visual_files)

if ($runtimeFiles.Count -eq 0) {
    Write-Host "[RuntimeVisualGate] PASSED (no runtime visual files declared)" -ForegroundColor Green
    exit 0
}

$forbiddenPatterns = @(
    @{ Name = "direct Sprite assignment"; Pattern = "\.\s*sprite\s*=" },
    @{ Name = "direct AnimatorController assignment"; Pattern = "\.\s*runtimeAnimatorController\s*=" },
    @{ Name = "runtime GameObject generation"; Pattern = "\bnew\s+GameObject\s*\(" },
    @{ Name = "runtime UI component generation"; Pattern = "\.\s*AddComponent\s*<\s*(?:UnityEngine\.UI\.)?(?:Image|RawImage|Canvas|RectTransform|Button|TextMeshProUGUI)\s*>" },
    @{ Name = "runtime visual prefab instantiation"; Pattern = "\b(?:UnityEngine\.)?Object\.Instantiate\s*\(" }
)

foreach ($fileValue in $runtimeFiles) {
    $relativePath = [string]$fileValue
    if ([string]::IsNullOrWhiteSpace($relativePath)) {
        $errors.Add("runtime_visual_files contains an empty path")
        continue
    }

    $filePath = if ([System.IO.Path]::IsPathRooted($relativePath)) { $relativePath } else { Join-Path $scriptRoot $relativePath }
    if (-not (Test-Path -LiteralPath $filePath)) {
        $errors.Add("runtime visual file does not exist: $relativePath")
        continue
    }

    $lines = Get-Content -LiteralPath $filePath -Encoding UTF8
    for ($lineIndex = 0; $lineIndex -lt $lines.Count; $lineIndex++) {
        $line = [string]$lines[$lineIndex]
        if ($line -match "^\s*//" -or $line -match "^\s*/\*") {
            continue
        }

        foreach ($forbidden in $forbiddenPatterns) {
            if ($line -match $forbidden.Pattern) {
                $errors.Add("$relativePath`:$($lineIndex + 1) forbidden $($forbidden.Name): $line.Trim()")
            }
        }
    }
}

if ($errors.Count -gt 0) {
    Write-Host "[RuntimeVisualGate] FAILED" -ForegroundColor Red
    foreach ($errorMessage in $errors) {
        Write-Host " - $errorMessage" -ForegroundColor Yellow
    }
    exit 1
}

Write-Host "[RuntimeVisualGate] PASSED" -ForegroundColor Green
Write-Host " files: $($runtimeFiles.Count)"
exit 0
