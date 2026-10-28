#!/usr/bin/env node
// Hand-logging wrapper: writes through the same hook script so the line format is identical.
// Usage: node scripts/ai-log.mjs <kind> <agent> "<summary>" "<detail>"
//   kind: prompt | response | subagent-response | tool | note
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const [kind = 'note', agent = 'main', summary = '', detail = ''] = process.argv.slice(2);
const root = join(dirname(fileURLToPath(import.meta.url)), '..');
const payload = { hook_event_name: 'Manual', kind, agent, summary, detail: detail || summary, cwd: root };
if (process.env.AI_LOG_TS) payload.ts = process.env.AI_LOG_TS;
if (process.env.AI_LOG_SESSION) payload.session_id = process.env.AI_LOG_SESSION;
spawnSync('node', [join(root, '.claude/hooks/log-interaction.mjs')], {
  input: JSON.stringify(payload),
  env: { ...process.env, CLAUDE_PROJECT_DIR: root },
  stdio: ['pipe', 'inherit', 'inherit'],
});
