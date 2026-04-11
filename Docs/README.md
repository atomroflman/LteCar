# LteCar Documentation

## Quick Links

- [Installation Guide](INSTALLATION.md) - Server and Onboard setup
- [Setup Tool](SETUP.md) - Menu-driven configuration
- [Features](FEATURES.md) - Feature overview and usage
- [Configuration Reference](CONFIGURATION.md) - Complete configuration reference

For the full container stack, see the installation guide. It covers the nginx + client + server + janus + postgres deployment and the reboot setup.

---

## System Overview

LteCar is a system for building and operating remotely controlled cars over LTE/Internet.

### Key Features

- **Quasi unlimited control channels** (steering, throttle, lights, sensors)
- **Real-time video streaming** from vehicle to web interface
- **Responsive remote control** over the internet
- **Multiple cars per server** - manage and control different vehicles simultaneously
- **Bidirectional audio chat** between driver and vehicle
- **Web interface** for control, video display, audio chat and configuration
- **Vehicle templates** for sharing vehicle configurations

### Architecture

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
│  ┌────────────┐  ┌────────────┐  ┌─────────────┐  │
│  │  Vehicle   │  │   Video    │  │    Audio    │  │
│  │ Connection │  │  Service   │  │    Chat     │  │
│  │  Manager   │  │            │  │             │  │
│  └────────────┘  └────────────┘  └─────────────┘  │
└────────────────────────────────────────────────────┘
```

---

## Important Notes

### LTE Connectivity

> The Onboard client initiates an **outbound-only connection** to the server. The vehicle **cannot be reached directly** from the internet - all communication is initiated by the vehicle. This works through NAT and most firewall configurations.

### Database Changes

> **Never modify the database manually.** Always use EF Core migrations.

### Onboard Client

> The Onboard client is **not reachable from the web** because of LTE's outbound-only connection. All features (bash tool, channel tester, etc.) work by the vehicle connecting to the server and the web client also connecting to the same server.

---

## Getting Started

1. [Install the server](INSTALLATION.md#server-installation)
2. [Install the onboard software](INSTALLATION.md#onboard-vehicle-installation)
3. [Configure using the setup tool](SETUP.md)
4. [Enable desired features](FEATURES.md#feature-flags-summary)

---

## Feature Flags

All optional features are **disabled by default**:

| Feature | Description |
|---------|-------------|
| `webSetup` | Web-based setup interface |
| `bashTool` | Remote bash command execution |
| `channelTester` | Web-based channel testing |
| `audio` | Audio chat functionality |
| `video` | Video streaming |

Enable via setup tool or `appSettings.json`.

---

## Directories

| Directory | Purpose |
|-----------|---------|
| `Server/` | ASP.NET Core server application |
| `Onboard/` | Raspberry Pi vehicle software |
| `Client/` | Web client application |
| `Shared/` | Shared libraries and SignalR contracts |
| `Docs/` | Documentation |
| `vehicleTemplates/` | Vehicle configuration templates |
