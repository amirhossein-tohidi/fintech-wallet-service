using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wallet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialWalletSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "WalletEntityIds",
                incrementBy: 10);

            migrationBuilder.CreateTable(
                name: "IdempotencyRequests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponseBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ExpireAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    OccurredOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastAttemptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeadLetteredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LockedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    DeadLetterRetryCount = table.Column<int>(type: "int", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastAttemptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeadLetteredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LockedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    DeadLetterRetryCount = table.Column<int>(type: "int", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserWallets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AvailableBalance = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    ReservedBalance = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserWallets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LedgerTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    WalletId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    ServiceType = table.Column<int>(type: "int", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    ReferenceId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LedgerTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LedgerTransactions_WalletId",
                        column: x => x.WalletId,
                        principalTable: "UserWallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PromoGrants",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    WalletId = table.Column<long>(type: "bigint", nullable: false),
                    ServiceType = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromoGrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromoGrants_WalletId",
                        column: x => x.WalletId,
                        principalTable: "UserWallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reservations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    WalletId = table.Column<long>(type: "bigint", nullable: false),
                    ServiceType = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    ExpireAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservations_WalletId",
                        column: x => x.WalletId,
                        principalTable: "UserWallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LedgerEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TransactionId = table.Column<long>(type: "bigint", nullable: false),
                    Account = table.Column<int>(type: "int", nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LedgerEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LedgerEntries_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "LedgerTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UIX_IdempotencyRequests_Key",
                table: "IdempotencyRequests",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_DeadLetter",
                table: "InboxMessages",
                columns: new[] { "DeadLetteredAt", "LockedUntil", "LastAttemptedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_EventType_Processing",
                table: "InboxMessages",
                columns: new[] { "EventType", "ProcessedAt", "DeadLetteredAt", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_Processing",
                table: "InboxMessages",
                columns: new[] { "ProcessedAt", "LockedUntil", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_TransactionId",
                table: "LedgerEntries",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerTransactions_WalletId_ServiceType_CreatedAt",
                table: "LedgerTransactions",
                columns: new[] { "WalletId", "ServiceType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "UIX_LedgerTransactions_IdempotencyKey",
                table: "LedgerTransactions",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_DeadLetter",
                table: "OutboxMessages",
                columns: new[] { "DeadLetteredAt", "LockedUntil", "LastAttemptedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_EventType_Processing",
                table: "OutboxMessages",
                columns: new[] { "EventType", "ProcessedAt", "DeadLetteredAt", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Processing",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAt", "LockedUntil", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PromoGrants_WalletId_ServiceType_ExpiresAt",
                table: "PromoGrants",
                columns: new[] { "WalletId", "ServiceType", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_WalletId_ServiceType_Status",
                table: "Reservations",
                columns: new[] { "WalletId", "ServiceType", "Status" });

            migrationBuilder.CreateIndex(
                name: "UIX_UserWallets_UserId",
                table: "UserWallets",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdempotencyRequests");

            migrationBuilder.DropTable(
                name: "InboxMessages");

            migrationBuilder.DropTable(
                name: "LedgerEntries");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "PromoGrants");

            migrationBuilder.DropTable(
                name: "Reservations");

            migrationBuilder.DropTable(
                name: "LedgerTransactions");

            migrationBuilder.DropTable(
                name: "UserWallets");

            migrationBuilder.DropSequence(
                name: "WalletEntityIds");
        }
    }
}
