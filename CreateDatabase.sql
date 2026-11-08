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
        PasswordHash NVARCHAR(200) NOT NULL,
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

-- ==========================================
-- INSERT DEFAULT USERS (WITH SHA-256 HASHES)
-- ==========================================

-- Password for admin: admin123
INSERT INTO Users (Username, PasswordHash, FullName, Role, IsActive)
SELECT 'admin', '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', 'System Administrator', 'Admin', 1
WHERE NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'admin');
GO

-- Password for teller1: teller123
INSERT INTO Users (Username, PasswordHash, FullName, Role, IsActive)
SELECT 'teller1', '8c690ca2a3d255f2728a0103d817d8fb6a4b04e33e83a5996a5e9d78997c0be0', 'Default Teller', 'Teller', 1
WHERE NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'teller1');
GO

-- Password for manager1: manager123
INSERT INTO Users (Username, PasswordHash, FullName, Role, IsActive)
SELECT 'manager1', 'b26f6dcf2a6ed0ff5f85ee7a1a21e4bb56cc15ee06c94d2f478ea67b80ec9e2b', 'Default Manager', 'Manager', 1
WHERE NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'manager1');
GO

PRINT 'SecureBank database created successfully with hashed passwords.';
GO