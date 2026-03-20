using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestAI.Infrastructure.Persistence.Migrations
{
    [Migration("20260320130000_Release2InventoryAndPricing")]
    public partial class Release2InventoryAndPricing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PriceLists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    BaseMode = table.Column<int>(type: "int", nullable: false),
                    TargetType = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceLists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceLists_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductWarehouseStocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductVariantId = table.Column<int>(type: "int", nullable: true),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    QuantityOnHand = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LastMovementAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductWarehouseStocks", x => x.Id);
                    table.ForeignKey("FK_ProductWarehouseStocks_Accounts_AccountId", x => x.AccountId, "Accounts", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_ProductWarehouseStocks_Products_ProductId", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_ProductWarehouseStocks_ProductVariants_ProductVariantId", x => x.ProductVariantId, "ProductVariants", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_ProductWarehouseStocks_Warehouses_WarehouseId", x => x.WarehouseId, "Warehouses", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductVariantId = table.Column<int>(type: "int", nullable: true),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    CounterpartWarehouseId = table.Column<int>(type: "int", nullable: true),
                    MovementType = table.Column<int>(type: "int", nullable: false),
                    QuantityDelta = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReferenceGroup = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.ForeignKey("FK_StockMovements_Accounts_AccountId", x => x.AccountId, "Accounts", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_StockMovements_Products_ProductId", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_StockMovements_ProductVariants_ProductVariantId", x => x.ProductVariantId, "ProductVariants", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_StockMovements_Warehouses_CounterpartWarehouseId", x => x.CounterpartWarehouseId, "Warehouses", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_StockMovements_Warehouses_WarehouseId", x => x.WarehouseId, "Warehouses", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PriceListItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    PriceListId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductVariantId = table.Column<int>(type: "int", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceListItems", x => x.Id);
                    table.ForeignKey("FK_PriceListItems_Accounts_AccountId", x => x.AccountId, "Accounts", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_PriceListItems_PriceLists_PriceListId", x => x.PriceListId, "PriceLists", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_PriceListItems_Products_ProductId", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_PriceListItems_ProductVariants_ProductVariantId", x => x.ProductVariantId, "ProductVariants", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(name: "IX_PriceLists_AccountId", table: "PriceLists", column: "AccountId");
            migrationBuilder.CreateIndex(name: "IX_PriceLists_AccountId_Name", table: "PriceLists", columns: new[] { "AccountId", "Name" }, unique: true);

            migrationBuilder.CreateIndex(name: "IX_ProductWarehouseStocks_AccountId", table: "ProductWarehouseStocks", column: "AccountId");
            migrationBuilder.CreateIndex(name: "IX_ProductWarehouseStocks_ProductId", table: "ProductWarehouseStocks", column: "ProductId");
            migrationBuilder.CreateIndex(name: "IX_ProductWarehouseStocks_ProductVariantId", table: "ProductWarehouseStocks", column: "ProductVariantId");
            migrationBuilder.CreateIndex(name: "IX_ProductWarehouseStocks_WarehouseId", table: "ProductWarehouseStocks", column: "WarehouseId");
            migrationBuilder.CreateIndex(name: "IX_ProductWarehouseStocks_AccountId_WarehouseId_ProductId", table: "ProductWarehouseStocks", columns: new[] { "AccountId", "WarehouseId", "ProductId" }, unique: true, filter: "[ProductVariantId] IS NULL");
            migrationBuilder.CreateIndex(name: "IX_ProductWarehouseStocks_AccountId_WarehouseId_ProductVariantId", table: "ProductWarehouseStocks", columns: new[] { "AccountId", "WarehouseId", "ProductVariantId" }, unique: true, filter: "[ProductVariantId] IS NOT NULL");

            migrationBuilder.CreateIndex(name: "IX_StockMovements_AccountId", table: "StockMovements", column: "AccountId");
            migrationBuilder.CreateIndex(name: "IX_StockMovements_ProductId", table: "StockMovements", column: "ProductId");
            migrationBuilder.CreateIndex(name: "IX_StockMovements_ProductVariantId", table: "StockMovements", column: "ProductVariantId");
            migrationBuilder.CreateIndex(name: "IX_StockMovements_WarehouseId", table: "StockMovements", column: "WarehouseId");
            migrationBuilder.CreateIndex(name: "IX_StockMovements_CounterpartWarehouseId", table: "StockMovements", column: "CounterpartWarehouseId");
            migrationBuilder.CreateIndex(name: "IX_StockMovements_AccountId_OccurredAtUtc", table: "StockMovements", columns: new[] { "AccountId", "OccurredAtUtc" });
            migrationBuilder.CreateIndex(name: "IX_StockMovements_AccountId_ProductId_ProductVariantId_WarehouseId_OccurredAtUtc", table: "StockMovements", columns: new[] { "AccountId", "ProductId", "ProductVariantId", "WarehouseId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(name: "IX_PriceListItems_AccountId", table: "PriceListItems", column: "AccountId");
            migrationBuilder.CreateIndex(name: "IX_PriceListItems_PriceListId", table: "PriceListItems", column: "PriceListId");
            migrationBuilder.CreateIndex(name: "IX_PriceListItems_ProductId", table: "PriceListItems", column: "ProductId");
            migrationBuilder.CreateIndex(name: "IX_PriceListItems_ProductVariantId", table: "PriceListItems", column: "ProductVariantId");
            migrationBuilder.CreateIndex(name: "IX_PriceListItems_PriceListId_ProductId", table: "PriceListItems", columns: new[] { "PriceListId", "ProductId" }, unique: true, filter: "[ProductVariantId] IS NULL");
            migrationBuilder.CreateIndex(name: "IX_PriceListItems_PriceListId_ProductVariantId", table: "PriceListItems", columns: new[] { "PriceListId", "ProductVariantId" }, unique: true, filter: "[ProductVariantId] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PriceListItems");
            migrationBuilder.DropTable(name: "ProductWarehouseStocks");
            migrationBuilder.DropTable(name: "StockMovements");
            migrationBuilder.DropTable(name: "PriceLists");
        }
    }
}
