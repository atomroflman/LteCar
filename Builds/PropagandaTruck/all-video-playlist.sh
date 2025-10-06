#!/bin/bash

# Verzeichnisse
VIDEO_DIR="$HOME/Videos"
ls "$HOME/Video"
SOCKETS=("/tmp/mpvsocket0" "/tmp/mpvsocket1")

printf '{ "command": ["set_property", "audio-device", "alsa/hdmi:CARD=vc4hdmi0,DEV=0"] }\n' | socat - UNIX-CONNECT:/tmp/mpvsocket0

# 1. Volume auf 100 setzen
for sock in "${SOCKETS[@]}"; do
    printf '{ "command": ["set_property", "volume", 100] }\n' | socat - UNIX-CONNECT:"$sock"
done

# 2. Alle Videos in Playlist laden
for f in "$VIDEO_DIR"/*.{mp4,mkv,avi}; do
    # Prüfen, ob Datei existiert (falls kein Match)
    [ -e "$f" ] || continue
    for sock in "${SOCKETS[@]}"; do
        printf '{ "command": ["loadfile", "%s", "append"] }\n' "$f" | socat - UNIX-CONNECT:"$sock"
    done
done

# 3. Ab dem ersten Video abspielen
for sock in "${SOCKETS[@]}"; do
    printf '{ "command": ["playlist-play-index", 0] }\n' | socat - UNIX-CONNECT:"$sock"
done
