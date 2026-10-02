using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScanLink.DesignSystem;

namespace ScanLink
{
    internal partial class EmployeeSelectionDialog : SLDialog
    {
        private ApiAuthService _apiAuthService;
        private SLTable employeeDataGridView;

        private SLTextBox searchTextBox;
        private SLButton searchButton;
        private CheckedListBox departmentCheckedListBox;
        private SLButton selectButton;
        private SLButton cancelButton;
        private SLText countLabel;
        private ApiAuthService.EmployeeInfo _selectedEmployee;
        private SLButton prevPageButton;
        private SLButton nextPageButton;
        private SLText pageInfoLabel;
        private int _currentPage = 0;

        public ApiAuthService.EmployeeInfo SelectedEmployee => _selectedEmployee;

        public EmployeeSelectionDialog(ApiAuthService apiAuthService)
        {
            _apiAuthService = apiAuthService;
            InitializeComponent();
        }

        // Layout (SLDialog, 800 wide, fixed body height so the table scrolls):
        //   [Name search ................................] [Search]
        //   [Departments: multi-column checked list            ]
        //   SLCard: employee table / footer: count · Previous · page · Next
        //   Footer: Cancel · Select employee
        private void InitializeComponent()
        {
            Title = "Choose an employee";
            Description = "Search by name or employee ID, then pick the person who is picking.";
            DialogWidth = 800;
            BodyHeight = 470;

            searchTextBox = new SLTextBox { PrefixIcon = "search", PlaceholderText = "Search by name or employee ID" };
            searchTextBox.Inner.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; searchButton.PerformClick(); } };
            searchButton = new SLButton { Text = "Search", Variant = SLVariant.Secondary, IconName = "search" };
            searchButton.Click += SearchButton_Click;

            var searchRow = new SLStack(SLOrientation.Horizontal, 12) { Align = SLAlign.End };
            var nameField = new SLField("Name", searchTextBox);
            searchRow.AddRange(nameField, searchButton);
            searchRow.SetGrow(nameField);
            Body.Controls.Add(searchRow);

            departmentCheckedListBox = new CheckedListBox { CheckOnClick = true, MultiColumn = true, ColumnWidth = 190 };
            Body.Controls.Add(new SLField("Departments", new SLFrame(departmentCheckedListBox) { Height = 76 }));

            employeeDataGridView = new SLTable { Height = 200 };
            employeeDataGridView.EmptyState = new SLEmptyState
            {
                Compact = true,
                IconName = "users",
                Title = "No employees match",
                Description = "Try a shorter name, or tick more departments."
            };

            prevPageButton = new SLButton { Text = "Previous", Variant = SLVariant.Secondary, ButtonSize = SLSize.Sm, IconName = "chevron-left", Enabled = false };
            prevPageButton.Click += PrevPageButton_Click;
            nextPageButton = new SLButton { Text = "Next", Variant = SLVariant.Secondary, ButtonSize = SLSize.Sm, IconEndName = "chevron-right" };
            nextPageButton.Click += NextPageButton_Click;
            pageInfoLabel = new SLText("Page 1 of 1", SLTextStyle.Muted) { SingleLine = true };
            countLabel = new SLText("Total: 0 employees", SLTextStyle.Muted) { SingleLine = true };

            var table = new SLCard { BodyPadding = Padding.Empty };
            table.Body.Controls.Add(employeeDataGridView);
            table.Body.SetGrow(employeeDataGridView);
            table.Footer.Justify = SLJustify.SpaceBetween;
            var pager = new SLStack(SLOrientation.Horizontal, 8) { Align = SLAlign.Center };
            pager.AddRange(prevPageButton, pageInfoLabel, nextPageButton);
            table.Footer.AddRange(countLabel, pager);
            Body.Controls.Add(table);
            Body.SetGrow(table);

            cancelButton = new SLButton { Text = "Cancel", Variant = SLVariant.Secondary, DialogResult = DialogResult.Cancel };
            selectButton = new SLButton { Text = "Select employee", Enabled = false };
            selectButton.Click += SelectButton_Click;
            AddAction(cancelButton);
            AddAction(selectButton);
            CancelButton = cancelButton;

            InitializeDataGridView();
            employeeDataGridView.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && selectButton.Enabled) selectButton.PerformClick(); };

            this.Load += EmployeeSelectionDialog_Load;
        }

        private void InitializeDataGridView()
        {
            employeeDataGridView.Columns.Clear();
            employeeDataGridView.Columns.Add("employee_site_id", "Employee ID");
            employeeDataGridView.Columns.Add("first_name", "First Name");
            employeeDataGridView.Columns.Add("last_name", "Last Name");
            employeeDataGridView.Columns.Add("department", "Department");
            employeeDataGridView.Columns.Add("user_id", "user ID");
            employeeDataGridView.Columns.Add("scanlink_id", "ScanLink ID");

            // Set column widths
            employeeDataGridView.Columns["employee_site_id"].Width = 50;
            employeeDataGridView.Columns["first_name"].Width = 70;
            employeeDataGridView.Columns["last_name"].Width = 70;
            employeeDataGridView.Columns["department"].Width = 70;
            employeeDataGridView.Columns["user_id"].Width = 250;
            employeeDataGridView.Columns["scanlink_id"].Visible = false;

            employeeDataGridView.SetMono("employee_site_id");
            employeeDataGridView.SetMuted("user_id");

            // Handle row selection
            employeeDataGridView.SelectionChanged += EmployeeDataGridView_SelectionChanged;
        }

        private async void EmployeeSelectionDialog_Load(object sender, EventArgs e)
        {
            await LoadDepartmentsAsync();
            await LoadEmployees(_currentPage);
        }

        private async void SearchButton_Click(object sender, EventArgs e)
        {
            _currentPage = 0;
            await LoadEmployees(_currentPage);
        } 

        private async Task LoadEmployees(int pageNo = 0)
        {
            try
            {
                // statusLabel.Text = "Loading employees...";
                // statusLabel.ForeColor = Color.FromArgb(52, 152, 219);
                searchButton.Enabled = false;
                employeeDataGridView.Enabled = false;
                prevPageButton.Enabled = false;
                nextPageButton.Enabled = false;

                System.Diagnostics.Debug.WriteLine($"EmployeeSelectionDialog: Starting to load employees...");

                // Create search request
                var searchRequest = new ApiAuthService.EmployeeSearchRequest();
                if (!string.IsNullOrWhiteSpace(searchTextBox.Text))
                {
                    searchRequest.name = searchTextBox.Text.Trim();
                }
                // Department filter
                var selectedDepartments = new List<string>();
                if (departmentCheckedListBox != null)
                {
                    for (int i = 0; i < departmentCheckedListBox.Items.Count; i++)
                    {
                        if (departmentCheckedListBox.GetItemChecked(i))
                        {
                            var departmentName = departmentCheckedListBox.Items[i].ToString();
                            if (departmentName != "All departments")
                            {
                                selectedDepartments.Add(departmentName);
                            }
                        }
                    }
                }
                
                if (selectedDepartments.Count > 0)
                {
                    searchRequest.departments = selectedDepartments.ToArray();
                    System.Diagnostics.Debug.WriteLine($"Department filter applied: {string.Join(", ", selectedDepartments)}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Department filter not applied - no departments selected");
                }
                // Explicitly pass pagination to API
                searchRequest.pageNo = pageNo.ToString();
                searchRequest.pageSize = "15";
                searchRequest.actionByUserType = string.IsNullOrWhiteSpace(searchRequest.actionByUserType) ? "SITE" : searchRequest.actionByUserType;

                // Debug the complete search request
                var serializer = new System.Web.Script.Serialization.JavaScriptSerializer { MaxJsonLength = int.MaxValue };
                var searchRequestJson = serializer.Serialize(searchRequest);
                System.Diagnostics.Debug.WriteLine($"Complete search request: {searchRequestJson}");

                var result = await _apiAuthService.GetEmployeesAsync(searchRequest, pageNo, 15);

                if (result.Success)
                {
                    var rawData = result.Data != null ? serializer.Serialize(result.Data) : "No data received";
                    var shortData = rawData.Length > 500 ? rawData.Substring(0, 500) + "..." : rawData;
                    // statusLabel.Text = $"API Response Status: Success. Raw Data (first 500 chars): {shortData}";
                    // statusLabel.ForeColor = Color.FromArgb(52, 152, 219);
                    await Task.Delay(150);
                }
                else
                {
                    // statusLabel.Text = $"API Response Status: Failed\nError: {result.ErrorMessage}";
                    // statusLabel.ForeColor = Color.FromArgb(231, 76, 60);
                    await Task.Delay(150);
                }

                if (result.Success && result.Data != null)
                {
                    System.Diagnostics.Debug.WriteLine($"EmployeeSelectionDialog: Successfully received {result.Data.content.Length} employees");

                    _currentPage = pageNo;

                    // Apply client-side department filter if both name and department are specified
                    var filteredEmployees = result.Data.content.AsEnumerable();
                    if (!string.IsNullOrWhiteSpace(searchTextBox.Text) && selectedDepartments.Count > 0)
                    {
                        var originalCount = filteredEmployees.Count();
                        filteredEmployees = filteredEmployees.Where(e => 
                            selectedDepartments.Any(d => string.Equals(e.department, d, StringComparison.OrdinalIgnoreCase)));
                        var filteredCount = filteredEmployees.Count();
                        System.Diagnostics.Debug.WriteLine($"Client-side department filter: {originalCount} -> {filteredCount} employees (filtering by '{string.Join(", ", selectedDepartments)}')");
                    }

                    employeeDataGridView.SuspendLayout();
                    try
                    {
                        employeeDataGridView.Rows.Clear();
                        foreach (var employee in filteredEmployees)
                        {
                            employeeDataGridView.Rows.Add(
                                employee.employee_site_id ?? "",
                                employee.first_name ?? "",
                                employee.last_name ?? "",
                                employee.department ?? "",
                                employee.user_id ?? "",
                                employee.scanlink_id ?? ""
                            );
                        }
                    }
                    finally
                    {
                        employeeDataGridView.ResumeLayout();
                        employeeDataGridView.Refresh();
                    }

                    // countLabel.Text = $"Total_elements: {result.Data.total_elements}, page_no: {result.Data.page_no}, page_size: {result.Data.page_size}, total_pages: {result.Data.total_pages}, last: {result.Data.last}, employees (showing {result.Data.content.Length})";
                    var finalCount = filteredEmployees.Count();
                    if (finalCount != result.Data.content.Length)
                    {
                        countLabel.Text = $"Showing {finalCount} of {result.Data.content.Length} employees (filtered)";
                    }
                    else
                    {
                        countLabel.Text = $"Total_elements: {result.Data.total_elements}";
                    }


                    // Update pagination buttons and label
                    prevPageButton.Enabled = _currentPage > 0;
                    nextPageButton.Enabled = !result.Data.last;
                    pageInfoLabel.Text = $"Page {(_currentPage + 1)} of {result.Data.total_pages}";

                    var loadedInfo = $"Loaded {result.Data.content.Length} employees successfully (Page {_currentPage})\n";
                    if (result.Data.content.Length > 0)
                    {
                        loadedInfo += $"First: {result.Data.content[0].first_name} {result.Data.content[0].last_name} (ID: {result.Data.content[0].employee_site_id})\n";
                        loadedInfo += $"Department: {result.Data.content[0].department}, user_id: {result.Data.content[0].user_id}";
                    }

                    // statusLabel.Text = loadedInfo;
                    // statusLabel.ForeColor = Color.FromArgb(46, 204, 113);
                }
                else
                {
                    // statusLabel.Text = $"Error: {result.ErrorMessage}";
                    // statusLabel.ForeColor = Color.FromArgb(231, 76, 60);
                    countLabel.Text = "Total: 0 employees";
                }
            }
            finally
            {
                searchButton.Enabled = true;
                employeeDataGridView.Enabled = true;
            }
        }

        private async Task LoadDepartmentsAsync()
        {
            try
            {
                // Provide immediate feedback
                // statusLabel.Text = "Loading departments...";
                // statusLabel.ForeColor = Color.FromArgb(52, 152, 219);

                var result = await _apiAuthService.GetDepartmentsAsync();
                System.Diagnostics.Debug.WriteLine($"GetDepartmentsAsync result: Success={result.Success}, StatusCode={result.StatusCode}");
                if (!result.Success)
                {
                    System.Diagnostics.Debug.WriteLine($"GetDepartmentsAsync error: {result.ErrorMessage}");
                }

                var items = new List<KeyValuePair<string, string>>();
                // Default "All" option
                items.Add(new KeyValuePair<string, string>(string.Empty, "All departments"));

                if (result.Success && result.Data != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Departments API returned {result.Data.Count} departments:");
                    
                    // Use HashSet to track unique department names and avoid duplicates
                    var uniqueDepartments = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    
                    foreach (var kvp in result.Data.OrderBy(k => k.Value, StringComparer.OrdinalIgnoreCase))
                    {
                        // Only add if we haven't seen this department name before
                        if (uniqueDepartments.Add(kvp.Value))
                        {
                            // Use department name as both key and value
                            items.Add(new KeyValuePair<string, string>(kvp.Value, kvp.Value));
                            System.Diagnostics.Debug.WriteLine($"  Department: ID='{kvp.Key}', Name='{kvp.Value}'");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"  Skipping duplicate department: '{kvp.Value}' (ID='{kvp.Key}')");
                        }
                    }
                    
                    departmentCheckedListBox.Items.Clear();
                    foreach (var item in items)
                    {
                        departmentCheckedListBox.Items.Add(item.Value);
                    }
                    // Select "All departments" by default
                    if (departmentCheckedListBox.Items.Count > 0)
                    {
                        departmentCheckedListBox.SetItemChecked(0, true);
                    }

                    System.Diagnostics.Debug.WriteLine($"CheckedListBox populated with {items.Count} unique items (removed {result.Data.Count - items.Count} duplicates)");
                    // statusLabel.Text = "Departments loaded";
                    // statusLabel.ForeColor = Color.FromArgb(46, 204, 113);
                }
                else
                {
                    departmentCheckedListBox.Items.Clear();
                    foreach (var item in items)
                    {
                        departmentCheckedListBox.Items.Add(item.Value);
                    }
                    // Select "All departments" by default
                    if (departmentCheckedListBox.Items.Count > 0)
                    {
                        departmentCheckedListBox.SetItemChecked(0, true);
                    }

                    // statusLabel.Text = "Departments unavailable (using default)";
                    // statusLabel.ForeColor = Color.FromArgb(231, 76, 60);
                }
            }
            catch (Exception)
            {
                // Ensure CheckedListBox still has a safe default
                departmentCheckedListBox.Items.Clear();
                departmentCheckedListBox.Items.Add("All departments");
                departmentCheckedListBox.SetItemChecked(0, true);

                //statusLabel.Text = $"Error loading departments: {ex.Message}";
                // statusLabel.ForeColor = Color.FromArgb(231, 76, 60);
            }
        }

        private async void PrevPageButton_Click(object sender, EventArgs e)
        {
            if (_currentPage > 0)
            {
                _currentPage--;
                await LoadEmployees(_currentPage);
            }
        }

        private async void NextPageButton_Click(object sender, EventArgs e)
        {
            _currentPage++;
            await LoadEmployees(_currentPage);
        }

        private void EmployeeDataGridView_SelectionChanged(object sender, EventArgs e)
        {
            selectButton.Enabled = employeeDataGridView.SelectedRows.Count > 0;
        }

        private void SelectButton_Click(object sender, EventArgs e)
        {
            if (employeeDataGridView.SelectedRows.Count > 0)
            {
                var selectedRow = employeeDataGridView.SelectedRows[0];
                
                // Find the employee in the original data (we'll need to store this)
                var employeeId = selectedRow.Cells["user_id"].Value?.ToString();
                
                if (!string.IsNullOrEmpty(employeeId))
                {
                    // Create a basic employee info object
                    _selectedEmployee = new ApiAuthService.EmployeeInfo
                    {
                        user_id = employeeId,
                        first_name = selectedRow.Cells["first_name"].Value?.ToString(),
                        last_name = selectedRow.Cells["last_name"].Value?.ToString(),
                        scanlink_id = selectedRow.Cells["scanlink_id"].Value?.ToString()
                    };

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
        }
    }
}
