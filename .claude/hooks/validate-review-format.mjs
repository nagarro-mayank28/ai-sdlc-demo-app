#!/usr/bin/env node
// SubagentStop hook. When the code-reviewer agent finishes, its answer must use the one review
// format every PR in this repo uses (see .claude/skills/review-pr/SKILL.md):
//
//   ## Review findings
//   | ID | Severity | File:line | Finding | Suggested fix |
//   | --- | --- | --- | --- | --- |
//   | R1 | high | backend/...:42 | ... | ... |
//   Summary: <one line>
//
// If the format is wrong, exit 2 sends the reason back and the reviewer rewrites its answer.
// Other agents pass straight through. Run by hand with: node validate-review-format.mjs --file review.md

import { readFileSync, existsSync } from 'node:fs';

const HEADER = /^\|\s*ID\s*\|\s*Severity\s*\|\s*File:line\s*\|\s*Finding\s*\|\s*Suggested fix\s*\|\s*$/m;
const ROW = /^\|\s*R\d+\s*\|\s*(blocker|high|medium|low|nit)\s*\|[^|]+\|[^|]+\|[^|]+\|\s*$/gm;

export function reviewProblems(text) {
  const problems = [];
  if (!/^## Review findings\s*$/m.test(text)) problems.push('Start the findings with the heading "## Review findings".');
  if (!HEADER.test(text)) problems.push('Use the table header: | ID | Severity | File:line | Finding | Suggested fix |');
  const rows = text.match(ROW) ?? [];
  if (rows.length === 0 && !/^No findings\.\s*$/m.test(text)) {
    problems.push('Add at least one row like | R1 | high | path:line | finding | fix | (severity: blocker, high, medium, low, nit), or write "No findings."');
  }
  if (!/^Summary:\s*\S+/m.test(text)) problems.push('End with a line "Summary: <one sentence>".');
  return problems;
}

function lastAssistantText(path) {
  if (!path || !existsSync(path)) return '';
  for (const line of readFileSync(path, 'utf8').trim().split('\n').reverse()) {
    try {
      const message = JSON.parse(line).message;
      if (message?.role !== 'assistant') continue;
      const parts = Array.isArray(message.content) ? message.content : [{ type: 'text', text: message.content }];
      const text = parts.filter((part) => part.type === 'text').map((part) => part.text).join('\n');
      if (text) return text;
    } catch {
      // ignore
    }
  }
  return '';
}

const fileIndex = process.argv.indexOf('--file');
if (fileIndex > -1) {
  const problems = reviewProblems(readFileSync(process.argv[fileIndex + 1], 'utf8'));
  console.log(problems.length ? problems.join('\n') : 'ok');
  process.exit(problems.length ? 1 : 0);
}

let input = {};
try {
  input = JSON.parse(readFileSync(0, 'utf8') || '{}');
} catch {
  process.exit(0);
}

if (input.agent_type && input.agent_type !== 'code-reviewer') process.exit(0);
if (input.stop_hook_active) process.exit(0); // already asked once; do not loop

const text = input.last_assistant_message ?? lastAssistantText(input.agent_transcript_path ?? input.transcript_path);
if (!text || !/review/i.test(text)) process.exit(0);

const problems = reviewProblems(text);
if (problems.length) {
  console.error(`Review output is not in the house format:\n- ${problems.join('\n- ')}\nRewrite the review in that format.`);
  process.exit(2);
}
process.exit(0);
