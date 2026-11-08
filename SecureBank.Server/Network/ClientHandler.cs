using System.Net.Sockets;
using System.Text;
using SecureBank.Server.Services;
using SecureBank.Shared.Models;
using SecureBank.Shared.Protocol;

namespace SecureBank.Server.Network;

public class ClientHandler
{
    private readonly TcpClient _tcpClient;
    private readonly NetworkStream _stream;
    private readonly AuthService _authService;
    private readonly TransactionService _transactionService;
    private User? _loggedInUser;

    public ClientHandler(TcpClient tcpClient, AuthService authService, TransactionService transactionService)
    {
        _tcpClient = tcpClient;
        _stream = tcpClient.GetStream();
        _authService = authService;
        _transactionService = transactionService;
    }

    public void Handle()
    {
        try
        {
            byte[] buffer = new byte[8192];
            while (true)
            {
                int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                Console.WriteLine("Received: " + message);
                string response = ProcessMessage(message);
                Send(response);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Client error: " + ex.Message);
        }
        finally
        {
            _tcpClient.Close();
            Console.WriteLine("Client disconnected.");
        }
    }

    private string ProcessMessage(string message)
    {
        string[] parts = MessageProtocol.Parse(message);
        string command = parts[0];

        if (command == "LOGIN")
        {
            if (parts.Length < 3) return MessageProtocol.Build(MessageProtocol.ResponseError, "Missing credentials");
            LoginResult result = _authService.Login(parts[1], parts[2]);

            if (result.Success)
            {
                _loggedInUser = new User { UserID = result.UserID, FullName = result.FullName, Role = result.Role };
                return MessageProtocol.Build(MessageProtocol.ResponseOk, result.UserID.ToString(), result.FullName, result.Role);
            }
            return MessageProtocol.Build(MessageProtocol.ResponseError, result.Message);
        }

        if (_loggedInUser == null)
            return MessageProtocol.Build(MessageProtocol.ResponseError, "Not logged in");

        // Teller Commands
        if (command == "LOOKUP") return _transactionService.LookupAccount(parts[1]);
        if (command == "GETTRANSACTIONS") return _transactionService.GetTransactions(parts[1]);
        if (command == "DEPOSIT") return _transactionService.Deposit(parts[1], decimal.Parse(parts[2]), _loggedInUser.UserID);
        if (command == "WITHDRAW") return _transactionService.Withdraw(parts[1], decimal.Parse(parts[2]), _loggedInUser.UserID);
        if (command == "TRANSFER") return _transactionService.Transfer(parts[1], parts[2], decimal.Parse(parts[3]), _loggedInUser.UserID);

        if (command == "CREATELOAN")
            return _transactionService.CreateLoan(int.Parse(parts[1]), decimal.Parse(parts[2]), decimal.Parse(parts[3]), int.Parse(parts[4]));

        if (command == "PAYLOAN")
            return _transactionService.PayLoan(int.Parse(parts[1]), decimal.Parse(parts[2]), _loggedInUser.UserID);

        // Manager Commands (Security check included)
        if (command == "GETLOANS")
        {
            if (_loggedInUser.Role != "Manager" && _loggedInUser.Role != "Admin")
                return MessageProtocol.Build(MessageProtocol.ResponseError, "Access denied. Manager only.");

            return _transactionService.GetPendingLoans();
        }

        if (command == "APPROVELOAN")
        {
            if (_loggedInUser.Role != "Manager" && _loggedInUser.Role != "Admin")
                return MessageProtocol.Build(MessageProtocol.ResponseError, "Access denied. Manager only.");

            return _transactionService.ApproveLoan(int.Parse(parts[1]), _loggedInUser.UserID);
        }

        return MessageProtocol.Build(MessageProtocol.ResponseError, "Unknown command");
    }

    public void Send(string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        _stream.Write(data, 0, data.Length);
        _stream.Flush();
    }
}