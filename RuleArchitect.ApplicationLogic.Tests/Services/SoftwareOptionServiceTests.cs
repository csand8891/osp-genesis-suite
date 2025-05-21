using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using RuleArchitect.ApplicationLogic.DTOs;
using RuleArchitect.ApplicationLogic.Services;
using RuleArchitect.Data;
using RuleArchitect.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MockQueryable.Moq;
using Microsoft.EntityFrameworkCore.Infrastructure; // Required for DatabaseFacade
using Microsoft.EntityFrameworkCore.Storage;    // Required for IDbContextTransaction

namespace RuleArchitect.ApplicationLogic.Tests.Services
{
    [TestFixture]
    public class SoftwareOptionServiceTests
    {
        private Mock<RuleArchitectContext> _mockContext;
        private SoftwareOptionService _service;

        // In-memory data lists
        private List<SoftwareOption> _softwareOptionsData;
        private List<ControlSystem> _controlSystemsData;
        private List<OptionNumberRegistry> _optionNumberRegistriesData;
        private List<Requirement> _requirementsData;
        private List<SoftwareOptionSpecificationCode> _softwareOptionSpecificationCodesData;
        private List<SoftwareOptionActivationRule> _softwareOptionActivationRulesData;
        private List<SoftwareOptionHistory> _softwareOptionHistoriesData;
        private List<SpecCodeDefinition> _specCodeDefinitionsData;

        // Mocks for DbSets
        private Mock<DbSet<SoftwareOption>> _mockSoftwareOptionsDbSet;
        private Mock<DbSet<ControlSystem>> _mockControlSystemsDbSet;
        private Mock<DbSet<OptionNumberRegistry>> _mockOptionNumberRegistriesDbSet;
        private Mock<DbSet<Requirement>> _mockRequirementsDbSet;
        private Mock<DbSet<SoftwareOptionSpecificationCode>> _mockSoftwareOptionSpecificationCodesDbSet;
        private Mock<DbSet<SoftwareOptionActivationRule>> _mockSoftwareOptionActivationRulesDbSet;
        private Mock<DbSet<SoftwareOptionHistory>> _mockSoftwareOptionHistoriesDbSet;
        private Mock<DbSet<SpecCodeDefinition>> _mockSpecCodeDefinitionsDbSet;

        private Mock<DatabaseFacade> _mockDatabaseFacade; // Declare mock for DatabaseFacade
        private Mock<IDbContextTransaction> _mockTransaction;

        [SetUp]
        public void Setup()
        {
            // IMPORTANT: We need to pass DbContextOptions to the RuleArchitectContext mock
            // if its constructor expects them, or ensure it has a parameterless constructor
            // for Moq to use if we are not providing constructor arguments.
            // Assuming RuleArchitectContext has a constructor that takes DbContextOptions,
            // or a parameterless one for Moq. If it ONLY takes options, you'd do:
            // var options = new DbContextOptions<RuleArchitectContext>();
            // _mockContext = new Mock<RuleArchitectContext>(options);
            // For simplicity, if your RuleArchitectContext has a parameterless constructor
            // (often added for migrations or simplified DI), Moq can use that.
            // If not, Moq might fail to instantiate RuleArchitectContext itself.
            // The error is about DatabaseFacade, so let's focus there first.
            _mockContext = new Mock<RuleArchitectContext>();


            // Initialize data lists
            _softwareOptionsData = new List<SoftwareOption>();
            _controlSystemsData = new List<ControlSystem>();
            _optionNumberRegistriesData = new List<OptionNumberRegistry>();
            _requirementsData = new List<Requirement>();
            _softwareOptionSpecificationCodesData = new List<SoftwareOptionSpecificationCode>();
            _softwareOptionActivationRulesData = new List<SoftwareOptionActivationRule>();
            _softwareOptionHistoriesData = new List<SoftwareOptionHistory>();
            _specCodeDefinitionsData = new List<SpecCodeDefinition>();

            // Build MockDbSets
            _mockSoftwareOptionsDbSet = _softwareOptionsData.AsQueryable().BuildMockDbSet();
            _mockControlSystemsDbSet = _controlSystemsData.AsQueryable().BuildMockDbSet();
            _mockOptionNumberRegistriesDbSet = _optionNumberRegistriesData.AsQueryable().BuildMockDbSet();
            _mockRequirementsDbSet = _requirementsData.AsQueryable().BuildMockDbSet();
            _mockSoftwareOptionSpecificationCodesDbSet = _softwareOptionSpecificationCodesData.AsQueryable().BuildMockDbSet();
            _mockSoftwareOptionActivationRulesDbSet = _softwareOptionActivationRulesData.AsQueryable().BuildMockDbSet();
            _mockSoftwareOptionHistoriesDbSet = _softwareOptionHistoriesData.AsQueryable().BuildMockDbSet();
            _mockSpecCodeDefinitionsDbSet = _specCodeDefinitionsData.AsQueryable().BuildMockDbSet();

            // Setup DbContext to return mocks for DbSets
            _mockContext.Setup(c => c.SoftwareOptions).Returns(_mockSoftwareOptionsDbSet.Object);
            _mockContext.Setup(c => c.ControlSystems).Returns(_mockControlSystemsDbSet.Object);
            _mockContext.Setup(c => c.OptionNumberRegistries).Returns(_mockOptionNumberRegistriesDbSet.Object);
            _mockContext.Setup(c => c.Requirements).Returns(_mockRequirementsDbSet.Object);
            _mockContext.Setup(c => c.SoftwareOptionSpecificationCodes).Returns(_mockSoftwareOptionSpecificationCodesDbSet.Object);
            _mockContext.Setup(c => c.SoftwareOptionActivationRules).Returns(_mockSoftwareOptionActivationRulesDbSet.Object);
            _mockContext.Setup(c => c.SoftwareOptionHistories).Returns(_mockSoftwareOptionHistoriesDbSet.Object);
            _mockContext.Setup(c => c.SpecCodeDefinitions).Returns(_mockSpecCodeDefinitionsDbSet.Object);

            // Setup Add/AddRange callbacks
            SetupAddOperations(_mockSoftwareOptionsDbSet, _softwareOptionsData);
            SetupAddOperations(_mockControlSystemsDbSet, _controlSystemsData);
            SetupAddOperations(_mockOptionNumberRegistriesDbSet, _optionNumberRegistriesData);
            SetupAddOperations(_mockRequirementsDbSet, _requirementsData);
            SetupAddOperations(_mockSoftwareOptionSpecificationCodesDbSet, _softwareOptionSpecificationCodesData);
            SetupAddOperations(_mockSoftwareOptionActivationRulesDbSet, _softwareOptionActivationRulesData);
            SetupAddOperations(_mockSoftwareOptionHistoriesDbSet, _softwareOptionHistoriesData);

            // Setup Remove/RemoveRange callbacks
            SetupRemoveOperations(_mockSoftwareOptionsDbSet, _softwareOptionsData);
            SetupRemoveOperations(_mockOptionNumberRegistriesDbSet, _optionNumberRegistriesData);
            SetupRemoveOperations(_mockRequirementsDbSet, _requirementsData);
            SetupRemoveOperations(_mockSoftwareOptionSpecificationCodesDbSet, _softwareOptionSpecificationCodesData);
            SetupRemoveOperations(_mockSoftwareOptionActivationRulesDbSet, _softwareOptionActivationRulesData);

            // --- FIX FOR DatabaseFacade ERROR ---
            // 1. Create a mock DbContext to pass to DatabaseFacade constructor.
            //    This inner mock context doesn't need extensive setup, just needs to be a DbContext.
            var mockInnerDbContext = new Mock<DbContext>(new DbContextOptions<DbContext>());

            // 2. Instantiate Mock<DatabaseFacade> passing the mock DbContext object.
            _mockDatabaseFacade = new Mock<DatabaseFacade>(mockInnerDbContext.Object);

            // 3. Setup the Database property on _mockContext to return our _mockDatabaseFacade.Object
            _mockContext.Setup(c => c.Database).Returns(_mockDatabaseFacade.Object);
            // --- END FIX ---

            // Mock Transaction: Now setup BeginTransactionAsync on the _mockDatabaseFacade
            _mockTransaction = new Mock<IDbContextTransaction>();
            _mockDatabaseFacade.Setup(db => db.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mockTransaction.Object);
            // CommitAsync and RollbackAsync are called on the _mockTransaction object itself,
            // so those setups on _mockTransaction (if you add them directly) remain valid.

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                        .ReturnsAsync(1);

            _service = new SoftwareOptionService(_mockContext.Object);
        }

        // (Helper methods SetupAddOperations and SetupRemoveOperations remain the same)
        private void SetupAddOperations<TEntity>(Mock<DbSet<TEntity>> mockDbSet, List<TEntity> dataList) where TEntity : class
        {
            mockDbSet.Setup(x => x.Add(It.IsAny<TEntity>()))
                .Callback<TEntity>(entity => dataList.Add(entity));
            mockDbSet.Setup(x => x.AddRange(It.IsAny<IEnumerable<TEntity>>()))
                .Callback<IEnumerable<TEntity>>(entities => dataList.AddRange(entities));
        }

        private void SetupRemoveOperations<TEntity>(Mock<DbSet<TEntity>> mockDbSet, List<TEntity> dataList) where TEntity : class
        {
            mockDbSet.Setup(x => x.Remove(It.IsAny<TEntity>()))
                .Callback<TEntity>(entity => dataList.Remove(entity));
            mockDbSet.Setup(x => x.RemoveRange(It.IsAny<IEnumerable<TEntity>>()))
                .Callback<IEnumerable<TEntity>>(entities => {
                    foreach (var entity in entities.ToList())
                    {
                        dataList.Remove(entity);
                    }
                });
        }

        // ... ALL YOUR TEST METHODS (GetAllSoftwareOptionsAsync_WhenNoOptionsExist_ReturnsEmptyList, etc.) ...
        // No changes should be needed in the test methods themselves for this specific fix.

        // Example test that uses transaction (UpdateSoftwareOptionAsync_WhenOptionExists_UpdatesOptionAndAddsHistory)
        // This test should now pass with the DatabaseFacade fix.
        [Test]
        public async Task UpdateSoftwareOptionAsync_WhenOptionExists_UpdatesOptionAndAddsHistory()
        {
            // Arrange
            string currentUser = "UpdateUser";
            int existingOptionId = 1;
            var initialOption = new SoftwareOption
            {
                SoftwareOptionId = existingOptionId,
                PrimaryName = "Old Name",
                Version = 1,
                LastModifiedBy = "OldUser",
                LastModifiedDate = DateTime.UtcNow.AddDays(-1),
                OptionNumberRegistries = new List<OptionNumberRegistry> { new OptionNumberRegistry { OptionNumberRegistryId = 1, OptionNumber = "OLD_ON", SoftwareOptionId = existingOptionId } },
                Requirements = new List<Requirement>()
            };
            _softwareOptionsData.Add(initialOption);
            if (initialOption.OptionNumberRegistries != null) _optionNumberRegistriesData.AddRange(initialOption.OptionNumberRegistries);


            var updateCommand = new UpdateSoftwareOptionCommandDto
            {
                SoftwareOptionId = existingOptionId,
                PrimaryName = "New Updated Name",
                AlternativeNames = "New Alt Name",
                Notes = "Updated notes",
                OptionNumbers = new List<OptionNumberRegistryCreateDto> { new OptionNumberRegistryCreateDto { OptionNumber = "NEW_ON123" } },
                Requirements = new List<RequirementCreateDto> { new RequirementCreateDto { RequirementType = "UpdatedType", GeneralRequiredValue = "UpdatedValue" } }
            };

            // Act
            var updatedOption = await _service.UpdateSoftwareOptionAsync(updateCommand, currentUser);

            // Assert
            Assert.That(updatedOption, Is.Not.Null);
            Assert.That(updatedOption.PrimaryName, Is.EqualTo("New Updated Name"));
            Assert.That(updatedOption.Version, Is.EqualTo(2));
            Assert.That(updatedOption.LastModifiedBy, Is.EqualTo(currentUser));

            Assert.That(_optionNumberRegistriesData.Count, Is.EqualTo(1));
            Assert.That(_optionNumberRegistriesData.First().OptionNumber, Is.EqualTo("NEW_ON123"));

            Assert.That(_requirementsData.Count, Is.EqualTo(1));
            Assert.That(_requirementsData.First().RequirementType, Is.EqualTo("UpdatedType"));

            Assert.That(_softwareOptionHistoriesData.Count, Is.EqualTo(1));
            var historyRecord = _softwareOptionHistoriesData.Last();
            Assert.That(historyRecord.Version, Is.EqualTo(2));
            Assert.That(historyRecord.PrimaryName, Is.EqualTo("New Updated Name"));
            Assert.That(historyRecord.ChangedBy, Is.EqualTo(currentUser));

            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
            _mockTransaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        // (The rest of your tests from the previous response)
        [Test]
        public async Task GetAllSoftwareOptionsAsync_WhenNoOptionsExist_ReturnsEmptyList()
        {
            // Act
            var result = await _service.GetAllSoftwareOptionsAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetAllSoftwareOptionsAsync_WhenOptionsExist_ReturnsAllOptions()
        {
            // Arrange
            var controlSystem = new ControlSystem { ControlSystemId = 1, Name = "CS1", MachineTypeId = 1 };
            _controlSystemsData.Add(controlSystem);

            var option1 = new SoftwareOption { SoftwareOptionId = 1, PrimaryName = "Option 1", Version = 1, LastModifiedDate = DateTime.UtcNow, ControlSystemId = 1, ControlSystem = controlSystem };
            var option2 = new SoftwareOption { SoftwareOptionId = 2, PrimaryName = "Option 2", Version = 1, LastModifiedDate = DateTime.UtcNow, ControlSystemId = 1, ControlSystem = controlSystem };
            _softwareOptionsData.AddRange(new List<SoftwareOption> { option1, option2 });

            // Act
            var result = await _service.GetAllSoftwareOptionsAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.Any(so => so.PrimaryName == "Option 1"));
            Assert.That(result.Any(so => so.PrimaryName == "Option 2"));
        }


        [Test]
        public async Task GetSoftwareOptionByIdAsync_WhenOptionExists_ReturnsOptionWithDetails()
        {
            // Arrange
            var controlSystem = new ControlSystem { ControlSystemId = 1, Name = "CS1", MachineTypeId = 1 };
            _controlSystemsData.Add(controlSystem);

            var expectedOption = new SoftwareOption
            {
                SoftwareOptionId = 10,
                PrimaryName = "Specific Option",
                Version = 1,
                LastModifiedDate = DateTime.UtcNow,
                ControlSystemId = 1,
                ControlSystem = controlSystem,
                OptionNumberRegistries = new List<OptionNumberRegistry> { new OptionNumberRegistry { OptionNumberRegistryId = 1, OptionNumber = "Opt123" } },
                Requirements = new List<Requirement> { new Requirement { RequirementId = 1, RequirementType = "TypeA" } }
            };
            _softwareOptionsData.Add(expectedOption);
            if (expectedOption.OptionNumberRegistries != null) _optionNumberRegistriesData.AddRange(expectedOption.OptionNumberRegistries);
            if (expectedOption.Requirements != null) _requirementsData.AddRange(expectedOption.Requirements);


            // Act
            var result = await _service.GetSoftwareOptionByIdAsync(10);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.SoftwareOptionId, Is.EqualTo(expectedOption.SoftwareOptionId));
            Assert.That(result.PrimaryName, Is.EqualTo(expectedOption.PrimaryName));
            Assert.That(result.ControlSystem, Is.Not.Null);
            Assert.That(result.ControlSystem.Name, Is.EqualTo("CS1"));
            Assert.That(result.OptionNumberRegistries, Is.Not.Empty);
            Assert.That(result.Requirements, Is.Not.Empty);
        }

        [Test]
        public async Task GetSoftwareOptionByIdAsync_WhenOptionDoesNotExist_ReturnsNull()
        {
            // Act
            var result = await _service.GetSoftwareOptionByIdAsync(999);

            // Assert
            Assert.That(result, Is.Null);
        }


        [Test]
        public async Task CreateSoftwareOptionAsync_WithValidData_CreatesAndReturnsOptionWithHistory()
        {
            // Arrange
            string currentUser = "TestUser";
            var specDef = new SpecCodeDefinition { SpecCodeDefinitionId = 1, SpecCodeNo = "S01", SpecCodeBit = "B01", Category = "Cat1", MachineTypeId = 1 };
            _specCodeDefinitionsData.Add(specDef);

            var createCommand = new CreateSoftwareOptionCommandDto
            {
                PrimaryName = "New Option",
                AlternativeNames = "AltName",
                ControlSystemId = 1,
                OptionNumbers = new List<OptionNumberRegistryCreateDto> { new OptionNumberRegistryCreateDto { OptionNumber = "ON123" } },
                Requirements = new List<RequirementCreateDto> { new RequirementCreateDto { RequirementType = "Type1", GeneralRequiredValue = "Val1" } },
                SpecificationCodes = new List<SoftwareOptionSpecificationCodeCreateDto> { new SoftwareOptionSpecificationCodeCreateDto { SpecCodeDefinitionId = 1, SpecificInterpretation = "Interp1" } },
                ActivationRules = new List<SoftwareOptionActivationRuleCreateDto> { new SoftwareOptionActivationRuleCreateDto { RuleName = "Rule1", ActivationSetting = "Setting1" } }
            };
            _controlSystemsData.Add(new ControlSystem { ControlSystemId = 1, Name = "CS1", MachineTypeId = 1 });


            // Act
            var createdOption = await _service.CreateSoftwareOptionAsync(createCommand, currentUser);

            // Assert
            Assert.That(createdOption, Is.Not.Null);
            Assert.That(createdOption.PrimaryName, Is.EqualTo("New Option"));
            Assert.That(_softwareOptionsData.Count, Is.EqualTo(1));
            Assert.That(_optionNumberRegistriesData.Count, Is.EqualTo(1));
            Assert.That(_optionNumberRegistriesData.First().OptionNumber, Is.EqualTo("ON123"));
            Assert.That(_requirementsData.Count, Is.EqualTo(1));
            Assert.That(_softwareOptionSpecificationCodesData.Count, Is.EqualTo(1));
            Assert.That(_softwareOptionActivationRulesData.Count, Is.EqualTo(1));
            Assert.That(_softwareOptionHistoriesData.Count, Is.EqualTo(1));
            Assert.That(_softwareOptionHistoriesData.First().Version, Is.EqualTo(1));
            Assert.That(_softwareOptionHistoriesData.First().ChangedBy, Is.EqualTo(currentUser));

            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Test]
        public async Task UpdateSoftwareOptionAsync_WhenOptionNotFound_ReturnsNull()
        {
            // Arrange
            var updateCommand = new UpdateSoftwareOptionCommandDto { SoftwareOptionId = 999, PrimaryName = "NonExistent" };
            string currentUser = "TestUser";

            // Act
            var result = await _service.UpdateSoftwareOptionAsync(updateCommand, currentUser);

            // Assert
            Assert.That(result, Is.Null);
            _mockTransaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never());
        }

        [Test]
        public async Task GetSoftwareOptionHistoryAsync_WhenHistoryExists_ReturnsHistoryList()
        {
            // Arrange
            int targetSoftwareOptionId = 1;
            _softwareOptionsData.Add(new SoftwareOption { SoftwareOptionId = targetSoftwareOptionId, PrimaryName = "OptionWithHistory" });
            _softwareOptionHistoriesData.AddRange(new List<SoftwareOptionHistory>
            {
                new SoftwareOptionHistory { SoftwareOptionHistoryId = 1, SoftwareOptionId = targetSoftwareOptionId, Version = 1, PrimaryName = "V1", ChangeTimestamp = DateTime.UtcNow.AddDays(-2) },
                new SoftwareOptionHistory { SoftwareOptionHistoryId = 2, SoftwareOptionId = targetSoftwareOptionId, Version = 2, PrimaryName = "V2", ChangeTimestamp = DateTime.UtcNow.AddDays(-1) },
                new SoftwareOptionHistory { SoftwareOptionHistoryId = 3, SoftwareOptionId = 2, Version = 1, PrimaryName = "OtherHistory", ChangeTimestamp = DateTime.UtcNow }
            });

            // Act
            var result = await _service.GetSoftwareOptionHistoryAsync(targetSoftwareOptionId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.All(h => h.SoftwareOptionId == targetSoftwareOptionId));
            Assert.That(result.First().Version, Is.EqualTo(2));
            Assert.That(result.Last().Version, Is.EqualTo(1));
        }

        [Test]
        public async Task GetSoftwareOptionHistoryAsync_WhenNoHistoryExists_ReturnsEmptyList()
        {
            // Arrange
            int targetSoftwareOptionId = 1;
            _softwareOptionsData.Add(new SoftwareOption { SoftwareOptionId = targetSoftwareOptionId, PrimaryName = "OptionNoHistory" });

            // Act
            var result = await _service.GetSoftwareOptionHistoryAsync(targetSoftwareOptionId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task DeleteSoftwareOptionAsync_WhenOptionExists_RemovesOptionAndReturnsTrue()
        {
            // Arrange
            int optionIdToDelete = 1;
            _softwareOptionsData.Add(new SoftwareOption { SoftwareOptionId = optionIdToDelete, PrimaryName = "ToDelete" });

            // Act
            var result = await _service.DeleteSoftwareOptionAsync(optionIdToDelete);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(_softwareOptionsData.Any(so => so.SoftwareOptionId == optionIdToDelete), Is.False);
            _mockSoftwareOptionsDbSet.Verify(m => m.Remove(It.Is<SoftwareOption>(so => so.SoftwareOptionId == optionIdToDelete)), Times.Once());
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Test]
        public async Task DeleteSoftwareOptionAsync_WhenOptionNotFound_ReturnsFalse()
        {
            // Arrange
            int nonExistentOptionId = 999;

            // Act
            var result = await _service.DeleteSoftwareOptionAsync(nonExistentOptionId);

            // Assert
            Assert.That(result, Is.False);
            _mockSoftwareOptionsDbSet.Verify(m => m.Remove(It.IsAny<SoftwareOption>()), Times.Never());
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());
        }

        [Test]
        public async Task DeleteSoftwareOptionAsync_WhenSaveChangesFails_ReturnsFalse()
        {
            // Arrange
            int optionIdToDelete = 1;
            _softwareOptionsData.Add(new SoftwareOption { SoftwareOptionId = optionIdToDelete, PrimaryName = "ToDelete" });

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new DbUpdateException("Simulated DB error"));

            // Act
            var result = await _service.DeleteSoftwareOptionAsync(optionIdToDelete);

            // Assert
            Assert.That(result, Is.False);
            // The item might have been "removed" from the DbSet's perspective before SaveChangesAsync
            // but it would still be in the in-memory list because the callback for Remove happened.
            // The service catches the exception and returns false. The actual state of the in-memory list
            // depends on whether you want to simulate the EF Core behavior of an attempted remove or not.
            // For this test, verifying the service returns false is the main goal.
            _mockSoftwareOptionsDbSet.Verify(m => m.Remove(It.Is<SoftwareOption>(so => so.SoftwareOptionId == optionIdToDelete)), Times.Once());
        }

    }
}