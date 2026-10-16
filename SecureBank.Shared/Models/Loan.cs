using System;
using System.Collections.Generic;
using System.Text;
namespace SecureBank.Shared.Models;

public class Loan
{
    public int LoanID
    {
        get;
        set;
    }

    public int CustomerID
    {
        get;
        set;
    }

    public decimal PrincipalAmount
    {
        get;
        set;
    }

    public decimal InterestRate
    {
        get;
        set;
    }

    public int TermMonths
    {
        get;
        set;
    }

    public DateTime StartDate
    {
        get;
        set;
    }

    public decimal MonthlyPayment
    {
        get;
        set;
    }

    public decimal RemainingBalance
    {
        get;
        set;
    }

    public string Status
    {
        get;
        set;
    }

    public int? ApprovedBy
    {
        get;
        set;
    }
}
