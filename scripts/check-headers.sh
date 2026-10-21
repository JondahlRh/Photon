#!/usr/bin/env bash
# -----------------------------------------------------------------------------
# Verifies that every C# source file starts with the mandatory header:
#
# // -----------------------------------------------------------------------------
# // Author:        <name>
# // AI Assistance: <description>
# // -----------------------------------------------------------------------------
# -----------------------------------------------------------------------------
set -euo pipefail

separator='// -----------------------------------------------------------------------------'
failed=0
checked=0

while IFS= read -r -d '' file; do
  checked=$((checked + 1))
  mapfile -t lines < <(head -n 4 "$file" | sed 's/^\xEF\xBB\xBF//; s/\r$//')

  if [[ "${lines[0]:-}" != "$separator" ]] \
    || ! [[ "${lines[1]:-}" =~ ^//\ Author:\ +[^[:space:]] ]] \
    || ! [[ "${lines[2]:-}" =~ ^//\ AI\ Assistance:\ +[^[:space:]] ]] \
    || [[ "${lines[3]:-}" != "$separator" ]] \
    || [[ "${lines[1]}" == *"["*"]"* ]] \
    || [[ "${lines[2]}" == *"["*"]"* ]]; then
    echo "::error file=${file},line=1::Missing or incomplete file header (Author / AI Assistance)."
    failed=$((failed + 1))
  fi
done < <(find . -name '*.cs' \
  -not -path '*/bin/*' -not -path '*/obj/*' \
  -not -name '*.g.cs' -not -name '*.Designer.cs' \
  -print0)

echo "Checked ${checked} file(s), ${failed} without valid header."
[[ $failed -eq 0 ]]
