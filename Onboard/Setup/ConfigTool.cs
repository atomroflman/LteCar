using Spectre.Console;
using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Globalization;
using Setup;

namespace Setup
{
    public static class ConfigTool
    {
        public static void Run()
        {
            // Detect or set culture (default: system, fallback: en)
            var culture = CultureInfo.CurrentUICulture;
            if (Environment.GetEnvironmentVariable("LC_ALL") == "de_DE" 
                || Environment.GetEnvironmentVariable("LANG") == "de_DE" 
                || Environment.GetEnvironmentVariable("LANG") == "de_DE.UTF-8")
                culture = new CultureInfo("de");
            // Optionally: allow override by env var or argument

            // Set culture for resources
            Resources.Culture = culture;

            AnsiConsole.MarkupLine($"[bold yellow]{Resources.Title}[/]");
            var configFile = "appSettings.json";
            dynamic? config = null;
            if (File.Exists(configFile))
            {
                var json = File.ReadAllText(configFile);
                config = JsonSerializer.Deserialize<dynamic>(json);
                AnsiConsole.MarkupLine($"[green]{Resources.Load_Config}");
            }
            else
            {
                config = new System.Dynamic.ExpandoObject();
                config.CarName = culture.TwoLetterISOLanguageName == "de" ? "MeinAuto" : "MyCar";
                config.EnableChannelTest = false;
                AnsiConsole.MarkupLine($"[red]{Resources.No_Config}");
            }

            bool running = true;
            bool dirty = false;
            while (running)
            {
                var carName = config.CarName ?? (culture.TwoLetterISOLanguageName == "de" ? "MeinAuto" : "MyCar");
                var enableTest = config.EnableChannelTest ?? false;
                var menu = new[] {
                    $"{Resources.Set_Carname} {string.Format(Resources.Current_Value, carName)}",
                    $"{Resources.Set_Enabletest} {string.Format(Resources.Current_Value, enableTest)}",
                    "[root] appSettings.json bearbeiten",
                    Resources.Save_Exit,
                    Resources.Exit,
                };
                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title($"[bold]{Resources.Main_Menu}[/]")
                        .AddChoices(menu)
                );

                if (choice.StartsWith(Resources.Set_Carname))
                {
                    var newName = AnsiConsole.Ask<string>(Resources.Carname_Prompt, carName);
                    if (newName != carName) { config.CarName = newName; dirty = true; }
                }
                else if (choice.StartsWith(Resources.Set_Enabletest))
                {
                    var newVal = AnsiConsole.Confirm(Resources.Enabletest_Prompt, enableTest);
                    if (newVal != enableTest) { config.EnableChannelTest = newVal; dirty = true; }
                }
                else if (choice.StartsWith("[root]"))
                {
                    // Submenu for editing all root properties
                    bool subRunning = true;
                    while (subRunning)
                    {
                        // List all root properties except objects (show as string, bool, int, etc.)
                        var rootProps = new[] {
                            "ServerName",
                            "ServerPort",
                            "UseHttps",
                            "CarName",
                            "CarSecret",
                            "EnableChannelTest"
                        };
                        var subMenu = new System.Collections.Generic.List<string>();
                        foreach (var prop in rootProps)
                        {
                            object val = ((IDictionary<string, object>)config).ContainsKey(prop) ? ((IDictionary<string, object>)config)[prop] : null;
                            subMenu.Add($"{prop} = {val}");
                        }
                        subMenu.Add(Resources.Save_Exit);
                        subMenu.Add(Resources.Exit);
                        var subChoice = AnsiConsole.Prompt(
                            new SelectionPrompt<string>()
                                .Title("[bold]appSettings.json root bearbeiten[/]")
                                .AddChoices(subMenu)
                        );
                        if (subChoice == Resources.Save_Exit)
                        {
                            var options = new JsonSerializerOptions { WriteIndented = true };
                            var jsonOut = JsonSerializer.Serialize(config, options);
                            File.WriteAllText(configFile, jsonOut);
                            AnsiConsole.MarkupLine($"[green]{Resources.Saved}[/]");
                            dirty = false;
                            subRunning = false;
                        }
                        else if (subChoice == Resources.Exit)
                        {
                            subRunning = false;
                        }
                        else
                        {
                            // Parse property name
                            var prop = subChoice.Split('=')[0].Trim();
                            object oldVal = ((IDictionary<string, object>)config).ContainsKey(prop) ? ((IDictionary<string, object>)config)[prop] : null;
                            object newVal = oldVal;
                            if (prop == "ServerPort")
                            {
                                int port = oldVal is JsonElement je && je.ValueKind == JsonValueKind.Number ? je.GetInt32() : Convert.ToInt32(oldVal);
                                newVal = AnsiConsole.Ask<int>($"{prop}", port);
                            }
                            else if (prop == "UseHttps" || prop == "EnableChannelTest")
                            {
                                bool bval = oldVal is JsonElement je && je.ValueKind == JsonValueKind.True ? true : (oldVal is JsonElement je2 && je2.ValueKind == JsonValueKind.False ? false : Convert.ToBoolean(oldVal));
                                newVal = AnsiConsole.Confirm($"{prop}", bval);
                            }
                            else
                            {
                                string sval = oldVal is JsonElement je && je.ValueKind == JsonValueKind.String ? je.GetString() : oldVal?.ToString() ?? "";
                                newVal = AnsiConsole.Ask<string>($"{prop}", sval);
                            }
                            ((IDictionary<string, object>)config)[prop] = newVal;
                            dirty = true;
                        }
                    }
                }
                else if (choice == Resources.Save_Exit)
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    var jsonOut = JsonSerializer.Serialize(config, options);
                    File.WriteAllText(configFile, jsonOut);
                    AnsiConsole.MarkupLine($"[green]{Resources.Saved}[/]");
                    running = false;
                }
                else if (choice == Resources.Exit)
                {
                    running = false;
                }
            }
        }
    }
}
