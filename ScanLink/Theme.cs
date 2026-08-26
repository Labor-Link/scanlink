using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ScanLink
{
    /// <summary>
    /// ScanLink v2 design tokens — the single source of truth for colour, type and metrics.
    /// A direct port of the design system's tokens/product.css; names track the CSS custom
    /// properties so the two can be diffed by eye.
    ///
    /// Rules: never write a literal Color.FromArgb in UI code again — add a token here.
    /// Prefer the semantic aliases (ActionPrimary, SurfaceCard, TextMuted) over raw ramps.
    /// </summary>
    internal static class Theme
    {
        // Primary: logo indigo, promoted out of the shield to become the action colour
        public static readonly Color Indigo50 = Rgb(0xEF, 0xEF, 0xFD);
        public static readonly Color Indigo100 = Rgb(0xDE, 0xDD, 0xFB);
        public static readonly Color Indigo200 = Rgb(0xBF, 0xBD, 0xF6);
        public static readonly Color Indigo500 = Rgb(0x4D, 0x4A, 0xEA);
        public static readonly Color Indigo600 = Rgb(0x3F, 0x3C, 0xD4);
        public static readonly Color Indigo700 = Rgb(0x35, 0x32, 0xB8);
        public static readonly Color Indigo900 = Rgb(0x22, 0x20, 0x8C);

        // Navy: application shell and headings
        public static readonly Color Navy600 = Rgb(0x32, 0x4A, 0x5F);
        public static readonly Color Navy800 = Rgb(0x22, 0x34, 0x4D);
        public static readonly Color Navy900 = Rgb(0x1B, 0x2A, 0x41);
        public static readonly Color Navy950 = Rgb(0x12, 0x1D, 0x2E);

        // Neutral ramp
        public static readonly Color N0 = Rgb(0xFF, 0xFF, 0xFF);
        public static readonly Color N25 = Rgb(0xFC, 0xFC, 0xFD);
        public static readonly Color N50 = Rgb(0xF7, 0xF8, 0xFA);
        public static readonly Color N100 = Rgb(0xF1, 0xF3, 0xF7);
        public static readonly Color N200 = Rgb(0xE4, 0xE7, 0xEC);
        public static readonly Color N300 = Rgb(0xD0, 0xD5, 0xDD);
        public static readonly Color N400 = Rgb(0x98, 0xA2, 0xB3);
        public static readonly Color N500 = Rgb(0x66, 0x70, 0x85);
        public static readonly Color N600 = Rgb(0x47, 0x54, 0x67);
        public static readonly Color N800 = Rgb(0x1D, 0x29, 0x39);
        public static readonly Color N900 = Rgb(0x10, 0x18, 0x28);

        // Status: each pairs a light ground with darker readable ink
        public static readonly Color Ok50 = Rgb(0xE9, 0xF7, 0xEF);
        public static readonly Color Ok500 = Rgb(0x12, 0x87, 0x5A);
        public static readonly Color Ok700 = Rgb(0x0C, 0x69, 0x44);
        public static readonly Color Warn50 = Rgb(0xFE, 0xF6, 0xE7);
        public static readonly Color Warn500 = Rgb(0xB5, 0x47, 0x08);
        public static readonly Color Warn700 = Rgb(0x93, 0x37, 0x0D);
        public static readonly Color Err50 = Rgb(0xFD, 0xEC, 0xEC);
        public static readonly Color Err500 = Rgb(0xD3, 0x2F, 0x2F);
        public static readonly Color Err700 = Rgb(0xB4, 0x23, 0x18);
        public static readonly Color Info50 = Rgb(0xEA, 0xF2, 0xFE);
        public static readonly Color Info500 = Rgb(0x17, 0x5C, 0xD3);

        // Semantic aliases — prefer these in UI code
        public static readonly Color ActionPrimary = Indigo500;
        public static readonly Color ActionPrimaryHover = Indigo600;
        public static readonly Color ActionPrimaryActive = Indigo700;
        public static readonly Color ActionSecondary = Navy900;
        public static readonly Color ActionSecondaryHover = Navy950;

        public static readonly Color SurfaceApp = N50;
        public static readonly Color SurfaceCard = N0;
        public static readonly Color SurfaceSunken = N100;
        public static readonly Color SurfaceAltRow = N25;
        public static readonly Color SurfaceShell = Navy900;

        public static readonly Color TextBody = N800;
        public static readonly Color TextHeading = N900;
        public static readonly Color TextMuted = N500;
        public static readonly Color TextLabel = N600;
        public static readonly Color TextOnAccent = N0;
        public static readonly Color TextLink = Indigo600;

        /// <summary>Sidebar ink. WinForms cannot composite text alpha, so the design
        /// system's rgba(255,255,255,.72) is pre-blended against Navy900.</summary>
        public static readonly Color TextOnShell = Rgb(0xC4, 0xC9, 0xD1);
        public static readonly Color TextOnShellStrong = N0;
        /// <summary>Pre-blended rgba(255,255,255,.07) hover wash over Navy900.</summary>
        public static readonly Color SurfaceShellHover = Rgb(0x2A, 0x38, 0x4D);

        public static readonly Color BorderDefault = N200;
        public static readonly Color BorderSubtle = N100;
        public static readonly Color BorderStrong = N300;
        public static readonly Color FocusRing = Indigo500;
        /// <summary>Grid row selection — the indigo wash, not the Windows highlight blue.</summary>
        public static readonly Color GridSelection = Indigo50;

        // Spacing — the strict 4px scale
        public const int S1 = 4;   public const int S2 = 8;   public const int S3 = 12;
        public const int S4 = 16;  public const int S5 = 20;  public const int S6 = 24;
        public const int S8 = 32;  public const int S10 = 40; public const int S12 = 48;
        public const int S16 = 64;

        // Radii
        public const int RadiusXs = 4;  public const int RadiusSm = 6;  public const int RadiusMd = 8;
        public const int RadiusLg = 12; public const int RadiusXl = 16;

        // Control sizing
        public const int HeightSm = 32; public const int HeightMd = 38; public const int HeightLg = 44;
        public const int SidebarWidth = 236;
        public const int TopBarHeight = 60;
        public const int StatusBarHeight = 32;
        // Kept tight: operators watch this grid all shift and v1 ran 28px rows.
        public const int GridRowHeight = 36;
        public const int GridHeaderHeight = 36;

        // Type. The CSS scale is px; WinForms is points. At 96dpi pt = px * 0.75.
        private const string FamilyUi = "Segoe UI";
        private const string FamilyMono = "Consolas";

        public static readonly Font FontXs = Ui(9.0f);            // 12px
        public static readonly Font FontXsBold = Ui(9.0f, FontStyle.Bold);
        public static readonly Font FontSm = Ui(9.75f);           // 13px
        public static readonly Font FontSmBold = Ui(9.75f, FontStyle.Bold);
        public static readonly Font FontMd = Ui(10.5f);           // 14px
        public static readonly Font FontMdBold = Ui(10.5f, FontStyle.Bold);
        public static readonly Font FontLg = Ui(12.0f);           // 16px
        public static readonly Font FontLgBold = Ui(12.0f, FontStyle.Bold);
        public static readonly Font FontXl = Ui(13.5f);           // 18px
        public static readonly Font Font2Xl = Ui(15.0f, FontStyle.Bold);  // 20px
        public static readonly Font Font3Xl = Ui(18.0f, FontStyle.Bold);  // 24px
        public static readonly Font Font4Xl = Ui(22.5f, FontStyle.Bold);  // 30px
        public static readonly Font FontTableHeader = Ui(9.0f, FontStyle.Bold);
        public static readonly Font FontMono = Mono(9.0f);
        public static readonly Font FontMonoSm = Mono(8.25f);

        private static Color Rgb(int r, int g, int b) { return Color.FromArgb(255, r, g, b); }
        private static Font Ui(float pt) { return Ui(pt, FontStyle.Regular); }
        private static Font Ui(float pt, FontStyle style) { return new Font(FamilyUi, pt, style, GraphicsUnit.Point); }
        private static Font Mono(float pt) { return new Font(FamilyMono, pt, FontStyle.Regular, GraphicsUnit.Point); }

        /// <summary>Rounded-rectangle path, radius clamped so small controls cannot
        /// produce a self-intersecting path.</summary>
        public static GraphicsPath RoundedPath(Rectangle bounds, int radius)
        {
            int max = Math.Min(bounds.Width, bounds.Height) / 2;
            if (radius > max) radius = max;

            GraphicsPath path = new GraphicsPath();
            if (radius <= 0) { path.AddRectangle(bounds); return path; }

            int d = radius * 2;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>Blends fore over back. WinForms controls do not composite, so
        /// translucent design tokens must be resolved to opaque colours first.</summary>
        public static Color Blend(Color fore, Color back, double alpha)
        {
            if (alpha < 0) alpha = 0;
            if (alpha > 1) alpha = 1;
            int r = (int)Math.Round(fore.R * alpha + back.R * (1 - alpha));
            int g = (int)Math.Round(fore.G * alpha + back.G * (1 - alpha));
            int b = (int)Math.Round(fore.B * alpha + back.B * (1 - alpha));
            return Color.FromArgb(255, r, g, b);
        }

        /// <summary>Status-strip ink for a tone, mirroring the StatusBar component.</summary>
        public static Color StatusInk(string tone)
        {
            if (string.IsNullOrEmpty(tone)) return TextMuted;
            switch (tone.ToLowerInvariant())
            {
                case "success": return Ok500;
                case "warning": return Warn500;
                case "error": return Err500;
                case "info": return Info500;
                default: return TextMuted;
            }
        }
    }
}
