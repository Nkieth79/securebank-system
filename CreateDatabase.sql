IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'SecureBank')
BEGIN
    CREATE DATABASE SecureBank;
END
GO

USE SecureBank;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        UserID INT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(50) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(100) NOT NULL,
        FullName NVARCHAR(100) NOT NULL,
        Role NVARCHAR(20) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Customers')
BEGIN
    CREATE TABLE Customers (
        CustomerID INT IDENTITY(1,1) PRIMARY KEY,
        FirstName NVARCHAR(50) NOT NULL,
        LastName NVARCHAR(50) NOT NULL,
        IDNumber NVARCHAR(13) NOT NULL UNIQUE,
        Email NVARCHAR(100),
        Phone NVARCHAR(15),
        Address NVARCHAR(200),
        JoinDate DATETIME NOT NULL DEFAULT GETDATE(),
        KYCStatus NVARCHAR(20) NOT NULL DEFAULT 'Pending'
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Accounts')
BEGIN
    CREATE TABLE Accounts (
        AccountID INT IDENTITY(1,1) PRIMARY KEY,
        AccountNumber NVARCHAR(20) NOT NULL UNIQUE,
        CustomerID INT NOT NULL,
        Type NVARCHAR(20) NOT NULL,
        Balance DECIMAL(18,2) NOT NULL DEFAULT 0,
        Status NVARCHAR(20) NOT NULL DEFAULT 'Active',
        OpenedDate DATETIME NOT NULL DEFAULT GETDATE(),
        ClosedDate DATETIME NULL,
        CONSTRAINT FK_Accounts_Customers FOREIGN KEY (CustomerID) REFERENCES Customers(CustomerID)
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Transactions')
BEGIN
    CREATE TABLE Transactions (
        TransactionID INT IDENTITY(1,1) PRIMARY KEY,
        AccountID INT NOT NULL,
        Type NVARCHAR(20) NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        Date DATETIME NOT NULL DEFAULT GETDATE(),
        Description NVARCHAR(200),
        BalanceAfter DECIMAL(18,2) NOT NULL,
        PerformedBy INT NOT NULL,
        CONSTRAINT FK_Transactions_Accounts FOREIGN KEY (AccountID) REFERENCES Accounts(AccountID),
        CONSTRAINT FK_Transactions_Users FOREIGN KEY (PerformedBy) REFERENCES Users(UserID)
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Loans')
BEGIN
    CREATE TABLE Loans (
        LoanID INT IDENTITY(1,1) PRIMARY KEY,
        CustomerID INT NOT NULL,
        PrincipalAmount DECIMAL(18,2) NOT NULL,
        InterestRate DECIMAL(5,4) NOT NULL,
        TermMonths INT NOT NULL,
        StartDate DATETIME NOT NULL DEFAULT GETDATE(),
        MonthlyPayment DECIMAL(18,2) NOT NULL,
        RemainingBalance DECIMAL(18,2) NOT NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'Pending',
        ApprovedBy INT NULL,
        CONSTRAINT FK_Loans_Customers FOREIGN KEY (CustomerID) REFERENCES Customers(CustomerID),
        CONSTRAINT FK_Loans_Users FOREIGN KEY (ApprovedBy) REFERENCES Users(UserID)
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LoanPayments')
BEGIN
    CREATE TABLE LoanPayments (
        PaymentID INT IDENTITY(1,1) PRIMARY KEY,
        LoanID INT NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        PaymentDate DATETIME NOT NULL DEFAULT GETDATE(),
        RemainingBalance DECIMAL(18,2) NOT NULL,
        RecordedBy INT NOT NULL,
        CONSTRAINT FK_LoanPayments_Loans FOREIGN KEY (LoanID) REFERENCES Loans(LoanID),
        CONSTRAINT FK_LoanPayments_Users FOREIGN KEY (RecordedBy) REFERENCES Users(UserID)
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLog')
BEGIN
    CREATE TABLE AuditLog (
        LogID INT IDENTITY(1,1) PRIMARY KEY,
        UserID INT NOT NULL,
        Action NVARCHAR(50) NOT NULL,
        EntityType NVARCHAR(50) NOT NULL,
        EntityID INT NULL,
        Timestamp DATETIME NOT NULL DEFAULT GETDATE(),
        Details NVARCHAR(500),
        CONSTRAINT FK_AuditLog_Users FOREIGN KEY (UserID) REFERENCES Users(UserID)
    );
END
GO

INSERT INTO Users (Username, PasswordHash, FullName, Role, IsActive)
SELECT 'admin', 'admin123', 'System Administrator', 'Admin', 1
WHERE NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'admin');
GO

INSERT INTO Users (Username, PasswordHash, FullName, Role, IsActive)
SELECT 'teller1', 'teller123', 'Default Teller', 'Teller', 1
WHERE NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'teller1');
GO

INSERT INTO Users (Username, PasswordHash, FullName, Role, IsActive)
SELECT 'manager1', 'manager123', 'Default Manager', 'Manager', 1
WHERE NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'manager1');
GO

PRINT 'SecureBank database created successfully.';
GO
INSERT INTO Customers (FirstName, LastName, IDNumber, Email, Phone, Address, KYCStatus)
SELECT 'John', 'Smith', '9001015009087', 'john@test.com', '0821234567', '1 Main St', 'Verified'
WHERE NOT EXISTS (SELECT 1 FROM Customers WHERE IDNumber = '9001015009087');

INSERT INTO Accounts (AccountNumber, CustomerID, Type, Balance, Status)
SELECT 'SB-000001', 1, 'Savings', 1000.00, 'Active'
WHERE NOT EXISTS (SELECT 1 FROM Accounts WHERE AccountNumber = 'SB-000001');

PRINT 'Test customer and account created.';