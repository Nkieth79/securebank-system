using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Data.SqlClient;
using SecureBank.Shared.Models;

namespace SecureBank.Server.Services;

public class AuthService
{
    private readonly string _connectionString;

    public AuthService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public LoginResult Login(string username, string password)
    {
        LoginResult result = new LoginResult
        {
            Success = false,
            Message = "Invalid credentials"
        };

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();

            string query = "SELECT UserID, Username, PasswordHash, FullName, Role, IsActive FROM Users WHERE Username = @username";
            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@username", username);

            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                if (!reader.Read())
                {
                    return result;
                }

                string storedPassword = reader["PasswordHash"].ToString() ?? "";
                bool isActive = Convert.ToBoolean(reader["IsActive"]);

                if (!isActive)
                {
                    result.Message = "Account is disabled";
                    return result;
                }

                if (storedPassword != password)
                {
                    return result;
                }

                result.Success = true;
                result.Message = "Login successful";
                result.UserID = Convert.ToInt32(reader["UserID"]);
                result.FullName = reader["FullName"].ToString() ?? "";
                result.Role = reader["Role"].ToString() ?? "";
            }
        }

        return result;
    }
}
