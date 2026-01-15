using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ChoreNotifier.Migrations
{
    /// <inheritdoc />
    public partial class AddNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificationAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Notification_Title = table.Column<string>(type: "text", nullable: false),
                    Notification_Message = table.Column<string>(type: "text", nullable: false),
                    NotificationType = table.Column<int>(type: "integer", nullable: true),
                    AttemptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RecipientId = table.Column<int>(type: "integer", nullable: false),
                    DeliveryStatus = table.Column<int>(type: "integer", nullable: false),
                    DeliveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationAttempts_Users_RecipientId",
                        column: x => x.RecipientId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationMethods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    TopicName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationMethods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationMethods_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationAttempts_RecipientId",
                table: "NotificationAttempts",
                column: "RecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationMethods_UserId",
                table: "NotificationMethods",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationAttempts");

            migrationBuilder.DropTable(
                name: "NotificationMethods");
        }
    }
}
