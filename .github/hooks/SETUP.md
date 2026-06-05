# Agent Logging Hooks Setup

Share these files to replicate the Copilot + Codex logging setup:

## Files Required
- `.github/hooks/agent-logging.json` — hook event configuration
- `.github/hooks/log-hook.ps1` — PowerShell logging script
- `.github/hooks/codex-logging.json` — Codex hook event configuration
- `.github/hooks/codex-log-hook.ps1` — Codex session log bridge

## Installation

1. Copy all files listed above to your workspace at `.github/hooks/`
2. VS Code/Copilot will automatically detect `agent-logging.json` from this location.
3. Codex logging is configured through `codex-logging.json`, which calls `codex-log-hook.ps1`.
4. To manually sync Codex CLI logs at any time, run:

   ```powershell
   powershell.exe -ExecutionPolicy Bypass -File ".github\hooks\codex-log-hook.ps1"
   ```

   Codex stores session transcripts in `%USERPROFILE%\.codex\sessions`; this script syncs new session lines from recent sessions into repo-local hook logs.

## What This Does

Automatically logs Copilot hook events and Codex session activity:

- **agent_log.txt** — Human-readable text log of all events
- **agent_log.jsonl** — Structured JSON log of all events
- **chat-events/<session_id>.jsonl** — Events grouped by chat session (PreToolUse, PostToolUse, Stop only)
- **transcripts/<event_name>/** — Transcript files copied per event type
- **transcripts/chat-events/<session_id>.jsonl** — Grouped chat events
- **codex_agent_log.txt** — Human-readable Codex log synced from local Codex sessions
- **codex_agent_log.jsonl** — Structured Codex JSONL log with normalized event names
- **codex-chat-events/<session_id>.jsonl** — Codex events grouped by Codex session/thread id
- **codex-log-state.json** — Internal sync state so repeated runs only append new Codex lines

## Events Tracked

- SessionStart
- UserPromptSubmit
- PreToolUse
- PostToolUse
- PreCompact
- SubagentStart
- SubagentStop
- Stop

## Platform Support

- Windows (PowerShell 5.1+)

No additional dependencies or configuration needed beyond copying the files.
