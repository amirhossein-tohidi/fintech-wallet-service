using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wallet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueUserWalletUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UIX_UserWallets_UserId",
                table: "UserWallets");

            migrationBuilder.CreateIndex(
                name: "UIX_UserWallets_UserId",
                table: "UserWallets",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UIX_UserWallets_UserId",
                table: "UserWallets");

            migrationBuilder.CreateIndex(
                name: "UIX_UserWallets_UserId",
                table: "UserWallets",
                column: "UserId");
        }
    }
}
