using System.Net.Sockets;
using System.Text;
using System.Windows;

namespace SecureBank.Client;

public partial class MainWindow : Window
{
    private TcpClient? _client;
    private NetworkStream? _stream;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void btnLogin_Click(object sender, RoutedEventArgs e)
    {
        string username = txtUsername.Text.Trim();
        string password = txtPassword.Password;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            lblMessage.Foreground = System.Windows.Media.Brushes.Orange;
            lblMessage.Text = "Please enter username and password.";
            return;
        }

        try
        {
            lblMessage.Foreground = System.Windows.Media.Brushes.Yellow;
            lblMessage.Text = "Connecting...";

            _client = new TcpClient();
            _client.Connect("127.0.0.1", 5000);
            _stream = _client.GetStream();

            string response = SendAndReceive("LOGIN|" + username + "|" + password);

            if (response.StartsWith("OK"))
            {
              
                string[] parts = response.Split('|');
                string fullName = parts.Length > 2 ? parts[2] : "";
                string role = parts.Length > 3 ? parts[3] : "";

                lblMessage.Foreground = System.Windows.Media.Brushes.LightGreen;
                lblMessage.Text = "Welcome, " + fullName + " (" + role + ")";
            }
            else
            {
                lblMessage.Foreground = System.Windows.Media.Brushes.Red;
                lblMessage.Text = "Login failed: " + response;

                _client.Close();
                _client = null;
                _stream = null;
            }
        }
        catch (Exception ex)
        {
            lblMessage.Foreground = System.Windows.Media.Brushes.Red;
            lblMessage.Text = "Error: " + ex.Message;
        }
    }

    private string SendAndReceive(string message)
    {
        if (_stream == null) throw new InvalidOperationException("Not connected");

        byte[] data = Encoding.UTF8.GetBytes(message);
        _stream.Write(data, 0, data.Length);

        byte[] buffer = new byte[4096];
        int bytesRead = _stream.Read(buffer, 0, buffer.Length);
        return Encoding.UTF8.GetString(buffer, 0, bytesRead);
    }
}