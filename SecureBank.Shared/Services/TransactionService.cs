using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.SqlClient;

namespace SecureBank.Server.Services;

public class TransactionService
{
    private readonly string _connectionString;

    public TransactionService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void LogAction(int userId, string action, string details)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = "INSERT INTO AuditLog (UserID, Action, EntityType, Details) VALUES (@uid, @act, 'System', @det)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@uid", userId);
                    cmd.Parameters.AddWithValue("@act", action);
                    cmd.Parameters.AddWithValue("@det", details);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        catch { }
    }

    public string GetAuditLogs()
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            string query = @"
                SELECT TOP 100 a.LogID, u.Username, a.Action, a.Details, a.Timestamp 
                FROM AuditLog a
                INNER JOIN Users u ON a.UserID = u.UserID
                ORDER BY a.LogID DESC";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                List<string> rows = new List<string>();
                while (reader.Read())
                {
                    string user = reader["Username"].ToString() ?? "";
                    string action = reader["Action"].ToString() ?? "";
                    string details = reader["Details"].ToString() ?? "";
                    string time = Convert.ToDateTime(reader["Timestamp"]).ToString("yyyy-MM-dd HH:mm:ss");

                    rows.Add(user + "~" + action + "~" + details + "~" + time);
                }

                return rows.Count == 0 ? "OK|EMPTY" : "OK|" + string.Join("~~", rows);
            }
        }
    }

    public string ToggleUserStatus(string username)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();

            if (username.ToLower() == "admin")
                return "ERROR|Security Rule: Cannot disable the main admin account";

            bool isCurrentlyActive = true;
            using (SqlCommand cmd = new SqlCommand("SELECT IsActive FROM Users WHERE Username = @user", conn))
            {
                cmd.Parameters.AddWithValue("@user", username);
                object result = cmd.ExecuteScalar();
                if (result == null) return "ERROR|User not found";
                isCurrentlyActive = Convert.ToBoolean(result);
            }

            bool newStatus = !isCurrentlyActive;
            using (SqlCommand cmd = new SqlCommand("UPDATE Users SET IsActive = @status WHERE Username = @user", conn))
            {
                cmd.Parameters.AddWithValue("@status", newStatus);
                cmd.Parameters.AddWithValue("@user", username);
                cmd.ExecuteNonQuery();
            }

            string statusText = newStatus ? "ACTIVE" : "DISABLED";
            return "OK|User '" + username + "' is now " + statusText;
        }
    }

    public string Deposit(string accountNumber, decimal amount, int performedBy)
    {
        if (amount <= 0) return "ERROR|Amount must be greater than zero";

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            decimal currentBalance;
            int accountId;

            using (SqlCommand cmd = new SqlCommand("SELECT AccountID, Balance FROM Accounts WHERE AccountNumber = @acc AND Status = 'Active'", conn))
            {
                cmd.Parameters.AddWithValue("@acc", accountNumber);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return "ERROR|Account not found or not active";
                    accountId = Convert.ToInt32(reader["AccountID"]);
                    currentBalance = Convert.ToDecimal(reader["Balance"]);
                }
            }

            decimal newBalance = currentBalance + amount;

            using (SqlCommand cmd = new SqlCommand("UPDATE Accounts SET Balance = @balance WHERE AccountID = @id", conn))
            {
                cmd.Parameters.AddWithValue("@balance", newBalance);
                cmd.Parameters.AddWithValue("@id", accountId);
                cmd.ExecuteNonQuery();
            }

            using (SqlCommand cmd = new SqlCommand("INSERT INTO Transactions (AccountID, Type, Amount, Description, BalanceAfter, PerformedBy) VALUES (@accId, 'Deposit', @amount, 'Cash deposit', @balanceAfter, @by)", conn))
            {
                cmd.Parameters.AddWithValue("@accId", accountId);
                cmd.Parameters.AddWithValue("@amount", amount);
                cmd.Parameters.AddWithValue("@balanceAfter", newBalance);
                cmd.Parameters.AddWithValue("@by", performedBy);
                cmd.ExecuteNonQuery();
            }

            return "OK|" + newBalance.ToString("F2");
        }
    }

    public string Withdraw(string accountNumber, decimal amount, int performedBy)
    {
        if (amount <= 0) return "ERROR|Amount must be greater than zero";

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            decimal currentBalance;
            int accountId;

            using (SqlCommand cmd = new SqlCommand("SELECT AccountID, Balance FROM Accounts WHERE AccountNumber = @acc AND Status = 'Active'", conn))
            {
                cmd.Parameters.AddWithValue("@acc", accountNumber);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return "ERROR|Account not found or not active";
                    accountId = Convert.ToInt32(reader["AccountID"]);
                    currentBalance = Convert.ToDecimal(reader["Balance"]);
                }
            }

            if (currentBalance < amount) return "ERROR|Insufficient funds";

            decimal newBalance = currentBalance - amount;

            using (SqlCommand cmd = new SqlCommand("UPDATE Accounts SET Balance = @balance WHERE AccountID = @id", conn))
            {
                cmd.Parameters.AddWithValue("@balance", newBalance);
                cmd.Parameters.AddWithValue("@id", accountId);
                cmd.ExecuteNonQuery();
            }

            using (SqlCommand cmd = new SqlCommand("INSERT INTO Transactions (AccountID, Type, Amount, Description, BalanceAfter, PerformedBy) VALUES (@accId, 'Withdrawal', @amount, 'Cash withdrawal', @balanceAfter, @by)", conn))
            {
                cmd.Parameters.AddWithValue("@accId", accountId);
                cmd.Parameters.AddWithValue("@amount", amount);
                cmd.Parameters.AddWithValue("@balanceAfter", newBalance);
                cmd.Parameters.AddWithValue("@by", performedBy);
                cmd.ExecuteNonQuery();
            }

            return "OK|" + newBalance.ToString("F2");
        }
    }

    public string Transfer(string fromAccount, string toAccount, decimal amount, int performedBy)
    {
        if (amount <= 0) return "ERROR|Amount must be greater than zero";
        if (fromAccount == toAccount) return "ERROR|Cannot transfer to the same account";

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            int fromId, toId;
            decimal fromBalance;

            using (SqlCommand cmd = new SqlCommand("SELECT AccountID, Balance FROM Accounts WHERE AccountNumber = @acc AND Status = 'Active'", conn))
            {
                cmd.Parameters.AddWithValue("@acc", fromAccount);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return "ERROR|Source account not found";
                    fromId = Convert.ToInt32(reader["AccountID"]);
                    fromBalance = Convert.ToDecimal(reader["Balance"]);
                }
            }

            using (SqlCommand cmd = new SqlCommand("SELECT AccountID FROM Accounts WHERE AccountNumber = @acc AND Status = 'Active'", conn))
            {
                cmd.Parameters.AddWithValue("@acc", toAccount);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return "ERROR|Destination account not found";
                    toId = Convert.ToInt32(reader["AccountID"]);
                }
            }

            if (fromBalance < amount) return "ERROR|Insufficient funds";

            decimal newFromBalance = fromBalance - amount;

            using (SqlCommand cmd = new SqlCommand("UPDATE Accounts SET Balance = @balance WHERE AccountID = @id", conn))
            {
                cmd.Parameters.AddWithValue("@balance", newFromBalance);
                cmd.Parameters.AddWithValue("@id", fromId);
                cmd.ExecuteNonQuery();
            }

            using (SqlCommand cmd = new SqlCommand("UPDATE Accounts SET Balance = Balance + @amount WHERE AccountID = @id", conn))
            {
                cmd.Parameters.AddWithValue("@amount", amount);
                cmd.Parameters.AddWithValue("@id", toId);
                cmd.ExecuteNonQuery();
            }

            string txQuery = "INSERT INTO Transactions (AccountID, Type, Amount, Description, BalanceAfter, PerformedBy) VALUES (@accId, 'Transfer', @amount, @desc, @balanceAfter, @by)";
            using (SqlCommand cmd = new SqlCommand(txQuery, conn))
            {
                cmd.Parameters.AddWithValue("@accId", fromId);
                cmd.Parameters.AddWithValue("@amount", amount);
                cmd.Parameters.AddWithValue("@desc", "Transfer to " + toAccount);
                cmd.Parameters.AddWithValue("@balanceAfter", newFromBalance);
                cmd.Parameters.AddWithValue("@by", performedBy);
                cmd.ExecuteNonQuery();
            }

            using (SqlCommand cmd = new SqlCommand(txQuery, conn))
            {
                cmd.Parameters.AddWithValue("@accId", toId);
                cmd.Parameters.AddWithValue("@amount", amount);
                cmd.Parameters.AddWithValue("@desc", "Transfer from " + fromAccount);
                cmd.Parameters.AddWithValue("@balanceAfter", 0);
                cmd.Parameters.AddWithValue("@by", performedBy);
                cmd.ExecuteNonQuery();
            }

            return "OK|" + newFromBalance.ToString("F2");
        }
    }

    public string LookupAccount(string accountNumber)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            string query = @"SELECT a.AccountNumber, a.Type, a.Balance, a.Status, c.FirstName, c.LastName
                             FROM Accounts a
                             INNER JOIN Customers c ON a.CustomerID = c.CustomerID
                             WHERE a.AccountNumber = @acc AND a.Status = 'Active'";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@acc", accountNumber);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return "ERROR|Account not found or closed";

                    string firstName = reader["FirstName"].ToString() ?? "";
                    string lastName = reader["LastName"].ToString() ?? "";
                    string type = reader["Type"].ToString() ?? "";
                    decimal balance = Convert.ToDecimal(reader["Balance"]);

                    return "OK|" + firstName + "|" + lastName + "|" + type + "|" + balance.ToString("F2");
                }
            }
        }
    }

    public string GetTransactions(string accountNumber)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            string query = @"SELECT TOP 10 t.Type, t.Amount, t.BalanceAfter, t.Date, t.Description
                             FROM Transactions t
                             INNER JOIN Accounts a ON t.AccountID = a.AccountID
                             WHERE a.AccountNumber = @acc
                             ORDER BY t.TransactionID DESC";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@acc", accountNumber);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    List<string> rows = new List<string>();
                    while (reader.Read())
                    {
                        string type = reader["Type"].ToString() ?? "";
                        string amount = Convert.ToDecimal(reader["Amount"]).ToString("F2");
                        string balanceAfter = Convert.ToDecimal(reader["BalanceAfter"]).ToString("F2");
                        string date = Convert.ToDateTime(reader["Date"]).ToString("yyyy-MM-dd HH:mm");
                        string description = reader["Description"].ToString() ?? "";

                        rows.Add(type + "~" + amount + "~" + balanceAfter + "~" + date + "~" + description);
                    }

                    return rows.Count == 0 ? "OK|EMPTY" : "OK|" + string.Join("~~", rows);
                }
            }
        }
    }

    public string CreateLoan(int customerId, decimal amount, decimal interestRate, int termMonths)
    {
        if (amount <= 0) return "ERROR|Amount must be greater than zero";
        if (termMonths <= 0) return "ERROR|Term must be greater than zero";

        decimal totalInterest = amount * interestRate * (termMonths / 12m);
        decimal totalRepayment = amount + totalInterest;
        decimal monthlyPayment = totalRepayment / termMonths;

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();

            using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Customers WHERE CustomerID = @id", conn))
            {
                cmd.Parameters.AddWithValue("@id", customerId);
                if ((int)cmd.ExecuteScalar() == 0) return "ERROR|Customer not found";
            }

            string query = @"INSERT INTO Loans (CustomerID, PrincipalAmount, InterestRate, TermMonths, MonthlyPayment, RemainingBalance, Status) 
                             VALUES (@cust, @amt, @rate, @term, @monthly, @balance, 'Pending');
                             SELECT SCOPE_IDENTITY();";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@cust", customerId);
                cmd.Parameters.AddWithValue("@amt", amount);
                cmd.Parameters.AddWithValue("@rate", interestRate);
                cmd.Parameters.AddWithValue("@term", termMonths);
                cmd.Parameters.AddWithValue("@monthly", monthlyPayment);
                cmd.Parameters.AddWithValue("@balance", totalRepayment);

                int loanId = Convert.ToInt32(cmd.ExecuteScalar());
                return "OK|Loan created with ID " + loanId + ". Awaiting approval.";
            }
        }
    }

    public string GetPendingLoans()
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            string query = @"SELECT l.LoanID, c.FirstName, c.LastName, l.PrincipalAmount, l.InterestRate, l.TermMonths 
                             FROM Loans l
                             INNER JOIN Customers c ON l.CustomerID = c.CustomerID
                             WHERE l.Status = 'Pending'";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                List<string> rows = new List<string>();
                while (reader.Read())
                {
                    string id = reader["LoanID"].ToString() ?? "";
                    string name = reader["FirstName"].ToString() + " " + reader["LastName"].ToString();
                    string amount = Convert.ToDecimal(reader["PrincipalAmount"]).ToString("F2");
                    string rate = Convert.ToDecimal(reader["InterestRate"]).ToString("P2");
                    string term = reader["TermMonths"].ToString() ?? "";

                    rows.Add(id + "~" + name + "~" + amount + "~" + rate + "~" + term);
                }

                return rows.Count == 0 ? "OK|EMPTY" : "OK|" + string.Join("~~", rows);
            }
        }
    }

    public string GetActiveLoans()
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            string query = @"SELECT l.LoanID, c.FirstName, c.LastName, l.PrincipalAmount, l.RemainingBalance 
                             FROM Loans l
                             INNER JOIN Customers c ON l.CustomerID = c.CustomerID
                             WHERE l.Status = 'Active'";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                List<string> rows = new List<string>();
                while (reader.Read())
                {
                    string id = reader["LoanID"].ToString() ?? "";
                    string name = reader["FirstName"].ToString() + " " + reader["LastName"].ToString();
                    string amount = Convert.ToDecimal(reader["PrincipalAmount"]).ToString("F2");
                    string remaining = Convert.ToDecimal(reader["RemainingBalance"]).ToString("F2");

                    rows.Add(id + "~" + name + "~" + amount + "~" + remaining);
                }

                return rows.Count == 0 ? "OK|EMPTY" : "OK|" + string.Join("~~", rows);
            }
        }
    }

    public string ApproveLoan(int loanId, int managerId)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            string query = "UPDATE Loans SET Status = 'Active', ApprovedBy = @mgr WHERE LoanID = @id AND Status = 'Pending'";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@mgr", managerId);
                cmd.Parameters.AddWithValue("@id", loanId);
                int rows = cmd.ExecuteNonQuery();
                return rows > 0 ? "OK|Loan approved successfully" : "ERROR|Loan not found or already processed";
            }
        }
    }

    public string RejectLoan(int loanId, int managerId)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            string query = "UPDATE Loans SET Status = 'Rejected', ApprovedBy = @mgr WHERE LoanID = @id AND Status = 'Pending'";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@mgr", managerId);
                cmd.Parameters.AddWithValue("@id", loanId);
                int rows = cmd.ExecuteNonQuery();
                return rows > 0 ? "OK|Loan rejected successfully" : "ERROR|Loan not found or already processed";
            }
        }
    }

    public string PayLoan(int loanId, decimal amount, int performedBy)
    {
        if (amount <= 0) return "ERROR|Amount must be greater than zero";

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            decimal currentBalance;
            string status;

            using (SqlCommand cmd = new SqlCommand("SELECT RemainingBalance, Status FROM Loans WHERE LoanID = @id", conn))
            {
                cmd.Parameters.AddWithValue("@id", loanId);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return "ERROR|Loan not found";
                    currentBalance = Convert.ToDecimal(reader["RemainingBalance"]);
                    status = reader["Status"].ToString() ?? "";
                }
            }

            if (status != "Active") return "ERROR|Loan is not active";
            if (amount > currentBalance) return "ERROR|Amount exceeds remaining balance";

            decimal newBalance = currentBalance - amount;
            string newStatus = newBalance == 0 ? "Paid" : "Active";

            using (SqlCommand cmd = new SqlCommand("UPDATE Loans SET RemainingBalance = @bal, Status = @status WHERE LoanID = @id", conn))
            {
                cmd.Parameters.AddWithValue("@bal", newBalance);
                cmd.Parameters.AddWithValue("@status", newStatus);
                cmd.Parameters.AddWithValue("@id", loanId);
                cmd.ExecuteNonQuery();
            }

            string paymentQuery = @"INSERT INTO LoanPayments (LoanID, Amount, RemainingBalance, RecordedBy) VALUES (@id, @amt, @bal, @by)";
            using (SqlCommand cmd = new SqlCommand(paymentQuery, conn))
            {
                cmd.Parameters.AddWithValue("@id", loanId);
                cmd.Parameters.AddWithValue("@amt", amount);
                cmd.Parameters.AddWithValue("@bal", newBalance);
                cmd.Parameters.AddWithValue("@by", performedBy);
                cmd.ExecuteNonQuery();
            }

            return "OK|Payment successful. Remaining balance: R" + newBalance.ToString("F2");
        }
    }

    public string CreateCustomer(string firstName, string lastName, string idNumber, string email, string phone, string address)
    {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(idNumber))
            return "ERROR|First name, last name, and ID number are required";

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();

            using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Customers WHERE IDNumber = @id", conn))
            {
                cmd.Parameters.AddWithValue("@id", idNumber);
                if ((int)cmd.ExecuteScalar() > 0) return "ERROR|Customer with this ID Number already exists";
            }

            string query = @"INSERT INTO Customers (FirstName, LastName, IDNumber, Email, Phone, Address, KYCStatus) 
                             VALUES (@first, @last, @id, @email, @phone, @address, 'Verified');
                             SELECT SCOPE_IDENTITY();";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@first", firstName);
                cmd.Parameters.AddWithValue("@last", lastName);
                cmd.Parameters.AddWithValue("@id", idNumber);
                cmd.Parameters.AddWithValue("@email", string.IsNullOrWhiteSpace(email) ? DBNull.Value : email);
                cmd.Parameters.AddWithValue("@phone", string.IsNullOrWhiteSpace(phone) ? DBNull.Value : phone);
                cmd.Parameters.AddWithValue("@address", string.IsNullOrWhiteSpace(address) ? DBNull.Value : address);

                int customerId = Convert.ToInt32(cmd.ExecuteScalar());
                return "OK|Customer created with ID " + customerId;
            }
        }
    }

    public string OpenAccount(int customerId, string type, decimal initialDeposit)
    {
        if (initialDeposit < 0) return "ERROR|Initial deposit cannot be negative";

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();

            using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Customers WHERE CustomerID = @id", conn))
            {
                cmd.Parameters.AddWithValue("@id", customerId);
                if ((int)cmd.ExecuteScalar() == 0) return "ERROR|Customer not found";
            }

            Random rnd = new Random();
            string accountNumber = "SB-" + rnd.Next(100000000, 999999999).ToString();

            string query = @"INSERT INTO Accounts (AccountNumber, CustomerID, Type, Balance, Status) 
                             VALUES (@accNum, @custId, @type, @balance, 'Active')";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@accNum", accountNumber);
                cmd.Parameters.AddWithValue("@custId", customerId);
                cmd.Parameters.AddWithValue("@type", type);
                cmd.Parameters.AddWithValue("@balance", initialDeposit);
                cmd.ExecuteNonQuery();
            }

            return "OK|Account " + accountNumber + " created with balance R" + initialDeposit.ToString("F2");
        }
    }

    public string CloseAccount(string accountNumber, int managerId)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();

            decimal balance = 0;
            int accountId = 0;
            string status = "";
            int customerId = 0;

            using (SqlCommand cmd = new SqlCommand("SELECT AccountID, Balance, Status, CustomerID FROM Accounts WHERE AccountNumber = @acc", conn))
            {
                cmd.Parameters.AddWithValue("@acc", accountNumber);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return "ERROR|Account not found";
                    accountId = Convert.ToInt32(reader["AccountID"]);
                    balance = Convert.ToDecimal(reader["Balance"]);
                    status = reader["Status"].ToString() ?? "";
                    customerId = Convert.ToInt32(reader["CustomerID"]);
                }
            }

            if (status == "Closed") return "ERROR|Account is already closed";
            if (balance > 0) return "ERROR|Cannot close account with a balance of R" + balance.ToString("F2") + ". Withdraw funds first.";

            using (SqlCommand cmd = new SqlCommand("UPDATE Accounts SET Status = 'Closed', ClosedDate = GETDATE() WHERE AccountID = @id", conn))
            {
                cmd.Parameters.AddWithValue("@id", accountId);
                cmd.ExecuteNonQuery();
            }

            using (SqlCommand cmd = new SqlCommand("UPDATE Loans SET Status = 'Rejected', ApprovedBy = @mgr WHERE CustomerID = @cust AND Status = 'Pending'", conn))
            {
                cmd.Parameters.AddWithValue("@mgr", managerId);
                cmd.Parameters.AddWithValue("@cust", customerId);
                cmd.ExecuteNonQuery();
            }

            return "OK|Account " + accountNumber + " has been closed. Any pending loans for this customer were rejected.";
        }
    }
}