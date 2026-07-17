using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ScanLink
{
    // Waterfall "create a new product combination" dialog: crop -> variety -> grade -> count ->
    // carton type -> avg weight, each step enabled only once the previous one is chosen. Crop and
    // variety (and every other level) are picked from EXISTING master values only - nothing here
    // creates a new crop/variety/grade/count/carton_type, only a new product_combination (and, if
    // needed, its backing product) that combines values that already exist.
    public partial class AddCombinationDialog : Form
    {
        private readonly ProductCombinationsService _productCombinationsService;

        private ComboBox cropCombo;
        private ComboBox varietyCombo;
        private ComboBox gradeCombo;
        private ComboBox countCombo;
        private ComboBox cartonTypeCombo;
        private NumericUpDown avgWeightInput;
        private Button createButton;
        private Button cancelButton;
        private Label statusLabel;

        public ProductCombination CreatedCombination { get; private set; }

        public AddCombinationDialog(ProductCombinationsService productCombinationsService)
        {
            _productCombinationsService = productCombinationsService;
            InitializeComponent();
            PopulateCropStep();
        }

        private void InitializeComponent()
        {
            this.Text = "Add Product Combination";
            this.Size = new Size(420, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };
            this.Controls.Add(panel);

            int y = 10;
            const int rowHeight = 55;

            AddStepRow(panel, ref y, rowHeight, "1. Crop", out cropCombo);
            AddStepRow(panel, ref y, rowHeight, "2. Variety", out varietyCombo);
            AddStepRow(panel, ref y, rowHeight, "3. Grade", out gradeCombo);
            AddStepRow(panel, ref y, rowHeight, "4. Count", out countCombo);
            AddStepRow(panel, ref y, rowHeight, "5. Carton Type", out cartonTypeCombo);

            var weightLabel = new Label
            {
                Text = "6. Avg Weight (kg)",
                Location = new Point(0, y),
                AutoSize = true,
                ForeColor = Color.FromArgb(52, 73, 94)
            };
            panel.Controls.Add(weightLabel);

            avgWeightInput = new NumericUpDown
            {
                Location = new Point(0, y + 20),
                Size = new Size(150, 24),
                DecimalPlaces = 2,
                Minimum = 0,
                Maximum = 100000,
                Increment = 0.1M,
                Enabled = false
            };
            panel.Controls.Add(avgWeightInput);
            y += rowHeight;

            statusLabel = new Label
            {
                Location = new Point(0, y),
                Size = new Size(380, 55),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(192, 57, 43),
                Text = ""
            };
            panel.Controls.Add(statusLabel);
            y += 60;

            createButton = new Button
            {
                Text = "Create",
                Location = new Point(200, y),
                Size = new Size(90, 32),
                BackColor = Color.FromArgb(13, 110, 253),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            createButton.FlatAppearance.BorderSize = 0;
            createButton.Click += CreateButton_Click;
            panel.Controls.Add(createButton);

            cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(300, y),
                Size = new Size(90, 32),
                DialogResult = DialogResult.Cancel
            };
            panel.Controls.Add(cancelButton);
            this.CancelButton = cancelButton;

            cropCombo.SelectedIndexChanged += (s, e) => OnStepSelected(cropCombo, varietyCombo, PopulateVarietyStep);
            varietyCombo.SelectedIndexChanged += (s, e) => OnStepSelected(varietyCombo, gradeCombo, PopulateGradeStep);
            gradeCombo.SelectedIndexChanged += (s, e) => OnStepSelected(gradeCombo, countCombo, PopulateCountStep);
            countCombo.SelectedIndexChanged += (s, e) => OnStepSelected(countCombo, cartonTypeCombo, PopulateCartonTypeStep);
            cartonTypeCombo.SelectedIndexChanged += (s, e) => OnCartonTypeSelected();
            avgWeightInput.ValueChanged += (s, e) => UpdateCreateButtonEnabled();
        }

        private void AddStepRow(Panel parent, ref int y, int rowHeight, string labelText, out ComboBox combo)
        {
            var label = new Label
            {
                Text = labelText,
                Location = new Point(0, y),
                AutoSize = true,
                ForeColor = Color.FromArgb(52, 73, 94)
            };
            parent.Controls.Add(label);

            var box = new ComboBox
            {
                Location = new Point(0, y + 20),
                Size = new Size(380, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false
            };
            parent.Controls.Add(box);
            combo = box;
            y += rowHeight;
        }

        private void OnStepSelected(ComboBox current, ComboBox next, Action populateNext)
        {
            if (current.SelectedIndex < 0) return;
            populateNext();
            UpdateCreateButtonEnabled();
        }

        private void OnCartonTypeSelected()
        {
            avgWeightInput.Enabled = cartonTypeCombo.SelectedIndex >= 0;
            UpdateCreateButtonEnabled();
        }

        private void UpdateCreateButtonEnabled()
        {
            createButton.Enabled = cropCombo.SelectedIndex >= 0
                && varietyCombo.SelectedIndex >= 0
                && gradeCombo.SelectedIndex >= 0
                && countCombo.SelectedIndex >= 0
                && cartonTypeCombo.SelectedIndex >= 0
                && avgWeightInput.Value > 0;
        }

        private void PopulateCropStep()
        {
            BindCombo(cropCombo, _productCombinationsService.GetUniqueCrops(),
                c => c.crop_id, c => c.crop_name);
            cropCombo.Enabled = cropCombo.Items.Count > 0;
            if (cropCombo.Items.Count == 0)
                statusLabel.Text = "No crops available - fetch product combinations first.";
        }

        private void PopulateVarietyStep()
        {
            BindCombo(varietyCombo, _productCombinationsService.GetUniqueVarieties(),
                v => v.variety_id, v => v.variety_name);
            varietyCombo.Enabled = true;
            ResetStepsFrom(gradeCombo, countCombo, cartonTypeCombo);
        }

        private void PopulateGradeStep()
        {
            BindCombo(gradeCombo, _productCombinationsService.GetUniqueGrades(),
                g => g.grade_id, g => g.grade_name);
            gradeCombo.Enabled = true;
            ResetStepsFrom(countCombo, cartonTypeCombo);
        }

        private void PopulateCountStep()
        {
            BindCombo(countCombo, _productCombinationsService.GetUniqueCounts(),
                c => c.count_id, c => c.count_name);
            countCombo.Enabled = true;
            ResetStepsFrom(cartonTypeCombo);
        }

        private void PopulateCartonTypeStep()
        {
            BindCombo(cartonTypeCombo, _productCombinationsService.GetUniqueCartonTypes(),
                c => c.carton_type_id, c => c.carton_type_name);
            cartonTypeCombo.Enabled = true;
            avgWeightInput.Enabled = false;
            avgWeightInput.Value = 0;
        }

        private void ResetStepsFrom(params ComboBox[] combos)
        {
            foreach (var combo in combos)
            {
                combo.DataSource = null;
                combo.Items.Clear();
                combo.Enabled = false;
            }
            avgWeightInput.Enabled = false;
            avgWeightInput.Value = 0;
        }

        private static void BindCombo<T>(ComboBox combo, List<T> items, Func<T, string> idSelector, Func<T, string> nameSelector)
        {
            combo.DataSource = items
                .Select(i => new { Id = idSelector(i), Name = nameSelector(i) })
                .ToList();
            combo.DisplayMember = "Name";
            combo.ValueMember = "Id";
            combo.SelectedIndex = -1;
        }

        private async void CreateButton_Click(object sender, EventArgs e)
        {
            createButton.Enabled = false;
            statusLabel.ForeColor = Color.FromArgb(52, 73, 94);
            statusLabel.Text = "Creating combination...";

            string cropId = cropCombo.SelectedValue?.ToString();
            string varietyId = varietyCombo.SelectedValue?.ToString();
            string gradeId = gradeCombo.SelectedValue?.ToString();
            string countId = countCombo.SelectedValue?.ToString();
            string cartonTypeId = cartonTypeCombo.SelectedValue?.ToString();
            double avgWeightKg = (double)avgWeightInput.Value;

            var result = await _productCombinationsService.CreateProductCombinationAsync(
                cropId, varietyId, gradeId, countId, cartonTypeId, avgWeightKg);

            if (result.Success)
            {
                CreatedCombination = result.Data;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                statusLabel.ForeColor = Color.FromArgb(192, 57, 43);
                statusLabel.Text = result.ErrorMessage ?? "Failed to create combination.";
                createButton.Enabled = true;
            }
        }
    }
}
