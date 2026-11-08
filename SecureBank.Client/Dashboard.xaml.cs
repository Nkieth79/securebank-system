using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Windows;

namespace SecureBank.Client;

public partial class Dashboard : Window
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly string _userFullName;
    private readonly string _userRole;

    public Dashboard(TcpClient client, string fullName, string role)
    {
        InitializeComponent();
        _client = client;
        _stream = client.GetStream();
        _userFullName = fullName;
        _userRole = role;

        lblUser.Text = fullName + " (" + role + ")";

        ConfigureTabsForRole(role);
        UpdateButtonStates();
    }

    private void ConfigureTabsForRole(string role)
    {
        if (role != "Manager" && role != "Admin")
        {
            tabManager.Visibility = Visibility.Collapsed;
        }

        if (role != "Admin")
        {
            tabAdmin.Visibility = Visibility.Collapsed;
        }
    }

    private void Field_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        bool hasAccount = !string.IsNullOrWhiteSpace(txtLookupAccount.Text);
        bool hasAmount = !string.IsNullOrWhiteSpace(txtAmount.Text);
        bool hasDestination = !string.IsNullOrWhiteSpace(txtDestination.Text);

        btnLookup.IsEnabled = hasAccount;
        btnDeposit.IsEnabled = hasAccount && hasAmount;
        btnWithdraw.IsEnabled = hasAccount && hasAmount;
        btnTransfer.IsEnabled = hasAccount && hasAmount && hasDestination;
    }

    // ==========================================
    // CUSTOMER CREATION & ACCOUNTS
    // ==========================================

    private void btnCreateCustomer_Click(object sender, RoutedEventArgs e)
    {
        lblCustomerMessage.Text = "";

        if (string.IsNullOrWhiteSpace(txtCustFirst.Text) || string.IsNullOrWhiteSpace(txtCustLast.Text) || string.IsNullOrWhiteSpace(txtCustId.Text))
        {
            lblCustomerMessage.Foreground = System.Windows.Media.Brushes.Red;
            lblCustomerMessage.Text = "First Name, Last Name, and ID Number are required.";
            return;
        }

        try
        {
            string first = txtCustFirst.Text.Trim().Replace("|", " ");
            string last = txtCustLast.Text.Trim().Replace("|", " ");
            string id = txtCustId.Text.Trim().Replace("|", " ");
            string email = string.IsNullOrWhiteSpace(txtCustEmail.Text) ? "None" : txtCustEmail.Text.Trim().Replace("|", " ");
            string phone = string.IsNullOrWhiteSpace(txtCustPhone.Text) ? "None" : txtCustPhone.Text.Trim().Replace("|", " ");
            string address = string.IsNullOrWhiteSpace(txtCustAddress.Text) ? "None" : txtCustAddress.Text.Trim().Replace("|", " ");

            string response = SendAndReceive($"CREATECUSTOMER|{first}|{last}|{id}|{email}|{phone}|{address}");

            if (response.StartsWith("OK"))
            {
                lblCustomerMessage.Foreground = System.Windows.Media.Brushes.LightGreen;
                lblCustomerMessage.Text = response.Substring(3);
                txtCustFirst.Clear();
                txtCustLast.Clear();
                txtCustId.Clear();
                txtCustEmail.Clear();
                txtCustPhone.Clear();
                txtCustAddress.Clear();
            }
            else
            {
                lblCustomerMessage.Foreground = System.Windows.Media.Brushes.Red;
                lblCustomerMessage.Text = response;
            }
        }
        catch (Exception ex)
        {
            lblCustomerMessage.Foreground = System.Windows.Media.Brushes.Red;
            lblCustomerMessage.Text = "Error: " + ex.Message;
        }
    }

    private void btnOpenAccount_Click(object sender, RoutedEventArgs e)
    {
        lblOpenAccMessage.Text = "";

        if (!int.TryParse(txtOpenAccCustId.Text.Trim(), out int custId) || cboOpenAccType.SelectedItem == null)
        {
            lblOpenAccMessage.Foreground = System.Windows.Media.Brushes.Red;
            lblOpenAccMessage.Text = "Please enter a valid Customer ID and select an Account Type.";
            return;
        }

        try
        {
            string type = (cboOpenAccType.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString() ?? "Savings";
            decimal deposit = 0;
            if (!string.IsNullOrWhiteSpace(txtOpenAccDeposit.Text))
            {
                if (!decimal.TryParse(txtOpenAccDeposit.Text.Trim(), out deposit))
                {
                    lblOpenAccMessage.Foreground = System.Windows.Media.Brushes.Red;
                    lblOpenAccMessage.Text = "Invalid deposit amount.";
                    return;
                }
            }

            string response = SendAndReceive($"OPENACCOUNT|{custId}|{type}|{deposit}");

            if (response.StartsWith("OK"))
            {
                lblOpenAccMessage.Foreground = System.Windows.Media.Brushes.LightGreen;
                lblOpenAccMessage.Text = response.Substring(3);
                txtOpenAccCustId.Clear();
                txtOpenAccDeposit.Clear();
            }
            else
            {
                lblOpenAccMessage.Foreground = System.Windows.Media.Brushes.Red;
                lblOpenAccMessage.Text = response;
            }
        }
        catch (Exception ex)
        {
            lblOpenAccMessage.Foreground = System.Windows.Media.Brushes.Red;
            lblOpenAccMessage.Text = "Error: " + ex.Message;
        }
    }

    // ==========================================
    // TELLER TAB LOGIC
    // ==========================================

    private void btnLookup_Click(object sender, RoutedEventArgs e)
    {
        lblLookupMessage.Text = "";
        lblTransactionResult.Text = "";

        string accountNumber = txtLookupAccount.Text.Trim();
        if (string.IsNullOrEmpty(accountNumber))
        {
            lblLookupMessage.Text = "Enter an account number.";
            return;
        }

        try
        {
            string response = SendAndReceive("LOOKUP|" + accountNumber);

            if (response.StartsWith("OK"))
            {
                string[] parts = response.Split('|');
                lblCustomerName.Text = parts[1] + " " + parts[2];
                lblAccountType.Text = parts[3];
                lblBalance.Text = "R" + parts[4];
                lblLookupMessage.Foreground = System.Windows.Media.Brushes.LightGreen;
                lblLookupMessage.Text = "Found";
                LoadTransactions(accountNumber);
            }
            else
            {
                lblLookupMessage.Foreground = System.Windows.Media.Brushes.Red;
                lblLookupMessage.Text = response;
                lblCustomerName.Text = "";
                lblAccountType.Text = "";
                lblBalance.Text = "R0.00";
                dgTransactions.ItemsSource = null;
            }
        }
        catch (Exception ex)
        {
            lblLookupMessage.Foreground = System.Windows.Media.Brushes.Red;
            lblLookupMessage.Text = "Error: " + ex.Message;
        }
    }

    private void LoadTransactions(string accountNumber)
    {
        try
        {
            string response = SendAndReceive("GETTRANSACTIONS|" + accountNumber);

            if (!response.StartsWith("OK"))
            {
                lblTransactionMessage.Foreground = System.Windows.Media.Brushes.Red;
                lblTransactionMessage.Text = response;
                dgTransactions.ItemsSource = null;
                return;
            }

            string data = response.Substring(3);

            if (data == "EMPTY")
            {
                dgTransactions.ItemsSource = null;
                lblTransactionMessage.Foreground = System.Windows.Media.Brushes.Gray;
                lblTransactionMessage.Text = "No transactions yet.";
                return;
            }

            string[] rows = data.Split(new string[] { "~~" }, StringSplitOptions.None);

            var list = new List<TransactionRow>();
            foreach (string row in rows)
            {
                string[] fields = row.Split('~');
                if (fields.Length >= 5)
                {
                    list.Add(new TransactionRow
                    {
                        Type = fields[0],
                        Amount = "R" + fields[1],
                        BalanceAfter = "R" + fields[2],
                        Date = fields[3],
                        Description = fields[4]
                    });
                }
            }

            dgTransactions.ItemsSource = list;
            lblTransactionMessage.Foreground = System.Windows.Media.Brushes.LightGreen;
            lblTransactionMessage.Text = list.Count + " transactions loaded.";
        }
        catch (Exception ex)
        {
            lblTransactionMessage.Foreground = System.Windows.Media.Brushes.Red;
            lblTransactionMessage.Text = "Error: " + ex.Message;
        }
    }

    private void btnDeposit_Click(object sender, RoutedEventArgs e)
    {
        lblTransactionResult.Text = "";
        if (!decimal.TryParse(txtAmount.Text.Trim(), out decimal amount) || amount <= 0)
        {
            lblTransactionResult.Foreground = System.Windows.Media.Brushes.Red;
            lblTransactionResult.Text = "Please enter a valid positive amount.";
            return;
        }

        try
        {
            string response = SendAndReceive("DEPOSIT|" + txtLookupAccount.Text.Trim() + "|" + amount.ToString("F2"));
            HandleTransactionResponse(response);
        }
        catch (Exception ex) { lblTransactionResult.Text = "Error: " + ex.Message; }
    }

    private void btnWithdraw_Click(object sender, RoutedEventArgs e)
    {
        lblTransactionResult.Text = "";
        if (!decimal.TryParse(txtAmount.Text.Trim(), out decimal amount) || amount <= 0)
        {
            lblTransactionResult.Foreground = System.Windows.Media.Brushes.Red;
            lblTransactionResult.Text = "Please enter a valid positive amount.";
            return;
        }

        try
        {
            string response = SendAndReceive("WITHDRAW|" + txtLookupAccount.Text.Trim() + "|" + amount.ToString("F2"));
            HandleTransactionResponse(response);
        }
        catch (Exception ex) { lblTransactionResult.Text = "Error: " + ex.Message; }
    }

    private void btnTransfer_Click(object sender, RoutedEventArgs e)
    {
        lblTransactionResult.Text = "";
        if (!decimal.TryParse(txtAmount.Text.Trim(), out decimal amount) || amount <= 0)
        {
            lblTransactionResult.Foreground = System.Windows.Media.Brushes.Red;
            lblTransactionResult.Text = "Please enter a valid positive amount.";
            return;
        }

        try
        {
            string response = SendAndReceive("TRANSFER|" + txtLookupAccount.Text.Trim() + "|" + txtDestination.Text.Trim() + "|" + amount.ToString("F2"));
            HandleTransactionResponse(response);
        }
        catch (Exception ex) { lblTransactionResult.Text = "Error: " + ex.Message; }
    }

    private void HandleTransactionResponse(string response)
    {
        if (response.StartsWith("OK"))
        {
            string[] parts = response.Split('|');
            string newBalance = parts.Length > 1 ? parts[1] : "";
            lblTransactionResult.Foreground = System.Windows.Media.Brushes.LightGreen;
            lblTransactionResult.Text = "Success. New balance: R" + newBalance;
            txtAmount.Clear();
            txtDestination.Clear();

            string account = txtLookupAccount.Text.Trim();
            if (!string.IsNullOrEmpty(account))
            {
                string lookupResponse = SendAndReceive("LOOKUP|" + account);
                if (lookupResponse.StartsWith("OK"))
                {
                    string[] lookupParts = lookupResponse.Split('|');
                    lblCustomerName.Text = lookupParts[1] + " " + lookupParts[2];
                    lblAccountType.Text = lookupParts[3];
                    lblBalance.Text = "R" + lookupParts[4];
                }
                LoadTransactions(account);
            }
        }
        else
        {
            lblTransactionResult.Foreground = System.Windows.Media.Brushes.Red;
            lblTransactionResult.Text = response;
        }
    }

    private void btnApplyLoan_Click(object sender, RoutedEventArgs e)
    {
        lblLoanMessage.Text = "";

        if (!int.TryParse(txtLoanCustId.Text.Trim(), out int custId) ||
            !decimal.TryParse(txtLoanAmount.Text.Trim(), out decimal amount) ||
            !decimal.TryParse(txtLoanRate.Text.Trim(), out decimal rate) ||
            !int.TryParse(txtLoanTerm.Text.Trim(), out int term))
        {
            lblLoanMessage.Foreground = System.Windows.Media.Brushes.Red;
            lblLoanMessage.Text = "Please enter valid numbers for all fields.";
            return;
        }

        try
        {
            string cmd = $"CREATELOAN|{custId}|{amount}|{rate}|{term}";
            string response = SendAndReceive(cmd);

            if (response.StartsWith("OK"))
            {
                lblLoanMessage.Foreground = System.Windows.Media.Brushes.LightGreen;
                lblLoanMessage.Text = response.Substring(3);
                txtLoanCustId.Clear();
                txtLoanAmount.Clear();
                txtLoanRate.Clear();
                txtLoanTerm.Clear();
            }
            else
            {
                lblLoanMessage.Foreground = System.Windows.Media.Brushes.Red;
                lblLoanMessage.Text = response;
            }
        }
        catch (Exception ex)
        {
            lblLoanMessage.Foreground = System.Windows.Media.Brushes.Red;
            lblLoanMessage.Text = "Error: " + ex.Message;
        }
    }

    // ==========================================
    // MANAGER TAB LOGIC
    // ==========================================

    private void btnRefreshLoans_Click(object sender, RoutedEventArgs e)
    {
        lblManagerMessage.Text = "";
        try
        {
            string response = SendAndReceive("GETLOANS");

            if (!response.StartsWith("OK"))
            {
                lblManagerMessage.Foreground = System.Windows.Media.Brushes.Red;
                lblManagerMessage.Text = response;
                dgPendingLoans.ItemsSource = null;
                return;
            }

            string data = response.Substring(3);
            if (data == "EMPTY")
            {
                dgPendingLoans.ItemsSource = null;
                lblManagerMessage.Foreground = System.Windows.Media.Brushes.Gray;
                lblManagerMessage.Text = "No pending loans.";
                LoadActiveLoans();
                return;
            }

            string[] rows = data.Split(new string[] { "~~" }, StringSplitOptions.None);
            var list = new List<PendingLoanRow>();

            foreach (string row in rows)
            {
                string[] fields = row.Split('~');
                if (fields.Length >= 5)
                {
                    list.Add(new PendingLoanRow
                    {
                        LoanId = fields[0],
                        CustomerName = fields[1],
                        Amount = "R" + fields[2],
                        Rate = fields[3],
                        Term = fields[4] + " months"
                    });
                }
            }

            dgPendingLoans.ItemsSource = list;
            lblManagerMessage.Foreground = System.Windows.Media.Brushes.LightGreen;
            lblManagerMessage.Text = list.Count + " pending loans loaded.";

            LoadActiveLoans();
        }
        catch (Exception ex)
        {
            lblManagerMessage.Foreground = System.Windows.Media.Brushes.Red;
            lblManagerMessage.Text = "Error: " + ex.Message;
        }
    }

    private void btnApproveLoan_Click(object sender, RoutedEventArgs e)
    {
        if (dgPendingLoans.SelectedItem is not PendingLoanRow selectedLoan)
        {
            lblManagerMessage.Foreground = System.Windows.Media.Brushes.Red;
            lblManagerMessage.Text = "Please select a loan to approve.";
            return;
        }

        try
        {
            string response = SendAndReceive("APPROVELOAN|" + selectedLoan.LoanId);

            if (response.StartsWith("OK"))
            {
                lblManagerMessage.Foreground = System.Windows.Media.Brushes.LightGreen;
                lblManagerMessage.Text = response.Substring(3);
                btnRefreshLoans_Click(sender, e);
                LoadActiveLoans();
            }
            else
            {
                lblManagerMessage.Foreground = System.Windows.Media.Brushes.Red;
                lblManagerMessage.Text = response;
            }
        }
        catch (Exception ex)
        {
            lblManagerMessage.Foreground = System.Windows.Media.Brushes.Red;
            lblManagerMessage.Text = "Error: " + ex.Message;
        }
    }

    private void btnRejectLoan_Click(object sender, RoutedEventArgs e)
    {
        if (dgPendingLoans.SelectedItem is not PendingLoanRow selectedLoan)
        {
            lblManagerMessage.Foreground = System.Windows.Media.Brushes.Red;
            lblManagerMessage.Text = "Please select a loan to reject.";
            return;
        }

        try
        {
            string response = SendAndReceive("REJECTLOAN|" + selectedLoan.LoanId);

            if (response.StartsWith("OK"))
            {
                lblManagerMessage.Foreground = System.Windows.Media.Brushes.LightGreen;
                lblManagerMessage.Text = response.Substring(3);
                btnRefreshLoans_Click(sender, e);
            }
            else
            {
                lblManagerMessage.Foreground = System.Windows.Media.Brushes.Red;
                lblManagerMessage.Text = response;
            }
        }
        catch (Exception ex)
        {
            lblManagerMessage.Foreground = System.Windows.Media.Brushes.Red;
            lblManagerMessage.Text = "Error: " + ex.Message;
        }
    }

    private void LoadActiveLoans()
    {
        try
        {
            string response = SendAndReceive("GETACTIVELOANS");

            if (!response.StartsWith("OK"))
            {
                lblManagerMessage.Foreground = System.Windows.Media.Brushes.Red;
                lblManagerMessage.Text = "Active Loans Error: " + response;
                return;
            }

            string data = response.Substring(3);
            if (data == "EMPTY")
            {
                dgActiveLoans.ItemsSource = null;
                return;
            }

            string[] rows = data.Split(new string[] { "~~" }, StringSplitOptions.None);
            var list = new List<ActiveLoanRow>();

            foreach (string row in rows)
            {
                string[] fields = row.Split('~');
                if (fields.Length >= 4)
                {
                    list.Add(new ActiveLoanRow
                    {
                        LoanId = fields[0],
                        CustomerName = fields[1],
                        Amount = "R" + fields[2],
                        RemainingBalance = "R" + fields[3]
                    });
                }
            }

            dgActiveLoans.ItemsSource = list;
        }
        catch (Exception ex)
        {
            lblManagerMessage.Foreground = System.Windows.Media.Brushes.Red;
            lblManagerMessage.Text = "Active Loans Error: " + ex.Message;
        }
    }

    private void btnCloseAccount_Click(object sender, RoutedEventArgs e)
    {
        lblCloseAccountMessage.Text = "";
        if (string.IsNullOrWhiteSpace(txtCloseAccountNumber.Text)) return;

        try
        {
            string response = SendAndReceive("CLOSEACCOUNT|" + txtCloseAccountNumber.Text.Trim());

            if (response.StartsWith("OK"))
            {
                lblCloseAccountMessage.Foreground = System.Windows.Media.Brushes.LightGreen;
                lblCloseAccountMessage.Text = response.Substring(3);
                txtCloseAccountNumber.Clear();
            }
            else
            {
                lblCloseAccountMessage.Foreground = System.Windows.Media.Brushes.Red;
                lblCloseAccountMessage.Text = response.Substring(6);
            }
        }
        catch (Exception ex)
        {
            lblCloseAccountMessage.Foreground = System.Windows.Media.Brushes.Red;
            lblCloseAccountMessage.Text = "Error: " + ex.Message;
        }
    }

    // ==========================================
    // ADMIN TAB LOGIC
    // ==========================================

    private void btnRefreshAuditLog_Click(object sender, RoutedEventArgs e)
    {
        lblAdminMessage.Text = "";
        try
        {
            string response = SendAndReceive("GETAUDITLOG");

            if (!response.StartsWith("OK"))
            {
                lblAdminMessage.Foreground = System.Windows.Media.Brushes.Red;
                lblAdminMessage.Text = response;
                dgAuditLog.ItemsSource = null;
                return;
            }

            string data = response.Substring(3);
            if (data == "EMPTY")
            {
                dgAuditLog.ItemsSource = null;
                lblAdminMessage.Foreground = System.Windows.Media.Brushes.Gray;
                lblAdminMessage.Text = "No audit logs found.";
                return;
            }

            string[] rows = data.Split(new string[] { "~~" }, StringSplitOptions.None);
            var list = new List<AuditLogRow>();

            foreach (string row in rows)
            {
                string[] fields = row.Split('~');
                if (fields.Length >= 4)
                {
                    list.Add(new AuditLogRow
                    {
                        User = fields[0],
                        Action = fields[1],
                        Details = fields[2],
                        Timestamp = fields[3]
                    });
                }
            }

            dgAuditLog.ItemsSource = list;
            lblAdminMessage.Foreground = System.Windows.Media.Brushes.LightGreen;
            lblAdminMessage.Text = list.Count + " log entries loaded.";
        }
        catch (Exception ex)
        {
            lblAdminMessage.Foreground = System.Windows.Media.Brushes.Red;
            lblAdminMessage.Text = "Error: " + ex.Message;
        }
    }

    private void btnToggleUser_Click(object sender, RoutedEventArgs e)
    {
        lblManageUserMessage.Text = "";
        if (string.IsNullOrWhiteSpace(txtManageUsername.Text)) return;

        try
        {
            string response = SendAndReceive("TOGGLEUSER|" + txtManageUsername.Text.Trim());

            if (response.StartsWith("OK"))
            {
                lblManageUserMessage.Foreground = System.Windows.Media.Brushes.LightGreen;
                lblManageUserMessage.Text = response.Substring(3);
                txtManageUsername.Clear();
                btnRefreshAuditLog_Click(sender, e);
            }
            else
            {
                lblManageUserMessage.Foreground = System.Windows.Media.Brushes.Red;
                lblManageUserMessage.Text = response.Substring(6);
            }
        }
        catch (Exception ex)
        {
            lblManageUserMessage.Foreground = System.Windows.Media.Brushes.Red;
            lblManageUserMessage.Text = "Error: " + ex.Message;
        }
    }

    private void btnExportCsv_Click(object sender, RoutedEventArgs e)
    {
        if (dgAuditLog.ItemsSource == null || dgAuditLog.Items.Count == 0)
        {
            lblAdminMessage.Foreground = System.Windows.Media.Brushes.Red;
            lblAdminMessage.Text = "No data to export. Refresh the log first.";
            return;
        }

        try
        {
            Microsoft.Win32.SaveFileDialog saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV File (*.csv)|*.csv",
                FileName = "AuditLog_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv"
            };

            if (saveDialog.ShowDialog() == true)
            {
                using (StreamWriter writer = new StreamWriter(saveDialog.FileName))
                {
                    writer.WriteLine("User,Action,Details,Timestamp");

                    var items = dgAuditLog.ItemsSource as List<AuditLogRow>;
                    if (items != null)
                    {
                        foreach (var row in items)
                        {
                            writer.WriteLine($"{row.User},\"{row.Action}\",\"{row.Details}\",{row.Timestamp}");
                        }
                    }
                }

                lblAdminMessage.Foreground = System.Windows.Media.Brushes.LightGreen;
                lblAdminMessage.Text = "Audit log exported successfully.";
            }
        }
        catch (Exception ex)
        {
            lblAdminMessage.Foreground = System.Windows.Media.Brushes.Red;
            lblAdminMessage.Text = "Error exporting: " + ex.Message;
        }
    }

    // ==========================================
    // SHARED
    // ==========================================

    private void btnLogout_Click(object sender, RoutedEventArgs e)
    {
        _stream?.Close();
        _client?.Close();

        MainWindow login = new MainWindow();
        login.Show();
        this.Close();
    }

    private string SendAndReceive(string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        _stream.Write(data, 0, data.Length);

        byte[] buffer = new byte[8192];
        int bytesRead = _stream.Read(buffer, 0, buffer.Length);
        return Encoding.UTF8.GetString(buffer, 0, bytesRead);
    }
}

// Classes for binding to DataGrids
public class TransactionRow
{
    public string Type { get; set; } = "";
    public string Amount { get; set; } = "";
    public string BalanceAfter { get; set; } = "";
    public string Date { get; set; } = "";
    public string Description { get; set; } = "";
}

public class PendingLoanRow
{
    public string LoanId { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string Amount { get; set; } = "";
    public string Rate { get; set; } = "";
    public string Term { get; set; } = "";
}

public class ActiveLoanRow
{
    public string LoanId { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string Amount { get; set; } = "";
    public string RemainingBalance { get; set; } = "";
}

public class AuditLogRow
{
    public string User { get; set; } = "";
    public string Action { get; set; } = "";
    public string Details { get; set; } = "";
    public string Timestamp { get; set; } = "";
}