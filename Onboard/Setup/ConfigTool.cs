using Spectre.Console;
using System.Text.Json;
using System.Globalization;
using LteCar.Onboard.Setup;
using Microsoft.Extensions.DependencyInjection;

namespace Setup
{
    public interface IPage
    {
        public string Title { get; }
        /// <summary>
        /// Renders the page and returns the next page to navigate to.
        /// </summary>
        IPage? Render();
    }

    public abstract class NavigationPageBase : IPage
    {
        private readonly IPage _parent;

        public virtual bool IsRoot => _parent == null;
        public virtual string Title => "Navigation Page: " + GetType().Name;

        protected abstract Dictionary<string, Type> GetSelectablePages();
        public NavigationPageBase(IPage parent)
        {
            this._parent=parent;
        }

        public IPage? Render()
        {
            var pages = GetSelectablePages();
            AnsiConsole.MarkupLine($"[yellow]{Title}[/]");
            var selection = AnsiConsole.Prompt<string>(new SelectionPrompt<string>()
                .AddChoices<string>(pages.Keys.Concat(IsRoot ? Array.Empty<string>() : new[] { "Back" }).ToArray()));

            switch (selection)
            {
                case "Back":
                    return _parent;
                default:
                    return pages[selection];
            }
        }
    }

    public class MainPage : NavigationPageBase
    {
        public MainPage() : base(null)
        {
        }

        protected override Dictionary<string, IPage> GetSelectablePages()
            => new Dictionary<string, IPage> {
                { "Set Template", new LoadTemplatePage() }
            };

    }

    public class LoadTemplatePage : IPage
    {
        public string Title => throw new NotImplementedException();

        public IPage? Render()
        {
            throw new NotImplementedException();
        }
    }
    public class SaveTemplatePage : IPage
    {
        public string Title => throw new NotImplementedException();

        public IPage? Render()
        {
            throw new NotImplementedException();
        }
    }

    public static class ConfigTool
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        public static void Run(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            // Detect or set culture (default: system, fallback: en)
            var culture = CultureInfo.CurrentUICulture;
            if (Environment.GetEnvironmentVariable("LC_ALL") == "de_DE"
                || Environment.GetEnvironmentVariable("LANG") == "de_DE"
                || Environment.GetEnvironmentVariable("LANG") == "de_DE.UTF-8")
                culture = new CultureInfo("de");
            // Optionally: allow override by env var or argument

            // Set culture for SetupTexts
            SetupTexts.Culture = culture;

            AnsiConsole.MarkupLine($"[bold yellow]{SetupTexts.Title}[/]");
            var configFile = "appSettings.json";
            dynamic? config = null;
            if (File.Exists(configFile))
            {
                var json = File.ReadAllText(configFile);
                config = JsonSerializer.Deserialize<dynamic>(json);
                AnsiConsole.MarkupLine($"[green]{SetupTexts.LoadConfig}[/]");
            }
            else
            {
                config = new System.Dynamic.ExpandoObject();
                config.CarName = culture.TwoLetterISOLanguageName == "de" ? "MeinAuto" : "MyCar";
                config.EnableChannelTest = false;
                AnsiConsole.MarkupLine($"[red]{SetupTexts.NoConfig}");
            }

            bool running = true;
            bool dirty = false;
            while (running)
            {
                var carName = config.CarName ?? (culture.TwoLetterISOLanguageName == "de" ? "MeinAuto" : "MyCar");
                var enableTest = config.EnableChannelTest ?? false;
                var menu = new[] {
                    $"{SetupTexts.SetCarName} {string.Format(SetupTexts.CurrentValue, carName)}",
                    $"{SetupTexts.SetEnabletest} {string.Format(SetupTexts.CurrentValue, enableTest)}",
                    "[root] appSettings.json bearbeiten",
                    SetupTexts.SaveExit,
                    SetupTexts.ExitWithoutSaving,
                };
                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title($"[bold]{SetupTexts.MainMenu}[/]")
                        .AddChoices(menu)
                );

                if (choice.StartsWith(SetupTexts.SetCarName))
                {
                    var newName = AnsiConsole.Ask<string>(SetupTexts.CarnamePrompt, carName);
                    if (newName != carName) { config.CarName = newName; dirty = true; }
                }
                else if (choice.StartsWith(SetupTexts.SetEnabletest))
                {
                    var newVal = AnsiConsole.Confirm(SetupTexts.EnabletestPrompt, enableTest);
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
                        subMenu.Add(SetupTexts.SaveExit);
                        subMenu.Add(SetupTexts.ExitWithoutSaving);
                        var subChoice = AnsiConsole.Prompt(
                            new SelectionPrompt<string>()
                                .Title("[bold]appSettings.json root bearbeiten[/]")
                                .AddChoices(subMenu)
                        );
                        if (subChoice == SetupTexts.SaveExit)
                        {
                            var options = new JsonSerializerOptions { WriteIndented = true };
                            var jsonOut = JsonSerializer.Serialize(config, options);
                            File.WriteAllText(configFile, jsonOut);
                            AnsiConsole.MarkupLine($"[green]{SetupTexts.Saved}[/]");
                            dirty = false;
                            subRunning = false;
                        }
                        else if (subChoice == SetupTexts.ExitWithoutSaving)
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
                else if (choice == SetupTexts.SaveExit)
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    var jsonOut = JsonSerializer.Serialize(config, options);
                    File.WriteAllText(configFile, jsonOut);
                    AnsiConsole.MarkupLine($"[green]{SetupTexts.Saved}[/]");
                    running = false;
                }
                else if (choice == SetupTexts.ExitWithoutSaving)
                {
                    running = false;
                }
            }
        }
    }
}
