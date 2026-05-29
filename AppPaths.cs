namespace Deekta;

/// <summary>
/// Centralises the on-disk locations deekta uses, creating them on demand.
/// Settings/logs live under %AppData%\deekta; temporary audio under %TEMP%\deekta.
/// </summary>
internal static class AppPaths
{
    public const string AppFolderName = "deekta";

    /// <summary>Pre-rename folder name, used once to migrate existing settings.</summary>
    public const string LegacyAppFolderName = "DictaCAT";

    public static string AppDataDir
    {
        get
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                AppFolderName);
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string SettingsFile => Path.Combine(AppDataDir, "settings.json");

    /// <summary>Path to the old %AppData%\DictaCAT\settings.json, if it still exists.</summary>
    public static string LegacySettingsFile => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        LegacyAppFolderName, "settings.json");

    public static string LogsDir
    {
        get
        {
            string dir = Path.Combine(AppDataDir, "logs");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string TempAudioDir
    {
        get
        {
            string dir = Path.Combine(Path.GetTempPath(), AppFolderName);
            Directory.CreateDirectory(dir);
            return dir;
        }
    }
}
