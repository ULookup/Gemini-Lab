param(
    [Parameter(Mandatory = $true)]
    [string]$TaskCardPath,
    [ValidateSet("write", "review")]
    [string]$Mode = "review"
)

$ErrorActionPreference = "Stop"
$scriptRoot = Split-Path -Parent $PSScriptRoot

if (-not (Test-Path -LiteralPath $TaskCardPath)) {
    Write-Host "[SceneVisualGate] FAILED" -ForegroundColor Red
    Write-Host " - missing task card: $TaskCardPath" -ForegroundColor Yellow
    exit 1
}

try {
    $task = (Get-Content -LiteralPath $TaskCardPath -Raw -Encoding UTF8) | ConvertFrom-Json
}
catch {
    Write-Host "[SceneVisualGate] FAILED" -ForegroundColor Red
    Write-Host " - task card JSON could not be parsed" -ForegroundColor Yellow
    exit 1
}

if ($task.scene_play_parity_required -ne $true) {
    Write-Host "[SceneVisualGate] SKIPPED (scene_play_parity_required=false)" -ForegroundColor DarkYellow
    exit 0
}

$errors = New-Object System.Collections.Generic.List[string]
$contracts = @($task.scene_visual_contracts)

function Resolve-RepoPath([string]$pathValue) {
    if ([string]::IsNullOrWhiteSpace($pathValue)) { return $null }
    if ([System.IO.Path]::IsPathRooted($pathValue)) { return $pathValue }
    return Join-Path $scriptRoot $pathValue
}

function Read-YamlObjectBlocks([string[]]$lines) {
    $blocks = New-Object System.Collections.Generic.List[object]
    $current = New-Object System.Collections.Generic.List[string]

    foreach ($line in $lines) {
        if ($line -match '^--- !u!\d+ &\d+') {
            if ($current.Count -gt 0) {
                $blocks.Add(@($current))
                $current = New-Object System.Collections.Generic.List[string]
            }
        }
        $current.Add([string]$line)
    }

    if ($current.Count -gt 0) {
        $blocks.Add(@($current))
    }

    return $blocks
}

function Find-GameObject([object[]]$blocks, [string]$objectName) {
    foreach ($block in $blocks) {
        $lines = @($block)
        if ($lines.Count -eq 0 -or $lines[0] -notmatch '^--- !u!1 &(?<id>\d+)') {
            continue
        }

        foreach ($line in $lines) {
            if ($line -match '^\s*m_Name:\s*(?<name>.*)$' -and $Matches["name"].Trim() -eq $objectName) {
                return [pscustomobject]@{
                    Id = $Matches["id"]
                    Lines = $lines
                }
            }
        }
    }

    return $null
}

function Has-NonEmptySpriteReference([object[]]$blocks, [string]$gameObjectId) {
    foreach ($block in $blocks) {
        $lines = @($block)
        $belongsToGameObject = $false
        $hasSpriteField = $false
        $spriteFileId = "0"

        foreach ($line in $lines) {
            if ($line -match '^\s*m_GameObject:\s*\{fileID:\s*(?<id>\d+)\}') {
                if ($Matches["id"] -eq $gameObjectId) {
                    $belongsToGameObject = $true
                }
            }

            if ($line -match '^\s*m_Sprite:\s*\{fileID:\s*(?<fileId>\d+)') {
                $hasSpriteField = $true
                $spriteFileId = $Matches["fileId"]
            }
        }

        if ($belongsToGameObject -and $hasSpriteField -and $spriteFileId -ne "0") {
            return $true
        }
    }

    return $false
}

if ($contracts.Count -eq 0) {
    $errors.Add("scene_play_parity_required=true requires at least one scene_visual_contracts entry")
}

foreach ($contract in $contracts) {
    $sceneValue = [string]$contract.scene
    if ([string]::IsNullOrWhiteSpace($sceneValue)) {
        $errors.Add("scene_visual_contract entry is missing scene")
        continue
    }

    $scenePath = Resolve-RepoPath $sceneValue
    if (-not (Test-Path -LiteralPath $scenePath)) {
        $errors.Add("scene file does not exist: $sceneValue")
        continue
    }

    $sceneLines = Get-Content -LiteralPath $scenePath -Encoding UTF8
    $blocks = @(Read-YamlObjectBlocks $sceneLines)
    $requiredNodes = @($contract.required_nodes)
    $spriteNodes = @($contract.require_sprite_nodes)

    if ($requiredNodes.Count -eq 0) {
        $errors.Add("scene contract has no required_nodes: $sceneValue")
        continue
    }

    foreach ($node in $requiredNodes) {
        $nodeName = [string]$node
        if ([string]::IsNullOrWhiteSpace($nodeName)) {
            $errors.Add("scene contract contains an empty required node name: $sceneValue")
            continue
        }

        $gameObject = Find-GameObject $blocks $nodeName
        if ($null -eq $gameObject) {
            $errors.Add("scene node is missing: '$nodeName' in $sceneValue")
            continue
        }

        if ($spriteNodes -contains $nodeName -and -not (Has-NonEmptySpriteReference $blocks $gameObject.Id)) {
            $errors.Add("scene node has no serialized non-empty Sprite reference: '$nodeName' in $sceneValue")
        }
    }
}

if ($errors.Count -gt 0) {
    Write-Host "[SceneVisualGate] FAILED" -ForegroundColor Red
    foreach ($errorMessage in $errors) {
        Write-Host " - $errorMessage" -ForegroundColor Yellow
    }
    exit 1
}

Write-Host "[SceneVisualGate] PASSED" -ForegroundColor Green
Write-Host " contracts: $($contracts.Count)"
exit 0
