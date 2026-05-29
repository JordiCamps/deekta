using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace Deekta;

/// <summary>
/// Generates the app/tray icons at runtime (no embedded .ico): a small microphone glyph.
/// Grey while idle, red while recording. The same glyph (with an accent colour) is used as
/// the Settings window icon.
/// </summary>
internal static class TrayIconFactory
{
    public static Icon CreateIdle() => CreateMic(Color.FromArgb(225, 225, 230));

    public static Icon CreateRecording() => CreateMic(Color.FromArgb(225, 60, 55));

    /// <summary>Window icon: a microphone in the app accent colour.</summary>
    public static Icon CreateAppIcon() => CreateMic(Color.FromArgb(80, 150, 240));

    private static Icon CreateMic(Color color)
    {
        const int size = 32;
        using var bmp = new Bitmap(size, size);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            // White halo first so the glyph stays visible on light and dark taskbars.
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

            // Microphone capsule (a vertical pill).
            var capsule = new RectangleF(11.5f, 3.5f, 9f, 15f);

            // Cradle arc (the U that holds the capsule) + stem + base.
            var cradle = new RectangleF(7.5f, 7.5f, 17f, 16f);
            void DrawStand(Pen p)
            {
                g.DrawArc(p, cradle, 25, 130);
                g.DrawLine(p, 16f, 23.5f, 16f, 27.5f);   // stem
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

    /// <summary>Builds a vertical pill (capsule) path from a bounding rectangle.</summary>
    private static GraphicsPath Pill(RectangleF r)
    {
        var path = new GraphicsPath();
        float d = r.Width; // diameter of the rounded ends
        path.AddArc(r.Left, r.Top, d, d, 180, 180);              // top cap
        path.AddArc(r.Left, r.Bottom - d, d, d, 0, 180);         // bottom cap
        path.CloseFigure();
        return path;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr handle);
}
