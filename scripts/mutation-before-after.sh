#!/usr/bin/env bash
# Runs Stryker.NET on the baseline (default: main) and on the current checkout and writes
# both mutation scores to docs/mutation/scores.md, ready to paste into ASSIGNMENT.md.
# Needs: dotnet tool install -g dotnet-stryker --version 4.13.0
# Usage: bash scripts/mutation-before-after.sh [baseline-ref]
set -euo pipefail

ROOT="$(git rev-parse --show-toplevel)"
BASELINE_REF="${1:-main}"
OUT="$ROOT/docs/mutation"
WORKTREE="$(mktemp -d)"
mkdir -p "$OUT"

cleanup() { git -C "$ROOT" worktree remove --force "$WORKTREE" >/dev/null 2>&1 || true; }
trap cleanup EXIT

run_stryker() {
  local checkout="$1" label="$2"
  echo "== Stryker on $label ($checkout)"
  # The baseline has no config; use the same one so both runs mutate the same files.
  if [ "$checkout" != "$ROOT" ]; then
    cp "$ROOT/backend/stryker-config.json" "$checkout/backend/stryker-config.json"
  fi
  (cd "$checkout/backend" && dotnet stryker -f stryker-config.json --output "$OUT/$label") | tee "$OUT/$label.log"
}

score_of() {
  node -e '
    const report = require(process.argv[1]);
    let detected = 0, undetected = 0;
    const counts = {};
    const tested = [];
    for (const [path, file] of Object.entries(report.files)) {
      let fileHasMutants = false;
      for (const mutant of file.mutants) {
        counts[mutant.status] = (counts[mutant.status] || 0) + 1;
        if (["Killed", "Timeout"].includes(mutant.status)) { detected++; fileHasMutants = true; }
        if (["Survived", "NoCoverage"].includes(mutant.status)) { undetected++; fileHasMutants = true; }
      }
      if (fileHasMutants) tested.push(path.split(/[\\/]/).pop());
    }
    const score = detected + undetected ? (100 * detected / (detected + undetected)).toFixed(2) : "n/a";
    const statuses = Object.entries(counts).map(([status, count]) => `${status} ${count}`).join(", ");
    console.log(`${score}% | ${statuses} | ${tested.join(", ")}`);
  ' "$(ls "$1"/reports/mutation-report.json)"
}

git -C "$ROOT" worktree add --detach "$WORKTREE" "$BASELINE_REF" >/dev/null
run_stryker "$WORKTREE" before
run_stryker "$ROOT" after

{
  echo "# Mutation scores (Stryker.NET 4.13.0)"
  echo
  echo "Mutated files: ApprovalService.cs, ApprovalLimits.cs. Score = (killed + timeout) / (killed + timeout + survived + no coverage)."
  echo
  echo "| Run | Commit | Score | Mutant statuses | Files with tested mutants |"
  echo "| --- | --- | --- | --- | --- |"
  echo "| before | $(git -C "$ROOT" rev-parse --short "$BASELINE_REF") ($BASELINE_REF) | $(score_of "$OUT/before") |"
  echo "| after | $(git -C "$ROOT" rev-parse --short HEAD) (HEAD) | $(score_of "$OUT/after") |"
  echo
  echo "Surviving mutants after the change:"
  echo
  echo '```'
  node "$ROOT/scripts/mutation-survivors.mjs" "$OUT/after/reports/mutation-report.json"
  echo '```'
} > "$OUT/scores.md"

cat "$OUT/scores.md"
