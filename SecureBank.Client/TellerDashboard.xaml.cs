using System.Net.Sockets;
using System.Text;
using System.Windows;

namespace SecureBank.Client;

public partial class TellerDashboard : Window
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly string _userFullName;
    private readonly string _userRole;

    public TellerDashboard(TcpClient client, string fullName, string role)
    {
        InitializeComponent();
        _client = client;
        _stream = client.GetStream();
        _userFullName = fullName;
        _userRole = role;

        lblUser.Text = fullName + " (" + role + ")";
        UpdateButtonStates();
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
        string account = txtLookupAccount.Text.Trim();
        string amount = txtAmount.Text.Trim();

        try
        {
            string response = SendAndReceive("DEPOSIT|" + account + "|" + amount);
            HandleTransactionResponse(response);
        }
        catch (Exception ex)
        {
            lblTransactionResult.Foreground = System.Windows.Media.Brushes.Red;
            lblTransactionResult.Text = "Error: " + ex.Message;
        }
    }

    private void btnWithdraw_Click(object sender, RoutedEventArgs e)
    {
        string account = txtLookupAccount.Text.Trim();
        string amount = txtAmount.Text.Trim();

        try
        {
            string response = SendAndReceive("WITHDRAW|" + account + "|" + amount);
            HandleTransactionResponse(response);
        }
        catch (Exception ex)
        {
            lblTransactionResult.Foreground = System.Windows.Media.Brushes.Red;
            lblTransactionResult.Text = "Error: " + ex.Message;
        }
    }

    private void btnTransfer_Click(object sender, RoutedEventArgs e)
    {
        string from = txtLookupAccount.Text.Trim();
        string to = txtDestination.Text.Trim();
        string amount = txtAmount.Text.Trim();

        try
        {
            string response = SendAndReceive("TRANSFER|" + from + "|" + to + "|" + amount);
            HandleTransactionResponse(response);
        }
        catch (Exception ex)
        {
            lblTransactionResult.Foreground = System.Windows.Media.Brushes.Red;
            lblTransactionResult.Text = "Error: " + ex.Message;
        }
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

public class TransactionRow
{
    public string Type { get; set; } = "";
    public string Amount { get; set; } = "";
    public string BalanceAfter { get; set; } = "";
    public string Date { get; set; } = "";
    public string Description { get; set; } = "";
}