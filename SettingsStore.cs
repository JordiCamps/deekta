using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Deekta;

/// <summary>
/// Loads and saves <see cref="Settings"/> as JSON in %AppData%\deekta\settings.json.
///
/// The OpenAI API key is never written in plain text. It is encrypted with Windows DPAPI
/// (<see cref="ProtectedData"/>, <see cref="DataProtectionScope.CurrentUser"/>) and stored
/// as a Base64 blob in a separate "ApiKeyProtected" field. DPAPI ties the ciphertext to the
/// current Windows user account, so it cannot be decrypted by other users on the machine.
/// </summary>
internal sealed class SettingsStore
{
    // Extra entropy mixed into the DPAPI blob — a defence-in-depth salt, not a secret.
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("DictaCAT.v1.apikey");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>The decrypted API key for the current session. Never serialised directly.</summary>
    public string ApiKey { get; set; } = string.Empty;

    public Settings Settings { get; set; } = new();

    public SettingsStore Load()
    {
        try
        {
            // Read the current file, or fall back once to the pre-rename %AppData%\DictaCAT
            // file so a renamed install keeps the user's (DPAPI-encrypted) API key.
            string path = AppPaths.SettingsFile;
            bool migrating = false;
            if (!File.Exists(path))
            {
                if (File.Exists(AppPaths.LegacySettingsFile))
                {
                    path = AppPaths.LegacySettingsFile;
                    migrating = true;
                    Logger.Info("Migrating settings from the legacy DictaCAT folder.");
                }
                else
                {
                    Settings = new Settings();
                    ApiKey = string.Empty;
                    return this;
                }
            }

            string json = File.ReadAllText(path, Encoding.UTF8);
            var dto = JsonSerializer.Deserialize<SettingsDto>(json, JsonOptions) ?? new SettingsDto();

            Settings = dto.ToSettings();
            ApiKey = Unprotect(dto.ApiKeyProtected);

            // Persist the migrated settings to the new %AppData%\deekta location so future
            // launches no longer depend on the legacy folder.
            if (migrating)
            {
                Save();
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to load settings; using defaults.", ex);
            Settings = new Settings();
            ApiKey = string.Empty;
        }

        return this;
    }

    public void Save()
    {
        try
        {
            var dto = SettingsDto.FromSettings(Settings);
            dto.ApiKeyProtected = Protect(ApiKey);

            string json = JsonSerializer.Serialize(dto, JsonOptions);
            File.WriteAllText(AppPaths.SettingsFile, json, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to save settings.", ex);
            throw;
        }
    }

    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);

    private static string? Protect(string plain)
    {
        if (string.IsNullOrEmpty(plain))
        {
            return null;
        }

        byte[] cipher = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(plain), Entropy, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(cipher);
    }

    private static string Unprotect(string? protectedBase64)
    {
        if (string.IsNullOrEmpty(protectedBase64))
        {
            return string.Empty;
        }

        try
        {
            byte[] cipher = Convert.FromBase64String(protectedBase64);
            byte[] plain = ProtectedData.Unprotect(cipher, Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plain);
        }
        catch (Exception ex)
        {
            // Blob unreadable (e.g. copied from another user/machine). Treat as no key.
            Logger.Error("Failed to decrypt stored API key.", ex);
            return string.Empty;
        }
    }

    /// <summary>On-disk shape. Separates the protected key from human-editable fields.</summary>
    private sealed class SettingsDto
    {
        public string Model { get; set; } = Settings.DefaultModel;

        // Retained only so older settings.json files still deserialise; no longer used —
        // the language is detected from the active keyboard layout at runtime.
        public string? Language { get; set; }

        public HotkeyCombo Hotkey { get; set; } = HotkeyCombo.Default;
        public bool AutoPaste { get; set; } = true;
        public bool StartWithWindows { get; set; }
        public bool Beep { get; set; } = true;

        /// <summary>Base64 DPAPI blob of the API key (CurrentUser scope). Null when unset.</summary>
        public string? ApiKeyProtected { get; set; }

        public Settings ToSettings() => new()
        {
            Model = string.IsNullOrWhiteSpace(Model) ? Settings.DefaultModel : Model,
            Hotkey = Hotkey ?? HotkeyCombo.Default,
            AutoPaste = AutoPaste,
            StartWithWindows = StartWithWindows,
            Beep = Beep,
        };

        public static SettingsDto FromSettings(Settings s) => new()
        {
            Model = s.Model,
            Hotkey = s.Hotkey,
            AutoPaste = s.AutoPaste,
            StartWithWindows = s.StartWithWindows,
            Beep = s.Beep,
        };
    }
}
