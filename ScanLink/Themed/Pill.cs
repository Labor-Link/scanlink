using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ScanLink.Themed
{
    /// <summary>
    /// A small rounded status pill with an optional leading dot — the "Live" indicator in
    /// the Scans header, and the same shape the design system uses for row status.
    ///
    /// Owner-drawn in one pass rather than assembled from a Label inside a rounded Panel:
    /// a Region-clipped panel stair-steps at this size and shows whatever sits behind it.
    /// </summary>
    internal class Pill : Control
    {
        private Color _ground = Theme.Indigo50;
        private Color _ink = Theme.Indigo700;
        private bool _showDot = true;

        public Pill(string text)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.UserPaint
                     | ControlStyles.ResizeRedraw
                     | ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
            Font = Theme.FontXsBold;
            Height = 26;
            Text = text ?? string.Empty;
            Fit();
        }

        public Color Ground { get { return _ground; } set { _ground = value; Invalidate(); } }
        public Color Ink { get { return _ink; } set { _ink = value; Invalidate(); } }
        public bool ShowDot { get { return _showDot; } set { _showDot = value; Fit(); Invalidate(); } }

        /// <summary>Applies one of the semantic tones from the design system.</summary>
        public void SetTone(string tone)
        {
            switch ((tone ?? string.Empty).ToLowerInvariant())
            {
                case "success": _ground = Theme.Ok50; _ink = Theme.Ok700; break;
                case "warning": _ground = Theme.Warn50; _ink = Theme.Warn700; break;
                case "error": _ground = Theme.Err50; _ink = Theme.Err700; break;
                case "info": _ground = Theme.Info50; _ink = Theme.Info500; break;
                default: _ground = Theme.Indigo50; _ink = Theme.Indigo700; break;
            }
            Invalidate();
        }

        private void Fit()
        {
            try
            {
                int text = TextRenderer.MeasureText(Text ?? string.Empty, Font).Width;
                Width = text + Theme.S3 * 2 + (_showDot ? Theme.S3 : 0);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[PILL] could not measure '" + Text + "': " + ex.Message);
                Width = 72;
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Fit();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = Theme.RoundedPath(bounds, Height / 2))
            using (SolidBrush fill = new SolidBrush(_ground))
            {
                g.FillPath(fill, path);
            }

            int textLeft = Theme.S3;
            if (_showDot)
            {
                using (SolidBrush dot = new SolidBrush(_ink))
                {
                    g.FillEllipse(dot, Theme.S3, Height / 2 - 3, 6, 6);
                }
                textLeft += Theme.S3;
            }

            TextRenderer.DrawText(g, Text, Font,
                new Rectangle(textLeft, 0, Width - textLeft - Theme.S2, Height),
                _ink, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
        }
    }
}
