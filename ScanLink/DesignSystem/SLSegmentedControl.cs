using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ScanLink.DesignSystem
{
    /// <summary>
    /// The segmented switch from the mockup's Scans screen (the Today / Last 7 days / This season
    /// range picker in design/mockup/screens/ScansScreen.js). Use it for a small set of mutually
    /// exclusive views or filters — tabs over a table, a date range.
    ///
    ///   Track   #F1F3F7, radius 6, 3px padding, 4px between items.
    ///   Item    padding 6x12, radius 4, 13px; selected: white, soft shadow, heading ink, 600;
    ///           others: muted ink, 500.
    ///
    ///   var tabs = new SLSegmentedControl("Today", "Last 7 days", "This season", "Custom");
    ///   tabs.SelectedIndexChanged += (s, e) => Reload(tabs.SelectedItem);
    /// </summary>
    [DesignerCategory("Code")]
    internal class SLSegmentedControl : SLControl
    {
        private const int Pad = 3, ItemGap = 4, ItemPadX = 12, ItemPadY = 6;
        private readonly List<string> _items = new List<string>();
        private int _selected;
        private int _hover = -1;

        public event EventHandler SelectedIndexChanged;

        public SLSegmentedControl(params string[] items)
        {
            AutoSize = true;
            TabStop = true;
            Cursor = Cursors.Hand;
            _items.AddRange(items);
            Size = GetPreferredSize(Size.Empty);
        }

        public IList<string> Items { get { return _items.AsReadOnly(); } }

        public int SelectedIndex
        {
            get { return _selected; }
            set
            {
                int v = Math.Max(0, Math.Min(_items.Count - 1, value));
                if (v == _selected) return;
                _selected = v;
                Invalidate();
                if (SelectedIndexChanged != null) SelectedIndexChanged(this, EventArgs.Empty);
            }
        }

        public string SelectedItem { get { return _selected >= 0 && _selected < _items.Count ? _items[_selected] : null; } }

        private static Font FontFor(bool selected) { return selected ? Theme.FontSmSemibold : Theme.FontSmMedium; }

        private int ItemHeight { get { return 13 + ItemPadY * 2; } } // font: 13px/1

        private List<Rectangle> ItemRects()
        {
            List<Rectangle> rects = new List<Rectangle>();
            int x = Pad;
            for (int i = 0; i < _items.Count; i++)
            {
                int w = SLPaint.Measure(_items[i], FontFor(i == _selected)).Width + ItemPadX * 2;
                rects.Add(new Rectangle(x, Pad, w, ItemHeight));
                x += w + ItemGap;
            }
            return rects;
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            int w = Pad * 2;
            for (int i = 0; i < _items.Count; i++)
                w += SLPaint.Measure(_items[i], FontFor(i == _selected)).Width + ItemPadX * 2;
            if (_items.Count > 1) w += ItemGap * (_items.Count - 1);
            return new Size(w, ItemHeight + Pad * 2);
        }

        private int HitTest(Point p)
        {
            List<Rectangle> rects = ItemRects();
            for (int i = 0; i < rects.Count; i++) if (rects[i].Contains(p)) return i;
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int h = HitTest(e.Location);
            if (h != _hover) { _hover = h; Invalidate(); }
        }

        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hover = -1; Invalidate(); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            int h = HitTest(e.Location);
            if (h >= 0) SelectedIndex = h;
        }

        protected override bool IsInputKey(Keys keyData)
        {
            return keyData == Keys.Left || keyData == Keys.Right || base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Left) SelectedIndex = _selected - 1;
            else if (e.KeyCode == Keys.Right) SelectedIndex = _selected + 1;
        }

        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Size size = GetPreferredSize(Size.Empty);
            Rectangle track = new Rectangle(0, 0, Math.Min(Width, size.Width), Math.Min(Height, size.Height));
            SLPaint.Box(g, track, Theme.RadiusSm, Theme.N100, Color.Empty);

            List<Rectangle> rects = ItemRects();
            for (int i = 0; i < rects.Count; i++)
            {
                bool sel = i == _selected;
                Rectangle r = rects[i];
                if (sel)
                {
                    SLPaint.Shadow(g, r, Theme.RadiusXs, 1);
                    SLPaint.Box(g, r, Theme.RadiusXs, Theme.SurfaceCard, Color.Empty);
                    if (Focused && ShowFocusCues) SLPaint.FocusRing(g, Rectangle.Inflate(r, -2, -2), Theme.RadiusXs);
                }
                Color ink = sel ? Theme.TextHeading : (i == _hover ? Theme.TextBody : Theme.TextMuted);
                SLPaint.Text(g, _items[i], FontFor(sel), r, ink, TextFormatFlags.HorizontalCenter);
            }
        }
    }
}
