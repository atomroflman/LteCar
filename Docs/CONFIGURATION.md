# LteCar Configuration Reference

## Configuration Files

### appSettings.json (Onboard)

Located in the config directory, contains vehicle and server settings.

```json
{
  "carId": "vehicle-001",
  "carName": "My RC Car",
  "carPasswordHash": "base64hash==",
  "serverUrl": "https://lte-rc.example.com:5000",
  "apiKey": "optional-api-key",
  
  "webSetup": false,
  "bashTool": false,
  "channelTester": false,
  "audio": false,
  "video": true,
  
  "UseHttps": true,
  "ServerName": "lte-rc.example.com",
  "ServerPort": 5000
}
```

### channelMap.json (Onboard)

Defines all hardware channels, telemetry, and video streams.

```json
{
  "pinManagers": {
    "pca9685": {
      "type": "Pca9685PwmExtension",
      "options": {
        "boardAddress": 0x40,
        "i2cBus": 1
      }
    }
  },
  "controlChannels": {
    "steering": {
      "address": 0,
      "controlType": "Steering",
      "pinManager": "pca9685",
      "minPulse": 1000,
      "maxPulse": 2000,
      "centerPulse": 1500
    },
    "throttle": {
      "address": 1,
      "controlType": "Throttle",
      "pinManager": "pca9685",
      "testDisabled": true
    }
  },
  "telemetryChannels": {
    "battery": {
      "type": "JbdBmsTelemetryReader",
      "readIntervalTicks": 50
    },
    "cpuTemp": {
      "type": "CpuTemperatureReader",
      "readIntervalTicks": 100
    }
  },
  "videoStreams": {
    "front": {
      "streamId": "rpi0",
      "enabled": true,
      "location": "front",
      "priority": 1
    }
  }
}
```

---

## Feature Flags

Feature flags control optional functionality. All are **disabled by default**.

| Flag | Type | Description |
|------|------|-------------|
| `webSetup` | bool | Enable web-based setup interface |
| `bashTool` | bool | Enable remote bash command execution |
| `channelTester` | bool | Enable channel testing from web UI |
| `audio` | bool | Enable audio chat functionality |
| `video` | bool | Enable video streaming |

### Enabling Features

**Via Setup Tool:**
1. Run `dotnet run -- setup`
2. Go to **6. Feature Flags**
3. Select feature to toggle

**Via JSON:**
```json
{
  "bashTool": true,
  "audio": true,
  "video": true
}
```

---

## Server appSettings.json

### Application Configuration

```json
{
  "ServerName": "lte-rc.example.com",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=ltecar;Username=user;Password=pass"
  },
  "Application": {
    "RunJanusServer": true,
    "IdleTimeoutMinutes": 30,
    "MaxConcurrentCars": 10
  },
  "Janus": {
    "HostName": "localhost",
    "PortRangeStart": 10001,
    "PortRangeEnd": 10100
  },
  "FileTransfer": {
    "ThrottleKBytesPerSecond": 1024,
    "MaxFileSizeMB": 100,
    "StoragePath": "./file-storage"
  }
}
```

---

## SignalR Hubs

| Hub | Path | Purpose |
|-----|------|---------|
| CarConnectionHub | `/hubs/connection` | Vehicle registration and connection |
| CarControlHub | `/hubs/control` | Remote control commands |
| TelemetryHub | `/hubs/telemetry` | Telemetry data streaming |
| CarUiHub | `/hubs/carui` | UI state updates |
| CarVideoHub | `/hubs/video` | Video streaming |
| UserChannelHub | `/hubs/userchannel` | User channel configuration |
| CarBashHub | `/hubs/carbash` | Bash command proxy |

---

## Environment Variables

### Onboard

| Variable | Description |
|----------|-------------|
| `CONFIG_DIR` | Config directory path |
| `VEHICLE_TEMPLATES_PATH` | Template base path |
| `LTE_USE_NEW_CONNECTION_MODEL` | Use new connection model (default: true) |

### Server

| Variable | Description |
|----------|-------------|
| `ASPNETCORE_ENVIRONMENT` | Development/Production |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `RunJanusServer` | Start Janus inside the server process |
| `JanusConfiguration__HostName` | Janus host name or IP |
| `JanusConfiguration__PortRangeStart` | First UDP video port |
| `JanusConfiguration__PortRangeEnd` | Last UDP video port |
| `FileTransfer__StoragePath` | Local file storage path |

---

## Database

LteCar uses PostgreSQL. **Never modify the database manually** - always use EF Core migrations.

### Migrations

```bash
cd Server

# Add migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Remove last migration (if not applied)
dotnet ef migrations remove
```

### Key Tables

| Table | Description |
|-------|-------------|
| Users | User accounts |
| Cars | Registered vehicles |
| CarChannels | Channel definitions per car |
| UserCarSetups | User-vehicle associations |

---

## SSH Key Authentication

Vehicles authenticate using SSH key pairs:

1. On first startup, vehicle generates RSA-2048 key pair
2. Private key is served via HTTP (port 8080) when vehicle is selected
3. User downloads key, uses it for SSH authentication
4. Private key is deleted from vehicle after download

### Key Files

| File | Location | Purpose |
|------|----------|---------|
| `carIdentityKey` | Config dir | Vehicle identity (GUID) |
| `ssh_key` | Config dir | Private key (PKCS#8 DER) |
| `ssh_key.pub` | Config dir | Public key (SPKI DER) |

---

## Network Ports

### Server (Azure VM)

| Port | Protocol | Service |
|------|----------|---------|
| 22 | TCP | SSH |
| 443 | TCP | HTTPS (reverse proxy) |
| 5000 | TCP | LteCar Server |
| 8080 | TCP | SSH key download |
| 10001-10100 | UDP | Video streams (Janus) |
| 11001-11100 | UDP | Audio streams (Janus) |

### Onboard (Vehicle)

| Port | Protocol | Service |
|------|----------|---------|
| 22 | TCP | SSH (optional) |
| 8080 | TCP | SSH key server |
| 5000 | TCP | Server HTTPS |

---

## Hardware Configuration

### Pin Managers

| Type | Description |
|------|-------------|
| `Pca9685PwmExtension` | 16-channel PWM controller via I2C |
| `RaspberryPiGpio` | Native GPIO pins |

### Control Types

| Type | Description |
|------|-------------|
| `Steering` | Servo steering control |
| `Throttle` | Motor/throttle control |
| `ServoControl` | General servo control |
| `GearControl` | Transmission control |
| `RotaryLights` | Rotating lights |

### Telemetry Types

| Type | Description |
|------|-------------|
| `CpuTemperatureReader` | CPU temperature |
| `ApplicationLifetimeReader` | App uptime |
| `JbdBmsTelemetryReader` | Battery BMS via UART |
