#!/bin/bash

BUS=1           # I2C Bus
ADDR=0x40       # PCA9685 Adresse
MIN=205         # Ca. 1ms Pulsweite
MAX=410         # Ca. 2ms Pulsweite

echo "Setting all 16 PWM channels to MIN ($MIN)..."

for CH in {0..15}; do
  BASE=$((0x06 + 4 * CH))
  MIN_L=$((MIN & 0xFF))
  MIN_H=$((MIN >> 8))
  i2cset -y $BUS $ADDR $BASE     0x00
  i2cset -y $BUS $ADDR $((BASE+1)) 0x00
  i2cset -y $BUS $ADDR $((BASE+2)) $(printf "0x%02X" $MIN_L)
  i2cset -y $BUS $ADDR $((BASE+3)) $(printf "0x%02X" $MIN_H)
done

sleep 1

echo "Setting all 16 PWM channels to MAX ($MAX)..."

for CH in {0..15}; do
  BASE=$((0x06 + 4 * CH))
  MAX_L=$((MAX & 0xFF))
  MAX_H=$((MAX >> 8))
  i2cset -y $BUS $ADDR $BASE     0x00
  i2cset -y $BUS $ADDR $((BASE+1)) 0x00
  i2cset -y $BUS $ADDR $((BASE+2)) $(printf "0x%02X" $MAX_L)
  i2cset -y $BUS $ADDR $((BASE+3)) $(printf "0x%02X" $MAX_H)
done

echo "Done."
