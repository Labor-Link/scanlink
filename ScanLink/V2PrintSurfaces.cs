using System;
using System.Drawing;
using System.Windows.Forms;
using ScanLink.Themed;

namespace ScanLink
{
    /// <summary>
    /// Colour and type for the print controls.
    ///
    /// Layout is no longer done here — the three-step page in V2PrintPage owns where these
    /// controls sit. This pass only restyles them, and it runs before the print page is
    /// built so that controls are already themed when they are adopted into a step.
    /// </summary>
    public partial class Form1
    {
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

                // configGroupBox and previewPanel are emptied by the wizard, which adopts
                // their children into the step cards. Hiding the husks keeps them in the
                // tree — ConfigPopupForm still compiles against them — without leaving two
                // empty framed boxes behind the page.
                if (previewPanel != null) previewPanel.Visible = false;
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
    }
}
