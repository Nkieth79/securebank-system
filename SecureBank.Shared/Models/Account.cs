using System;
using System.Collections.Generic;
using System.Text;

namespace SecureBank.Shared.Models;

public class Account
{
    public int AccountID
    {
        get;
        set;
    }

    public string AccountNumber
    {
        get;
        set;
    }

    public int CustomerID
    {
        get;
        set;
    }

    public string Type
    {
        get;
        set;
    }

    public decimal Balance
    {
        get;
        set;
    }

    public string Status
    {
        get;
        set;
    }

    public DateTime OpenedDate
    {
        get;
        set;
    }

    public DateTime? ClosedDate
    {
        get;
        set;
    }
}
