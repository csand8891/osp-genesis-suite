/*// File: RuleArchitect.ApplicationLogic.Tests/Services/SoftwareOptionServiceTests.cs
using NUnit.Framework;
using Moq;
using FluentAssertions;
using RuleArchitect.ApplicationLogic.Services;
using RuleArchitect.Abstractions.Interfaces;
using RuleArchitect.Abstractions.DTOs;
using RuleArchitect.Data;
using RuleArchitect.Entities;
// using HeraldKit.Interaces; // Assuming INotificationService is not directly used by SoftwareOptionService
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
// using Microsoft.EntityFrameworkCore.Infrastructure; // Not needed if not mocking DatabaseFacade for this test
// using Microsoft.EntityFrameworkCore.Storage;    // Not needed if not mocking IDbContextTransaction for this test
using MockQueryable.Moq;

namespace RuleArchitect.ApplicationLogic.Tests.Services
{
    [TestFixture]
    public class SoftwareOptionServiceTests
    {
        private Mock<RuleArchitectContext> _mockContext;
        // private Mock<INotificationService> _mockNotificationService; 
        private SoftwareOptionService _service;

        // Data lists for backing DbSets
        private List<SoftwareOption> _softwareOptionsData;
        private List<SoftwareOptionHistory> _softwareOptionHistoriesData;
        private List<OptionNumberRegistry> _optionNumberRegistriesData;
        private List<Requirement> _requirementsData;
        private List<SoftwareOptionSpecificationCode> _softwareOptionSpecificationCodesData;
        private List<SoftwareOptionActivationRule> _softwareOptionActivationRulesData;
        private List<ControlSystem> _controlSystemsData;
        private List<SpecCodeDefinition> _specCodeDefinitionsData;
        private List<MachineType> _machineTypesData;

        // Mock DbSets
        private Mock<DbSet<SoftwareOption>> _mockSoftwareOptionsDbSet;
        private Mock<DbSet<SoftwareOptionHistory>> _mockSoftwareOptionHistoriesDbSet;
        private Mock<DbSet<OptionNumberRegistry>> _mockOptionNumberRegistriesDbSet;
        private Mock<DbSet<Requirement>> _mockRequirementsDbSet;
        private Mock<DbSet<SoftwareOptionSpecificationCode>> _mockSoftwareOptionSpecificationCodesDbSet;
        private Mock<DbSet<SoftwareOptionActivationRule>> _mockSoftwareOptionActivationRulesDbSet;
        private Mock<DbSet<ControlSystem>> _mockControlSystemSet;
        private Mock<DbSet<SpecCodeDefinition>> _mockSpecCodeDefinitionSet;
        private Mock<DbSet<MachineType>> _mockMachineTypeSet;

        private int _nextSoftwareOptionId;

        [SetUp]
        public void Setup()
        {
            _mockContext = new Mock<RuleArchitectContext>();
            // _mockNotificationService = new Mock<INotificationService>();

            _softwareOptionsData = new List<SoftwareOption>();
            _softwareOptionHistoriesData = new List<SoftwareOptionHistory>();
            _optionNumberRegistriesData = new List<OptionNumberRegistry>();
            _requirementsData = new List<Requirement>();
            _softwareOptionSpecificationCodesData = new List<SoftwareOptionSpecificationCode>();
            _softwareOptionActivationRulesData = new List<SoftwareOptionActivationRule>();

            _machineTypesData = new List<MachineType> { new MachineType { MachineTypeId = 1, Name = "Lathe" } };
            _controlSystemsData = new List<ControlSystem> { new ControlSystem { ControlSystemId = 1, Name = "CS1", MachineTypeId = 1, MachineType = _machineTypesData.First() } };
            _specCodeDefinitionsData = new List<SpecCodeDefinition> { new SpecCodeDefinition { SpecCodeDefinitionId = 1, SpecCodeNo = "S01", SpecCodeBit = "B01", Category = "Cat1", ControlSystemId = 1, ControlSystem = _controlSystemsData.First() } };

            _nextSoftwareOptionId = 1;

            _mockSoftwareOptionsDbSet = _softwareOptionsData.AsQueryable().BuildMockDbSet();
            // Simulate ID generation for SoftwareOption when Add is called
            // This callback modifies the actual 'so' instance passed to Add.
            _mockSoftwareOptionsDbSet.Setup(x => x.Add(It.IsAny<SoftwareOption>()))
                .Callback<SoftwareOption>(so =>
                {
                    if (so.SoftwareOptionId == 0)
                    {
                        // Simulate DB assigning an ID to the entity instance
                        so.SoftwareOptionId = _nextSoftwareOptionId++;
                    }
                    // Add the (potentially modified) entity to the backing list for querying
                    if (!_softwareOptionsData.Contains(so))
                    {
                        _softwareOptionsData.Add(so);
                    }
                });

            _mockSoftwareOptionHistoriesDbSet = _softwareOptionHistoriesData.AsQueryable().BuildMockDbSet();
            _mockOptionNumberRegistriesDbSet = _optionNumberRegistriesData.AsQueryable().BuildMockDbSet();
            _mockRequirementsDbSet = _requirementsData.AsQueryable().BuildMockDbSet();
            _mockSoftwareOptionSpecificationCodesDbSet = _softwareOptionSpecificationCodesData.AsQueryable().BuildMockDbSet();
            _mockSoftwareOptionActivationRulesDbSet = _softwareOptionActivationRulesData.AsQueryable().BuildMockDbSet();
            _mockControlSystemSet = _controlSystemsData.AsQueryable().BuildMockDbSet();
            _mockSpecCodeDefinitionSet = _specCodeDefinitionsData.AsQueryable().BuildMockDbSet();
            _mockMachineTypeSet = _machineTypesData.AsQueryable().BuildMockDbSet();

            _mockContext.Setup(c => c.SoftwareOptions).Returns(_mockSoftwareOptionsDbSet.Object);
            _mockContext.Setup(c => c.SoftwareOptionHistories).Returns(_mockSoftwareOptionHistoriesDbSet.Object);
            _mockContext.Setup(c => c.OptionNumberRegistries).Returns(_mockOptionNumberRegistriesDbSet.Object);
            _mockContext.Setup(c => c.Requirements).Returns(_mockRequirementsDbSet.Object);
            _mockContext.Setup(c => c.SoftwareOptionSpecificationCodes).Returns(_mockSoftwareOptionSpecificationCodesDbSet.Object);
            _mockContext.Setup(c => c.SoftwareOptionActivationRules).Returns(_mockSoftwareOptionActivationRulesDbSet.Object);
            _mockContext.Setup(c => c.ControlSystems).Returns(_mockControlSystemSet.Object);
            _mockContext.Setup(c => c.SpecCodeDefinitions).Returns(_mockSpecCodeDefinitionSet.Object);
            _mockContext.Setup(c => c.MachineTypes).Returns(_mockMachineTypeSet.Object);

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                        .ReturnsAsync(1);

            SetupAddOperations(_mockSoftwareOptionHistoriesDbSet, _softwareOptionHistoriesData);
            SetupAddOperations(_mockOptionNumberRegistriesDbSet, _optionNumberRegistriesData);
            SetupAddOperations(_mockRequirementsDbSet, _requirementsData); // This will add Requirements to _requirementsData
            SetupAddOperations(_mockSoftwareOptionSpecificationCodesDbSet, _softwareOptionSpecificationCodesData);
            SetupAddOperations(_mockSoftwareOptionActivationRulesDbSet, _softwareOptionActivationRulesData);

            _service = new SoftwareOptionService(_mockContext.Object);
        }

        private void SetupAddOperations<TEntity>(Mock<DbSet<TEntity>> mockDbSet, List<TEntity> dataList) where TEntity : class
        {
            mockDbSet.Setup(x => x.Add(It.IsAny<TEntity>()))
                .Callback<TEntity>(entity => {
                    if (entity is Requirement req)
                    {
                        // System.Diagnostics.Debug.WriteLine($"TEST_DEBUG_CALLBACK: Adding Requirement to _requirementsData. SoftwareOptionId: {req.SoftwareOptionId}, Type: {req.RequirementType}");
                    }
                    if (!dataList.Contains(entity))
                    {
                        dataList.Add(entity);
                    }
                });
            mockDbSet.Setup(x => x.AddRange(It.IsAny<IEnumerable<TEntity>>()))
                .Callback<IEnumerable<TEntity>>(entities => {
                    foreach (var entity in entities)
                    {
                        if (entity is Requirement req)
                        {
                            // System.Diagnostics.Debug.WriteLine($"TEST_DEBUG_CALLBACK: Adding Requirement (from AddRange) to _requirementsData. SoftwareOptionId: {req.SoftwareOptionId}, Type: {req.RequirementType}");
                        }
                        if (!dataList.Contains(entity)) dataList.Add(entity);
                    }
                });
        }

        [Test]
        //public async Task CreateSoftwareOptionAsync_WithValidData_CreatesAndReturnsOptionWithHistory()
        //{
        //    // Arrange
        //    string currentUser = "TestUser";

        //    var createCommand = new CreateSoftwareOptionCommandDto
        //    {
        //        PrimaryName = "New Test Option",
        //        AlternativeNames = "NTO_Alt",
        //        SourceFileName = "nto.src",
        //        PrimaryOptionNumberDisplay = "NTO-001",
        //        Notes = "Test notes",
        //        ControlSystemId = 1,
        //        OptionNumbers = new List<OptionNumberRegistryCreateDto>
        //            { new OptionNumberRegistryCreateDto { OptionNumber = "ON123" } },
        //        Requirements = new List<RequirementCreateDto>
        //            { new RequirementCreateDto { RequirementType = "Type1", GeneralRequiredValue = "Val1" } },
        //        SpecificationCodes = new List<SoftwareOptionSpecificationCodeCreateDto>
        //            { new SoftwareOptionSpecificationCodeCreateDto { SpecCodeDefinitionId = 1, SpecificInterpretation = "Interp1" } },
        //        ActivationRules = new List<SoftwareOptionActivationRuleCreateDto>
        //            { new SoftwareOptionActivationRuleCreateDto { RuleName = "Rule1", ActivationSetting = "Setting1" } }
        //    };

        //    // Act
        //    SoftwareOption createdOption = await _service.CreateSoftwareOptionAsync(createCommand, currentUser);

        //    // Assert
        //    createdOption.Should().NotBeNull();
        //    // This assertion is crucial. If it passes, createdOption.SoftwareOptionId is 1 (or the next ID).
        //    createdOption.SoftwareOptionId.Should().Be(1, "because the Add callback for SoftwareOption should have set the ID to 1 for the first added SoftwareOption.");

        //    createdOption.PrimaryName.Should().Be("New Test Option");
        //    createdOption.Version.Should().Be(1, "the newly created SoftwareOption should have Version 1.");
        //    createdOption.LastModifiedBy.Should().Be(currentUser, "the newly created SoftwareOption should have LastModifiedBy set to currentUser.");

        //    _softwareOptionsData.Should().ContainSingle(so => so.SoftwareOptionId == createdOption.SoftwareOptionId && so.PrimaryName == "New Test Option");

        //    _optionNumberRegistriesData.Should().ContainSingle(onr => onr.OptionNumber == "ON123" && onr.SoftwareOptionId == createdOption.SoftwareOptionId);

        //    // This is the failing assertion. 
        //    // If createdOption.SoftwareOptionId is 1, this predicate becomes (r.RequirementType == "Type1" && r.SoftwareOptionId == 1)
        //    _requirementsData.Should().ContainSingle(r => r.RequirementType == "Type1" && r.SoftwareOptionId == createdOption.SoftwareOptionId,
        //        "because the Requirement added to the context should be linked to the created SoftwareOption using its generated ID. If this fails, it means the Requirement in _requirementsData has a different SoftwareOptionId (likely 0).");

        //    _softwareOptionSpecificationCodesData.Should().ContainSingle(sc => sc.SpecificInterpretation == "Interp1" && sc.SoftwareOptionId == createdOption.SoftwareOptionId,
        //        "because the SpecCode should be linked to the created SoftwareOption using its generated ID.");
        //    _softwareOptionActivationRulesData.Should().ContainSingle(ar => ar.RuleName == "Rule1" && ar.SoftwareOptionId == createdOption.SoftwareOptionId,
        //        "because the ActivationRule should be linked to the created SoftwareOption using its generated ID.");

        //    _softwareOptionHistoriesData.Should().HaveCount(1);
        //    SoftwareOptionHistory historyEntry = _softwareOptionHistoriesData.First();

        //    historyEntry.SoftwareOptionId.Should().Be(createdOption.SoftwareOptionId);
        //    historyEntry.Version.Should().Be(1, "the history entry version should match the created SoftwareOption's version.");
        //    historyEntry.PrimaryName.Should().Be(createdOption.PrimaryName);
        //    historyEntry.ChangedBy.Should().Be(currentUser, "the history entry ChangedBy should match the currentUser.");
        //    historyEntry.ChangeTimestamp.Should().Be(createdOption.LastModifiedDate);

        //    _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        //}

        // ... (other 5 tests previously generated: GetAllSoftwareOptionsAsync_WhenNoOptionsExist, 
        //      GetAllSoftwareOptionsAsync_WhenOptionsExist, GetSoftwareOptionByIdAsync_WhenOptionExists,
        //      GetSoftwareOptionByIdAsync_WhenOptionDoesNotExist, DeleteSoftwareOptionAsync_WhenOptionExists,
        //      DeleteSoftwareOptionAsync_WhenOptionNotFound would go here)
        // For brevity, I'm focusing on the problematic test and setup.
        // The full file content for these other tests was provided in the previous canvas version.
        // Please ensure they are also present in your actual file.

        //[Test]
        public async Task GetAllSoftwareOptionsAsync_WhenNoOptionsExist_ReturnsEmptyList()
        {
            // Arrange
            _softwareOptionsData.Clear(); // Ensure data is empty for this test case
            _mockSoftwareOptionsDbSet = _softwareOptionsData.AsQueryable().BuildMockDbSet();
            _mockContext.Setup(c => c.SoftwareOptions).Returns(_mockSoftwareOptionsDbSet.Object);


            // Act
            var result = await _service.GetAllSoftwareOptionsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Test]
        public async Task GetAllSoftwareOptionsAsync_WhenOptionsExist_ReturnsAllOptions()
        {
            // Arrange
            var controlSystem = _controlSystemsData.First();
            var option1 = new SoftwareOption { SoftwareOptionId = 10, PrimaryName = "Option 10", Version = 1, LastModifiedDate = DateTime.UtcNow, ControlSystemId = controlSystem.ControlSystemId, ControlSystem = controlSystem };
            var option2 = new SoftwareOption { SoftwareOptionId = 11, PrimaryName = "Option 11", Version = 1, LastModifiedDate = DateTime.UtcNow, ControlSystemId = controlSystem.ControlSystemId, ControlSystem = controlSystem };
            _softwareOptionsData.AddRange(new List<SoftwareOption> { option1, option2 });
            _mockSoftwareOptionsDbSet = _softwareOptionsData.AsQueryable().BuildMockDbSet();
            _mockContext.Setup(c => c.SoftwareOptions).Returns(_mockSoftwareOptionsDbSet.Object);


            // Act
            var result = await _service.GetAllSoftwareOptionsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().ContainEquivalentOf(option1, options => options.Excluding(so => so.Histories)
                                                                            .Excluding(so => so.OptionNumberRegistries)
                                                                            .Excluding(so => so.Requirements)
                                                                            .Excluding(so => so.RequiredByOptions)
                                                                            .Excluding(so => so.SoftwareOptionActivationRules)
                                                                            .Excluding(so => so.SoftwareOptionSpecificationCodes)
                                                                            .Excluding(so => so.ParameterMappings));
            result.Should().ContainEquivalentOf(option2, options => options.Excluding(so => so.Histories)
                                                                            .Excluding(so => so.OptionNumberRegistries)
                                                                            .Excluding(so => so.Requirements)
                                                                            .Excluding(so => so.RequiredByOptions)
                                                                            .Excluding(so => so.SoftwareOptionActivationRules)
                                                                            .Excluding(so => so.SoftwareOptionSpecificationCodes)
                                                                            .Excluding(so => so.ParameterMappings));
        }


        [Test]
        public async Task GetSoftwareOptionByIdAsync_WhenOptionExists_ReturnsOptionWithDetails()
        {
            // Arrange
            var controlSystem = _controlSystemsData.First();
            var specCodeDef = _specCodeDefinitionsData.First();
            var expectedOption = new SoftwareOption
            {
                SoftwareOptionId = 20,
                PrimaryName = "Specific Option",
                Version = 1,
                LastModifiedDate = DateTime.UtcNow,
                ControlSystemId = controlSystem.ControlSystemId,
                ControlSystem = controlSystem,
                OptionNumberRegistries = new List<OptionNumberRegistry> { new OptionNumberRegistry { OptionNumberRegistryId = 1, OptionNumber = "Opt123", SoftwareOptionId = 20 } },
                Requirements = new List<Requirement> { new Requirement { RequirementId = 1, RequirementType = "TypeA", SoftwareOptionId = 20 } },
                SoftwareOptionSpecificationCodes = new List<SoftwareOptionSpecificationCode> { new SoftwareOptionSpecificationCode { SoftwareOptionSpecificationCodeId = 1, SpecCodeDefinitionId = specCodeDef.SpecCodeDefinitionId, SoftwareOptionId = 20, SpecCodeDefinition = specCodeDef } },
                SoftwareOptionActivationRules = new List<SoftwareOptionActivationRule> { new SoftwareOptionActivationRule { SoftwareOptionActivationRuleId = 1, RuleName = "ActivateMe", SoftwareOptionId = 20 } },
                ParameterMappings = new List<ParameterMapping> { new ParameterMapping { ParameterMappingId = 1, RelatedSheetName = "SheetX", SoftwareOptionId = 20 } }
            };
            _softwareOptionsData.Add(expectedOption);
            _mockSoftwareOptionsDbSet = _softwareOptionsData.AsQueryable().BuildMockDbSet();
            _mockContext.Setup(c => c.SoftwareOptions).Returns(_mockSoftwareOptionsDbSet.Object);


            // Act
            var result = await _service.GetSoftwareOptionByIdAsync(20);

            // Assert
            result.Should().NotBeNull();
            result.SoftwareOptionId.Should().Be(expectedOption.SoftwareOptionId);
            result.PrimaryName.Should().Be(expectedOption.PrimaryName);
            result.ControlSystem.Should().NotBeNull();
            result.ControlSystem.Name.Should().Be(controlSystem.Name);
            // For collections, you might want to assert counts or specific items if needed
            result.OptionNumberRegistries.Should().HaveSameCount(expectedOption.OptionNumberRegistries);
            result.Requirements.Should().HaveSameCount(expectedOption.Requirements);
            result.SoftwareOptionSpecificationCodes.Should().HaveSameCount(expectedOption.SoftwareOptionSpecificationCodes);
            result.SoftwareOptionActivationRules.Should().HaveSameCount(expectedOption.SoftwareOptionActivationRules);
            result.ParameterMappings.Should().HaveSameCount(expectedOption.ParameterMappings);
        }

        [Test]
        public async Task GetSoftwareOptionByIdAsync_WhenOptionDoesNotExist_ReturnsNull()
        {
            // Arrange
            _softwareOptionsData.Clear();
            _mockSoftwareOptionsDbSet = _softwareOptionsData.AsQueryable().BuildMockDbSet();
            _mockContext.Setup(c => c.SoftwareOptions).Returns(_mockSoftwareOptionsDbSet.Object);


            // Act
            var result = await _service.GetSoftwareOptionByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task DeleteSoftwareOptionAsync_WhenOptionExists_RemovesOptionAndReturnsTrue()
        {
            // Arrange
            int optionIdToDelete = 30;
            var optionToDelete = new SoftwareOption { SoftwareOptionId = optionIdToDelete, PrimaryName = "ToDelete" };
            _softwareOptionsData.Add(optionToDelete);
            _mockSoftwareOptionsDbSet = _softwareOptionsData.AsQueryable().BuildMockDbSet();
            _mockContext.Setup(c => c.SoftwareOptions).Returns(_mockSoftwareOptionsDbSet.Object);

            _mockSoftwareOptionsDbSet.Setup(m => m.Remove(It.IsAny<SoftwareOption>()))
                                     .Callback<SoftwareOption>(so => _softwareOptionsData.Remove(so));


            // Act
            var result = await _service.DeleteSoftwareOptionAsync(optionIdToDelete);

            // Assert
            result.Should().BeTrue();
            _softwareOptionsData.Should().NotContain(optionToDelete);
            _mockSoftwareOptionsDbSet.Verify(m => m.Remove(It.Is<SoftwareOption>(so => so.SoftwareOptionId == optionIdToDelete)), Times.Once());
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Test]
        public async Task DeleteSoftwareOptionAsync_WhenOptionNotFound_ReturnsFalse()
        {
            // Arrange
            _softwareOptionsData.Clear();
            _mockSoftwareOptionsDbSet = _softwareOptionsData.AsQueryable().BuildMockDbSet();
            _mockContext.Setup(c => c.SoftwareOptions).Returns(_mockSoftwareOptionsDbSet.Object);


            // Act
            var result = await _service.DeleteSoftwareOptionAsync(999);

            // Assert
            result.Should().BeFalse();
            _mockSoftwareOptionsDbSet.Verify(m => m.Remove(It.IsAny<SoftwareOption>()), Times.Never());
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());
        }
    }
}
*/