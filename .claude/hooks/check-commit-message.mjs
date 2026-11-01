#!/usr/bin/env node
// PreToolUse hook on Bash. When the agent runs `git commit -m ...`, the message must be:
//
//   type(scope): summary            <- feat|fix|test|docs|chore|refactor, summary 10-72 chars
//                                   <- blank line (a second -m gives you this)
//   Body that says what changed.
//   Why: the reason for the change.
//
// Exit code 2 blocks the command and sends the reason back to the agent so it can retry.
// Can also be run by hand: node .claude/hooks/check-commit-message.mjs --message "feat(x): ..."

import { readFileSync } from 'node:fs';

const SUBJECT = /^(feat|fix|test|docs|chore|refactor)(\([a-z0-9-]+\))?: .{10,72}$/;

function messagesFromCommand(command) {
  const messages = [];
  const pattern = /(?:-m|--message)(?:=|\s+)(?:"((?:[^"\\]|\\.)*)"|'([^']*)'|(\S+))/g;
  let match;
  while ((match = pattern.exec(command)) !== null) {
    messages.push((match[1] ?? match[2] ?? match[3]).replace(/\\"/g, '"'));
  }
  return messages;
}

export function problemsWith(fullMessage) {
  const problems = [];
  const [subject = '', ...rest] = fullMessage.split('\n');
  const body = rest.join('\n').trim();
  if (!SUBJECT.test(subject.trim())) {
    problems.push(`First line "${subject}" must look like "feat(approvals): add manager limit" (types: feat, fix, test, docs, chore, refactor; 10-72 chars after the colon).`);
  }
  if (!body) problems.push('Add a body after a blank line saying what changed.');
  if (!/^Why:\s*\S+/m.test(body)) problems.push('The body needs a line starting with "Why:" giving the reason.');
  return problems;
}

function main() {
  const manualIndex = process.argv.indexOf('--message');
  if (manualIndex > -1) {
    const problems = problemsWith(process.argv[manualIndex + 1] ?? '');
    console.log(problems.length ? problems.join('\n') : 'ok');
    process.exit(problems.length ? 1 : 0);
  }

  let input = {};
  try {
    input = JSON.parse(readFileSync(0, 'utf8') || '{}');
  } catch {
    process.exit(0);
  }
  const command = input.tool_input?.command ?? '';
  if (!/\bgit\s+commit\b/.test(command)) process.exit(0);

  const messages = messagesFromCommand(command);
  if (messages.length === 0) process.exit(0); // -F file or editor: not ours to check

  const problems = problemsWith(messages.join('\n\n'));
  if (problems.length) {
    console.error(`Commit message rejected by check-commit-message hook:\n- ${problems.join('\n- ')}`);
    process.exit(2);
  }
  process.exit(0);
}

main();
