# Workflow-Guide – Projekt Photon

Dieser Guide beschreibt, wie Änderungen von der Idee bis auf `main` gelangen. Er ergänzt den Style Guide im Wiki: Der Style Guide sagt, **wie** Code aussehen soll, dieser Guide sagt, **wie** er ins Repo kommt.

---

## Auf einen Blick

```
feature/<beschreibung>  ──PR──▶  dev  ──PR (Sprint-Ende)──▶  main  ──manuell──▶  Release
        │                         ▲
        └── CI grün + 1 Approval ─┘  (Squash and Merge)
```

| Branch | Zweck | Wer schreibt hinein? |
|---|---|---|
| `main` | Stabile Release-Stände | Nur per PR von `dev`, gemeinsam zum Sprint-Ende |
| `dev` | Integrations-Branch der laufenden Entwicklung | Nur per PR von Feature-Branches |
| `feature/<beschreibung>` | Eine Aufgabe oder User Story | Du |

Direktes Pushen auf `main` und `dev` ist gesperrt. Ein PR kann erst gemergt werden, wenn **die CI grün ist** und **ein weiteres Teammitglied zugestimmt hat**.

---

## Einmalige Einrichtung

1. **.NET 10 SDK** installieren (nicht nur die Runtime): `winget install Microsoft.DotNet.SDK.10`
2. **Repo lokal klonen**, nicht in einen Cloud-Sync-Ordner (Google Drive, OneDrive, Dropbox). Die Sync-Programme sperren Dateien während des Builds.
   ```powershell
   cd C:\dev
   git clone https://github.com/<owner>/<repo>.git Photon
   cd Photon
   dotnet tool restore
   ```
3. **CSharpier in der IDE einrichten** und „Format on Save“ aktivieren:
   - Visual Studio: Erweiterung *CSharpier* → Tools → Options → CSharpier → *Reformat with CSharpier on Save*
   - Rider: Plugin *CSharpier* → Settings → Tools → CSharpier → *Run on Save*
   - VS Code: Erweiterung *CSharpier - Code formatter*, dazu `"editor.formatOnSave": true`
4. **Git-Identität** prüfen, damit Commits dir zugeordnet werden:
   ```powershell
   git config --global user.name "Vorname Nachname"
   git config --global user.email "deine-github-mail@example.com"
   ```

---

## Der Ablauf für jede Aufgabe

### 1. Aufgabe wählen

Jede Änderung gehört zu einem **Issue** (oder einer Karte im Board). Weise dir das Issue zu, damit niemand doppelt arbeitet.

### 2. Feature-Branch von aktuellem `dev` anlegen

```powershell
git switch dev
git pull
git switch -c feature/obj-parser
```

**Regeln für Branch-Namen:**
- immer `feature/` + kurze englische Beschreibung
- Kleinbuchstaben, Wörter mit Bindestrich: `feature/phong-shading`, `feature/camera-controls`
- nicht: `feature/Max_Test`, `feature/fix`, `mein-branch`

### 3. Entwickeln und committen

Committe in **kleinen, sinnvollen Schritten**. Die Zwischen-Commits werden beim Mergen zusammengefasst, du kannst also ruhig oft committen.

```powershell
git add .
git commit -m "add vertex parsing to obj loader"
```

**Regeln für Commit-Nachrichten:**
- Englisch, Präsens, Imperativ („add“, nicht „added“ oder „adds“)
- beschreibt, **was** sich ändert und, wenn nicht offensichtlich, **warum**
- kurz, ideal unter 72 Zeichen

| Gut | Schlecht |
|---|---|
| `add ray triangle intersection` | `stuff` |
| `fix normal vector normalization` | `fixed bug` |
| `move shading logic to phong shader` | `WIP` |
| `increase shadow ray offset to avoid acne` | `Änderungen an Shader.cs` |

**Neue `.cs`-Dateien** brauchen den Pflicht-Header:
```csharp
// -----------------------------------------------------------------------------
// Author:        <Name / GitHub-Handle>
// AI Assistance: <None / GitHub Copilot for boilerplate / ...>
// -----------------------------------------------------------------------------
```

### 4. Vor dem Push lokal prüfen

So findest du Fehler in Sekunden statt erst nach Minuten in der CI:

```powershell
dotnet csharpier format .
dotnet build -c Release
dotnet test -c Release
```

Alle drei müssen ohne Fehler durchlaufen. Warnungen gelten als Fehler.

### 5. Pushen und Pull Request öffnen

```powershell
git push -u origin feature/obj-parser
```

Danach auf GitHub **Compare & pull request** klicken.

- **Base-Branch: `dev`** (nicht `main`!)
- **Titel:** Englisch, Imperativ, wie eine Commit-Nachricht, z. B. `add obj file parser`. Wichtig: Der PR-Titel wird beim Squash zur Commit-Nachricht auf `dev`.
- **Beschreibung:** Deutsch. Die Vorlage wird automatisch eingefügt; fülle sie aus.
- **Reviewer** zuweisen.
- Noch nicht fertig? Als **Draft** öffnen. Die CI läuft trotzdem, aber niemand wird zum Review aufgefordert.

### 6. CI abwarten

Die CI startet automatisch und prüft Formatierung, Datei-Header, Architektur, Build, Tests, Coverage und Paket-Sicherheit. Ergebnis siehst du unten im PR.

Ist ein Check rot: auf **Details** klicken, Fehler lokal beheben, committen, pushen. Die CI läuft automatisch erneut. Häufige Fehler stehen weiter unten.

### 7. Review

Mindestens ein Teammitglied prüft den PR (Vier-Augen-Prinzip). Siehe Abschnitt *Reviews*.

Auf Kommentare reagierst du mit neuen Commits im selben Branch, nicht mit einem neuen PR. Neue Commits setzen ein bestehendes Approval zurück; dann muss der Reviewer kurz erneut zustimmen.

### 8. Mergen

Sobald CI grün und Approval vorhanden:

1. Falls GitHub „This branch is out-of-date“ meldet: **Update branch** klicken und die CI erneut abwarten.
2. **Squash and merge** klicken.
3. Commit-Nachricht prüfen (sollte dem PR-Titel entsprechen) und bestätigen.
4. **Delete branch** klicken.

Gemergt wird in der Regel von der Person, die den PR erstellt hat.

### 9. Aufräumen

```powershell
git switch dev
git pull
git branch -d feature/obj-parser
```

Weiter mit der nächsten Aufgabe ab Schritt 1.

---

## Reviews

**Als Reviewer** prüfst du, was die CI nicht prüfen kann:

- Ist die Lösung verständlich und an der richtigen Stelle (2-Schichten-Architektur)?
- Sind die Namen sprechend und die öffentlichen APIs dokumentiert?
- Werden Fließkommazahlen mit Epsilon verglichen statt mit `==`?
- Starten Sekundärstrahlen mit Epsilon-Versatz (Ray Acne)?
- Gibt es Tests für die neue Logik?
- Ist der Datei-Header inhaltlich korrekt (Autor, KI-Einsatz)?

**So reagierst du:**
- **Approve:** passt, kann gemergt werden. Kleine Anmerkungen dürfen trotzdem als Kommentar dabei sein.
- **Request changes:** muss vor dem Merge geändert werden.
- **Comment:** Frage oder Vorschlag, nicht blockierend.

Kommentare auf Deutsch, freundlich und konkret. Statt „Das ist falsch“ lieber „Hier fehlt der Epsilon-Vergleich, siehe Style Guide“. Bei größeren Änderungen hilft ein kurzer gemeinsamer Walkthrough mehr als lange Kommentar-Threads.

Reviews sollten zeitnah erfolgen, ideal innerhalb eines Werktags.

---

## Feature-Branch aktuell halten

Wurde `dev` weiterentwickelt, während du arbeitest, holst du die Änderungen so in deinen Branch:

```powershell
git switch dev
git pull
git switch feature/obj-parser
git merge dev
```

Gibt es Konflikte: Dateien im Editor öffnen, Konflikte auflösen, dann
```powershell
git add .
git commit
```

Bitte **kein `git rebase`** auf bereits gepushten Branches und **kein `git push --force`**, wenn andere am selben Branch arbeiten.

---

## Häufige CI-Fehler und Lösungen

| Fehlermeldung (Auszug) | Ursache | Lösung |
|---|---|---|
| `Was not formatted` | CSharpier-Formatierung fehlt | `dotnet csharpier format .`, committen, pushen |
| `Missing or incomplete file header` | Header fehlt oder enthält Platzhalter `[...]` | Header am Dateianfang ergänzen |
| `IDE1006: Naming rule violation` | Namenskonvention verletzt, z. B. privates Feld ohne `_` | Umbenennen gemäß Style Guide |
| `IDE0161: Convert to file-scoped namespace` | `namespace X { ... }` statt `namespace X;` | Auf File-scoped Namespace umstellen |
| `CS1591: Missing XML comment` | Öffentliches Element ohne XML-Kommentar | `/// <summary>` ergänzen |
| `SA1600: Elements should be documented` | Element ohne XML-Kommentar, auch private Felder und Methoden | `/// <summary>` ergänzen, bei Overrides `/// <inheritdoc/>` |
| `CS8602: Dereference of a possibly null reference` | Möglicher `null`-Zugriff | Null prüfen oder Typ korrekt als `Type?` markieren |
| `must not depend on UI/graphics libraries or higher layers` | Avalonia, OpenTK oder SkiaSharp in `Photon.Core`/`Photon.Application` verwendet, oder `Photon.Core` greift auf eine höhere Schicht zu | Code in die passende Schicht verschieben (siehe Architecture Notebook) |
| `Vulnerable NuGet packages found` | Paket mit bekannter Sicherheitslücke | Paket aktualisieren (Dependabot-PR prüfen) |
| `NU1004` / Lock file out of date | Paket geändert, `packages.lock.json` nicht committet | `dotnet restore` lokal, Lock-Dateien mitcommitten |

---

## Pakete hinzufügen oder aktualisieren

```powershell
dotnet add src/Photon.App package <Paketname>
dotnet restore
```

Die geänderten `packages.lock.json`-Dateien **immer mitcommitten**. Neue Pakete in `Photon.Core` vorher im Team abstimmen; UI- und Grafik-Bibliotheken sind dort verboten.

**Dependabot-PRs** (automatische Paket-Updates) werden wie normale PRs behandelt: CI abwarten, kurz die Release Notes ansehen, approven, mergen.

---

## Sprint-Ende: Release

Das Release führt das ganze Team gemeinsam durch.

1. Alle PRs des Sprints sind auf `dev` gemergt, `dev` ist grün.
2. PR von `dev` nach `main` öffnen, Titel z. B. `release sprint 3`.
3. CI abwarten, gemeinsam reviewen.
4. **Create a merge commit** wählen (nicht Squash!).
5. Release erstellen: **Actions → Release → Run workflow**, Branch `main`, Versionsnummer eingeben (z. B. `0.3.0`).
6. Das fertige Release mit EXE erscheint unter **Releases**.

---

## Do's und Don'ts

**Do**
- pro Aufgabe ein eigener Branch und ein eigener PR
- kleine PRs (lieber drei kleine als einen riesigen)
- vor dem Push lokal formatieren, bauen und testen
- PR-Titel sorgfältig wählen, er landet in der Historie
- nach dem Merge den Branch löschen

**Don't**
- direkt auf `dev` oder `main` arbeiten
- PRs gegen `main` öffnen (außer beim Sprint-Release von `dev`)
- `git push --force` auf geteilte Branches
- fremde PRs ungelesen approven
- Build-Ausgaben (`bin/`, `obj/`) oder persönliche IDE-Dateien committen

---

## Schnellreferenz

```powershell
# Neue Aufgabe beginnen
git switch dev; git pull; git switch -c feature/<beschreibung>

# Vor dem Push
dotnet csharpier format .; dotnet build -c Release; dotnet test -c Release

# Committen und pushen
git add .; git commit -m "<what changed>"; git push -u origin feature/<beschreibung>

# Nach dem Merge aufräumen
git switch dev; git pull; git branch -d feature/<beschreibung>
```
