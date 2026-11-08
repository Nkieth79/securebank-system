using System.Security.Cryptography;
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

        string hashedPassword = HashPassword(password);

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

                string storedHash = reader["PasswordHash"].ToString() ?? "";
                bool isActive = Convert.ToBoolean(reader["IsActive"]);

                if (!isActive)
                {
                    result.Message = "Account is disabled";
                    return result;
                }

                if (storedHash != hashedPassword)
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

    public string HashPassword(string password)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
}