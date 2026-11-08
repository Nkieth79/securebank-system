USE SecureBank;
IF NOT EXISTS (SELECT 1 FROM Accounts WHERE AccountNumber = 'SB-000002')
BEGIN
    INSERT INTO Accounts (AccountNumber, CustomerID, Type, Balance, Status)
    VALUES ('SB-000002', 1, 'Cheque', 500.00, 'Active');
    PRINT 'Second account created.';
END