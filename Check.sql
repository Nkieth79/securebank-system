USE SecureBank;
GO

-- 1. Check Users
SELECT UserID, Username, FullName, Role, IsActive FROM Users;

-- 2. Check the 10 Customers
SELECT CustomerID, FirstName, LastName, IDNumber, KYCStatus FROM Customers;

-- 3. Check Accounts
SELECT AccountID, AccountNumber, CustomerID, Type, Balance, Status FROM Accounts;

-- 4. Check Pending Loans
SELECT LoanID, CustomerID, PrincipalAmount, Status FROM Loans;