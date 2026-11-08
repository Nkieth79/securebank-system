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
        try
        {
            string response = SendAndReceive("DEPOSIT|" + txtLookupAccount.Text.Trim() + "|" + txtAmount.Text.Trim());
            HandleTransactionResponse(response);
        }
        catch (Exception ex) { lblTransactionResult.Text = "Error: " + ex.Message; }
    }

    private void btnWithdraw_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string response = SendAndReceive("WITHDRAW|" + txtLookupAccount.Text.Trim() + "|" + txtAmount.Text.Trim());
            HandleTransactionResponse(response);
        }
        catch (Exception ex) { lblTransactionResult.Text = "Error: " + ex.Message; }
    }

    private void btnTransfer_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string response = SendAndReceive("TRANSFER|" + txtLookupAccount.Text.Trim() + "|" + txtDestination.Text.Trim() + "|" + txtAmount.Text.Trim());
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

        if (string.IsNullOrWhiteSpace(txtLoanCustId.Text) || string.IsNullOrWhiteSpace(txtLoanAmount.Text) ||
            string.IsNullOrWhiteSpace(txtLoanRate.Text) || string.IsNullOrWhiteSpace(txtLoanTerm.Text))
        {
            lblLoanMessage.Foreground = System.Windows.Media.Brushes.Red;
            lblLoanMessage.Text = "Please fill in all loan fields.";
            return;
        }

        try
        {
            string cmd = $"CREATELOAN|{txtLoanCustId.Text.Trim()}|{txtLoanAmount.Text.Trim()}|{txtLoanRate.Text.Trim()}|{txtLoanTerm.Text.Trim()}";
            string response = SendAndReceive(cmd);

            if (response.StartsWith("OK"))
            {
                lblLoanMessage.Foreground = System.Windows.Media.Brushes.LightGreen;
                lblLoanMessage.Text = response.Substring(3); // Remove "OK|"
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
                btnRefreshLoans_Click(sender, e); // Refresh the grid
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

    // ==========================================
    // SHARED
    // ==========================================

    private void btnLogout_Click(object sender, RoutedEventArgs e)
    {
        _stream?.Close();
        _client?.Close();
        Application.Current.Shutdown();
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