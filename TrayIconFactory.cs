using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace Deekta;

/// <summary>
/// Generates the app/tray icons at runtime (no embedded .ico): a small microphone glyph.
/// Grey while idle, red while recording, accent blue for the app/window icon. The glyph is
/// drawn against a 32×32 coordinate space and scaled for larger sizes.
/// </summary>
internal static class TrayIconFactory
{
    private static readonly Color Accent = Color.FromArgb(80, 150, 240);

    public static Icon CreateIdle() => CreateMicIcon(Color.FromArgb(225, 225, 230));

    public static Icon CreateRecording() => CreateMicIcon(Color.FromArgb(225, 60, 55));

    /// <summary>Window icon: a microphone in the app accent colour.</summary>
    public static Icon CreateAppIcon() => CreateMicIcon(Accent);

    /// <summary>A larger microphone bitmap (accent colour) for the branding panel.</summary>
    public static Bitmap CreateAppBitmap(int size)
    {
        var bmp = new Bitmap(size, size);
        using Graphics g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        g.ScaleTransform(size / 32f, size / 32f);
        DrawMic(g, Accent);
        return bmp;
    }

    private static Icon CreateMicIcon(Color color)
    {
        using var bmp = new Bitmap(32, 32);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            DrawMic(g, color);
        }

        // Icon.FromHandle borrows the GDI handle; clone to an owned icon, then free the handle.
        IntPtr hIcon = bmp.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(hIcon);
            return (Icon)temp.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    /// <summary>Draws the microphone glyph in a 32×32 coordinate space.</summary>
    private static void DrawMic(Graphics g, Color color)
    {
        using var halo = new Pen(Color.FromArgb(160, 255, 255, 255), 4f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round,
        };
        using var stroke = new Pen(color, 2.4f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round,
        };
        using var fill = new SolidBrush(color);
        using var haloBrush = new SolidBrush(Color.FromArgb(160, 255, 255, 255));

        var capsule = new RectangleF(11.5f, 3.5f, 9f, 15f);
        var cradle = new RectangleF(7.5f, 7.5f, 17f, 16f);

        void DrawStand(Pen p)
        {
            g.DrawArc(p, cradle, 25, 130);
            g.DrawLine(p, 16f, 23.5f, 16f, 27.5f);     // stem
            g.DrawLine(p, 11.5f, 27.5f, 20.5f, 27.5f); // base
        }

        // Halo pass.
        using (var capPath = Pill(capsule))
        {
            g.FillPath(haloBrush, capPath);
        }
        DrawStand(halo);

        // Coloured pass.
        using (var capPath = Pill(capsule))
        {
            g.FillPath(fill, capPath);
        }
        DrawStand(stroke);
    }

    /// <summary>Builds a vertical pill (capsule) path from a bounding rectangle.</summary>
    private static GraphicsPath Pill(RectangleF r)
    {
        var path = new GraphicsPath();
        float d = r.Width; // diameter of the rounded ends
        path.AddArc(r.Left, r.Top, d, d, 180, 180);       // top cap
        path.AddArc(r.Left, r.Bottom - d, d, d, 0, 180);  // bottom cap
        path.CloseFigure();
        return path;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr handle);
}
