# CI/CD Setup – Projekt Photon

## Dateien im Überblick

| Datei | Zweck |
|---|---|
| `.github/workflows/ci.yml` | CI bei jedem PR gegen `dev`/`main`: Style, Build, Tests, Coverage, Security |
| `.github/workflows/release.yml` | Manuelles Release (EXE/ZIP für Windows + Linux) als GitHub Release |
| `.github/dependabot.yml` | Wöchentliche Update-PRs für NuGet, .NET SDK und Actions (gegen `dev`) |
| `Directory.Build.props` | Gemeinsame Build-Regeln: Nullable, Warnings = Errors, XML-Docs, NuGet-Audit, Lockfiles |
| `global.json` | Fixiert das .NET-10-SDK für alle (lokal und CI) |
| `.config/dotnet-tools.json` | Lokale Tools: CSharpier und ReportGenerator in fester Version |
| `scripts/check-headers.sh` | Prüft den Pflicht-Datei-Header (Author / AI Assistance) |
| `scripts/check-architecture.sh` | Prüft, dass `Photon.Core` keine UI-/OpenGL-Abhängigkeiten hat |
| `.gitattributes`, `.csharpierrc.json` | Aus dem Style Guide übernommen |

## Erwartete Projektstruktur

Die Workflows gehen von dieser Struktur aus (Solution-Datei im Root):

```
Photon.slnx
src/Photon.Core/Photon.Core.csproj      (Rendering, keine UI-Abhängigkeiten)
src/Photon.App/Photon.App.csproj        (Avalonia + OpenTK)
tests/Photon.Core.Tests/...Tests.csproj (Testprojekte enden auf .Tests)
```

Weicht der Pfad zur App ab, `APP_PROJECT` in `release.yml` anpassen.

## Erstmalige Einrichtung (lokal)

```bash
dotnet tool restore                  # CSharpier + ReportGenerator installieren
dotnet restore                       # erzeugt packages.lock.json -> mit committen!
dotnet csharpier format .            # vor jedem Commit formatieren
```

Testprojekte brauchen das Paket `coverlet.collector` (ist bei `dotnet new xunit` dabei).

## Branch Protection einrichten (Rulesets)

Settings → Rules → Rulesets → New branch ruleset

**Ruleset „dev“** (Target: `dev`)
- Restrict deletions, Block force pushes
- Require a pull request before merging
  - Required approvals: **1**
  - Dismiss stale approvals when new commits are pushed
  - Allowed merge methods: **Squash**
- Require status checks to pass → Check **„CI passed“** hinzufügen
  - Require branches to be up to date before merging

**Ruleset „main“** (Target: `main`)
- wie oben, aber Allowed merge methods: **Merge**

Der Check „CI passed“ erscheint erst in der Auswahl, nachdem die CI einmal gelaufen ist (z. B. per ersten PR oder manuell über „Run workflow“).

## Release erstellen

Actions → **Release** → Run workflow → Branch `main`, Version eingeben (z. B. `0.1.0`).
Für eine zusätzliche Freigabe: Settings → Environments → `release` anlegen und Required reviewers eintragen.

## Coverage-Grenze

In `ci.yml` steht `COVERAGE_MIN: 0` (nur Bericht). Sobald genug Tests existieren, z. B. auf `60` setzen, dann schlägt die CI darunter fehl. Der Bericht erscheint in der Zusammenfassung jedes CI-Laufs und als Artefakt.
