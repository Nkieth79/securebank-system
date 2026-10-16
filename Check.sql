USE SecureBank;
SELECT AccountNumber, Balance FROM Accounts;
SELECT TOP 10 TransactionID, AccountID, Type, Amount, BalanceAfter, Date FROM Transactions ORDER BY TransactionID DESC;