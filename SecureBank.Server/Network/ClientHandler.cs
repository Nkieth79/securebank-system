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

        if (command == "DEPOSIT")
        {
            if (!decimal.TryParse(parts[2], out decimal amount)) return MessageProtocol.Build(MessageProtocol.ResponseError, "Invalid amount format");
            string res = _transactionService.Deposit(parts[1], amount, _loggedInUser.UserID);
            if (res.StartsWith("OK")) _transactionService.LogAction(_loggedInUser.UserID, "DEPOSIT", "Deposited R" + amount + " to " + parts[1]);
            return res;
        }

        if (command == "WITHDRAW")
        {
            if (!decimal.TryParse(parts[2], out decimal amount)) return MessageProtocol.Build(MessageProtocol.ResponseError, "Invalid amount format");
            return _transactionService.Withdraw(parts[1], amount, _loggedInUser.UserID);
        }

        if (command == "TRANSFER")
        {
            if (!decimal.TryParse(parts[3], out decimal amount)) return MessageProtocol.Build(MessageProtocol.ResponseError, "Invalid amount format");
            return _transactionService.Transfer(parts[1], parts[2], amount, _loggedInUser.UserID);
        }

        if (command == "CREATELOAN")
        {
            if (!int.TryParse(parts[1], out int custId) ||
                !decimal.TryParse(parts[2], out decimal loanAmount) ||
                !decimal.TryParse(parts[3], out decimal rate) ||
                !int.TryParse(parts[4], out int term))
            {
                return MessageProtocol.Build(MessageProtocol.ResponseError, "Invalid number format in loan application");
            }
            return _transactionService.CreateLoan(custId, loanAmount, rate, term);
        }

        if (command == "PAYLOAN")
        {
            if (!int.TryParse(parts[1], out int loanId) || !decimal.TryParse(parts[2], out decimal amount)) return MessageProtocol.Build(MessageProtocol.ResponseError, "Invalid arguments");
            return _transactionService.PayLoan(loanId, amount, _loggedInUser.UserID);
        }

        if (command == "CREATECUSTOMER")
        {
            if (parts.Length < 7) return MessageProtocol.Build(MessageProtocol.ResponseError, "Missing arguments");
            return _transactionService.CreateCustomer(parts[1], parts[2], parts[3], parts[4], parts[5], parts[6]);
        }

        if (command == "OPENACCOUNT")
        {
            if (!int.TryParse(parts[1], out int custId) || !decimal.TryParse(parts[3], out decimal deposit)) return MessageProtocol.Build(MessageProtocol.ResponseError, "Invalid arguments");
            return _transactionService.OpenAccount(custId, parts[2], deposit);
        }

        // Manager Commands
        if (command == "GETLOANS")
        {
            if (_loggedInUser.Role != "Manager" && _loggedInUser.Role != "Admin")
                return MessageProtocol.Build(MessageProtocol.ResponseError, "Access denied. Manager only.");
            return _transactionService.GetPendingLoans();
        }

        if (command == "GETACTIVELOANS")
        {
            if (_loggedInUser.Role != "Manager" && _loggedInUser.Role != "Admin")
                return MessageProtocol.Build(MessageProtocol.ResponseError, "Access denied. Manager only.");
            return _transactionService.GetActiveLoans();
        }

        if (command == "APPROVELOAN")
        {
            if (_loggedInUser.Role != "Manager" && _loggedInUser.Role != "Admin")
                return MessageProtocol.Build(MessageProtocol.ResponseError, "Access denied. Manager only.");
            string res = _transactionService.ApproveLoan(int.Parse(parts[1]), _loggedInUser.UserID);
            if (res.StartsWith("OK")) _transactionService.LogAction(_loggedInUser.UserID, "APPROVE LOAN", "Approved Loan ID " + parts[1]);
            return res;
        }

        if (command == "REJECTLOAN")
        {
            if (_loggedInUser.Role != "Manager" && _loggedInUser.Role != "Admin")
                return MessageProtocol.Build(MessageProtocol.ResponseError, "Access denied. Manager only.");
            string res = _transactionService.RejectLoan(int.Parse(parts[1]), _loggedInUser.UserID);
            if (res.StartsWith("OK")) _transactionService.LogAction(_loggedInUser.UserID, "REJECT LOAN", "Rejected Loan ID " + parts[1]);
            return res;
        }

        if (command == "CLOSEACCOUNT")
        {
            if (_loggedInUser.Role != "Manager" && _loggedInUser.Role != "Admin")
                return MessageProtocol.Build(MessageProtocol.ResponseError, "Access denied. Manager only.");
            string res = _transactionService.CloseAccount(parts[1], _loggedInUser.UserID);
            if (res.StartsWith("OK")) _transactionService.LogAction(_loggedInUser.UserID, "CLOSE ACCOUNT", "Closed account " + parts[1]);
            return res;
        }

        // Admin Commands
        if (command == "GETAUDITLOG")
        {
            if (_loggedInUser.Role != "Admin")
                return MessageProtocol.Build(MessageProtocol.ResponseError, "Access denied. Admin only.");
            return _transactionService.GetAuditLogs();
        }

        if (command == "TOGGLEUSER")
        {
            if (_loggedInUser.Role != "Admin")
                return MessageProtocol.Build(MessageProtocol.ResponseError, "Access denied. Admin only.");

            string res = _transactionService.ToggleUserStatus(parts[1]);
            if (res.StartsWith("OK"))
                _transactionService.LogAction(_loggedInUser.UserID, "TOGGLE USER", res.Substring(3));

            return res;
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