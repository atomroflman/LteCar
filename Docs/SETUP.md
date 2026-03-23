# LteCar Setup Tool

The LteCar Onboard software includes an interactive, menu-driven setup tool similar to `raspi-config`. It provides a user-friendly interface for configuring the vehicle.

## Starting the Setup Tool

```bash
cd Onboard
dotnet run -- setup
```

Or use the environment variable:

```bash
export CONFIG_DIR=/path/to/config
dotnet run -- setup
```

## Main Menu

```
┌─────────────────────────────────────────┐
│            LteCar Setup                │
│                                         │
│  1. System Options                      │
│  2. Network / Server                    │
│  3. Vehicle Configuration              │
│  4. Hardware Test                       │
│  5. Templates                           │
│  6. Feature Flags                       │
│  7. Update / Recovery                   │
│                                         │
│  < Finish and Reboot >                  │
│  < Finish without Reboot >              │
└─────────────────────────────────────────┘
```

---

## 1. System Options

Configure basic system settings:

| Option | Description |
|--------|-------------|
| S1. Hostname | Set the vehicle's hostname |
| S2. Memory Split | Configure GPU memory allocation |
| S3. SSH | Enable/disable SSH server |
| S4. Boot Options | Configure boot behavior |

---

## 2. Network / Server

Configure server connection:

| Option | Description |
|--------|-------------|
| N1. Server URL | Set the server URL (e.g., `https://lte-rc.example.com:5000`) |
| N2. Connection Test | Test connectivity to the server |
| N3. WiFi Settings | Configure WiFi connection |

### Setting Server URL

When prompted, enter the full URL including protocol and port:

```
Current server: https://lte-rc.northeurope.cloudapp.azure.com:5000
Enter server URL: [https://your-server:5000]
```

---

## 3. Vehicle Configuration

Manage vehicle settings and channel configuration:

| Option | Description |
|--------|-------------|
| V1. Vehicle Name | Set a friendly name for the vehicle |
| V2. Edit channelMap | View/edit channel mapping |
| V3. View Configuration | Display current configuration |
| V4. Reset to Defaults | Reset all configuration |

---

## 4. Hardware Test

Test connected hardware components:

| Option | Description |
|--------|-------------|
| H1. All Outputs | Test all hardware outputs |
| H2. Control Channels | Test steering, throttle, etc. |
| H3. Telemetry Sensors | Test sensor readings |
| H4. LEDs / Lights | Test lighting outputs |
| H5. Servos | Test servo motors |
| H6. Motors | Test motor controllers |

---

## 5. Templates

Manage vehicle configuration templates:

| Option | Description |
|--------|-------------|
| T1. List Templates | Show available templates |
| T2. Select Template | Apply a template to current config |
| T3. Create Template | Save current config as new template |
| T4. Delete Template | Remove a template |
| T5. Set Template Base Path | Configure where templates are stored |

### Template Storage

Templates are stored in `vehicleTemplates/` directory at:
- The path specified by `VEHICLE_TEMPLATES_PATH` environment variable
- Or `~/vehicleTemplates/` by default

Each template contains:
- `config.json` - ChannelMap configuration
- `metadata.json` - Template metadata (name, description, version)

---

## 6. Feature Flags

Enable or disable optional features:

| Feature | Description |
|---------|-------------|
| F1. Web Setup Interface | Enable web-based setup interface |
| F2. Bash Tool | Enable remote bash command execution |
| F3. Channel Tester | Enable channel testing interface |
| F4. Audio | Enable audio chat functionality |
| F5. Video | Enable video streaming |

Features are disabled by default. Enable them as needed.

---

## 7. Update / Recovery

System maintenance and recovery options:

| Option | Description |
|--------|-------------|
| U1. Check for Updates | Check for software updates |
| U2. Update Software | Update to latest version |
| U3. Backup Configuration | Backup current config |
| U4. Restore from Backup | Restore from backup |
| U5. Factory Reset | Reset to factory defaults |
| U6. View Logs | View system logs |

---

## Configuration Files

The setup tool manages these files in the config directory:

| File | Purpose |
|------|---------|
| `appSettings.json` | Vehicle settings, server URL, feature flags |
| `channelMap.json` | Hardware channels, telemetry, video streams |
| `.configdir` | Stores selected config directory path |

### Example appSettings.json

```json
{
  "carId": "vehicle-001",
  "carName": "My RC Car",
  "serverUrl": "https://lte-rc.example.com:5000",
  "webSetup": true,
  "bashTool": false,
  "channelTester": false,
  "audio": true,
  "video": true
}
```

---

## Command Line Options

```bash
# Start in setup mode
dotnet run -- setup

# With custom config directory
dotnet run -- --config-dir=/path/to/config setup

# Environment variable
CONFIG_DIR=/path/to/config dotnet run -- setup
```

---

## Config Directory Selection

On first run (or without `--config-dir`), the setup tool will:

1. Check for `CONFIG_DIR` environment variable
2. Check for existing `.configdir` file
3. Offer interactive selection
4. Persist selection in `.configdir`

The config directory contains:
- `appSettings.json`
- `channelMap.json`
- SSH keys
- Car identity key
