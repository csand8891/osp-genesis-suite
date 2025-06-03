using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RuleArchitect.Migrations
{
    public partial class InitialCreateFromCorrectModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MachineTypes",
                columns: table => new
                {
                    MachineTypeId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineTypes", x => x.MachineTypeId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserName = table.Column<string>(maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(nullable: true),
                    PasswordSalt = table.Column<string>(nullable: true),
                    Role = table.Column<string>(nullable: true),
                    IsActive = table.Column<bool>(nullable: false),
                    LastLoginDate = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "ControlSystems",
                columns: table => new
                {
                    ControlSystemId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(maxLength: 100, nullable: false),
                    MachineTypeId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControlSystems", x => x.ControlSystemId);
                    table.ForeignKey(
                        name: "FK_ControlSystems_MachineTypes_MachineTypeId",
                        column: x => x.MachineTypeId,
                        principalTable: "MachineTypes",
                        principalColumn: "MachineTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MachineModels",
                columns: table => new
                {
                    MachineModelId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(maxLength: 100, nullable: false),
                    MachineTypeId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineModels", x => x.MachineModelId);
                    table.ForeignKey(
                        name: "FK_MachineModels_MachineTypes_MachineTypeId",
                        column: x => x.MachineTypeId,
                        principalTable: "MachineTypes",
                        principalColumn: "MachineTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SoftwareOptions",
                columns: table => new
                {
                    SoftwareOptionId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PrimaryName = table.Column<string>(maxLength: 255, nullable: false),
                    AlternativeNames = table.Column<string>(maxLength: 500, nullable: true),
                    SourceFileName = table.Column<string>(maxLength: 255, nullable: true),
                    PrimaryOptionNumberDisplay = table.Column<string>(maxLength: 100, nullable: true),
                    Notes = table.Column<string>(nullable: true),
                    CheckedBy = table.Column<string>(maxLength: 100, nullable: true),
                    CheckedDate = table.Column<DateTime>(nullable: true),
                    ControlSystemId = table.Column<int>(nullable: true),
                    Version = table.Column<int>(nullable: false),
                    LastModifiedDate = table.Column<DateTime>(nullable: false),
                    LastModifiedBy = table.Column<string>(maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoftwareOptions", x => x.SoftwareOptionId);
                    table.ForeignKey(
                        name: "FK_SoftwareOptions_ControlSystems_ControlSystemId",
                        column: x => x.ControlSystemId,
                        principalTable: "ControlSystems",
                        principalColumn: "ControlSystemId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SpecCodeDefinitions",
                columns: table => new
                {
                    SpecCodeDefinitionId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SpecCodeNo = table.Column<string>(maxLength: 50, nullable: false),
                    SpecCodeBit = table.Column<string>(maxLength: 50, nullable: false),
                    Description = table.Column<string>(maxLength: 255, nullable: true),
                    Category = table.Column<string>(maxLength: 50, nullable: false),
                    ControlSystemId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecCodeDefinitions", x => x.SpecCodeDefinitionId);
                    table.ForeignKey(
                        name: "FK_SpecCodeDefinitions_ControlSystems_ControlSystemId",
                        column: x => x.ControlSystemId,
                        principalTable: "ControlSystems",
                        principalColumn: "ControlSystemId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    OrderId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderNumber = table.Column<string>(maxLength: 100, nullable: false),
                    CustomerName = table.Column<string>(maxLength: 255, nullable: true),
                    OrderDate = table.Column<DateTime>(nullable: false),
                    RequiredDate = table.Column<DateTime>(nullable: true),
                    Status = table.Column<int>(nullable: false),
                    Notes = table.Column<string>(nullable: true),
                    ControlSystemId = table.Column<int>(nullable: false),
                    MachineModelId = table.Column<int>(nullable: false),
                    CreatedByUserId = table.Column<int>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    OrderReviewerUserId = table.Column<int>(nullable: true),
                    OrderReviewedAt = table.Column<DateTime>(nullable: true),
                    OrderReviewNotes = table.Column<string>(nullable: true),
                    ProductionTechUserId = table.Column<int>(nullable: true),
                    ProductionCompletedAt = table.Column<DateTime>(nullable: true),
                    ProductionNotes = table.Column<string>(nullable: true),
                    SoftwareReviewerUserId = table.Column<int>(nullable: true),
                    SoftwareReviewedAt = table.Column<DateTime>(nullable: true),
                    SoftwareReviewNotes = table.Column<string>(nullable: true),
                    LastModifiedByUserId = table.Column<int>(nullable: true),
                    LastModifiedAt = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.OrderId);
                    table.ForeignKey(
                        name: "FK_Orders_ControlSystems_ControlSystemId",
                        column: x => x.ControlSystemId,
                        principalTable: "ControlSystems",
                        principalColumn: "ControlSystemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Users_LastModifiedByUserId",
                        column: x => x.LastModifiedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_MachineModels_MachineModelId",
                        column: x => x.MachineModelId,
                        principalTable: "MachineModels",
                        principalColumn: "MachineModelId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Users_OrderReviewerUserId",
                        column: x => x.OrderReviewerUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Users_ProductionTechUserId",
                        column: x => x.ProductionTechUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Users_SoftwareReviewerUserId",
                        column: x => x.SoftwareReviewerUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OptionNumberRegistries",
                columns: table => new
                {
                    OptionNumberRegistryId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OptionNumber = table.Column<string>(maxLength: 50, nullable: false),
                    SoftwareOptionId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OptionNumberRegistries", x => x.OptionNumberRegistryId);
                    table.ForeignKey(
                        name: "FK_OptionNumberRegistries_SoftwareOptions_SoftwareOptionId",
                        column: x => x.SoftwareOptionId,
                        principalTable: "SoftwareOptions",
                        principalColumn: "SoftwareOptionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParameterMappings",
                columns: table => new
                {
                    ParameterMappingId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RelatedSheetName = table.Column<string>(maxLength: 255, nullable: true),
                    ConditionIdentifier = table.Column<string>(maxLength: 255, nullable: true),
                    ConditionName = table.Column<string>(maxLength: 255, nullable: true),
                    SettingContext = table.Column<string>(maxLength: 255, nullable: true),
                    ConfigurationDetailsJson = table.Column<string>(nullable: false),
                    SoftwareOptionId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParameterMappings", x => x.ParameterMappingId);
                    table.ForeignKey(
                        name: "FK_ParameterMappings_SoftwareOptions_SoftwareOptionId",
                        column: x => x.SoftwareOptionId,
                        principalTable: "SoftwareOptions",
                        principalColumn: "SoftwareOptionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SoftwareOptionActivationRules",
                columns: table => new
                {
                    SoftwareOptionActivationRuleId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SoftwareOptionId = table.Column<int>(nullable: false),
                    RuleName = table.Column<string>(maxLength: 255, nullable: true),
                    ActivationSetting = table.Column<string>(maxLength: 255, nullable: false),
                    Notes = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoftwareOptionActivationRules", x => x.SoftwareOptionActivationRuleId);
                    table.ForeignKey(
                        name: "FK_SoftwareOptionActivationRules_SoftwareOptions_SoftwareOptionId",
                        column: x => x.SoftwareOptionId,
                        principalTable: "SoftwareOptions",
                        principalColumn: "SoftwareOptionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SoftwareOptionHistories",
                columns: table => new
                {
                    SoftwareOptionHistoryId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SoftwareOptionId = table.Column<int>(nullable: false),
                    Version = table.Column<int>(nullable: false),
                    PrimaryName = table.Column<string>(maxLength: 255, nullable: false),
                    AlternativeNames = table.Column<string>(maxLength: 500, nullable: true),
                    SourceFileName = table.Column<string>(maxLength: 255, nullable: true),
                    PrimaryOptionNumberDisplay = table.Column<string>(maxLength: 100, nullable: true),
                    Notes = table.Column<string>(nullable: true),
                    CheckedBy = table.Column<string>(maxLength: 100, nullable: true),
                    CheckedDate = table.Column<DateTime>(nullable: true),
                    ControlSystemId = table.Column<int>(nullable: true),
                    ChangeTimestamp = table.Column<DateTime>(nullable: false),
                    ChangedBy = table.Column<string>(maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoftwareOptionHistories", x => x.SoftwareOptionHistoryId);
                    table.ForeignKey(
                        name: "FK_SoftwareOptionHistories_SoftwareOptions_SoftwareOptionId",
                        column: x => x.SoftwareOptionId,
                        principalTable: "SoftwareOptions",
                        principalColumn: "SoftwareOptionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Requirements",
                columns: table => new
                {
                    RequirementId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SoftwareOptionId = table.Column<int>(nullable: false),
                    RequirementType = table.Column<string>(maxLength: 100, nullable: false),
                    Condition = table.Column<string>(maxLength: 100, nullable: true),
                    GeneralRequiredValue = table.Column<string>(nullable: false),
                    RequiredSoftwareOptionId = table.Column<int>(nullable: true),
                    RequiredSpecCodeDefinitionId = table.Column<int>(nullable: true),
                    OspFileName = table.Column<string>(maxLength: 255, nullable: true),
                    OspFileVersion = table.Column<string>(maxLength: 50, nullable: true),
                    Notes = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Requirements", x => x.RequirementId);
                    table.ForeignKey(
                        name: "FK_Requirements_SoftwareOptions_RequiredSoftwareOptionId",
                        column: x => x.RequiredSoftwareOptionId,
                        principalTable: "SoftwareOptions",
                        principalColumn: "SoftwareOptionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Requirements_SpecCodeDefinitions_RequiredSpecCodeDefinitionId",
                        column: x => x.RequiredSpecCodeDefinitionId,
                        principalTable: "SpecCodeDefinitions",
                        principalColumn: "SpecCodeDefinitionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Requirements_SoftwareOptions_SoftwareOptionId",
                        column: x => x.SoftwareOptionId,
                        principalTable: "SoftwareOptions",
                        principalColumn: "SoftwareOptionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    OrderItemId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderId = table.Column<int>(nullable: false),
                    SoftwareOptionId = table.Column<int>(nullable: false),
                    AddedAt = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.OrderItemId);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItems_SoftwareOptions_SoftwareOptionId",
                        column: x => x.SoftwareOptionId,
                        principalTable: "SoftwareOptions",
                        principalColumn: "SoftwareOptionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SoftwareOptionSpecificationCodes",
                columns: table => new
                {
                    SoftwareOptionSpecificationCodeId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SoftwareOptionId = table.Column<int>(nullable: false),
                    SpecCodeDefinitionId = table.Column<int>(nullable: false),
                    SoftwareOptionActivationRuleId = table.Column<int>(nullable: true),
                    SpecificInterpretation = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoftwareOptionSpecificationCodes", x => x.SoftwareOptionSpecificationCodeId);
                    table.ForeignKey(
                        name: "FK_SoftwareOptionSpecificationCodes_SoftwareOptionActivationRules_SoftwareOptionActivationRuleId",
                        column: x => x.SoftwareOptionActivationRuleId,
                        principalTable: "SoftwareOptionActivationRules",
                        principalColumn: "SoftwareOptionActivationRuleId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SoftwareOptionSpecificationCodes_SoftwareOptions_SoftwareOptionId",
                        column: x => x.SoftwareOptionId,
                        principalTable: "SoftwareOptions",
                        principalColumn: "SoftwareOptionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SoftwareOptionSpecificationCodes_SpecCodeDefinitions_SpecCodeDefinitionId",
                        column: x => x.SpecCodeDefinitionId,
                        principalTable: "SpecCodeDefinitions",
                        principalColumn: "SpecCodeDefinitionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "MachineTypes",
                columns: new[] { "MachineTypeId", "Name" },
                values: new object[] { 1, "Lathe" });

            migrationBuilder.InsertData(
                table: "MachineTypes",
                columns: new[] { "MachineTypeId", "Name" },
                values: new object[] { 2, "Machining Center" });

            migrationBuilder.InsertData(
                table: "MachineTypes",
                columns: new[] { "MachineTypeId", "Name" },
                values: new object[] { 3, "Grinder" });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "IsActive", "LastLoginDate", "PasswordHash", "PasswordSalt", "Role", "UserName" },
                values: new object[] { 1, true, null, "gHarxnybaF14pg0khiMv27IsdXuj2dmx0ytALdo5+aE=", "f9DAu0b2jcGAhuVKmgFYNw==", "Administrator", "admin" });

            migrationBuilder.InsertData(
                table: "ControlSystems",
                columns: new[] { "ControlSystemId", "MachineTypeId", "Name" },
                values: new object[] { 1, 1, "P300L" });

            migrationBuilder.InsertData(
                table: "ControlSystems",
                columns: new[] { "ControlSystemId", "MachineTypeId", "Name" },
                values: new object[] { 2, 1, "P300S" });

            migrationBuilder.InsertData(
                table: "ControlSystems",
                columns: new[] { "ControlSystemId", "MachineTypeId", "Name" },
                values: new object[] { 5, 1, "P200L" });

            migrationBuilder.InsertData(
                table: "ControlSystems",
                columns: new[] { "ControlSystemId", "MachineTypeId", "Name" },
                values: new object[] { 3, 2, "P300M" });

            migrationBuilder.InsertData(
                table: "ControlSystems",
                columns: new[] { "ControlSystemId", "MachineTypeId", "Name" },
                values: new object[] { 4, 2, "E100M" });

            migrationBuilder.InsertData(
                table: "ControlSystems",
                columns: new[] { "ControlSystemId", "MachineTypeId", "Name" },
                values: new object[] { 6, 2, "P200M" });

            migrationBuilder.CreateIndex(
                name: "IX_ControlSystems_MachineTypeId",
                table: "ControlSystems",
                column: "MachineTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ControlSystemName",
                table: "ControlSystems",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MachineModels_MachineTypeId",
                table: "MachineModels",
                column: "MachineTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_MachineModelName",
                table: "MachineModels",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MachineTypeName",
                table: "MachineTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OptionNumberRegistries_SoftwareOptionId",
                table: "OptionNumberRegistries",
                column: "SoftwareOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_SoftwareOptionId",
                table: "OrderItems",
                column: "SoftwareOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Order_SoftwareOption",
                table: "OrderItems",
                columns: new[] { "OrderId", "SoftwareOptionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ControlSystemId",
                table: "Orders",
                column: "ControlSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CreatedByUserId",
                table: "Orders",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_LastModifiedByUserId",
                table: "Orders",
                column: "LastModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_MachineModelId",
                table: "Orders",
                column: "MachineModelId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderNumber",
                table: "Orders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderReviewerUserId",
                table: "Orders",
                column: "OrderReviewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ProductionTechUserId",
                table: "Orders",
                column: "ProductionTechUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_SoftwareReviewerUserId",
                table: "Orders",
                column: "SoftwareReviewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ParameterMappings_SoftwareOptionId",
                table: "ParameterMappings",
                column: "SoftwareOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Requirements_RequiredSoftwareOptionId",
                table: "Requirements",
                column: "RequiredSoftwareOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Requirements_RequiredSpecCodeDefinitionId",
                table: "Requirements",
                column: "RequiredSpecCodeDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Requirements_SoftwareOptionId",
                table: "Requirements",
                column: "SoftwareOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareOptionActivationRules_SoftwareOptionId",
                table: "SoftwareOptionActivationRules",
                column: "SoftwareOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareOptionHistory_SoftwareOptionId_Version",
                table: "SoftwareOptionHistories",
                columns: new[] { "SoftwareOptionId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareOptions_ControlSystemId",
                table: "SoftwareOptions",
                column: "ControlSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareOptionSpecificationCodes_SoftwareOptionActivationRuleId",
                table: "SoftwareOptionSpecificationCodes",
                column: "SoftwareOptionActivationRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareOptionSpecificationCodes_SoftwareOptionId",
                table: "SoftwareOptionSpecificationCodes",
                column: "SoftwareOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareOptionSpecificationCodes_SpecCodeDefinitionId",
                table: "SoftwareOptionSpecificationCodes",
                column: "SpecCodeDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_SpecCodeDefinitions_ControlSystemId",
                table: "SpecCodeDefinitions",
                column: "ControlSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_SpecCodeNoBitControlSystemCategory",
                table: "SpecCodeDefinitions",
                columns: new[] { "SpecCodeNo", "SpecCodeBit", "ControlSystemId", "Category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserName",
                table: "Users",
                column: "UserName",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OptionNumberRegistries");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "ParameterMappings");

            migrationBuilder.DropTable(
                name: "Requirements");

            migrationBuilder.DropTable(
                name: "SoftwareOptionHistories");

            migrationBuilder.DropTable(
                name: "SoftwareOptionSpecificationCodes");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "SoftwareOptionActivationRules");

            migrationBuilder.DropTable(
                name: "SpecCodeDefinitions");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "MachineModels");

            migrationBuilder.DropTable(
                name: "SoftwareOptions");

            migrationBuilder.DropTable(
                name: "ControlSystems");

            migrationBuilder.DropTable(
                name: "MachineTypes");
        }
    }
}
