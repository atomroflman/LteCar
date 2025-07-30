# LteCar – Dokumentation

## Zweck

LteCar ist ein System zum Bau und Betrieb von ferngesteuerten Autos über LTE/Internet. Es ermöglicht:
- **Quasi unbegrenzte Anzahl von Steuerkanälen** (z.B. Motor, Lenkung, Licht, Sensoren)
- **Echtzeit-Videoübertragung** vom Fahrzeug zur Weboberfläche
- **Reaktionsschnelle Steuerung** über das Internet
- **Mehrere Autos pro Server** – Verwaltung und Steuerung verschiedener Fahrzeuge gleichzeitig
- **Webseite** zur Steuerung, Videoanzeige und Konfiguration

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

---

## Features

- **SignalR** für Echtzeit-Kommunikation (Steuerung, Telemetrie)
- **Janus Gateway** für WebRTC Video-Streaming
- **Flexible Channel-Konfiguration**: beliebige Funktionen und Sensoren
- **Mehrbenutzerfähig**: mehrere Nutzer und Fahrzeuge pro Server
- **Weboberfläche**: Steuerung, Video, Setup, Gamepad-Unterstützung

---

## Weitere Infos

- Quellcode und Beispiele: siehe die jeweiligen Unterordner (`Server`, `Onboard`, `Client`)
- API-Dokumentation: `/api/*` Endpunkte am Server
- Anpassung der Kanäle: `channelMap.json` und Weboberfläche

---

## Kontakt & Support

Fragen, Feedback oder Beiträge bitte direkt im GitHub-Repository stellen.
