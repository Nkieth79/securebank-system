IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'SecureBank')
BEGIN
    CREATE DATABASE SecureBank;
END
GO

USE SecureBank;
GO

-- ==========================================
-- CREATE TABLES (IF THEY DON'T EXIST)
-- ==========================================

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
-- INSERT USERS (ONLY IF EMPTY)
-- ==========================================
IF NOT EXISTS (SELECT 1 FROM Users)
BEGIN
    INSERT INTO Users (Username, PasswordHash, FullName, Role, IsActive) VALUES 
    ('admin', '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', 'System Administrator', 'Admin', 1),
    ('teller1', 'd277f25b136e402488431a226b652b2bff919188028af250d9dab860e2c436fd', 'Default Teller', 'Teller', 1),
    ('manager1', '866485796cfa8d7c0cf7111640205b83076433547577511d81f8030ae99ecea5', 'Default Manager', 'Manager', 1);
END
GO

-- ==========================================
-- INSERT SEED DATA (ONLY IF EMPTY)
-- ==========================================
IF NOT EXISTS (SELECT 1 FROM Customers)
BEGIN
    -- 1. Insert 10 Customers
    INSERT INTO Customers (FirstName, LastName, IDNumber, Email, Phone, Address, KYCStatus) VALUES
    ('John', 'Smith', '9001015009087', 'john@test.com', '0821111111', '1 Main St', 'Verified'),
    ('Jane', 'Doe', '9102025009088', 'jane@test.com', '0822222222', '2 Main St', 'Verified'),
    ('Michael', 'Johnson', '9203035009089', 'michael@test.com', '0823333333', '3 Main St', 'Verified'),
    ('Sarah', 'Williams', '9304045009090', 'sarah@test.com', '0824444444', '4 Main St', 'Verified'),
    ('David', 'Brown', '9405055009091', 'david@test.com', '0825555555', '5 Main St', 'Verified'),
    ('Emily', 'Jones', '9506065009092', 'emily@test.com', '0826666666', '6 Main St', 'Verified'),
    ('James', 'Garcia', '9607075009093', 'james@test.com', '0827777777', '7 Main St', 'Verified'),
    ('Jessica', 'Miller', '9708085009094', 'jessica@test.com', '0828888888', '8 Main St', 'Verified'),
    ('Robert', 'Davis', '9809095009095', 'robert@test.com', '0829999999', '9 Main St', 'Verified'),
    ('Ashley', 'Rodriguez', '9910105009096', 'ashley@test.com', '0831111111', '10 Main St', 'Verified');

    -- 2. Insert Accounts (2 per customer)
    INSERT INTO Accounts (AccountNumber, CustomerID, Type, Balance, Status) VALUES
    ('SB-100000001', 1, 'Savings', 5000.00, 'Active'),
    ('SB-100000002', 1, 'Cheque', 1200.00, 'Active'),
    ('SB-100000003', 2, 'Savings', 7500.00, 'Active'),
    ('SB-100000004', 2, 'Credit', 0.00, 'Active'),
    ('SB-100000005', 3, 'Savings', 250.00, 'Active'),
    ('SB-100000006', 4, 'Cheque', 15000.00, 'Active'),
    ('SB-100000007', 5, 'Savings', 3200.00, 'Active'),
    ('SB-100000008', 6, 'Savings', 800.00, 'Active'),
    ('SB-100000009', 7, 'Savings', 12500.00, 'Active'),
    ('SB-100000010', 8, 'Cheque', 50.00, 'Active');

    -- 3. Insert Pending Loans
    INSERT INTO Loans (CustomerID, PrincipalAmount, InterestRate, TermMonths, MonthlyPayment, RemainingBalance, Status) VALUES
    (1, 10000.00, 0.10, 12, 916.67, 11000.00, 'Pending'),
    (3, 5000.00, 0.08, 6, 866.67, 5200.00, 'Pending'),
    (5, 20000.00, 0.12, 24, 1033.33, 22400.00, 'Pending'),
    (7, 15000.00, 0.09, 12, 1375.00, 16350.00, 'Pending');
END
GO

PRINT 'SecureBank database setup complete.';
GO