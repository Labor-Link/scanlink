using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScanLink.DesignSystem;

namespace ScanLink
{
    // Waterfall "create a new product combination" dialog: crop -> variety -> grade -> count ->
    // carton type -> avg weight, each step enabled only once the previous one is chosen. Crop and
    // variety are picked from EXISTING master values only (not creatable here). Grade, count and
    // carton type can EITHER be picked from existing values OR created on the fly via a trailing
    // "+ Add new..." option in each of those three dropdowns.
    internal partial class AddCombinationDialog : SLDialog
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
        private SLNumberBox avgWeightInput;
        private SLButton createButton;
        private SLButton cancelButton;
        private SLBanner statusBanner;

        public ProductCombination CreatedCombination { get; private set; }

        public AddCombinationDialog(ProductCombinationsService productCombinationsService)
        {
            _productCombinationsService = productCombinationsService;
            InitializeComponent();
            PopulateCropStep();
        }

        // Layout: SLDialog with a 2-column SLFieldSet (crop/variety, grade/count,
        // carton type/avg weight), a status banner, and Cancel + Create in the footer.
        private void InitializeComponent()
        {
            Title = "Add product combination";
            Description = "Choose each step in order. The next one unlocks when the one before it is set.";
            DialogWidth = 560;

            var fields = new SLFieldSet { Columns = 2 };
            cropCombo = AddStep(fields, "Crop", "Choose a crop");
            varietyCombo = AddStep(fields, "Variety", "Choose a variety");
            gradeCombo = AddStep(fields, "Grade", "Choose a grade");
            countCombo = AddStep(fields, "Count", "Choose a count");
            cartonTypeCombo = AddStep(fields, "Carton type", "Choose a carton type");

            avgWeightInput = new SLNumberBox
            {
                Unit = "kg",
                DecimalPlaces = 2,
                Minimum = 0,
                Maximum = 100000,
                Increment = 0.1M,
                Enabled = false
            };
            fields.Add(new SLField("Avg weight", avgWeightInput) { Required = true });
            Body.Controls.Add(fields);

            statusBanner = new SLBanner { Tone = SLTone.Error, IconName = "alert-circle" };
            Body.Controls.Add(statusBanner);
            SLVisibility.Set(statusBanner, false);

            cancelButton = new SLButton { Text = "Cancel", Variant = SLVariant.Secondary, DialogResult = DialogResult.Cancel };
            createButton = new SLButton { Text = "Create combination", Enabled = false };
            createButton.Click += CreateButton_Click;
            AddAction(cancelButton);
            AddAction(createButton);
            CancelButton = cancelButton;

            cropCombo.SelectedIndexChanged += (s, e) => OnStepSelected(cropCombo, varietyCombo, PopulateVarietyStep);
            varietyCombo.SelectedIndexChanged += (s, e) => OnStepSelected(varietyCombo, gradeCombo, PopulateGradeStep);
            gradeCombo.SelectedIndexChanged += GradeCombo_SelectedIndexChanged;
            countCombo.SelectedIndexChanged += CountCombo_SelectedIndexChanged;
            cartonTypeCombo.SelectedIndexChanged += CartonTypeCombo_SelectedIndexChanged;
            avgWeightInput.ValueChanged += (s, e) => UpdateCreateButtonEnabled();
        }

        private static ComboBox AddStep(SLFieldSet fields, string label, string placeholder)
        {
            var box = new SLComboBox { PlaceholderText = placeholder, Enabled = false };
            fields.Add(new SLField(label, box) { Required = true });
            return box;
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
            if (SelectedId(gradeCombo) == NewItemId)
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
            if (SelectedId(countCombo) == NewItemId)
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
            if (SelectedId(cartonTypeCombo) == NewItemId)
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

        // Prompts for a name with SLPrompt, creates the grade on the server, and - on success -
        // inserts it into gradeCombo's list and selects it (as if the user had picked an
        // existing row) instead of re-deriving options from cached combinations, since a
        // brand-new grade has no combinations referencing it yet.
        private async Task HandleAddNewGrade()
        {
            string name = SLPrompt.Ask(this, "Add a new grade", "Grade name", hint: "For example \"Class 3\".", confirmText: "Add grade");
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
            string name = SLPrompt.Ask(this, "Add a new count", "Count or size", hint: "For example \"75\".", confirmText: "Add count");
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
            string name = SLPrompt.Ask(this, "Add a new carton type", "Carton type name", hint: "For example \"F20D - 20kg\".", confirmText: "Next");
            if (string.IsNullOrWhiteSpace(name))
            {
                ResetComboSelection(cartonTypeCombo);
                return;
            }

            string weightText = SLPrompt.Ask(this, "Add a new carton type", "Empty carton weight (kg)", "0", hint: "For example \"15\".", confirmText: "Add carton type");
            if (weightText == null)
            {
                ResetComboSelection(cartonTypeCombo);
                return;
            }
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
            ShowStatus(SLTone.Info, "loader", text);
        }

        private void SetErrorStatus(string text)
        {
            ShowStatus(SLTone.Error, "alert-circle", text);
        }

        private void ClearStatus()
        {
            SLVisibility.Set(statusBanner, false);
        }

        private void ShowStatus(SLTone tone, string icon, string text)
        {
            statusBanner.Tone = tone;
            statusBanner.IconName = icon;
            statusBanner.Message = text;
            SLVisibility.Set(statusBanner, !string.IsNullOrEmpty(text));
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
            try
            {
                combo.DataSource = null;
                combo.DisplayMember = "Name";
                combo.ValueMember = "Id";
                combo.DataSource = list;
                combo.SelectedValue = id;
            }
            finally
            {
                _suppressSelectionEvents = false;
            }
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
                SetErrorStatus("No crops are available yet. Fetch product combinations first, then try again.");
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

        // Binds {id, name} rows to a combo. DisplayMember/ValueMember MUST be set before the
        // DataSource: assigning DataSource snaps SelectedIndex to 0 and raises
        // SelectedIndexChanged synchronously, and until ValueMember is set SelectedValue returns
        // the whole ComboOption rather than its Id. Selection events are suppressed for the same
        // reason - the intermediate selection here is an artifact of binding, not a user choice.
        private void BindCombo<T>(ComboBox combo, List<T> items, Func<T, string> idSelector, Func<T, string> nameSelector, string addNewLabel = null)
        {
            var rows = items
                .Select(i => new ComboOption { Id = idSelector(i), Name = nameSelector(i) })
                .ToList();
            if (addNewLabel != null)
                rows.Add(new ComboOption { Id = NewItemId, Name = addNewLabel });

            _suppressSelectionEvents = true;
            try
            {
                combo.DataSource = null;
                combo.DisplayMember = "Name";
                combo.ValueMember = "Id";
                combo.DataSource = rows;
                combo.SelectedIndex = -1;
            }
            finally
            {
                _suppressSelectionEvents = false;
            }
        }

        // Selected row's id, or null when nothing (or a not-yet-bound row) is selected. Never
        // hard-cast SelectedValue: mid-rebind it can still be the bound ComboOption itself.
        private static string SelectedId(ComboBox combo)
        {
            return combo.SelectedValue as string;
        }

        private async void CreateButton_Click(object sender, EventArgs e)
        {
            createButton.Loading = true;
            ShowStatus(SLTone.Info, "loader", "Creating combination…");

            string cropId = cropCombo.SelectedValue?.ToString();
            string varietyId = varietyCombo.SelectedValue?.ToString();
            string gradeId = gradeCombo.SelectedValue?.ToString();
            string countId = countCombo.SelectedValue?.ToString();
            string cartonTypeId = cartonTypeCombo.SelectedValue?.ToString();
            double avgWeightKg = (double)avgWeightInput.Value;

            var result = await _productCombinationsService.CreateProductCombinationAsync(
                cropId, varietyId, gradeId, countId, cartonTypeId, avgWeightKg);

            createButton.Loading = false;
            if (result.Success)
            {
                CreatedCombination = result.Data;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                SetErrorStatus(result.ErrorMessage ?? "The combination couldn't be created. Try again.");
                createButton.Enabled = true;
            }
        }
    }
}
