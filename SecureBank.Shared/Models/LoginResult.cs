using System;
using System.Collections.Generic;
using System.Text;

namespace SecureBank.Shared.Models;

public class LoginResult
{
    public bool Success
    {
        get;
        set;
    }

    public string Message
    {
        get;
        set;
    }

    public string Role
    {
        get;
        set;
    }

    public string FullName
    {
        get;
        set;
    }

    public int UserID
    {
        get;
        set;
    }
}
