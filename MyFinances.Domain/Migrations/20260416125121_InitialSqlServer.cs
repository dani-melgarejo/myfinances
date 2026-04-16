using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MyFinances.Domain.Migrations
{
    /// <inheritdoc />
    public partial class InitialSqlServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "currencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false),
                    Name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Symbol = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_currencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "assets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ticker = table.Column<string>(type: "varchar(10)", nullable: false),
                    currency_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_assets_currencies_currency_id",
                        column: x => x.currency_id,
                        principalTable: "currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "currency_exchange_rates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    from_currency_id = table.Column<int>(type: "int", nullable: false),
                    to_currency_id = table.Column<int>(type: "int", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsLatest = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Source = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_currency_exchange_rates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_currency_exchange_rates_currencies_from_currency_id",
                        column: x => x.from_currency_id,
                        principalTable: "currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_currency_exchange_rates_currencies_to_currency_id",
                        column: x => x.to_currency_id,
                        principalTable: "currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "movements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    asset_id = table.Column<int>(type: "int", nullable: false),
                    Operation = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(10,3)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    currency_id = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_movements_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_movements_assets_asset_id",
                        column: x => x.asset_id,
                        principalTable: "assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_movements_currencies_currency_id",
                        column: x => x.currency_id,
                        principalTable: "currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "possessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    asset_id = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    totalprice = table.Column<decimal>(type: "decimal(10,3)", nullable: false),
                    Worth = table.Column<decimal>(type: "decimal(12,5)", nullable: false),
                    currency_id = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_possessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_possessions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_possessions_assets_asset_id",
                        column: x => x.asset_id,
                        principalTable: "assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_possessions_currencies_currency_id",
                        column: x => x.currency_id,
                        principalTable: "currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "stocks_data",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    asset_id = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Close = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    currency_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stocks_data", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stocks_data_assets_asset_id",
                        column: x => x.asset_id,
                        principalTable: "assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_stocks_data_currencies_currency_id",
                        column: x => x.currency_id,
                        principalTable: "currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "currencies",
                columns: new[] { "Id", "Code", "CreatedAt", "IsActive", "IsDefault", "Name", "Symbol", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "USD", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, true, "US Dollar", "$", null },
                    { 2, "EUR", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, "Euro", "€", null },
                    { 3, "GBP", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, "British Pound Sterling", "£", null },
                    { 4, "JPY", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, "Japanese Yen", "¥", null },
                    { 5, "CAD", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, "Canadian Dollar", "C$", null },
                    { 6, "AUD", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, "Australian Dollar", "A$", null },
                    { 7, "CHF", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, "Swiss Franc", "CHF", null },
                    { 8, "CNY", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, "Chinese Yuan", "¥", null },
                    { 9, "ARS", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, "Argentine Peso", "$", null },
                    { 10, "BRL", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, "Brazilian Real", "R$", null },
                    { 11, "MXN", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, "Mexican Peso", "$", null },
                    { 12, "CLP", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, "Chilean Peso", "$", null },
                    { 13, "COP", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, "Colombian Peso", "$", null },
                    { 14, "PEN", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, "Peruvian Sol", "S/", null },
                    { 15, "UYU", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, "Uruguayan Peso", "$U", null },
                    { 16, "INR", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, "Indian Rupee", "₹", null },
                    { 17, "KRW", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, "South Korean Won", "₩", null },
                    { 18, "SGD", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, "Singapore Dollar", "S$", null },
                    { 19, "HKD", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, "Hong Kong Dollar", "HK$", null },
                    { 20, "NZD", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, "New Zealand Dollar", "NZ$", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_assets_currency_id",
                table: "assets",
                column: "currency_id");

            migrationBuilder.CreateIndex(
                name: "IX_currencies_Code",
                table: "currencies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_currencies_IsDefault",
                table: "currencies",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_currency_exchange_rates_Date_IsLatest",
                table: "currency_exchange_rates",
                columns: new[] { "Date", "IsLatest" });

            migrationBuilder.CreateIndex(
                name: "IX_currency_exchange_rates_from_currency_id_to_currency_id_IsLatest",
                table: "currency_exchange_rates",
                columns: new[] { "from_currency_id", "to_currency_id", "IsLatest" },
                unique: true,
                filter: "IsLatest = 1");

            migrationBuilder.CreateIndex(
                name: "IX_currency_exchange_rates_to_currency_id",
                table: "currency_exchange_rates",
                column: "to_currency_id");

            migrationBuilder.CreateIndex(
                name: "IX_movements_asset_id",
                table: "movements",
                column: "asset_id");

            migrationBuilder.CreateIndex(
                name: "IX_movements_currency_id",
                table: "movements",
                column: "currency_id");

            migrationBuilder.CreateIndex(
                name: "IX_movements_UserId",
                table: "movements",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_possessions_asset_id",
                table: "possessions",
                column: "asset_id");

            migrationBuilder.CreateIndex(
                name: "IX_possessions_currency_id",
                table: "possessions",
                column: "currency_id");

            migrationBuilder.CreateIndex(
                name: "IX_possessions_UserId",
                table: "possessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_stocks_data_asset_id_Date",
                table: "stocks_data",
                columns: new[] { "asset_id", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stocks_data_currency_id",
                table: "stocks_data",
                column: "currency_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "currency_exchange_rates");

            migrationBuilder.DropTable(
                name: "movements");

            migrationBuilder.DropTable(
                name: "possessions");

            migrationBuilder.DropTable(
                name: "stocks_data");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "assets");

            migrationBuilder.DropTable(
                name: "currencies");
        }
    }
}
