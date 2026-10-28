#!/usr/bin/env node
// Appends one line per AI interaction to ai-logs/ai-interactions.jsonl.
// Wired to UserPromptSubmit, PostToolUse, SubagentStop and Stop in .claude/settings.json.
// Every line has the same shape, so the log can be read with jq or loaded as a table:
//   { ts, session, event, agent, kind, summary, detail }
// It never blocks the agent: any error is swallowed and the hook exits 0.

import { appendFileSync, mkdirSync, readFileSync, existsSync } from 'node:fs';
import { join, dirname } from 'node:path';

const MAX_DETAIL = 2000;

function readStdin() {
  try {
    return readFileSync(0, 'utf8');
  } catch {
    return '';
  }
}

function clip(text, max = MAX_DETAIL) {
  if (text === undefined || text === null) return '';
  const value = typeof text === 'string' ? text : JSON.stringify(text);
  return value.length > max ? `${value.slice(0, max)}… [${value.length - max} more chars]` : value;
}

function redact(text) {
  // Demo passwords are public, but keep anything that looks like a bearer token out of the log.
  return text.replace(/Bearer\s+[A-Za-z0-9._-]{16,}/g, 'Bearer [redacted]');
}

function lastAssistantText(transcriptPath) {
  if (!transcriptPath || !existsSync(transcriptPath)) return '';
  const lines = readFileSync(transcriptPath, 'utf8').trim().split('\n').reverse();
  for (const line of lines) {
    try {
      const entry = JSON.parse(line);
      const message = entry.message ?? entry;
      if (message.role !== 'assistant') continue;
      const content = Array.isArray(message.content) ? message.content : [{ type: 'text', text: message.content }];
      const text = content.filter((part) => part.type === 'text').map((part) => part.text).join('\n');
      if (text) return text;
    } catch {
      // skip malformed lines
    }
  }
  return '';
}

function describe(input) {
  const event = input.hook_event_name ?? 'Manual';
  switch (event) {
    case 'UserPromptSubmit':
      return { kind: 'prompt', summary: clip(input.prompt, 160), detail: input.prompt };
    case 'PreToolUse':
    case 'PostToolUse': {
      const toolInput = input.tool_input ?? {};
      const target = toolInput.command ?? toolInput.file_path ?? toolInput.pattern ?? toolInput.description ?? '';
      return {
        kind: 'tool',
        summary: `${input.tool_name}: ${clip(target, 140)}`,
        detail: { input: toolInput, response: clip(input.tool_response, 600) },
      };
    }
    case 'SubagentStop': {
      const text = input.last_assistant_message ?? lastAssistantText(input.agent_transcript_path ?? input.transcript_path);
      return { kind: 'subagent-response', summary: clip(text, 160), detail: text };
    }
    case 'Stop': {
      const text = input.last_assistant_message ?? lastAssistantText(input.transcript_path);
      return { kind: 'response', summary: clip(text, 160), detail: text };
    }
    default:
      return { kind: input.kind ?? 'note', summary: clip(input.summary ?? '', 160), detail: input.detail ?? '' };
  }
}

try {
  const raw = readStdin();
  const input = raw.trim() ? JSON.parse(raw) : {};
  const projectDir = process.env.CLAUDE_PROJECT_DIR ?? input.cwd ?? process.cwd();
  const logFile = join(projectDir, 'ai-logs', 'ai-interactions.jsonl');
  mkdirSync(dirname(logFile), { recursive: true });

  const { kind, summary, detail } = describe(input);
  const entry = {
    ts: input.ts ?? new Date().toISOString(),
    session: input.session_id ?? 'manual',
    event: input.hook_event_name ?? 'Manual',
    agent: input.agent_type ?? input.agent ?? 'main',
    kind,
    summary: redact(summary),
    detail: redact(clip(detail)),
  };
  appendFileSync(logFile, `${JSON.stringify(entry)}\n`);
} catch {
  // Logging must never break the agent.
}
process.exit(0);
