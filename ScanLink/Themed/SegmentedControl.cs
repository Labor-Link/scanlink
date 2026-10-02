using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ScanLink.Themed
{
    /// <summary>
    /// The design system's segmented control: a sunken track holding mutually exclusive
    /// options, the selected one lifted onto the card surface.
    ///
    /// Laid out by hand rather than with a FlowLayoutPanel. An AutoSize container whose
    /// children are docked collapses to zero in WinForms, and the widths here have to be
    /// measured from the captions anyway, so explicit placement is both safer and shorter.
    /// </summary>
    internal class SegmentedControl : Panel
    {
        private const int TrackPad = 3;
        private const int SegmentHeight = Theme.HeightSm;
        private const int SegmentPadX = Theme.S4;

        private readonly List<Button> _segments = new List<Button>();
        private string _selectedKey;

        /// <summary>Raised only on a real change, and only for user clicks —
        /// <see cref="SetSelected"/> is silent so callers can sync state without recursing.</summary>
        public event EventHandler<string> SegmentSelected;

        public SegmentedControl()
        {
            Height = SegmentHeight + TrackPad * 2;
            Width = TrackPad * 2;
            BackColor = Theme.SurfaceSunken;
            ThemeStyles.RoundedCorners(this, Theme.RadiusMd);
        }

        public string SelectedKey { get { return _selectedKey; } }

        public void AddSegment(string key, string text)
        {
            Button segment = new Button
            {
                Text = text,
                Tag = key,
                Height = SegmentHeight,
                Top = TrackPad,
                Left = Width - TrackPad,
                FlatStyle = FlatStyle.Flat,
                UseMnemonic = false,
                UseVisualStyleBackColor = false,
                Cursor = Cursors.Hand
            };
            segment.FlatAppearance.BorderSize = 0;
            segment.Width = MeasureSegment(text);
            segment.Click += (s, e) =>
            {
                string clicked = (string)((Button)s).Tag;
                if (clicked == _selectedKey) return;
                SetSelected(clicked);
                EventHandler<string> handler = SegmentSelected;
                if (handler != null) handler(this, clicked);
            };

            _segments.Add(segment);
            Controls.Add(segment);

            Width = segment.Right + TrackPad;
            if (_selectedKey == null) SetSelected(key);
            else Restyle();
        }

        private static int MeasureSegment(string text)
        {
            try
            {
                return TextRenderer.MeasureText(text ?? string.Empty, Theme.FontSmBold).Width + SegmentPadX * 2;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[SEGMENT] could not measure '" + text + "': " + ex.Message);
                return 96;
            }
        }

        /// <summary>Selects without raising SegmentSelected.</summary>
        public void SetSelected(string key)
        {
            _selectedKey = key;
            Restyle();
        }

        private void Restyle()
        {
            foreach (Button segment in _segments)
            {
                bool on = ((string)segment.Tag) == _selectedKey;
                segment.BackColor = on ? Theme.SurfaceCard : Theme.SurfaceSunken;
                segment.ForeColor = on ? Theme.TextHeading : Theme.TextMuted;
                segment.Font = on ? Theme.FontSmBold : Theme.FontSm;
                segment.FlatAppearance.MouseOverBackColor = on ? Theme.SurfaceCard : Theme.N200;
                segment.FlatAppearance.MouseDownBackColor = Theme.N200;
                ThemeStyles.RoundedCorners(segment, Theme.RadiusSm);
            }
        }
    }
}
