using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScanLink
{
    // Waterfall "create a new product combination" dialog: crop -> variety -> grade -> count ->
    // carton type -> avg weight, each step enabled only once the previous one is chosen. Crop and
    // variety are picked from EXISTING master values only (not creatable here). Grade, count and
    // carton type can EITHER be picked from existing values OR created on the fly via a trailing
    // "+ Add new..." option in each of those three dropdowns.
    public partial class AddCombinationDialog : Form
    {
        // Sentinel value id for the trailing "+ Add new..." row in the grade/count/carton-type
        // dropdowns. Never sent to the server - selecting it triggers a create-then-select flow.
        private const string NewItemId = "__new__";

        private class ComboOption
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }

        private readonly ProductCombinationsService _productCombinationsService;

        // Set while we're programmatically changing a combo's selection (after inserting a
        // newly-created item, or reverting a cancelled "add new") so that doesn't itself
        // re-trigger the SelectedIndexChanged handler.
        private bool _suppressSelectionEvents;

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
            gradeCombo.SelectedIndexChanged += GradeCombo_SelectedIndexChanged;
            countCombo.SelectedIndexChanged += CountCombo_SelectedIndexChanged;
            cartonTypeCombo.SelectedIndexChanged += CartonTypeCombo_SelectedIndexChanged;
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
            if (_suppressSelectionEvents || current.SelectedIndex < 0) return;
            populateNext();
            UpdateCreateButtonEnabled();
        }

        private async void GradeCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suppressSelectionEvents || gradeCombo.SelectedIndex < 0) return;
            if ((string)gradeCombo.SelectedValue == NewItemId)
            {
                await HandleAddNewGrade();
                return;
            }
            PopulateCountStep();
            UpdateCreateButtonEnabled();
        }

        private async void CountCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suppressSelectionEvents || countCombo.SelectedIndex < 0) return;
            if ((string)countCombo.SelectedValue == NewItemId)
            {
                await HandleAddNewCount();
                return;
            }
            PopulateCartonTypeStep();
            UpdateCreateButtonEnabled();
        }

        private async void CartonTypeCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suppressSelectionEvents || cartonTypeCombo.SelectedIndex < 0) return;
            if ((string)cartonTypeCombo.SelectedValue == NewItemId)
            {
                await HandleAddNewCartonType();
                return;
            }
            EnableWeightInputForCartonSelection();
        }

        private void EnableWeightInputForCartonSelection()
        {
            avgWeightInput.Enabled = cartonTypeCombo.SelectedIndex >= 0;
            avgWeightInput.Value = 0;
            UpdateCreateButtonEnabled();
        }

        // Prompts for a name via the same lightweight VB InputBox already used elsewhere in this
        // app (Form1's LAN-target prompt), creates the grade on the server, and - on success -
        // inserts it into gradeCombo's list and selects it (as if the user had picked an
        // existing row) instead of re-deriving options from cached combinations, since a
        // brand-new grade has no combinations referencing it yet.
        private async Task HandleAddNewGrade()
        {
            string name = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter the new grade name (e.g. \"Class 3\"):", "Add New Grade", "");
            if (string.IsNullOrWhiteSpace(name))
            {
                ResetComboSelection(gradeCombo);
                return;
            }

            SetBusyStatus("Creating grade...");
            var result = await _productCombinationsService.CreateGradeAsync(name.Trim());
            if (result.Success && result.Data != null)
            {
                InsertAndSelectNewItem(gradeCombo, result.Data.id, result.Data.name);
                ClearStatus();
                PopulateCountStep();
            }
            else
            {
                SetErrorStatus(result.ErrorMessage ?? "Failed to create grade.");
                ResetComboSelection(gradeCombo);
            }
            UpdateCreateButtonEnabled();
        }

        private async Task HandleAddNewCount()
        {
            string name = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter the new count/size (e.g. \"75\"):", "Add New Count", "");
            if (string.IsNullOrWhiteSpace(name))
            {
                ResetComboSelection(countCombo);
                return;
            }

            SetBusyStatus("Creating count...");
            var result = await _productCombinationsService.CreateCountAsync(name.Trim());
            if (result.Success && result.Data != null)
            {
                InsertAndSelectNewItem(countCombo, result.Data.id, result.Data.name);
                ClearStatus();
                PopulateCartonTypeStep();
            }
            else
            {
                SetErrorStatus(result.ErrorMessage ?? "Failed to create count.");
                ResetComboSelection(countCombo);
            }
            UpdateCreateButtonEnabled();
        }

        private async Task HandleAddNewCartonType()
        {
            string name = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter the new carton type name (e.g. \"F20D - 20kg\"):", "Add New Carton Type", "");
            if (string.IsNullOrWhiteSpace(name))
            {
                ResetComboSelection(cartonTypeCombo);
                return;
            }

            string weightText = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter the empty carton weight in kg (e.g. \"15\"):", "Add New Carton Type", "0");
            if (!double.TryParse(weightText, out double weightKg) || weightKg < 0)
            {
                SetErrorStatus("Weight must be a non-negative number.");
                ResetComboSelection(cartonTypeCombo);
                return;
            }

            SetBusyStatus("Creating carton type...");
            var result = await _productCombinationsService.CreateCartonTypeAsync(name.Trim(), weightKg);
            if (result.Success && result.Data != null)
            {
                InsertAndSelectNewItem(cartonTypeCombo, result.Data.id, result.Data.name);
                ClearStatus();
                EnableWeightInputForCartonSelection();
            }
            else
            {
                SetErrorStatus(result.ErrorMessage ?? "Failed to create carton type.");
                ResetComboSelection(cartonTypeCombo);
            }
            UpdateCreateButtonEnabled();
        }

        private void SetBusyStatus(string text)
        {
            createButton.Enabled = false;
            statusLabel.ForeColor = Color.FromArgb(52, 73, 94);
            statusLabel.Text = text;
        }

        private void SetErrorStatus(string text)
        {
            statusLabel.ForeColor = Color.FromArgb(192, 57, 43);
            statusLabel.Text = text;
        }

        private void ClearStatus()
        {
            statusLabel.Text = "";
        }

        private void ResetComboSelection(ComboBox combo)
        {
            _suppressSelectionEvents = true;
            combo.SelectedIndex = -1;
            _suppressSelectionEvents = false;
        }

        // Inserts a freshly-created {id, name} row just before the trailing "+ Add new..." row
        // and selects it, without re-triggering SelectedIndexChanged (the DataSource reset would
        // otherwise fire it with an indeterminate intermediate selection).
        private void InsertAndSelectNewItem(ComboBox combo, string id, string name)
        {
            var list = (combo.DataSource as List<ComboOption>) ?? new List<ComboOption>();
            int insertAt = list.FindIndex(o => o.Id == NewItemId);
            var newOption = new ComboOption { Id = id, Name = name };
            if (insertAt >= 0) list.Insert(insertAt, newOption);
            else list.Add(newOption);

            _suppressSelectionEvents = true;
            combo.DataSource = null;
            combo.DataSource = list;
            combo.DisplayMember = "Name";
            combo.ValueMember = "Id";
            combo.SelectedValue = id;
            _suppressSelectionEvents = false;
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
                g => g.grade_id, g => g.grade_name, "+ Add new grade...");
            gradeCombo.Enabled = true;
            ResetStepsFrom(countCombo, cartonTypeCombo);
        }

        private void PopulateCountStep()
        {
            BindCombo(countCombo, _productCombinationsService.GetUniqueCounts(),
                c => c.count_id, c => c.count_name, "+ Add new count...");
            countCombo.Enabled = true;
            ResetStepsFrom(cartonTypeCombo);
        }

        private void PopulateCartonTypeStep()
        {
            BindCombo(cartonTypeCombo, _productCombinationsService.GetUniqueCartonTypes(),
                c => c.carton_type_id, c => c.carton_type_name, "+ Add new carton type...");
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

        private static void BindCombo<T>(ComboBox combo, List<T> items, Func<T, string> idSelector, Func<T, string> nameSelector, string addNewLabel = null)
        {
            var rows = items
                .Select(i => new ComboOption { Id = idSelector(i), Name = nameSelector(i) })
                .ToList();
            if (addNewLabel != null)
                rows.Add(new ComboOption { Id = NewItemId, Name = addNewLabel });

            combo.DataSource = rows;
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
