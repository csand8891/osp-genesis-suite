// File: RuleArchitect.ApplicationLogic.Tests/Services/OrderServiceTests.cs
using NUnit.Framework;
using Moq;
using FluentAssertions;
using RuleArchitect.ApplicationLogic.Services;
using RuleArchitect.ApplicationLogic.Interfaces;
using RuleArchitect.ApplicationLogic.DTOs;
using RuleArchitect.Data;
using RuleArchitect.Entities;
using HeraldKit.Interfaces;
using GenesisSentry.Entities; // Ensure this project is referenced and this using directive is present
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;

namespace RuleArchitect.ApplicationLogic.Tests.Services
{
    [TestFixture]
    public class OrderServiceTests
    {
        private Mock<RuleArchitectContext> _mockContext;
        private Mock<INotificationService> _mockNotificationService;
        private Mock<ISoftwareOptionService> _mockSoftwareOptionService;
        private OrderService _orderService;

        // Data lists for backing DbSets
        private List<Order> _ordersData;
        private List<OrderItem> _orderItemsData;
        private List<ControlSystem> _controlSystemsData;
        private List<MachineModel> _machineModelsData;
        private List<SoftwareOption> _softwareOptionsData;
        private List<UserEntity> _usersData;
        private List<MachineType> _machineTypesData;


        // Mock DbSets
        private Mock<DbSet<Order>> _mockOrderSet;
        private Mock<DbSet<OrderItem>> _mockOrderItemSet;
        private Mock<DbSet<ControlSystem>> _mockControlSystemSet;
        private Mock<DbSet<MachineModel>> _mockMachineModelSet;
        private Mock<DbSet<SoftwareOption>> _mockSoftwareOptionEntitySet;
        private Mock<DbSet<UserEntity>> _mockUserSet;
        private Mock<DbSet<MachineType>> _mockMachineTypeSet;


        [SetUp]
        public void Setup()
        {
            _mockContext = new Mock<RuleArchitectContext>();
            _mockNotificationService = new Mock<INotificationService>();
            _mockSoftwareOptionService = new Mock<ISoftwareOptionService>();

            // Initialize data lists fresh for each test to avoid interference
            _ordersData = new List<Order>();
            _orderItemsData = new List<OrderItem>();
            _machineTypesData = new List<MachineType> { new MachineType { MachineTypeId = 1, Name = "Lathe" } };
            _controlSystemsData = new List<ControlSystem> { new ControlSystem { ControlSystemId = 1, Name = "P300L", MachineTypeId = 1, MachineType = _machineTypesData.First() } };
            _machineModelsData = new List<MachineModel> { new MachineModel { MachineModelId = 1, Name = "LB3000", MachineTypeId = 1, MachineType = _machineTypesData.First() } };
            _softwareOptionsData = new List<SoftwareOption>
            {
                new SoftwareOption { SoftwareOptionId = 1, PrimaryName = "SO1" },
                new SoftwareOption { SoftwareOptionId = 2, PrimaryName = "SO2" }
            };
            _usersData = new List<UserEntity>
            {
                new UserEntity { UserId = 1, UserName = "testuser" },
                new UserEntity { UserId = 2, UserName = "anotheruser" }
            };

            _mockOrderSet = _ordersData.AsQueryable().BuildMockDbSet();
            _mockOrderItemSet = _orderItemsData.AsQueryable().BuildMockDbSet();
            _mockControlSystemSet = _controlSystemsData.AsQueryable().BuildMockDbSet();
            _mockMachineModelSet = _machineModelsData.AsQueryable().BuildMockDbSet();
            _mockSoftwareOptionEntitySet = _softwareOptionsData.AsQueryable().BuildMockDbSet();
            _mockUserSet = _usersData.AsQueryable().BuildMockDbSet();
            _mockMachineTypeSet = _machineTypesData.AsQueryable().BuildMockDbSet();


            _mockContext.Setup(c => c.Orders).Returns(_mockOrderSet.Object);
            _mockContext.Setup(c => c.OrderItems).Returns(_mockOrderItemSet.Object);
            _mockContext.Setup(c => c.ControlSystems).Returns(_mockControlSystemSet.Object);
            _mockContext.Setup(c => c.MachineModels).Returns(_mockMachineModelSet.Object);
            _mockContext.Setup(c => c.SoftwareOptions).Returns(_mockSoftwareOptionEntitySet.Object);
            _mockContext.Setup(c => c.Users).Returns(_mockUserSet.Object);
            _mockContext.Setup(c => c.MachineTypes).Returns(_mockMachineTypeSet.Object);


            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                        .ReturnsAsync(1);

            _mockOrderSet.Setup(m => m.Add(It.IsAny<Order>()))
                         .Callback<Order>(order => _ordersData.Add(order)); // Add to the backing list

            _orderService = new OrderService(
                _mockContext.Object,
                _mockNotificationService.Object,
                _mockSoftwareOptionService.Object);
        }

        #region CreateOrderAsync Tests

        [Test]
        public async Task CreateOrderAsync_WithValidData_ShouldCreateOrderAndReturnDto()
        {
            // Arrange
            var createDto = new CreateOrderDto
            {
                OrderNumber = "ORD123",
                CustomerName = "Test Customer",
                OrderDate = DateTime.UtcNow.Date,
                ControlSystemId = 1, // Assumes this ID exists in _controlSystemsData
                MachineModelId = 1,  // Assumes this ID exists in _machineModelsData
                SoftwareOptionIds = new List<int> { 1 } // Assumes this ID exists in _softwareOptionsData
            };
            int createdByUserId = 1;

            _mockSoftwareOptionService.Setup(s => s.GetSoftwareOptionByIdAsync(1))
                                      .ReturnsAsync(_softwareOptionsData.First(so => so.SoftwareOptionId == 1));

            // No explicit Setup for AnyAsync needed here. 
            // The service will query the in-memory lists (_controlSystemsData, _machineModelsData, _ordersData).
            // Ensure _ordersData is empty for the duplicate check to pass.

            // Act
            OrderDetailDto resultDto = await _orderService.CreateOrderAsync(createDto, createdByUserId);

            // Assert
            resultDto.Should().NotBeNull();
            resultDto.OrderNumber.Should().Be(createDto.OrderNumber);
            // ... other assertions ...
            _mockOrderSet.Verify(m => m.Add(It.Is<Order>(o => o.OrderNumber == createDto.OrderNumber)), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockNotificationService.Verify(n => n.ShowSuccess(It.IsAny<string>(), It.Is<string>(title => title == "Order Created"), It.IsAny<TimeSpan?>()), Times.Once);
        }

        [Test]
        public void CreateOrderAsync_WithNullDto_ShouldThrowArgumentNullException()
        {
            // Arrange
            CreateOrderDto createDto = null;
            int createdByUserId = 1;

            // Act & Assert
            Func<Task> act = async () => await _orderService.CreateOrderAsync(createDto, createdByUserId);
            act.Should().ThrowAsync<ArgumentNullException>().WithMessage("*createOrderDto*");
        }

        [Test]
        public void CreateOrderAsync_WithDuplicateOrderNumber_ShouldThrowInvalidOperationException()
        {
            // Arrange
            string duplicateOrderNumber = "ORDEXIST";
            _ordersData.Add(new Order { OrderNumber = duplicateOrderNumber, ControlSystemId = 1, MachineModelId = 1 }); // Add duplicate to backing list

            var createDto = new CreateOrderDto { OrderNumber = duplicateOrderNumber, ControlSystemId = 1, MachineModelId = 1 };
            int createdByUserId = 1;

            // No explicit Setup for AnyAsync needed. It will check _ordersData.

            // Act & Assert
            Func<Task> act = async () => await _orderService.CreateOrderAsync(createDto, createdByUserId);
            act.Should().ThrowAsync<InvalidOperationException>().WithMessage($"*'{duplicateOrderNumber}' already exists*");
        }


        [Test]
        public void CreateOrderAsync_WithInvalidControlSystemId_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var createDto = new CreateOrderDto { OrderNumber = "ORD456", ControlSystemId = 99, MachineModelId = 1 }; // 99 doesn't exist
            int createdByUserId = 1;

            // _controlSystemsData in Setup does not contain ID 99. AnyAsync will return false.

            // Act & Assert
            Func<Task> act = async () => await _orderService.CreateOrderAsync(createDto, createdByUserId);
            act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*ControlSystem*with ID 99*not found*");
        }


        [Test]
        public void CreateOrderAsync_WithInvalidSoftwareOptionId_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var createDto = new CreateOrderDto
            {
                OrderNumber = "ORD789",
                ControlSystemId = 1,
                MachineModelId = 1,
                SoftwareOptionIds = new List<int> { 99 } // 99 doesn't exist
            };
            int createdByUserId = 1;

            _mockSoftwareOptionService.Setup(s => s.GetSoftwareOptionByIdAsync(99))
                                      .ReturnsAsync((SoftwareOption)null);

            // AnyAsync for ControlSystem, MachineModel, Orders will use data from Setup.

            // Act & Assert
            Func<Task> act = async () => await _orderService.CreateOrderAsync(createDto, createdByUserId);
            act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*SoftwareOption*with ID 99*not found*");
        }


        #endregion

        #region GetOrderByIdAsync Tests

        [Test]
        public async Task GetOrderByIdAsync_WhenOrderExists_ShouldReturnOrderDetailDto()
        {
            // Arrange
            int orderId = 1;
            var orderEntity = new Order
            {
                OrderId = orderId,
                OrderNumber = "TestOrder",
                CreatedByUserId = 1,
                ControlSystemId = 1,
                MachineModelId = 1,
                ControlSystem = _controlSystemsData.First(cs => cs.ControlSystemId == 1),
                MachineModel = _machineModelsData.First(mm => mm.MachineModelId == 1),
                CreatedByUser = _usersData.First(u => u.UserId == 1),
                OrderItems = new List<OrderItem> { new OrderItem { OrderItemId = 1, SoftwareOptionId = 1, SoftwareOption = _softwareOptionsData.First(so => so.SoftwareOptionId == 1) } }
            };
            _ordersData.Add(orderEntity); // Add to backing list

            // Rebuild mock to reflect the added data for this specific test
            _mockOrderSet = _ordersData.AsQueryable().BuildMockDbSet();
            _mockContext.Setup(c => c.Orders).Returns(_mockOrderSet.Object);


            // Act
            OrderDetailDto result = await _orderService.GetOrderByIdAsync(orderId);

            // Assert
            result.Should().NotBeNull();
            result.OrderId.Should().Be(orderId);
            result.OrderNumber.Should().Be("TestOrder");
            result.ControlSystemName.Should().Be("P300L");
            result.MachineModelName.Should().Be("LB3000");
            result.MachineTypeName.Should().Be("Lathe");
            result.CreatedByUserName.Should().Be("testuser");
            result.OrderItems.Should().HaveCount(1);
            result.OrderItems.First().SoftwareOptionName.Should().Be("SO1");
        }

        [Test]
        public async Task GetOrderByIdAsync_WhenOrderDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            int orderId = 99;
            // _ordersData is empty by default from Setup for this test

            // Act
            OrderDetailDto result = await _orderService.GetOrderByIdAsync(orderId);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetAllOrdersAsync Tests

        [Test]
        public async Task GetAllOrdersAsync_WithNoFilters_ShouldReturnAllOrders()
        {
            // Arrange
            _ordersData.AddRange(new List<Order>
            {
                new Order { OrderId = 1, OrderNumber = "ORD001", OrderDate = DateTime.UtcNow.AddDays(-1), CreatedByUserId = 1, ControlSystemId = 1, MachineModelId = 1, ControlSystem = _controlSystemsData.First(), MachineModel = _machineModelsData.First(), CreatedByUser = _usersData.First() },
                new Order { OrderId = 2, OrderNumber = "ORD002", OrderDate = DateTime.UtcNow, CreatedByUserId = 1, ControlSystemId = 1, MachineModelId = 1, ControlSystem = _controlSystemsData.First(), MachineModel = _machineModelsData.First(), CreatedByUser = _usersData.First()  }
            });
            _mockOrderSet = _ordersData.AsQueryable().BuildMockDbSet(); // Rebuild with data
            _mockContext.Setup(c => c.Orders).Returns(_mockOrderSet.Object);


            var filterDto = new OrderFilterDto();

            // Act
            IEnumerable<OrderDetailDto> result = await _orderService.GetAllOrdersAsync(filterDto);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
        }

        [Test]
        public async Task GetAllOrdersAsync_WithStatusFilter_ShouldReturnFilteredOrders()
        {
            // Arrange
            _ordersData.AddRange(new List<Order>
            {
                new Order { OrderId = 1, OrderNumber = "ORD001", Status = OrderStatus.Draft, OrderDate = DateTime.UtcNow, CreatedByUserId = 1, ControlSystemId = 1, MachineModelId = 1, ControlSystem = _controlSystemsData.First(), MachineModel = _machineModelsData.First(), CreatedByUser = _usersData.First() },
                new Order { OrderId = 2, OrderNumber = "ORD002", Status = OrderStatus.ReadyForProduction, OrderDate = DateTime.UtcNow, CreatedByUserId = 1, ControlSystemId = 1, MachineModelId = 1, ControlSystem = _controlSystemsData.First(), MachineModel = _machineModelsData.First(), CreatedByUser = _usersData.First() }
            });
            _mockOrderSet = _ordersData.AsQueryable().BuildMockDbSet(); // Rebuild with data
            _mockContext.Setup(c => c.Orders).Returns(_mockOrderSet.Object);

            var filterDto = new OrderFilterDto { Status = OrderStatus.Draft };

            // Act
            IEnumerable<OrderDetailDto> result = await _orderService.GetAllOrdersAsync(filterDto);

            // Assert
            result.Should().HaveCount(1);
            result.First().OrderNumber.Should().Be("ORD001");
        }

        #endregion

        #region PutOrderOnHoldAsync Tests

        [Test]
        public async Task PutOrderOnHoldAsync_WhenOrderExistsAndCanBeHeld_ShouldUpdateStatusAndReturnTrue()
        {
            // Arrange
            int orderId = 1;
            int userId = 1;
            string notes = "Customer request";
            var orderEntity = new Order { OrderId = orderId, OrderNumber = "ORDHOLD", Status = OrderStatus.Draft };
            _ordersData.Add(orderEntity);
            _mockOrderSet = _ordersData.AsQueryable().BuildMockDbSet();
            _mockContext.Setup(c => c.Orders).Returns(_mockOrderSet.Object);


            // Act
            var result = await _orderService.PutOrderOnHoldAsync(orderId, userId, notes);

            // Assert
            result.Should().BeTrue();
            orderEntity.Status.Should().Be(OrderStatus.OnHold);
            orderEntity.LastModifiedByUserId.Should().Be(userId);
            orderEntity.Notes.Should().Contain(notes);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockNotificationService.Verify(n => n.ShowInformation(It.Is<string>(s => s.Contains("on hold")), It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Once);
        }

        [Test]
        public async Task PutOrderOnHoldAsync_WhenOrderIsCompleted_ShouldReturnFalseAndNotify()
        {
            // Arrange
            int orderId = 1;
            var orderEntity = new Order { OrderId = orderId, OrderNumber = "ORDCOMP", Status = OrderStatus.Completed };
            _ordersData.Add(orderEntity);
            _mockOrderSet = _ordersData.AsQueryable().BuildMockDbSet();
            _mockContext.Setup(c => c.Orders).Returns(_mockOrderSet.Object);


            // Act
            var result = await _orderService.PutOrderOnHoldAsync(orderId, 1, "test");

            // Assert
            result.Should().BeFalse();
            orderEntity.Status.Should().Be(OrderStatus.Completed);
            _mockNotificationService.Verify(n => n.ShowWarning(It.IsAny<string>(), "Action Not Allowed", It.IsAny<TimeSpan?>()), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task PutOrderOnHoldAsync_WhenOrderNotFound_ShouldReturnFalseAndNotifyError()
        {
            // Arrange
            int orderId = 99;
            // _ordersData is empty

            // Act
            var result = await _orderService.PutOrderOnHoldAsync(orderId, 1, "test");

            // Assert
            result.Should().BeFalse();
            _mockNotificationService.Verify(n => n.ShowError(It.Is<string>(s => s.Contains("not found")), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        }

        #endregion
    }
}
