using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace App.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLeadPricingAndBookingStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. QuotedPrice on ServiceRequests
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ServiceRequests') AND name = 'QuotedPrice')
                BEGIN
                    ALTER TABLE [ServiceRequests] ADD [QuotedPrice] decimal(18,2) NULL;
                END
            ");

            // 2. LostReason on Leads
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Leads') AND name = 'LostReason')
                BEGIN
                    ALTER TABLE [Leads] ADD [LostReason] nvarchar(500) NULL;
                END
            ");

            // 3. QuotedPrice on Leads
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Leads') AND name = 'QuotedPrice')
                BEGIN
                    ALTER TABLE [Leads] ADD [QuotedPrice] decimal(18,2) NULL;
                END
            ");

            // 4. ServiceAddress on Leads
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Leads') AND name = 'ServiceAddress')
                BEGIN
                    ALTER TABLE [Leads] ADD [ServiceAddress] nvarchar(300) NULL;
                END
            ");

            // 5. Status on Leads
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Leads') AND name = 'Status')
                BEGIN
                    ALTER TABLE [Leads] ADD [Status] nvarchar(20) NOT NULL DEFAULT 'New';
                END
            ");

            // 6. Email on Customers
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Customers') AND name = 'Email')
                BEGIN
                    ALTER TABLE [Customers] ADD [Email] nvarchar(150) NULL;
                END
            ");

            // 7. Users table
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
                BEGIN
                    CREATE TABLE [Users] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [Username] nvarchar(100) NOT NULL,
                        [PasswordHash] nvarchar(200) NOT NULL,
                        [Role] nvarchar(50) NOT NULL,
                        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
                    );
                END
            ");

            // 8. Unique index on Customers.ContactDetails
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Customers_ContactDetails' AND object_id = OBJECT_ID('Customers'))
                BEGIN
                    CREATE UNIQUE INDEX [IX_Customers_ContactDetails] ON [Customers] ([ContactDetails]);
                END
            ");

            // 9. Data Migration: transition legacy 'Pending' bookings to 'Scheduled'
            migrationBuilder.Sql("UPDATE [ServiceRequests] SET [Status] = 'Scheduled' WHERE [Status] = 'Pending';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Customers_ContactDetails",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "QuotedPrice",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "LostReason",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "QuotedPrice",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "ServiceAddress",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Customers");
        }
    }
}
