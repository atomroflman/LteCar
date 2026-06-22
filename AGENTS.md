# Agent Instructions for LteCar

## Nach jeder abgeschlossenen Aufgabe: Test-System aktualisieren

Server (`lte-rc-server`) und lte-truck (Onboard-Pi) sind das **Test-System**.
Nach jeder Code-Änderung, die einen Task abschliesst, müssen beide Hosts via
Skill `ltecar-update` auf den aktuellen Stand des Repos gebracht werden.

Ausnahmen (kein Auto-Update nötig):

- Reine Lese-/Recherche-Tasks (Dateien lesen, `grep`, `git log`, ...)
- Reine Doku-Änderungen ohne deploybare Auswirkung
- Wenn der User explizit "nicht deployen" sagt

Bei destruktiven Aktionen (z. B. `docker compose down`, Factory-Reset,
Reboot) gilt weiterhin: **vorher fragen**.

## Build & Lint als Vor-Check

Vor dem Rollout kurz prüfen, dass der Code überhaupt baut:

- Client: `cd Client && npm run lint` und `Client/node_modules/.bin/tsc --noEmit -p Client/tsconfig.json`
- Server: `dotnet build Server/LteCar.Server.csproj -c Release`
- Onboard: `dotnet build Onboard/LteCar.Onboard.csproj -c Release`

Wenn einer der Schritte fehlschlägt: **nicht ausrollen**, User informieren.

## SSH-Aliase

`~/.ssh/config` muss `lte-truck` und `lte-rc-server` auflösen. Details im
Skill `ltecar-ssh`.
