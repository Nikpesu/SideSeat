param(
    [string]$CodexHome = (Join-Path $env:USERPROFILE '.codex'),
    [string]$WorkspaceRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [int]$RecentSessionCount = 5
)

$ErrorActionPreference = 'Stop'

$logPath = Join-Path $PSScriptRoot 'codex_agent_log.txt'
$jsonlPath = Join-Path $PSScriptRoot 'codex_agent_log.jsonl'
$chatEventsDir = Join-Path $PSScriptRoot 'codex-chat-events'
$statePath = Join-Path $PSScriptRoot 'codex-log-state.json'

function Ensure-Directory {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path | Out-Null
    }
}

function ConvertTo-SafeFileName {
    param([string]$Value)

    $safeValue = $Value -replace '[^a-zA-Z0-9._-]', '_'
    if ([string]::IsNullOrWhiteSpace($safeValue)) {
        return 'unknown-session'
    }

    return $safeValue
}

function Get-SessionIdFromPath {
    param([string]$Path)

    $name = [System.IO.Path]::GetFileNameWithoutExtension($Path)
    if ($name -match '([0-9a-f]{4,8}(?:-[0-9a-f]{4}){3,}-[0-9a-f]{12})$') {
        return $Matches[1]
    }

    return $name
}

function Get-EventName {
    param($Record)

    if ($null -ne $Record.payload -and $null -ne $Record.payload.type) {
        if ($Record.type -eq 'response_item' -and $Record.payload.type -eq 'function_call') {
            return 'PreToolUse'
        }

        if ($Record.type -eq 'response_item' -and $Record.payload.type -eq 'function_call_output') {
            return 'PostToolUse'
        }

        if ($Record.type -eq 'event_msg' -and $Record.payload.type -eq 'agent_message') {
            return 'AgentMessage'
        }

        if ($Record.type -eq 'event_msg' -and $Record.payload.type -eq 'token_count') {
            return 'TokenCount'
        }

        if ($Record.type -eq 'response_item' -and $Record.payload.type -eq 'message') {
            return 'Message'
        }

        return [string]$Record.payload.type
    }

    if ($null -ne $Record.type) {
        return [string]$Record.type
    }

    return 'UnknownEvent'
}

function Get-ToolName {
    param($Record)

    if ($Record.type -eq 'response_item' -and $Record.payload.type -eq 'function_call' -and $null -ne $Record.payload.name) {
        return [string]$Record.payload.name
    }

    if ($Record.type -eq 'response_item' -and $Record.payload.type -eq 'function_call_output') {
        return 'function_call_output'
    }

    return $null
}

function Read-State {
    if (-not (Test-Path -LiteralPath $statePath)) {
        return @{}
    }

    try {
        $state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json -ErrorAction Stop
        $map = @{}
        foreach ($property in $state.PSObject.Properties) {
            $map[$property.Name] = [int64]$property.Value
        }

        return $map
    }
    catch {
        return @{}
    }
}

function Write-State {
    param([hashtable]$State)

    $State | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $statePath -Encoding UTF8
}

$sessionsRoot = Join-Path $CodexHome 'sessions'
if (-not (Test-Path -LiteralPath $sessionsRoot)) {
    throw "Codex sessions directory not found: $sessionsRoot"
}

Ensure-Directory -Path $chatEventsDir
$state = Read-State
$sessionFiles = Get-ChildItem -LiteralPath $sessionsRoot -Recurse -Filter '*.jsonl' |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First $RecentSessionCount

foreach ($sessionFile in $sessionFiles) {
    $sessionPath = $sessionFile.FullName
    $sessionId = Get-SessionIdFromPath -Path $sessionPath
    $safeSessionId = ConvertTo-SafeFileName -Value $sessionId
    $sessionOffset = if ($state.ContainsKey($sessionPath)) { [int64]$state[$sessionPath] } else { 0 }
    $lineNumber = 0
    $newOffset = $sessionOffset

    foreach ($line in Get-Content -LiteralPath $sessionPath) {
        $lineNumber++
        if ($lineNumber -le $sessionOffset -or [string]::IsNullOrWhiteSpace($line)) {
            continue
        }

        try {
            $parsedRecord = $line | ConvertFrom-Json -ErrorAction Stop
            $eventName = Get-EventName -Record $parsedRecord
            $toolName = Get-ToolName -Record $parsedRecord
            $recordTimestamp = if ($null -ne $parsedRecord.timestamp) { [string]$parsedRecord.timestamp } else { (Get-Date).ToString('o') }
            $cwd = if ($null -ne $parsedRecord.cwd) { [string]$parsedRecord.cwd } else { $WorkspaceRoot }

            $jsonlRecord = [pscustomobject]@{
                timestamp = (Get-Date).ToString('o')
                source = 'codex'
                hook_event_name = $eventName
                session_id = $sessionId
                transcript_path = $sessionPath
                transcript_line = $lineNumber
                event_timestamp = $recordTimestamp
                tool_name = $toolName
                cwd = $cwd
                payload = $parsedRecord
            }

            Add-Content -LiteralPath $jsonlPath -Value ($jsonlRecord | ConvertTo-Json -Compress -Depth 50)

            $textEntry = @(
                "=== Codex Event $($jsonlRecord.timestamp) ==="
                "event=$eventName session=$sessionId line=$lineNumber tool=$toolName"
                $line
                ""
            ) -join [Environment]::NewLine
            Add-Content -LiteralPath $logPath -Value $textEntry

            $sessionLogPath = Join-Path $chatEventsDir ($safeSessionId + '.jsonl')
            Add-Content -LiteralPath $sessionLogPath -Value ($jsonlRecord | ConvertTo-Json -Compress -Depth 50)
        }
        catch {
            $jsonlRecord = [pscustomobject]@{
                timestamp = (Get-Date).ToString('o')
                source = 'codex'
                hook_event_name = 'ParseError'
                session_id = $sessionId
                transcript_path = $sessionPath
                transcript_line = $lineNumber
                rawInput = $line
                error = $_.Exception.Message
            }
            Add-Content -LiteralPath $jsonlPath -Value ($jsonlRecord | ConvertTo-Json -Compress -Depth 10)
        }

        $newOffset = $lineNumber
    }

    $state[$sessionPath] = $newOffset
}

Write-State -State $state
