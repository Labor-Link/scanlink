using System;
using System.Drawing;
using System.Windows.Forms;
using ScanLink.Themed;

namespace ScanLink
{
    /// <summary>
    /// Restyles the print surfaces and collapses the advanced printer settings behind a
    /// disclosure toggle.
    ///
    /// The reparenting contract is preserved exactly: configPanel, actionPanel and
    /// advancedPanel stay whole, self-contained controls, because the popup forms take them
    /// by reference and move them at runtime. Only colours, type and the visibility of one
    /// group change.
    ///
    /// Not done here: the prototype's three-step print wizard. That changes when validation
    /// runs and when the job is dispatched, which is behaviour rather than layout.
    /// </summary>
    public partial class Form1
    {
        private Button _advancedToggle;
        private bool _advancedExpanded;

        private void BuildV2PrintSurfaces()
        {
            try
            {
                StylePrintPanel(configPanel);
                StylePrintPanel(actionPanel);
                StylePrintPanel(advancedPanel);
                StylePrintPanel(previewPanel);

                StyleGroupBox(configGroupBox);
                StyleGroupBox(advancedGroupBox);

                BuildAdvancedDisclosure();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[PRINT] v2 print surface build failed: " + ex);
            }
        }

        private void StylePrintPanel(Control panel)
        {
            if (panel == null) return;
            panel.BackColor = Theme.SurfaceApp;
            ThemeStyles.Inputs(panel);
        }

        private void StyleGroupBox(GroupBox box)
        {
            if (box == null) return;
            box.BackColor = Theme.SurfaceCard;
            box.ForeColor = Theme.TextHeading;
            box.Font = Theme.FontMdBold;
            ThemeStyles.RoundedCorners(box, Theme.RadiusLg);
        }

        /// <summary>
        /// "Most people never need to change these." The label size, quality and darkness
        /// controls are the most intimidating part of the print screen and are wrong to
        /// change by accident, so they start hidden behind a toggle.
        ///
        /// This hides existing controls; it does not remove them. Everything inside is still
        /// constructed, still wired and still applied when a job prints.
        /// </summary>
        private void BuildAdvancedDisclosure()
        {
            if (advancedGroupBox == null) return;

            Control[] collapsible = new Control[] { dimensionsPanel, qualityPanel, printerConfigPanel };

            _advancedToggle = new Button
            {
                Text = "Show settings",
                Width = 140,
                Height = Theme.HeightSm,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            ThemeStyles.Secondary(_advancedToggle);

            _advancedToggle.Location = new Point(
                Math.Max(Theme.S3, advancedGroupBox.ClientSize.Width - _advancedToggle.Width - Theme.S4),
                Theme.S2);

            _advancedToggle.Click += (s, e) =>
            {
                _advancedExpanded = !_advancedExpanded;
                ApplyAdvancedDisclosure(collapsible);
            };

            advancedGroupBox.Controls.Add(_advancedToggle);
            _advancedToggle.BringToFront();

            _advancedExpanded = false;
            ApplyAdvancedDisclosure(collapsible);
        }

        private void ApplyAdvancedDisclosure(Control[] collapsible)
        {
            foreach (Control c in collapsible)
            {
                if (c != null) c.Visible = _advancedExpanded;
            }
            if (_advancedToggle != null)
            {
                _advancedToggle.Text = _advancedExpanded ? "Hide settings" : "Show settings";
            }
            LayoutRootPanels();
        }
    }
}
