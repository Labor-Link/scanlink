using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ScanLink.DesignSystem
{
    /// <summary>
    /// The mockup's StatusBar (StatusBar.js): a quiet 36px footer strip for ambient state —
    /// white, 1px top border, 24px side padding, a 7px dot and 12px text tinted by tone, an
    /// optional muted note on the right. Anything the user must act on belongs in an SLBanner.
    ///   Tone: Neutral = ready (grey), Info, Success, Warning, Error.
    /// </summary>
    [DesignerCategory("Code")]
    internal class SLStatusBar : SLControl
    {
        public const int BarHeight = 36;
        private SLTone _tone = SLTone.Neutral;
        private string _right;

        public SLStatusBar()
        {
            BackColor = Theme.SurfaceCard;
            Height = BarHeight;
        }

        public SLTone Tone { get { return _tone; } set { _tone = value; Invalidate(); } }

        /// <summary>Muted text at the right edge (e.g. "Last synced 2 min ago").</summary>
        public string RightText { get { return _right; } set { _right = value; Invalidate(); } }

        private static void Colors(SLTone tone, out Color fg, out Color dot)
        {
            switch (tone)
            {
                case SLTone.Info: fg = Theme.Info500; dot = Theme.Info500; break;
                case SLTone.Success: fg = Theme.Ok700; dot = Theme.Ok500; break;
                case SLTone.Warning: fg = Theme.Warn700; dot = Theme.Warn500; break;
                case SLTone.Error: fg = Theme.Err700; dot = Theme.Err500; break;
                default: fg = Theme.TextMuted; dot = Theme.N400; break;
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (SolidBrush b = new SolidBrush(Theme.SurfaceCard)) e.Graphics.FillRectangle(b, ClientRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            SLPaint.HLine(g, Theme.BorderDefault, 0, Width, 0);

            Color fg, dot;
            Colors(_tone, out fg, out dot);
            int rightW = string.IsNullOrEmpty(_right) ? 0 : SLPaint.Measure(_right, Theme.FontXs).Width + 1;
            if (rightW > 0)
                SLPaint.Text(g, _right, Theme.FontXs, new Rectangle(Width - 24 - rightW, 1, rightW, Height - 1), Theme.TextMuted, TextFormatFlags.Left);

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (SolidBrush b = new SolidBrush(dot)) g.FillEllipse(b, 24, (Height - 7) / 2f + 0.5f, 7, 7);
            int x = 24 + 7 + 10;
            SLPaint.TextEllipsis(g, Text, Theme.FontXs, new Rectangle(x, 1, Math.Max(10, Width - x - 24 - (rightW > 0 ? rightW + 16 : 0)), Height - 1), fg, TextFormatFlags.Left);
        }
    }
}
