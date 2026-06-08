[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Image,

    [Parameter(Mandatory = $true)]
    [string]$CurrentVersion,

    [string]$NewVersion,

    [string]$Dockerfile = "Dockerfile",

    [string]$Context = ".",

    [string]$ChangelogDirectory = "changelogs",

    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

function Normalize-Version {
    param([Parameter(Mandatory = $true)][string]$Version)

    $normalized = $Version.Trim().ToLowerInvariant()
    if (-not $normalized.StartsWith("v")) {
        $normalized = "v$normalized"
    }

    if ($normalized -notmatch '^v\d+\.\d+(?:\.\d+)?$') {
        throw "Version '$Version' must use vX.Y or vX.Y.Z format."
    }

    return $normalized
}

function Get-Next-Version {
    param([Parameter(Mandatory = $true)][string]$Version)

    $normalized = Normalize-Version $Version
    $parts = $normalized.Substring(1).Split(".")
    $parts[1] = ([int]$parts[1] + 1).ToString()
    return "v$($parts -join '.')"
}

function Invoke-Docker {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    Write-Host "docker $($Arguments -join ' ')"
    if ($DryRun) {
        return
    }

    & docker @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Docker command failed: docker $($Arguments -join ' ')"
    }
}

$normalizedImage = $Image.Trim().TrimEnd(":")
if ($normalizedImage -notmatch '^[a-z0-9]+(?:[._-][a-z0-9]+)*/[a-z0-9]+(?:[._-][a-z0-9]+)*$') {
    throw "Image '$Image' must use Docker Hub owner/repository format."
}

$current = Normalize-Version $CurrentVersion
$selected = if ([string]::IsNullOrWhiteSpace($NewVersion)) {
    Get-Next-Version $current
} else {
    Normalize-Version $NewVersion
}

if (-not (Test-Path -LiteralPath $Dockerfile)) {
    throw "Dockerfile not found: $Dockerfile"
}

if (-not (Test-Path -LiteralPath $Context)) {
    throw "Build context not found: $Context"
}

$changelogPath = Join-Path $ChangelogDirectory "$selected.md"
if (-not (Test-Path -LiteralPath $changelogPath)) {
    throw "Missing changelog: $changelogPath"
}

if ((Get-Content -LiteralPath $changelogPath -Raw) -match '\bTODO\b') {
    throw "Changelog contains TODO placeholders: $changelogPath"
}

Invoke-Docker -Arguments @("info", "--format", "{{.OSType}}/{{.Architecture}}")
Invoke-Docker -Arguments @(
    "build",
    "--pull",
    "-f", $Dockerfile,
    "--build-arg", "APP_VERSION=$selected",
    "--label", "org.opencontainers.image.version=$selected",
    "--label", "com.sideseat.container.version=$selected",
    "-t", "${normalizedImage}:${selected}",
    "-t", "${normalizedImage}:latest",
    $Context
)
Invoke-Docker -Arguments @("push", "${normalizedImage}:${selected}")
Invoke-Docker -Arguments @("push", "${normalizedImage}:latest")
Invoke-Docker -Arguments @("buildx", "imagetools", "inspect", "${normalizedImage}:${selected}")
Invoke-Docker -Arguments @("buildx", "imagetools", "inspect", "${normalizedImage}:latest")

Write-Host "Published ${normalizedImage}:${selected} and ${normalizedImage}:latest"
