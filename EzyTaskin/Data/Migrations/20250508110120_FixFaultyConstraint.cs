using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EzyTaskin.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixFaultyConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Offers_Requests_Id",
                table: "Offers");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Offers_OfferId",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_OfferId",
                table: "Requests");

            migrationBuilder.RenameColumn(
                name: "OfferId",
                table: "Requests",
                newName: "SelectedId");

            migrationBuilder.AddColumn<Guid>(
                name: "RequestId",
                table: "Offers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Requests_SelectedId",
                table: "Requests",
                column: "SelectedId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_RequestId",
                table: "Offers",
                column: "RequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_Requests_RequestId",
                table: "Offers",
                column: "RequestId",
                principalTable: "Requests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Offers_SelectedId",
                table: "Requests",
                column: "SelectedId",
                principalTable: "Offers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Offers_Requests_RequestId",
                table: "Offers");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Offers_SelectedId",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_SelectedId",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Offers_RequestId",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "Offers");

            migrationBuilder.RenameColumn(
                name: "SelectedId",
                table: "Requests",
                newName: "OfferId");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_OfferId",
                table: "Requests",
                column: "OfferId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_Requests_Id",
                table: "Offers",
                column: "Id",
                principalTable: "Requests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Offers_OfferId",
                table: "Requests",
                column: "OfferId",
                principalTable: "Offers",
                principalColumn: "Id");
        }
    }
}
