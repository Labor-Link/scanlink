using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScanLink
{
    public partial class EmployeeSelectionDialog : Form
    {
        private ApiAuthService _apiAuthService;
        private DataGridView employeeDataGridView;
        
        private Label nameLabel;
        private TextBox searchTextBox;
        private Button searchButton;
        private CheckedListBox departmentCheckedListBox;
        private Label departmentLabel;
        private Button selectButton;
        private Button cancelButton;
        // private Label statusLabel;
        private Label countLabel;
        private ApiAuthService.EmployeeInfo _selectedEmployee;
        private Button prevPageButton;
        private Button nextPageButton;
        private Label pageInfoLabel;
        private int _currentPage = 0;

        public ApiAuthService.EmployeeInfo SelectedEmployee => _selectedEmployee;

        public EmployeeSelectionDialog(ApiAuthService apiAuthService)
        {
            _apiAuthService = apiAuthService;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Select Employee";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Create main panel
            var mainPanel = new Panel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.Padding = new Padding(10);
            mainPanel.AutoScroll = true;
            this.Controls.Add(mainPanel);

            // Create search panel
            var searchPanel = new Panel();
            searchPanel.Height = 100;
            searchPanel.Dock = DockStyle.Top;
            searchPanel.BackColor = Color.FromArgb(247, 249, 249);
            mainPanel.Controls.Add(searchPanel);

            // Name label
            nameLabel = new Label();
            nameLabel.Text = "Name:";
            nameLabel.Location = new Point(10, 17);
            nameLabel.AutoSize = true;
            nameLabel.ForeColor = Color.FromArgb(52, 73, 94);
            searchPanel.Controls.Add(nameLabel);

            // Search textbox
            searchTextBox = new TextBox();
            searchTextBox.Location = new Point(50, 15);
            searchTextBox.Size = new Size(250, 20);
            // searchTextBox.Text = "Search by name or employee ID...";
            searchTextBox.Text = "";
            searchTextBox.ForeColor = Color.Gray;
            searchTextBox.Enter += (s, e) => {
                if (searchTextBox.Text == "Search by name or employee ID...") {
                    searchTextBox.Text = "";
                    searchTextBox.ForeColor = Color.Black;
                }
            };
            searchTextBox.Leave += (s, e) => {
                if (string.IsNullOrWhiteSpace(searchTextBox.Text)) {
                    searchTextBox.Text = "";
                    searchTextBox.ForeColor = Color.Gray;
                }
            };
            searchPanel.Controls.Add(searchTextBox);

            // Department label
            departmentLabel = new Label();
            departmentLabel.Text = "Departments:";
            departmentLabel.Location = new Point(320, 17);
            departmentLabel.AutoSize = true;
            departmentLabel.ForeColor = Color.FromArgb(52, 73, 94);
            searchPanel.Controls.Add(departmentLabel);
            

            // Department checked list box
            departmentCheckedListBox = new CheckedListBox();
            departmentCheckedListBox.Location = new Point(395, 13);
            departmentCheckedListBox.Size = new Size(180, 80);
            departmentCheckedListBox.CheckOnClick = true;
            departmentCheckedListBox.IntegralHeight = false;
            departmentCheckedListBox.BorderStyle = BorderStyle.FixedSingle;
            departmentCheckedListBox.BackColor = Color.White;
            searchPanel.Controls.Add(departmentCheckedListBox);

            // Search button
            searchButton = new Button();
            searchButton.Text = "🔍 Search";
            searchButton.Location = new Point(585, 13);
            searchButton.Size = new Size(80, 25);
            searchButton.BackColor = Color.FromArgb(52, 152, 219);
            searchButton.FlatStyle = FlatStyle.Flat;
            searchButton.ForeColor = Color.White;
            searchButton.Click += SearchButton_Click;
            searchPanel.Controls.Add(searchButton);

            // Create data grid view
            employeeDataGridView = new DataGridView();
            employeeDataGridView.Location = new Point(10, 110); // Position below search panel
            employeeDataGridView.Size = new Size(760, 410);
            employeeDataGridView.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            employeeDataGridView.AllowUserToAddRows = false;
            employeeDataGridView.AllowUserToDeleteRows = false;
            employeeDataGridView.ReadOnly = true;
            employeeDataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            employeeDataGridView.MultiSelect = false;
            employeeDataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            employeeDataGridView.BackgroundColor = Color.White;
            employeeDataGridView.BorderStyle = BorderStyle.FixedSingle;
            employeeDataGridView.GridColor = Color.FromArgb(220, 220, 220);
            employeeDataGridView.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);
            employeeDataGridView.DefaultCellStyle.SelectionBackColor = Color.FromArgb(52, 152, 219);
            employeeDataGridView.DefaultCellStyle.SelectionForeColor = Color.White;
            employeeDataGridView.RowHeadersVisible = false;
            employeeDataGridView.ColumnHeadersVisible = true;
            employeeDataGridView.EnableHeadersVisualStyles = false;
            employeeDataGridView.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(52, 73, 94);
            employeeDataGridView.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            employeeDataGridView.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            employeeDataGridView.ColumnHeadersHeight = 30;
            employeeDataGridView.RowTemplate.Height = 25;
            mainPanel.Controls.Add(employeeDataGridView);


            // Create button panel
            var buttonPanel = new Panel();
            buttonPanel.Height = 50;
            buttonPanel.Dock = DockStyle.Bottom;
            buttonPanel.BackColor = Color.FromArgb(247, 249, 249);
            mainPanel.Controls.Add(buttonPanel);

            // Status label
            // statusLabel = new Label();
            // statusLabel.Text = "Loading employees...";
            // statusLabel.Location = new Point(10, 18);
            // statusLabel.AutoSize = true;
            // statusLabel.ForeColor = Color.FromArgb(52, 152, 219);
            // buttonPanel.Controls.Add(statusLabel);

            
            // Previous page button
            prevPageButton = new Button();
            prevPageButton.Text = "◀ Prev";
            prevPageButton.Location = new Point(10, 25);
            prevPageButton.Size = new Size(80, 24);
            prevPageButton.BackColor = Color.FromArgb(52, 152, 219);
            prevPageButton.FlatStyle = FlatStyle.Flat;
            prevPageButton.ForeColor = Color.White;
            prevPageButton.Enabled = false;
            prevPageButton.Click += PrevPageButton_Click;
            buttonPanel.Controls.Add(prevPageButton);
            

            // Page info label
            pageInfoLabel = new Label();
            pageInfoLabel.Text = "Page 1 of 1";
            pageInfoLabel.Location = new Point(93, 29);
            pageInfoLabel.AutoSize = true;
            pageInfoLabel.ForeColor = Color.FromArgb(52, 73, 94);
            buttonPanel.Controls.Add(pageInfoLabel);

            // Next page button
            nextPageButton = new Button();
            nextPageButton.Text = "Next ▶";
            nextPageButton.Location = new Point(170, 25);
            nextPageButton.Size = new Size(80, 24);
            nextPageButton.BackColor = Color.FromArgb(52, 152, 219);
            nextPageButton.FlatStyle = FlatStyle.Flat;
            nextPageButton.ForeColor = Color.White;
            nextPageButton.Enabled = true;
            nextPageButton.Click += NextPageButton_Click;
            buttonPanel.Controls.Add(nextPageButton);

            // Count label
            countLabel = new Label();
            countLabel.Text = "Total: 0 employees";
            countLabel.Location = new Point(270, 30);
            countLabel.AutoSize = true;
            countLabel.ForeColor = Color.FromArgb(127, 140, 141);
            buttonPanel.Controls.Add(countLabel);

            // Cancel button
            cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.Location = new Point(680, 25);
            cancelButton.Size = new Size(75, 25);
            cancelButton.DialogResult = DialogResult.Cancel;
            buttonPanel.Controls.Add(cancelButton);

            // Select button
            selectButton = new Button();
            selectButton.Text = "Select";
            selectButton.Location = new Point(600, 25);
            selectButton.Size = new Size(75, 25);
            selectButton.BackColor = Color.FromArgb(46, 204, 113);
            selectButton.FlatStyle = FlatStyle.Flat;
            selectButton.ForeColor = Color.White;
            selectButton.Enabled = false;
            selectButton.Click += SelectButton_Click;
            buttonPanel.Controls.Add(selectButton);

            // Initialize data grid columns
            InitializeDataGridView();

            // Load employees on form load
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

            // Set column widths
            employeeDataGridView.Columns["employee_site_id"].Width = 50;
            employeeDataGridView.Columns["first_name"].Width = 70;
            employeeDataGridView.Columns["last_name"].Width = 70;
            employeeDataGridView.Columns["department"].Width = 70;
            employeeDataGridView.Columns["user_id"].Width = 250;

            // Style the grid
            employeeDataGridView.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);
            employeeDataGridView.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(50, 74, 95);
            employeeDataGridView.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.White;

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
                var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
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
                                employee.user_id ?? ""
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
            catch (Exception ex)
            {
                // statusLabel.Text = $"Error: {ex.Message}";
                // statusLabel.ForeColor = Color.FromArgb(231, 76, 60);
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
            catch (Exception ex)
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
                        last_name = selectedRow.Cells["last_name"].Value?.ToString()
                    };

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
        }
    }
}
