// Current file: csand8891/osp-genesis-suite/osp-genesis-suite-development/RuleArchitect/Entities/RuleArchitectContext.cs
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
        public virtual DbSet<Order> Orders { get; set; } = null!;
        public virtual DbSet<OrderItem> OrderItems { get; set; } = null!;
        public virtual DbSet<MachineModel> MachineModels { get; set; } = null!;
        public virtual DbSet<UserActivityLog> UserActivityLogs { get; set; } = null!;
        public virtual DbSet<RoleEntity> Roles { get; set; } = null!; // New DbSet for Roles

        public RuleArchitectContext() { }

        public RuleArchitectContext(DbContextOptions<RuleArchitectContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string connectionString = ConfigurationManager.ConnectionStrings["RuleArchitectSqliteConnection"].ConnectionString;
                optionsBuilder.UseSqlite(connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- Existing Configurations ---
            modelBuilder.Entity<ControlSystem>(entity =>
            {
                entity.HasIndex(cs => cs.Name).IsUnique().HasName("IX_ControlSystemName");
                entity.HasOne(cs => cs.MachineType)
                      .WithMany(mt => mt.ControlSystems)
                      .HasForeignKey(cs => cs.MachineTypeId)
                      .IsRequired().OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<MachineType>(entity =>
            {
                entity.HasIndex(mt => mt.Name).IsUnique().HasName("IX_MachineTypeName");
                // Relationship to MachineModel is configured below in MachineModel entity configuration
            });

            modelBuilder.Entity<SpecCodeDefinition>(entity =>
            {
                entity.HasIndex(scd => new { scd.SpecCodeNo, scd.SpecCodeBit, scd.ControlSystemId, scd.Category })
                      .IsUnique().HasName("IX_SpecCodeNoBitControlSystemCategory");
                entity.HasOne(scd => scd.ControlSystem)
                      .WithMany(cs => cs.SpecCodeDefinitions)
                      .HasForeignKey(scd => scd.ControlSystemId)
                      .IsRequired().OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SoftwareOptionHistory>(entity =>
            {
                entity.HasOne(h => h.SoftwareOption)
                      .WithMany(so => so.Histories)
                      .HasForeignKey(h => h.SoftwareOptionId)
                      .IsRequired().OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(h => new { h.SoftwareOptionId, h.Version })
                      .HasName("IX_SoftwareOptionHistory_SoftwareOptionId_Version").IsUnique(false);
            });

            modelBuilder.Entity<OptionNumberRegistry>(entity =>
            {
                entity.HasOne(onr => onr.SoftwareOption)
                      .WithMany(so => so.OptionNumberRegistries)
                      .HasForeignKey(onr => onr.SoftwareOptionId)
                      .IsRequired().OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SoftwareOption>(entity =>
            {
                entity.HasOne(so => so.ControlSystem)
                      .WithMany(cs => cs.SoftwareOptions)
                      .HasForeignKey(so => so.ControlSystemId)
                      .IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull);
                entity.HasMany(so => so.ParameterMappings)
                      .WithOne(pm => pm.SoftwareOption)
                      .HasForeignKey(pm => pm.SoftwareOptionId)
                      .IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<SoftwareOptionActivationRule>(entity =>
            {
                entity.HasOne(soar => soar.SoftwareOption)
                      .WithMany(so => so.SoftwareOptionActivationRules)
                      .HasForeignKey(soar => soar.SoftwareOptionId)
                      .IsRequired().OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SoftwareOptionSpecificationCode>(entity =>
            {
                entity.HasOne(sosc => sosc.SoftwareOption)
                      .WithMany(so => so.SoftwareOptionSpecificationCodes)
                      .HasForeignKey(sosc => sosc.SoftwareOptionId)
                      .IsRequired().OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(sosc => sosc.SpecCodeDefinition)
                      .WithMany(scd => scd.SoftwareOptionSpecificationCodes)
                      .HasForeignKey(sosc => sosc.SpecCodeDefinitionId)
                      .IsRequired().OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(sosc => sosc.SoftwareOptionActivationRule)
                      .WithMany(soar => soar.SoftwareOptionSpecificationCodes)
                      .HasForeignKey(sosc => sosc.SoftwareOptionActivationRuleId)
                      .IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Requirement>(entity =>
            {
                entity.HasOne(r => r.SoftwareOption)
                      .WithMany(so => so.Requirements)
                      .HasForeignKey(r => r.SoftwareOptionId)
                      .IsRequired().OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(r => r.RequiredSoftwareOption)
                      .WithMany(so => so.RequiredByOptions)
                      .HasForeignKey(r => r.RequiredSoftwareOptionId)
                      .IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull);
                entity.HasOne(r => r.RequiredSpecCodeDefinition)
                      .WithMany(scd => scd.Requirements)
                      .HasForeignKey(r => r.RequiredSpecCodeDefinitionId)
                      .IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<UserEntity>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.HasIndex(e => e.UserName).IsUnique();
                entity.Property(e => e.UserName).IsRequired().HasMaxLength(100);
                // Role property is now a collection, direct configuration here might change or be removed
                // if relying on the many-to-many configuration below.
            });

            // --- Configuration for RoleEntity ---
            modelBuilder.Entity<RoleEntity>(entity =>
            {
                entity.HasKey(r => r.RoleId);
                entity.HasIndex(r => r.RoleName).IsUnique();
                entity.Property(r => r.RoleName).IsRequired().HasMaxLength(100);

                // Configure the collection of ApplicationPermission enums
                entity.Property(e => e.Permissions)
                    .HasConversion(
                        v => string.Join(',', v.Select(p => p.ToString())), // Convert list of enums to comma-separated string
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                              .Select(s => Enum.Parse<RuleArchitect.Abstractions.Enums.ApplicationPermission>(s)).ToList() // Convert string back to list of enums
                    );
            });

            // --- UserEntity to RoleEntity (Many-to-Many) ---
            modelBuilder.Entity<UserEntity>()
                .HasMany(u => u.Roles)
                .WithMany(r => r.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRoles",
                    j => j.HasOne<RoleEntity>().WithMany().HasForeignKey("RoleId").OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<UserEntity>().WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId");
                        j.ToTable("UserRoles");
                    });


            // --- Configuration for MachineModel ---
            modelBuilder.Entity<MachineModel>(entity =>
            {
                entity.HasKey(mm => mm.MachineModelId);
                entity.HasIndex(mm => mm.Name).IsUnique().HasName("IX_MachineModelName");

                entity.HasOne(mm => mm.MachineType) // MachineModel has one MachineType
                      .WithMany(mt => mt.MachineModels) // MachineType has many MachineModels
                      .HasForeignKey(mm => mm.MachineTypeId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Restrict); // Prevent deleting MachineType if MachineModels are linked
            });

            // --- Updated Order and OrderItem Configurations ---
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasIndex(o => o.OrderNumber).IsUnique().HasName("IX_OrderNumber");

                entity.HasOne(o => o.ControlSystem)
                      .WithMany()
                      .HasForeignKey(o => o.ControlSystemId)
                      .IsRequired().OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(o => o.MachineModel) // <-- NEW Relationship Order to MachineModel
                      .WithMany() // Assuming MachineModel doesn't need ICollection<Order>
                      .HasForeignKey(o => o.MachineModelId)
                      .IsRequired() // If MachineModelId is not nullable
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(o => o.CreatedByUser).WithMany().HasForeignKey(o => o.CreatedByUserId)
                      .IsRequired().OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(o => o.OrderReviewerUser).WithMany().HasForeignKey(o => o.OrderReviewerUserId)
                      .IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull); // Or Restrict
                entity.HasOne(o => o.ProductionTechUser).WithMany().HasForeignKey(o => o.ProductionTechUserId)
                      .IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull); // Or Restrict
                entity.HasOne(o => o.SoftwareReviewerUser).WithMany().HasForeignKey(o => o.SoftwareReviewerUserId)
                      .IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull); // Or Restrict
                entity.HasOne(o => o.LastModifiedByUser).WithMany().HasForeignKey(o => o.LastModifiedByUserId)
                      .IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull); // Or Restrict

                entity.HasMany(o => o.OrderItems)
                      .WithOne(oi => oi.Order)
                      .HasForeignKey(oi => oi.OrderId)
                      .IsRequired().OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserActivityLog>(entity =>
            {
                entity.HasOne(ual => ual.User)
                      .WithMany()
                      .HasForeignKey(ual => ual.UserId)
                      .IsRequired().OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.Timestamp).HasName("IX_UserActivityLog_Timestamp");
                entity.HasIndex(e => e.UserId).HasName("IX_UserActivityLog_UserId");
                entity.HasIndex(e => e.ActivityType).HasName("IX_UserActivityLog_ActivityType");

                entity.HasIndex(e => new { e.TargetEntityType, e.TargetEntityId })
                      .HasName("IX_UserActivityLog_TargetEntityType_TargetEntity");
                entity.HasIndex(e => new { e.UserId, e.Timestamp })
                      .HasName("IX_UserActivityLog_User_Timestamp");

            });

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasIndex(oi => new { oi.OrderId, oi.SoftwareOptionId })
                      .IsUnique().HasName("IX_Order_SoftwareOption");
                entity.HasOne(oi => oi.SoftwareOption).WithMany()
                      .HasForeignKey(oi => oi.SoftwareOptionId)
                      .IsRequired().OnDelete(DeleteBehavior.Restrict);
            });

            // --- Seed Data for MachineType ---
            modelBuilder.Entity<MachineType>().HasData(
                new MachineType { MachineTypeId = 1, Name = "Lathe" },
                new MachineType { MachineTypeId = 2, Name = "Machining Center" },
                new MachineType { MachineTypeId = 3, Name = "Grinder" }
                // Add more machine types as needed, ensuring MachineTypeId is unique
            );

            // --- Seed Data For ControlSystem ---
            modelBuilder.Entity<ControlSystem>().HasData(
                new ControlSystem { ControlSystemId = 1, Name = "P300L", MachineTypeId = 1 }, // Links to Lathe
                new ControlSystem { ControlSystemId = 2, Name = "P300S", MachineTypeId = 1 }, // Links to Lathe
                new ControlSystem { ControlSystemId = 3, Name = "P300M", MachineTypeId = 2 }, // Links to Machining Center
                new ControlSystem { ControlSystemId = 4, Name = "E100M", MachineTypeId = 2 },
                new ControlSystem { ControlSystemId = 5, Name = "P200L", MachineTypeId = 1 },
                new ControlSystem { ControlSystemId = 6, Name = "P200M", MachineTypeId = 2 }
                // Add more control systems as needed
            );
            string adminSalt = "f9DAu0b2jcGAhuVKmgFYNw=="; // e.g., Convert.ToBase64String(new byte[16])
            string adminHash = "gHarxnybaF14pg0khiMv27IsdXuj2dmx0ytALdo5+aE="; // e.g., Hash of "DefaultAdminPassword123!" using the salt

            // --- Seed Administrator Role ---
            var adminRole = new RoleEntity
            {
                RoleId = 1,
                RoleName = "Administrator",
                Permissions = Enum.GetValues(typeof(RuleArchitect.Abstractions.Enums.ApplicationPermission)).Cast<RuleArchitect.Abstractions.Enums.ApplicationPermission>().ToList()
            };
            modelBuilder.Entity<RoleEntity>().HasData(adminRole);

            // --- Seed Admin User (update existing to remove direct Role string) ---
            modelBuilder.Entity<UserEntity>().HasData(
                new UserEntity
                {
                    UserId = 1,
                    UserName = "admin",
                    Email = "admin@example.com", // Ensure Email is provided if it's required
                    PasswordSalt = adminSalt,
                    PasswordHash = adminHash,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow, // Provide values for all required properties
                    UpdatedAt = DateTime.UtcNow
                    // Roles collection is linked via the UserRoles join table seeding below
                }
            );

            // --- Seed UserRoles join table entry ---
            modelBuilder.Entity<Dictionary<string, object>>("UserRoles").HasData(
                new { UserId = 1, RoleId = 1 }
            );
        }
    }
}