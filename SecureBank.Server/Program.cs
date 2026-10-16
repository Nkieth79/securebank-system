using SecureBank.Server.Network;
using SecureBank.Server.Services;
using SecureBank.Shared.Protocol;

namespace SecureBank.Server;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== SecureBank Server ===");

        string connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=SecureBank;Integrated Security=True;TrustServerCertificate=True";

        AuthService authService = new AuthService(connectionString);
        TransactionService transactionService = new TransactionService(connectionString);
        TcpServer server = new TcpServer(MessageProtocol.Port, authService, transactionService);
        server.Start();

        Console.WriteLine("Press Enter to stop server.");
        Console.ReadLine();

        server.Stop();
    }
}