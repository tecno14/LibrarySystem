-- =================================================================
-- LibraryDB SQL SCRIPT
-- =================================================================

-- Create the database if it does not already exist
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'LibraryDB')
BEGIN
    CREATE DATABASE LibraryDB;
END
GO

-- Set the context to the LibraryDB database for all subsequent commands
USE LibraryDB;
GO

-- Drop existing tables in reverse order of dependency to ensure a clean slate
IF OBJECT_ID('dbo.Borrowings', 'U') IS NOT NULL
    DROP TABLE dbo.Borrowings;
GO
IF OBJECT_ID('dbo.BookCopies', 'U') IS NOT NULL
    DROP TABLE dbo.BookCopies;
GO
IF OBJECT_ID('dbo.Books', 'U') IS NOT NULL
    DROP TABLE dbo.Books;
GO
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL
    DROP TABLE dbo.Users;
GO

-- =================================================================
-- TABLE 1: Users
-- Stores user account information and roles for authorization.
-- =================================================================
CREATE TABLE dbo.Users (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(100) COLLATE Latin1_General_CI_AS NOT NULL CHECK (
        Username NOT LIKE '%[^A-Za-z0-9_]%' AND LEN(Username) BETWEEN 3 AND 100
    ),
    PasswordHash NVARCHAR(255) NOT NULL, -- Always store hashed passwords
    FullName NVARCHAR(200) NOT NULL,
    Role NVARCHAR(50) NOT NULL CHECK (Role IN ('Manager', 'Member')) DEFAULT 'Member',
    IsHidden BIT NOT NULL DEFAULT 0, -- For soft deletes/archiving
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

-- Add a unique index to prevent duplicate usernames
CREATE UNIQUE INDEX IX_Users_Username ON dbo.Users(Username);
GO

-- =================================================================
-- TABLE 2: Books (Book Titles)
-- Represents the abstract book's metadata (one row per unique ISBN).
-- =================================================================
CREATE TABLE dbo.Books (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ISBN NVARCHAR(20) NOT NULL,
    Title NVARCHAR(255) NOT NULL,
    Author NVARCHAR(255) NOT NULL,
    Publisher NVARCHAR(150) NULL,
    PublicationYear INT NULL,
    Genre NVARCHAR(100) NULL,
    CoverImageUrl NVARCHAR(500) NULL,
    IsHidden BIT NOT NULL DEFAULT 0, -- For soft deletes/archiving
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

-- Add a unique index on ISBN to enforce it as the book title's unique identifier.
CREATE UNIQUE INDEX IX_Books_ISBN ON dbo.Books(ISBN);
GO
-- Add an index on Genre for fast category-based searching.
CREATE INDEX IX_Books_Genre ON dbo.Books(Genre);
GO

-- =================================================================
-- TABLE 3: BookCopies
-- Represents each individual, physical copy of a book that can be borrowed.
-- =================================================================
CREATE TABLE dbo.BookCopies (
    Id INT PRIMARY KEY IDENTITY(1,1),
    -- Foreign key links to the book's metadata in the Books table
    ISBN NVARCHAR(20) NOT NULL,
    -- Status of this specific copy
    Status INT NOT NULL
        DEFAULT 0,
    Location NVARCHAR(100) NULL, -- e.g., "Shelf 4B", "Branch Library"
    AddedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    -- Define the foreign key relationship
    CONSTRAINT FK_BookCopies_Books FOREIGN KEY (ISBN) REFERENCES dbo.Books(ISBN)
        ON DELETE CASCADE -- If a book title is deleted, all its copies are also deleted.
);
GO

-- Add an index on the foreign key for performance.
CREATE INDEX IX_BookCopies_ISBN ON dbo.BookCopies(ISBN);
GO

-- =================================================================
-- TABLE 4: Borrowings
-- Tracks the history of who borrowed which specific copy and when.
-- =================================================================
CREATE TABLE dbo.Borrowings (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    CopyId INT NOT NULL, -- Links to a specific physical copy
    BorrowDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    DueDate DATETIME2 NOT NULL,
    ReturnDate DATETIME2 NULL, -- A NULL value means the copy is still checked out

    -- Define foreign key relationships
    CONSTRAINT FK_Borrowings_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_Borrowings_BookCopies FOREIGN KEY (CopyId) REFERENCES dbo.BookCopies(Id)
);
GO

-- Add indexes on foreign keys for performance.
CREATE INDEX IX_Borrowings_UserId ON dbo.Borrowings(UserId);
CREATE INDEX IX_Borrowings_CopyId ON dbo.Borrowings(CopyId);
GO

PRINT 'Database schema created successfully.';