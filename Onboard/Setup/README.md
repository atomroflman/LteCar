# LTE Car Vehicle Setup Tool

Ein interaktives Setup-Tool zur Konfiguration von LTE Car Fahrzeugen.

## Features

🚗 **Fahrzeug-Grundeinstellungen**
- Car ID und Name konfigurieren
- Fahrzeug-Passwort setzen
- Grundlegende Identifikation

🔧 **Hardware-Konfiguration**
- Pin-Manager (PCA9685, Raspberry Pi GPIO) einrichten
- Hardware-Adressen und I2C-Konfiguration
- Flexible Hardware-Unterstützung

🎮 **Control-Channels**
- Lenkung (Steering) konfigurieren
- Antrieb (Throttle) einrichten
- Beleuchtung und Schaltung
- Hardware-Zuordnung mit Adressen

📊 **Telemetrie-Channels**
- CPU-Temperatur Monitoring
- Battery Management System (JBD BMS)
- Anwendungs-Lebensdauer Tracking
- Flexible Lese-Intervalle

📹 **Video-Streams**
- Kamera-Streams konfigurieren
- Position und Priorität festlegen
- Video-Stream Metadaten

🌐 **Server-Verbindung**
- Server-URL konfigurieren
- API-Schlüssel verwalten
- Verbindungsparameter

📑 **Fahrzeug-Vorlagen**
- EMO NT6 RC Car Template
- Basic Car Konfiguration
- Advanced Car mit allen Features
- Einfache Template-Anwendung

## Verwendung

### Über das Setup-Script (Empfohlen)
```bash
cd Onboard
./setup-vehicle.sh
```

### Direkt über .NET CLI
```bash
cd Onboard
dotnet run -- setup
```

### Über das kompilierte Binary
```bash
cd Onboard
dotnet build
./bin/Debug/net8.0/LteCar.Onboard setup
```

## Konfigurationsdateien

Das Setup-Tool erstellt und bearbeitet folgende Dateien:

### `channelMap.json`
Enthält die Hardware- und Channel-Konfiguration:
- Pin-Manager Definitionen
- Control-Channel Zuordnungen
- Telemetrie-Channel Konfiguration
- Video-Stream Metadaten

### `appSettings.json`
Enthält die Fahrzeug-Grundeinstellungen:
- Car ID und Name
- Passwort-Hash
- Server-Verbindungsparameter
- API-Konfiguration

## Beispiel-Konfiguration (EMO NT6)

```json
{
  "pinManagers": {
    "Pca9685": {
      "type": "Pca9685PwmExtension",
      "options": {
        "boardAddress": 0,
        "i2cBus": 1
      }
    }
  },
  "controlChannels": {
    "steer": {
      "address": 12,
      "controlType": "Steering",
      "pinManager": "Pca9685"
    },
    "throttle": {
      "address": 33,
      "controlType": "Throttle",
      "pinManager": "Pca9685",
      "testDisabled": true
    }
  },
  "videoStreams": {
    "mainCamera": {
      "name": "Main Camera",
      "description": "Primary vehicle camera",
      "type": "camera",
      "location": "front",
      "enabled": true,
      "priority": 1
    }
  }
}
```

## Templates

### EMO NT6 RC Car
- Standard RC-Auto Konfiguration
- PCA9685 PWM Controller
- Lenkung, Antrieb, Beleuchtung
- Haupt-Kamera

### Basic Car
- Einfache Raspberry Pi GPIO Konfiguration
- Motor und Servo Control
- Minimale Telemetrie

### Advanced Car
- Vollständige Konfiguration
- Erweiterte Telemetrie (CPU, Battery)
- Mehrere Kameras
- Alle Control-Optionen

## Hardware-Unterstützung

### Pin-Manager
- **PCA9685**: 16-Kanal PWM Controller (I2C)
- **Raspberry Pi GPIO**: Native GPIO Pins

### Control-Types
- **Steering**: Lenkung (Servo)
- **Throttle**: Antrieb (Motor/ESC)
- **ServoControl**: Allgemeine Servo-Steuerung
- **GearControl**: Schaltung
- **RotaryLights**: Drehbare Beleuchtung

### Telemetrie-Types
- **CpuTemperatureReader**: CPU-Temperatur
- **ApplicationLifetimeReader**: Anwendungs-Status
- **JbdBmsTelemetryReader**: Battery Management

## Erweiterte Nutzung

### Eigene Templates erstellen
Bearbeiten Sie `VehicleTemplateManager.cs` um eigene Fahrzeug-Templates zu erstellen.

### Custom Control-Types
Entwickeln Sie eigene Control-Types in `Onboard/Control/ControlTypes/`

### Eigene Telemetrie
Implementieren Sie `TelemetryReaderBase` für spezielle Sensoren

## Fehlerbehebung

### Setup startet nicht
- Überprüfen Sie, ob .NET 8.0+ installiert ist
- Stellen Sie sicher, dass Sie im `Onboard/` Verzeichnis sind
- Bauen Sie das Projekt neu: `dotnet build`

### Hardware wird nicht erkannt
- Überprüfen Sie I2C-Verbindungen (für PCA9685)
- Testen Sie GPIO-Pins einzeln
- Prüfen Sie Hardware-Adressen in der Konfiguration

### Server-Verbindung fehlschlägt
- Überprüfen Sie die Server-URL
- Validieren Sie API-Schlüssel
- Testen Sie Netzwerk-Konnektivität

## Support

Bei Problemen:
1. Überprüfen Sie die Log-Ausgaben
2. Validieren Sie die Konfigurationsdateien
3. Testen Sie Hardware-Komponenten einzeln
4. Verwenden Sie die Debug-Modi für detaillierte Ausgaben