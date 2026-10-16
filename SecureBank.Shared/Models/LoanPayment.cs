using System;
using System.Collections.Generic;
using System.Text;

namespace SecureBank.Shared.Models;

public class LoanPayment
{
    public int PaymentID
    {
        get;
        set;
    }

    public int LoanID
    {
        get;
        set;
    }

    public decimal Amount
    {
        get;
        set;
    }

    public DateTime PaymentDate
    {
        get;
        set;
    }

    public decimal RemainingBalance
    {
        get;
        set;
    }

    public int RecordedBy
    {
        get;
        set;
    }
}
