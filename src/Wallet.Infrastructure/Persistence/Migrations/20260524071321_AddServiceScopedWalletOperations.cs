using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wallet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceScopedWalletOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PromoGrant_UserWallets_UserWalletId",
                table: "PromoGrant");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_WalletId_Status",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_LedgerTransactions_WalletId",
                table: "LedgerTransactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PromoGrant",
                table: "PromoGrant");

            migrationBuilder.DropIndex(
                name: "IX_PromoGrant_UserWalletId",
                table: "PromoGrant");

            migrationBuilder.DropColumn(
                name: "UserWalletId",
                table: "PromoGrant");

            migrationBuilder.RenameTable(
                name: "PromoGrant",
                newName: "PromoGrants");

            migrationBuilder.AddColumn<int>(
                name: "ServiceType",
                table: "Reservations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ServiceType",
                table: "LedgerTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "RemainingAmount",
                table: "PromoGrants",
                type: "decimal(18,0)",
                precision: 18,
                scale: 0,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ModifiedAt",
                table: "PromoGrants",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "PromoGrants",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "PromoGrants",
                type: "decimal(18,0)",
                precision: 18,
                scale: 0,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "PromoGrants",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "ServiceType",
                table: "PromoGrants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PromoGrants",
                table: "PromoGrants",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_WalletId_ServiceType_Status",
                table: "Reservations",
                columns: new[] { "WalletId", "ServiceType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerTransactions_WalletId_ServiceType_CreatedAt",
                table: "LedgerTransactions",
                columns: new[] { "WalletId", "ServiceType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PromoGrants_WalletId_ServiceType_ExpiresAt",
                table: "PromoGrants",
                columns: new[] { "WalletId", "ServiceType", "ExpiresAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_PromoGrants_WalletId",
                table: "PromoGrants",
                column: "WalletId",
                principalTable: "UserWallets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PromoGrants_WalletId",
                table: "PromoGrants");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_WalletId_ServiceType_Status",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_LedgerTransactions_WalletId_ServiceType_CreatedAt",
                table: "LedgerTransactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PromoGrants",
                table: "PromoGrants");

            migrationBuilder.DropIndex(
                name: "IX_PromoGrants_WalletId_ServiceType_ExpiresAt",
                table: "PromoGrants");

            migrationBuilder.DropColumn(
                name: "ServiceType",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "ServiceType",
                table: "LedgerTransactions");

            migrationBuilder.DropColumn(
                name: "ServiceType",
                table: "PromoGrants");

            migrationBuilder.RenameTable(
                name: "PromoGrants",
                newName: "PromoGrant");

            migrationBuilder.AlterColumn<decimal>(
                name: "RemainingAmount",
                table: "PromoGrant",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,0)",
                oldPrecision: 18,
                oldScale: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ModifiedAt",
                table: "PromoGrant",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "PromoGrant",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "PromoGrant",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,0)",
                oldPrecision: 18,
                oldScale: 0);

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "PromoGrant",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<long>(
                name: "UserWalletId",
                table: "PromoGrant",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PromoGrant",
                table: "PromoGrant",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_WalletId_Status",
                table: "Reservations",
                columns: new[] { "WalletId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerTransactions_WalletId",
                table: "LedgerTransactions",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_PromoGrant_UserWalletId",
                table: "PromoGrant",
                column: "UserWalletId");

            migrationBuilder.AddForeignKey(
                name: "FK_PromoGrant_UserWallets_UserWalletId",
                table: "PromoGrant",
                column: "UserWalletId",
                principalTable: "UserWallets",
                principalColumn: "Id");
        }
    }
}
