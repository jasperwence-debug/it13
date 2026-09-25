using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLeadConversionTraceability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Leads') AND name = 'ConvertedAt')
                BEGIN
                    ALTER TABLE [Leads] ADD [ConvertedAt] datetime2 NULL;
                END

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Leads') AND name = 'ConvertedCustomerId')
                BEGIN
                    ALTER TABLE [Leads] ADD [ConvertedCustomerId] int NULL;
                END

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Customers') AND name = 'LeadId')
                BEGIN
                    ALTER TABLE [Customers] ADD [LeadId] int NULL;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConvertedAt",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "ConvertedCustomerId",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "LeadId",
                table: "Customers");
        }
    }
}
