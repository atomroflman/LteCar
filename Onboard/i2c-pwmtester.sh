#!/bin/bash

ADDR=0x40
PRESCALE=0x79  # Für 50 Hz
rundum_channel=15

# Anzahl der verfügbaren Modi (z.B. 8 für 8 Effekte)
modi_count=10

# PWM-Impulse für Taste 5 (2.5ms) und Taste 3 (1.5ms)
taste5_ticks=$(printf '%d' $(echo "4096 * 2.5 / 20" | bc -l))
taste3_ticks=$(printf '%d' $(echo "4096 * 1.5 / 20" | bc -l))

set_rundum_pwm() {
  local pulse=$1
  reg_base=$((0x06 + 4 * rundum_channel))
  on_l=$(printf "0x%02X" $reg_base)
  on_h=$(printf "0x%02X" $((reg_base + 1)))
  off_l=$(printf "0x%02X" $((reg_base + 2)))
  off_h=$(printf "0x%02X" $((reg_base + 3)))
  lsb=$(printf "0x%02X" $((pulse & 0xFF)))
  msb=$(printf "0x%02X" $((pulse >> 8)))
  echo "Setze PWM: $pulse Ticks auf Kanal $rundum_channel"
  i2cset -y 1 $ADDR $on_l 0x00
  i2cset -y 1 $ADDR $on_h 0x00
  i2cset -y 1 $ADDR $off_l $lsb
  i2cset -y 1 $ADDR $off_h $msb
}

# Frequenz einmalig setzen
i2cset -y 1 $ADDR 0x00 0x10       # Sleep
i2cset -y 1 $ADDR 0xFE $PRESCALE  # Set prescale
i2cset -y 1 $ADDR 0x00 0x20       # Wake + Auto-Inc

# UI
clear
echo "🔦 Rundumlicht-Modus auf Kanal $rundum_channel"
echo "-------------------------"
echo "Tasten 1-0: Modus direkt wählen"
echo "Q: Beenden"
echo "Verfügbare Modi: $modi_count"
echo "Moduswahl: Taste 1-0 (z.B. 3 für Modus 3)"

# Merker für aktuellen Modus
current_modus=0
switchMode=1

# Funktion zum Blättern zu Modus n
    set_rundum_pwm $taste3_ticks
set_modus() {
  local ziel=$1
  local diff=$((ziel - current_modus))
  if (( diff < 0 )); then
    diff=$((modi_count + diff))
  fi
  echo "Schalte von Modus $current_modus zu $ziel (Taste 5 und 3 $diff mal)"
  for ((i=0; i<diff; i++)); do
    switchNext
    sleep 0.005
  done
  current_modus=$ziel
}

switchNext() {
  if (( switchMode % 2 == 0 )); then
    set_rundum_pwm $taste5_ticks
  else
    set_rundum_pwm $taste3_ticks
  fi
  switchMode=$((switchMode + 1))
}

stty -echo -icanon time 0 min 0
while true; do
  read -rsn1 key
  if [[ $key =~ ^[0-9]$ ]]; then
    ziel=$key
    set_modus $ziel
  elif [[ $key == "a" ]]; then
    set_modus 10
  elif [[ $key == "b" ]]; then
    set_modus 11
  elif [[ $key == "q" ]]; then
    break
  fi
  sleep 0.05
done
stty sane
echo -e "\nRundumlicht-Modus beendet."
