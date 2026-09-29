using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNormalizedCustomerAndLeadNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Customers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MiddleName",
                table: "Customers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Customers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Suffix",
                table: "Customers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Leads",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MiddleName",
                table: "Leads",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Leads",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Suffix",
                table: "Leads",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            // Data Migration: Split legacy single strings on spaces
            // First token -> FirstName, Last token -> LastName, Middle tokens -> MiddleName
            migrationBuilder.Sql(@"
                -- Backfill Customers
                DECLARE @tokC TABLE (idx int, val nvarchar(100));
                DECLARE @cId int, @cName nvarchar(200);
                DECLARE curC CURSOR LOCAL FAST_FORWARD FOR
                    SELECT CustomerId, CustomerName FROM Customers WHERE CustomerName IS NOT NULL AND LTRIM(RTRIM(CustomerName)) <> '' AND (FirstName IS NULL OR FirstName = '');
                OPEN curC;
                FETCH NEXT FROM curC INTO @cId, @cName;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    DELETE FROM @tokC;
                    DECLARE @stepIdxC int = 0;
                    SET @cName = LTRIM(RTRIM(@cName));
                    WHILE LEN(@cName) > 0
                    BEGIN
                        DECLARE @pC int = CHARINDEX(' ', @cName);
                        IF @pC = 0
                        BEGIN
                            SET @stepIdxC = @stepIdxC + 1;
                            INSERT INTO @tokC (idx, val) VALUES (@stepIdxC, LEFT(@cName, 50));
                            BREAK;
                        END
                        ELSE
                        BEGIN
                            DECLARE @wC nvarchar(100) = LTRIM(RTRIM(SUBSTRING(@cName, 1, @pC - 1)));
                            IF LEN(@wC) > 0
                            BEGIN
                                SET @stepIdxC = @stepIdxC + 1;
                                INSERT INTO @tokC (idx, val) VALUES (@stepIdxC, LEFT(@wC, 50));
                            END
                            SET @cName = LTRIM(SUBSTRING(@cName, @pC + 1, LEN(@cName)));
                        END
                    END
                    DECLARE @cntC int = (SELECT COUNT(*) FROM @tokC);
                    DECLARE @fnC nvarchar(50) = '', @mnC nvarchar(50) = NULL, @lnC nvarchar(50) = '';
                    IF @cntC = 1
                    BEGIN
                        SELECT @fnC = val, @lnC = val FROM @tokC WHERE idx = 1;
                    END
                    ELSE IF @cntC = 2
                    BEGIN
                        SELECT @fnC = val FROM @tokC WHERE idx = 1;
                        SELECT @lnC = val FROM @tokC WHERE idx = 2;
                    END
                    ELSE IF @cntC >= 3
                    BEGIN
                        SELECT @fnC = val FROM @tokC WHERE idx = 1;
                        SELECT @lnC = val FROM @tokC WHERE idx = @cntC;
                        SELECT @mnC = STRING_AGG(val, ' ') FROM @tokC WHERE idx > 1 AND idx < @cntC;
                        IF LEN(@mnC) > 50 SET @mnC = LEFT(@mnC, 50);
                    END
                    UPDATE Customers SET FirstName = @fnC, MiddleName = @mnC, LastName = @lnC WHERE CustomerId = @cId;
                    FETCH NEXT FROM curC INTO @cId, @cName;
                END
                CLOSE curC;
                DEALLOCATE curC;

                -- Backfill Leads
                DECLARE @tokL TABLE (idx int, val nvarchar(100));
                DECLARE @lId int, @lName nvarchar(200);
                DECLARE curL CURSOR LOCAL FAST_FORWARD FOR
                    SELECT LeadId, LeadName FROM Leads WHERE LeadName IS NOT NULL AND LTRIM(RTRIM(LeadName)) <> '' AND (FirstName IS NULL OR FirstName = '');
                OPEN curL;
                FETCH NEXT FROM curL INTO @lId, @lName;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    DELETE FROM @tokL;
                    DECLARE @stepIdxL int = 0;
                    SET @lName = LTRIM(RTRIM(@lName));
                    WHILE LEN(@lName) > 0
                    BEGIN
                        DECLARE @pL int = CHARINDEX(' ', @lName);
                        IF @pL = 0
                        BEGIN
                            SET @stepIdxL = @stepIdxL + 1;
                            INSERT INTO @tokL (idx, val) VALUES (@stepIdxL, LEFT(@lName, 50));
                            BREAK;
                        END
                        ELSE
                        BEGIN
                            DECLARE @wL nvarchar(100) = LTRIM(RTRIM(SUBSTRING(@lName, 1, @pL - 1)));
                            IF LEN(@wL) > 0
                            BEGIN
                                SET @stepIdxL = @stepIdxL + 1;
                                INSERT INTO @tokL (idx, val) VALUES (@stepIdxL, LEFT(@wL, 50));
                            END
                            SET @lName = LTRIM(SUBSTRING(@lName, @pL + 1, LEN(@lName)));
                        END
                    END
                    DECLARE @cntL int = (SELECT COUNT(*) FROM @tokL);
                    DECLARE @fnL nvarchar(50) = '', @mnL nvarchar(50) = NULL, @lnL nvarchar(50) = '';
                    IF @cntL = 1
                    BEGIN
                        SELECT @fnL = val, @lnL = val FROM @tokL WHERE idx = 1;
                    END
                    ELSE IF @cntL = 2
                    BEGIN
                        SELECT @fnL = val FROM @tokL WHERE idx = 1;
                        SELECT @lnL = val FROM @tokL WHERE idx = 2;
                    END
                    ELSE IF @cntL >= 3
                    BEGIN
                        SELECT @fnL = val FROM @tokL WHERE idx = 1;
                        SELECT @lnL = val FROM @tokL WHERE idx = @cntL;
                        SELECT @mnL = STRING_AGG(val, ' ') FROM @tokL WHERE idx > 1 AND idx < @cntL;
                        IF LEN(@mnL) > 50 SET @mnL = LEFT(@mnL, 50);
                    END
                    UPDATE Leads SET FirstName = @fnL, MiddleName = @mnL, LastName = @lnL WHERE LeadId = @lId;
                    FETCH NEXT FROM curL INTO @lId, @lName;
                END
                CLOSE curL;
                DEALLOCATE curL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "FirstName", table: "Customers");
            migrationBuilder.DropColumn(name: "MiddleName", table: "Customers");
            migrationBuilder.DropColumn(name: "LastName", table: "Customers");
            migrationBuilder.DropColumn(name: "Suffix", table: "Customers");

            migrationBuilder.DropColumn(name: "FirstName", table: "Leads");
            migrationBuilder.DropColumn(name: "MiddleName", table: "Leads");
            migrationBuilder.DropColumn(name: "LastName", table: "Leads");
            migrationBuilder.DropColumn(name: "Suffix", table: "Leads");
        }
    }
}
