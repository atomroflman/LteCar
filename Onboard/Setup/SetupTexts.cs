using System.Globalization;

namespace LteCar.Onboard.Setup;

public enum Language
{
    German,
    English
}

public static class SetupTexts
{
    private static Language _currentLanguage = Language.German;
    
    public static Language CurrentLanguage
    {
        get => _currentLanguage;
        set => _currentLanguage = value;
    }

    // Main Menu
    public static string MainMenuTitle => CurrentLanguage switch
    {
        Language.German => "Fahrzeug-Setup",
        Language.English => "Vehicle Setup",
        _ => "Fahrzeug-Setup"
    };

    public static string MainMenuConfigureControl => CurrentLanguage switch
    {
        Language.German => "🔧 Control-Channels konfigurieren",
        Language.English => "🔧 Configure Control Channels",
        _ => "🔧 Control-Channels konfigurieren"
    };

    public static string MainMenuConfigureTelemetry => CurrentLanguage switch
    {
        Language.German => "📡 Telemetrie-Channels konfigurieren",
        Language.English => "📡 Configure Telemetry Channels",
        _ => "📡 Telemetrie-Channels konfigurieren"
    };

    public static string MainMenuConfigureVideo => CurrentLanguage switch
    {
        Language.German => "📹 Video-Streams konfigurieren",
        Language.English => "📹 Configure Video Streams",
        _ => "📹 Video-Streams konfigurieren"
    };

    public static string MainMenuLoadTemplate => CurrentLanguage switch
    {
        Language.German => "📋 Vorlage laden und anwenden",
        Language.English => "📋 Load and Apply Template",
        _ => "📋 Vorlage laden und anwenden"
    };

    public static string MainMenuSaveTemplate => CurrentLanguage switch
    {
        Language.German => "💾 Aktuelle Konfiguration als Vorlage speichern",
        Language.English => "💾 Save Current Configuration as Template",
        _ => "💾 Aktuelle Konfiguration als Vorlage speichern"
    };

    public static string MainMenuTemplateManager => CurrentLanguage switch
    {
        Language.German => "🔧 Template-Manager",
        Language.English => "🔧 Template Manager",
        _ => "🔧 Template-Manager"
    };

    public static string MainMenuTestChannels => CurrentLanguage switch
    {
        Language.German => "🧪 Channel-Konfiguration testen",
        Language.English => "🧪 Test Channel Configuration",
        _ => "🧪 Channel-Konfiguration testen"
    };

    public static string MainMenuSaveConfiguration => CurrentLanguage switch
    {
        Language.German => "💾 Konfiguration speichern",
        Language.English => "💾 Save Configuration",
        _ => "💾 Konfiguration speichern"
    };

    public static string MainMenuChangeLanguage => CurrentLanguage switch
    {
        Language.German => "🌍 Sprache ändern",
        Language.English => "🌍 Change Language",
        _ => "🌍 Sprache ändern"
    };

    public static string MainMenuExit => CurrentLanguage switch
    {
        Language.German => "❌ Beenden",
        Language.English => "❌ Exit",
        _ => "❌ Beenden"
    };

    // General
    public static string WhatToDo => CurrentLanguage switch
    {
        Language.German => "Was möchten Sie [green]tun[/]?",
        Language.English => "What would you like to [green]do[/]?",
        _ => "Was möchten Sie [green]tun[/]?"
    };

    public static string PressAnyKey => CurrentLanguage switch
    {
        Language.German => "Drücken Sie eine beliebige Taste...",
        Language.English => "Press any key...",
        _ => "Drücken Sie eine beliebige Taste..."
    };

    public static string Cancel => CurrentLanguage switch
    {
        Language.German => "❌ Abbrechen",
        Language.English => "❌ Cancel",
        _ => "❌ Abbrechen"
    };

    public static string Back => CurrentLanguage switch
    {
        Language.German => "❌ Zurück zum Hauptmenü",
        Language.English => "❌ Back to Main Menu",
        _ => "❌ Zurück zum Hauptmenü"
    };

    public static string Success => CurrentLanguage switch
    {
        Language.German => "✅ Erfolgreich",
        Language.English => "✅ Success",
        _ => "✅ Erfolgreich"
    };

    public static string Error => CurrentLanguage switch
    {
        Language.German => "❌ Fehler",
        Language.English => "❌ Error",
        _ => "❌ Fehler"
    };

    public static string Warning => CurrentLanguage switch
    {
        Language.German => "⚠️ Warnung",
        Language.English => "⚠️ Warning",
        _ => "⚠️ Warnung"
    };

    // Configuration Status
    public static string NoConfigurationFound => CurrentLanguage switch
    {
        Language.German => "Es wurde keine Konfiguration gefunden.",
        Language.English => "No configuration found.",
        _ => "Es wurde keine Konfiguration gefunden."
    };

    public static string UseTemplate => CurrentLanguage switch
    {
        Language.German => "Möchten Sie eine Vorlage verwenden?",
        Language.English => "Would you like to use a template?",
        _ => "Möchten Sie eine Vorlage verwenden?"
    };

    public static string ContinueWithoutTemplate => CurrentLanguage switch
    {
        Language.German => "Drücken Sie eine beliebige Taste um fortzufahren...",
        Language.English => "Press any key to continue...",
        _ => "Drücken Sie eine beliebige Taste um fortzufahren..."
    };

    public static string ConfigurationLoaded => CurrentLanguage switch
    {
        Language.German => "[green]✓ Konfiguration geladen[/]",
        Language.English => "[green]✓ Configuration loaded[/]",
        _ => "[green]✓ Konfiguration geladen[/]"
    };

    public static string ConfigurationSaved => CurrentLanguage switch
    {
        Language.German => "[green]✓ Konfiguration gespeichert[/]",
        Language.English => "[green]✓ Configuration saved[/]",
        _ => "[green]✓ Konfiguration gespeichert[/]"
    };

    // Vehicle Basics
    public static string VehicleBasicsTitle => CurrentLanguage switch
    {
        Language.German => "Fahrzeug-Grundeinstellungen",
        Language.English => "Vehicle Basic Settings",
        _ => "Fahrzeug-Grundeinstellungen"
    };

    public static string VehicleId => CurrentLanguage switch
    {
        Language.German => "Fahrzeug-ID:",
        Language.English => "Vehicle ID:",
        _ => "Fahrzeug-ID:"
    };

    public static string VehicleName => CurrentLanguage switch
    {
        Language.German => "Fahrzeug-Name:",
        Language.English => "Vehicle Name:",
        _ => "Fahrzeug-Name:"
    };

    public static string ChangeVehiclePassword => CurrentLanguage switch
    {
        Language.German => "Fahrzeug-Passwort ändern?",
        Language.English => "Change vehicle password?",
        _ => "Fahrzeug-Passwort ändern?"
    };

    public static string NewPassword => CurrentLanguage switch
    {
        Language.German => "Neues Passwort:",
        Language.English => "New password:",
        _ => "Neues Passwort:"
    };

    public static string BasicSettingsUpdated => CurrentLanguage switch
    {
        Language.German => "Grundeinstellungen aktualisiert",
        Language.English => "Basic settings updated",
        _ => "Grundeinstellungen aktualisiert"
    };

    // Hardware Configuration
    public static string HardwareConfigurationTitle => CurrentLanguage switch
    {
        Language.German => "Hardware-Konfiguration",
        Language.English => "Hardware Configuration",
        _ => "Hardware-Konfiguration"
    };

    public static string HardwareOptions => CurrentLanguage switch
    {
        Language.German => "Hardware-Optionen:",
        Language.English => "Hardware Options:",
        _ => "Hardware-Optionen:"
    };

    public static string AddPinManager => CurrentLanguage switch
    {
        Language.German => "Pin-Manager hinzufügen",
        Language.English => "Add Pin Manager",
        _ => "Pin-Manager hinzufügen"
    };

    public static string EditPinManager => CurrentLanguage switch
    {
        Language.German => "Pin-Manager bearbeiten",
        Language.English => "Edit Pin Manager",
        _ => "Pin-Manager bearbeiten"
    };

    public static string RemovePinManager => CurrentLanguage switch
    {
        Language.German => "Pin-Manager entfernen",
        Language.English => "Remove Pin Manager",
        _ => "Pin-Manager entfernen"
    };

    public static string BackToMainMenu => CurrentLanguage switch
    {
        Language.German => "Zurück zum Hauptmenü",
        Language.English => "Back to Main Menu",
        _ => "Zurück zum Hauptmenü"
    };

    public static string PinManagerName => CurrentLanguage switch
    {
        Language.German => "Pin-Manager Name:",
        Language.English => "Pin Manager Name:",
        _ => "Pin-Manager Name:"
    };

    public static string PinManagerType => CurrentLanguage switch
    {
        Language.German => "Pin-Manager Typ:",
        Language.English => "Pin Manager Type:",
        _ => "Pin-Manager Typ:"
    };

    public static string BoardAddress => CurrentLanguage switch
    {
        Language.German => "Board Address (0-127):",
        Language.English => "Board Address (0-127):",
        _ => "Board Address (0-127):"
    };

    public static string I2cBus => CurrentLanguage switch
    {
        Language.German => "I2C Bus (0 oder 1):",
        Language.English => "I2C Bus (0 or 1):",
        _ => "I2C Bus (0 oder 1):"
    };

    public static string PinManagerAdded => CurrentLanguage switch
    {
        Language.German => "Pin-Manager '{0}' hinzugefügt",
        Language.English => "Pin Manager '{0}' added",
        _ => "Pin-Manager '{0}' hinzugefügt"
    };

    public static string NoPinManagersAvailable => CurrentLanguage switch
    {
        Language.German => "Keine Pin-Manager vorhanden",
        Language.English => "No Pin Managers available",
        _ => "Keine Pin-Manager vorhanden"
    };

    public static string SelectPinManager => CurrentLanguage switch
    {
        Language.German => "Pin-Manager auswählen:",
        Language.English => "Select Pin Manager:",
        _ => "Pin-Manager auswählen:"
    };

    public static string EditingNotImplemented => CurrentLanguage switch
    {
        Language.German => "Bearbeitung von '{0}' noch nicht implementiert",
        Language.English => "Editing of '{0}' not yet implemented",
        _ => "Bearbeitung von '{0}' noch nicht implementiert"
    };

    public static string RemovePinManagerTitle => CurrentLanguage switch
    {
        Language.German => "Pin-Manager entfernen:",
        Language.English => "Remove Pin Manager:",
        _ => "Pin-Manager entfernen:"
    };

    // Template Management
    public static string SaveCurrentAsTemplate => CurrentLanguage switch
    {
        Language.German => "💾 Aktuelle Konfiguration als Vorlage speichern",
        Language.English => "💾 Save Current Configuration as Template",
        _ => "💾 Aktuelle Konfiguration als Vorlage speichern"
    };

    public static string NoConfigurationToSave => CurrentLanguage switch
    {
        Language.German => "[red]❌ Keine Konfiguration vorhanden. Es gibt nichts zu speichern.[/]",
        Language.English => "[red]❌ No configuration available. Nothing to save.[/]",
        _ => "[red]❌ Keine Konfiguration vorhanden. Es gibt nichts zu speichern.[/]"
    };

    public static string TemplateName => CurrentLanguage switch
    {
        Language.German => "Name der [green]Vorlage[/]:",
        Language.English => "Template [green]name[/]:",
        _ => "Name der [green]Vorlage[/]:"
    };

    public static string TemplateDescription => CurrentLanguage switch
    {
        Language.German => "Beschreibung der Vorlage:",
        Language.English => "Template description:",
        _ => "Beschreibung der Vorlage:"
    };

    public static string DefaultTemplateDescription => CurrentLanguage switch
    {
        Language.German => "Fahrzeug-Konfiguration erstellt am {0:yyyy-MM-dd}",
        Language.English => "Vehicle configuration created on {0:yyyy-MM-dd}",
        _ => "Fahrzeug-Konfiguration erstellt am {0:yyyy-MM-dd}"
    };

    public static string InvalidTemplateName => CurrentLanguage switch
    {
        Language.German => "[red]Ungültiger Vorlagen-Name[/]",
        Language.English => "[red]Invalid template name[/]",
        _ => "[red]Ungültiger Vorlagen-Name[/]"
    };

    // Template Manager
    public static string TemplateManagerTitle => CurrentLanguage switch
    {
        Language.German => "Template Manager",
        Language.English => "Template Manager",
        _ => "Template Manager"
    };

    public static string ShowTemplates => CurrentLanguage switch
    {
        Language.German => "📋 Vorlagen anzeigen",
        Language.English => "📋 Show Templates",
        _ => "📋 Vorlagen anzeigen"
    };

    public static string DeleteTemplate => CurrentLanguage switch
    {
        Language.German => "🗑️ Vorlage löschen",
        Language.English => "🗑️ Delete Template",
        _ => "🗑️ Vorlage löschen"
    };

    public static string NoTemplatesFolder => CurrentLanguage switch
    {
        Language.German => "[red]Keine Vorlagen-Ordner gefunden[/]",
        Language.English => "[red]No templates folder found[/]",
        _ => "[red]Keine Vorlagen-Ordner gefunden[/]"
    };

    public static string NoTemplatesToDelete => CurrentLanguage switch
    {
        Language.German => "[yellow]Keine Vorlagen zum Löschen gefunden[/]",
        Language.English => "[yellow]No templates found to delete[/]",
        _ => "[yellow]Keine Vorlagen zum Löschen gefunden[/]"
    };

    public static string WhichTemplateToDelete => CurrentLanguage switch
    {
        Language.German => "Welche [red]Vorlage[/] möchten Sie löschen?",
        Language.English => "Which [red]template[/] would you like to delete?",
        _ => "Welche [red]Vorlage[/] möchten Sie löschen?"
    };

    // Channel Testing
    public static string TestingChannelConfiguration => CurrentLanguage switch
    {
        Language.German => "🧪 Channel-Konfiguration wird getestet...",
        Language.English => "🧪 Testing channel configuration...",
        _ => "🧪 Channel-Konfiguration wird getestet..."
    };

    public static string NoConfigurationToTest => CurrentLanguage switch
    {
        Language.German => "[red]❌ Keine Konfiguration vorhanden. Bitte konfigurieren Sie zuerst die Channels.[/]",
        Language.English => "[red]❌ No configuration available. Please configure channels first.[/]",
        _ => "[red]❌ Keine Konfiguration vorhanden. Bitte konfigurieren Sie zuerst die Channels.[/]"
    };

    // Language Selection
    public static string SelectLanguage => CurrentLanguage switch
    {
        Language.German => "Sprache auswählen / Select Language",
        Language.English => "Select Language / Sprache auswählen",
        _ => "Sprache auswählen / Select Language"
    };

    public static string LanguageGerman => CurrentLanguage switch
    {
        Language.German => "🇩🇪 Deutsch",
        Language.English => "🇩🇪 German",
        _ => "🇩🇪 Deutsch"
    };

    public static string LanguageEnglish => CurrentLanguage switch
    {
        Language.German => "🇺🇸 Englisch",
        Language.English => "🇺🇸 English",
        _ => "🇺🇸 Englisch"
    };

    public static string LanguageChanged => CurrentLanguage switch
    {
        Language.German => "[green]✓ Sprache geändert zu Deutsch[/]",
        Language.English => "[green]✓ Language changed to English[/]",
        _ => "[green]✓ Sprache geändert[/]"
    };

    // Pin Manager Configuration
    public static string ConfigurePinManagers => CurrentLanguage switch
    {
        Language.German => "🔧 Pin-Manager konfigurieren",
        Language.English => "🔧 Configure Pin Managers",
        _ => "🔧 Pin-Manager konfigurieren"
    };

    public static string DeletePinManager => CurrentLanguage switch
    {
        Language.German => "🗑️ Pin-Manager löschen",
        Language.English => "🗑️ Delete Pin Manager",
        _ => "🗑️ Pin-Manager löschen"
    };

    public static string NoPinManagers => CurrentLanguage switch
    {
        Language.German => "[yellow]Keine Pin-Manager konfiguriert[/]",
        Language.English => "[yellow]No pin managers configured[/]",
        _ => "[yellow]Keine Pin-Manager konfiguriert[/]"
    };

    // Channel Configuration
    public static string AddChannel => CurrentLanguage switch
    {
        Language.German => "➕ Channel hinzufügen",
        Language.English => "➕ Add Channel",
        _ => "➕ Channel hinzufügen"
    };

    public static string EditChannel => CurrentLanguage switch
    {
        Language.German => "✏️ Channel bearbeiten",
        Language.English => "✏️ Edit Channel",
        _ => "✏️ Channel bearbeiten"
    };

    public static string DeleteChannel => CurrentLanguage switch
    {
        Language.German => "🗑️ Channel löschen",
        Language.English => "🗑️ Delete Channel",
        _ => "🗑️ Channel löschen"
    };

    public static string NoChannels => CurrentLanguage switch
    {
        Language.German => "[yellow]Keine Channels konfiguriert[/]",
        Language.English => "[yellow]No channels configured[/]",
        _ => "[yellow]Keine Channels konfiguriert[/]"
    };

    // Validation Messages
    public static string ValidationFailed => CurrentLanguage switch
    {
        Language.German => "[red]Validierung fehlgeschlagen[/]",
        Language.English => "[red]Validation failed[/]",
        _ => "[red]Validierung fehlgeschlagen[/]"
    };

    public static string ConfigurationValid => CurrentLanguage switch
    {
        Language.German => "[green]✓ Konfiguration ist gültig[/]",
        Language.English => "[green]✓ Configuration is valid[/]",
        _ => "[green]✓ Konfiguration ist gültig[/]"
    };

    // File Operations
    public static string FileNotFound => CurrentLanguage switch
    {
        Language.German => "[red]Datei nicht gefunden[/]",
        Language.English => "[red]File not found[/]",
        _ => "[red]Datei nicht gefunden[/]"
    };

    public static string FileSaved => CurrentLanguage switch
    {
        Language.German => "[green]✓ Datei gespeichert[/]",
        Language.English => "[green]✓ File saved[/]",
        _ => "[green]✓ Datei gespeichert[/]"
    };

    public static string FileLoadError => CurrentLanguage switch
    {
        Language.German => "[red]Fehler beim Laden der Datei[/]",
        Language.English => "[red]Error loading file[/]",
        _ => "[red]Fehler beim Laden der Datei[/]"
    };

    // Confirmation Messages
    public static string OverwriteConfiguration => CurrentLanguage switch
    {
        Language.German => "Vorhandene Konfiguration überschreiben?",
        Language.English => "Overwrite existing configuration?",
        _ => "Vorhandene Konfiguration überschreiben?"
    };

    public static string ConfirmDelete => CurrentLanguage switch
    {
        Language.German => "Sind Sie sicher, dass Sie löschen möchten?",
        Language.English => "Are you sure you want to delete?",
        _ => "Sind Sie sicher, dass Sie löschen möchten?"
    };

    public static string ConfirmExit => CurrentLanguage switch
    {
        Language.German => "Sind Sie sicher, dass Sie beenden möchten?",
        Language.English => "Are you sure you want to exit?",
        _ => "Sind Sie sicher, dass Sie beenden möchten?"
    };

    public static string OperationCancelled => CurrentLanguage switch
    {
        Language.German => "[yellow]Vorgang abgebrochen[/]",
        Language.English => "[yellow]Operation cancelled[/]",
        _ => "[yellow]Vorgang abgebrochen[/]"
    };
}