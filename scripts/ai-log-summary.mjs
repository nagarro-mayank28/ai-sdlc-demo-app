#!/usr/bin/env node
// Prints a count of logged AI interactions by agent and kind.
import { readFileSync } from 'node:fs';
const lines = readFileSync(new URL('../ai-logs/ai-interactions.jsonl', import.meta.url), 'utf8').trim().split('\n');
const counts = {};
for (const line of lines) {
  const { agent, kind } = JSON.parse(line);
  const key = `${agent.padEnd(16)} ${kind}`;
  counts[key] = (counts[key] ?? 0) + 1;
}
console.log(`${lines.length} entries`);
for (const [key, count] of Object.entries(counts).sort()) console.log(`${String(count).padStart(4)}  ${key}`);
