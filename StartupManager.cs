using System.Windows.Forms;
using Microsoft.Win32;

namespace Deekta;

/// <summary>
/// Toggles "start with Windows" by writing the executable path to the per-user Run key
/// (HKCU\Software\Microsoft\Windows\CurrentVersion\Run). Per-user scope needs no admin rights.
/// </summary>
internal static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "deekta";

    public static bool IsEnabled()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            return key?.GetValue(ValueName) is not null;
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to read startup registry value.", ex);
            return false;
        }
    }

    public static void SetEnabled(bool enabled)
    {
        try
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
                ?? throw new InvalidOperationException("Could not open the Run registry key.");

            if (enabled)
            {
                key.SetValue(ValueName, $"\"{ExecutablePath}\"");
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to update startup registry value.", ex);
            throw;
        }
    }

    private static string ExecutablePath =>
        Environment.ProcessPath ?? Application.ExecutablePath;
}
