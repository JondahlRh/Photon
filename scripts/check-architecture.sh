#!/usr/bin/env bash
# -----------------------------------------------------------------------------
# Enforces the layer rules from the architecture notebook:
# Photon.Core and Photon.Application must not reference any UI or graphics
# framework, and Photon.Core must not reference the projects above it.
# -----------------------------------------------------------------------------
set -euo pipefail

ui='Avalonia|OpenTK|SkiaSharp|System\.Windows|Microsoft\.WindowsDesktop'
# The trailing non-letter keeps "Photon.App" from matching "Photon.Application"
app='Photon\.App([^[:alpha:]]|$)'
application='Photon\.Application([^[:alpha:]]|$)'

failed=0

# Checks project references and using directives of one project.
# $1: project directory, $2: forbidden pattern
check_project() {
  local dir="$1"
  local forbidden="$2"
  local name
  name="$(basename "$dir")"
  local project="${dir}/${name}.csproj"

  if [[ ! -f "$project" ]]; then
    echo "::warning::${project} not found yet - skipping architecture check."
    return
  fi

  if grep -nE "<(PackageReference|ProjectReference|FrameworkReference)[^>]*(${forbidden})" "$project"; then
    echo "::error file=${project}::${name} must not depend on UI/graphics libraries or higher layers."
    failed=1
  fi

  if grep -rnE --include='*.cs' "^\s*using\s+(${forbidden})" "$dir"; then
    echo "::error::${name} source files use UI/graphics or higher-layer namespaces."
    failed=1
  fi
}

check_project src/Photon.Core "${ui}|${app}|${application}"
check_project src/Photon.Application "${ui}|${app}"

[[ $failed -eq 0 ]] || exit 1
echo "Architecture check passed: Photon.Core and Photon.Application are UI-independent."
