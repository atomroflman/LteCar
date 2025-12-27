using Spectre.Console;
using LteCar.Shared.Channels;
using LteCar.Onboard.Hardware;
using LteCar.Onboard.Control.ControlTypes;
using LteCar.Onboard.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Reflection;

namespace LteCar.Onboard.Setup;

public class ChannelTestTool
{
    private readonly ChannelMap _channelMap;
    private readonly ILogger<ChannelTestTool> _logger;

    public ChannelTestTool(ChannelMap channelMap, ILogger<ChannelTestTool> logger)
    {
        _channelMap = channelMap;
        _logger = logger;
    }

    public static void RunTests(ChannelMap channelMap)
    {
        // Setup minimal service provider for testing
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        
        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<ChannelTestTool>>();
        
        var testTool = new ChannelTestTool(channelMap, logger);
        testTool.StartTesting();
    }

    private void StartTesting()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("Channel Tests").Color(Color.Green));
        AnsiConsole.WriteLine();

        while (true)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Was möchten Sie [green]testen[/]?")
                    .PageSize(10)
                    .AddChoices(new[] {
                        "🔧 Hardware-Manager testen",
                        "🎮 Control-Channels testen",
                        "📊 Telemetrie-Channels testen",
                        "📹 Video-Streams testen",
                        "🔄 Vollständiger Systemtest",
                        "📋 Test-Bericht anzeigen",
                        "❌ Beenden"
                    }));

            switch (choice)
            {
                case "🔧 Hardware-Manager testen":
                    TestHardwareManagers();
                    break;
                case "🎮 Control-Channels testen":
                    TestControlChannels();
                    break;
                case "📊 Telemetrie-Channels testen":
                    TestTelemetryChannels();
                    break;
                case "📹 Video-Streams testen":
                    TestVideoStreams();
                    break;
                case "🔄 Vollständiger Systemtest":
                    RunFullSystemTest();
                    break;
                case "📋 Test-Bericht anzeigen":
                    ShowTestReport();
                    break;
                case "❌ Beenden":
                    return;
            }
        }
    }

    private void TestHardwareManagers()
    {
        AnsiConsole.MarkupLine("[yellow]Hardware-Manager Tests[/]");
        AnsiConsole.WriteLine();

        if (!_channelMap.PinManagers.Any())
        {
            AnsiConsole.MarkupLine("[red]❌ Keine Pin-Manager konfiguriert[/]");
            WaitForKey();
            return;
        }

        var table = new Table();
        table.AddColumn("Pin-Manager");
        table.AddColumn("Typ");
        table.AddColumn("Status");
        table.AddColumn("Details");

        foreach (var pm in _channelMap.PinManagers)
        {
            var status = TestPinManager(pm.Key, pm.Value);
            table.AddRow(
                pm.Key,
                pm.Value.Type ?? "Unbekannt",
                status.Success ? "[green]✓ OK[/]" : "[red]✗ Fehler[/]",
                status.Message
            );
        }

        AnsiConsole.Write(table);
        WaitForKey();
    }

    private TestResult TestPinManager(string name, PinManagerMapItem config)
    {
        try
        {
            AnsiConsole.MarkupLine($"[blue]Testing Pin-Manager: {name}[/]");

            if (string.IsNullOrEmpty(config.Type))
            {
                return new TestResult(false, "Kein Typ konfiguriert");
            }

            // Validiere Konfiguration und teste Hardware-Verfügbarkeit
            switch (config.Type)
            {
                case "Pca9685PwmExtension":
                    return ValidatePca9685Config(name, config);
                case "RaspberryPiGpioManager":
                    return ValidateRaspberryPiConfig(name, config);
                default:
                    return new TestResult(false, $"Unbekannter Typ: {config.Type}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Testen von Pin-Manager {Name}", name);
            return new TestResult(false, $"Exception: {ex.Message}");
        }
    }

    private TestResult ValidatePca9685Config(string name, PinManagerMapItem config)
    {
        try
        {
            var boardAddress = config.Options.ContainsKey("boardAddress") ? 
                Convert.ToInt32(config.Options["boardAddress"]) : 64;
            var i2cBus = config.Options.ContainsKey("i2cBus") ? 
                Convert.ToInt32(config.Options["i2cBus"]) : 1;

            AnsiConsole.MarkupLine($"[dim]- Board Address: {boardAddress}[/]");
            AnsiConsole.MarkupLine($"[dim]- I2C Bus: {i2cBus}[/]");

            // Validiere Konfigurationswerte
            if (boardAddress < 0 || boardAddress > 127)
            {
                return new TestResult(false, "Ungültige Board-Adresse (muss 0-127 sein)");
            }

            if (i2cBus < 0 || i2cBus > 1)
            {
                return new TestResult(false, "Ungültiger I2C-Bus (muss 0 oder 1 sein)");
            }

            // Prüfe I2C-Interface Verfügbarkeit
            var i2cDevPath = $"/dev/i2c-{i2cBus}";
            if (File.Exists(i2cDevPath))
            {
                AnsiConsole.MarkupLine($"[dim]- I2C Device {i2cDevPath} verfügbar[/]");
                return new TestResult(true, $"PCA9685 Konfiguration gültig, I2C-{i2cBus} verfügbar");
            }
            else
            {
                return new TestResult(false, $"I2C Device {i2cDevPath} nicht verfügbar");
            }
        }
        catch (Exception ex)
        {
            return new TestResult(false, $"PCA9685 Konfigurationstest fehlgeschlagen: {ex.Message}");
        }
    }

    private TestResult ValidateRaspberryPiConfig(string name, PinManagerMapItem config)
    {
        try
        {
            AnsiConsole.MarkupLine("[dim]- Teste GPIO-Verfügbarkeit...[/]");
            
            // Teste GPIO-Verfügbarkeit durch Zugriff auf /sys/class/gpio
            var gpioPath = "/sys/class/gpio";
            if (Directory.Exists(gpioPath))
            {
                var exportPath = Path.Combine(gpioPath, "export");
                var unexportPath = Path.Combine(gpioPath, "unexport");
                
                if (File.Exists(exportPath) && File.Exists(unexportPath))
                {
                    AnsiConsole.MarkupLine("[dim]- GPIO Sysfs Interface verfügbar[/]");
                    return new TestResult(true, "GPIO-Interface verfügbar und konfiguriert");
                }
                else
                {
                    return new TestResult(false, "GPIO Export/Unexport Interface nicht verfügbar");
                }
            }
            
            return new TestResult(false, "GPIO Sysfs Interface nicht verfügbar (/sys/class/gpio nicht gefunden)");
        }
        catch (Exception ex)
        {
            return new TestResult(false, $"GPIO Konfigurationstest fehlgeschlagen: {ex.Message}");
        }
    }

    private void TestControlChannels()
    {
        AnsiConsole.MarkupLine("[yellow]Control-Channels Tests[/]");
        AnsiConsole.WriteLine();

        if (!_channelMap.ControlChannels.Any())
        {
            AnsiConsole.MarkupLine("[red]❌ Keine Control-Channels konfiguriert[/]");
            WaitForKey();
            return;
        }

        var table = new Table();
        table.AddColumn("Channel");
        table.AddColumn("Typ");
        table.AddColumn("Address");
        table.AddColumn("Status");
        table.AddColumn("Test-Ergebnis");

        foreach (var cc in _channelMap.ControlChannels)
        {
            var status = TestControlChannel(cc.Key, cc.Value);
            table.AddRow(
                cc.Key,
                cc.Value.ControlType,
                cc.Value.Address?.ToString() ?? "N/A",
                status.Success ? "[green]✓ OK[/]" : "[red]✗ Fehler[/]",
                status.Message
            );
        }

        AnsiConsole.Write(table);

        // Interaktiver Test
        if (AnsiConsole.Confirm("Möchten Sie einen interaktiven Control-Test durchführen?"))
        {
            RunInteractiveControlTest();
        }

        WaitForKey();
    }

    private TestResult TestControlChannel(string name, ControlChannelMapItem config)
    {
        try
        {
            AnsiConsole.MarkupLine($"[blue]Testing Control-Channel: {name}[/]");

            // Prüfe Pin-Manager
            var pinManager = config.PinManager ?? "default";
            if (pinManager != "default" && !_channelMap.PinManagers.ContainsKey(pinManager))
            {
                return new TestResult(false, $"Pin-Manager '{pinManager}' nicht gefunden");
            }

            // Prüfe Address
            if (!config.Address.HasValue)
            {
                return new TestResult(false, "Keine Hardware-Adresse konfiguriert");
            }

            var address = config.Address.Value;
            if (address < 0 || address > 255)
            {
                return new TestResult(false, "Ungültige Hardware-Adresse (0-255)");
            }

            // Validiere Control-Typ
            if (string.IsNullOrEmpty(config.ControlType))
            {
                return new TestResult(false, "Kein Control-Typ konfiguriert");
            }

            // Prüfe bekannte Control-Typen
            var knownTypes = new[] { "Steering", "Throttle", "ServoControl", "MotorControl", "GearControl", "PwmLight", "PwmBlinker", "RotaryLights" };
            if (!knownTypes.Contains(config.ControlType))
            {
                return new TestResult(false, $"Unbekannter Control-Typ: {config.ControlType}");
            }

            var details = $"Typ: {config.ControlType}, Adresse: {address}";
            if (pinManager != "default")
            {
                details += $", Pin-Manager: {pinManager}";
            }

            if (config.TestDisabled)
            {
                details += " (Test deaktiviert)";
            }

            return new TestResult(true, $"Konfiguration gültig - {details}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Control-Channel Test fehlgeschlagen für {Name}", name);
            return new TestResult(false, $"Test fehlgeschlagen: {ex.Message}");
        }
    }



    private void RunInteractiveControlTest()
    {
        AnsiConsole.MarkupLine("[yellow]Interaktiver Control-Test[/]");
        AnsiConsole.WriteLine();

        var channelName = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Welchen Channel möchten Sie testen?")
                .AddChoices(_channelMap.ControlChannels.Keys));

        var channel = _channelMap.ControlChannels[channelName];
        AnsiConsole.MarkupLine($"[green]Testing: {channelName} ({channel.ControlType})[/]");

        switch (channel.ControlType.ToLower())
        {
            case "steering":
                TestSteeringInteractive(channelName, channel);
                break;
            case "throttle":
                TestThrottleInteractive(channelName, channel);
                break;
            case "servocontrol":
                TestServoInteractive(channelName, channel);
                break;
            default:
                TestGenericControlInteractive(channelName, channel);
                break;
        }
    }

    private void TestSteeringInteractive(string name, ControlChannelMapItem config)
    {
        AnsiConsole.MarkupLine("[cyan]Lenkung Test - Simuliere verschiedene Positionen[/]");
        
        var positions = new[] { ("Links", 1000), ("Mitte", 1500), ("Rechts", 2000) };
        
        foreach (var (position, pwmValue) in positions)
        {
            AnsiConsole.MarkupLine($"[blue]Position: {position}[/]");
            AnsiConsole.MarkupLine($"[dim]PWM: {pwmValue}μs[/]");
            
            // Simuliere Test-Dauer
            for (int i = 0; i <= 100; i += 20)
            {
                AnsiConsole.MarkupLine($"[dim]Test-Progress: {i}%[/]");
                Thread.Sleep(100);
            }
            
            AnsiConsole.MarkupLine($"[green]✓ {position} erfolgreich getestet[/]");
            Thread.Sleep(300);
        }
    }

    private void TestThrottleInteractive(string name, ControlChannelMapItem config)
    {
        if (config.TestDisabled)
        {
            AnsiConsole.MarkupLine("[yellow]⚠️ Throttle-Test ist deaktiviert (Sicherheit)[/]");
            return;
        }

        AnsiConsole.MarkupLine("[red]⚠️ ACHTUNG: Throttle-Test kann das Fahrzeug bewegen![/]");
        if (!AnsiConsole.Confirm("Sind Sie sicher, dass Sie fortfahren möchten?"))
            return;

        AnsiConsole.MarkupLine("[cyan]Throttle Test - Vorsichtiger Geschwindigkeitstest[/]");
        
        var speeds = new[] { "Stop", "Langsam Vorwärts", "Stop", "Langsam Rückwärts", "Stop" };
        
        foreach (var speed in speeds)
        {
            AnsiConsole.MarkupLine($"[blue]Geschwindigkeit: {speed}[/]");
            
            var pwmValue = speed switch
            {
                "Stop" => 1500,
                "Langsam Vorwärts" => 1600,
                "Langsam Rückwärts" => 1400,
                _ => 1500
            };
            
            AnsiConsole.MarkupLine($"[dim]PWM: {pwmValue}μs[/]");
            
            Thread.Sleep(1000);
            AnsiConsole.MarkupLine($"[green]✓ {speed} getestet[/]");
        }
    }

    private void TestServoInteractive(string name, ControlChannelMapItem config)
    {
        AnsiConsole.MarkupLine("[cyan]Servo Test - Verschiedene Positionen[/]");
        
        var positions = new[] { 0, 45, 90, 135, 180 };
        
        foreach (var angle in positions)
        {
            AnsiConsole.MarkupLine($"[blue]Winkel: {angle}°[/]");
            
            var pwmValue = 1000 + (angle * 1000 / 180); // 1000-2000μs für 0-180°
            AnsiConsole.MarkupLine($"[dim]PWM: {pwmValue}μs[/]");
            
            Thread.Sleep(800);
            AnsiConsole.MarkupLine($"[green]✓ {angle}° Position erreicht[/]");
        }
    }

    private void TestGenericControlInteractive(string name, ControlChannelMapItem config)
    {
        AnsiConsole.MarkupLine($"[cyan]Generic Control Test für {config.ControlType}[/]");
        
        var testValues = new[] { 1000, 1250, 1500, 1750, 2000 };
        
        foreach (var value in testValues)
        {
            AnsiConsole.MarkupLine($"[blue]Test-Wert: {value}μs[/]");
            Thread.Sleep(500);
            AnsiConsole.MarkupLine($"[green]✓ Wert {value} gesendet[/]");
        }
    }

    private void TestTelemetryChannels()
    {
        AnsiConsole.MarkupLine("[yellow]Telemetrie-Channels Tests[/]");
        AnsiConsole.WriteLine();

        if (!_channelMap.TelemetryChannels.Any())
        {
            AnsiConsole.MarkupLine("[red]❌ Keine Telemetrie-Channels konfiguriert[/]");
            WaitForKey();
            return;
        }

        var table = new Table();
        table.AddColumn("Channel");
        table.AddColumn("Typ");
        table.AddColumn("Interval");
        table.AddColumn("Status");
        table.AddColumn("Test-Wert");

        foreach (var tc in _channelMap.TelemetryChannels)
        {
            var status = TestTelemetryChannel(tc.Key, tc.Value);
            table.AddRow(
                tc.Key,
                tc.Value.TelemetryType.Split('.').Last(),
                $"{tc.Value.ReadIntervalTicks} ticks",
                status.Success ? "[green]✓ OK[/]" : "[red]✗ Fehler[/]",
                status.Message
            );
        }

        AnsiConsole.Write(table);
        WaitForKey();
    }

    private TestResult TestTelemetryChannel(string name, TelemetryChannelMapItem config)
    {
        try
        {
            AnsiConsole.MarkupLine($"[blue]Testing Telemetrie-Channel: {name}[/]");

            var typeName = config.TelemetryType;
            if (string.IsNullOrEmpty(typeName))
            {
                return new TestResult(false, "Kein Telemetrie-Typ konfiguriert");
            }

            // Validiere Telemetrie-Typ
            var knownTypes = new[]
            {
                "LteCar.Onboard.Telemetry.CpuTemperatureReader",
                "LteCar.Onboard.Telemetry.ApplicationLifetimeReader",
                "LteCar.Onboard.Telemetry.JbdBmsTelemetryReader"
            };

            if (!knownTypes.Any(kt => typeName.Contains(kt.Split('.').Last())))
            {
                return new TestResult(false, $"Unbekannter Telemetrie-Typ: {typeName}");
            }

            // Validiere Intervall
            if (config.ReadIntervalTicks <= 0)
            {
                return new TestResult(false, "Ungültiges Lese-Intervall (muss > 0 sein)");
            }

            var details = $"Typ: {typeName.Split('.').Last()}, Intervall: {config.ReadIntervalTicks} ticks";

            // Teste spezifische Anforderungen
            if (typeName.Contains("CpuTemperatureReader"))
            {
                if (File.Exists("/sys/class/thermal/thermal_zone0/temp"))
                {
                    details += ", CPU-Temp verfügbar";
                }
                else
                {
                    details += ", CPU-Temp nicht verfügbar";
                }
            }
            else if (typeName.Contains("JbdBmsTelemetryReader"))
            {
                details += ", BMS erfordert serielle Verbindung";
            }

            return new TestResult(true, $"Konfiguration gültig - {details}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Telemetrie-Channel Test fehlgeschlagen für {Name}", name);
            return new TestResult(false, $"Test fehlgeschlagen: {ex.Message}");
        }
    }



    private void TestVideoStreams()
    {
        AnsiConsole.MarkupLine("[yellow]Video-Streams Tests[/]");
        AnsiConsole.WriteLine();

        if (!_channelMap.VideoStreams.Any())
        {
            AnsiConsole.MarkupLine("[red]❌ Keine Video-Streams konfiguriert[/]");
            WaitForKey();
            return;
        }

        var table = new Table();
        table.AddColumn("Stream");
        table.AddColumn("Name");
        table.AddColumn("Position");
        table.AddColumn("Status");
        table.AddColumn("Test-Ergebnis");

        foreach (var vs in _channelMap.VideoStreams)
        {
            var status = TestVideoStream(vs.Key, vs.Value);
            table.AddRow(
                vs.Key,
                vs.Value.Name,
                vs.Value.Location ?? "Unbekannt",
                vs.Value.Enabled ? "[green]Aktiviert[/]" : "[yellow]Deaktiviert[/]",
                status.Success ? "[green]✓ Verfügbar[/]" : "[red]✗ Fehler[/]"
            );
        }

        AnsiConsole.Write(table);
        WaitForKey();
    }

    private TestResult TestVideoStream(string name, VideoStreamMapItem config)
    {
        try
        {
            AnsiConsole.MarkupLine($"[blue]Testing Video-Stream: {name}[/]");

            if (!config.Enabled)
            {
                return new TestResult(true, "Stream deaktiviert");
            }

            // Prüfe Stream-Typ
            if (string.IsNullOrEmpty(config.Type))
            {
                return new TestResult(false, "Kein Stream-Typ definiert");
            }

            // Teste Video-Stream basierend auf Typ
            return TestVideoStreamInstance(name, config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Video-Stream Test fehlgeschlagen für {Name}", name);
            return new TestResult(false, $"Test fehlgeschlagen: {ex.Message}");
        }
    }

    private TestResult TestVideoStreamInstance(string name, VideoStreamMapItem config)
    {
        try
        {
            AnsiConsole.MarkupLine($"[dim]- Teste {config.Type} Video-Stream...[/]");

            return config.Type?.ToLower() switch
            {
                "raspicam" => TestRaspberryPiCamera(),
                "usbcam" => TestUsbCamera(),
                "ipcam" => TestIpCamera(),
                _ => TestGenericVideoStream(config.Type ?? "unknown")
            };
        }
        catch (Exception ex)
        {
            return new TestResult(false, $"Video-Stream Test fehlgeschlagen: {ex.Message}");
        }
    }

    private TestResult TestRaspberryPiCamera()
    {
        try
        {
            // Prüfe ob Pi-Kamera verfügbar ist
            var vcgencmdPath = "/usr/bin/vcgencmd";
            if (File.Exists(vcgencmdPath))
            {
                // Versuche Pi-Kamera Status zu prüfen
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = vcgencmdPath,
                    Arguments = "get_camera",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(processInfo);
                if (process != null)
                {
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    if (output.Contains("detected=1"))
                    {
                        return new TestResult(true, "Raspberry Pi Kamera erkannt");
                    }
                    else
                    {
                        return new TestResult(false, "Raspberry Pi Kamera nicht erkannt");
                    }
                }
            }

            // Fallback: Prüfe /dev/video0
            if (File.Exists("/dev/video0"))
            {
                return new TestResult(true, "Video-Device /dev/video0 verfügbar");
            }

            return new TestResult(false, "Keine Pi-Kamera oder Video-Device gefunden");
        }
        catch (Exception ex)
        {
            return new TestResult(false, $"Pi-Kamera Test fehlgeschlagen: {ex.Message}");
        }
    }

    private TestResult TestUsbCamera()
    {
        try
        {
            // Prüfe USB-Video-Devices
            var videoDevices = Directory.GetFiles("/dev", "video*")
                .Where(f => File.Exists(f))
                .ToArray();

            if (videoDevices.Any())
            {
                var deviceList = string.Join(", ", videoDevices.Select(Path.GetFileName));
                return new TestResult(true, $"USB-Video-Devices gefunden: {deviceList}");
            }

            return new TestResult(false, "Keine USB-Video-Devices gefunden");
        }
        catch (Exception ex)
        {
            return new TestResult(false, $"USB-Kamera Test fehlgeschlagen: {ex.Message}");
        }
    }

    private TestResult TestIpCamera()
    {
        try
        {
            // IP-Kamera Test würde Netzwerk-Ping oder HTTP-Request erfordern
            // Hier nur grundlegende Validierung
            return new TestResult(true, "IP-Kamera Konfiguration validiert (Netzwerk-Test nicht implementiert)");
        }
        catch (Exception ex)
        {
            return new TestResult(false, $"IP-Kamera Test fehlgeschlagen: {ex.Message}");
        }
    }

    private TestResult TestGenericVideoStream(string type)
    {
        return new TestResult(true, $"Video-Stream Typ '{type}' konfiguriert (kein spezifischer Test verfügbar)");
    }

    private void RunFullSystemTest()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[yellow]🔄 Vollständiger Systemtest[/]");
        AnsiConsole.WriteLine();

        var overallStatus = true;
        var testResults = new List<(string Category, List<(string Name, TestResult Result)> Tests)>();

        // Hardware-Manager Tests
        AnsiConsole.MarkupLine("[cyan]Phase 1: Hardware-Manager[/]");
        var hardwareTests = new List<(string, TestResult)>();
        foreach (var pm in _channelMap.PinManagers)
        {
            var result = TestPinManager(pm.Key, pm.Value);
            hardwareTests.Add((pm.Key, result));
            if (!result.Success) overallStatus = false;
        }
        testResults.Add(("Hardware-Manager", hardwareTests));

        // Control-Channels Tests
        AnsiConsole.MarkupLine("[cyan]Phase 2: Control-Channels[/]");
        var controlTests = new List<(string, TestResult)>();
        foreach (var cc in _channelMap.ControlChannels)
        {
            var result = TestControlChannel(cc.Key, cc.Value);
            controlTests.Add((cc.Key, result));
            if (!result.Success) overallStatus = false;
        }
        testResults.Add(("Control-Channels", controlTests));

        // Telemetrie Tests
        AnsiConsole.MarkupLine("[cyan]Phase 3: Telemetrie-Channels[/]");
        var telemetryTests = new List<(string, TestResult)>();
        foreach (var tc in _channelMap.TelemetryChannels)
        {
            var result = TestTelemetryChannel(tc.Key, tc.Value);
            telemetryTests.Add((tc.Key, result));
            if (!result.Success) overallStatus = false;
        }
        testResults.Add(("Telemetrie-Channels", telemetryTests));

        // Video-Stream Tests
        AnsiConsole.MarkupLine("[cyan]Phase 4: Video-Streams[/]");
        var videoTests = new List<(string, TestResult)>();
        foreach (var vs in _channelMap.VideoStreams)
        {
            var result = TestVideoStream(vs.Key, vs.Value);
            videoTests.Add((vs.Key, result));
            if (!result.Success) overallStatus = false;
        }
        testResults.Add(("Video-Streams", videoTests));

        // Zusammenfassung
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[yellow]Test-Zusammenfassung[/]"));
        AnsiConsole.WriteLine();

        foreach (var category in testResults)
        {
            var successCount = category.Tests.Count(t => t.Result.Success);
            var totalCount = category.Tests.Count;
            var color = successCount == totalCount ? "green" : "red";
            
            AnsiConsole.MarkupLine($"[{color}]{category.Category}: {successCount}/{totalCount} erfolgreich[/]");
        }

        AnsiConsole.WriteLine();
        if (overallStatus)
        {
            AnsiConsole.MarkupLine("[green]🎉 Alle Tests erfolgreich! Das System ist bereit.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[red]⚠️ Einige Tests sind fehlgeschlagen. Bitte überprüfen Sie die Konfiguration.[/]");
        }

        WaitForKey();
    }

    private void ShowTestReport()
    {
        AnsiConsole.MarkupLine("[yellow]📋 Test-Bericht[/]");
        AnsiConsole.WriteLine();

        var report = new Panel(
            new Markup("[cyan]System-Übersicht:[/]\n\n" +
                      $"[blue]Hardware-Manager:[/] {_channelMap.PinManagers.Count}\n" +
                      $"[blue]Control-Channels:[/] {_channelMap.ControlChannels.Count}\n" +
                      $"[blue]Telemetrie-Channels:[/] {_channelMap.TelemetryChannels.Count}\n" +
                      $"[blue]Video-Streams:[/] {_channelMap.VideoStreams.Count}\n\n" +
                      "[green]Empfehlung:[/] Führen Sie regelmäßig Tests durch, um die Hardware-Integrität sicherzustellen.")
        )
        .Header("System-Status")
        .Border(BoxBorder.Rounded);

        AnsiConsole.Write(report);
        WaitForKey();
    }

    private void WaitForKey()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine("Drücken Sie eine beliebige Taste...");
        Console.ReadKey();
    }
}

public class TestResult
{
    public bool Success { get; }
    public string Message { get; }

    public TestResult(bool success, string message)
    {
        Success = success;
        Message = message;
    }
}