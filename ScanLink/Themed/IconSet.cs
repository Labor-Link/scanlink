using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace ScanLink.Themed
{
    /// <summary>
    /// Serves the Lucide icons rasterised into PNGs and embedded in the assembly.
    ///
    /// WinForms cannot draw SVG, and Lucide strokes with currentColor, so each icon is baked
    /// once per size and tint it is used in. See tools/render-icons.js.
    ///
    /// Every lookup can fail softly: Get returns null when an icon is absent and the Apply
    /// helpers leave the control's text alone, so a missing asset degrades to the emoji the
    /// app shipped with rather than to a blank button.
    /// </summary>
    internal static class IconSet
    {
        internal enum Tint
        {
            /// <summary>#1D2939 — buttons and headings on white.</summary>
            Dark,
            /// <summary>#C4C9D1 — sidebar, inactive.</summary>
            Light,
            /// <summary>#FFFFFF — sidebar, active row.</summary>
            White
        }

        private static readonly Dictionary<string, string> ResourceByFile =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Image> Cache =
            new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
        private static bool _indexed;

        private static void EnsureIndexed()
        {
            if (_indexed) return;
            _indexed = true;
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                foreach (string resource in asm.GetManifestResourceNames())
                {
                    if (!resource.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;
                    // Index by the trailing "<name>_<size>_<tint>.png". MSBuild prefixes the
                    // namespace and folder path, so matching on the tail is the stable key.
                    int cut = resource.LastIndexOf('.', resource.Length - 5);
                    string file = (cut >= 0) ? resource.Substring(cut + 1) : resource;
                    ResourceByFile[file] = resource;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[ICONS] could not index resources: " + ex.Message);
            }
        }

        /// <summary>Any embedded PNG by file name, or null. Used for the knockout logo that
        /// sits on the navy shell — the standard mark is dark ink and disappears there.</summary>
        public static Image GetImage(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;
            EnsureIndexed();

            Image cached;
            if (Cache.TryGetValue(fileName, out cached)) return cached;

            string resource;
            if (!ResourceByFile.TryGetValue(fileName, out resource)) return null;
            return Materialise(resource, fileName);
        }

        /// <summary>Returns the icon, or null if it was never embedded.</summary>
        public static Image Get(string name, int size, Tint tint)
        {
            if (string.IsNullOrEmpty(name)) return null;
            EnsureIndexed();

            // MSBuild leaves hyphens intact in practice, but try both spellings.
            string suffix = "_" + size + "_" + tint.ToString().ToLowerInvariant() + ".png";
            string[] candidates = { name + suffix, name.Replace("-", "_") + suffix };

            foreach (string key in candidates)
            {
                Image cached;
                if (Cache.TryGetValue(key, out cached)) return cached;

                string resource;
                if (!ResourceByFile.TryGetValue(key, out resource)) continue;

                Image made = Materialise(resource, key);
                if (made != null) return made;
            }
            return null;
        }

        private static Image Materialise(string resource, string cacheKey)
        {
            try
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
                {
                    if (stream == null) return null;
                    // Copy out of the manifest stream: Image keeps a reference to its source
                    // stream, and that stream is disposed on exit here.
                    Image image = Image.FromStream(stream);
                    Bitmap copy = new Bitmap(image);
                    image.Dispose();
                    Cache[cacheKey] = copy;
                    return copy;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[ICONS] failed to load " + resource + ": " + ex.Message);
                return null;
            }
        }

        /// <summary>Puts an icon on a button and removes the emoji that stood in for it.
        /// Returns false and changes nothing if the icon is unavailable.</summary>
        public static bool ApplyTo(ButtonBase control, string name, int size, Tint tint)
        {
            if (control == null) return false;
            Image image = Get(name, size, tint);
            if (image == null) return false;

            control.Image = image;
            control.ImageAlign = ContentAlignment.MiddleLeft;
            control.TextImageRelation = TextImageRelation.ImageBeforeText;
            control.TextAlign = ContentAlignment.MiddleLeft;
            control.Text = StripLeadingGlyph(control.Text);
            control.Padding = new Padding(Theme.S2, 0, Theme.S3, 0);

            Button button = control as Button;
            if (button != null) ThemeStyles.FitToText(button);
            return true;
        }

        /// <summary>As above, for labels.</summary>
        public static bool ApplyTo(Label control, string name, int size, Tint tint)
        {
            if (control == null) return false;
            Image image = Get(name, size, tint);
            if (image == null) return false;

            control.Image = image;
            control.ImageAlign = ContentAlignment.MiddleLeft;
            control.TextAlign = ContentAlignment.MiddleLeft;
            control.Text = StripLeadingGlyph(control.Text);
            control.Padding = new Padding(size + Theme.S2, 0, 0, 0);
            return true;
        }

        /// <summary>Removes a leading emoji and the space after it: "🔄 Refresh" becomes
        /// "Refresh". Text that does not start with a symbol passes through unchanged.</summary>
        public static string StripLeadingGlyph(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            int i = 0;
            while (i < text.Length)
            {
                char c = text[i];
                bool isGlyph = char.IsSurrogate(c)
                               || c == '️'      // variation selector
                               || c == '‍'      // zero-width joiner
                               || (c > 0x2000 && c < 0x3300 && !char.IsLetterOrDigit(c));
                if (!isGlyph) break;
                i++;
            }
            if (i == 0) return text;
            return text.Substring(i).TrimStart();
        }
    }
}
