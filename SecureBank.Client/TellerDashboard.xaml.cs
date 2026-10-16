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
    }

    private void btnLookup_Click(object sender, RoutedEventArgs e)
    {
        lblLookupMessage.Text = "";
        lblTransactionMessage.Text = "";

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
            }
            else
            {
                lblLookupMessage.Foreground = System.Windows.Media.Brushes.Red;
                lblLookupMessage.Text = response;
                lblCustomerName.Text = "";
                lblAccountType.Text = "";
                lblBalance.Text = "R0.00";
            }
        }
        catch (Exception ex)
        {
            lblLookupMessage.Foreground = System.Windows.Media.Brushes.Red;
            lblLookupMessage.Text = "Error: " + ex.Message;
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