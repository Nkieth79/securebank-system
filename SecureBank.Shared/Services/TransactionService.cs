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

    public string Deposit(string accountNumber, decimal amount, int performedBy)
    {
        if (amount <= 0)
        {
            return "ERROR|Amount must be greater than zero";
        }

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();

            decimal currentBalance;
            int accountId;

            string balanceQuery = "SELECT AccountID, Balance FROM Accounts WHERE AccountNumber = @acc AND Status = 'Active'";
            using (SqlCommand cmd = new SqlCommand(balanceQuery, conn))
            {
                cmd.Parameters.AddWithValue("@acc", accountNumber);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return "ERROR|Account not found or not active";
                    }
                    accountId = Convert.ToInt32(reader["AccountID"]);
                    currentBalance = Convert.ToDecimal(reader["Balance"]);
                }
            }

            decimal newBalance = currentBalance + amount;

            string updateQuery = "UPDATE Accounts SET Balance = @balance WHERE AccountID = @id";
            using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
            {
                cmd.Parameters.AddWithValue("@balance", newBalance);
                cmd.Parameters.AddWithValue("@id", accountId);
                cmd.ExecuteNonQuery();
            }

            string txQuery = "INSERT INTO Transactions (AccountID, Type, Amount, Description, BalanceAfter, PerformedBy) VALUES (@accId, 'Deposit', @amount, 'Cash deposit', @balanceAfter, @by)";
            using (SqlCommand cmd = new SqlCommand(txQuery, conn))
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
        if (amount <= 0)
        {
            return "ERROR|Amount must be greater than zero";
        }

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();

            decimal currentBalance;
            int accountId;

            string balanceQuery = "SELECT AccountID, Balance FROM Accounts WHERE AccountNumber = @acc AND Status = 'Active'";
            using (SqlCommand cmd = new SqlCommand(balanceQuery, conn))
            {
                cmd.Parameters.AddWithValue("@acc", accountNumber);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return "ERROR|Account not found or not active";
                    }
                    accountId = Convert.ToInt32(reader["AccountID"]);
                    currentBalance = Convert.ToDecimal(reader["Balance"]);
                }
            }

            if (currentBalance < amount)
            {
                return "ERROR|Insufficient funds";
            }

            decimal newBalance = currentBalance - amount;

            string updateQuery = "UPDATE Accounts SET Balance = @balance WHERE AccountID = @id";
            using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
            {
                cmd.Parameters.AddWithValue("@balance", newBalance);
                cmd.Parameters.AddWithValue("@id", accountId);
                cmd.ExecuteNonQuery();
            }

            string txQuery = "INSERT INTO Transactions (AccountID, Type, Amount, Description, BalanceAfter, PerformedBy) VALUES (@accId, 'Withdrawal', @amount, 'Cash withdrawal', @balanceAfter, @by)";
            using (SqlCommand cmd = new SqlCommand(txQuery, conn))
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
        if (amount <= 0)
        {
            return "ERROR|Amount must be greater than zero";
        }

        if (fromAccount == toAccount)
        {
            return "ERROR|Cannot transfer to the same account";
        }

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();

            int fromId;
            int toId;
            decimal fromBalance;

            string fromQuery = "SELECT AccountID, Balance FROM Accounts WHERE AccountNumber = @acc AND Status = 'Active'";
            using (SqlCommand cmd = new SqlCommand(fromQuery, conn))
            {
                cmd.Parameters.AddWithValue("@acc", fromAccount);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return "ERROR|Source account not found";
                    }
                    fromId = Convert.ToInt32(reader["AccountID"]);
                    fromBalance = Convert.ToDecimal(reader["Balance"]);
                }
            }

            string toQuery = "SELECT AccountID FROM Accounts WHERE AccountNumber = @acc AND Status = 'Active'";
            using (SqlCommand cmd = new SqlCommand(toQuery, conn))
            {
                cmd.Parameters.AddWithValue("@acc", toAccount);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return "ERROR|Destination account not found";
                    }
                    toId = Convert.ToInt32(reader["AccountID"]);
                }
            }

            if (fromBalance < amount)
            {
                return "ERROR|Insufficient funds";
            }

            decimal newFromBalance = fromBalance - amount;

            string updateFromQuery = "UPDATE Accounts SET Balance = @balance WHERE AccountID = @id";
            using (SqlCommand cmd = new SqlCommand(updateFromQuery, conn))
            {
                cmd.Parameters.AddWithValue("@balance", newFromBalance);
                cmd.Parameters.AddWithValue("@id", fromId);
                cmd.ExecuteNonQuery();
            }

            string updateToQuery = "UPDATE Accounts SET Balance = Balance + @amount WHERE AccountID = @id";
            using (SqlCommand cmd = new SqlCommand(updateToQuery, conn))
            {
                cmd.Parameters.AddWithValue("@amount", amount);
                cmd.Parameters.AddWithValue("@id", toId);
                cmd.ExecuteNonQuery();
            }

            string fromTxQuery = "INSERT INTO Transactions (AccountID, Type, Amount, Description, BalanceAfter, PerformedBy) VALUES (@accId, 'Transfer', @amount, @desc, @balanceAfter, @by)";
            using (SqlCommand cmd = new SqlCommand(fromTxQuery, conn))
            {
                cmd.Parameters.AddWithValue("@accId", fromId);
                cmd.Parameters.AddWithValue("@amount", amount);
                cmd.Parameters.AddWithValue("@desc", "Transfer to " + toAccount);
                cmd.Parameters.AddWithValue("@balanceAfter", newFromBalance);
                cmd.Parameters.AddWithValue("@by", performedBy);
                cmd.ExecuteNonQuery();
            }

            using (SqlCommand cmd = new SqlCommand(fromTxQuery, conn))
            {
                cmd.Parameters.AddWithValue("@accId", toId);
                cmd.Parameters.AddWithValue("@amount", amount);
                cmd.Parameters.AddWithValue("@desc", "Transfer from " + fromAccount);
                cmd.Parameters.AddWithValue("@balanceAfter", 0);
                cmd.Parameters.AddWithValue("@by", performedBy);
                cmd.ExecuteNonQuery();
            }
            return "OK|" + newFromBalance.ToString("F2");
            //return "OK|Transferred " + amount.ToString("F2");
        }
    }

    public string LookupAccount(string accountNumber)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();

            string query = @"SELECT a.AccountNumber, a.Type, a.Balance, a.Status,
                                    c.FirstName, c.LastName
                             FROM Accounts a
                             INNER JOIN Customers c ON a.CustomerID = c.CustomerID
                             WHERE a.AccountNumber = @acc";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@acc", accountNumber);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return "ERROR|Account not found";
                    }

                    string firstName = reader["FirstName"].ToString() ?? "";
                    string lastName = reader["LastName"].ToString() ?? "";
                    string type = reader["Type"].ToString() ?? "";
                    decimal balance = Convert.ToDecimal(reader["Balance"]);

                    return "OK|" + firstName + "|" + lastName + "|" + type + "|" + balance.ToString("F2");
                }
            }
        }
    }


}