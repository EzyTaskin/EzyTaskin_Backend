using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EzyTaskin.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Consumers_AccountId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Providers_AccountId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Offers_Requests_OfferId",
                table: "Offers");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Reviews_RequestId",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Offers_OfferId",
                table: "Offers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_AccountId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "OfferId",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "RequestId",
                table: "Requests",
                newName: "OfferId");

            migrationBuilder.RenameIndex(
                name: "IX_Requests_RequestId",
                table: "Requests",
                newName: "IX_Requests_OfferId");

            migrationBuilder.AddColumn<Guid>(
                name: "RequestId",
                table: "Reviews",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "AccountId",
                table: "Providers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AccountId",
                table: "Consumers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_RequestId",
                table: "Reviews",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Providers_AccountId",
                table: "Providers",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Consumers_AccountId",
                table: "Consumers",
                column: "AccountId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Consumers_AspNetUsers_AccountId",
                table: "Consumers",
                column: "AccountId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Providers_AspNetUsers_AccountId",
                table: "Providers",
                column: "AccountId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Offers_OfferId",
                table: "Requests",
                column: "OfferId",
                principalTable: "Offers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Requests_RequestId",
                table: "Reviews",
                column: "RequestId",
                principalTable: "Requests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Consumers_AspNetUsers_AccountId",
                table: "Consumers");

            migrationBuilder.DropForeignKey(
                name: "FK_Providers_AspNetUsers_AccountId",
                table: "Providers");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Offers_OfferId",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Requests_RequestId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_RequestId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Providers_AccountId",
                table: "Providers");

            migrationBuilder.DropIndex(
                name: "IX_Consumers_AccountId",
                table: "Consumers");

            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "Consumers");

            migrationBuilder.RenameColumn(
                name: "OfferId",
                table: "Requests",
                newName: "RequestId");

            migrationBuilder.RenameIndex(
                name: "IX_Requests_OfferId",
                table: "Requests",
                newName: "IX_Requests_RequestId");

            migrationBuilder.AddColumn<Guid>(
                name: "OfferId",
                table: "Offers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AccountId",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Offers_OfferId",
                table: "Offers",
                column: "OfferId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_AccountId",
                table: "AspNetUsers",
                column: "AccountId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Consumers_AccountId",
                table: "AspNetUsers",
                column: "AccountId",
                principalTable: "Consumers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Providers_AccountId",
                table: "AspNetUsers",
                column: "AccountId",
                principalTable: "Providers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_Requests_OfferId",
                table: "Offers",
                column: "OfferId",
                principalTable: "Requests",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Reviews_RequestId",
                table: "Requests",
                column: "RequestId",
                principalTable: "Reviews",
                principalColumn: "Id");
        }
    }
}
