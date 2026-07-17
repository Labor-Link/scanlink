using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web.Script.Serialization;

namespace ScanLink
{
    public class MultiUserDashboard : Form
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.dashboardPanel = new System.Windows.Forms.Panel();
            this.titleLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // dashboardPanel
            //
            this.dashboardPanel.AutoScroll = true;
            this.dashboardPanel.BackColor = Color.White;
            this.dashboardPanel.Location = new System.Drawing.Point(50, 90);
            this.dashboardPanel.Name = "dashboardPanel";
            this.dashboardPanel.Size = new System.Drawing.Size(1300, 750);
            this.dashboardPanel.TabIndex = 0;
            //
            // titleLabel
            //
            this.titleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 20F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.titleLabel.Location = new System.Drawing.Point(50, 30);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(400, 40);
            this.titleLabel.TabIndex = 1;
            this.titleLabel.Text = "Multi Access Dashboard";
            //
            // MultiUserDashboard
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.BackColor = Color.White;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1400, 900);
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.dashboardPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.Name = "MultiUserDashboard";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Multi Access Dashboard";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Shown += new System.EventHandler(this.MultiUserDashboard_Shown);
            this.Load += new System.EventHandler(this.MultiUserDashboard_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel dashboardPanel;
        private System.Windows.Forms.Label titleLabel;

        private readonly ApiAuthService _apiAuthService;
        private readonly JavaScriptSerializer _jsonSerializer;
        private Dictionary<string, object> _tokenPayload;
        private string _selectedSiteId;
        private string _selectedSiteName;
        private List<SiteTile> _siteTiles;

        // Define classes similar to React code
        public class SiteInfo
        {
            public string first { get; set; }
            public string second { get; set; }
        }

        public class AuthorityInfo
        {
            public string authority { get; set; }
            public string source_type { get; set; }
            public object sites { get; set; }
            public object active_site { get; set; }
            public Dictionary<string, SiteInfo> farms { get; set; }
            public SiteInfo farm { get; set; }
        }

        public interface Token
        {
            string profile_file_id { get; set; }
            string last_name { get; set; }
            string user { get; set; }
            string first_name { get; set; }
            List<AuthorityInfo> authorities { get; set; }
            string sub { get; set; }
            long iat { get; set; }
            long exp { get; set; }
        }

        public class SiteTile
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Type { get; set; } // "OWNER" or "EMPLOYEE"
            public AuthorityInfo Authority { get; set; }
        }

        public MultiUserDashboard(ApiAuthService apiAuthService, Dictionary<string, object> tokenPayload)
        {
            InitializeComponent();
            try
            {
                this.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { /* icon is cosmetic; fall back to default if extraction fails */ }
            _apiAuthService = apiAuthService;
            _jsonSerializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            _tokenPayload = tokenPayload;
            _siteTiles = new List<SiteTile>();
        }

        private void MultiUserDashboard_Load(object sender, EventArgs e)
        {
            LoadSiteTiles();

            // Check if there's only one site available
            if (_siteTiles.Count == 1)
            {
                // Auto-select the single site
                var singleSite = _siteTiles[0];

                // Auto-select and close
                _selectedSiteId = singleSite.Id;
                _selectedSiteName = singleSite.Name;
                this.DialogResult = DialogResult.OK;
                this.Close();
                return;
            }
            else if (_siteTiles.Count == 0)
            {
                // Handle no sites case (already handled in LoadSiteTiles with ShowError)
                return;
            }

            // Multiple sites - show the dashboard normally
            RenderDashboard();
        }

        private void LoadSiteTiles()
        {
            try
            {
                if (_tokenPayload == null || !_tokenPayload.ContainsKey("authorities"))
                {
                    ShowError("No authorities found in token");
                    return;
                }

                var authorities = _tokenPayload["authorities"];
                if (!(authorities is System.Collections.ArrayList authoritiesList))
                {
                    ShowError("Invalid authorities format in token");
                    return;
                }

                foreach (var authority in authoritiesList)
                {
                    if (!(authority is Dictionary<string, object> authorityDict))
                        continue;

                    var auth = new AuthorityInfo
                    {
                        authority = authorityDict.ContainsKey("authority") ? authorityDict["authority"]?.ToString() : null,
                        source_type = authorityDict.ContainsKey("source_type") ? authorityDict["source_type"]?.ToString() : null,
                        sites = authorityDict.ContainsKey("sites") ? authorityDict["sites"] : null,
                        active_site = authorityDict.ContainsKey("active_site") ? authorityDict["active_site"] : null,
                        farms = authorityDict.ContainsKey("farms") ? ExtractFarms(authorityDict["farms"]) : null,
                        farm = authorityDict.ContainsKey("farm") ? ExtractSite(authorityDict["farm"]) : null
                    };

                    // Only process OWNER and EMPLOYEE authorities (not SECURITY_COMPANY_ADMIN)
                    if (auth.authority == "OWNER")
                    {
                        var ownerTiles = CreateOwnerTiles(auth);
                        _siteTiles.AddRange(ownerTiles);
                    }
                    else if (auth.authority == "EMPLOYEE")
                    {
                        var employeeTiles = CreateEmployeeTiles(auth);
                        _siteTiles.AddRange(employeeTiles);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error loading site tiles: {ex.Message}");
            }
        }

        private Dictionary<string, SiteInfo> ExtractFarms(object farmsObj)
        {
            if (!(farmsObj is Dictionary<string, object> farmsDict))
                return null;

            var farms = new Dictionary<string, SiteInfo>();
            foreach (var kvp in farmsDict)
            {
                if (kvp.Value is Dictionary<string, object> farmDict)
                {
                    farms[kvp.Key] = ExtractSite(farmDict);
                }
            }
            return farms;
        }

        private SiteInfo ExtractSite(object siteObj)
        {
            if (!(siteObj is Dictionary<string, object> siteDict))
                return null;

            return new SiteInfo
            {
                first = siteDict.ContainsKey("first") ? siteDict["first"]?.ToString() : null,
                second = siteDict.ContainsKey("second") ? siteDict["second"]?.ToString() : null
            };
        }

        private List<SiteTile> CreateOwnerTiles(AuthorityInfo owner)
        {
            var tiles = new List<SiteTile>();

            // Handle farms structure
            if (owner.farms != null && owner.farms.Count > 0)
            {
                foreach (var farm in owner.farms)
                {
                    tiles.Add(new SiteTile
                    {
                        Id = farm.Value.first,
                        Name = farm.Value.second,
                        Type = "OWNER",
                        Authority = owner
                    });
                }
            }
            // Handle single farm structure
            else if (owner.farm != null)
            {
                tiles.Add(new SiteTile
                {
                    Id = owner.farm.first,
                    Name = owner.farm.second,
                    Type = "OWNER",
                    Authority = owner
                });
            }

            return tiles;
        }

        private List<SiteTile> CreateEmployeeTiles(AuthorityInfo employee)
        {
            var tiles = new List<SiteTile>();
            var allSites = new List<SiteInfo>();

            // Add sites from the sites field
            if (employee.sites != null)
            {
                var sites = GetSitesArray(employee.sites);
                allSites.AddRange(sites);
            }

            // Add active_site if it exists
            if (employee.active_site != null)
            {
                var activeSite = ExtractSite(employee.active_site);
                if (activeSite != null)
                {
                    allSites.Add(activeSite);
                }
            }

            // Remove duplicates and create tiles
            var seenIds = new HashSet<string>();
            foreach (var site in allSites)
            {
                if (site.first != null && !seenIds.Contains(site.first))
                {
                    seenIds.Add(site.first);
                    tiles.Add(new SiteTile
                    {
                        Id = site.first,
                        Name = site.second,
                        Type = "EMPLOYEE",
                        Authority = employee
                    });
                }
            }

            return tiles;
        }

        private List<SiteInfo> GetSitesArray(object sitesObj)
        {
            var sites = new List<SiteInfo>();

            if (sitesObj is Dictionary<string, object> sitesDict)
            {
                // Single site object
                var site = ExtractSite(sitesDict);
                if (site != null)
                {
                    sites.Add(site);
                }
            }
            else if (sitesObj is System.Collections.ArrayList sitesList)
            {
                // Array of sites
                foreach (var siteObj in sitesList)
                {
                    var site = ExtractSite(siteObj);
                    if (site != null)
                    {
                        sites.Add(site);
                    }
                }
            }

            return sites;
        }

        private void RenderDashboard()
        {
            // Clear existing controls
            dashboardPanel.Controls.Clear();

            if (_siteTiles.Count == 0)
            {
                ShowError("No sites available for your account");
                return;
            }

            // Group tiles by type
            var ownerTiles = _siteTiles.Where(t => t.Type == "OWNER").ToList();
            var employeeTiles = _siteTiles.Where(t => t.Type == "EMPLOYEE").ToList();

            int yOffset = 20;

            // Render Owner section
            if (ownerTiles.Count > 0)
            {
                yOffset = RenderRoleSection("Owner", ownerTiles, yOffset);
            }

            // Render Employee section
            if (employeeTiles.Count > 0)
            {
                yOffset = RenderRoleSection("Employee - HR/HR Manager/Farm Manager", employeeTiles, yOffset);
            }

            // Set form height based on content
            this.Height = Math.Max(400, yOffset + 100);
        }

        private int RenderRoleSection(string title, List<SiteTile> tiles, int yOffset)
        {
            // Section title
            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("Microsoft Sans Serif", 14, FontStyle.Bold),
                Location = new Point(20, yOffset),
                AutoSize = true
            };
            dashboardPanel.Controls.Add(titleLabel);
            yOffset += 40;

            // Tiles grid
            int tilesPerRow = 3;
            int tileWidth = 390; // 280 * 1.5 = 420
            int tileHeight = 75;  // 150 / 2 = 75
            int tileSpacing = 50;
            int xOffset = 0;

            for (int i = 0; i < tiles.Count; i++)
            {
                int row = i / tilesPerRow;
                int col = i % tilesPerRow;

                var tilePanel = CreateSiteTile(tiles[i]);
                tilePanel.Location = new Point(
                    xOffset + col * (tileWidth + tileSpacing),
                    yOffset + row * (tileHeight + tileSpacing)
                );
                tilePanel.Size = new Size(tileWidth, tileHeight);
                dashboardPanel.Controls.Add(tilePanel as Control);
            }

            // Calculate new yOffset
            int rows = (tiles.Count + tilesPerRow - 1) / tilesPerRow;
            yOffset += rows * (tileHeight + tileSpacing) + 30;

            return yOffset;
        }

        private RoundedPanel CreateSiteTile(SiteTile siteTile)
        {
            var panel = new RoundedPanel
            {
                BackColor = Color.FromArgb(240, 240, 240),
                Cursor = Cursors.Hand,
                CornerRadius = 15
            };

            // Icon (simplified - using text for now)
            var iconLabel = new Label
            {
                Text = siteTile.Type == "OWNER" ? "🏭" : "🏢",
                Font = new Font("Microsoft Sans Serif", 24), // Reduced from 32 to fit shorter tile
                Location = new Point(15, 10),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            // Site name
            var nameLabel = new Label
            {
                Text = siteTile.Name ?? "Unnamed Site",
                Font = new Font("Microsoft Sans Serif", 12, FontStyle.Bold),
                Location = new Point(90, 12),
                AutoSize = true,
                MaximumSize = new Size(280, 0), // Increased from 180 to fit wider tile
                BackColor = Color.Transparent
            };

            // Site ID
            var idLabel = new Label
            {
                Text = $"ID: {siteTile.Id}",
                Font = new Font("Microsoft Sans Serif", 9),
                ForeColor = Color.Black,
                Location = new Point(90, 35), // Moved up from 55 to fit shorter tile
                AutoSize = true,
                BackColor = Color.Transparent
            };

            panel.Controls.AddRange(new Control[] { iconLabel, nameLabel, idLabel });

            // Click event
            panel.Click += (sender, e) => SelectSite(siteTile);

            return panel;
        }

        // Custom panel with rounded corners
        private class RoundedPanel : Panel
        {
            public int CornerRadius { get; set; } = 15;
            private bool _isHovered = false;

            protected override void OnMouseEnter(EventArgs e)
            {
                base.OnMouseEnter(e);
                _isHovered = true;
                Invalidate(); // Trigger repaint
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                _isHovered = false;
                Invalidate(); // Trigger repaint
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                // Determine background color based on hover state
                Color backgroundColor = _isHovered ? Color.LightGray : BackColor; // Lighter on hover

                // Create a rounded rectangle path
                using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    path.AddArc(0, 0, CornerRadius, CornerRadius, 180, 90);
                    path.AddArc(Width - CornerRadius, 0, CornerRadius, CornerRadius, 270, 90);
                    path.AddArc(Width - CornerRadius, Height - CornerRadius, CornerRadius, CornerRadius, 0, 90);
                    path.AddArc(0, Height - CornerRadius, CornerRadius, CornerRadius, 90, 90);
                    path.CloseFigure();

                    // Fill the background
                    using (var brush = new SolidBrush(backgroundColor))
                    {
                        e.Graphics.FillPath(brush, path);
                    }

                    // Draw the border
                    using (var pen = new Pen(Color.Black, 2))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }

                // Set the region to clip child controls to the rounded shape
                Region = new Region(CreateRoundedRectanglePath(ClientRectangle, CornerRadius));
            }

            private System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
            {
                var path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
                path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
                path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
                path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
                path.CloseFigure();
                return path;
            }
        }

        private void SelectSite(SiteTile siteTile)
        {
            _selectedSiteId = siteTile.Id;
            _selectedSiteName = siteTile.Name;

            // Close dialog with OK result
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        public string GetSelectedSiteId()
        {
            return _selectedSiteId;
        }

        public string GetSelectedSiteName()
        {
            return _selectedSiteName;
        }

        private void MultiUserDashboard_Shown(object sender, EventArgs e)
        {
            // Form is already centered and maximized via StartPosition and WindowState
        }
    }
}
