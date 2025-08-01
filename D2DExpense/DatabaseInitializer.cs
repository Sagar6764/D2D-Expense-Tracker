using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

public class DatabaseInitializer
{
    private readonly string _appDbConnection;

    public DatabaseInitializer(IConfiguration configuration)
    {
        // This should already point to your Azure SQL DB (D2DExpenseDB)
        _appDbConnection = configuration.GetConnectionString("DefaultConnection");
    }

    public void EnsureTablesExist()
    {
        using (var connection = new SqlConnection(_appDbConnection))
        {
            connection.Open();
            var cmdText = @"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users')
                BEGIN
                    CREATE TABLE Users (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        Name NVARCHAR(100),
                        Email NVARCHAR(100),
                        Password NVARCHAR(256),
                        MonthlyIncome DECIMAL(10,2),
                        Age INT,
                        MobileNumber NVARCHAR(20),
                        ProfilePicture NVARCHAR(MAX)
                    );
                END;

                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SIPDetails')
BEGIN
    CREATE TABLE SIPDetails (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Email NVARCHAR(100),
        SIPName NVARCHAR(100),
        MonthlyAmount DECIMAL(10,2),
        StartDate DATE,
        SchemeCode NVARCHAR(10) -- 👈 Added this line
    );
END
ELSE IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'SIPDetails' AND COLUMN_NAME = 'SchemeCode'
)
BEGIN
    ALTER TABLE SIPDetails ADD SchemeCode NVARCHAR(10) DEFAULT('');
END;


                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SIPTransactions')
                BEGIN
                    CREATE TABLE SIPTransactions (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        SIPId INT FOREIGN KEY REFERENCES SIPDetails(Id),
                        NAVDate DATE,
                        NAV DECIMAL(10,4),
                        Units DECIMAL(18,4)
                    );
                END;

                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Expenses')
                BEGIN
                    CREATE TABLE Expenses (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        Email NVARCHAR(100),
                        Date DATE,
                        Month NVARCHAR(50),
                        Type NVARCHAR(50),
                        Category NVARCHAR(100),
                        Amount DECIMAL(10,2)
                    );
                END;
            ";
            using (var command = new SqlCommand(cmdText, connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }
}
