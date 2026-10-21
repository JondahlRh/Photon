#!/usr/bin/env bash
# -----------------------------------------------------------------------------
# Enforces the 2-layer architecture:
# Photon.Core must not reference any UI or graphics framework.
# -----------------------------------------------------------------------------
set -euo pipefail

core_project="src/Photon.Core/Photon.Core.csproj"
forbidden='Avalonia|OpenTK|SkiaSharp|System\.Windows|Microsoft\.WindowsDesktop|Photon\.App'

if [[ ! -f "$core_project" ]]; then
  echo "::warning::${core_project} not found yet - skipping architecture check."
  exit 0
fi

if grep -nE "<(PackageReference|ProjectReference|FrameworkReference)[^>]*(${forbidden})" "$core_project"; then
  echo "::error file=${core_project}::Photon.Core must not depend on UI/graphics libraries."
  exit 1
fi

if grep -rnE --include='*.cs' "^\s*using\s+(${forbidden})" src/Photon.Core; then
  echo "::error::Photon.Core source files use UI/graphics namespaces."
  exit 1
fi

echo "Architecture check passed: Photon.Core is UI-independent."
