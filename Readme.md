# LteCar – Dokumentation

## Zweck

LteCar ist ein System zum Bau und Betrieb von ferngesteuerten Autos über LTE/Internet. Es ermöglicht:
- **Quasi unbegrenzte Anzahl von Steuerkanälen** (z.B. Motor, Lenkung, Licht, Sensoren)
- **Echtzeit-Videoübertragung** vom Fahrzeug zur Weboberfläche
- **Reaktionsschnelle Steuerung** über das Internet
- **Mehrere Autos pro Server** – Verwaltung und Steuerung verschiedener Fahrzeuge gleichzeitig
- **Bidirektionaler Audio-Chat** zwischen Fahrer und Fahrzeug
- **Webseite** zur Steuerung, Videoanzeige, Audio-Chat und Konfiguration

---

## Installation

### Server

1. Voraussetzungen: Linux, Docker oder .NET 8, Node.js, Janus Gateway
2. Repository klonen und Basisinstallation:
```bash
git clone https://github.com/atomroflman/LteCar.git
cd LteCar
bash install-server.sh
```
3. Janus Gateway installieren (siehe `Server/bash/install-janus.sh` für Details).
4. Server starten:
```bash
bash start-server.sh
```
    oder als Systemdienst (`Server/install.sh`).

### Onboard (Fahrzeug)

1. Raspberry Pi vorbereiten.
2.
```bash
git clone https://github.com/atomroflman/LteCar.git
cd LteCar
sudo ./pi-install-car.sh
```
3. Konfiguration anpassen (siehe unten).
4. Onboard-Software starten:
```bash
cd Onboard
dotnet run
```

---

## Konfiguration Onboard

- **carId.txt**: Eindeutige Fahrzeug-ID (wird beim ersten Start erzeugt).
- **channelMap.json**: Definition aller Steuerkanäle (z.B. Motor, Lenkung, Sensoren).
- **appSettings.json**: Netzwerk- und Servereinstellungen.
- **VideoSettings**: Videoauflösung, Bitrate etc. (im Server und Onboard konfigurierbar).

### Konfigurationsoptionen (appSettings.json)

| Option | Standard | Beschreibung |
|--------|----------|---------------|
| `ServerName` | localhost | Hostname des Servers |
| `ServerPort` | 5000 | Server-Port |
| `UseHttps` | true | HTTPS verwenden |
| `VideoPort` | 10001 | Video-Stream Port |
| `AudioPort` | 11001 | Audio-Stream Port |
| `AutoConfigureMediaMtx` | true | MediaMTX automatisch konfigurieren |
| `LTE_USE_NEW_CONNECTION_MODEL` | true | Neues Kommunikationsmodell aktivieren |

---

## Features

### Kommunikationsmodell

Das Fahrzeug verwendet nun eine **zentrale Verbindung** über den `VehicleConnectionManager`:

```
┌─────────────────────────────────────────────────────────────────┐
│                    VehicleConnectionManager                      │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │              SignalR Connection (Single)                 │   │
│  └─────────────────────────────────────────────────────────┘   │
│                              │                                  │
│         ┌────────────────────┼────────────────────┐           │
│         │                    │                    │           │
│    ┌────▼────┐        ┌─────▼─────┐       ┌──────▼──────┐    │
│    │Control   │        │ Telemetry │       │    Video    │    │
│    │Service   │        │  Service  │       │   Service   │    │
│    └─────────┘        └───────────┘       └─────────────┘    │
│                                                               │
│    ┌─────────────────────────────────────────────────────────┐│
│    │              Auto-Discovery System                      ││
│    │  Alle IVehicleService-Implementierungen werden          ││
│    │  automatisch erkannt und initialisiert                  ││
│    └─────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
```

**Vorteile:**
- Nur **eine** persistente Verbindung zum Server
- Automatisches Reconnection-Handling
- Services kümmern sich nicht mehr um Connection-Handling
- Einfache Erweiterung durch `IVehicleService`-Interface

### Neuen Service erstellen

```csharp
public class MeinNeuerService : VehicleServiceBase
{
    public override string ServiceName => "MeinNeuer";
    
    public override Task OnConnectedAsync(HubConnection connection)
    {
        // Wird aufgerufen wenn die Verbindung hergestellt ist
        return Task.CompletedTask;
    }
}
```

Der Service wird automatisch via Dependency Injection erkannt.

### Video-Streaming

- **Janus Gateway** für WebRTC Video-Streaming
- **MediaMTX** für flexible Stream-Konfiguration
- Dynamische Endpoint-Konfiguration basierend auf Serverdaten

### Audio-Chat

Bidirektionaler Audio-Chat zwischen Fahrer und Fahrzeug:

- **Mikrofon-Auswahl**: USB oder Jack-Eingang
- **Lautsprecher-Auswahl**: Audio-Output-Gerät
- **Aufnahme-Steuerung**: Start/Stop über Control Center
- **EchoCancellation** und **NoiseSuppression** standardmäßig aktiviert

### Flexible Channel-Konfiguration

Beliebige Funktionen und Sensoren über `channelMap.json`:

```json
{
  "ControlChannels": {
    "steering": { "Type": "ServoControl", "ServerId": 1 },
    "throttle": { "Type": "ThrottleControl", "ServerId": 2 }
  },
  "TelemetryChannels": {
    "battery": {
      "TelemetryType": "LteCar.Onboard.Telemetry.JbdBmsTelemetryReader",
      "ReadIntervalTicks": 50
    }
  },
  "VideoStreams": {
    "front": { "StreamId": "rpi0", "Enabled": true }
  }
}
```

### Mehrbenutzerfähig

- Mehrere Nutzer pro Server
- Mehrere Fahrzeuge pro Server
- SSH-basierte Authentifizierung für Fahrzeugsteuerung

---

## Architektur

```
┌─────────────────────────────────────────────────────────────────┐
│                     Client (Browser)                             │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────────┐  │
│  │  Video   │  │  Audio   │  │  Telemetry│  │    Flow      │  │
│  │  Stream  │  │   Chat   │  │  Display  │  │   Editor     │  │
│  └────┬─────┘  └────┬─────┘  └─────┬─────┘  └──────┬───────┘  │
└───────┼─────────────┼─────────────┼────────────────┼──────────┘
        │             │             │                │
        │ WebRTC      │ SignalR     │ SignalR       │ SignalR
        │             │             │                │
┌───────▼─────────────▼─────────────▼────────────────▼──────────┐
│                     Server (ASP.NET Core)                       │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────────┐   │
│  │ Janus    │  │ Telemetry│  │  Audio   │  │    Car       │   │
│  │ Gateway  │  │   Hub    │  │   Hub    │  │  Control     │   │
│  └──────────┘  └──────────┘  └──────────┘  └──────┬───────┘   │
└───────────────────────────────────────────────────┼───────────┘
                                                    │
                                          SignalR   │
┌───────────────────────────────────────────────────▼───────────┐
│                     Onboard (Raspberry Pi)                    │
│  ┌────────────────┐  ┌────────────────┐  ┌───────────────┐ │
│  │VehicleConnection│  │    Video       │  │    Audio      │ │
│  │    Manager      │  │   Service      │  │     Chat      │ │
│  └────────────────┘  └────────────────┘  └───────────────┘ │
│  ┌────────────────┐  ┌────────────────┐  ┌───────────────┐ │
│  │    Telemetry    │  │    Control     │  │    Media      │ │
│  │    Service     │  │    Service     │  │     MTX       │ │
│  └────────────────┘  └────────────────┘  └───────────────┘ │
└──────────────────────────────────────────────────────────────┘
```

---

## Weitere Infos

- Quellcode und Beispiele: sieh die jeweiligen Unterordner (`Server`, `Onboard`, `Client`)
- API-Dokumentation: `/api/*` Endpunkte am Server
- Anpassung der Kanäle: `channelMap.json` und Weboberfläche

---

## Environment-Variablen

| Variable | Standard | Beschreibung |
|----------|----------|---------------|
| `LTE_USE_NEW_CONNECTION_MODEL` | true | Verwendet das neue zentrale Kommunikationsmodell |

---

## Kontakt & Support

Fragen, Feedback oder Beiträge bitte direkt im GitHub-Repository stellen.
