using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ScanLink.DesignSystem
{
    // Names deliberately mirror the mockup's props so a React usage maps 1:1:
    //   <Button variant="secondary" size="sm">  ->  new SLButton { Variant = SLVariant.Secondary, ButtonSize = SLSize.Sm }
    //   <Badge tone="success" dot>              ->  new SLBadge { Tone = SLTone.Success, Dot = true }

    internal enum SLVariant { Primary, Secondary, Ghost, Navy, Success, Danger, DangerQuiet }

    internal enum SLSize { Sm, Md, Lg }

    internal enum SLTone { Neutral, Brand, Success, Warning, Error, Info }

    internal enum SLDialogTone { Default, Danger, Warning }

    internal enum SLIconButtonVariant { Ghost, Solid, Danger }

    /// <summary>Text roles used across the mockup. Pick a role, never a raw font.</summary>
    internal enum SLTextStyle
    {
        /// <summary>30px / 600 / 1.25, heading ink. Login hero.</summary>
        Display,
        /// <summary>24px / 600 / 1.25, heading ink. Page titles ("Sign in").</summary>
        Heading,
        /// <summary>16px / 600 / 1.3, heading ink. Card and dialog titles.</summary>
        Title,
        /// <summary>14px / 600 / 1.3, heading ink. FieldSet and EmptyState titles.</summary>
        TitleSm,
        /// <summary>14px / 400 / 1.5, body ink. Lead paragraphs.</summary>
        Body,
        /// <summary>13px / 400 / 1.5, body ink. Default UI text, table cells, key/value values.</summary>
        BodySm,
        /// <summary>13px / 600 / 1.4, body ink.</summary>
        BodySmStrong,
        /// <summary>13px / 500 / 1.4, body ink. Names in lists, key/value values.</summary>
        BodySmMedium,
        /// <summary>13px / 400 / 1.5, muted ink. Subtitles and descriptions.</summary>
        Muted,
        /// <summary>12px / 400 / 1.4, muted ink. Hints and captions.</summary>
        Caption,
        /// <summary>13px / 500 / 1.2, label ink. Form field labels.</summary>
        Label,
        /// <summary>13px Consolas, body ink. Serials, ports, addresses.</summary>
        Mono,
        /// <summary>12px Consolas, muted ink. Secondary line under a name.</summary>
        MonoCaption
    }

    internal struct SLTypeSpec
    {
        public Font Font;
        public Color Color;
        public float LineHeight;

        public SLTypeSpec(Font font, Color color, float lineHeight)
        {
            Font = font; Color = color; LineHeight = lineHeight;
        }

        public int LineBox { get { return SLPaint.LineBox(Font, LineHeight); } }
        public float LineBoxF { get { return SLPaint.LineBoxF(Font, LineHeight); } }
    }

    internal static class SLType
    {
        public static SLTypeSpec Of(SLTextStyle style)
        {
            switch (style)
            {
                case SLTextStyle.Display: return new SLTypeSpec(Theme.Font4XlSemibold, Theme.TextHeading, 1.25f);
                case SLTextStyle.Heading: return new SLTypeSpec(Theme.Font3XlSemibold, Theme.TextHeading, 1.25f);
                case SLTextStyle.Title: return new SLTypeSpec(Theme.FontLgSemibold, Theme.TextHeading, 1.3f);
                case SLTextStyle.TitleSm: return new SLTypeSpec(Theme.FontMdSemibold, Theme.TextHeading, 1.3f);
                case SLTextStyle.Body: return new SLTypeSpec(Theme.FontMd, Theme.TextBody, 1.5f);
                case SLTextStyle.BodySm: return new SLTypeSpec(Theme.FontSm, Theme.TextBody, 1.5f);
                case SLTextStyle.BodySmStrong: return new SLTypeSpec(Theme.FontSmSemibold, Theme.TextBody, 1.4f);
                case SLTextStyle.Muted: return new SLTypeSpec(Theme.FontSm, Theme.TextMuted, 1.5f);
                case SLTextStyle.Caption: return new SLTypeSpec(Theme.FontXs, Theme.TextMuted, 1.4f);
                case SLTextStyle.BodySmMedium: return new SLTypeSpec(Theme.FontSmMedium, Theme.TextBody, 1.4f);
                case SLTextStyle.Label: return new SLTypeSpec(Theme.FontSmMedium, Theme.TextLabel, 1.2f);
                case SLTextStyle.Mono: return new SLTypeSpec(Theme.FontMonoBody, Theme.TextBody, 1.4f);
                case SLTextStyle.MonoCaption: return new SLTypeSpec(Theme.FontMonoXs, Theme.TextMuted, 1.3f);
                default: return new SLTypeSpec(Theme.FontSm, Theme.TextBody, 1.5f);
            }
        }
    }

    /// <summary>
    /// A control whose height depends on its width (wrapped text, stacked content).
    /// SLStack, SLCard and SLDialog ask for this instead of trusting Height, which is how
    /// layouts size themselves without hard-coded pixel positions.
    /// </summary>
    internal interface ISLMeasure
    {
        int MeasureHeight(int width);
    }

    /// <summary>
    /// A container that paints different surfaces under different children (a card's white
    /// body vs its #FCFCFD footer band). Children ask it, through SLPaint.BackdropOf, what
    /// colour is behind them so their rounded corners blend in.
    /// </summary>
    internal interface ISLSurface
    {
        Color SurfaceFor(Control child);
    }

    internal static class SLLayout
    {
        /// <summary>Height a child wants at the given width.</summary>
        public static int HeightOf(Control child, int width)
        {
            ISLMeasure m = child as ISLMeasure;
            if (m != null) return m.MeasureHeight(width);
            if (child.AutoSize) return child.GetPreferredSize(new Size(width, 0)).Height;
            return child.Height;
        }

        /// <summary>Width a child wants when not stretched.</summary>
        public static int WidthOf(Control child)
        {
            if (child.AutoSize) return child.GetPreferredSize(Size.Empty).Width;
            return child.Width;
        }

        /// <summary>
        /// Called at the end of a measuring container's OnLayout: when its content height has
        /// changed, the parent re-lays out so it can resize this container. Parents that do not
        /// measure (plain Panels, Forms) are left alone; size those with Dock or SLDialog.
        /// </summary>
        public static void NotifyIfChanged(Control container, int measured, ref int last)
        {
            if (measured == last) return;
            last = measured;
            Control parent = container.Parent;
            if (parent is ISLMeasure || parent is SLDialog) parent.PerformLayout(container, "Bounds");
        }

        /// <summary>Greedy word wrap, measured with the same renderer that draws.</summary>
        public static List<string> Wrap(string text, Font font, int width)
        {
            List<string> lines = new List<string>();
            if (string.IsNullOrEmpty(text)) return lines;
            foreach (string para in text.Replace("\r\n", "\n").Split('\n'))
            {
                if (width <= 0) { lines.Add(para); continue; }
                string[] words = para.Split(' ');
                string line = "";
                foreach (string word in words)
                {
                    string candidate = line.Length == 0 ? word : line + " " + word;
                    if (line.Length > 0 && SLPaint.Measure(candidate, font).Width > width)
                    {
                        lines.Add(line);
                        line = word;
                    }
                    else line = candidate;
                }
                lines.Add(line);
            }
            return lines;
        }

        /// <summary>Draws wrapped text with CSS line-height rhythm; returns the height used.</summary>
        public static int DrawWrapped(Graphics g, string text, SLTypeSpec spec, Rectangle area, TextFormatFlags align)
        {
            return DrawWrapped(g, text, spec.Font, spec.Color, spec.LineBoxF, area, align);
        }

        public static int DrawWrapped(Graphics g, string text, Font font, Color color, float lineBox, Rectangle area, TextFormatFlags align)
        {
            List<string> lines = Wrap(text, font, area.Width);
            for (int i = 0; i < lines.Count; i++)
            {
                int top = area.Y + (int)Math.Round(i * lineBox);
                int bottom = area.Y + (int)Math.Round((i + 1) * lineBox);
                SLPaint.Text(g, lines[i], font, new Rectangle(area.X, top, area.Width, bottom - top), color, align);
            }
            return (int)Math.Round(lines.Count * lineBox);
        }

        public static int WrappedHeight(string text, SLTypeSpec spec, int width)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            return (int)Math.Round(Wrap(text, spec.Font, width).Count * spec.LineBoxF);
        }
    }

    /// <summary>
    /// Base for SL controls that paint everything themselves: double-buffered, and the
    /// background is the parent's colour so rounded shapes get "transparent" corners.
    /// </summary>
    internal abstract class SLControl : Control
    {
        protected SLControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (SolidBrush b = new SolidBrush(SLPaint.BackdropOf(this)))
                e.Graphics.FillRectangle(b, ClientRectangle);
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            if (AutoSize && Parent != null) Parent.PerformLayout(this, "Text");
            Invalidate();
        }
    }

    /// <summary>Shared hover tooltip for icon-only controls.</summary>
    internal static class SLToolTip
    {
        private static ToolTip _tip;
        public static void Set(Control c, string text)
        {
            if (_tip == null) _tip = new ToolTip { InitialDelay = 400, ShowAlways = true };
            _tip.SetToolTip(c, text);
        }
    }
}
