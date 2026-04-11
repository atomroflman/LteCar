# LteCar Installation Guide

## Prerequisites

- **Server**: Linux (Ubuntu 20.04+), .NET 8 SDK, PostgreSQL, Janus Gateway
- **Onboard (Vehicle)**: Raspberry Pi 3/4, .NET 8 SDK, Linux (Raspbian/Ubuntu)
- **Client**: Modern web browser (Chrome, Firefox, Edge)

## Server Installation

### Local Debug Setup

For local debugging, start PostgreSQL and Janus with Docker:

```bash
docker compose up -d postgres janus
```

Then run the server from `Server/` with the development launch profile. This keeps Janus external, so the server can also point to a remote Janus by changing `JanusConfiguration__HostName`.

If you use the root `docker-compose.yml`, it starts both services with the local defaults used by the debug profile.

### Full Container Stack

For a self-contained deployment, use the root Compose stack:

```bash
docker compose up --build
```

This starts nginx in front, plus client, server, Janus, and PostgreSQL.

All services use `restart: unless-stopped`, so they come back automatically after the container runtime restarts. For a host reboot, enable the container runtime service and start the stack once via systemd or a boot script.

Recommended boot-time setup on the server:

```bash
sudo systemctl enable --now podman
sudo systemctl enable --now ltecar-compose.service
```

Install `deploy/ltecar-compose.service` as `/etc/systemd/system/ltecar-compose.service` and adjust `WorkingDirectory` to your checkout or deployment path.

### Automatic HTTPS

If you install the server with `install.sh`, you can opt into automatic HTTPS via Caddy. The installer will ask for a public domain name and then configure Caddy as a reverse proxy in front of the local client container.

### 1. Clone Repository

```bash
git clone https://github.com/atomroflman/LteCar.git
cd LteCar
```

### 2. Install .NET 8

```bash
# Ubuntu/Debian
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0
```

### 3. Install PostgreSQL

```bash
sudo apt-get install -y postgresql postgresql-contrib
sudo systemctl start postgresql
sudo systemctl enable postgresql
```

### 4. Install Janus Gateway

```bash
# See Server/bash/install-janus.sh for detailed instructions
bash Server/bash/install-janus.sh
```

### 5. Configure Server

Edit `Server/appSettings.json`:

```json
{
  "ServerName": "your-server hostname",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=ltecar;Username=ltecar;Password=your-password"
  },
  "Application": {
    "RunJanusServer": true
  }
}
```

### 6. Database Migration

The server automatically runs migrations on startup. To manually apply:

```bash
cd Server
dotnet ef database update
```

### 7. Start Server

```bash
# Development
dotnet run

# Production (with Docker)
docker build -t ltecar-server -f server.dockerfile .
docker run -d -p 5000:5000 --name ltecar-server ltecar-server
```

Or use the provided scripts:

```bash
bash start-server.sh
```

### Local vs Remote Janus

- Local debug: `JanusConfiguration__HostName=localhost` and run `docker compose up -d janus`
- Remote Janus: leave `JanusConfiguration.HostName` pointed at the remote instance
- Do not enable `RunJanusServer` when Janus already runs in Docker or remotely

---

## Onboard (Vehicle) Installation

### Quick install from the server

If the server is already running, you can generate a preconfigured onboard installer directly from it and paste this on the vehicle:

```bash
curl -fsSL https://YOUR-SERVER/api/install/onboard.sh | sudo bash
```

The generated script pre-fills the normal `install.sh` with defaults for `onboard`, the server URL, the preferred branch, and optionally the current git ref. The normal installer still asks for these values interactively, runs the setup tool after the install, and does **not** start the onboard service immediately.

### 1. Clone Repository

```bash
git clone https://github.com/atomroflman/LteCar.git
cd LteCar
```

### 2. Install .NET 8 (Raspberry Pi)

```bash
# For Raspberry Pi OS/Ubuntu
wget https://packages.microsoft.com/config/debian/11/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0
```

### 3. Install I2C and GPIO Libraries

```bash
sudo apt-get install -y i2c-tools libi2c-dev pigpio python3-pigpio
sudo systemctl enable pigpio
sudo systemctl start pigpio
```

### 4. Connect Hardware

- **PCA9685 PWM Controller**: Connect via I2C (SDA/SCL)
- **Raspberry Pi GPIO**: For simple outputs
- **Camera**: Raspberry Pi Camera Module
- **Battery BMS**: JBD BMS via UART

### 5. Initial Setup

Run the interactive setup tool:

```bash
cd Onboard
dotnet run -- setup
```

This launches the raspi-config style setup menu. See [SETUP.md](SETUP.md) for details.

### 6. Configure Connection

In the Setup menu:
1. Go to **Network / Server** → **N1. Server URL**
2. Enter your server URL (e.g., `https://lte-rc.northeurope.cloudapp.azure.com:5000`)

### 7. Start Onboard Software

```bash
cd Onboard
dotnet run
```

---

## Config Directory

The Onboard client supports flexible config directory locations:

### Command Line

```bash
dotnet run -- --config-dir=/path/to/config
```

### Environment Variable

```bash
export CONFIG_DIR=/path/to/config
dotnet run
```

### Interactive Selection

If no config directory is specified, the setup tool will prompt for selection. The chosen path is persisted in `.configdir` file.

---

## Azure Firewall

If using Azure VM, open these ports:

| Port | Service |
|------|---------|
| 5000 | HTTPS Server |
| 8080 | SSH Key Download (Onboard) |
| 10001-10100 | Video Streams |
| 11001-11100 | Audio Streams |

```bash
az network nsg rule create \
  --resource-group myResourceGroup \
  --nsg-name myNsg \
  --name allow-https \
  --protocol tcp \
  --destination-port-range 5000 \
  --priority 100
```

---

## Troubleshooting

### Onboard won't connect to server

1. Check server URL is correct
2. Ensure server is running and accessible
3. Check firewall rules
4. Verify HTTPS certificate is valid

### Hardware not detected

1. Check I2C is enabled: `sudo raspi-config` → Interface Options → I2C
2. Verify connections: `i2cdetect -y 1`
3. Check GPIO permissions

### Database migration errors

Never manually modify the database. Always use EF Core migrations:

```bash
cd Server
dotnet ef migrations add AddNewFeature
dotnet ef database update
```
