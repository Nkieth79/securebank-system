using System;
using System.Collections.Generic;
using System.Text;
namespace SecureBank.Shared.Models;

public class Transaction
{
    public int TransactionID
    {
        get;
        set;
    }

    public int AccountID
    {
        get;
        set;
    }

    public string Type
    {
        get;
        set;
    }

    public decimal Amount
    {
        get;
        set;
    }

    public DateTime Date
    {
        get;
        set;
    }

    public string Description
    {
        get;
        set;
    }

    public decimal BalanceAfter
    {
        get;
        set;
    }

    public int PerformedBy
    {
        get;
        set;
    }
}
