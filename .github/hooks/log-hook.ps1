$ErrorActionPreference = 'Stop'

$logPath = Join-Path $PSScriptRoot 'agent_log.txt'
$jsonlPath = Join-Path $PSScriptRoot 'agent_log.jsonl'
$timestamp = (Get-Date).ToString('o')

# Hook payload is sent on stdin as JSON.
$rawInput = [Console]::In.ReadToEnd()

$entry = @(
    "=== Hook Event $timestamp ==="
    $rawInput
    ""
) -join [Environment]::NewLine

Add-Content -Path $logPath -Value $entry

$parsedPayload = $null
try {
    if (-not [string]::IsNullOrWhiteSpace($rawInput)) {
        $parsedPayload = $rawInput | ConvertFrom-Json -ErrorAction Stop
    }
}
catch {
    $parsedPayload = $null
}

$jsonlRecord = if ($null -ne $parsedPayload) {
    [pscustomobject]@{
        timestamp = $timestamp
        payload = $parsedPayload
    }
}
else {
    [pscustomobject]@{
        timestamp = $timestamp
        rawInput = $rawInput
    }
}

Add-Content -Path $jsonlPath -Value ($jsonlRecord | ConvertTo-Json -Compress -Depth 20)

$trackedEvents = @('PreToolUse', 'PostToolUse', 'Stop')
$hookEventName = $null
$sessionId = $null
if ($null -ne $parsedPayload) {
    if ($null -ne $parsedPayload.hook_event_name) {
        $hookEventName = [string]$parsedPayload.hook_event_name
    }
    if ($null -ne $parsedPayload.session_id) {
        $sessionId = [string]$parsedPayload.session_id
    }
}

if (-not [string]::IsNullOrWhiteSpace($hookEventName) -and $trackedEvents -contains $hookEventName -and -not [string]::IsNullOrWhiteSpace($sessionId)) {
    $chatEventsDir = Join-Path $PSScriptRoot 'chat-events'
    if (-not (Test-Path -LiteralPath $chatEventsDir)) {
        New-Item -ItemType Directory -Path $chatEventsDir | Out-Null
    }

    $safeSessionId = ($sessionId -replace '[^a-zA-Z0-9._-]', '_')
    if ([string]::IsNullOrWhiteSpace($safeSessionId)) {
        $safeSessionId = 'unknown-session'
    }

    $sessionLogPath = Join-Path $chatEventsDir ($safeSessionId + '.jsonl')
    Add-Content -Path $sessionLogPath -Value ($jsonlRecord | ConvertTo-Json -Compress -Depth 20)
}

$transcriptSourcePath = $null
if ($null -ne $parsedPayload -and $null -ne $parsedPayload.transcript_path) {
    $transcriptSourcePath = [string]$parsedPayload.transcript_path
}

if (-not [string]::IsNullOrWhiteSpace($transcriptSourcePath) -and (Test-Path -LiteralPath $transcriptSourcePath)) {
    $transcriptRootDir = Join-Path $PSScriptRoot 'transcripts'
    if (-not (Test-Path -LiteralPath $transcriptRootDir)) {
        New-Item -ItemType Directory -Path $transcriptRootDir | Out-Null
    }

    $eventFolderName = 'UnknownEvent'
    if (-not [string]::IsNullOrWhiteSpace($hookEventName)) {
        $eventFolderName = $hookEventName
    }

    $safeEventName = ($eventFolderName -replace '[^a-zA-Z0-9._-]', '_')
    if ([string]::IsNullOrWhiteSpace($safeEventName)) {
        $safeEventName = 'UnknownEvent'
    }

    $transcriptDir = Join-Path $transcriptRootDir $safeEventName
    if (-not (Test-Path -LiteralPath $transcriptDir)) {
        New-Item -ItemType Directory -Path $transcriptDir | Out-Null
    }

    $transcriptName = [System.IO.Path]::GetFileName($transcriptSourcePath)
    $transcriptDestinationPath = Join-Path $transcriptDir $transcriptName
    Copy-Item -LiteralPath $transcriptSourcePath -Destination $transcriptDestinationPath -Force
}
