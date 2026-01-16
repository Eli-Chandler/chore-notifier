using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChoreNotifier.Migrations
{
    /// <inheritdoc />
    public partial class CascadeUserDeletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotificationMethods_Users_UserId",
                table: "NotificationMethods");

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationMethods_Users_UserId",
                table: "NotificationMethods",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotificationMethods_Users_UserId",
                table: "NotificationMethods");

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationMethods_Users_UserId",
                table: "NotificationMethods",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
