using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFinances.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexMarketData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropIndex(
            //    name: "IX_stocks_data_asset_id",
            //    table: "stocks_data");

            migrationBuilder.CreateIndex(
                name: "IX_stocks_data_asset_id_Date",
                table: "stocks_data",
                columns: new[] { "asset_id", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropIndex(
            //    name: "IX_stocks_data_asset_id_Date",
            //    table: "stocks_data");

            migrationBuilder.CreateIndex(
                name: "IX_stocks_data_asset_id",
                table: "stocks_data",
                column: "asset_id");
        }
    }
}
