using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EzyTaskin.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentCommandAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "PaymentCommands",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Amount",
                table: "PaymentCommands");
        }
    }
}
