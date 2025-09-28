using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;
using LteCar.Shared.Channels;

namespace LteCar.Onboard.Setup;

public static class VehicleTemplateManager
{
    public static string ChooseTemplate()
    {
        var templatesPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "VehicleTemplates");
        
        if (!Directory.Exists(templatesPath))
        {
            AnsiConsole.MarkupLine("[red]Kein Vorlagen-Ordner gefunden[/]");
            AnsiConsole.WriteLine("Drücken Sie eine beliebige Taste...");
            Console.ReadKey();
            return string.Empty;
        }

        var templateDirs = Directory.GetDirectories(templatesPath);
        
        if (!templateDirs.Any())
        {
            AnsiConsole.MarkupLine("[red]Keine Vorlagen gefunden[/]");
            AnsiConsole.WriteLine("Drücken Sie eine beliebige Taste...");
            Console.ReadKey();
            return string.Empty;
        }

        var templates = new List<VehicleTemplate>();
        var templateChoices = new List<string>();

        foreach (var dir in templateDirs)
        {
            var configFile = Path.Combine(dir, "config.json");
            if (!File.Exists(configFile))
                continue;
                
            try
            {
                var content = File.ReadAllText(configFile);
                var template = JsonSerializer.Deserialize<VehicleTemplate>(content, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (template != null)
                {
                    templates.Add(template);
                    var displayName = $"{template.Name} - {template.Description}";
                    templateChoices.Add(displayName);
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine("[red]Fehler beim Laden von {0}: {1}[/]", 
                    Path.GetFileName(dir), ex.Message);
            }
        }

        if (!templateChoices.Any())
        {
            AnsiConsole.MarkupLine("[red]Keine gültigen Vorlagen gefunden[/]");
            AnsiConsole.WriteLine("Drücken Sie eine beliebige Taste...");
            Console.ReadKey();
            return string.Empty;
        }

        templateChoices.Add("❌ Abbrechen");

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Welche [green]Vorlage[/] möchten Sie verwenden?")
                .PageSize(10)
                .AddChoices(templateChoices));

        if (choice == "❌ Abbrechen")
            return string.Empty;

        var selectedIndex = templateChoices.IndexOf(choice);
        if (selectedIndex >= 0 && selectedIndex < templates.Count)
        {
            var selectedTemplate = templates[selectedIndex];
            
            // Template-Details anzeigen
            ShowTemplateDetails(selectedTemplate);
            
            if (AnsiConsole.Confirm($"Möchten Sie die Vorlage '{selectedTemplate.Name}' anwenden?"))
            {
                return selectedTemplate.Name;
            }
        }

        return string.Empty;
    }

    public static void ApplyTemplate(string templateName, ref ChannelMap channelMap)
    {
        var templatesPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "VehicleTemplates");
        var templateDir = Path.Combine(templatesPath, templateName);
        var configFile = Path.Combine(templateDir, "config.json");
        
        if (!File.Exists(configFile))
        {
            AnsiConsole.MarkupLine("[red]Vorlage '{0}' nicht gefunden[/]", templateName);
            return;
        }

        try
        {
            var content = File.ReadAllText(configFile);
            var template = JsonSerializer.Deserialize<VehicleTemplate>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (template?.ChannelMap != null)
            {
                channelMap = template.ChannelMap;
                AnsiConsole.MarkupLine("[green]✓ Vorlage '{0}' erfolgreich angewendet[/]", templateName);
                
                // Zeige verfügbare zusätzliche Dateien
                var scriptsPath = Path.Combine(templateDir, "scripts");
                if (Directory.Exists(scriptsPath) && Directory.GetFiles(scriptsPath).Any())
                {
                    AnsiConsole.MarkupLine("[blue]💡 Zusätzliche Skripte verfügbar in: {0}[/]", scriptsPath);
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Ungültige Vorlage: {0}[/]", templateName);
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("[red]Fehler beim Anwenden der Vorlage: {0}[/]", ex.Message);
        }
    }

    public static void SaveCurrentAsTemplate(ChannelMap channelMap, string templateName, string description)
    {
        var templatesPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "VehicleTemplates");
        var templateDir = Path.Combine(templatesPath, templateName);
        
        // Erstelle Template-Ordner und Unterordner
        Directory.CreateDirectory(templateDir);
        Directory.CreateDirectory(Path.Combine(templateDir, "scripts"));
        Directory.CreateDirectory(Path.Combine(templateDir, "models"));
        Directory.CreateDirectory(Path.Combine(templateDir, "docs"));

        var template = new VehicleTemplate
        {
            Name = templateName,
            Description = description,
            Version = "1.0",
            Author = Environment.UserName,
            Created = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            ChannelMap = channelMap
        };

        var configPath = Path.Combine(templateDir, "config.json");
        var json = JsonSerializer.Serialize(template, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        File.WriteAllText(configPath, json);
        
        // Erstelle README für das Template
        var readmePath = Path.Combine(templateDir, "README.md");
        var readmeContent = $@"# {templateName} Template

## Beschreibung
{description}

## Konfiguration
Erstellt am: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
Autor: {Environment.UserName}

## Dateien
- `config.json`: Haupt-Konfigurationsdatei
- `scripts/`: Zusätzliche Skripte für dieses Fahrzeug
- `models/`: 3D-Modelle und CAD-Dateien
- `docs/`: Zusätzliche Dokumentation

## Installation
```bash
cd /path/to/LteCar/Onboard
./setup-vehicle.sh
# Wähle ""📋 Vorlage laden und anwenden"" -> ""{templateName}""
```
";
        File.WriteAllText(readmePath, readmeContent);
        
        AnsiConsole.MarkupLine($"[green]✅ Vorlage '{templateName}' erfolgreich gespeichert unter {templateDir}[/]");
        AnsiConsole.MarkupLine($"[blue]📁 Ordnerstruktur:[/]");
        AnsiConsole.MarkupLine($"  • config.json - Konfigurationsdatei");
        AnsiConsole.MarkupLine($"  • README.md - Dokumentation");
        AnsiConsole.MarkupLine($"  • scripts/ - Zusätzliche Skripte");
        AnsiConsole.MarkupLine($"  • models/ - 3D-Modelle");
        AnsiConsole.MarkupLine($"  • docs/ - Weitere Dokumentation");
        
        try
        {
            // Erstelle ein Standard-Setup-Skript
            var setupScriptPath = Path.Combine(templateDir, "scripts", "setup.sh");
            var setupScript = $@"#!/bin/bash
# {templateName} spezifische Setup-Skripte

echo ""🚛 {templateName} Setup wird gestartet...""

# Beispiel: Spezielle Hardware-Kalibrierung
echo ""📡 Kalibriere Servo-Motoren...""
# servo_calibration_commands_here

# Beispiel: Sensor-Setup
echo ""📊 Konfiguriere Sensoren...""
# sensor_setup_commands_here

echo ""✅ {templateName} Setup abgeschlossen!""
";
            File.WriteAllText(setupScriptPath, setupScript);
            
            // Mache Skript ausführbar (Unix-Systeme)
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                var chmod = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "chmod",
                        Arguments = $"+x \"{setupScriptPath}\"",
                        UseShellExecute = false
                    }
                };
                chmod.Start();
                chmod.WaitForExit();
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]⚠️ Warnung beim Erstellen des Setup-Skripts: {ex.Message}[/]");
        }
    }

    public static void ListTemplates()
    {
        var templatesPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "VehicleTemplates");
        
        AnsiConsole.MarkupLine("[green]📋 Verfügbare Fahrzeug-Vorlagen:[/]");
        AnsiConsole.WriteLine();

        if (!Directory.Exists(templatesPath))
        {
            AnsiConsole.MarkupLine("[yellow]Kein Vorlagen-Ordner gefunden[/]");
            return;
        }

        var templateDirs = Directory.GetDirectories(templatesPath);
        
        if (!templateDirs.Any())
        {
            AnsiConsole.MarkupLine("[yellow]Keine Vorlagen gefunden[/]");
            return;
        }

        var table = new Table();
        table.AddColumn("Name");
        table.AddColumn("Beschreibung");
        table.AddColumn("Version");
        table.AddColumn("Autor");
        table.AddColumn("Zusätzliche Dateien");

        foreach (var dir in templateDirs)
        {
            var configFile = Path.Combine(dir, "config.json");
            if (!File.Exists(configFile))
                continue;
                
            try
            {
                var content = File.ReadAllText(configFile);
                var template = JsonSerializer.Deserialize<VehicleTemplate>(content, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (template != null)
                {
                    var additionalFiles = new List<string>();
                    
                    if (File.Exists(Path.Combine(dir, "README.md")))
                        additionalFiles.Add("README");
                        
                    var scriptsCount = Directory.Exists(Path.Combine(dir, "scripts")) 
                        ? Directory.GetFiles(Path.Combine(dir, "scripts")).Length : 0;
                    if (scriptsCount > 0)
                        additionalFiles.Add($"{scriptsCount} Script(s)");
                        
                    var modelsCount = Directory.Exists(Path.Combine(dir, "models"))
                        ? Directory.GetFiles(Path.Combine(dir, "models")).Length : 0;
                    if (modelsCount > 0)
                        additionalFiles.Add($"{modelsCount} Modell(e)");
                        
                    var docsCount = Directory.Exists(Path.Combine(dir, "docs"))
                        ? Directory.GetFiles(Path.Combine(dir, "docs")).Length : 0;
                    if (docsCount > 0)
                        additionalFiles.Add($"{docsCount} Dokument(e)");

                    table.AddRow(
                        template.Name ?? "Unbekannt",
                        template.Description ?? "Keine Beschreibung",
                        template.Version ?? "N/A",
                        template.Author ?? "Unbekannt",
                        additionalFiles.Any() ? string.Join(", ", additionalFiles) : "-"
                    );
                }
            }
            catch (Exception ex)
            {
                table.AddRow(
                    Path.GetFileName(dir),
                    $"[red]Fehler: {ex.Message}[/]",
                    "N/A",
                    "N/A",
                    "N/A"
                );
            }
        }

        AnsiConsole.Write(table);
    }

    private static void ShowTemplateDetails(VehicleTemplate template)
    {
        var panel = new Panel(
            new Markup($"[bold]{template.Name}[/]\n\n" +
                      $"[blue]Beschreibung:[/] {template.Description}\n" +
                      $"[blue]Version:[/] {template.Version}\n" +
                      $"[blue]Autor:[/] {template.Author}\n" +
                      $"[blue]Erstellt:[/] {template.Created}\n\n" +
                      $"[green]Konfiguration:[/]\n" +
                      $"• Pin-Manager: {template.ChannelMap?.PinManagers.Count ?? 0}\n" +
                      $"• Control-Channels: {template.ChannelMap?.ControlChannels.Count ?? 0}\n" +
                      $"• Telemetrie-Channels: {template.ChannelMap?.TelemetryChannels.Count ?? 0}\n" +
                      $"• Video-Streams: {template.ChannelMap?.VideoStreams.Count ?? 0}")
        )
        .Header("Template-Details")
        .Border(BoxBorder.Rounded);

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    public static void DeleteTemplate(string templateName)
    {
        var templatesPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "VehicleTemplates");
        var templateDir = Path.Combine(templatesPath, templateName);

        if (!Directory.Exists(templateDir))
        {
            AnsiConsole.MarkupLine($"[red]❌ Vorlage '{templateName}' nicht gefunden[/]");
            return;
        }

        // Zeige was gelöscht wird
        var fileCount = Directory.GetFiles(templateDir, "*", SearchOption.AllDirectories).Length;
        var dirCount = Directory.GetDirectories(templateDir, "*", SearchOption.AllDirectories).Length;
        
        AnsiConsole.MarkupLine($"[yellow]⚠️ Diese Aktion löscht:[/]");
        AnsiConsole.MarkupLine($"  • Template-Ordner: {templateName}");
        AnsiConsole.MarkupLine($"  • {fileCount} Datei(en)");
        AnsiConsole.MarkupLine($"  • {dirCount} Unterordner");
        AnsiConsole.WriteLine();

        var confirmed = AnsiConsole.Confirm($"[red]Sind Sie sicher, dass Sie die gesamte Vorlage '{templateName}' löschen möchten?[/]");
        
        if (!confirmed)
        {
            AnsiConsole.MarkupLine("[yellow]Löschvorgang abgebrochen[/]");
            return;
        }

        try
        {
            Directory.Delete(templateDir, true);
            AnsiConsole.MarkupLine($"[green]✅ Vorlage '{templateName}' und alle zugehörigen Dateien erfolgreich gelöscht[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Fehler beim Löschen der Vorlage: {ex.Message}[/]");
        }
    }
}

public class VehicleTemplate
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("version")]
    public string? Version { get; set; }
    
    [JsonPropertyName("author")]
    public string? Author { get; set; }
    
    [JsonPropertyName("created")]
    public string? Created { get; set; }
    
    [JsonPropertyName("channelMap")]
    public ChannelMap? ChannelMap { get; set; }
}