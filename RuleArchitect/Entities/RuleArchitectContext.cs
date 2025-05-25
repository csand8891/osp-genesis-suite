using Microsoft.EntityFrameworkCore;
using RuleArchitect.Entities;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;
using GenesisSentry.Interfaces;
using GenesisSentry.Entities;
namespace RuleArchitect.Data
{
    public class RuleArchitectContext : DbContext, IAuthenticationDbContext
    {
        public virtual DbSet<MachineType> MachineTypes { get; set; } = null!;
        public virtual DbSet<ControlSystem> ControlSystems { get; set; } = null!;
        public virtual DbSet<SoftwareOption> SoftwareOptions { get; set; } = null!;
        public virtual DbSet<OptionNumberRegistry> OptionNumberRegistries { get; set; } = null!;
        public virtual DbSet<SpecCodeDefinition> SpecCodeDefinitions { get; set; } = null!;
        public virtual DbSet<SoftwareOptionActivationRule> SoftwareOptionActivationRules { get; set; } = null!;
        public virtual DbSet<SoftwareOptionSpecificationCode> SoftwareOptionSpecificationCodes { get; set; } = null!;
        public virtual DbSet<Requirement> Requirements { get; set; } = null!;
        public virtual DbSet<ParameterMapping> ParameterMappings { get; set; } = null!;
        public virtual DbSet<SoftwareOptionHistory> SoftwareOptionHistories { get; set; } = null!;
        public virtual DbSet<UserEntity> Users { get; set; } = null!;

        // Parameterless constructor for existing uses (like EF Migrations tool)
        public RuleArchitectContext()
        {
        }

        // Constructor for Dependency Injection
        public RuleArchitectContext(DbContextOptions<RuleArchitectContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // This will be used if the parameterless constructor is called,
                // or if optionsBuilder is not configured by a derived context.
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
                      .IsUnique(false); // Ensure this is the desired behavior for uniqueness
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

                // Relationship: SoftwareOption to ParameterMapping (One-to-Many, ParameterMapping.SoftwareOptionId is optional)
                entity.HasMany(so => so.ParameterMappings)
                      .WithOne(pm => pm.SoftwareOption)
                      .HasForeignKey(pm => pm.SoftwareOptionId)
                      .IsRequired(false) // Based on int? SoftwareOptionId in ParameterMapping.cs
                      .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<SoftwareOptionActivationRule>(entity =>
            {
                // Relationship to SoftwareOption (Many-to-One, SoftwareOptionId is required)
                entity.HasOne(soar => soar.SoftwareOption)
                      .WithMany(so => so.SoftwareOptionActivationRules)
                      .HasForeignKey(soar => soar.SoftwareOptionId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SoftwareOptionSpecificationCode>(entity =>
            {
                // Relationship to SoftwareOption (Many-to-One, SoftwareOptionId is required)
                entity.HasOne(sosc => sosc.SoftwareOption)
                      .WithMany(so => so.SoftwareOptionSpecificationCodes)
                      .HasForeignKey(sosc => sosc.SoftwareOptionId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Cascade);

                // Relationship to SpecCodeDefinition (Many-to-One, SpecCodeDefinitionId is required)
                entity.HasOne(sosc => sosc.SpecCodeDefinition)
                      .WithMany(scd => scd.SoftwareOptionSpecificationCodes)
                      .HasForeignKey(sosc => sosc.SpecCodeDefinitionId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Restrict);

                // Relationship to SoftwareOptionActivationRule (Many-to-One, SoftwareOptionActivationRuleId is optional)
                entity.HasOne(sosc => sosc.SoftwareOptionActivationRule)
                      .WithMany(soar => soar.SoftwareOptionSpecificationCodes)
                      .HasForeignKey(sosc => sosc.SoftwareOptionActivationRuleId)
                      .IsRequired(false)
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
                      .OnDelete(DeleteBehavior.ClientSetNull);

                // Relationship of Requirement TO SpecCodeDefinition (the one that IS required by this Requirement) - OPTIONAL
                entity.HasOne(r => r.RequiredSpecCodeDefinition)
                      .WithMany(scd => scd.Requirements)
                      .HasForeignKey(r => r.RequiredSpecCodeDefinitionId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<UserEntity>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.HasIndex(e => e.UserName).IsUnique();
                entity.Property(e => e.UserName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.PasswordSalt).IsRequired();
                entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
                entity.Property(e => e.IsActive).IsRequired();
            });
        }
    }
}