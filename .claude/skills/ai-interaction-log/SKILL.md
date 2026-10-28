---
name: ai-interaction-log
description: Use when you need to record, read or summarise the AI interaction log (ai-logs/ai-interactions.jsonl), or when working in a tool whose hooks do not fire automatically and the interaction must be logged by hand.
---

# AI interaction log

Every prompt, tool call, sub-agent answer and final answer is appended to `ai-logs/ai-interactions.jsonl` by `.claude/hooks/log-interaction.mjs`. One JSON object per line, always the same keys:

| key | meaning |
| --- | --- |
| `ts` | ISO-8601 UTC timestamp |
| `session` | agent session id (`manual` for hand entries) |
| `event` | `UserPromptSubmit`, `PostToolUse`, `SubagentStop`, `Stop` or `Manual` |
| `agent` | `main` or the sub-agent name (`implementer`, `code-reviewer`, ...) |
| `kind` | `prompt`, `tool`, `subagent-response`, `response`, `note` |
| `summary` | one line, at most 160 chars |
| `detail` | full text, clipped at 2,000 chars, bearer tokens redacted |

## Logging by hand (Copilot chat, web chat, or any tool without hooks)

Pipe the same JSON the hook would receive into the same script, so the line format never differs:

```bash
echo '{"hook_event_name":"Manual","agent":"main","kind":"prompt","summary":"Implement step 1","detail":"<full prompt>"}' \
  | node .claude/hooks/log-interaction.mjs
```

or use the wrapper: `node scripts/ai-log.mjs prompt main "short summary" "full text"`.

## Reading it

- Count by kind: `node scripts/ai-log-summary.mjs`
- Prompts only: `grep '"kind":"prompt"' ai-logs/ai-interactions.jsonl`

## Rules

- Never edit or delete existing lines; append a correcting `note` instead.
- Never paste secrets into a prompt; the redaction only covers bearer tokens.
