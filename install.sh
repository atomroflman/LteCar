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

# ── Package manager detection ────────────────────────────────────────
if command -v apt-get &>/dev/null; then
    APT="apt-get"
elif command -v apt &>/dev/null; then
    APT="apt"
else
    echo "Error: neither apt-get nor apt found. This installer requires a Debian/Ubuntu-based system."
    exit 1
fi

pkg_install() {
    "$APT" install -y "$@"
}

pkg_update() {
    "$APT" update -y
}

# ── Helper: run a command as the real user ───────────────────────────
run_as_user() {
    sudo -u "$RUN_USER" --preserve-env=PATH,HOME "$@"
}

# ── Repository ───────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

if [ -d "$SCRIPT_DIR/.git" ]; then
    REPO_DIR="$SCRIPT_DIR"
    CURRENT_BRANCH=$(git -C "$REPO_DIR" branch --show-current)
    echo "Repository found at $REPO_DIR (branch: $CURRENT_BRANCH)"
else
    echo "No repository found. Cloning LteCar ..."
    pkg_install git

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

    if [ -d "$REPO_DIR" ] && [ "$(ls -A "$REPO_DIR")" ]; then
        echo "Target directory $REPO_DIR already exists and is not empty. Skipping clone."
        CURRENT_BRANCH="$BRANCH_CHOICE"
    else
        run_as_user git clone -b "$BRANCH_CHOICE" "$REPO_URL" "$REPO_DIR"
        CURRENT_BRANCH="$BRANCH_CHOICE"
        echo "Cloned branch '$CURRENT_BRANCH' to $REPO_DIR"
    fi
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

# ── Deployment mode selection ────────────────────────────────────────
echo "What do you want to install?"
echo "  1) Server   (Compose stack: client + server + nginx + janus + postgres)"
echo "  2) Onboard  (Bare metal: vehicle / car client for Raspberry Pi)"
echo ""
read -rp "Choose [1/2]: " DEPLOY_MODE

case "$DEPLOY_MODE" in
    1) DEPLOY_MODE="server" ;;
    2) DEPLOY_MODE="onboard" ;;
    *)
        echo "Invalid choice. Exiting."
        exit 1
        ;;
esac

echo ""

# =====================================================================
#  SERVER – Compose stack
# =====================================================================
if [ "$DEPLOY_MODE" = "server" ]; then

    # ── Container engine selection ───────────────────────────────────
    echo "Which container engine do you want to use?"
    echo "  1) Docker"
    echo "  2) Podman"
    echo ""
    read -rp "Choose [1/2]: " ENGINE_CHOICE

    case "$ENGINE_CHOICE" in
        1) COMPOSE_ENGINE="docker" ;;
        2) COMPOSE_ENGINE="podman" ;;
        *)
            echo "Invalid choice. Exiting."
            exit 1
            ;;
    esac

    # ── Compose stack selection ──────────────────────────────────────
    echo ""
    echo "Which compose stack do you want to deploy?"
    echo "  1) Full stack (client + server + nginx + janus + postgres)"
    echo "  2) Local debug support only (postgres + janus)"
    echo ""
    read -rp "Choose [1/2]: " STACK_CHOICE

    case "$STACK_CHOICE" in
        1) INSTALL_MODE="compose-full" ;;
        2) INSTALL_MODE="compose-debug" ;;
        *)
            echo "Invalid choice. Exiting."
            exit 1
            ;;
    esac

    echo ""
    echo ">> Engine : $COMPOSE_ENGINE"
    echo ">> Stack  : $INSTALL_MODE"
    echo ""

    # ── Phase 1: System packages ─────────────────────────────────────
    echo "── Phase 1: System packages ──────────────────────────"
    pkg_update
    pkg_install git curl

    if [ "$COMPOSE_ENGINE" = "docker" ]; then
        if ! command -v docker &>/dev/null; then
            echo "Installing Docker ..."
            pkg_install docker.io docker-compose-plugin
            usermod -aG docker "$RUN_USER"
            echo "User '$RUN_USER' added to the docker group."
        else
            echo "Docker already installed: $(docker --version)"
            if ! id -nG "$RUN_USER" | grep -qw docker; then
                usermod -aG docker "$RUN_USER"
                echo "User '$RUN_USER' added to the docker group."
            fi
        fi
        COMPOSE_CMD="docker compose"
    else
        if ! command -v podman &>/dev/null; then
            echo "Installing Podman ..."
            pkg_install podman podman-compose
        else
            echo "Podman already installed: $(podman --version)"
        fi
        COMPOSE_CMD="podman compose"
    fi

    # ── Phase 2: Compose stack ───────────────────────────────────────
    echo ""
    echo "── Phase 2: Compose stack ────────────────────────────"

    if [ "$INSTALL_MODE" = "compose-full" ]; then
        COMPOSE_FILE="$REPO_DIR/docker-compose.yml"
    elif [ "$INSTALL_MODE" = "compose-debug" ]; then
        COMPOSE_FILE="$REPO_DIR/docker-compose.debug.yml"
    fi

    if [ ! -f "$COMPOSE_FILE" ]; then
        echo "Error: Compose file not found at $COMPOSE_FILE"
        exit 1
    fi

    echo "Compose file: $COMPOSE_FILE"
    echo "Pulling/building images ..."
    run_as_user $COMPOSE_CMD -f "$COMPOSE_FILE" pull --ignore-buildable
    run_as_user $COMPOSE_CMD -f "$COMPOSE_FILE" build

    # ── Phase 3: systemd service (optional) ─────────────────────────
    echo ""
    read -rp "Install as systemd autostart service? [y/N]: " INSTALL_SERVICES
    if [[ ! "${INSTALL_SERVICES,,}" =~ ^(y|j)$ ]]; then
        echo ""
        echo "Autostart skipped. Start the stack manually:"
        echo "  $COMPOSE_CMD -f $COMPOSE_FILE up -d"
        echo ""
        echo "============================================"
        echo "  Installation complete!"
        echo "============================================"
        exit 0
    fi

    echo ""
    echo "── Phase 3: systemd service ──────────────────────────"

    if [ "$COMPOSE_ENGINE" = "docker" ]; then
        ENGINE_BIN="/usr/bin/docker"
        AFTER_TARGET="docker.service"
    else
        ENGINE_BIN="/usr/bin/podman"
        AFTER_TARGET="podman.service"
    fi

    cat > /etc/systemd/system/ltecar.service <<EOF
[Unit]
Description=LteCar compose stack
Wants=network-online.target
After=network-online.target $AFTER_TARGET

[Service]
Type=oneshot
RemainAfterExit=yes
User=$RUN_USER
WorkingDirectory=$REPO_DIR
ExecStart=$ENGINE_BIN compose -f $COMPOSE_FILE up -d
ExecStop=$ENGINE_BIN compose -f $COMPOSE_FILE down
TimeoutStartSec=0

[Install]
WantedBy=multi-user.target
EOF

    systemctl daemon-reload
    systemctl enable --now ltecar.service

    echo ""
    systemctl status ltecar.service --no-pager || true

    echo ""
    echo "============================================"
    echo "  Installation complete!"
    echo "============================================"
    echo "  Manage:  sudo systemctl {start|stop|restart|status} ltecar.service"
    echo "  Logs  :  $COMPOSE_CMD -f $COMPOSE_FILE logs -f"
    echo "============================================"
    exit 0
fi

# =====================================================================
#  ONBOARD – Bare metal (Raspberry Pi)
# =====================================================================
if [ "$DEPLOY_MODE" = "onboard" ]; then

    echo ">> Installing Onboard (bare metal) ..."
    echo ""

    # ── Phase 1: System packages ─────────────────────────────────────
    echo "── Phase 1: System packages ──────────────────────────"
    pkg_update
    pkg_install \
        git curl \
        ffmpeg \
        libcamera0 libcamera-tools \
        i2c-tools \
        wiringpi

    # ── Phase 2: mediamtx ───────────────────────────────────────────
    echo ""
    echo "── Phase 2: mediamtx ─────────────────────────────────"

    MEDIAMTX_VERSION="v1.17.1"
    EXTERN_DIR="$REPO_DIR/Onboard/Extern"
    MEDIAMTX_DEST="$EXTERN_DIR/mediamtx"

    # Detect architecture
    ARCH=$(uname -m)
    case "$ARCH" in
        aarch64)        MEDIAMTX_ARCH="linux_arm64" ;;
        armv7l|armv7)   MEDIAMTX_ARCH="linux_armv7" ;;
        armv6l)         MEDIAMTX_ARCH="linux_armv6" ;;
        x86_64)         MEDIAMTX_ARCH="linux_amd64" ;;
        *)
            echo "Unsupported architecture: $ARCH"
            exit 1
            ;;
    esac

    MEDIAMTX_TARBALL="mediamtx_${MEDIAMTX_VERSION}_${MEDIAMTX_ARCH}.tar.gz"
    MEDIAMTX_URL="https://github.com/bluenviron/mediamtx/releases/download/${MEDIAMTX_VERSION}/${MEDIAMTX_TARBALL}"

    echo "Architecture : $ARCH -> $MEDIAMTX_ARCH"
    echo "Version      : $MEDIAMTX_VERSION"
    echo "Downloading  : $MEDIAMTX_URL"

    TMP_DIR=$(mktemp -d)
    curl -fsSL "$MEDIAMTX_URL" -o "$TMP_DIR/$MEDIAMTX_TARBALL"
    tar -xzf "$TMP_DIR/$MEDIAMTX_TARBALL" -C "$TMP_DIR" mediamtx
    install -m 755 "$TMP_DIR/mediamtx" "$MEDIAMTX_DEST"
    chown "$RUN_USER:$RUN_USER" "$MEDIAMTX_DEST"
    rm -rf "$TMP_DIR"

    echo "mediamtx installed to $MEDIAMTX_DEST"

    # ── Phase 3: .NET SDK ────────────────────────────────────────────
    echo ""
    echo "── Phase 3: .NET SDK ─────────────────────────────────"

    DOTNET_INSTALL_SCRIPT="/tmp/dotnet-install.sh"
    curl -fsSL https://dot.net/v1/dotnet-install.sh -o "$DOTNET_INSTALL_SCRIPT"
    chmod +x "$DOTNET_INSTALL_SCRIPT"
    run_as_user "$DOTNET_INSTALL_SCRIPT" --channel 10.0
    rm -f "$DOTNET_INSTALL_SCRIPT"

    # Resolve DOTNET_ROOT for the user
    DOTNET_ROOT="$RUN_USER_HOME/.dotnet"

    # ── Phase 4: Build ───────────────────────────────────────────────
    echo ""
    echo "── Phase 4: Build ────────────────────────────────────"

    echo "Building .NET Onboard Client ..."
    run_as_user env DOTNET_ROOT="$DOTNET_ROOT" PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH" \
        "$DOTNET_ROOT/dotnet" publish "$REPO_DIR/Onboard/LteCar.Onboard.csproj" -c Release

    # ── Phase 5: systemd service (optional) ─────────────────────────
    echo ""
    read -rp "Install as systemd autostart service? [y/N]: " INSTALL_SERVICES
    if [[ ! "${INSTALL_SERVICES,,}" =~ ^(y|j)$ ]]; then
        echo ""
        echo "Autostart skipped. Start manually:"
        echo "  cd $REPO_DIR/Onboard && $DOTNET_ROOT/dotnet run -c Release"
        echo ""
        echo "============================================"
        echo "  Installation complete!"
        echo "============================================"
        exit 0
    fi

    echo ""
    echo "── Phase 5: systemd service ──────────────────────────"

    LOG_DIR="/var/log/ltecar"
    mkdir -p "$LOG_DIR"
    chown "$RUN_USER:$RUN_USER" "$LOG_DIR"

    ONBOARD_DLL="$REPO_DIR/Onboard/bin/Release/net10.0/publish/LteCar.Onboard.dll"
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

    echo ""
    echo "============================================"
    echo "  Installation complete!"
    echo "============================================"
    echo "  Logs  :  $LOG_DIR/onboard.log"
    echo "  Manage:  sudo systemctl {start|stop|restart|status} ltecar-onboard.service"
    echo "============================================"
    exit 0
fi
