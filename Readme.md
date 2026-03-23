# LteCar – Remote Control über LTE/Internet

## Quick Start

```bash
# Server
cd Server && dotnet run

# Onboard (Fahrzeug)
cd Onboard && dotnet run -- setup    # Erstes Setup
cd Onboard && dotnet run            # Normaler Start
```

**Dokumentation:** Siehe [Docs/README.md](Docs/README.md) für vollständige Dokumentation.

---

## Wichtige Hinweise

> **LTE-Konnektivität**: Das Onboard-Fahrzeug initiiert eine **ausgehende Verbindung** zum Server. Das Fahrzeug ist **nicht direkt aus dem Internet erreichbar** – alle Kommunikation wird vom Fahrzeug initiiert.

> **Datenbank**: Niemals die Datenbank manuell ändern. Immer EF Core Migrations verwenden.

---

## Features

| Feature | Beschreibung |
|---------|--------------|
| Remote Control | Steuerung über LTE/Internet mit niedriger Latenz |
| Video-Streaming | Echtzeit-Video von Kamera |
| Audio-Chat | Bidirektionale Audiokommunikation |
| Bash Tool | Remote Bash-Befehle auf Fahrzeug ausführen |
| Channel Tester | Hardware-Kanäle testen |
| Templates | Fahrzeugkonfigurationen teilen |

**Feature Flags**: Alle optionalen Features sind **standardmäßig deaktiviert** (`webSetup`, `bashTool`, `channelTester`, `audio`, `video`). Aktivierung via Setup-Tool oder `appSettings.json`.

---

## Installation

### Server

```bash
git clone https://github.com/atomroflman/LteCar.git
cd LteCar/Server
dotnet run
```

### Onboard (Raspberry Pi)

```bash
git clone https://github.com/atomroflman/LteCar.git
cd LteCar/Onboard
dotnet run -- setup   # Interaktives Setup
dotnet run            # Start
```

**Details:** [Docs/INSTALLATION.md](Docs/INSTALLATION.md)

---

## Setup-Tool (raspi-config Style)

```bash
cd Onboard && dotnet run -- setup
```

Menüstruktur:
1. **System Options** – Hostname, SSH, Boot
2. **Network / Server** – Server-URL konfigurieren
3. **Vehicle Configuration** – Kanäle, Name
4. **Hardware Test** – Outputs, Servos, Motoren testen
5. **Templates** – Fahrzeugvorlagen verwalten
6. **Feature Flags** – Features ein/aus
7. **Update / Recovery** – Updates, Backup, Factory Reset

**Details:** [Docs/SETUP.md](Docs/SETUP.md)

---

## Konfiguration

### Onboard (appSettings.json)

```json
{
  "carId": "vehicle-001",
  "carName": "My RC Car",
  "serverUrl": "https://server.example.com:5000",
  "bashTool": false,
  "audio": false,
  "video": true
}
```

### Feature Flags

| Flag | Standard | Beschreibung |
|------|---------|--------------|
| `webSetup` | false | Web-Setup Interface |
| `bashTool` | false | Remote Bash-Tool |
| `channelTester` | false | Kanal-Tester |
| `audio` | false | Audio-Chat |
| `video` | false | Video-Streaming |

**Details:** [Docs/CONFIGURATION.md](Docs/CONFIGURATION.md)

---

## Architektur

```
┌──────────────┐     WebRTC      ┌──────────────┐
│   Browser    │◄──────────────►│    Server    │
│   (Client)   │    SignalR     │  (ASP.NET)   │
└──────────────┘                └──────┬───────┘
                                       │
                              SignalR  │  WebRTC
                                       │
┌──────────────────────────────────────▼───────────────┐
│                    Onboard (Raspberry Pi)             │
│  ┌────────────┐  ┌────────────┐  ┌─────────────┐   │
│  │  Vehicle   │  │   Video    │  │    Audio    │   │
│  │ Connection │  │  Service   │  │    Chat     │   │
│  │  Manager   │  │            │  │             │   │
│  └────────────┘  └────────────┘  └─────────────┘   │
│  ┌────────────┐  ┌────────────┐  ┌─────────────┐   │
│  │  Telemetry │  │  Control   │  │  BashTool   │   │
│  │  Service   │  │  Service   │  │  Service    │   │
│  └────────────┘  └────────────┘  └─────────────┘   │
└────────────────────────────────────────────────────┘
```

---

## SignalR Hubs

| Hub | Pfad | Zweck |
|-----|------|-------|
| CarConnectionHub | `/hubs/connection` | Fahrzeug-Verbindung |
| CarControlHub | `/hubs/control` | Fernsteuerung |
| TelemetryHub | `/hubs/telemetry` | Telemetrie |
| CarUiHub | `/hubs/carui` | UI-Updates |
| CarVideoHub | `/hubs/video` | Video-Streaming |
| CarBashHub | `/hubs/carbash` | Bash-Proxy |

---

## Dokumentation

- [Docs/README.md](Docs/README.md) – Übersicht
- [Docs/INSTALLATION.md](Docs/INSTALLATION.md) – Installationsanleitung
- [Docs/SETUP.md](Docs/SETUP.md) – Setup-Tool
- [Docs/FEATURES.md](Docs/FEATURES.md) – Feature-Dokumentation
- [Docs/CONFIGURATION.md](Docs/CONFIGURATION.md) – Konfigurationsreferenz

---

## Environment-Variablen

| Variable | Beschreibung |
|----------|--------------|
| `CONFIG_DIR` | Konfigurationsverzeichnis (Onboard) |
| `VEHICLE_TEMPLATES_PATH` | Template-Pfad |
| `LTE_USE_NEW_CONNECTION_MODEL` | Neues Verbindungsmodell (default: true) |

---

## Kontakt & Support

Fragen, Feedback oder Beiträge bitte direkt im GitHub-Repository stellen.
