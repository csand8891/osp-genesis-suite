using Microsoft.EntityFrameworkCore.Migrations;

namespace RuleArchitect.Migrations
{
    public partial class SeedAdminUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "IsActive", "LastLoginDate", "PasswordHash", "PasswordSalt", "Role", "UserName" },
                values: new object[] { 1, true, null, "gHarxnybaF14pg0khiMv27IsdXuj2dmx0ytALdo5+aE=", "f9DAu0b2jcGAhuVKmgFYNw==", "Administrator", "admin" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1);
        }
    }
}
