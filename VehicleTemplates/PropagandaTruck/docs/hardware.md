# PropagandaTruck Dokumentation

## Hardware-Spezifikationen

### Servo-Motoren
- **Steering**: SG90 Micro Servo, 180° Rotation
- **Throttle**: Continuous Rotation Servo
- **Camera Pan**: SG90 Micro Servo
- **Camera Tilt**: SG90 Micro Servo

### Sensoren
- **GPS**: NEO-8M GPS Module
- **IMU**: MPU-6050 6-Axis Gyroscope + Accelerometer
- **Battery**: INA219 Current/Voltage Sensor

### Kamera
- **Type**: Raspberry Pi Camera Module v2
- **Resolution**: 1920x1080
- **Stream Port**: 8080

## Verdrahtung

### PCA9685 PWM Controller
```
Pin 0: Steering Servo (Orange Wire)
Pin 1: Throttle Servo (Orange Wire)  
Pin 2: Camera Pan Servo (Orange Wire)
Pin 3: Camera Tilt Servo (Orange Wire)
```

### I2C Verbindungen
```
SDA: GPIO 2 (Pin 3)
SCL: GPIO 3 (Pin 5)
```

## Kalibrierung

### Servo-Kalibrierung
1. Neutralposition bestimmen
2. Min/Max Werte einstellen
3. Deadband konfigurieren

### Kamera-Kalibrierung
1. Fokus einstellen
2. Belichtung optimieren
3. Stream-Qualität testen