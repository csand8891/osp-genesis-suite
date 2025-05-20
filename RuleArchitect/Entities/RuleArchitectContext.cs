using Microsoft.EntityFrameworkCore;
using RuleArchitect.Entities; // Assuming your entities are here
using System.Configuration;

namespace RuleArchitect.Data
{
    public class RuleArchitectContext : DbContext
    {
        public DbSet<MachineType> MachineTypes { get; set; } = null!;
        public DbSet<ControlSystem> ControlSystems { get; set; } = null!;
        public DbSet<SoftwareOption> SoftwareOptions { get; set; } = null!;
        public DbSet<OptionNumberRegistry> OptionNumberRegistries { get; set; } = null!;
        public DbSet<SpecCodeDefinition> SpecCodeDefinitions { get; set; } = null!;
        public DbSet<SoftwareOptionActivationRule> SoftwareOptionActivationRules { get; set; } = null!;
        public DbSet<SoftwareOptionSpecificationCode> SoftwareOptionSpecificationCodes { get; set; } = null!;
        public DbSet<Requirement> Requirements { get; set; } = null!;
        public DbSet<ParameterMapping> ParameterMappings { get; set; } = null!;
        public DbSet<SoftwareOptionHistory> SoftwareOptionHistories { get; set; } = null!;

        public RuleArchitectContext()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Ensure your RuleArchitect project has a reference to System.Configuration.dll
                // and your App.config has the "RuleArchitectSqliteConnection" connection string.
                string connectionString = ConfigurationManager.ConnectionStrings["RuleArchitectSqliteConnection"].ConnectionString;
                optionsBuilder.UseSqlite(connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- Index Configurations (from removed [Index] attributes) ---

            modelBuilder.Entity<ControlSystem>(entity =>
            {
                entity.HasIndex(cs => cs.Name)
                      .IsUnique()
                      .HasName("IX_ControlSystemName"); // EF Core 3.1 uses HasName

                // Relationship: ControlSystem to MachineType (Many-to-One)
                entity.HasOne(cs => cs.MachineType)
                      .WithMany(mt => mt.ControlSystems)
                      .HasForeignKey(cs => cs.MachineTypeId)
                      .IsRequired() // Since MachineTypeId is not nullable
                      .OnDelete(DeleteBehavior.Restrict); // Example: Prevent deletion of MachineType if ControlSystems are linked.
                                                          // EF Core default for required is Cascade. Adjust as needed.
            });

            modelBuilder.Entity<MachineType>(entity =>
            {
                entity.HasIndex(mt => mt.Name)
                      .IsUnique()
                      .HasName("IX_MachineTypeName");
            });

            modelBuilder.Entity<SpecCodeDefinition>(entity =>
            {
                // Composite unique index
                entity.HasIndex(scd => new { scd.SpecCodeNo, scd.SpecCodeBit, scd.MachineTypeId })
                      .IsUnique()
                      .HasName("IX_SpecCodeNoBitMachineType");

                // Relationship: SpecCodeDefinition to MachineType (Many-to-One)
                entity.HasOne(scd => scd.MachineType)
                      .WithMany(mt => mt.SpecCodeDefinitions)
                      .HasForeignKey(scd => scd.MachineTypeId)
                      .IsRequired() // Since MachineTypeId is not nullable
                      .OnDelete(DeleteBehavior.Restrict); // Example
            });


            // --- Relationship Configurations (incorporating your previous setup) ---

            modelBuilder.Entity<SoftwareOptionHistory>(entity =>
            {
                entity.HasOne(h => h.SoftwareOption)
                      .WithMany(so => so.Histories)
                      .HasForeignKey(h => h.SoftwareOptionId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(h => new { h.SoftwareOptionId, h.Version })
                      .HasName("IX_SoftwareOptionHistory_SoftwareOptionId_Version")
                      .IsUnique(false);
            });

            modelBuilder.Entity<OptionNumberRegistry>(entity =>
            {
                entity.HasOne(onr => onr.SoftwareOption)
                      .WithMany(so => so.OptionNumberRegistries)
                      .HasForeignKey(onr => onr.SoftwareOptionId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SoftwareOption>(entity =>
            {
                // Relationship: SoftwareOption to ControlSystem (Many-to-One, optional)
                entity.HasOne(so => so.ControlSystem)
                      .WithMany(cs => cs.SoftwareOptions)
                      .HasForeignKey(so => so.ControlSystemId)
                      .IsRequired(false) // Based on int? ControlSystemId in SoftwareOption.cs
                      .OnDelete(DeleteBehavior.ClientSetNull); // Default for optional if FK is nullable.
                                                               // Use Restrict if ControlSystem should not be deleted if SoftwareOptions link to it,
                                                               // even if link is removed.

                // Relationship: SoftwareOption to ParameterMapping (One-to-Many, ParameterMapping.SoftwareOptionId is optional)
                entity.HasMany(so => so.ParameterMappings)
                      .WithOne(pm => pm.SoftwareOption)
                      .HasForeignKey(pm => pm.SoftwareOptionId)
                      .IsRequired(false) // Based on int? SoftwareOptionId in ParameterMapping.cs
                      .OnDelete(DeleteBehavior.ClientSetNull); // If SoftwareOption deleted, set FK to null in ParameterMapping
                                                               // Or .OnDelete(DeleteBehavior.Cascade) if mappings should be deleted.
            });

            modelBuilder.Entity<SoftwareOptionActivationRule>(entity =>
            {
                // Relationship to SoftwareOption (Many-to-One, SoftwareOptionId is required)
                entity.HasOne(soar => soar.SoftwareOption)
                      .WithMany(so => so.SoftwareOptionActivationRules)
                      .HasForeignKey(soar => soar.SoftwareOptionId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Cascade); // If SoftwareOption is deleted, its ActivationRules are deleted.
            });

            modelBuilder.Entity<SoftwareOptionSpecificationCode>(entity =>
            {
                // Relationship to SoftwareOption (Many-to-One, SoftwareOptionId is required)
                entity.HasOne(sosc => sosc.SoftwareOption)
                      .WithMany(so => so.SoftwareOptionSpecificationCodes)
                      .HasForeignKey(sosc => sosc.SoftwareOptionId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Cascade); // If SoftwareOption is deleted, its links to SpecCodes are deleted.

                // Relationship to SpecCodeDefinition (Many-to-One, SpecCodeDefinitionId is required)
                entity.HasOne(sosc => sosc.SpecCodeDefinition)
                      .WithMany(scd => scd.SoftwareOptionSpecificationCodes)
                      .HasForeignKey(sosc => sosc.SpecCodeDefinitionId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Restrict); // Example: Prevent deleting a SpecCodeDefinition if it's in use here.

                // Relationship to SoftwareOptionActivationRule (Many-to-One, SoftwareOptionActivationRuleId is optional)
                entity.HasOne(sosc => sosc.SoftwareOptionActivationRule)
                      .WithMany(soar => soar.SoftwareOptionSpecificationCodes)
                      .HasForeignKey(sosc => sosc.SoftwareOptionActivationRuleId)
                      .IsRequired(false) // Based on int? SoftwareOptionActivationRuleId
                      .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Requirement>(entity =>
            {
                // Relationship of Requirement TO SoftwareOption (the one that HAS the requirement)
                entity.HasOne(r => r.SoftwareOption)
                      .WithMany(so => so.Requirements)
                      .HasForeignKey(r => r.SoftwareOptionId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Cascade);

                // Relationship of Requirement TO another SoftwareOption (the one that IS required by this Requirement) - OPTIONAL
                entity.HasOne(r => r.RequiredSoftwareOption)
                      .WithMany(so => so.RequiredByOptions)
                      .HasForeignKey(r => r.RequiredSoftwareOptionId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.ClientSetNull); // If RequiredSoftwareOption is deleted, just null out the FK here.
                                                               // Using Restrict would prevent deletion of RequiredSoftwareOption if it's linked.

                // Relationship of Requirement TO SpecCodeDefinition (the one that IS required by this Requirement) - OPTIONAL
                entity.HasOne(r => r.RequiredSpecCodeDefinition)
                      .WithMany(scd => scd.Requirements)
                      .HasForeignKey(r => r.RequiredSpecCodeDefinitionId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.ClientSetNull); // Similar logic, or Restrict.
            });

            // Ensure you review all entities and their desired relationships and constraints.
            // For example, if any string properties that are not [Required] should have a default SQL value,
            // or if any numeric properties need specific precision/scale for SQLite (though usually less critical for SQLite).
        }
    }
}