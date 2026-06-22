# LteCar – Konzepte & Features

## Architektur
- **3-Schichten-Stack**: Browser (Next.js) ↔ Server (ASP.NET Core 8) ↔ Onboard (Raspberry Pi, .NET 8).
- **Kommunikation**: SignalR (MessagePack/JSON) + WebRTC (Janus) für Video.
- **LTE-Design**: Onboard öffnet ausschließlich ausgehende Verbindungen → NAT-/firewall-tauglich.
- **Container**: `docker-compose.yml` startet `postgres`, `server`, `janus`, `client`, `nginx` (Port 8080).
- **Nginx**: Reverse-Proxy mit WebSocket-Upgrade für `/hubs/`, `/api/`, `/janus/`, `/janus-ws/`.

## SignalR-Hubs (`Shared/HubPaths.cs`)
| Hub | Pfad | Zweck |
|-----|------|-------|
| `CarConnectionHub` | `/hubs/connection` | Fahrzeugregistrierung, ChannelMap-Sync, FileTransfer |
| `CarControlHub` | `/hubs/control` | SSH-Auth, Steuerbefehle, Ping |
| `TelemetryHub` | `/hubs/telemetry` | Telemetrie-Streaming |
| `CarUiHub` | `/hubs/carui` | Live-Status, Bash-Output-Broadcast |
| `CarVideoHub` | `/hubs/video` | Video-Stream-Verwaltung |
| `CarBashHub` | `/hubs/carbash` | Interaktive Bash-Sessions |
| `UserChannelHub` | `/hubs/userchannel` | Gamepad-Sync zwischen Browsern |

## Neues Verbindungsmodell (`LTE_USE_NEW_CONNECTION_MODEL`, default true)
- Erst `SyncChannelMap` (Map → Server vergibt IDs + SHA256-Hash).
- Dann `OpenCarConnection(carIdentityKey, hash)`.
- Server fordert nur bei Hash-Mismatch `RequiresChannelMapUpdate` → spart Bandbreite/Roundtrips.

## VehicleConnectionManager (Onboard)
- Zentrale HubConnection mit Auto-Reconnect (exponentielles Backoff) zu `CarConnectionHub`.
- Reflektiert alle `IVehicleService`-Implementierungen → ruft `OnConnected`/`OnReconnected`.
- `CarConnectionStore` (Server) mappt `connectionId ↔ carId` via `BiDictionary`.
- `channelMap.server.json` cached die Server-ID-Zuordnung.

## Setup-Tool (raspi-config-Style, Spectre.Console)
Start: `dotnet run -- setup`. 7 Menüs: System, Network/Server, Vehicle Config, Hardware Test, Templates, Feature Flags, Update/Recovery. Bearbeitet `appSettings.json`, `channelMap.json`, SSH-/Identity-Keys.

## Feature Flags
| Flag | Beschreibung |
|------|--------------|
| `webSetup` | Web-Setup-Interface |
| `bashTool` | Remote-Bash-Befehlsausführung |
| `channelTester` | Hardware-Kanal-Tests |
| `audio` | Bidirektionaler Audio-Chat |
| `video` | Video-Streaming |

## Onboard-Services
- **TelemetryService** – Tick-Schleife, liest `TelemetryReaderBase` (CpuTemp, JBD-BMS, Lifetime), pusht via TelemetryHub.
- **ControlService** – Empfängt Steuerbefehle, SSH-Challenge-Auth, delegiert an Hardware (PCA9685/GPIO).
- **VideoStreamService** – Verwaltet MediaMTX-Prozess + Konfiguration aus ChannelMap.
- **AudioChatService** – Bidirektionale Audio-Verbindung, Geräteverwaltung, Echo-Cancellation.
- **BashToolService** – Lokale Prozesse, Output via `CarUiHub` an Web-Client.
- **ChannelTester** – Konsolen-Test für Hardware-Kanäle.
- **SshKeyService** – RSA-2048, Challenge/Verify, Fingerprint-Log.
- **CarConfigurationService** – Speichert Server-Konfig (Janus, VideoSettings).
- **MediaMtxConfigurator** – Konfiguriert `mediamtx.yml` für Kamera-Stream.

## WebRTC / Janus
- Pfad: Kamera → MediaMTX (RTSP) → ffmpeg/TCP-Relay → Janus (RTP → WebRTC) → Browser.
- `VideoStreamReceiverService` allokiert UDP-Ports (10000–10200), legt Janus-Stream-Endpunkte an.
- `ActiveVideoStreamViewerRegistry` startet/stoppt Stream nur bei aktivem Viewer.

## Datenbank (EF Core 9, PostgreSQL)
Entities: `User`, `Car`, `CarChannel`, `CarTelemetry`, `CarVideoStream`, `UserCarSetup`, `UserSetupFlowNodeBase`, `UserSetupLink`, `FileTransfer`. Sqids-kodierte Session-IDs via `UserSessionSeq`.

## Client (Next.js 15, React 19, Zustand, ReactFlow)
- `/` – Hauptseite: Video, Auto-Auswahl, Steuerung, Telemetrie.
- `/car/[carId]` – ReactFlow-Editor für Gamepad→Channel-Mapping.
- `/car/[carId]/bash` – Bash-Terminal.
- Komponenten: `car-control`, `car-video-panel`, `video-stream` (Janus-Client), `telemetry`, `ssh-key-manager`, `session-transfer`, `gamepad-viewer`, `audio-chat`, `update-control`, `config-guard`, `install-dialog`.

## Templates (`VehicleTemplates/`)
Pro Fahrzeug ein Ordner mit `config.json` (ChannelMap) + optional `scripts/`, `models/`, `docs/`, `README.md`. `VehicleTemplateManager`: List/Choose/Apply/Save/Delete. Pfad via `VEHICLE_TEMPLATES_PATH`.

## Bash-Tool
- Web → Server: `CarControlHub.ExecuteBashCommand` / `CarBashHub.ExecuteCommand`.
- Server → Onboard: SignalR-Relay → `BashToolService`.
- Onboard: startet `/bin/bash`, piped Output via `SendBashOutput` über `CarUiHub`.

## File-Transfer
- **Upload**: `RequestFileUpload` → Token → `POST /api/filetransfer/{token}` (max 100 MB, SHA256-Check) → `FileReady`-Notification.
- **Download**: `GET /api/filetransfer/{token}/download` mit Range-Support + Bandbreiten-Throttle (Default 20 KB/s).
- `FileTransfer`-Entity trackt Status.

## Authentifizierung
- **Browser**: Cookie `LteCarAuth` (`HttpOnly`, `SameSite=Lax`), Sqids-Session-Token, Recovery-Key, 5-Min-`TransferCode` für Session-Transfer.
- **Fahrzeug-Identität**: GUID (`carIdentityKey.txt`), SHA256-Verifikation.
- **Steuerungs-Auth**: SSH-Key-Challenge-Response (RSA-2048, Web Crypto API). Private Key wird nach Download auf Onboard gelöscht.
- **DataProtection-Keys** in `Server/DataProtectionKeys/`.

## Konfiguration
- **Onboard `appSettings.json`**: `ServerName`, `ServerPort`, `UseHttps`, `CarName`, `CarSecret`, `CameraOptions`, Feature Flags.
- **Server `appSettings.json`**: `IdSalt`, `IdAlphabet`, `ConnectionStrings.DefaultConnection`, `JanusConfiguration`, `FileTransfer`.
- **Onboard Config-Verzeichnis**: via `CONFIG_DIR` oder `--config-dir=` / `.configdir`.
- **ChannelMap**: trennt `pinManagers`, `controlChannels`, `telemetryChannels`, `videoStreams`.
- **Env-Variablen** (Server): `ConnectionStrings__DefaultConnection`, `JanusConfiguration__HostName`, `FileTransfer__StoragePath`, `ASPNETCORE_ENVIRONMENT`, `GIT_BRANCH`, `GIT_COMMIT`.
