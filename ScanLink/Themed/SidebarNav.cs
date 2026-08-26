using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ScanLink.Themed
{
    /// <summary>
    /// The navy application rail — the app's only navigation. Replaces the seven equal blue
    /// tiles in the old 60px header, which were told apart only by a 1px border colour and
    /// said nothing about where you were.
    ///
    /// Items carry no behaviour: each raises ItemSelected and Form1 forwards that to the
    /// existing *_Click handler, so every guard in those handlers still runs.
    /// </summary>
    internal class SidebarNav : Panel
    {
        internal class NavItem
        {
            public string Key;
            public string IconName;
            public string Glyph;
            public string Label;
            public bool IsActive;
            internal Panel Row;
            internal Label GlyphLabel;
            internal Label TextLabel;
        }

        private readonly List<NavItem> _items = new List<NavItem>();
        private readonly Panel _body;
        private readonly Panel _footer;
        private string _activeKey;

        public event EventHandler<string> ItemSelected;

        public SidebarNav()
        {
            Dock = DockStyle.Left;
            Width = Theme.SidebarWidth;
            BackColor = Theme.SurfaceShell;

            // Explicit height, not AutoSize: an AutoSize container whose children are docked
            // collapses to zero in WinForms, which hid the site tile and Sign out entirely.
            _footer = new Panel
            {
                Dock = DockStyle.Bottom,
                BackColor = Theme.SurfaceShell,
                Padding = new Padding(Theme.S2, Theme.S2, Theme.S2, Theme.S3),
                Height = 52 + 34 + Theme.S2 + Theme.S3
            };

            _body = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.SurfaceShell,
                AutoScroll = false,
                Padding = new Padding(Theme.S2, Theme.S4, Theme.S2, Theme.S3)
            };

            Controls.Add(_body);
            Controls.Add(_footer);
        }

        public Panel Footer { get { return _footer; } }
        public string ActiveKey { get { return _activeKey; } }

        /// <summary>Brand mark at the top of the rail.</summary>
        public void AddBrand(Image logo, string fallbackText)
        {
            Control brand;
            if (logo != null)
            {
                brand = new PictureBox
                {
                    Image = logo,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Height = 36,
                    Dock = DockStyle.Top,
                    Margin = Padding.Empty,
                    BackColor = Color.Transparent
                };
            }
            else
            {
                brand = new Label
                {
                    Text = fallbackText,
                    Font = Theme.Font2Xl,
                    ForeColor = Theme.TextOnShellStrong,
                    Height = 36,
                    Dock = DockStyle.Top,
                    TextAlign = ContentAlignment.MiddleLeft,
                    BackColor = Color.Transparent,
                    UseMnemonic = false
                };
            }

            _body.Controls.Add(new Panel { Dock = DockStyle.Top, Height = Theme.S5, BackColor = Color.Transparent });
            _body.Controls.Add(brand);
        }

        /// <summary>Group heading above a set of destinations.</summary>
        public void AddGroupTitle(string title)
        {
            Label heading = new Label
            {
                Text = (title ?? string.Empty).ToUpperInvariant(),
                Font = Theme.FontXsBold,
                ForeColor = Theme.Blend(Color.White, Theme.SurfaceShell, 0.42),
                Dock = DockStyle.Top,
                Height = 34,
                Padding = new Padding(Theme.S4, Theme.S4, 0, 0),
                TextAlign = ContentAlignment.BottomLeft,
                BackColor = Color.Transparent,
                UseMnemonic = false
            };
            _body.Controls.Add(heading);
            heading.BringToFront();
        }

        /// <summary>Adds a destination. iconName is a Lucide icon; if it is not embedded the
        /// item falls back to glyph so the rail is never blank.</summary>
        public void AddItem(string key, string iconName, string glyph, string label)
        {
            NavItem item = new NavItem { Key = key, IconName = iconName, Glyph = glyph, Label = label };

            Panel row = new Panel
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = Theme.SurfaceShell,
                Cursor = Cursors.Hand,
                Margin = Padding.Empty
            };

            Label glyphLabel = new Label
            {
                Font = new Font("Segoe UI Emoji", 11f),
                ForeColor = Theme.TextOnShell,
                Location = new Point(Theme.S4, 11),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };

            Image icon = IconSet.Get(iconName, 20, IconSet.Tint.Light);
            if (icon != null)
            {
                glyphLabel.AutoSize = false;
                glyphLabel.Size = new Size(20, 20);
                glyphLabel.Image = icon;
            }
            else
            {
                glyphLabel.AutoSize = true;
                glyphLabel.Text = glyph;
            }

            Label textLabel = new Label
            {
                Text = label,
                Font = Theme.FontMd,
                ForeColor = Theme.TextOnShell,
                Location = new Point(46, 12),
                AutoSize = true,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                // WinForms treats '&' in Text as a mnemonic prefix, so "Crops & products"
                // rendered as "Crops  products" with the ampersand swallowed.
                UseMnemonic = false
            };

            row.Controls.Add(glyphLabel);
            row.Controls.Add(textLabel);

            item.Row = row;
            item.GlyphLabel = glyphLabel;
            item.TextLabel = textLabel;
            _items.Add(item);

            // The labels sit on top of the row and swallow its mouse events, so each one
            // forwards click and hover or the row has dead patches.
            foreach (Control c in new Control[] { row, glyphLabel, textLabel })
            {
                c.Click += (s, e) => Select(key);
                c.MouseEnter += (s, e) => SetHover(item, true);
                c.MouseLeave += (s, e) => SetHover(item, false);
            }

            _body.Controls.Add(row);
            row.BringToFront();
        }

        /// <summary>The active row is a rounded pill, not a full-bleed bar.</summary>
        private static void RoundedCornersFor(Panel row, bool active)
        {
            if (row == null) return;
            if (active) ThemeStyles.RoundedCorners(row, Theme.RadiusSm);
            else if (row.Region != null) row.Region = null;
        }

        private void SetHover(NavItem item, bool hovered)
        {
            if (item.IsActive) return;
            item.Row.BackColor = hovered ? Theme.SurfaceShellHover : Theme.SurfaceShell;
        }

        /// <summary>Raises ItemSelected. Marking active is separate, because some
        /// destinations open a dialog and the page behind them does not change.</summary>
        public void Select(string key)
        {
            EventHandler<string> handler = ItemSelected;
            if (handler != null) handler(this, key);
        }

        public void SetActive(string key)
        {
            _activeKey = key;
            foreach (NavItem item in _items)
            {
                item.IsActive = (item.Key == key);
                item.Row.BackColor = item.IsActive ? Theme.ActionPrimary : Theme.SurfaceShell;
                RoundedCornersFor(item.Row, item.IsActive);
                item.TextLabel.ForeColor = item.IsActive ? Theme.TextOnShellStrong : Theme.TextOnShell;
                item.TextLabel.Font = item.IsActive ? Theme.FontMdBold : Theme.FontMd;
                item.GlyphLabel.ForeColor = item.IsActive ? Theme.TextOnShellStrong : Theme.TextOnShell;

                // Icons are baked per tint, so the active row swaps to the white variant
                // rather than being recoloured at paint time.
                if (item.GlyphLabel.Image != null)
                {
                    Image swap = IconSet.Get(item.IconName, 20,
                        item.IsActive ? IconSet.Tint.White : IconSet.Tint.Light);
                    if (swap != null) item.GlyphLabel.Image = swap;
                }
            }
        }
    }
}
