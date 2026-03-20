using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestAI.Infrastructure.Persistence.Migrations
{
    public partial class Release5CashAndAccounts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CashMovementId",
                table: "SupplierAccountMovements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                table: "SupplierAccountMovements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CashRegisters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashRegisters", x => x.Id);
                    table.ForeignKey(name: "FK_CashRegisters_Accounts_AccountId", column: x => x.AccountId, principalTable: "Accounts", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(name: "FK_CashRegisters_Branches_BranchId", column: x => x.BranchId, principalTable: "Branches", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CashSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    CashRegisterId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OpenedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OpenedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ClosingBalanceExpected = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ClosingBalanceDeclared = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashSessions", x => x.Id);
                    table.ForeignKey(name: "FK_CashSessions_Accounts_AccountId", column: x => x.AccountId, principalTable: "Accounts", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(name: "FK_CashSessions_CashRegisters_CashRegisterId", column: x => x.CashRegisterId, principalTable: "CashRegisters", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CashMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    CashRegisterId = table.Column<int>(type: "int", nullable: false),
                    CashSessionId = table.Column<int>(type: "int", nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    OriginType = table.Column<int>(type: "int", nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    SupplierId = table.Column<int>(type: "int", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Concept = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Observations = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashMovements", x => x.Id);
                    table.ForeignKey(name: "FK_CashMovements_Accounts_AccountId", column: x => x.AccountId, principalTable: "Accounts", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(name: "FK_CashMovements_CashRegisters_CashRegisterId", column: x => x.CashRegisterId, principalTable: "CashRegisters", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_CashMovements_CashSessions_CashSessionId", column: x => x.CashSessionId, principalTable: "CashSessions", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(name: "FK_CashMovements_Customers_CustomerId", column: x => x.CustomerId, principalTable: "Customers", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_CashMovements_Suppliers_SupplierId", column: x => x.SupplierId, principalTable: "Suppliers", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerAccountMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    MovementType = table.Column<int>(type: "int", nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    SaleId = table.Column<int>(type: "int", nullable: true),
                    CashMovementId = table.Column<int>(type: "int", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IssuedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DebitAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreditAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAccountMovements", x => x.Id);
                    table.ForeignKey(name: "FK_CustomerAccountMovements_Accounts_AccountId", column: x => x.AccountId, principalTable: "Accounts", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(name: "FK_CustomerAccountMovements_CashMovements_CashMovementId", column: x => x.CashMovementId, principalTable: "CashMovements", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_CustomerAccountMovements_Customers_CustomerId", column: x => x.CustomerId, principalTable: "Customers", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_CustomerAccountMovements_Sales_SaleId", column: x => x.SaleId, principalTable: "Sales", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerAccountAllocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    SourceMovementId = table.Column<int>(type: "int", nullable: false),
                    TargetMovementId = table.Column<int>(type: "int", nullable: false),
                    AppliedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAccountAllocations", x => x.Id);
                    table.ForeignKey(name: "FK_CustomerAccountAllocations_Accounts_AccountId", column: x => x.AccountId, principalTable: "Accounts", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(name: "FK_CustomerAccountAllocations_CustomerAccountMovements_SourceMovementId", column: x => x.SourceMovementId, principalTable: "CustomerAccountMovements", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_CustomerAccountAllocations_CustomerAccountMovements_TargetMovementId", column: x => x.TargetMovementId, principalTable: "CustomerAccountMovements", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierAccountAllocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    SourceMovementId = table.Column<int>(type: "int", nullable: false),
                    TargetMovementId = table.Column<int>(type: "int", nullable: false),
                    AppliedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierAccountAllocations", x => x.Id);
                    table.ForeignKey(name: "FK_SupplierAccountAllocations_Accounts_AccountId", column: x => x.AccountId, principalTable: "Accounts", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(name: "FK_SupplierAccountAllocations_SupplierAccountMovements_SourceMovementId", column: x => x.SourceMovementId, principalTable: "SupplierAccountMovements", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_SupplierAccountAllocations_SupplierAccountMovements_TargetMovementId", column: x => x.TargetMovementId, principalTable: "SupplierAccountMovements", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(name: "IX_SupplierAccountMovements_CashMovementId", table: "SupplierAccountMovements", column: "CashMovementId", unique: true, filter: "[CashMovementId] IS NOT NULL");
            migrationBuilder.AddForeignKey(name: "FK_SupplierAccountMovements_CashMovements_CashMovementId", table: "SupplierAccountMovements", column: "CashMovementId", principalTable: "CashMovements", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.CreateIndex(name: "IX_CashRegisters_AccountId_Code", table: "CashRegisters", columns: new[] { "AccountId", "Code" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_CashRegisters_BranchId", table: "CashRegisters", column: "BranchId");
            migrationBuilder.CreateIndex(name: "IX_CashSessions_AccountId_CashRegisterId_Status", table: "CashSessions", columns: new[] { "AccountId", "CashRegisterId", "Status" });
            migrationBuilder.CreateIndex(name: "IX_CashSessions_CashRegisterId", table: "CashSessions", column: "CashRegisterId");
            migrationBuilder.CreateIndex(name: "IX_CashMovements_AccountId_CashRegisterId_OccurredAtUtc", table: "CashMovements", columns: new[] { "AccountId", "CashRegisterId", "OccurredAtUtc" });
            migrationBuilder.CreateIndex(name: "IX_CashMovements_CashRegisterId", table: "CashMovements", column: "CashRegisterId");
            migrationBuilder.CreateIndex(name: "IX_CashMovements_CashSessionId", table: "CashMovements", column: "CashSessionId");
            migrationBuilder.CreateIndex(name: "IX_CashMovements_CustomerId", table: "CashMovements", column: "CustomerId");
            migrationBuilder.CreateIndex(name: "IX_CashMovements_SupplierId", table: "CashMovements", column: "SupplierId");
            migrationBuilder.CreateIndex(name: "IX_CustomerAccountMovements_AccountId_CashMovementId", table: "CustomerAccountMovements", columns: new[] { "AccountId", "CashMovementId" }, unique: true, filter: "[CashMovementId] IS NOT NULL");
            migrationBuilder.CreateIndex(name: "IX_CustomerAccountMovements_AccountId_CustomerId_IssuedAtUtc", table: "CustomerAccountMovements", columns: new[] { "AccountId", "CustomerId", "IssuedAtUtc" });
            migrationBuilder.CreateIndex(name: "IX_CustomerAccountMovements_AccountId_SaleId", table: "CustomerAccountMovements", columns: new[] { "AccountId", "SaleId" }, unique: true, filter: "[SaleId] IS NOT NULL");
            migrationBuilder.CreateIndex(name: "IX_CustomerAccountMovements_CashMovementId", table: "CustomerAccountMovements", column: "CashMovementId", unique: true, filter: "[CashMovementId] IS NOT NULL");
            migrationBuilder.CreateIndex(name: "IX_CustomerAccountMovements_CustomerId", table: "CustomerAccountMovements", column: "CustomerId");
            migrationBuilder.CreateIndex(name: "IX_CustomerAccountMovements_SaleId", table: "CustomerAccountMovements", column: "SaleId", unique: true, filter: "[SaleId] IS NOT NULL");
            migrationBuilder.CreateIndex(name: "IX_CustomerAccountAllocations_AccountId_SourceMovementId_TargetMovementId", table: "CustomerAccountAllocations", columns: new[] { "AccountId", "SourceMovementId", "TargetMovementId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_CustomerAccountAllocations_SourceMovementId", table: "CustomerAccountAllocations", column: "SourceMovementId");
            migrationBuilder.CreateIndex(name: "IX_CustomerAccountAllocations_TargetMovementId", table: "CustomerAccountAllocations", column: "TargetMovementId");
            migrationBuilder.CreateIndex(name: "IX_SupplierAccountAllocations_AccountId_SourceMovementId_TargetMovementId", table: "SupplierAccountAllocations", columns: new[] { "AccountId", "SourceMovementId", "TargetMovementId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_SupplierAccountAllocations_SourceMovementId", table: "SupplierAccountAllocations", column: "SourceMovementId");
            migrationBuilder.CreateIndex(name: "IX_SupplierAccountAllocations_TargetMovementId", table: "SupplierAccountAllocations", column: "TargetMovementId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_SupplierAccountMovements_CashMovements_CashMovementId", table: "SupplierAccountMovements");
            migrationBuilder.DropTable(name: "CustomerAccountAllocations");
            migrationBuilder.DropTable(name: "SupplierAccountAllocations");
            migrationBuilder.DropTable(name: "CustomerAccountMovements");
            migrationBuilder.DropTable(name: "CashMovements");
            migrationBuilder.DropTable(name: "CashSessions");
            migrationBuilder.DropTable(name: "CashRegisters");
            migrationBuilder.DropIndex(name: "IX_SupplierAccountMovements_CashMovementId", table: "SupplierAccountMovements");
            migrationBuilder.DropColumn(name: "CashMovementId", table: "SupplierAccountMovements");
            migrationBuilder.DropColumn(name: "PaymentMethod", table: "SupplierAccountMovements");
        }
    }
}
