# LteCar Video-Stack – Architektur & Schritt-für-Schritt-Testanleitung

Dieses Dokument beschreibt den kompletten Datenfluss eines Video-Streams vom Raspberry-Pi-Kamerasensor bis zum Browser, definiert Testpunkte an jedem Glied der Kette und gibt Kommandos an, mit denen geprüft werden kann, ob der Stream bis zu diesem Punkt funktioniert.

> Stand: 2026-08-10  
> Betroffene Systeme: `lte-truck` (Onboard / Raspberry Pi), `lte-rc-server` (Server + Janus), Browser-Client.

---

## 1. Überblick

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  BROWSER                                                                      │
│  ┌─────────────────┐   WebRTC (SFU)   ┌──────────────────────────────────┐  │
│  │ video-stream.tsx│ ◄────────────────► │ Janus WebRTC Server (Container)  │  │
│  └─────────────────┘                   │  - streaming plugin              │  │
│                                        │  - RTP mountpoint (UDP ingest)   │  │
└────────────────────────────────────────┴──────────────────────────────────┘  │
                                           ▲                                    │
                                           │ UDP/RTP (Port 10000–10200)        │
                                           │                                    │
┌──────────────────────────────────────────┴──────────────────────────────────┐
│  lte-rc-server                                                                │
│  ┌──────────────────────────┐    SignalR     ┌────────────────────────────┐ │
│  │ LteCar.Server            │ ◄──────────────►│ LteCar.Onboard (lte-truck) │ │
│  │  - VideoStreamReceiver   │                 │  - VideoStreamService      │ │
│  │  - ActiveVideoStreamViewerRegistry          │  - MediaMtxConfigurator    │ │
│  └──────────────────────────┘                 └────────────────────────────┘ │
│                          │                                                    │
│                          ▼ Janus HTTP API (localhost:8088)                    │
│                   ┌──────────────┐                                            │
│                   │ Janus        │                                            │
│                   └──────────────┘                                            │
└───────────────────────────────────────────────────────────────────────────────┘

                                           │
                                           │ RTSP
                                           │
                            ┌──────────────▼──────────────┐
                            │ MediaMTX (auf lte-truck)    │
                            │  - source: rpiCamera        │
                            │  - runOnInit: ffmpeg        │
                            └──────────────┬──────────────┘
                                           │
                                           ▼ libcamera
                            ┌─────────────────────────────┐
                            │ Raspberry Pi Camera (OV5647)│
                            │  /dev/media0 + /dev/media3  │
                            └─────────────────────────────┘
```

### Kurzbeschreibung der Schritte

1. **Kamera / libcamera** – MediaMTX öffnet die Raspberry-Pi-Kamera über `libcamera`.
2. **MediaMTX / RTSP** – MediaMTX stellt den Stream intern als `rtsp://localhost:8554/<streamId>` bereit.
3. **Onboard ffmpeg** – MediaMTX startet `runOnInit: ffmpeg`, der RTSP liest und RTP zum Server sendet.
4. **Netzwerk lt-rc-server** – RTP-Pakete kommen am Server an (UDP-Port-Bereich 10000–10200).
5. **Janus ingest** – Der Server erzeugt über die Janus-HTTP-API einen RTP-Mountpoint und Janus empfängt das RTP.
6. **SignalR Steuerung** – Client und Server koordinieren über SignalR, wann ein Stream aktiv sein soll.
7. **WebRTC Auslieferung** – Der Browser holt den Stream per WebRTC von Janus ab.

---

## 2. Schritt-für-Schritt-Testanleitung

### Schritt 0: Onboard-Prozess läuft

Der gesamte Video-Stack beginnt auf dem Truck. Wenn `LteCar.Onboard` nicht läuft, gibt es keinen Stream.

**Was passiert:**
- `LteCar.Onboard` registriert sich am Server (`CarConnectionHub.OpenCarConnection`).
- Empfängt es `StartVideoStream(streamId, settings)`, startet es MediaMTX neu und konfiguriert ffmpeg mit `settings.TargetPort`.

**Test:**

```bash
ssh lte-truck "ps aux | grep -E 'LteCar.Onboard|mediamtx|mtxrpicam' | grep -v grep"
```

**Erwartetes Ergebnis:**
```text
greg-e  <pid>  …  ./LteCar.Onboard
greg-e  <pid>  …  /…/Extern/mediamtx /…/Extern/mediamtx.yml
greg-e  <pid>  …  /dev/shm/mediamtx-rpicamera-…/mtxrpicam
```

**Wenn nicht:**
- Service `ltecar-onboard.service` ist `failed` (siehe `systemctl status ltecar-onboard`).
- Oder der manuell gestartete Prozess wurde beendet (Reboot, Crash, Terminal-Session beendet).
- Lösung: `LteCar.Onboard` neu starten (siehe Abschnitt 3).

---

### Schritt 1: Kamera wird von MediaMTX geöffnet

**Was passiert:**
- MediaMTX startet für jeden `source: rpiCamera`-Pfad den Helfer `mtxrpicam`.
- `mtxrpicam` spricht über libcamera mit `/dev/media0` und `/dev/media3`.

**Test:**

```bash
ssh lte-truck "sudo lsof /dev/media0 /dev/media3 2>/dev/null"
```

**Erwartetes Ergebnis:**
```text
COMMAND    PID   USER FD   TYPE DEVICE NAME
mtxrpicam <pid> greg-e 6uW  CHR  511,0 /dev/media0
mtxrpicam <pid> greg-e 7uW  CHR  511,3 /dev/media3
```

**Test:**

```bash
ssh lte-truck "grep -E 'Camera.acquire|stream is available' /tmp/ltecar_onboard_restart.log | tail -10"
```

**Erwartetes Ergebnis:**
```text
[path mainCamera] stream is available and online, 1 track (H264)
```

**Wenn nicht:**
- Fehler `Pipeline handler in use by another process` → alter `mtxrpicam`-Prozess blockiert die Kamera.
- Lösung: alle `mtxrpicam`-Prozesse beenden, Onboard/MediaMTX neu starten.

---

### Schritt 2: RTSP-Stream auf localhost verfügbar

**Was passiert:**
- MediaMTX publiziert den Kamera-Stream unter `rtsp://localhost:8554/<streamId>`.
- `runOnInit: ffmpeg` liest diesen RTSP-Stream und sendet ihn als RTP zum Server.

**Test:**

```bash
ssh lte-truck "ffmpeg -rtsp_transport tcp -i rtsp://localhost:8554/mainCamera -c copy -f null - 2>&1 | tail -20"
```

**Erwartetes Ergebnis:**
- Kein `404 Not Found`.
- Ausgabe enthält `Stream #0:0: Video: h264` und läuft weiter (mit `frame= … fps= …`).

**Wenn nicht:**
- MediaMTX hat den Pfad nicht korrekt erstellt.
- Config prüfen: `cat /home/greg-e/SignalRC/Onboard/bin/Debug/net10.0/Extern/mediamtx.yml`.

---

### Schritt 3: ffmpeg sendet RTP zum Server

**Was passiert:**
- `ffmpeg -i rtsp://localhost:8554/mainCamera -c copy -f rtp rtp://<server>:<targetPort>?pkt_size=1300`
- `<targetPort>` wird vom Server vergeben und per SignalR an Onboard übermittelt.

**Test auf lte-truck:**

```bash
ssh lte-truck "ps aux | grep -E 'ffmpeg.*rtp' | grep -v grep"
```

**Erwartetes Ergebnis:**
```text
greg-e  <pid>  …  ffmpeg -t 2147483647 -i rtsp://localhost:8554/mainCamera -c copy -f rtp rtp://lte-rc.northeurope.cloudapp.azure.com:<port>?pkt_size=1300
```

**Test auf lte-rc-server (UDP-Pakete ankommend):**

```bash
ssh lte-rc-server "ss -uanp | grep -E ':10000|:10001'"
```

**Erwartetes Ergebnis:**
```text
UNCONN 0  0  *:<port>  *:*  users:(("rootlessport",pid=…,fd=…))
```

**Genauerer Test (Pakete zählen, 30 Sekunden):**

```bash
ssh lte-rc-server "sudo timeout 30 tcpdump -nni any udp port <targetPort> -c 100 2>&1 | tail -20"
```

**Wenn nicht:**
- Firewall / NAT blockiert ausgehenden RTP-Verkehr.
- ffmpeg-Arguments falsch (Server-Hostname / Port).
- Onboard hat keinen gültigen `TargetPort` vom Server erhalten.

---

### Schritt 4: Janus läuft und ist vom Server erreichbar

**Was passiert:**
- Der Server spricht Janus über dessen HTTP-API auf Port 8088 an.
- Für jeden aktiven Stream wird ein RTP-Mountpoint im `janus.plugin.streaming` erzeugt.

**Test:**

```bash
ssh lte-rc-server "curl -s http://localhost:8088/janus/info | head -10"
```

**Erwartetes Ergebnis:**
```json
{
  "janus": "server_info",
  "name": "Janus WebRTC Server",
  "version_string": "1.1.4",
  ...
}
```

**Test:**

```bash
ssh lte-rc-server "docker ps | grep janus"
# bzw. podman
ssh lte-rc-server "podman ps | grep janus"
```

**Erwartetes Ergebnis:**
```text
<container-id>  docker.io/canyan/janus-gateway:latest  …  Up …  0.0.0.0:8088->8088, 0.0.0.0:8188->8188, 10000-10200/udp
```

**Wenn nicht:**
- Janus-Container nicht gestartet.
- `janus.transport.http.jcfg` bindet nur IPv6; `ip = "0.0.0.0"` setzen.

---

### Schritt 5: Janus-Mountpoint existiert für den Stream

**Was passiert:**
- `VideoStreamReceiverService.OpenJanusEndpointAsync` erzeugt:
  1. Janus-Session
  2. Handle für `janus.plugin.streaming`
  3. RTP-Mountpoint mit der DB-Id des Streams und `VideoPort = stream.JanusPort`

**Test (manuelle Janus-API):**

```bash
ssh lte-rc-server '
  SESSION=$(curl -s -X POST http://localhost:8088/janus -d "{\"janus\":\"create\",\"transaction\":\"t1\"}" | jq -r .data.id) &&
  HANDLE=$(curl -s -X POST http://localhost:8088/janus/$SESSION -d "{\"janus\":\"attach\",\"plugin\":\"janus.plugin.streaming\",\"transaction\":\"t2\"}" | jq -r .data.id) &&
  curl -s -X POST http://localhost:8088/janus/$SESSION/$HANDLE -d "{\"janus\":\"message\",\"body\":{\"request\":\"list\"},\"transaction\":\"t3\"}" | jq .'
```

**Erwartetes Ergebnis:**
```json
{
  "janus": "success",
  "plugindata": {
    "data": {
      "streaming": "list",
      "list": [
        {
          "id": <streamDbId>,
          "description": "<car>-<streamName>",
          "type": "rtp",
          "video_port": <janusPort>
        }
      ]
    }
  }
}
```

**Wenn nicht:**
- Server-Log prüfen: `VideoStreamReceiverService` konnte Janus-Endpoint nicht erstellen.
- Mögliche Ursachen: Port-Bereich erschöpft, Janus nicht erreichbar, Mountpoint-Id-Kollision.

---

### Schritt 6: SignalR-Steuerung funktioniert

**Was passiert:**
- Im Browser wird ein Stream ausgewählt (`car-video-panel.tsx`).
- `ActivateStream(streamId)` wird über SignalR `CarConnectionHub` aufgerufen.
- Der Server startet den Stream (`StartStreamForViewersAsync`) und sendet `StartVideoStream` an den Truck.
- Der Truck startet daraufhin MediaMTX/ffmpeg (siehe Schritt 3).

**Test aus dem Browser-DevTools-Konsolen-Tab (authentifiziert auf der UI):**

```javascript
const conn = new signalR.HubConnectionBuilder()
  .withUrl('/hubs/connection')
  .withAutomaticReconnect()
  .build();
await conn.start();
const streams = await conn.invoke('GetVideoStreamsForCar', <carId>);
console.table(streams);
await conn.invoke('ActivateStream', streams[0].id);
```

**Erwartetes Ergebnis:**
- `GetVideoStreamsForCar` liefert mindestens einen aktivierten Stream zurück.
- `ActivateStream` kehrt ohne Fehler zurück.
- Auf dem Truck erscheint ein ffmpeg-Prozess (siehe Schritt 3).

**Wenn nicht:**
- SignalR-Verbindung prüfen (`/hubs/connection` muss durch nginx auf den Server proxied werden).
- Server-Log auf Fehler bei `ActivateStream` prüfen.

---

### Schritt 7: Browser kann Janus erreichen

**Was passiert:**
- `video-stream.tsx` lädt `/janus.js` und verbindet sich mit `/janus` (HTTP) und `/janus-ws` (WebSocket).
- nginx leitet beides an den Janus-Container weiter.

**Test:**

```bash
# vom lokalen Rechner / Browser-Host
curl -s https://<server-url>/janus/info | head -5
# z. B.:
curl -s https://lte-rc.northeurope.cloudapp.azure.com/janus/info | head -5
```

**Erwartetes Ergebnis:**
```json
{ "janus": "server_info", ... }
```

**Test WebSocket:**

```bash
curl -i -N \
  -H "Connection: Upgrade" \
  -H "Upgrade: websocket" \
  -H "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==" \
  -H "Sec-WebSocket-Version: 13" \
  https://<server-url>/janus-ws
```

**Erwartetes Ergebnis:**
- HTTP/1.1 101 Switching Protocols.

**Wenn nicht:**
- nginx-Config prüfen (`nginx/nginx.conf`).
- Firewall am Server prüfen.
- WebSocket-Pfad muss exakt `/janus-ws` (ohne trailing slash) erreichbar sein.

---

### Schritt 8: WebRTC-ICE und Medienfluss im Browser

**Was passiert:**
- `video-stream.tsx` attached das `janus.plugin.streaming`-Plugin.
- Es fordert `request: 'list'` an, sucht den passenden Mountpoint anhand `streamId` und sendet `request: 'watch'`.
- Janus antwortet mit einem SDP-Offer; der Browser erzeugt einen SDP-Answer und sendet `request: 'start'`.
- Janus liefert Video-Frames an den Browser.

**Test im Browser-DevTools:**

1. **Network-Tab:** `/janus` und `/janus-ws` müssen 200/101 zurückgeben.
2. **Console:** Keine Fehler wie `Janus init error`, `plugin attach error`, `noStreamsAvailable`.
3. **WebRTC-Internals:** `chrome://webrtc-internals` (Chrome) oder `about:webrtc` (Firefox).
   - ICE state sollte `connected` oder `completed` werden.
   - Inbound-RTP-Statistik sollte `packetsReceived` und `framesPerSecond` ansteigen.

**Erwartetes Ergebnis:**
- `<video>`-Element zeigt Bild.
- Overlay zeigt fps und Bitrate.

**Wenn nicht:**
- `chrome://webrtc-internals` öffnen und prüfen:
  - Keine ICE-Candidates? → NAT/Firewall-Problem, `JANUS_NAT_1_1` in `docker-compose.yml` prüfen.
  - ICE connected, aber keine Pakete? → RTP kommt nicht in Janus an (Schritte 3–5 prüfen).
  - `noStreamsAvailable` → Mountpoint existiert nicht (Schritt 5).

---

## 3. Wiederanlauf nach einem Ausfall

### 3.1 Truck wurde neu gestartet

Derzeit läuft Onboard auf `lte-truck` nicht automatisch, weil `ltecar-onboard.service` im Zustand `failed` ist. Nach einem Reboot muss manuell neu gestartet werden:

```bash
ssh lte-truck

cd /home/greg-e/SignalRC/Onboard/bin/Debug/net10.0/
DOTNET_ROOT=/home/greg-e/.dotnet \
DOTNET_ENVIRONMENT=Production \
PATH=/home/greg-e/.dotnet:/home/greg-e/.dotnet/tools:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin \
nohup ./LteCar.Onboard > /tmp/ltecar_onboard_restart.log 2>&1 &
```

Danach Schritt 0–2 prüfen.

### 3.2 Kamera blockiert

```bash
ssh lte-truck "ps aux | grep -E 'mtxrpicam|mediamtx' | grep -v grep"
# Wenn mehrere Instanzen laufen:
ssh lte-truck "sudo killall -9 mtxrpicam mediamtx"
# Onboard neu starten (siehe 3.1)
```

### 3.3 Janus-Mountpoint stale / doppelt

```bash
ssh lte-rc-server '
  SESSION=$(curl -s -X POST http://localhost:8088/janus -d "{\"janus\":\"create\",\"transaction\":\"t1\"}" | jq -r .data.id) &&
  HANDLE=$(curl -s -X POST http://localhost:8088/janus/$SESSION -d "{\"janus\":\"attach\",\"plugin\":\"janus.plugin.streaming\",\"transaction\":\"t2\"}" | jq -r .data.id) &&
  curl -s -X POST http://localhost:8088/janus/$SESSION/$HANDLE -d "{\"janus\":\"message\",\"body\":{\"request\":\"list\"},\"transaction\":\"t3\"}" | jq .'
```

Bei Bedarf einzelne Mountpoints destroyen:

```bash
ssh lte-rc-server '
  SESSION=… HANDLE=…
  curl -s -X POST http://localhost:8088/janus/$SESSION/$HANDLE \
    -d "{\"janus\":\"message\",\"body\":{\"request\":\"destroy\",\"id\":<streamDbId>},\"transaction\":\"t4\"}"'
```

---

## 4. Bekannte Schwachstellen & geplante Fixes

| Problem | Ursache | Status |
|---------|---------|--------|
| Onboard startet nach Reboot nicht automatisch | `ltecar-onboard.service` ist `failed` | Muss untersucht / Service repariert werden |
| Kamera bleibt von altem `mtxrpicam` blockiert | `MediaMtxConfigurator.StopAsync()` beendetet nur den Hauptprozess, nicht den Helfer | Fix in `Onboard/Services/MediaMtxConfigurator.cs` implementiert |
| Server-pushte ChannelMap enthält keine `pinManagers` | `ChannelMap.PinManagers` hat `[IgnoreMember]` | Fix in `Shared/Channels/ChannelMap.cs` implementiert |
| Server-Container-Neustart verliert Janus-Sessions | Janus hält Sessions im RAM | Kein Fix nötig; Server baut Mountpoints bei Bedarf neu |

---

## 5. Dateien im Projekt

| Datei | Zweck |
|-------|-------|
| `Onboard/Video/VideoStreamService.cs` | Startet/stoppt MediaMTX basierend auf aktiven Streams |
| `Onboard/Services/MediaMtxConfigurator.cs` | Schreibt `mediamtx.yml`, startet/beendet MediaMTX |
| `Server/Hubs/CarConnectionHub.cs` | SignalR-Hub für Stream-Aktivierung/-Deaktivierung |
| `Server/Services/VideoStreamReceiverService.cs` | Weist Ports zu, erzeugt Janus-Mountpoints |
| `Server/Services/ActiveVideoStreamViewerRegistry.cs` | Zählt Viewer pro Stream |
| `Shared/Channels/ChannelMap.cs` | Datentyp für ChannelMap inkl. PinManagers |
| `Client/src/components/car-video-panel.tsx` | UI für Stream-Auswahl und Aktivierung |
| `Client/src/components/video-stream.tsx` | Janus/WebRTC-Player im Browser |
| `nginx/nginx.conf` | Reverse-Proxy für Client, Server, Janus |
| `docker-compose.yml` | Container-Orchestrierung |
| `janus-config/janus.plugin.streaming.jcfg` | Deaktiviert Janus-Beispiel-Streams (`no_default_streams = true`) |
