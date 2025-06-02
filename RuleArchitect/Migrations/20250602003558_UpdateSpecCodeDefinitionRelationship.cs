using Microsoft.EntityFrameworkCore.Migrations;

namespace RuleArchitect.Migrations
{
    public partial class UpdateSpecCodeDefinitionRelationship : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Turn off foreign key constraints for this operation block
            migrationBuilder.Sql("PRAGMA foreign_keys=OFF;");

            // 1. Rename the existing SpecCodeDefinitions table
            migrationBuilder.Sql("ALTER TABLE SpecCodeDefinitions RENAME TO SpecCodeDefinitions_old;");

            // 2. Create the new SpecCodeDefinitions table with the updated schema
            migrationBuilder.Sql(@"
        CREATE TABLE SpecCodeDefinitions (
            SpecCodeDefinitionId INTEGER NOT NULL CONSTRAINT PK_SpecCodeDefinitions PRIMARY KEY AUTOINCREMENT,
            SpecCodeNo TEXT NOT NULL,
            SpecCodeBit TEXT NOT NULL,
            Description TEXT NULL,
            Category TEXT NOT NULL,
            ControlSystemId INTEGER NOT NULL,
            CONSTRAINT FK_SpecCodeDefinitions_ControlSystems_ControlSystemId FOREIGN KEY (ControlSystemId) REFERENCES ControlSystems (ControlSystemId) ON DELETE RESTRICT
        );
    ");

            // 3. Copy data from the old table to the new table.
            //    CORRECTED: Provide a placeholder literal for ControlSystemId in the SELECT list.
            //    Since SpecCodeDefinitions_old is empty, no rows will be inserted, so the '1' is just a valid integer placeholder.
            migrationBuilder.Sql(@"
        INSERT INTO SpecCodeDefinitions (
            SpecCodeDefinitionId, SpecCodeNo, SpecCodeBit, Description, Category, ControlSystemId
        )
        SELECT
            SpecCodeDefinitionId, SpecCodeNo, SpecCodeBit, Description, Category,
            1 /* Placeholder for ControlSystemId; SpecCodeDefinitions_old does not have this column. */
        FROM SpecCodeDefinitions_old;
    ");

            // 4. Drop the old table
            migrationBuilder.Sql("DROP TABLE SpecCodeDefinitions_old;");

            // 5. Re-create indexes on the new table.
            migrationBuilder.Sql(@"
        CREATE UNIQUE INDEX IX_SpecCodeNoBitControlSystem
        ON SpecCodeDefinitions (SpecCodeNo, SpecCodeBit, ControlSystemId);
    ");

            // Turn foreign key constraints back on
            migrationBuilder.Sql("PRAGMA foreign_keys=ON;");

            // Ensure original EF Core C# operations for this table (DropForeignKey, AddColumn, DropColumn, etc.)
            // are commented out or removed in this Up() method.
        }
    }
}
