using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RuleArchitect.Migrations
{
    public partial class AddUserLogActivityEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserActivityLog",
                columns: table => new
                {
                    UserActivtyLogId = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(nullable: false),
                    UserName = table.Column<string>(maxLength: 100, nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    ActivityType = table.Column<string>(maxLength: 100, nullable: false),
                    TargetEntityType = table.Column<string>(maxLength: 100, nullable: true),
                    TargetEntityId = table.Column<int>(nullable: true),
                    TargetEntityDescription = table.Column<string>(maxLength: 255, nullable: true),
                    Description = table.Column<string>(nullable: false),
                    Details = table.Column<string>(nullable: true),
                    IpAddress = table.Column<string>(maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserActivityLog", x => x.UserActivtyLogId);
                    table.ForeignKey(
                        name: "FK_UserActivityLog_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserActivityLog_ActivityType",
                table: "UserActivityLog",
                column: "ActivityType");

            migrationBuilder.CreateIndex(
                name: "IX_UserActivityLog_Timestamp",
                table: "UserActivityLog",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_UserActivityLog_UserId",
                table: "UserActivityLog",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserActivityLog_TargetEntityType_TargetEntity",
                table: "UserActivityLog",
                columns: new[] { "TargetEntityType", "TargetEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserActivityLog_User_Timestamp",
                table: "UserActivityLog",
                columns: new[] { "UserId", "Timestamp" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserActivityLog");
        }
    }
}
