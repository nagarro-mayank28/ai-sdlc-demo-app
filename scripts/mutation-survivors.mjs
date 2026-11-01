#!/usr/bin/env node
// Lists every surviving (or uncovered) mutant in a Stryker JSON report, one per line:
//   ApprovalService.cs:75  StringMutation  "..." -> ""
// Usage: node scripts/mutation-survivors.mjs [path/to/mutation-report.json]
import { readFileSync } from 'node:fs';

const reportPath = process.argv[2] ?? 'docs/mutation/after/reports/mutation-report.json';
const report = JSON.parse(readFileSync(reportPath, 'utf8'));
let count = 0;

for (const [path, file] of Object.entries(report.files)) {
  const name = path.split(/[\\/]/).pop();
  const lines = file.source.split(/\r?\n/);
  for (const mutant of file.mutants) {
    if (!['Survived', 'NoCoverage'].includes(mutant.status)) continue;
    count++;
    const { start, end } = mutant.location;
    const original = start.line === end.line
      ? lines[start.line - 1].slice(start.column - 1, end.column - 1)
      : `${lines[start.line - 1].slice(start.column - 1).trim()} ...`;
    const replacement = mutant.replacement ?? '(removed)';
    console.log(`${name}:${start.line}  ${mutant.status}  ${mutant.mutatorName}  ${original.trim()}  ->  ${replacement.trim()}`);
  }
}

console.log(count === 0 ? 'No surviving mutants.' : `${count} surviving mutant(s).`);
