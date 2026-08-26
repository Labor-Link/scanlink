using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ScanLink.Themed
{
    /// <summary>
    /// Pill badge with an optional leading dot — encodes state in form as well as text so
    /// status reads at a glance in a dense grid. Tones always pair a light ground with a
    /// darker readable ink, so the text stays legible rather than relying on colour alone.
    /// </summary>
    internal class BadgeLabel : Control
    {
        public enum BadgeTone { Neutral, Brand, Success, Warning, Error }

        private BadgeTone _tone = BadgeTone.Neutral;
        private bool _showDot = true;

        public BadgeLabel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.UserPaint
                     | ControlStyles.ResizeRedraw
                     | ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
            Font = Theme.FontXsBold;
            Size = new Size(84, 22);
        }

        public BadgeTone Tone { get { return _tone; } set { _tone = value; Invalidate(); } }
        public bool ShowDot { get { return _showDot; } set { _showDot = value; Invalidate(); } }

        private void ResolveTone(out Color ground, out Color ink)
        {
            switch (_tone)
            {
                case BadgeTone.Brand:   ground = Theme.Indigo50; ink = Theme.Indigo700; break;
                case BadgeTone.Success: ground = Theme.Ok50;     ink = Theme.Ok700;     break;
                case BadgeTone.Warning: ground = Theme.Warn50;   ink = Theme.Warn700;   break;
                case BadgeTone.Error:   ground = Theme.Err50;    ink = Theme.Err700;    break;
                default:                ground = Theme.SurfaceSunken; ink = Theme.TextLabel; break;
            }
        }

        /// <summary>Width needed to render the current text without clipping.</summary>
        public int PreferredWidth()
        {
            Size textSize = TextRenderer.MeasureText(Text ?? string.Empty, Font);
            int dot = _showDot ? 14 : 0;
            return textSize.Width + dot + (Theme.S3 * 2);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color ground, ink;
            ResolveTone(out ground, out ink);

            Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            int radius = bounds.Height / 2;   // pill

            using (GraphicsPath path = Theme.RoundedPath(bounds, radius))
            using (SolidBrush fill = new SolidBrush(ground))
            {
                g.FillPath(fill, path);
            }

            int textLeft = Theme.S3;
            if (_showDot)
            {
                int d = 6;
                using (SolidBrush dot = new SolidBrush(ink))
                {
                    g.FillEllipse(dot, Theme.S3, (Height - d) / 2, d, d);
                }
                textLeft = Theme.S3 + d + 5;
            }

            Rectangle textRect = new Rectangle(textLeft, 0, Width - textLeft - Theme.S2, Height);
            TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.Left
                                    | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
            TextRenderer.DrawText(g, Text, Font, textRect, ink, flags);
        }
    }
}
