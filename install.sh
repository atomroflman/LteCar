#!/bin/bash
set -e

# ── Guard: must run via sudo, not as direct root login ──────────────
if [ "$EUID" -ne 0 ]; then
    echo "Please run with sudo:  sudo bash install.sh"
    exit 1
fi

if [ -z "$SUDO_USER" ] || [ "$SUDO_USER" = "root" ]; then
    echo "Do not run this script as the root user directly."
    echo "Please run with sudo from a regular user account:  sudo bash install.sh"
    exit 1
fi

RUN_USER="$SUDO_USER"
RUN_USER_HOME=$(eval echo "~$RUN_USER")
REPO_URL="https://github.com/atomroflman/LteCar.git"

# ── Helper: run a command as the real user ───────────────────────────
run_as_user() {
    sudo -u "$RUN_USER" --preserve-env=PATH,DOTNET_ROOT,HOME "$@"
}

# ── Repository ───────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

if [ -d "$SCRIPT_DIR/.git" ]; then
    REPO_DIR="$SCRIPT_DIR"
    CURRENT_BRANCH=$(git -C "$REPO_DIR" branch --show-current)
    echo "Repository found at $REPO_DIR (branch: $CURRENT_BRANCH)"
else
    echo "No repository found. Cloning LteCar ..."
    apt install -y git

    REPO_DIR="$RUN_USER_HOME/LteCar"

    echo ""
    echo "Available branches:"
    mapfile -t BRANCHES < <(git ls-remote --heads "$REPO_URL" | sed 's|.*refs/heads/||' | sort)

    DEFAULT_IDX=1
    for i in "${!BRANCHES[@]}"; do
        idx=$((i + 1))
        marker=""
        if [ "${BRANCHES[$i]}" = "master" ]; then
            DEFAULT_IDX=$idx
            marker=" (default)"
        fi
        echo "  $idx) ${BRANCHES[$i]}$marker"
    done

    echo ""
    read -rp "Choose branch [${DEFAULT_IDX}]: " BRANCH_INPUT
    BRANCH_INPUT="${BRANCH_INPUT:-$DEFAULT_IDX}"

    if [[ "$BRANCH_INPUT" =~ ^[0-9]+$ ]] && [ "$BRANCH_INPUT" -ge 1 ] && [ "$BRANCH_INPUT" -le "${#BRANCHES[@]}" ]; then
        BRANCH_CHOICE="${BRANCHES[$((BRANCH_INPUT - 1))]}"
    else
        BRANCH_CHOICE="$BRANCH_INPUT"
    fi

    run_as_user git clone -b "$BRANCH_CHOICE" "$REPO_URL" "$REPO_DIR"
    CURRENT_BRANCH="$BRANCH_CHOICE"
    echo "Cloned branch '$CURRENT_BRANCH' to $REPO_DIR"
fi

echo ""
echo "============================================"
echo "  LteCar Installer"
echo "============================================"
echo "  User  : $RUN_USER"
echo "  Home  : $RUN_USER_HOME"
echo "  Repo  : $REPO_DIR"
echo "  Branch: $CURRENT_BRANCH"
echo "============================================"
echo ""

# ── Mode selection ───────────────────────────────────────────────────
echo "What do you want to install?"
echo "  1) Server   (LteCar Server + Web-Client)"
echo "  2) Onboard  (Vehicle / Car client)"
echo ""
read -rp "Choose [1/2]: " INSTALL_MODE

case "$INSTALL_MODE" in
    1) INSTALL_MODE="server"  ;;
    2) INSTALL_MODE="onboard" ;;
    *)
        echo "Invalid choice. Exiting."
        exit 1
        ;;
esac
echo ""
echo ">> Installing mode: $INSTALL_MODE"

INSTALL_POSTGRES=false
if [ "$INSTALL_MODE" = "server" ]; then
    echo ""
    read -rp "Install a local PostgreSQL database? [Y/n]: " PG_CHOICE
    if [[ ! "${PG_CHOICE,,}" =~ ^n$ ]]; then
        INSTALL_POSTGRES=true
    fi
fi
echo ""

# =====================================================================
#  Phase 1 – System packages (requires root)
# =====================================================================
echo "── Phase 1: System packages ──────────────────────────"
apt update -y
apt upgrade -y
apt install -y git nano curl

if [ "$INSTALL_MODE" = "server" ]; then
    apt install -y nodejs npm \
        tcpdump build-essential automake libtool \
        pkg-config gengetopt gtk-doc-tools \
        libmicrohttpd-dev libjansson-dev libnice-dev \
        libssl-dev libsrtp2-dev libsofia-sip-ua-dev \
        libglib2.0-dev libopus-dev libogg-dev \
        libini-config-dev libcollection-dev \
        cmake libusrsctp-dev zlib1g-dev libconfig-dev libwebsockets-dev
fi

if [ "$INSTALL_POSTGRES" = true ]; then
    echo ""
    echo "Installing PostgreSQL ..."
    apt install -y postgresql postgresql-contrib

    echo "Configuring PostgreSQL database ..."
    PG_DB="ltecar"
    PG_USER="ltecar"
    PG_PASS="ltecar"

    if sudo -u postgres psql -tAc "SELECT 1 FROM pg_roles WHERE rolname='$PG_USER'" | grep -q 1; then
        echo "PostgreSQL user '$PG_USER' already exists."
    else
        sudo -u postgres psql -c "CREATE USER $PG_USER WITH PASSWORD '$PG_PASS';"
        echo "Created PostgreSQL user '$PG_USER'."
    fi

    if sudo -u postgres psql -tAc "SELECT 1 FROM pg_database WHERE datname='$PG_DB'" | grep -q 1; then
        echo "PostgreSQL database '$PG_DB' already exists."
    else
        sudo -u postgres psql -c "CREATE DATABASE $PG_DB OWNER $PG_USER;"
        echo "Created PostgreSQL database '$PG_DB'."
    fi

    echo "PostgreSQL ready: $PG_USER@localhost/$PG_DB"
fi

if [ "$INSTALL_MODE" = "onboard" ]; then
    apt install -y \
        libcamera-apps gstreamer1.0-tools gstreamer1.0-plugins-base \
        gstreamer1.0-plugins-good gstreamer1.0-plugins-bad gstreamer1.0-plugins-ugly \
        gstreamer1.0-libav gstreamer1.0-rtsp gstreamer1.0-webrtc vnstat \
        libmicrohttpd-dev libjansson-dev libssl-dev libsrtp2-dev \
        libsofia-sip-ua-dev libglib2.0-dev libopus-dev libogg-dev \
        libini-config-dev libcollection-dev libconfig-dev pkg-config \
        gengetopt libtool automake cmake build-essential \
        libnice-dev libcurl4-openssl-dev liblua5.3-dev libnanomsg-dev \
        libwebsockets-dev libevent-dev
fi

# =====================================================================
#  Phase 2 – .NET SDK (installed as regular user)
# =====================================================================
echo ""
echo "── Phase 2: .NET SDK ─────────────────────────────────"

export DOTNET_ROOT="$RUN_USER_HOME/.dotnet"
export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"

if [ -x "$DOTNET_ROOT/dotnet" ]; then
    echo ".NET SDK already installed at $DOTNET_ROOT"
    run_as_user "$DOTNET_ROOT/dotnet" --info | head -3
else
    echo "Installing .NET SDK 8.0 for user $RUN_USER ..."
    run_as_user bash -c 'curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0'
fi

if ! grep -q 'DOTNET_ROOT' "$RUN_USER_HOME/.bashrc"; then
    run_as_user bash -c "echo 'export DOTNET_ROOT=\$HOME/.dotnet' >> \$HOME/.bashrc"
    run_as_user bash -c "echo 'export PATH=\$PATH:\$HOME/.dotnet:\$HOME/.dotnet/tools' >> \$HOME/.bashrc"
    echo "Added DOTNET_ROOT and PATH to $RUN_USER_HOME/.bashrc"
fi

echo ".NET SDK ready: $(run_as_user "$DOTNET_ROOT/dotnet" --version)"

# =====================================================================
#  Phase 3 – Build (as regular user)
# =====================================================================
echo ""
echo "── Phase 3: Build ────────────────────────────────────"

if [ "$INSTALL_MODE" = "server" ]; then
    # ── Janus Gateway ────────────────────────────────────
    echo "Building Janus Gateway ..."
    bash "$REPO_DIR/Server/bash/install-janus.sh"

    # ── Web Client (Next.js) ─────────────────────────────
    echo ""
    echo "Building Web Client ..."
    cd "$REPO_DIR/Client"
    run_as_user npm install
    run_as_user npm run build
    echo "Client build completed."

    # ── .NET Server ──────────────────────────────────────
    echo ""
    echo "Building .NET Server ..."
    cd "$REPO_DIR/Server"
    run_as_user "$DOTNET_ROOT/dotnet" build -c Release
fi

if [ "$INSTALL_MODE" = "onboard" ]; then
    # ── WiringPi ─────────────────────────────────────────
    WIRINGPI_DIR="$RUN_USER_HOME/WiringPi"
    if [ -d "$WIRINGPI_DIR" ]; then
        echo "WiringPi already present at $WIRINGPI_DIR, skipping."
    else
        echo "Building WiringPi ..."
        run_as_user git clone https://github.com/WiringPi/WiringPi.git "$WIRINGPI_DIR"
        cd "$WIRINGPI_DIR"
        ./build
    fi

    # ── .NET Onboard Client ──────────────────────────────
    echo ""
    echo "Building .NET Onboard Client ..."
    cd "$REPO_DIR/Onboard"
    run_as_user "$DOTNET_ROOT/dotnet" publish LteCar.Onboard.csproj -c Release
fi

# =====================================================================
#  Phase 4 – systemd services (requires root for install, runs as user)
# =====================================================================
echo ""
read -rp "Install as systemd services? [y/N]: " INSTALL_SERVICES
if [[ ! "${INSTALL_SERVICES,,}" =~ ^(y|j)$ ]]; then
    echo "Service installation skipped."
    echo ""
    echo "Installation finished. You can start manually:"
    if [ "$INSTALL_MODE" = "server" ]; then
        echo "  cd $REPO_DIR/Server && dotnet run -c Release"
    else
        echo "  cd $REPO_DIR/Onboard && dotnet run"
    fi
    exit 0
fi

echo ""
echo "── Phase 4: systemd services ─────────────────────────"

LOG_DIR="/var/log/ltecar"
mkdir -p "$LOG_DIR"
chown "$RUN_USER:$RUN_USER" "$LOG_DIR"

if [ "$INSTALL_MODE" = "server" ]; then
    # ── ltecar-server service ────────────────────────────
    SERVER_EXEC="$REPO_DIR/Server/bin/Release/net8.0/LteCar.Server"
    if [ ! -f "$SERVER_EXEC" ]; then
        echo "Error: Server executable not found at $SERVER_EXEC"
        exit 1
    fi
    chmod +x "$SERVER_EXEC"

    cat > /etc/systemd/system/ltecar-server.service <<EOF
[Unit]
Description=LteCar .NET Server
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
ExecStart=$SERVER_EXEC
Restart=on-failure
RestartSec=5
User=$RUN_USER
WorkingDirectory=$REPO_DIR/Server
Environment=DOTNET_ROOT=$DOTNET_ROOT
Environment=PATH=$DOTNET_ROOT:$DOTNET_ROOT/tools:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin
StandardOutput=append:$LOG_DIR/server.log
StandardError=append:$LOG_DIR/server.err

[Install]
WantedBy=multi-user.target
EOF

    # ── ltecar-client service ────────────────────────────
    CLIENT_SCRIPT="$REPO_DIR/Client/out/standalone/server.js"
    if [ ! -f "$CLIENT_SCRIPT" ]; then
        echo "Error: Client script not found at $CLIENT_SCRIPT"
        exit 1
    fi

    cat > /etc/systemd/system/ltecar-client.service <<EOF
[Unit]
Description=LteCar Web Client (Next.js)
After=network-online.target ltecar-server.service
Wants=network-online.target

[Service]
Type=simple
ExecStart=/usr/bin/node $CLIENT_SCRIPT
Restart=on-failure
RestartSec=5
User=$RUN_USER
WorkingDirectory=$REPO_DIR/Client
StandardOutput=append:$LOG_DIR/client.log
StandardError=append:$LOG_DIR/client.err

[Install]
WantedBy=multi-user.target
EOF

    systemctl daemon-reload
    systemctl enable --now ltecar-server.service
    systemctl enable --now ltecar-client.service
    echo ""
    systemctl status ltecar-server.service --no-pager || true
    systemctl status ltecar-client.service --no-pager || true
fi

if [ "$INSTALL_MODE" = "onboard" ]; then
    ONBOARD_DLL="$REPO_DIR/Onboard/bin/Release/net8.0/publish/LteCar.Onboard.dll"
    if [ ! -f "$ONBOARD_DLL" ]; then
        echo "Error: Onboard DLL not found at $ONBOARD_DLL"
        exit 1
    fi

    cat > /etc/systemd/system/ltecar-onboard.service <<EOF
[Unit]
Description=LteCar Onboard Client
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
ExecStart=$DOTNET_ROOT/dotnet $ONBOARD_DLL
Restart=always
RestartSec=5
User=$RUN_USER
WorkingDirectory=$REPO_DIR/Onboard
Environment=DOTNET_ROOT=$DOTNET_ROOT
Environment=DOTNET_ENVIRONMENT=Production
Environment=PATH=$DOTNET_ROOT:$DOTNET_ROOT/tools:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin
StandardOutput=append:$LOG_DIR/onboard.log
StandardError=append:$LOG_DIR/onboard.err

[Install]
WantedBy=multi-user.target
EOF

    systemctl daemon-reload
    systemctl enable --now ltecar-onboard.service
    echo ""
    systemctl status ltecar-onboard.service --no-pager || true
fi

echo ""
echo "============================================"
echo "  Installation complete!"
echo "============================================"
echo "  Logs: $LOG_DIR/"
echo "  Manage:  sudo systemctl {start|stop|status} ltecar-*.service"
echo "============================================"
