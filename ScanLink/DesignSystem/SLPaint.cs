using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ScanLink.DesignSystem
{
    /// <summary>
    /// Painting primitives shared by every SL* control. Controls paint themselves completely,
    /// so these replace the old approach of clipping stock controls with a Region (which cannot
    /// anti-alias and leaves jagged corners).
    /// </summary>
    internal static class SLPaint
    {
        /// <summary>Rounded rectangle in float space; radius clamped to half the short side.</summary>
        public static GraphicsPath RoundedRect(RectangleF r, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float max = Math.Min(r.Width, r.Height) / 2f;
            if (radius > max) radius = max;
            if (radius <= 0.01f) { path.AddRectangle(r); return path; }
            float d = radius * 2f;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>
        /// The browser's "background + 1px solid border + border-radius" box. The border is
        /// painted as an outer fill with the inner fill on top, which keeps the 1px line crisp
        /// and evenly anti-aliased at the corners (a 1px pen stroke would straddle pixels).
        /// Pass Color.Empty for border to draw a borderless box.
        /// </summary>
        public static void Box(Graphics g, Rectangle bounds, int radius, Color fill, Color border)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            SmoothingMode old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            PixelOffsetMode oldOffset = g.PixelOffsetMode;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            if (border.IsEmpty || border.A == 0)
            {
                using (GraphicsPath p = RoundedRect(bounds, radius))
                using (SolidBrush b = new SolidBrush(fill))
                    g.FillPath(b, p);
            }
            else
            {
                using (GraphicsPath outer = RoundedRect(bounds, radius))
                using (SolidBrush b = new SolidBrush(border))
                    g.FillPath(b, outer);
                RectangleF inner = new RectangleF(bounds.X + 1, bounds.Y + 1, bounds.Width - 2, bounds.Height - 2);
                using (GraphicsPath p = RoundedRect(inner, Math.Max(0, radius - 1)))
                using (SolidBrush b = new SolidBrush(fill))
                    g.FillPath(b, p);
            }

            g.SmoothingMode = old;
            g.PixelOffsetMode = oldOffset;
        }

        /// <summary>Focus ring: a 3px soft band drawn OUTSIDE box (box must leave room).</summary>
        public static void FocusRing(Graphics g, Rectangle box, int radius)
        {
            int w = Theme.FocusRingWidth;
            Rectangle outer = new Rectangle(box.X - w, box.Y - w, box.Width + w * 2, box.Height + w * 2);
            Box(g, outer, radius + w, Theme.FocusRingSoft, Color.Empty);
        }

        /// <summary>
        /// Soft drop shadow approximating the CSS elevation tokens. Drawn as stacked translucent
        /// rounded rects below box; the caller must leave the spill area inside its bounds.
        /// level 1 = --e-1 (0 1px 2px .06), level 2 = --e-2, level 3 = --e-3.
        /// </summary>
        public static void Shadow(Graphics g, Rectangle box, int radius, int level)
        {
            int offsetY, blur; int alpha;
            switch (level)
            {
                case 1: offsetY = 1; blur = 2; alpha = 15; break;   // .06
                case 2: offsetY = 1; blur = 3; alpha = 26; break;   // .10
                case 3: offsetY = 4; blur = 12; alpha = 20; break;  // .08
                default: return;
            }
            SmoothingMode old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            for (int i = blur; i >= 1; i--)
            {
                int a = Math.Max(1, alpha / blur);
                Rectangle r = new Rectangle(box.X - i + 1, box.Y + offsetY - i + 1, box.Width + 2 * i - 2, box.Height + 2 * i - 2);
                using (GraphicsPath p = RoundedRect(r, radius + i - 1))
                using (SolidBrush b = new SolidBrush(Color.FromArgb(a, Theme.ShadowInk)))
                    g.FillPath(b, p);
            }
            g.SmoothingMode = old;
        }

        /// <summary>
        /// The colour visible behind a control: walks up past Transparent parents. Controls fill
        /// their corners with this before drawing a rounded shape, which is what makes the
        /// corners look transparent.
        /// </summary>
        public static Color BackdropOf(Control control)
        {
            Control child = control;
            Control c = control == null ? null : control.Parent;
            while (c != null)
            {
                ISLSurface surface = c as ISLSurface;
                if (surface != null) return surface.SurfaceFor(child);
                if (c.BackColor.A == 255) return c.BackColor;
                child = c;
                c = c.Parent;
            }
            return Theme.SurfaceApp;
        }

        public const TextFormatFlags SingleLine =
            TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;

        public const TextFormatFlags Wrapped =
            TextFormatFlags.NoPadding | TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl |
            TextFormatFlags.NoPrefix;

        /// <summary>Single-line text, vertically centred in rect.</summary>
        public static void Text(Graphics g, string text, Font font, Rectangle rect, Color color, TextFormatFlags align)
        {
            if (string.IsNullOrEmpty(text)) return;
            TextRenderer.DrawText(g, text, font, rect, color, SingleLine | align);
        }

        /// <summary>Wrapped text from the top-left of rect.</summary>
        public static void Paragraph(Graphics g, string text, Font font, Rectangle rect, Color color)
        {
            if (string.IsNullOrEmpty(text)) return;
            TextRenderer.DrawText(g, text, font, rect, color, Wrapped);
        }

        // Measuring goes through a device context: the MeasureText overloads WITHOUT one ignore
        // NoPadding and add ~6px of overhang padding, which made every button, badge and
        // label-to-asterisk gap 6px wider than the mockup (seen in the CI fidelity report).
        private static Bitmap _measureBitmap;
        private static Graphics _measureGraphics;

        private static Graphics MeasureSurface
        {
            get
            {
                if (_measureGraphics == null)
                {
                    _measureBitmap = new Bitmap(1, 1);
                    _measureGraphics = Graphics.FromImage(_measureBitmap);
                }
                return _measureGraphics;
            }
        }

        public static Size Measure(string text, Font font)
        {
            if (string.IsNullOrEmpty(text)) return Size.Empty;
            return TextRenderer.MeasureText(MeasureSurface, text, font, new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        }

        /// <summary>Height of text wrapped to width, matching Paragraph.</summary>
        public static int MeasureHeight(string text, Font font, int width)
        {
            if (string.IsNullOrEmpty(text) || width <= 0) return 0;
            return TextRenderer.MeasureText(MeasureSurface, text, font, new Size(width, int.MaxValue), Wrapped).Height;
        }

        /// <summary>
        /// CSS line box height for a font size and line-height, e.g. 13px * 1.5 = 20.
        /// Used so stacked text keeps the browser's vertical rhythm instead of GDI's tighter one.
        /// </summary>
        public static int LineBox(Font font, float lineHeight)
        {
            return (int)Math.Round(LineBoxF(font, lineHeight));
        }

        /// <summary>Exact CSS line box (13px * 1.5 = 19.5). Stacked lines accumulate this and
        /// round once, as the browser does, instead of rounding every line.</summary>
        public static float LineBoxF(Font font, float lineHeight)
        {
            return font.SizeInPoints * 96f / 72f * lineHeight;
        }

        public static void HLine(Graphics g, Color color, int x1, int x2, int y)
        {
            using (Pen p = new Pen(color)) g.DrawLine(p, x1, y, x2 - 1, y);
        }
    }
}
