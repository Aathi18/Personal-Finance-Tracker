using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;

namespace Personal_Finance_Tracker
{
    public partial class Form1 : Form
    {
        private readonly string dataFilePath = "transactions.csv";
        private int _rowIndexToEdit = -1;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Hide the input panel on startup
            splitContainer1.Panel1Collapsed = true;

            // Setup UI
            SetupUIDefaults();
            SetupDataGridViewStyling();

            // Load Data
            LoadTransactionsFromFile();
        }

        private void SetupUIDefaults()
        {
            // Populate ComboBoxes
            cmbType.Items.Add("Income");
            cmbType.Items.Add("Expense");
            cmbCategory.Items.Add("Salary");
            cmbCategory.Items.Add("Groceries");
            cmbCategory.Items.Add("Rent");
            cmbCategory.Items.Add("Entertainment");
            cmbCategory.Items.Add("Utilities");
            cmbCategory.Items.Add("Transport");

            // Set default selections
            ResetInputForm();
        }

        private void SetupDataGridViewStyling()
        {
            dgvTransactions.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(50, 50, 53);
            dgvTransactions.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 122, 204);
            dgvTransactions.DefaultCellStyle.SelectionForeColor = Color.White;
        }

        private void ShowInputPanel(bool isEditMode)
        {
            splitContainer1.Panel1Collapsed = false;
            groupBox1.Text = isEditMode ? "Edit Transaction" : "Add New Transaction";
            btnSave.Text = isEditMode ? "Update" : "Save";
        }

        private void HideInputPanel()
        {
            splitContainer1.Panel1Collapsed = true;
            ResetInputForm();
        }

        private void ResetInputForm()
        {
            // Clear input fields and reset state
            dtpDate.Value = DateTime.Now;
            txtDescription.Clear();
            cmbCategory.SelectedIndex = 1;
            cmbType.SelectedIndex = 1;
            txtNotes.Clear();
            txtAmount.Clear();
            _rowIndexToEdit = -1;
            txtDescription.Focus();
        }

        // --- Event Handlers ---

        private void btnShowAddPanel_Click(object sender, EventArgs e)
        {
            ResetInputForm();
            ShowInputPanel(isEditMode: false);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDescription.Text))
            {
                MessageBox.Show("Description cannot be empty.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!decimal.TryParse(txtAmount.Text, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Please enter a valid, positive amount.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // If in edit mode, update the existing row
            if (_rowIndexToEdit > -1)
            {
                DataGridViewRow row = dgvTransactions.Rows[_rowIndexToEdit];
                row.Cells["Date"].Value = dtpDate.Value.ToShortDateString();
                row.Cells["Description"].Value = txtDescription.Text;
                row.Cells["Category"].Value = cmbCategory.SelectedItem.ToString();
                row.Cells["Type"].Value = cmbType.SelectedItem.ToString();
                row.Cells["Amount"].Value = amount.ToString("F2");
                row.Cells["Notes"].Value = txtNotes.Text;
            }
            else // Add a new row
            {
                dgvTransactions.Rows.Add(
                    dtpDate.Value.ToShortDateString(),
                    txtDescription.Text,
                    cmbCategory.SelectedItem.ToString(),
                    cmbType.SelectedItem.ToString(),
                    amount.ToString("F2"),
                    txtNotes.Text
                );
            }
            HideInputPanel();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            HideInputPanel();
        }

        private void dgvTransactions_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // Handle Delete
            if (dgvTransactions.Columns[e.ColumnIndex].Name == "colDelete")
            {
                if (MessageBox.Show("Are you sure you want to delete this transaction?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    dgvTransactions.Rows.RemoveAt(e.RowIndex);
                }
            }
            // Handle Edit
            else if (dgvTransactions.Columns[e.ColumnIndex].Name == "colEdit")
            {
                _rowIndexToEdit = e.RowIndex;
                DataGridViewRow row = dgvTransactions.Rows[_rowIndexToEdit];

                // Populate the input panel with row data
                dtpDate.Value = DateTime.Parse(row.Cells["Date"].Value.ToString());
                txtDescription.Text = row.Cells["Description"].Value.ToString();
                cmbCategory.Text = row.Cells["Category"].Value.ToString();
                cmbType.Text = row.Cells["Type"].Value.ToString();
                txtAmount.Text = row.Cells["Amount"].Value.ToString();
                txtNotes.Text = row.Cells["Notes"].Value?.ToString();

                ShowInputPanel(isEditMode: true);
            }
        }

        private void btnFilter_Click(object sender, EventArgs e)
        {
            DateTime startDate = dtpFilterStart.Value.Date;
            DateTime endDate = dtpFilterEnd.Value.Date.AddDays(1).AddTicks(-1);
            foreach (DataGridViewRow row in dgvTransactions.Rows)
            {
                if (row.IsNewRow) continue;
                DateTime rowDate = DateTime.Parse(row.Cells["Date"].Value.ToString());
                row.Visible = (rowDate >= startDate && rowDate <= endDate);
            }
        }

        private void btnClearFilter_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgvTransactions.Rows)
            {
                row.Visible = true;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveTransactionsToFile();
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab == tabDashboard)
            {
                UpdateDashboard();
            }
        }

        // --- Data and Dashboard Logic (Mostly Unchanged) ---

        private void LoadTransactionsFromFile()
        {
            if (!File.Exists(dataFilePath)) return;
            string[] lines = File.ReadAllLines(dataFilePath);
            foreach (string line in lines)
            {
                string[] parts = line.Split(',');
                if (parts.Length == 6) dgvTransactions.Rows.Add(parts);
            }
        }

        private void SaveTransactionsToFile()
        {
            var lines = new List<string>();
            foreach (DataGridViewRow row in dgvTransactions.Rows)
            {
                if (!row.IsNewRow)
                {
                    string note = row.Cells["Notes"].Value?.ToString().Replace(',', ';') ?? "";
                    lines.Add($"{row.Cells[0].Value},{row.Cells[1].Value},{row.Cells[2].Value},{row.Cells[3].Value},{row.Cells[4].Value},{note}");
                }
            }
            File.WriteAllLines(dataFilePath, lines);
        }

        private void UpdateDashboard()
        {
            decimal totalIncome = 0;
            decimal totalExpenses = 0;
            var categoryTotals = new Dictionary<string, decimal>();

            foreach (DataGridViewRow row in dgvTransactions.Rows)
            {
                if (row.IsNewRow || !row.Visible) continue;
                decimal amount = decimal.Parse(row.Cells["Amount"].Value.ToString());
                string type = row.Cells["Type"].Value.ToString();
                string category = row.Cells["Category"].Value.ToString();

                if (type == "Income") totalIncome += amount;
                else
                {
                    totalExpenses += amount;
                    if (categoryTotals.ContainsKey(category)) categoryTotals[category] += amount;
                    else categoryTotals[category] = amount;
                }
            }
            decimal netBalance = totalIncome - totalExpenses;

            lblTotalIncome.Text = $"Total Income: {totalIncome:C}";
            lblTotalExpenses.Text = $"Total Expenses: {totalExpenses:C}";
            lblNetBalance.Text = $"Net Balance: {netBalance:C}";
            lblTotalIncome.ForeColor = Color.LightGreen;
            lblTotalExpenses.ForeColor = Color.LightCoral;
            lblNetBalance.ForeColor = netBalance >= 0 ? Color.LightGreen : Color.LightCoral;

            chartExpenses.Series["Series1"].Points.Clear();
            chartExpenses.Titles.Clear();
            var title = chartExpenses.Titles.Add("Expenses by Category");
            title.ForeColor = Color.White;
            foreach (var entry in categoryTotals)
            {
                chartExpenses.Series["Series1"].Points.AddXY(entry.Key, entry.Value);
            }

            var series = chartIncomeVsExpense.Series["Series1"];
            series.Points.Clear();
            chartIncomeVsExpense.Titles.Clear();
            title = chartIncomeVsExpense.Titles.Add("Income vs. Expenses");
            title.ForeColor = Color.White;
            series.Points.AddXY("Income", totalIncome);
            series.Points.AddXY("Expenses", totalExpenses);
            series.Points[0].Color = Color.MediumSeaGreen;
            series.Points[1].Color = Color.IndianRed;

            lvRecentTransactions.Items.Clear();
            var recentRows = dgvTransactions.Rows.Cast<DataGridViewRow>()
                               .Where(r => !r.IsNewRow && r.Visible)
                               .Reverse()
                               .Take(5);

            foreach (var row in recentRows)
            {
                var listViewItem = new ListViewItem(row.Cells["Description"].Value.ToString());
                listViewItem.SubItems.Add(row.Cells["Type"].Value.ToString());
                listViewItem.SubItems.Add(decimal.Parse(row.Cells["Amount"].Value.ToString()).ToString("C"));
                listViewItem.ForeColor = (row.Cells["Type"].Value.ToString() == "Income") ? Color.LightGreen : Color.LightCoral;
                lvRecentTransactions.Items.Add(listViewItem);
            }
            lvRecentTransactions.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }
    }
}