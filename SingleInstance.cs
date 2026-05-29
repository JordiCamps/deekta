using System.Threading;

namespace Deekta;

/// <summary>
/// Single-instance coordination. The first instance owns a named mutex and a named, auto-reset
/// event; a second launch detects the mutex is taken, signals the event (asking the running
/// instance to surface its Settings window) and exits. Names are machine/session-scoped GUIDs.
/// </summary>
internal static class SingleInstance
{
    public const string MutexName =
        "deekta_SingleInstance_Mutex_{B1F0D3A6-3C2E-4E0B-9A7C-6F5D2E1A8C90}";

    private const string ShowSettingsEventName =
        "deekta_ShowSettings_Event_{D4A2C7E1-9B5F-4A3D-8E6C-1F0B2D3A4C5E}";

    /// <summary>Creates the event the running instance waits on. Owned for the app lifetime.</summary>
    public static EventWaitHandle CreateShowSettingsSignal() =>
        new(initialState: false, EventResetMode.AutoReset, ShowSettingsEventName);

    /// <summary>
    /// Signals an already-running instance to show its Settings window.
    /// Returns false if no running instance was found.
    /// </summary>
    public static bool SignalShowSettings()
    {
        try
        {
            if (EventWaitHandle.TryOpenExisting(ShowSettingsEventName, out EventWaitHandle? handle))
            {
                using (handle)
                {
                    handle.Set();
                }
                return true;
            }
        }
        catch
        {
            // Best-effort; nothing else to do if signalling fails.
        }
        return false;
    }
}
