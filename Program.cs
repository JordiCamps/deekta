using System.Threading;
using System.Windows.Forms;

namespace Deekta;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, SingleInstance.MutexName, out bool createdNew);
        if (!createdNew)
        {
            // Another instance is already running: ask it to show Settings, then exit.
            SingleInstance.SignalShowSettings();
            return;
        }

        Logger.Init();
        Localization.Init();
        Logger.Info("deekta starting.");

        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => Logger.Error("Unhandled UI exception.", e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Logger.Error("Unhandled domain exception.", e.ExceptionObject as Exception);

        try
        {
            Application.Run(new TrayAppContext());
        }
        finally
        {
            Logger.Info("deekta exiting.");
            GC.KeepAlive(mutex);
        }
    }
}
