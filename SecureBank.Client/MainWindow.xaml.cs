using System.Net.Sockets;
using System.Text;
using System.Windows;

namespace SecureBank.Client;

public partial class MainWindow : Window
{
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

            TcpClient client = new TcpClient();
            client.Connect("127.0.0.1", 5000);
            NetworkStream stream = client.GetStream();

            byte[] data = Encoding.UTF8.GetBytes("LOGIN|" + username + "|" + password);
            stream.Write(data, 0, data.Length);

            byte[] buffer = new byte[4096];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            if (response.StartsWith("OK"))
            {
                string[] parts = response.Split('|');
                string fullName = parts.Length > 2 ? parts[2] : "";
                string role = parts.Length > 3 ? parts[3] : "";

                Dashboard dashboard = new Dashboard(client, fullName, role);
                dashboard.Show();
                this.Close();
            }
            else
            {
                lblMessage.Foreground = System.Windows.Media.Brushes.Red;
                lblMessage.Text = "Login failed: " + response;
                client.Close();
            }
        }
        catch (Exception ex)
        {
            lblMessage.Foreground = System.Windows.Media.Brushes.Red;
            lblMessage.Text = "Error: " + ex.Message;
        }
    }
}