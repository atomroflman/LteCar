using Spectre.Console;
using System.Text.Json;
using System.Text.Json.Serialization;
using LteCar.Shared.Channels;
using LteCar.Shared;

namespace LteCar.Onboard.Setup;

public class VehicleSetupTool
{
    private readonly string _channelMapPath;
    private readonly string _appSettingsPath;
    private ChannelMap _channelMap;
    private AppSettings _appSettings;

    public VehicleSetupTool()
    {
        _channelMapPath = Path.Combine(Directory.GetCurrentDirectory(), "channelMap.json");
        _appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appSettings.json");
        LoadConfiguration();
    }

    public static void Run()
    {
        var tool = new VehicleSetupTool();
        tool.StartSetup();
    }

    private void LoadConfiguration()
    {
        // Load channelMap.json
        if (File.Exists(_channelMapPath))
        {
            var channelMapJson = File.ReadAllText(_channelMapPath);
            _channelMap = JsonSerializer.Deserialize<ChannelMap>(channelMapJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }) ?? new ChannelMap();
        }
        else
        {
            _channelMap = new ChannelMap();
        }

        // Load appSettings.json
        if (File.Exists(_appSettingsPath))
        {
            var appSettingsJson = File.ReadAllText(_appSettingsPath);
            _appSettings = JsonSerializer.Deserialize<AppSettings>(appSettingsJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }) ?? new AppSettings();
        }
        else
        {
            _appSettings = new AppSettings();
        }
    }

    private void SaveConfiguration()
    {
        // Save channelMap.json
        var channelMapJson = JsonSerializer.Serialize(_channelMap, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        File.WriteAllText(_channelMapPath, channelMapJson);

        // Save appSettings.json
        var appSettingsJson = JsonSerializer.Serialize(_appSettings, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        File.WriteAllText(_appSettingsPath, appSettingsJson);
    }

    private void StartSetup()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText(SetupTexts.MainMenuTitle).Color(Color.Green));
        AnsiConsole.WriteLine();

        // Check if configuration is empty and suggest template
        if (IsConfigurationEmpty())
        {
            AnsiConsole.MarkupLine($"[yellow]{SetupTexts.NoConfigurationFound}[/]");
            if (AnsiConsole.Confirm(SetupTexts.UseTemplate))
            {
                var templateName = VehicleTemplateManager.ChooseTemplate();
                if (!string.IsNullOrEmpty(templateName))
                {
                    VehicleTemplateManager.ApplyTemplate(templateName, ref _channelMap);
                    AnsiConsole.WriteLine(SetupTexts.ContinueWithoutTemplate);
                    Console.ReadKey();
                }
            }
        }

        while (true)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title(SetupTexts.WhatToDo)
                    .PageSize(10)
                    .AddChoices(new[] {
                        SetupTexts.MainMenuConfigureControl,
                        SetupTexts.MainMenuConfigureTelemetry, 
                        SetupTexts.MainMenuConfigureVideo,
                        SetupTexts.MainMenuLoadTemplate,
                        SetupTexts.MainMenuSaveTemplate,
                        SetupTexts.MainMenuTemplateManager,
                        SetupTexts.MainMenuTestChannels,
                        SetupTexts.MainMenuSaveConfiguration,
                        SetupTexts.MainMenuChangeLanguage,
                        SetupTexts.MainMenuExit
                    }));

            if (choice == SetupTexts.MainMenuConfigureControl)
            {
                ConfigureControlChannels();
            }
            else if (choice == SetupTexts.MainMenuConfigureTelemetry)
            {
                ConfigureTelemetryChannels();
            }
            else if (choice == SetupTexts.MainMenuConfigureVideo)
            {
                ConfigureVideoStreams();
            }
            else if (choice == SetupTexts.MainMenuLoadTemplate)
            {
                ApplyTemplate();
            }
            else if (choice == SetupTexts.MainMenuSaveTemplate)
            {
                SaveCurrentAsTemplate();
            }
            else if (choice == SetupTexts.MainMenuTemplateManager)
            {
                ManageTemplates();
            }
            else if (choice == SetupTexts.MainMenuTestChannels)
            {
                TestChannelConfiguration();
            }
            else if (choice == SetupTexts.MainMenuSaveConfiguration)
            {
                SaveConfiguration();
                AnsiConsole.MarkupLine(SetupTexts.ConfigurationSaved);
                return;
            }
            else if (choice == SetupTexts.MainMenuChangeLanguage)
            {
                ChangeLanguage();
            }
            else if (choice == SetupTexts.MainMenuExit)
            {
                if (AnsiConsole.Confirm(SetupTexts.ConfirmExit))
                    return;
            }
        }
    }

    private void ConfigureVehicleBasics()
    {
        AnsiConsole.MarkupLine($"[yellow]{SetupTexts.VehicleBasicsTitle}[/]");
        AnsiConsole.WriteLine();

        _appSettings.CarId = AnsiConsole.Ask<string>(SetupTexts.VehicleId, _appSettings.CarId ?? "");
        _appSettings.CarName = AnsiConsole.Ask<string>(SetupTexts.VehicleName, _appSettings.CarName ?? "");
        
        if (AnsiConsole.Confirm(SetupTexts.ChangeVehiclePassword))
        {
            var password = AnsiConsole.Prompt(
                new TextPrompt<string>(SetupTexts.NewPassword)
                    .PromptStyle("red")
                    .Secret());
            _appSettings.CarPasswordHash = HashPassword(password);
        }

        AnsiConsole.MarkupLine($"[green]✓ {SetupTexts.BasicSettingsUpdated}[/]");
    }

    private void ConfigureHardware()
    {
        AnsiConsole.MarkupLine($"[yellow]{SetupTexts.HardwareConfigurationTitle}[/]");
        AnsiConsole.WriteLine();

        while (true)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title(SetupTexts.HardwareOptions)
                    .AddChoices(new[] {
                        SetupTexts.AddPinManager,
                        SetupTexts.EditPinManager,
                        SetupTexts.RemovePinManager,
                        SetupTexts.BackToMainMenu
                    }));

            if (choice == SetupTexts.AddPinManager)
            {
                AddPinManager();
            }
            else if (choice == SetupTexts.EditPinManager)
            {
                EditPinManager();
            }
            else if (choice == SetupTexts.RemovePinManager)
            {
                RemovePinManager();
            }
            else if (choice == SetupTexts.BackToMainMenu)
            {
                return;
            }
        }
    }

    private void AddPinManager()
    {
        var name = AnsiConsole.Ask<string>(SetupTexts.PinManagerName);
        
        var type = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(SetupTexts.PinManagerType)
                .AddChoices(new[] {
                    "Pca9685PwmExtension",
                    "RaspberryPiGpioManager"
                }));

        var pinManager = new PinManagerMapItem
        {
            Type = type,
            Options = new Dictionary<string, object>()
        };

        if (type == "Pca9685PwmExtension")
        {
            var boardAddress = AnsiConsole.Ask<int>(SetupTexts.BoardAddress, 64);
            var i2cBus = AnsiConsole.Ask<int>(SetupTexts.I2cBus, 1);
            
            pinManager.Options["boardAddress"] = boardAddress;
            pinManager.Options["i2cBus"] = i2cBus;
        }

        _channelMap.PinManagers[name] = pinManager;
        AnsiConsole.MarkupLine($"[green]✓ {string.Format(SetupTexts.PinManagerAdded, name)}[/]");
    }

    private void EditPinManager()
    {
        if (!_channelMap.PinManagers.Any())
        {
            AnsiConsole.MarkupLine($"[red]{SetupTexts.NoPinManagersAvailable}[/]");
            return;
        }

        var name = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(SetupTexts.SelectPinManager)
                .AddChoices(_channelMap.PinManagers.Keys));

        // Hier könnte eine detaillierte Bearbeitung implementiert werden
        AnsiConsole.MarkupLine($"[yellow]{string.Format(SetupTexts.EditingNotImplemented, name)}[/]");
    }

    private void RemovePinManager()
    {
        if (!_channelMap.PinManagers.Any())
        {
            AnsiConsole.MarkupLine($"[red]{SetupTexts.NoPinManagersAvailable}[/]");
            return;
        }

        var name = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(SetupTexts.RemovePinManagerTitle)
                .AddChoices(_channelMap.PinManagers.Keys));

        if (AnsiConsole.Confirm($"Pin-Manager '{name}' wirklich entfernen?"))
        {
            _channelMap.PinManagers.Remove(name);
            AnsiConsole.MarkupLine($"[green]✓ Pin-Manager '{name}' entfernt[/]");
        }
    }

    private void ConfigureControlChannels()
    {
        AnsiConsole.MarkupLine("[yellow]Control-Channels Konfiguration[/]");
        AnsiConsole.WriteLine();

        while (true)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Control-Channel Optionen:")
                    .AddChoices(new[] {
                        "Channel hinzufügen",
                        "Channel bearbeiten",
                        "Channel entfernen",
                        "Zurück zum Hauptmenü"
                    }));

            switch (choice)
            {
                case "Channel hinzufügen":
                    AddControlChannel();
                    break;
                case "Channel bearbeiten":
                    EditControlChannel();
                    break;
                case "Channel entfernen":
                    RemoveControlChannel();
                    break;
                case "Zurück zum Hauptmenü":
                    return;
            }
        }
    }

    private void AddControlChannel()
    {
        var name = AnsiConsole.Ask<string>("Channel Name:");
        
        var controlType = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Control Typ:")
                .AddChoices(new[] {
                    "Steering",
                    "Throttle",
                    "ServoControl",
                    "MotorControl"
                }));

        var address = AnsiConsole.Ask<int>("Hardware Address (0-255):");
        
        string? pinManager = null;
        if (_channelMap.PinManagers.Any())
        {
            var choices = _channelMap.PinManagers.Keys.ToList();
            choices.Add("default");
            
            pinManager = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Pin-Manager:")
                    .AddChoices(choices));
                    
            if (pinManager == "default") pinManager = null;
        }

        var testDisabled = false;
        if (controlType == "Throttle")
        {
            testDisabled = AnsiConsole.Confirm("Test-Modus deaktivieren? (Sicherheit)");
        }

        var channel = new ControlChannelMapItem
        {
            ControlType = controlType,
            Address = address,
            PinManager = pinManager,
            TestDisabled = testDisabled
        };

        _channelMap.ControlChannels[name] = channel;
        AnsiConsole.MarkupLine($"[green]✓ Control-Channel '{name}' hinzugefügt[/]");
    }

    private void EditControlChannel()
    {
        if (!_channelMap.ControlChannels.Any())
        {
            AnsiConsole.MarkupLine("[red]Keine Control-Channels vorhanden[/]");
            return;
        }

        var name = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Control-Channel auswählen:")
                .AddChoices(_channelMap.ControlChannels.Keys));

        AnsiConsole.MarkupLine($"[yellow]Bearbeitung von '{name}' noch nicht implementiert[/]");
    }

    private void RemoveControlChannel()
    {
        if (!_channelMap.ControlChannels.Any())
        {
            AnsiConsole.MarkupLine("[red]Keine Control-Channels vorhanden[/]");
            return;
        }

        var name = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Control-Channel entfernen:")
                .AddChoices(_channelMap.ControlChannels.Keys));

        if (AnsiConsole.Confirm($"Control-Channel '{name}' wirklich entfernen?"))
        {
            _channelMap.ControlChannels.Remove(name);
            AnsiConsole.MarkupLine($"[green]✓ Control-Channel '{name}' entfernt[/]");
        }
    }

    private void ConfigureTelemetryChannels()
    {
        AnsiConsole.MarkupLine("[yellow]Telemetrie-Channels Konfiguration[/]");
        AnsiConsole.WriteLine();

        while (true)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Telemetrie-Channel Optionen:")
                    .AddChoices(new[] {
                        "Channel hinzufügen",
                        "Channel bearbeiten", 
                        "Channel entfernen",
                        "Zurück zum Hauptmenü"
                    }));

            switch (choice)
            {
                case "Channel hinzufügen":
                    AddTelemetryChannel();
                    break;
                case "Channel bearbeiten":
                    EditTelemetryChannel();
                    break;
                case "Channel entfernen":
                    RemoveTelemetryChannel();
                    break;
                case "Zurück zum Hauptmenü":
                    return;
            }
        }
    }

    private void AddTelemetryChannel()
    {
        var name = AnsiConsole.Ask<string>("Channel Name:");
        
        var telemetryType = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Telemetrie Typ:")
                .AddChoices(new[] {
                    "LteCar.Onboard.Telemetry.CpuTemperatureReader",
                    "LteCar.Onboard.Telemetry.ApplicationLifetimeReader",
                    "LteCar.Onboard.Telemetry.JbdBmsTelemetryReader"
                }));

        var interval = AnsiConsole.Ask<int>("Lese-Intervall (Ticks):", 1000);

        var channel = new TelemetryChannelMapItem
        {
            TelemetryType = telemetryType,
            ReadIntervalTicks = interval
        };

        _channelMap.TelemetryChannels[name] = channel;
        AnsiConsole.MarkupLine($"[green]✓ Telemetrie-Channel '{name}' hinzugefügt[/]");
    }

    private void EditTelemetryChannel()
    {
        if (!_channelMap.TelemetryChannels.Any())
        {
            AnsiConsole.MarkupLine("[red]Keine Telemetrie-Channels vorhanden[/]");
            return;
        }

        var name = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Telemetrie-Channel auswählen:")
                .AddChoices(_channelMap.TelemetryChannels.Keys));

        AnsiConsole.MarkupLine($"[yellow]Bearbeitung von '{name}' noch nicht implementiert[/]");
    }

    private void RemoveTelemetryChannel()
    {
        if (!_channelMap.TelemetryChannels.Any())
        {
            AnsiConsole.MarkupLine("[red]Keine Telemetrie-Channels vorhanden[/]");
            return;
        }

        var name = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Telemetrie-Channel entfernen:")
                .AddChoices(_channelMap.TelemetryChannels.Keys));

        if (AnsiConsole.Confirm($"Telemetrie-Channel '{name}' wirklich entfernen?"))
        {
            _channelMap.TelemetryChannels.Remove(name);
            AnsiConsole.MarkupLine($"[green]✓ Telemetrie-Channel '{name}' entfernt[/]");
        }
    }

    private void ConfigureVideoStreams()
    {
        AnsiConsole.MarkupLine("[yellow]Video-Streams Konfiguration[/]");
        AnsiConsole.WriteLine();

        while (true)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Video-Stream Optionen:")
                    .AddChoices(new[] {
                        "Stream hinzufügen",
                        "Stream bearbeiten",
                        "Stream entfernen",
                        "Zurück zum Hauptmenü"
                    }));

            switch (choice)
            {
                case "Stream hinzufügen":
                    AddVideoStream();
                    break;
                case "Stream bearbeiten":
                    EditVideoStream();
                    break;
                case "Stream entfernen":
                    RemoveVideoStream();
                    break;
                case "Zurück zum Hauptmenü":
                    return;
            }
        }
    }

    private void AddVideoStream()
    {
        var streamId = AnsiConsole.Ask<string>("Stream Key:");
        var name = AnsiConsole.Ask<string>("Stream Name: (default: Key)");
        var displayName = AnsiConsole.Ask<string>("Anzeige Name:");
        var location = AnsiConsole.Ask<string>("Kamera Position:", "Front");
        var type = AnsiConsole.Ask<string>("Stream Typ:", "raspicam");
        var enabled = AnsiConsole.Confirm("Stream aktiviert?", true);

        var stream = new VideoStreamMapItem
        {
            StreamId = streamId, 
            Name = displayName,
            Location = location,
            Type = type,
            Enabled = enabled
        };

        _channelMap.VideoStreams[name] = stream;
        AnsiConsole.MarkupLine($"[green]✓ Video-Stream '{name}' hinzugefügt[/]");
    }

    private void EditVideoStream()
    {
        if (!_channelMap.VideoStreams.Any())
        {
            AnsiConsole.MarkupLine("[red]Keine Video-Streams vorhanden[/]");
            return;
        }

        var name = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Video-Stream auswählen:")
                .AddChoices(_channelMap.VideoStreams.Keys));

        AnsiConsole.MarkupLine($"[yellow]Bearbeitung von '{name}' noch nicht implementiert[/]");
    }

    private void RemoveVideoStream()
    {
        if (!_channelMap.VideoStreams.Any())
        {
            AnsiConsole.MarkupLine("[red]Keine Video-Streams vorhanden[/]");
            return;
        }

        var name = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Video-Stream entfernen:")
                .AddChoices(_channelMap.VideoStreams.Keys));

        if (AnsiConsole.Confirm($"Video-Stream '{name}' wirklich entfernen?"))
        {
            _channelMap.VideoStreams.Remove(name);
            AnsiConsole.MarkupLine($"[green]✓ Video-Stream '{name}' entfernt[/]");
        }
    }

    private void ConfigureServerConnection()
    {
        AnsiConsole.MarkupLine("[yellow]Server-Verbindung konfigurieren[/]");
        AnsiConsole.WriteLine();

        _appSettings.ServerUrl = AnsiConsole.Ask<string>("Server URL:", _appSettings.ServerUrl ?? "https://localhost:5001");
        _appSettings.ApiKey = AnsiConsole.Ask<string>("API Key:", _appSettings.ApiKey ?? "");

        AnsiConsole.MarkupLine("[green]✓ Server-Verbindung konfiguriert[/]");
    }

    private void ApplyTemplate()
    {
        var templateName = VehicleTemplateManager.ChooseTemplate();
        if (!string.IsNullOrEmpty(templateName))
        {
            if (AnsiConsole.Confirm(SetupTexts.OverwriteConfiguration))
            {
                VehicleTemplateManager.ApplyTemplate(templateName, ref _channelMap);
                AnsiConsole.MarkupLine($"[green]{SetupTexts.Success}[/]");
            }
        }
    }

    private void SaveCurrentAsTemplate()
    {
        if (IsConfigurationEmpty())
        {
            AnsiConsole.MarkupLine(SetupTexts.NoConfigurationToSave);
            AnsiConsole.WriteLine(SetupTexts.PressAnyKey);
            Console.ReadKey();
            return;
        }

        AnsiConsole.MarkupLine($"[yellow]{SetupTexts.SaveCurrentAsTemplate}[/]");
        AnsiConsole.WriteLine();

        var templateName = AnsiConsole.Ask<string>(SetupTexts.TemplateName);
        
        // Entferne ungültige Zeichen für Dateinamen
        templateName = string.Join("", templateName.Split(Path.GetInvalidFileNameChars()));
        
        if (string.IsNullOrWhiteSpace(templateName))
        {
            AnsiConsole.MarkupLine(SetupTexts.InvalidTemplateName);
            return;
        }

        var description = AnsiConsole.Ask<string>(SetupTexts.TemplateDescription, 
            string.Format(SetupTexts.DefaultTemplateDescription, DateTime.Now));

        VehicleTemplateManager.SaveCurrentAsTemplate(_channelMap, templateName, description);
        
        AnsiConsole.WriteLine(SetupTexts.PressAnyKey);
        Console.ReadKey();
    }

    private void ManageTemplates()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new FigletText(SetupTexts.TemplateManagerTitle).Color(Color.Blue));
            AnsiConsole.WriteLine();

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title(SetupTexts.WhatToDo)
                    .AddChoices(new[] {
                        SetupTexts.ShowTemplates,
                        SetupTexts.DeleteTemplate,
                        SetupTexts.Back
                    }));

            if (choice == SetupTexts.ShowTemplates)
            {
                VehicleTemplateManager.ListTemplates();
                AnsiConsole.WriteLine(SetupTexts.PressAnyKey);
                Console.ReadKey();
            }
            else if (choice == SetupTexts.DeleteTemplate)
            {
                DeleteTemplate();
            }
            else if (choice == SetupTexts.Back)
            {
                return;
            }
        }
    }

    private void DeleteTemplate()
    {
        var templatesPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "VehicleTemplates");
        
        if (!Directory.Exists(templatesPath))
        {
            AnsiConsole.MarkupLine(SetupTexts.NoTemplatesFolder);
            AnsiConsole.WriteLine(SetupTexts.PressAnyKey);
            Console.ReadKey();
            return;
        }

        var templateDirs = Directory.GetDirectories(templatesPath);
        
        if (!templateDirs.Any())
        {
            AnsiConsole.MarkupLine(SetupTexts.NoTemplatesToDelete);
            AnsiConsole.WriteLine(SetupTexts.PressAnyKey);
            Console.ReadKey();
            return;
        }

        var templateNames = templateDirs
            .Select(d => Path.GetFileName(d))
            .ToList();
        templateNames.Add(SetupTexts.Cancel);

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(SetupTexts.WhichTemplateToDelete)
                .AddChoices(templateNames));

        if (choice != SetupTexts.Cancel)
        {
            VehicleTemplateManager.DeleteTemplate(choice);
        }

        AnsiConsole.WriteLine(SetupTexts.PressAnyKey);
        Console.ReadKey();
    }

    private void TestChannelConfiguration()
    {
        if (IsConfigurationEmpty())
        {
            AnsiConsole.MarkupLine(SetupTexts.NoConfigurationToTest);
            AnsiConsole.WriteLine(SetupTexts.PressAnyKey);
            Console.ReadKey();
            return;
        }

        AnsiConsole.MarkupLine($"[yellow]{SetupTexts.TestingChannelConfiguration}[/]");
        AnsiConsole.WriteLine();

        ChannelTestTool.RunTests(_channelMap);
    }

    private void ChangeLanguage()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("Language / Sprache").Color(Color.Blue));
        AnsiConsole.WriteLine();

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(SetupTexts.SelectLanguage)
                .AddChoices(new[] {
                    SetupTexts.LanguageGerman,
                    SetupTexts.LanguageEnglish,
                    SetupTexts.Cancel
                }));

        if (choice == SetupTexts.LanguageGerman)
        {
            SetupTexts.CurrentLanguage = Language.German;
            AnsiConsole.MarkupLine(SetupTexts.LanguageChanged);
        }
        else if (choice == SetupTexts.LanguageEnglish)
        {
            SetupTexts.CurrentLanguage = Language.English;
            AnsiConsole.MarkupLine(SetupTexts.LanguageChanged);
        }

        if (choice != SetupTexts.Cancel)
        {
            AnsiConsole.WriteLine(SetupTexts.PressAnyKey);
            Console.ReadKey();
        }
    }

    private void ShowConfiguration()
    {
        AnsiConsole.MarkupLine("[yellow]Aktuelle Konfiguration[/]");
        AnsiConsole.WriteLine();

        var table = new Table();
        table.AddColumn("Kategorie");
        table.AddColumn("Anzahl");
        table.AddColumn("Details");

        table.AddRow("Fahrzeug", "1", $"ID: {_appSettings.CarId ?? "Nicht gesetzt"}");
        table.AddRow("Pin-Manager", _channelMap.PinManagers.Count.ToString(), 
                    string.Join(", ", _channelMap.PinManagers.Keys));
        table.AddRow("Control-Channels", _channelMap.ControlChannels.Count.ToString(), 
                    string.Join(", ", _channelMap.ControlChannels.Keys));
        table.AddRow("Telemetrie-Channels", _channelMap.TelemetryChannels.Count.ToString(), 
                    string.Join(", ", _channelMap.TelemetryChannels.Keys));  
        table.AddRow("Video-Streams", _channelMap.VideoStreams.Count.ToString(), 
                    string.Join(", ", _channelMap.VideoStreams.Keys));

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine("Drücken Sie eine beliebige Taste...");
        Console.ReadKey();
    }

    private bool IsConfigurationEmpty()
    {
        return !_channelMap.PinManagers.Any() && 
               !_channelMap.ControlChannels.Any() && 
               !_channelMap.TelemetryChannels.Any() && 
               !_channelMap.VideoStreams.Any();
    }

    private string HashPassword(string password)
    {
        // Einfacher Hash für Demo - in Produktion sollte BCrypt o.ä. verwendet werden
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password + "salt"));
    }
}

public class AppSettings
{
    [JsonPropertyName("carId")]
    public string? CarId { get; set; }
    
    [JsonPropertyName("carName")]
    public string? CarName { get; set; }
    
    [JsonPropertyName("carPasswordHash")]
    public string? CarPasswordHash { get; set; }
    
    [JsonPropertyName("serverUrl")]
    public string? ServerUrl { get; set; }
    
    [JsonPropertyName("apiKey")]
    public string? ApiKey { get; set; }
}