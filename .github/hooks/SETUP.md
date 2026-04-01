# Agent Logging Hooks Setup

Share these files with your friend to replicate this hook setup:

## Files Required
- `.github/hooks/agent-logging.json` — hook event configuration
- `.github/hooks/log-hook.ps1` — PowerShell logging script

## Installation

1. Copy both files to your workspace at `.github/hooks/`
2. VS Code will automatically detect and load hooks from this location.

## What This Does

Automatically logs all agent sessions and tool use:

- **agent_log.txt** — Human-readable text log of all events
- **agent_log.jsonl** — Structured JSON log of all events
- **chat-events/<session_id>.jsonl** — Events grouped by chat session (PreToolUse, PostToolUse, Stop only)
- **transcripts/<event_name>/** — Transcript files copied per event type
- **transcripts/chat-events/<session_id>.jsonl** — Grouped chat events

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
