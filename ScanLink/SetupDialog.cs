using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScanLink.DesignSystem;

namespace ScanLink
{
    /// <summary>
    /// Crops &amp; products: the reference lists ScanLink prints from, one tab per list.
    /// Built on SLDialog (also used as a page through EmbeddedFormHost):
    ///   SLSegmentedControl tabs + row count
    ///   Crop / Variety filters (Combinations tab only)
    ///   SLCard with an edge-to-edge SLTable
    /// </summary>
    [DesignerCategory("Code")]
    internal partial class SetupDialog : SLDialog
    {
        // Tab label shown -> key the loaders switch on.
        private static readonly string[][] Tabs =
        {
            new[] { "Combinations", "Combination Table" },
            new[] { "Crops", "Crops" },
            new[] { "Products", "Products" },
            new[] { "Varieties", "Variety" },
            new[] { "Grades", "Grade" },
            new[] { "Counts", "Count" }
        };

        private ProductCombinationsService _productCombinationsService;
        private SLSegmentedControl _tabs;
        private SLText _rowCount;
        private SLCard _tableCard;
        private SLTable _currentDataGridView;
        private SLComboBox _cropFilterComboBox;
        private SLComboBox _varietyFilterComboBox;
        private SLStack _filterPanel;

        public SetupDialog(ProductCombinationsService productCombinationsService)
        {
            InitializeComponent();
            _productCombinationsService = productCombinationsService;
            InitializeDialog();
        }

        private void InitializeDialog()
        {
            Title = "Crops & products";
            Description = "The reference lists ScanLink prints from. They come from the Labour Link dashboard and are read-only here.";
            DialogWidth = 960;
            BodyHeight = 520;

            _tabs = new SLSegmentedControl(Array.ConvertAll(Tabs, t => t[0]));
            _tabs.SelectedIndexChanged += (s, e) => ShowTable(Tabs[_tabs.SelectedIndex][1]);
            _rowCount = new SLText("", SLTextStyle.Muted) { SingleLine = true };
            var header = new SLStack(SLOrientation.Horizontal, 12) { Align = SLAlign.Center, Justify = SLJustify.SpaceBetween };
            header.AddRange(_tabs, _rowCount);
            Body.Controls.Add(header);

            _filterPanel = new SLStack(SLOrientation.Horizontal, 12) { Align = SLAlign.Start };
            CreateFilterControls();
            Body.Controls.Add(_filterPanel);

            _tableCard = new SLCard { BodyPadding = Padding.Empty };
            Body.Controls.Add(_tableCard);
            Body.SetGrow(_tableCard);

            // Opened modeless (Show) by the legacy button, where DialogResult does not close.
            var close = new SLButton { Text = "Close", Variant = SLVariant.Secondary, DialogResult = DialogResult.Cancel };
            close.Click += (s, e) => Close();
            AddAction(close);

            ShowTable("Combination Table");
        }

        private void ShowTable(string tableType)
        {
            _tableCard.Body.Controls.Clear();
            SLVisibility.Set(_filterPanel, tableType == "Combination Table");

            switch (tableType)
            {
                case "Combination Table":
                    CreateCombinationTable();
                    break;
                case "Crops":
                    CreateCropsTable();
                    break;
                case "Products":
                    CreateProductsTable();
                    break;
                case "Variety":
                    CreateVarietyTable();
                    break;
                case "Grade":
                    CreateGradeTable();
                    break;
                case "Count":
                    CreateCountTable();
                    break;
            }

            _tableCard.Body.Controls.Add(_currentDataGridView);
            _tableCard.Body.SetGrow(_currentDataGridView);
            LoadTableData(tableType);
            UpdateRowCount();
        }

        private void UpdateRowCount()
        {
            int n = _currentDataGridView == null ? 0 : _currentDataGridView.Rows.Count;
            _rowCount.Text = n + (n == 1 ? " row" : " rows");
            if (_rowCount.Parent != null) _rowCount.Parent.PerformLayout();
        }

        private void CreateFilterControls()
        {
            _cropFilterComboBox = new SLComboBox { Width = 240 };
            _cropFilterComboBox.Items.Add("All crops");
            if (_productCombinationsService != null && _productCombinationsService.HasCachedData())
            {
                var crops = _productCombinationsService.GetUniqueCrops();
                foreach (var crop in crops.OrderBy(c => c.crop_name))
                {
                    _cropFilterComboBox.Items.Add($"{crop.crop_id} - {crop.crop_name}");
                }
            }
            _cropFilterComboBox.SelectedIndex = 0;

            _varietyFilterComboBox = new SLComboBox { Width = 240 };
            PopulateVarietyDropdown();

            _cropFilterComboBox.SelectedIndexChanged += FilterComboBox_SelectedIndexChanged;
            _varietyFilterComboBox.SelectedIndexChanged += FilterComboBox_SelectedIndexChanged;

            _filterPanel.AddRange(
                new SLField("Crop", _cropFilterComboBox) { Width = 240 },
                new SLField("Variety", _varietyFilterComboBox) { Width = 240 });
        }

        private void PopulateVarietyDropdown()
        {
            _varietyFilterComboBox.Items.Clear();
            _varietyFilterComboBox.Items.Add("All varieties");

            if (_productCombinationsService != null && _productCombinationsService.HasCachedData())
            {
                List<VarietyItem> varietiesToShow;

                // If a specific crop is selected, show only varieties for that crop
                if (_cropFilterComboBox != null && _cropFilterComboBox.SelectedIndex > 0)
                {
                    string selectedCropItem = _cropFilterComboBox.SelectedItem.ToString();
                    string selectedCropId = selectedCropItem.Split('-')[0].Trim();

                    // Get combinations for the selected crop and extract unique varieties
                    var combinationsForCrop = _productCombinationsService.GetAllCombinations()
                        .Where(c => c.crop_id == selectedCropId)
                        .ToList();

                    varietiesToShow = combinationsForCrop
                        .GroupBy(c => c.variety_id)
                        .Select(g => new VarietyItem
                        {
                            variety_id = g.Key,
                            variety_name = g.First().variety_name
                        })
                        .ToList();
                }
                else
                {
                    // Show all varieties when "All Crops" is selected
                    varietiesToShow = _productCombinationsService.GetUniqueVarieties();
                }

                // Add varieties to dropdown, sorted by name
                foreach (var variety in varietiesToShow.OrderBy(v => v.variety_name))
                {
                    _varietyFilterComboBox.Items.Add($"{variety.variety_id} - {variety.variety_name}");
                }
            }

            // Reset selection to "All Varieties" when repopulating
            _varietyFilterComboBox.SelectedIndex = 0;
        }

        private void FilterComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox changedComboBox = (ComboBox)sender;

            // If crop selection changed, update variety dropdown
            if (changedComboBox == _cropFilterComboBox)
            {
                PopulateVarietyDropdown();
            }

            // Reload combination data with current filters
            LoadCombinationData();
            UpdateRowCount();
        }

        private void CreateCombinationTable()
        {
            SLTable dataGridView = NewTable();
            dataGridView.Columns.Add("id", "ID");
            dataGridView.Columns.Add("crop_id", "Crop");
            dataGridView.Columns.Add("product_id", "Product ID");
            dataGridView.Columns.Add("variety_id", "Variety");
            dataGridView.Columns.Add("grade_id", "Grade");
            dataGridView.Columns.Add("count_id", "Count");
            dataGridView.Columns.Add("carton_type", "Carton type");
            dataGridView.Columns.Add("avg_weight_kg", "Avg weight (kg)");
            dataGridView.Columns["avg_weight_kg"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridView.Columns["avg_weight_kg"].FillWeight = 140;   // header needs room for "AVG WEIGHT (KG)"
            dataGridView.Columns["carton_type"].FillWeight = 120;
            dataGridView.SetMono("id");
            dataGridView.SetMono("product_id");

            _currentDataGridView = dataGridView;
        }

        private SLTable NewTable()
        {
            var table = new SLTable { Height = 200, ScrollBars = ScrollBars.Both };
            table.EmptyState = new SLEmptyState
            {
                Compact = true,
                IconName = "database",
                Title = "Nothing here yet",
                Description = "Product lists load when ScanLink connects to Labour Link. Check the connection, then reopen this page."
            };
            return table;
        }

        private void CreateIndividualTables()
        {
            // These will be created on demand in their respective methods
        }

        private void CreateCropsTable()
        {
            CreateIndividualTable("Crop ID", "Crop name");
        }

        private void CreateProductsTable()
        {
            CreateIndividualTable("Product ID", "Product name");
        }

        private void CreateVarietyTable()
        {
            CreateIndividualTable("Variety ID", "Variety name");
        }

        private void CreateGradeTable()
        {
            CreateIndividualTable("Grade ID", "Grade name");
        }

        private void CreateCountTable()
        {
            CreateIndividualTable("Count ID", "Count name");
        }

        private void CreateIndividualTable(string idColumnName, string nameColumnName)
        {
            SLTable dataGridView = NewTable();
            dataGridView.Columns.Add("id", idColumnName);
            dataGridView.Columns.Add("name", nameColumnName);
            dataGridView.Columns["id"].FillWeight = 30;
            dataGridView.SetMono("id");

            _currentDataGridView = dataGridView;
        }

        private void LoadTableData(string tableType)
        {
            if (_productCombinationsService == null || !_productCombinationsService.HasCachedData())
            {
                _currentDataGridView.Rows.Clear();
                return;
            }

            _currentDataGridView.Rows.Clear();

            switch (tableType)
            {
                case "Combination Table":
                    LoadCombinationData();
                    break;
                case "Crops":
                    LoadCropsData();
                    break;
                case "Products":
                    LoadProductsData();
                    break;
                case "Variety":
                    LoadVarietyData();
                    break;
                case "Grade":
                    LoadGradeData();
                    break;
                case "Count":
                    LoadCountData();
                    break;
            }
        }

        private void LoadCombinationData()
        {
            var combinations = _productCombinationsService.GetAllCombinations();

            // Apply crop filter if selected
            if (_cropFilterComboBox != null && _cropFilterComboBox.SelectedIndex > 0)
            {
                // Extract crop_id from selected item (format: "crop_id - crop_name")
                string selectedItem = _cropFilterComboBox.SelectedItem.ToString();
                string selectedCropId = selectedItem.Split('-')[0].Trim();
                combinations = combinations.Where(c => c.crop_id == selectedCropId).ToList();
            }

            // Apply variety filter if selected
            if (_varietyFilterComboBox != null && _varietyFilterComboBox.SelectedIndex > 0)
            {
                // Extract variety_id from selected item (format: "variety_id - variety_name")
                string selectedItem = _varietyFilterComboBox.SelectedItem.ToString();
                string selectedVarietyId = selectedItem.Split('-')[0].Trim();
                combinations = combinations.Where(c => c.variety_id == selectedVarietyId).ToList();
            }

            _currentDataGridView.Rows.Clear();

            foreach (var combo in combinations)
            {
                _currentDataGridView.Rows.Add(
                    combo.id,
                    $"{combo.crop_id} ({GetCropName(combo.crop_id)})",
                    combo.product_id,
                    $"{combo.variety_id} ({GetVarietyName(combo.variety_id)})",
                    $"{combo.grade_id} ({GetGradeName(combo.grade_id)})",
                    $"{combo.count_id} ({GetCountName(combo.count_id)})",
                    GetCartonTypeName(combo.carton_type_id, combo.carton_type_name),
                    combo.avg_weight_kg
                );
            }
        }

        private string GetCartonTypeName(string cartonId, string cartonName = null)
        {
            if (!string.IsNullOrWhiteSpace(cartonName)) return cartonName;
            if (string.IsNullOrWhiteSpace(cartonId)) return "Unknown";
            return $"Unknown ({cartonId})";
        }

        private string GetCropName(string cropId)
        {
            if (string.IsNullOrEmpty(cropId) || _productCombinationsService == null || !_productCombinationsService.HasCachedData())
                return "Unknown";

            var crops = _productCombinationsService.GetUniqueCrops();
            var crop = crops?.FirstOrDefault(c => string.Equals(c.crop_id, cropId, StringComparison.OrdinalIgnoreCase));
            return crop?.crop_name ?? "Unknown";
        }

        private string GetVarietyName(string varietyId)
        {
            if (string.IsNullOrEmpty(varietyId) || _productCombinationsService == null || !_productCombinationsService.HasCachedData())
                return "Unknown";

            var combinations = _productCombinationsService.GetAllCombinations();
            var variety = combinations?.FirstOrDefault(c => string.Equals(c.variety_id, varietyId, StringComparison.OrdinalIgnoreCase));
            return variety?.variety_name ?? "Unknown";
        }

        private string GetGradeName(string gradeId)
        {
            if (string.IsNullOrEmpty(gradeId) || _productCombinationsService == null || !_productCombinationsService.HasCachedData())
                return "Unknown";

            var combinations = _productCombinationsService.GetAllCombinations();
            var grade = combinations?.FirstOrDefault(c => string.Equals(c.grade_id, gradeId, StringComparison.OrdinalIgnoreCase));
            return grade?.grade_name ?? "Unknown";
        }

        private string GetCountName(string countId)
        {
            if (string.IsNullOrEmpty(countId) || _productCombinationsService == null || !_productCombinationsService.HasCachedData())
                return "Unknown";

            var combinations = _productCombinationsService.GetAllCombinations();
            var count = combinations?.FirstOrDefault(c => string.Equals(c.count_id, countId, StringComparison.OrdinalIgnoreCase));
            return count?.count_name ?? "Unknown";
        }

        private void LoadCropsData()
        {
            var crops = _productCombinationsService.GetUniqueCrops();

            foreach (var crop in crops)
            {
                _currentDataGridView.Rows.Add(crop.crop_id, crop.crop_name);
            }
        }

        private void LoadProductsData()
        {
            var combinations = _productCombinationsService.GetAllCombinations();
            var products = combinations
                .GroupBy(c => c.product_id)
                .Select(g => new { product_id = g.Key, product_name = g.First().product_name })
                .OrderBy(p => p.product_name)
                .ToList();

            foreach (var product in products)
            {
                _currentDataGridView.Rows.Add(product.product_id, product.product_name);
            }
        }

        private void LoadVarietyData()
        {
            var combinations = _productCombinationsService.GetAllCombinations();
            var varieties = combinations
                .GroupBy(c => c.variety_id)
                .Select(g => new { variety_id = g.Key, variety_name = g.First().variety_name })
                .OrderBy(v => v.variety_name)
                .ToList();

            foreach (var variety in varieties)
            {
                _currentDataGridView.Rows.Add(variety.variety_id, variety.variety_name);
            }
        }

        private void LoadGradeData()
        {
            var combinations = _productCombinationsService.GetAllCombinations();
            var grades = combinations
                .GroupBy(c => c.grade_id)
                .Select(g => new { grade_id = g.Key, grade_name = g.First().grade_name })
                .OrderBy(g => g.grade_name)
                .ToList();

            foreach (var grade in grades)
            {
                _currentDataGridView.Rows.Add(grade.grade_id, grade.grade_name);
            }
        }

        private void LoadCountData()
        {
            var combinations = _productCombinationsService.GetAllCombinations();
            var counts = combinations
                .GroupBy(c => c.count_id)
                .Select(g => new { count_id = g.Key, count_name = g.First().count_name })
                .OrderBy(c => c.count_name)
                .ToList();

            foreach (var count in counts)
            {
                _currentDataGridView.Rows.Add(count.count_id, count.count_name);
            }
        }

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
            this.components = new System.ComponentModel.Container();
            this.Text = "Crops & products";
        }

        #endregion
    }
}
