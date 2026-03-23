# LteCar Features

## Overview

LteCar provides the following features for remote vehicle control:

- **Remote Control**: Drive your car over LTE/Internet with low latency
- **Video Streaming**: Real-time video from on-board camera
- **Audio Chat**: Bidirectional audio communication
- **Telemetry**: Monitor battery, CPU temperature, and other sensors
- **Bash Tool**: Execute bash commands on the vehicle remotely
- **Channel Tester**: Test hardware channels from the web interface

---

## Remote Control

### How It Works

1. User opens the web interface and selects a vehicle
2. SSH authentication challenge-response occurs
3. Upon successful authentication, control is granted
4. User uses gamepad/keyboard to drive
5. Control signals are sent via SignalR to the server, then to the vehicle
6. Vehicle executes controls and streams telemetry back

### Channels

LteCar supports unlimited control channels defined in `channelMap.json`:

| Channel Type | Description |
|-------------|-------------|
| Steering | Servo-based steering control |
| Throttle | Motor speed control |
| Lights | LED and lighting control |
| Gear | Transmission/gear control |

---

## Video Streaming

### Architecture

- **Janus Gateway**: WebRTC signaling and streaming
- **MediaMTX**: RTSP server for camera feeds
- **WebRTC**: Browser-compatible video streaming

### Configuration

```json
{
  "VideoStreams": {
    "front": {
      "StreamId": "rpi0",
      "Enabled": true,
      "Location": "front"
    }
  }
}
```

### Ports

Video streams use ports in range 10001-10100.

---

## Audio Chat

### Features

- Bidirectional audio between driver and vehicle
- Microphone and speaker selection
- Echo cancellation enabled by default
- Noise suppression

### Configuration

Enable via feature flag:

```json
{
  "audio": true
}
```

### Ports

Audio streams use ports in range 11001-11100.

---

## Bash Tool (Remote Command Execution)

> **Security Note**: BashTool allows remote execution of bash commands on the vehicle. Enable only when needed.

### How It Works

1. Web client connects to server via SignalR hub `/hubs/carbash`
2. Vehicle's BashToolService connects to the same hub
3. Web client sends commands to server
4. Server proxies command to vehicle
5. Vehicle executes command and streams output back

### Architecture

```
┌──────────────┐     SignalR      ┌──────────────┐     SignalR      ┌──────────────┐
│ Web Client   │◄───────────────►│    Server    │◄───────────────►│   Vehicle    │
│              │   /hubs/carbash │  CarBashHub  │   /hubs/carbash │ BashToolSvc  │
└──────────────┘                └──────────────┘                └──────────────┘
```

### Enable

1. Via Setup Tool: **6. Feature Flags** → **F2. Bash Tool** → Enable
2. Or via `appSettings.json`:

```json
{
  "bashTool": true
}
```

### Usage

Commands are executed in the vehicle's userspace with:
- Working directory: User's home directory
- Environment: Standard user environment
- Output: UTF-8 encoded, streamed in real-time

### Security

- Commands run as the user running the Onboard service
- No root privileges by default
- Can be disabled via feature flag

---

## Channel Tester

The Channel Tester allows testing individual hardware channels from the web interface.

### How It Works

1. User selects a channel from the web UI
2. User sets a value (e.g., 50% throttle)
3. Command is sent to vehicle via SignalR
4. Vehicle executes the control
5. Telemetry confirms execution

### Enable

```json
{
  "channelTester": true
}
```

### Use Cases

- Test servo calibration
- Verify motor response
- Check LED connections
- Validate sensor readings

---

## Web Setup Interface

The Web Setup provides a browser-based alternative to the console setup tool.

### Enable

```json
{
  "webSetup": true
}
```

### Features

- Vehicle configuration via web browser
- Channel mapping
- Feature flag toggles
- System monitoring

---

## Templates

Vehicle templates allow sharing and reusing configurations.

### Template Structure

```
vehicleTemplates/
├── MyCar/
│   ├── config.json      # ChannelMap configuration
│   └── metadata.json    # Template metadata
└── AnotherCar/
    └── ...
```

### Template Metadata

```json
{
  "name": "My RC Car",
  "description": "Standard RC car configuration",
  "version": "1.0.0",
  "author": "Your Name",
  "createdAt": "2024-01-01"
}
```

### Creating a Template

1. Configure your vehicle
2. Run setup: `dotnet run -- setup`
3. Go to **5. Templates** → **T3. Create Template from Current**
4. Enter name and description

### Applying a Template

1. Run setup: `dotnet run -- setup`
2. Go to **5. Templates** → **T2. Select Template**
3. Choose template from list

### Template Base Path

Templates are looked up from:
1. `VEHICLE_TEMPLATES_PATH` environment variable
2. `~/.ltecar/vehicleTemplates/` by default

---

## LTE Connectivity Note

> **Important**: The Onboard client initiates an **outbound-only connection** to the server. This means:
>
> - The vehicle cannot be reached directly from the internet
> - All communication is initiated by the vehicle
> - No inbound ports need to be opened on the vehicle
> - This works through NAT and most firewall configurations

The server must be accessible from the vehicle (standard HTTPS port 5000).

---

## Feature Flags Summary

| Feature | Default | Description |
|---------|---------|-------------|
| `webSetup` | false | Web-based setup interface |
| `bashTool` | false | Remote bash command execution |
| `channelTester` | false | Web-based channel testing |
| `audio` | false | Audio chat functionality |
| `video` | false | Video streaming |

All features are **disabled by default**. Enable via setup tool or `appSettings.json`.
