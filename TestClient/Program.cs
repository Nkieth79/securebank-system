using System.Net.Sockets;
using System.Text;

TcpClient client = new TcpClient();
client.Connect("127.0.0.1", 5000);
NetworkStream stream = client.GetStream();

string SendAndReceive(string message)
{
    byte[] data = Encoding.UTF8.GetBytes(message);
    stream.Write(data, 0, data.Length);
    byte[] buffer = new byte[4096];
    int bytesRead = stream.Read(buffer, 0, buffer.Length);
    return Encoding.UTF8.GetString(buffer, 0, bytesRead);
}

Console.WriteLine("=== TestClient ===");

Console.Write("Username: ");
string username = Console.ReadLine() ?? "";

Console.Write("Password: ");
string password = Console.ReadLine() ?? "";

Console.WriteLine("DEBUG: Sending login...");
string loginResponse = SendAndReceive("LOGIN|" + username + "|" + password);
Console.WriteLine("DEBUG: Received response: [" + loginResponse + "]");
Console.WriteLine("DEBUG: Response starts with OK? " + loginResponse.StartsWith("OK"));

if (!loginResponse.StartsWith("OK"))
{
    Console.WriteLine("Login failed. Exiting.");
    Console.ReadKey();
    return;
}

Console.WriteLine("DEBUG: About to enter menu loop...");

while (true)
{
    Console.WriteLine("\n--- Menu ---");
    Console.WriteLine("1. Deposit");
    Console.WriteLine("2. Withdraw");
    Console.WriteLine("3. Transfer");
    Console.WriteLine("4. Exit");
    Console.Write("Choice: ");

    string choice = Console.ReadLine() ?? "";

    if (choice == "4")
    {
        break;
    }

    Console.Write("Account number: ");
    string acc = Console.ReadLine() ?? "";

    if (choice == "1" || choice == "2")
    {
        Console.Write("Amount: ");
        string amount = Console.ReadLine() ?? "";

        string cmd = choice == "1" ? "DEPOSIT" : "WITHDRAW";
        string response = SendAndReceive(cmd + "|" + acc + "|" + amount);
        Console.WriteLine("Result: " + response);
    }
    else if (choice == "3")
    {
        Console.Write("Destination account: ");
        string dest = Console.ReadLine() ?? "";

        Console.Write("Amount: ");
        string amount = Console.ReadLine() ?? "";

        string response = SendAndReceive("TRANSFER|" + acc + "|" + dest + "|" + amount);
        Console.WriteLine("Result: " + response);
    }
}

client.Close();
Console.WriteLine("\nPress any key to exit.");
Console.ReadKey();