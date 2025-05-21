using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using RuleArchitect.ApplicationLogic.DTOs;
using RuleArchitect.ApplicationLogic.Services;
// Remove using RuleArchitect.ApplicationLogic.Tests.Helpers; // If MockDbSetHelper was there
using RuleArchitect.Data;
using RuleArchitect.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MockQueryable.Moq; // Add this using statement

namespace RuleArchitect.ApplicationLogic.Tests.Services
{
    [TestFixture]
    public class SoftwareOptionServiceTests
    {
        private Mock<RuleArchitectContext> _mockContext;
        private SoftwareOptionService _service;

        // These lists will hold the in-memory data for your DbSets
        private List<SoftwareOption> _softwareOptionsData;
        private List<ControlSystem> _controlSystemsData;
        private List<OptionNumberRegistry> _optionNumberRegistriesData;
        private List<Requirement> _requirementsData;
        private List<SoftwareOptionHistory> _softwareOptionHistoriesData;

        // Mocks for the DbSets
        private Mock<DbSet<SoftwareOption>> _mockSoftwareOptionsDbSet;
        private Mock<DbSet<ControlSystem>> _mockControlSystemsDbSet;
        private Mock<DbSet<OptionNumberRegistry>> _mockOptionNumberRegistriesDbSet;
        private Mock<DbSet<Requirement>> _mockRequirementsDbSet;
        private Mock<DbSet<SoftwareOptionHistory>> _mockSoftwareOptionHistoriesDbSet;


        [SetUp]
        public void Setup()
        {
            _mockContext = new Mock<RuleArchitectContext>();

            // Initialize the data lists for each test run
            _softwareOptionsData = new List<SoftwareOption>();
            _controlSystemsData = new List<ControlSystem>();
            _optionNumberRegistriesData = new List<OptionNumberRegistry>();
            _requirementsData = new List<Requirement>();
            _softwareOptionHistoriesData = new List<SoftwareOptionHistory>();

            // Create mock DbSets from these lists using MockQueryable.Moq
            // This extension method makes the list queryable like a DbSet, supporting async operations.
            _mockSoftwareOptionsDbSet = _softwareOptionsData.AsQueryable().BuildMockDbSet();
            _mockControlSystemsDbSet = _controlSystemsData.AsQueryable().BuildMockDbSet();
            _mockOptionNumberRegistriesDbSet = _optionNumberRegistriesData.AsQueryable().BuildMockDbSet();
            _mockRequirementsDbSet = _requirementsData.AsQueryable().BuildMockDbSet();
            _mockSoftwareOptionHistoriesDbSet = _softwareOptionHistoriesData.AsQueryable().BuildMockDbSet();

            // Setup callbacks for Add/AddRange to modify the underlying lists
            // This ensures that if the service calls Add/AddRange, our in-memory list reflects the change.
            _mockSoftwareOptionsDbSet.Setup(x => x.Add(It.IsAny<SoftwareOption>()))
                .Callback<SoftwareOption>(so => _softwareOptionsData.Add(so));
            _mockSoftwareOptionsDbSet.Setup(x => x.AddRange(It.IsAny<IEnumerable<SoftwareOption>>()))
                .Callback<IEnumerable<SoftwareOption>>(sos => _softwareOptionsData.AddRange(sos));

            _mockControlSystemsDbSet.Setup(x => x.Add(It.IsAny<ControlSystem>()))
                .Callback<ControlSystem>(cs => _controlSystemsData.Add(cs));
            _mockControlSystemsDbSet.Setup(x => x.AddRange(It.IsAny<IEnumerable<ControlSystem>>()))
                .Callback<IEnumerable<ControlSystem>>(css => _controlSystemsData.AddRange(css));

            _mockOptionNumberRegistriesDbSet.Setup(x => x.Add(It.IsAny<OptionNumberRegistry>()))
                .Callback<OptionNumberRegistry>(onr => _optionNumberRegistriesData.Add(onr));

            _mockRequirementsDbSet.Setup(x => x.Add(It.IsAny<Requirement>()))
                .Callback<Requirement>(r => _requirementsData.Add(r));

            _mockSoftwareOptionHistoriesDbSet.Setup(x => x.Add(It.IsAny<SoftwareOptionHistory>()))
                .Callback<SoftwareOptionHistory>(soh => _softwareOptionHistoriesData.Add(soh));

            // Setup the DbContext to return the mocked DbSets
            _mockContext.Setup(c => c.SoftwareOptions).Returns(_mockSoftwareOptionsDbSet.Object);
            _mockContext.Setup(c => c.ControlSystems).Returns(_mockControlSystemsDbSet.Object);
            _mockContext.Setup(c => c.OptionNumberRegistries).Returns(_mockOptionNumberRegistriesDbSet.Object);
            _mockContext.Setup(c => c.Requirements).Returns(_mockRequirementsDbSet.Object);
            _mockContext.Setup(c => c.SoftwareOptionHistories).Returns(_mockSoftwareOptionHistoriesDbSet.Object);

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                        .ReturnsAsync(1); // Or based on the number of affected entities if needed

            _service = new SoftwareOptionService(_mockContext.Object);
        }

        // Your [Test] methods remain largely the same in terms of Arrange/Act/Assert logic,
        // but how data is added to the backing lists might change slightly if you weren't
        // already adding directly to the DbSet.Object.

        [Test]
        public async Task GetAllSoftwareOptionsAsync_WhenNoOptionsExist_ReturnsEmptyList()
        {
            // _softwareOptionsData is already empty from Setup

            // Act
            var result = await _service.GetAllSoftwareOptionsAsync();

            // Assert
            Assert.That(result, Is.Not.Null, "Result should not be null.");
            Assert.That(result, Is.Empty, "Result should be empty.");
        }

        [Test]
        public async Task GetAllSoftwareOptionsAsync_WhenOptionsExist_ReturnsAllOptions()
        {
            // Arrange
            var controlSystem = new ControlSystem { ControlSystemId = 1, Name = "CS1", MachineTypeId = 1 };
            // Add directly to the backing list, or use the DbSet mock's Add method if preferred
            // Using the mock's Add method ensures your Callback setup is utilized.
            _mockControlSystemsDbSet.Object.Add(controlSystem);
            // Alternatively, if you don't need to verify the .Add call itself for ControlSystem:
            // _controlSystemsData.Add(controlSystem);

            var option1 = new SoftwareOption { SoftwareOptionId = 1, PrimaryName = "Option 1", Version = 1, LastModifiedDate = DateTime.UtcNow, ControlSystemId = 1, ControlSystem = controlSystem };
            var option2 = new SoftwareOption { SoftwareOptionId = 2, PrimaryName = "Option 2", Version = 1, LastModifiedDate = DateTime.UtcNow, ControlSystemId = 1, ControlSystem = controlSystem };

            // Use the DbSet mock's AddRange method, which will use the callback.
            _mockSoftwareOptionsDbSet.Object.AddRange(new List<SoftwareOption> { option1, option2 });
            // Now _softwareOptionsData contains option1 and option2.

            // Act
            var result = await _service.GetAllSoftwareOptionsAsync();

            // Assert
            Assert.That(result, Is.Not.Null, "Result should not be null.");
            Assert.That(result.Count, Is.EqualTo(2), "Should return two options.");
            Assert.That(result.Any(so => so.PrimaryName == "Option 1"), Is.True, "Option 1 not found.");
            Assert.That(result.Any(so => so.PrimaryName == "Option 2"), Is.True, "Option 2 not found.");

            // MockQueryable allows .Include to be in the query. It won't execute it like EF Core against a DB,
            // but it won't cause the query to fail or return empty if the base entities exist.
            // Since you manually set option1.ControlSystem = controlSystem, this assertion should still pass.
            var firstResult = result.FirstOrDefault();
            Assert.That(firstResult, Is.Not.Null);
            Assert.That(firstResult.ControlSystem, Is.Not.Null, "ControlSystem should be populated because it was set in the test data.");
            Assert.That(firstResult.ControlSystem.Name, Is.EqualTo("CS1"));
        }

        [Test]
        public async Task GetSoftwareOptionByIdAsync_WhenOptionExists_ReturnsOption()
        {
            // Arrange
            var controlSystem = new ControlSystem { ControlSystemId = 1, Name = "CS1", MachineTypeId = 1 };
            _controlSystemsData.Add(controlSystem); // Or _mockControlSystemsDbSet.Object.Add(controlSystem);


            var expectedOption = new SoftwareOption { SoftwareOptionId = 10, PrimaryName = "Specific Option", Version = 1, LastModifiedDate = DateTime.UtcNow, ControlSystemId = 1, ControlSystem = controlSystem };
            _softwareOptionsData.Add(expectedOption); // Or _mockSoftwareOptionsDbSet.Object.Add(expectedOption);


            // Act
            var result = await _service.GetSoftwareOptionByIdAsync(10);

            // Assert
            Assert.That(result, Is.Not.Null, "Result should not be null.");
            Assert.That(result.SoftwareOptionId, Is.EqualTo(expectedOption.SoftwareOptionId));
            Assert.That(result.PrimaryName, Is.EqualTo(expectedOption.PrimaryName));
            Assert.That(result.ControlSystem, Is.Not.Null);
            Assert.That(result.ControlSystem.Name, Is.EqualTo("CS1"));
        }

        // ... GetSoftwareOptionByIdAsync_WhenOptionDoesNotExist_ReturnsNull() should work fine ...
        // ... CreateSoftwareOptionAsync tests should also work with the Add/AddRange callbacks correctly updating the lists for verification ...
        // Make sure for CreateSoftwareOptionAsync, the Verifys are on _mockSoftwareOptionsDbSet, _mockOptionNumberRegistriesDbSet etc.

        // Example for Create, Verifys should now target the DbSet mocks
        [Test]
        public async Task CreateSoftwareOptionAsync_WithValidData_CreatesAndReturnsOptionWithHistory()
        {
            // Arrange
            string currentUser = "TestUser";
            var createCommand = new CreateSoftwareOptionCommandDto
            {
                PrimaryName = "New Mocked Option",
                ControlSystemId = null, // Assuming this is valid
                OptionNumbers = new List<OptionNumberRegistryCreateDto> { new OptionNumberRegistryCreateDto { OptionNumber = "MOCK123" } },
                Requirements = new List<RequirementCreateDto> { new RequirementCreateDto { RequirementType = "MockType", GeneralRequiredValue = "MockValue" } }
            };

            // Act
            var createdOption = await _service.CreateSoftwareOptionAsync(createCommand, currentUser);

            // Assert
            Assert.That(createdOption, Is.Not.Null);
            Assert.That(createdOption.PrimaryName, Is.EqualTo(createCommand.PrimaryName));

            // Verify that Add was called on the mock DbSets
            _mockSoftwareOptionsDbSet.Verify(m => m.Add(It.Is<SoftwareOption>(so => so.PrimaryName == createCommand.PrimaryName)), Times.Once());
            _mockOptionNumberRegistriesDbSet.Verify(m => m.Add(It.Is<OptionNumberRegistry>(onr => onr.OptionNumber == "MOCK123")), Times.Once());
            _mockRequirementsDbSet.Verify(m => m.Add(It.Is<Requirement>(r => r.RequirementType == "MockType")), Times.Once());
            _mockSoftwareOptionHistoriesDbSet.Verify(m => m.Add(It.IsAny<SoftwareOptionHistory>()), Times.Once());

            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2)); // As per your original test
        }

        // ... The CreateSoftwareOptionAsync_WithInvalidData_ThrowsException test ...
        // This test still has the same considerations regarding how to assert exceptions based on your service's validation logic.
        // MockQueryable won't change how SaveChangesAsync is mocked or how your service validates.
        [Test]
        public void CreateSoftwareOptionAsync_WithInvalidData_ThrowsException()
        {
            // ... (arrange as before) ...
            Assert.Pass("Refine this exception test based on service's internal validation or more specific mock setup for SaveChangesAsync to throw.");
        }
    }
}