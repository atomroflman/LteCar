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

Es gibt ein einziges Installationsskript für Server und Onboard (Fahrzeug).
Das Skript muss mit `sudo` von einem normalen Benutzer ausgeführt werden (nicht als root direkt).

```bash
git clone https://github.com/atomroflman/LteCar.git
cd LteCar
sudo bash install.sh
```

Das Skript fragt interaktiv ab, ob ein **Server** oder ein **Onboard-Client** installiert werden soll,
und bietet am Ende optional die Einrichtung als systemd-Service an.

### Was passiert bei der Installation?

**Server-Modus:**
- System-Pakete (Node.js, npm)
- .NET 8 SDK (für den aktuellen Benutzer)
- Janus Gateway (WebRTC)
- Next.js Web-Client Build
- .NET Server Build
- Optional: systemd-Services `ltecar-server` + `ltecar-client`

**Onboard-Modus:**
- System-Pakete (GStreamer, Kamera-Bibliotheken)
- .NET 8 SDK (für den aktuellen Benutzer)
- WiringPi (GPIO)
- .NET Onboard Build
- Optional: systemd-Service `ltecar-onboard`

### Logs & Verwaltung

```bash
# Logs
ls /var/log/ltecar/

# Services verwalten
sudo systemctl status ltecar-server
sudo systemctl restart ltecar-onboard
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
