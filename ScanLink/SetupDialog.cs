using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScanLink
{
    public partial class SetupDialog : Form
    {
        private ProductCombinationsService _productCombinationsService;
        private Button _selectedButton;
        private DataGridView _currentDataGridView;
        private ComboBox _cropFilterComboBox;
        private Label _cropFilterLabel;
        private ComboBox _varietyFilterComboBox;
        private Label _varietyFilterLabel;
        private Panel _filterPanel;

        public SetupDialog(ProductCombinationsService productCombinationsService)
        {
            InitializeComponent();
            _productCombinationsService = productCombinationsService;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(800, 600);

            InitializeDialog();
        }

        private void InitializeDialog()
        {
            // Create button panel at top
            Panel buttonPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.LightGray
            };

            // Create buttons
            Button combinationTableButton = CreateButton("Combination Table", 0);
            Button cropsButton = CreateButton("Crops", 1);
            Button productsButton = CreateButton("Products", 2);
            Button varietyButton = CreateButton("Variety", 3);
            Button gradeButton = CreateButton("Grade", 4);
            Button countButton = CreateButton("Count", 5);

            buttonPanel.Controls.AddRange(new Control[] {
                combinationTableButton, cropsButton, productsButton,
                varietyButton, gradeButton, countButton
            });

            // Create filter panel (initially hidden)
            _filterPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.LightBlue,
                Visible = false // Hidden by default
            };

            // Create filter controls
            CreateFilterControls();

            // Create main panel for tables
            Panel mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            // Create DataGridViews for each table
            CreateCombinationTable();
            CreateIndividualTables();

            mainPanel.Controls.Add(_currentDataGridView);

            this.Controls.Add(mainPanel);
            this.Controls.Add(_filterPanel);
            this.Controls.Add(buttonPanel);

            // Default selection - set Combination Table as selected
            _selectedButton = combinationTableButton;
            _selectedButton.BackColor = Color.LightBlue;
            ShowTable("Combination Table");
        }

        private Button CreateButton(string text, int index)
        {
            Button button = new Button
            {
                Text = text,
                Size = new Size(120, 40),
                Location = new Point(index * 125 + 10, 5),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Tag = text
            };

            button.Click += Button_Click;
            return button;
        }

        private void Button_Click(object sender, EventArgs e)
        {
            Button clickedButton = (Button)sender;

            // Unselect previous button
            if (_selectedButton != null)
            {
                _selectedButton.BackColor = Color.White;
            }

            // Select new button
            _selectedButton = clickedButton;
            _selectedButton.BackColor = Color.LightBlue;

            // Show corresponding table
            ShowTable(clickedButton.Text);
        }

        private void ShowTable(string tableType)
        {
            // Add new table
            Panel mainPanel = (Panel)this.Controls[0]; // Main panel is the first control
            mainPanel.Controls.Clear();

            // Show/hide filter panel based on table type
            _filterPanel.Visible = (tableType == "Combination Table");

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

            mainPanel.Controls.Add(_currentDataGridView);
            LoadTableData(tableType);
        }

        private void CreateFilterControls()
        {
            // Crop filter controls
            _cropFilterLabel = new Label
            {
                Text = "Crop:",
                Location = new Point(10, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            _cropFilterComboBox = new ComboBox
            {
                Location = new Point(45, 7),
                Size = new Size(180, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Populate crop filter dropdown
            _cropFilterComboBox.Items.Add("All Crops");
            if (_productCombinationsService != null && _productCombinationsService.HasCachedData())
            {
                var crops = _productCombinationsService.GetUniqueCrops();
                foreach (var crop in crops.OrderBy(c => c.crop_name))
                {
                    _cropFilterComboBox.Items.Add($"{crop.crop_id} - {crop.crop_name}");
                }
            }
            _cropFilterComboBox.SelectedIndex = 0; // Default to "All Crops"

            // Variety filter controls
            _varietyFilterLabel = new Label
            {
                Text = "Variety:",
                Location = new Point(250, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            _varietyFilterComboBox = new ComboBox
            {
                Location = new Point(300, 7),
                Size = new Size(180, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Initially populate variety filter dropdown with all varieties
            PopulateVarietyDropdown();

            // Event handlers
            _cropFilterComboBox.SelectedIndexChanged += FilterComboBox_SelectedIndexChanged;
            _varietyFilterComboBox.SelectedIndexChanged += FilterComboBox_SelectedIndexChanged;

            _filterPanel.Controls.Add(_cropFilterLabel);
            _filterPanel.Controls.Add(_cropFilterComboBox);
            _filterPanel.Controls.Add(_varietyFilterLabel);
            _filterPanel.Controls.Add(_varietyFilterComboBox);
        }

        private void PopulateVarietyDropdown()
        {
            _varietyFilterComboBox.Items.Clear();
            _varietyFilterComboBox.Items.Add("All Varieties");

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
        }

        private void CreateCombinationTable()
        {
            DataGridView dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ScrollBars = ScrollBars.Both
            };

            dataGridView.Columns.Add("id", "ID");
            dataGridView.Columns.Add("crop_id", "Crop ID");
            dataGridView.Columns.Add("product_id", "Product ID");
            dataGridView.Columns.Add("variety_id", "Variety ID");
            dataGridView.Columns.Add("grade_id", "Grade ID");
            dataGridView.Columns.Add("count_id", "Count ID");
            dataGridView.Columns.Add("carton_type", "Carton Type");
            dataGridView.Columns.Add("avg_weight_kg", "Avg Weight (KG)");

            _currentDataGridView = dataGridView;
        }

        private void CreateIndividualTables()
        {
            // These will be created on demand in their respective methods
        }

        private void CreateCropsTable()
        {
            CreateIndividualTable("Crop ID", "Crop Name");
        }

        private void CreateProductsTable()
        {
            CreateIndividualTable("Product ID", "Product Name");
        }

        private void CreateVarietyTable()
        {
            CreateIndividualTable("Variety ID", "Variety Name");
        }

        private void CreateGradeTable()
        {
            CreateIndividualTable("Grade ID", "Grade Name");
        }

        private void CreateCountTable()
        {
            CreateIndividualTable("Count ID", "Count Name");
        }

        private void CreateIndividualTable(string idColumnName, string nameColumnName)
        {
            DataGridView dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ScrollBars = ScrollBars.Both
            };

            dataGridView.Columns.Add("id", idColumnName);
            dataGridView.Columns.Add("name", nameColumnName);

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
                    GetCartonTypeName(combo.carton_type_id),
                    combo.avg_weight_kg
                );
            }
        }

        private string GetCartonTypeName(string cartonId)
        {
            if (string.IsNullOrWhiteSpace(cartonId)) return "Unknown";
            
            if (cartonId == "001") return "D15D - 15kg";
            if (cartonId == "002") return "E15D - 15kg";
            if (cartonId == "003") return "J60B - 600kg";
            if (cartonId == "004") return "Bins - 300kg";
            if (cartonId == "005") return "Foldable Bins - 350kg";

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
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Text = "Setup - Data Tables";
        }

        #endregion
    }
}
