using Spectre.Console;
using System.Text.Json;
using System.Text.Json.Serialization;
using LteCar.Shared.Channels;

namespace LteCar.Onboard.Setup;

public class SetupMenu
{
    private readonly ConfigLoader _configLoader;
    private readonly string _appSettingsPath;
    private AppSettings _appSettings;
    private ChannelMap _channelMap;
    private bool _needsRestart;

    public SetupMenu(ConfigLoader configLoader)
    {
        _configLoader = configLoader;
        _appSettingsPath = configLoader.AppSettingsPath;
        LoadConfiguration();
    }

    public static void Run(ConfigLoader configLoader)
    {
        var menu = new SetupMenu(configLoader);
        menu.ShowMainMenu();
    }

    private void LoadConfiguration()
    {
        if (File.Exists(_appSettingsPath))
        {
            var json = File.ReadAllText(_appSettingsPath);
            _appSettings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }) ?? new AppSettings();
        }
        else
        {
            _appSettings = new AppSettings();
        }

        var channelMapPath = _configLoader.ChannelMapPath;
        if (File.Exists(channelMapPath))
        {
            var json = File.ReadAllText(channelMapPath);
            _channelMap = JsonSerializer.Deserialize<ChannelMap>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }) ?? new ChannelMap();
        }
        else
        {
            _channelMap = new ChannelMap();
        }
    }

    private void SaveConfiguration()
    {
        var appSettingsJson = JsonSerializer.Serialize(_appSettings, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        File.WriteAllText(_appSettingsPath, appSettingsJson);

        var channelMapJson = JsonSerializer.Serialize(_channelMap, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        File.WriteAllText(_configLoader.ChannelMapPath, channelMapJson);
    }

    public void ShowMainMenu()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new FigletText("LteCar Setup").Color(Color.DarkCyan));
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Panel($"[dim]Template: {_configLoader.ConfigDir}[/]").Border(BoxBorder.None));
            AnsiConsole.WriteLine();

            var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Select an option:")
                .PageSize(12)
                .AddChoices(new[]
                {
                    "1. System Options",
                    "2. Network / Server",
                    "3. Vehicle Configuration",
                    "4. Hardware Test",
                    "5. Templates",
                    "6. Feature Flags",
                    "7. Update / Recovery",
                    "",
                    "< Finish and Reboot >",
                    "< Finish without Reboot >"
                }));

            switch (choice)
            {
                case "1. System Options":
                    ShowSystemMenu();
                    break;
                case "2. Network / Server":
                    ShowNetworkMenu();
                    break;
                case "3. Vehicle Configuration":
                    ShowVehicleMenu();
                    break;
                case "4. Hardware Test":
                    ShowHardwareTestMenu();
                    break;
                case "5. Templates":
                    ShowTemplateMenu();
                    break;
                case "6. Feature Flags":
                    ShowFeatureFlagsMenu();
                    break;
                case "7. Update / Recovery":
                    ShowUpdateMenu();
                    break;
                case "< Finish and Reboot >":
                    if (_needsRestart)
                    {
                        AnsiConsole.MarkupLine("[yellow]Reboot required for changes to take effect.[/]");
                        if (AnsiConsole.Confirm("Reboot now?"))
                        {
                            AnsiConsole.MarkupLine("[yellow]Rebooting...[/]");
                            SaveConfiguration();
                            return;
                        }
                    }
                    else
                    {
                        SaveConfiguration();
                        return;
                    }
                    break;
                case "< Finish without Reboot >":
                    SaveConfiguration();
                    return;
            }
        }
    }

    private void ShowSystemMenu()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Panel("[bold cyan]System Options[/]").Border(BoxBorder.Rounded));
            AnsiConsole.WriteLine();

            var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Select an option:")
                .AddChoices(new[]
                {
                    "S1. Hostname",
                    "S2. Memory Split",
                    "S3. SSH",
                    "S4. Boot Options",
                    "",
                    "< Back >"
                }));

            switch (choice)
            {
                case "S1. Hostname":
                    EditHostname();
                    break;
                case "S2. Memory Split":
                    EditMemorySplit();
                    break;
                case "S3. SSH":
                    EditSSH();
                    break;
                case "S4. Boot Options":
                    EditBootOptions();
                    break;
                case "< Back >":
                    return;
            }
        }
    }

    private void EditHostname()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Panel("[bold cyan]Hostname Configuration[/]").Border(BoxBorder.Rounded));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Hostname is set in appSettings.json[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.Prompt(new TextPrompt<string>("Press Enter to continue..."));
    }

    private void EditMemorySplit()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Panel("[bold cyan]Memory Split[/]").Border(BoxBorder.Rounded));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Memory split configuration not yet implemented[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.Prompt(new TextPrompt<string>("Press Enter to continue..."));
    }

    private void EditSSH()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Panel("[bold cyan]SSH Configuration[/]").Border(BoxBorder.Rounded));
        AnsiConsole.WriteLine();
        var enabled = AnsiConsole.Confirm("Enable SSH server?");
        AnsiConsole.MarkupLine($"[green]SSH enabled: {enabled}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.Prompt(new TextPrompt<string>("Press Enter to continue..."));
    }

    private void EditBootOptions()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Panel("[bold cyan]Boot Options[/]").Border(BoxBorder.Rounded));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Boot options not yet implemented[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.Prompt(new TextPrompt<string>("Press Enter to continue..."));
    }

    private void ShowNetworkMenu()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Panel("[bold cyan]Network / Server[/]").Border(BoxBorder.Rounded));
            AnsiConsole.WriteLine();

            var serverName = _appSettings.ServerUrl ?? "not set";
            AnsiConsole.MarkupLine($"[dim]Current server: {serverName}[/]");
            AnsiConsole.WriteLine();

            var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Select an option:")
                .AddChoices(new[]
                {
                    "N1. Server URL",
                    "N2. Connection Test",
                    "N3. WiFi Settings",
                    "",
                    "< Back >"
                }));

            switch (choice)
            {
                case "N1. Server URL":
                    EditServerUrl();
                    break;
                case "N2. Connection Test":
                    TestConnection();
                    break;
                case "N3. WiFi Settings":
                    EditWifiSettings();
                    break;
                case "< Back >":
                    return;
            }
        }
    }

    private void EditServerUrl()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Panel("[bold cyan]Server URL[/]").Border(BoxBorder.Rounded));
        AnsiConsole.WriteLine();

        var currentUrl = _appSettings.ServerUrl ?? "https://lte-rc.northeurope.cloudapp.azure.com:5000";
        var newUrl = AnsiConsole.Ask<string>("Enter server URL:", currentUrl);
        _appSettings.ServerUrl = newUrl;
        _needsRestart = true;
        AnsiConsole.MarkupLine("[green]Server URL updated.[/]");
    }

    private void TestConnection()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Panel("[bold cyan]Connection Test[/]").Border(BoxBorder.Rounded));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Testing connection...[/]");
        AnsiConsole.MarkupLine("[yellow](This is a placeholder - actual test not yet implemented)[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.Prompt(new TextPrompt<string>("Press Enter to continue..."));
    }

    private void EditWifiSettings()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Panel("[bold cyan]WiFi Settings[/]").Border(BoxBorder.Rounded));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]WiFi settings not yet implemented[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.Prompt(new TextPrompt<string>("Press Enter to continue..."));
    }

    private void ShowVehicleMenu()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Panel("[bold cyan]Vehicle Configuration[/]").Border(BoxBorder.Rounded));
            AnsiConsole.WriteLine();

            var templateName = Path.GetFileName(_configLoader.ConfigDir);
            AnsiConsole.MarkupLine($"[dim]Template: {templateName}[/]");
            AnsiConsole.WriteLine();

            var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Select an option:")
                .AddChoices(new[]
                {
                    "V1. Vehicle Name",
                    "V2. Edit channelMap",
                    "V3. View Configuration",
                    "V4. Reset to Defaults",
                    "",
                    "< Back >"
                }));

            switch (choice)
            {
                case "V1. Vehicle Name":
                    EditVehicleName();
                    break;
                case "V2. Edit channelMap":
                    EditChannelMap();
                    break;
                case "V3. View Configuration":
                    ViewConfiguration();
                    break;
                case "V4. Reset to Defaults":
                    ResetToDefaults();
                    break;
                case "< Back >":
                    return;
            }
        }
    }

    private void EditVehicleName()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Panel("[bold cyan]Vehicle Name[/]").Border(BoxBorder.Rounded));
        AnsiConsole.WriteLine();

        var currentName = _appSettings.CarName ?? "MyCar";
        var newName = AnsiConsole.Ask<string>("Enter vehicle name:", currentName);
        _appSettings.CarName = newName;
        AnsiConsole.MarkupLine("[green]Vehicle name updated.[/]");
    }

    private void EditChannelMap()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Panel("[bold cyan]Channel Map Editor[/]").Border(BoxBorder.Rounded));
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[yellow]Channel map editing is done through the existing setup wizard.[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Available channels:");
        AnsiConsole.MarkupLine($"  - Control channels: {_channelMap.ControlChannels.Count}");
        AnsiConsole.MarkupLine($"  - Telemetry channels: {_channelMap.TelemetryChannels.Count}");
        AnsiConsole.MarkupLine($"  - Video streams: {_channelMap.VideoStreams.Count}");
        AnsiConsole.WriteLine();
        AnsiConsole.Prompt(new TextPrompt<string>("Press Enter to continue..."));
    }

    private void ViewConfiguration()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Panel("[bold cyan]Vehicle Configuration[/]").Border(BoxBorder.Rounded));
        AnsiConsole.WriteLine();

        var table = new Table();
        table.AddColumn("Setting");
        table.AddColumn("Value");

        table.AddRow("Vehicle Name", _appSettings.CarName ?? "Not set");
        table.AddRow("Car ID", _appSettings.CarId ?? "Not set");
        table.AddRow("Server URL", _appSettings.ServerUrl ?? "Not set");
        table.AddRow("Config Directory", _configLoader.ConfigDir);

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        if (AnsiConsole.Confirm("Show channel map details?"))
        {
            var channelTable = new Table();
            channelTable.AddColumn("Type");
            channelTable.AddColumn("Count");
            channelTable.AddRow("Control Channels", _channelMap.ControlChannels.Count.ToString());
            channelTable.AddRow("Telemetry Channels", _channelMap.TelemetryChannels.Count.ToString());
            channelTable.AddRow("Video Streams", _channelMap.VideoStreams.Count.ToString());
            channelTable.AddRow("Pin Managers", _channelMap.PinManagers.Count.ToString());
            AnsiConsole.Write(channelTable);
        }

        AnsiConsole.WriteLine();
        AnsiConsole.Prompt(new TextPrompt<string>("Press Enter to continue..."));
    }

    private void ResetToDefaults()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Panel("[bold cyan]Reset to Defaults[/]").Border(BoxBorder.Rounded));
        AnsiConsole.WriteLine();

        if (AnsiConsole.Confirm("This will reset vehicle configuration to defaults. Continue?"))
        {
            _channelMap = new ChannelMap();
            _appSettings = new AppSettings();
            _needsRestart = true;
            AnsiConsole.MarkupLine("[green]Configuration reset to defaults.[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.Prompt(new TextPrompt<string>("Press Enter to continue..."));
    }

    private void ShowHardwareTestMenu()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Panel("[bold cyan]Hardware Test[/]").Border(BoxBorder.Rounded));
            AnsiConsole.WriteLine();

            var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Select hardware to test:")
                .AddChoices(new[]
                {
                    "H1. All Outputs",
                    "H2. Control Channels",
                    "H3. Telemetry Sensors",
                    "H4. LEDs / Lights",
                    "H5. Servos",
                    "H6. Motors",
                    "",
                    "< Back >"
                }));

            switch (choice)
            {
                case "H1. All Outputs":
                    TestAllOutputs();
                    break;
                case "H2. Control Channels":
                    TestControlChannels();
                    break;
                case "H3. Telemetry Sensors":
                    TestTelemetrySensors();
                    break;
                case "H4. LEDs / Lights":
                    TestLEDs();
                    break;
                case "H5. Servos":
                    TestServos();
                    break;
                case "H6. Motors":
                    TestMotors();
                    break;
                case "< Back >":
                    return;
            }
        }
    }

    private void TestAllOutputs()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Panel("[bold cyan]Test All Outputs[/]").Border(BoxBorder.Rounded));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Running all hardware tests...[/]");
        AnsiConsole.MarkupLine("[yellow](Tests not yet implemented - needs integration with ControlService)[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.Prompt(new TextPrompt<string>("Press Enter to continue..."));
    }

    private void TestControlChannels()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Panel("[bold cyan]Test Control Channels[/]").Border(BoxBorder.Rounded));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[dim]Available control channels: {_channelMap.ControlChannels.Count}[/]");

        if (_channelMap.ControlChannels.Count > 0)
        {
            AnsiConsole.MarkupLine("[yellow](Channel testing not yet implemented)[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.Prompt(new TextPrompt<string>("Press Enter to continue..."));
    }

    private void TestTelemetrySensors()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Panel("[bold cyan]Test Telemetry Sensors[/]").Border(BoxBorder.Rounded));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[dim]Available telemetry channels: {_channelMap.TelemetryChannels.Count}[/]");
        AnsiConsole.MarkupLine("[yellow](Sensor testing not yet implemented)[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.Prompt(new TextPrompt<string>("Press Enter to continue..."));
    }

    private void TestLEDs()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Panel("[bold cyan]Test LEDs / Lights[/]").Border(BoxBorder.Rounded));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow](LED testing not yet implemented)[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.Prompt(new TextPrompt<string>("Press Enter to continue..."));
    }

    private void TestServos()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Panel("[bold cyan]Test Servos[/]").Border(BoxBorder.Rounded));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow](Servo testing not yet implemented)[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.Prompt(new TextPrompt<string>("Press Enter to continue..."));
    }

    private void TestMotors()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Panel("[bold cyan]Test Motors[/]").Border(BoxBorder.Rounded));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow](Motor testing not yet implemented)[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.Prompt(new TextPrompt<string>("Press Enter to continue..."));
    }

    private void ShowTemplateMenu()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Panel("[bold cyan]Templates[/]").Border(BoxBorder.Rounded));
            AnsiConsole.WriteLine();

            var currentTemplate = Path.GetFileName(_configLoader.ConfigDir);
            AnsiConsole.MarkupLine($"[dim]Current template: {currentTemplate}[/]");
            AnsiConsole.WriteLine();

            var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Select an option:")
                .AddChoices(new[]
                {
                    "T1. List Templates",
                    "T2. Select Template",
                    "T3. Create Template from Current",
                    "T4. Delete Template",
                    "T5. Set Template Base Path",
                    "",
                    "< Back >"
                }));

            switch (choice)
            {
                case "T1. List Templates":
                    VehicleTemplateManager.ListTemplates();
                    AnsiConsole.Prompt(new TextPrompt<string>("Press Enter to continue..."));
                    break;
                case "T2. Select Template":
                    SelectTemplate();
                    break;
                case "T3. Create Template from Current":
                    CreateTemplateFromCurrent();
                    break;
                case "T4. Delete Template":
                    DeleteTemplate();
                    break;
                case "T5. Set Template Base Path":
                    SetTemplateBasePath();
                    break;
                case "< Back >":
                    return;
            }
        }
    }

    private void SelectTemplate()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Panel("[bold cyan]Select Template[/]").Border(BoxBorder.Rounded));
        AnsiConsole.WriteLine();

        var templateName = VehicleTemplateManager.ChooseTemplate();
        if (!string.IsNullOrEmpty(templateName))
        {
            VehicleTemplateManager.ApplyTemplate(templateName, ref _channelMap);
            _needsRestart = true;
        }

        AnsiConsole.Prompt(new TextPrompt<string>("Press Enter to continue..."));
    }

    private void CreateTemplateFromCurrent()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Panel("[bold cyan]Create Template[/]").Border(BoxBorder.Rounded));
        AnsiConsole.WriteLine();

        var templateName = AnsiConsole.Ask<string>("Template name:");
        var description = AnsiConsole.Ask<string>("Description:");

        VehicleTemplateManager.SaveCurrentAsTemplate(_channelMap, templateName, description);

        AnsiConsole.Prompt(new TextPrompt<string>("Press Enter to continue..."));
    }

    private void DeleteTemplate()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Panel("[bold cyan]Delete Template[/]").Border(BoxBorder.Rounded));
        AnsiConsole.WriteLine();

        var templatesPath = GetTemplateBasePath();
        if (!Directory.Exists(templatesPath))
        {
            AnsiConsole.MarkupLine("[red]No templates directory found.[/]");
            AnsiConsole.Prompt(new TextPrompt<string>("Press Enter to continue..."));
            return;
        }

        var templateDirs = Directory.GetDirectories(templatesPath)
            .Select(d => Path.GetFileName(d)!)
            .ToList();

        if (!templateDirs.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No templates found.[/]");
            AnsiConsole.Prompt(new TextPrompt<string>("Press Enter to continue..."));
            return;
        }

        var toDelete = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("Select template to delete:")
            .AddChoices(templateDirs));

        VehicleTemplateManager.DeleteTemplate(toDelete);

        AnsiConsole.Prompt(new TextPrompt<string>("Press Enter to continue..."));
    }

    private void SetTemplateBasePath()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Panel("[bold cyan]Set Template Base Path[/]").Border(BoxBorder.Rounded));
        AnsiConsole.WriteLine();

        var currentPath = GetTemplateBasePath();
        AnsiConsole.MarkupLine($"[dim]Current path: {currentPath}[/]");
        AnsiConsole.WriteLine();

        var newPath = AnsiConsole.Ask<string>("Enter template base path (or press Enter to use current):");
        if (!string.IsNullOrWhiteSpace(newPath) && Directory.Exists(newPath))
        {
            SaveTemplateBasePath(newPath);
            AnsiConsole.MarkupLine("[green]Template base path updated.[/]");
        }
        else if (!string.IsNullOrWhiteSpace(newPath))
        {
            AnsiConsole.MarkupLine("[red]Directory does not exist.[/]");
        }

        AnsiConsole.Prompt(new TextPrompt<string>("Press Enter to continue..."));
    }

    private string GetTemplateBasePath()
    {
        var basePath = Environment.GetEnvironmentVariable("VEHICLE_TEMPLATES_PATH");
        if (!string.IsNullOrEmpty(basePath) && Directory.Exists(basePath))
            return basePath;

        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "vehicleTemplates");
    }

    private void SaveTemplateBasePath(string path)
    {
        Environment.SetEnvironmentVariable("VEHICLE_TEMPLATES_PATH", path);
    }

    private void ShowFeatureFlagsMenu()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Panel("[bold cyan]Feature Flags[/]").Border(BoxBorder.Rounded));
            AnsiConsole.WriteLine();

            var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Select feature to configure:")
                .AddChoices(new[]
                {
                    "F1. Web Setup Interface",
                    "F2. Bash Tool",
                    "F3. Channel Tester",
                    "F4. Audio",
                    "F5. Video",
                    "",
                    "< Back >"
                }));

            switch (choice)
            {
                case "F1. Web Setup Interface":
                    ToggleFeature("webSetup");
                    break;
                case "F2. Bash Tool":
                    ToggleFeature("bashTool");
                    break;
                case "F3. Channel Tester":
                    ToggleFeature("channelTester");
                    break;
                case "F4. Audio":
                    ToggleFeature("audio");
                    break;
                case "F5. Video":
                    ToggleFeature("video");
                    break;
                case "< Back >":
                    return;
            }
        }
    }

    private void ToggleFeature(string feature)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Panel($"[bold cyan]Toggle {feature}[/]").Border(BoxBorder.Rounded));
        AnsiConsole.WriteLine();

        var property = typeof(AppSettings).GetProperty(feature);
        if (property != null && property.PropertyType == typeof(bool))
        {
            var current = (bool)(property.GetValue(_appSettings) ?? false);
            var newValue = AnsiConsole.Confirm($"Enable {feature}?", current);
            property.SetValue(_appSettings, newValue);
            _needsRestart = true;
            AnsiConsole.MarkupLine($"[green]{feature} set to {newValue}.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[yellow]Feature '{feature}' is not a boolean flag or not implemented.[/]");
        }

        AnsiConsole.Prompt(new TextPrompt<string>("Press Enter to continue..."));
    }

    private void ShowUpdateMenu()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Panel("[bold cyan]Update / Recovery[/]").Border(BoxBorder.Rounded));
            AnsiConsole.WriteLine();

            var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Select an option:")
                .AddChoices(new[]
                {
                    "U1. Check for Updates",
                    "U2. Update Software",
                    "U3. Backup Configuration",
                    "U4. Restore from Backup",
                    "U5. Factory Reset",
                    "U6. View Logs",
                    "",
                    "< Back >"
                }));

            switch (choice)
            {
                case "U1. Check for Updates":
                    CheckForUpdates();
                    break;
                case "U2. Update Software":
                    UpdateSoftware();
                    break;
                case "U3. Backup Configuration":
                    BackupConfiguration();
                    break;
                case "U4. Restore from Backup":
                    RestoreFromBackup();
                    break;
                case "U5. Factory Reset":
                    FactoryReset();
                    break;
                case "U6. View Logs":
                    ViewLogs();
                    break;
                case "< Back >":
                    return;
            }
        }
    }

    private void CheckForUpdates()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Panel("[bold cyan]Check for Updates[/]").Border(BoxBorder.Rounded));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Update check not yet implemented[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.Prompt(new TextPrompt<string>("Press Enter to continue..."));
    }

    private void UpdateSoftware()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Panel("[bold cyan]Update Software[/]").Border(BoxBorder.Rounded));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Software update not yet implemented[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.Prompt(new TextPrompt<string>("Press Enter to continue..."));
    }

    private void BackupConfiguration()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Panel("[bold cyan]Backup Configuration[/]").Border(BoxBorder.Rounded));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Backup not yet implemented[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.Prompt(new TextPrompt<string>("Press Enter to continue..."));
    }

    private void RestoreFromBackup()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Panel("[bold cyan]Restore from Backup[/]").Border(BoxBorder.Rounded));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Restore not yet implemented[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.Prompt(new TextPrompt<string>("Press Enter to continue..."));
    }

    private void FactoryReset()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Panel("[bold red]Factory Reset[/]").Border(BoxBorder.Rounded));
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[red]WARNING: This will delete all configuration![/]");
        AnsiConsole.WriteLine();

        if (AnsiConsole.Confirm("Are you sure you want to factory reset?"))
        {
            if (AnsiConsole.Confirm("[red]This action cannot be undone. Type 'yes' to confirm:[/]"))
            {
                _channelMap = new ChannelMap();
                _appSettings = new AppSettings();
                SaveConfiguration();
                _needsRestart = true;
                AnsiConsole.MarkupLine("[green]Factory reset complete.[/]");
            }
        }

        AnsiConsole.Prompt(new TextPrompt<string>("Press Enter to continue..."));
    }

    private void ViewLogs()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Panel("[bold cyan]View Logs[/]").Border(BoxBorder.Rounded));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Log viewing not yet implemented[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.Prompt(new TextPrompt<string>("Press Enter to continue..."));
    }
}
