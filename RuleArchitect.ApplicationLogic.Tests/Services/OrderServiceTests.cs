// File: RuleArchitect.ApplicationLogic.Tests/Services/OrderServiceTests.cs
using NUnit.Framework;
using Moq;
using FluentAssertions;
using RuleArchitect.ApplicationLogic.Services;
using RuleArchitect.Abstractions.Interfaces;
using RuleArchitect.Abstractions.DTOs.Order;
using RuleArchitect.Abstractions.DTOs.Auth;
using RuleArchitect.Data;
using RuleArchitect.Entities;
using HeraldKit.Interfaces; // For INotificationService from HeraldKit
using RuleArchitect.Abstractions.Enums;
using GenesisSentry.Entities; // For UserEntity
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RuleArchitect.ApplicationLogic.Tests.Helpers;

namespace RuleArchitect.ApplicationLogic.Tests.Services
{
    [TestFixture]
    public class OrderServiceTests
    {
        private Mock<RuleArchitectContext> _mockContext;
        private Mock<HeraldKit.Interfaces.INotificationService> _mockNotificationService; // Explicitly HeraldKit's
        private Mock<ISoftwareOptionService> _mockSoftwareOptionService;
        private Mock<IUserActivityLogService> _mockUserActivityLogService;
        private Mock<IUserService> _mockUserService;
        private OrderService _orderService;

        private List<Order> _ordersData;
        private List<OrderItem> _orderItemsData;
        private List<ControlSystem> _controlSystemsData;
        private List<MachineModel> _machineModelsData;
        private List<SoftwareOption> _softwareOptionsData;
        private List<UserEntity> _usersData;

        private Mock<DbSet<Order>> _mockOrderSet;
        private Mock<DbSet<OrderItem>> _mockOrderItemSet;
        private Mock<DbSet<ControlSystem>> _mockControlSystemSet;
        private Mock<DbSet<MachineModel>> _mockMachineModelSet;

        [SetUp]
        public void Setup()
        {
            _mockContext = new Mock<RuleArchitectContext>();
            _mockNotificationService = new Mock<HeraldKit.Interfaces.INotificationService>();
            _mockSoftwareOptionService = new Mock<ISoftwareOptionService>();
            _mockUserActivityLogService = new Mock<IUserActivityLogService>();
            _mockUserService = new Mock<IUserService>();

            _usersData = new List<UserEntity>
            {
                new UserEntity { UserId = 1, UserName = "testuser", Email = "test@user.com", Role = "User", IsActive = true },
                new UserEntity { UserId = 2, UserName = "reviewer", Email = "reviewer@user.com", Role = "Reviewer", IsActive = true },
                new UserEntity { UserId = 3, UserName = "tech", Email = "tech@user.com", Role = "Technician", IsActive = true },
                new UserEntity { UserId = 4, UserName = "admin", Email = "admin@user.com", Role = "Administrator", IsActive = true }
            };

            _ordersData = new List<Order>();
            _orderItemsData = new List<OrderItem>();

            _controlSystemsData = new List<ControlSystem> { new ControlSystem { ControlSystemId = 1, Name = "CS1" } };
            _machineModelsData = new List<MachineModel> { new MachineModel { MachineModelId = 1, Name = "MM1" } };
            _softwareOptionsData = new List<SoftwareOption> {
                new SoftwareOption { SoftwareOptionId = 1, PrimaryName = "SO1", PrimaryOptionNumberDisplay = "S001" },
                new SoftwareOption { SoftwareOptionId = 2, PrimaryName = "SO2", PrimaryOptionNumberDisplay = "S002" },
                new SoftwareOption { SoftwareOptionId = 3, PrimaryName = "SO3", PrimaryOptionNumberDisplay = "S003" }
            };

            _mockOrderSet = MockDbSetHelper.CreateMockDbSet(_ordersData);
            _mockOrderItemSet = MockDbSetHelper.CreateMockDbSet(_orderItemsData);
            _mockControlSystemSet = MockDbSetHelper.CreateMockDbSet(_controlSystemsData);
            _mockMachineModelSet = MockDbSetHelper.CreateMockDbSet(_machineModelsData);

            _mockContext.Setup(c => c.Orders).Returns(_mockOrderSet.Object);
            _mockContext.Setup(c => c.OrderItems).Returns(_mockOrderItemSet.Object);
            _mockContext.Setup(c => c.ControlSystems).Returns(_mockControlSystemSet.Object);
            _mockContext.Setup(c => c.MachineModels).Returns(_mockMachineModelSet.Object);

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _mockUserService.Setup(s => s.GetUserByIdAsync(It.IsAny<int>()))
                            .ReturnsAsync((int id) => {
                                var userEntity = _usersData.FirstOrDefault(u => u.UserId == id);
                                return userEntity != null ?
                                       new UserDto { UserId = userEntity.UserId, UserName = userEntity.UserName, Role = userEntity.Role, IsActive = userEntity.IsActive } :
                                       null;
                            });

            _mockSoftwareOptionService.Setup(s => s.GetSoftwareOptionByIdAsync(It.IsAny<int>()))
                                     .ReturnsAsync((int id) => _softwareOptionsData.FirstOrDefault(so => so.SoftwareOptionId == id));


            _orderService = new OrderService(
                _mockContext.Object,
                _mockNotificationService.Object,
                _mockSoftwareOptionService.Object,
                _mockUserActivityLogService.Object,
                _mockUserService.Object);
        }

        // --- Test methods for UpdateOrderAsync ---
        [Test]
        public async Task UpdateOrderAsync_WhenOrderExistsAndIsValid_ShouldUpdateOrderAndReturnDto()
        {
            // Arrange
            var orderId = 1;
            var initialOrderItem = new OrderItem { OrderItemId = 1, SoftwareOptionId = 1, OrderId = orderId };
            var initialOrder = new Order
            {
                OrderId = orderId, OrderNumber = "ORD001", CustomerName = "Old Customer", Status = OrderStatus.Draft,
                ControlSystemId = 1, MachineModelId = 1, CreatedByUserId = 1, CreatedAt = DateTime.UtcNow.AddDays(-1),
                OrderItems = new List<OrderItem> { initialOrderItem }
            };
            _ordersData.Add(initialOrder);
            _orderItemsData.Add(initialOrderItem);


            var updateDto = new UpdateOrderDto
            {
                CustomerName = "New Customer",
                RequiredDate = DateTime.UtcNow.AddDays(10),
                Notes = "Updated notes",
                ControlSystemId = 1,
                MachineModelId = 1,
                SoftwareOptionIds = new List<int> { 2 } // Remove SO1, Add SO2
            };
            var modifiedByUserId = 4;
            var adminUser = new UserDto { UserId = modifiedByUserId, UserName = "admin" };
            _mockUserService.Setup(s => s.GetUserByIdAsync(modifiedByUserId)).ReturnsAsync(adminUser);


            // Act
            var resultDto = await _orderService.UpdateOrderAsync(orderId, updateDto, modifiedByUserId);

            // Assert
            resultDto.Should().NotBeNull();
            resultDto.CustomerName.Should().Be(updateDto.CustomerName);
            resultDto.Notes.Should().Be(updateDto.Notes);
            resultDto.OrderItems.Should().HaveCount(1);
            resultDto.OrderItems.First().SoftwareOptionId.Should().Be(2);

            initialOrder.LastModifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2)); // Increased tolerance
            initialOrder.LastModifiedByUserId.Should().Be(modifiedByUserId);

            _mockOrderItemSet.Verify(m => m.Remove(It.Is<OrderItem>(oi => oi.OrderItemId == 1)), Times.Once);
            _mockOrderItemSet.Verify(m => m.Add(It.Is<OrderItem>(oi => oi.SoftwareOptionId == 2 && oi.OrderId == orderId)), Times.Once);

            _mockUserActivityLogService.Verify(log => log.LogActivityAsync(
                modifiedByUserId, adminUser.UserName, "UpdateOrder", It.IsAny<string>(), It.IsAny<bool>(), "Order", orderId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _mockNotificationService.Verify(n => n.ShowSuccess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task UpdateOrderAsync_WhenOrderNotFound_ShouldReturnNullAndShowError()
        {
            // Arrange
            var updateDto = new UpdateOrderDto { CustomerName = "Test" };

            // Act
            var result = await _orderService.UpdateOrderAsync(999, updateDto, 1);

            // Assert
            result.Should().BeNull();
            _mockNotificationService.Verify(n => n.ShowError(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Once);
            _mockUserActivityLogService.Verify(log => log.LogActivityAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task UpdateOrderAsync_WhenOrderIsCompleted_ShouldReturnDtoAndShowWarning()
        {
            // Arrange
            var orderId = 1;
            var initialOrder = new Order { OrderId = orderId, OrderNumber = "ORD001", Status = OrderStatus.Completed };
            _ordersData.Add(initialOrder);
            var updateDto = new UpdateOrderDto { CustomerName = "Attempt Update" };

            // Act
            var resultDto = await _orderService.UpdateOrderAsync(orderId, updateDto, 1);

            // Assert
            resultDto.Should().NotBeNull();
            resultDto.OrderNumber.Should().Be("ORD001");
            _mockNotificationService.Verify(n => n.ShowWarning(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task UpdateOrderAsync_WhenControlSystemNotFound_ShouldReturnNullAndShowError()
        {
            // Arrange
            var orderId = 1;
             _ordersData.Add(new Order { OrderId = orderId, ControlSystemId = 1, Status = OrderStatus.Draft });
            var updateDto = new UpdateOrderDto { ControlSystemId = 999 };

            // Act
            var result = await _orderService.UpdateOrderAsync(orderId, updateDto, 1);

            // Assert
            result.Should().BeNull();
            _mockNotificationService.Verify(n => n.ShowError(It.Is<string>(s => s.Contains("ControlSystem with ID 999 not found")), It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task UpdateOrderAsync_WhenSoftwareOptionForAddNotFound_ShouldShowErrorAndPartiallySucceed()
        {
            // Arrange
            var orderId = 1;
            var initialOrder = new Order { OrderId = orderId, OrderNumber = "ORD001", CustomerName = "Old Customer", Status = OrderStatus.Draft, ControlSystemId = 1, MachineModelId = 1 };
            _ordersData.Add(initialOrder);
            var updateDto = new UpdateOrderDto { SoftwareOptionIds = new List<int> { 999 } };
            var modifiedByUserId = 1;

            _mockSoftwareOptionService.Setup(s => s.GetSoftwareOptionByIdAsync(999)).ReturnsAsync((SoftwareOption)null);

            // Act
            var resultDto = await _orderService.UpdateOrderAsync(orderId, updateDto, modifiedByUserId);

            // Assert
            resultDto.Should().NotBeNull();
            resultDto.OrderItems.Should().BeEmpty();
            _mockNotificationService.Verify(n => n.ShowError(It.Is<string>(s => s.Contains("SoftwareOption with ID 999 not found")), It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task UpdateOrderAsync_WhenDbUpdateExceptionOccurs_ShouldReturnNullAndShowError()
        {
            // Arrange
            var orderId = 1;
            _ordersData.Add(new Order { OrderId = orderId, Status = OrderStatus.Draft, ControlSystemId = 1, MachineModelId = 1 });
            var updateDto = new UpdateOrderDto { CustomerName = "New Name" };
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new DbUpdateException("Test DB error"));

            // Act
            var result = await _orderService.UpdateOrderAsync(orderId, updateDto, 1);

            // Assert
            result.Should().BeNull();
            _mockNotificationService.Verify(n => n.ShowError(It.Is<string>(s => s.Contains("Database error")), It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Once);
        }


        // --- Test methods for RemoveSoftwareOptionFromOrderAsync ---
        [Test]
        public async Task RemoveSoftwareOptionFromOrderAsync_WhenItemExistsAndOrderIsValid_ShouldRemoveItemAndReturnTrue()
        {
            // Arrange
            var orderId = 1;
            var orderItemIdToRemove = 10;
            var softwareOptionId = 1;
            var orderItemEntity = new OrderItem { OrderItemId = orderItemIdToRemove, SoftwareOptionId = softwareOptionId, OrderId = orderId };
            var order = new Order {
                OrderId = orderId, OrderNumber = "ORD001", Status = OrderStatus.Draft,
                OrderItems = new List<OrderItem> { orderItemEntity }
            };
            _ordersData.Add(order);
            _orderItemsData.Add(orderItemEntity);
            var removedByUserId = 1;
            var testUser = new UserDto { UserId = removedByUserId, UserName = "testuser" };
            _mockUserService.Setup(s => s.GetUserByIdAsync(removedByUserId)).ReturnsAsync(testUser);


            // Act
            var result = await _orderService.RemoveSoftwareOptionFromOrderAsync(orderId, orderItemIdToRemove, removedByUserId);

            // Assert
            result.Should().BeTrue();
            _mockOrderItemSet.Verify(m => m.Remove(It.Is<OrderItem>(oi => oi.OrderItemId == orderItemIdToRemove)), Times.Once);
            order.LastModifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2)); // Increased tolerance
            order.LastModifiedByUserId.Should().Be(removedByUserId);
            _mockUserActivityLogService.Verify(log => log.LogActivityAsync(removedByUserId, testUser.UserName, "RemoveSoftwareOptionFromOrder", It.IsAny<string>(), It.IsAny<bool>(), "Order", orderId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _mockNotificationService.Verify(n => n.ShowSuccess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task RemoveSoftwareOptionFromOrderAsync_WhenOrderNotFound_ShouldReturnFalseAndShowError()
        {
            // Act
            var result = await _orderService.RemoveSoftwareOptionFromOrderAsync(999, 1, 1);
            // Assert
            result.Should().BeFalse();
            _mockNotificationService.Verify(n => n.ShowError(It.Is<string>(s => s.Contains("Order with ID 999 not found")), It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Once);
        }

        [Test]
        public async Task RemoveSoftwareOptionFromOrderAsync_WhenOrderItemNotFound_ShouldReturnFalseAndShowError()
        {
            // Arrange
            _ordersData.Add(new Order { OrderId = 1, Status = OrderStatus.Draft, OrderItems = new List<OrderItem>() });
            // Act
            var result = await _orderService.RemoveSoftwareOptionFromOrderAsync(1, 999, 1);
            // Assert
            result.Should().BeFalse();
            _mockNotificationService.Verify(n => n.ShowError(It.Is<string>(s => s.Contains("Software option (item ID: 999) not found")), It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Once);
        }

        [Test]
        public async Task RemoveSoftwareOptionFromOrderAsync_WhenOrderStatusPreventsRemoval_ShouldReturnFalseAndShowWarning()
        {
            // Arrange
            var orderId = 1;
            var orderItemId = 10;
            var orderItemEntity = new OrderItem { OrderItemId = orderItemId, OrderId = orderId };
            _ordersData.Add(new Order {
                OrderId = orderId, Status = OrderStatus.Completed,
                OrderItems = new List<OrderItem> { orderItemEntity }
            });
            _orderItemsData.Add(orderItemEntity);
            // Act
            var result = await _orderService.RemoveSoftwareOptionFromOrderAsync(orderId, orderItemId, 1);
            // Assert
            result.Should().BeFalse();
            _mockNotificationService.Verify(n => n.ShowWarning(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Once);
        }

        [Test]
        public async Task RemoveSoftwareOptionFromOrderAsync_WhenDbUpdateExceptionOccurs_ShouldReturnFalseAndShowError()
        {
             // Arrange
            var orderId = 1;
            var orderItemIdToRemove = 10;
            var orderItemEntity = new OrderItem { OrderItemId = orderItemIdToRemove, OrderId = orderId };
            var order = new Order {
                OrderId = orderId, Status = OrderStatus.Draft,
                OrderItems = new List<OrderItem> { orderItemEntity }
            };
            _ordersData.Add(order);
            _orderItemsData.Add(orderItemEntity);
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new DbUpdateException("Test DB error"));
            // Act
            var result = await _orderService.RemoveSoftwareOptionFromOrderAsync(orderId, orderItemIdToRemove, 1);
            // Assert
            result.Should().BeFalse();
            _mockNotificationService.Verify(n => n.ShowError(It.Is<string>(s => s.Contains("Database error")), It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Once);
        }


        // --- Test methods for SubmitOrderForProductionAsync ---
        [Test]
        public async Task SubmitOrderForProductionAsync_WhenOrderIsDraft_ShouldSucceed()
        {
            // Arrange
            var order = new Order { OrderId = 1, OrderNumber = "ORD1", Status = OrderStatus.Draft, CreatedByUserId = 1 };
            _ordersData.Add(order);
            int reviewerUserId = 2;
            var reviewerUser = new UserDto { UserId = reviewerUserId, UserName = "reviewer" };
            _mockUserService.Setup(s => s.GetUserByIdAsync(reviewerUserId)).ReturnsAsync(reviewerUser);
            string notes = "LGTM";

            // Act
            var result = await _orderService.SubmitOrderForProductionAsync(order.OrderId, reviewerUserId, notes);

            // Assert
            result.Should().BeTrue();
            order.Status.Should().Be(OrderStatus.ReadyForProduction);
            order.OrderReviewerUserId.Should().Be(reviewerUserId);
            order.OrderReviewedAt.Should().NotBeNull();
            order.OrderReviewedAt.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2)); // Increased tolerance
            order.OrderReviewNotes.Should().Be(notes);
            order.LastModifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2)); // Increased tolerance
            order.LastModifiedByUserId.Should().Be(reviewerUserId);

            _mockUserActivityLogService.Verify(l => l.LogActivityAsync(reviewerUserId, reviewerUser.UserName, "SubmitOrderForProduction", It.IsAny<string>(), It.IsAny<bool>(), "Order", order.OrderId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _mockNotificationService.Verify(n => n.ShowSuccess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task SubmitOrderForProductionAsync_WhenOrderNotFound_ShouldReturnFalseAndShowError()
        {
            // Act
            var result = await _orderService.SubmitOrderForProductionAsync(999, 1, "notes");
            // Assert
            result.Should().BeFalse();
            _mockNotificationService.Verify(n => n.ShowError(It.Is<string>(s => s.Contains("Order with ID 999 not found")), It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Once);
        }

        [Test]
        public async Task SubmitOrderForProductionAsync_WhenOrderNotInDraftStatus_ShouldReturnFalseAndShowWarning()
        {
            // Arrange
            var order = new Order { OrderId = 1, OrderNumber = "ORD1", Status = OrderStatus.OnHold };
            _ordersData.Add(order);
            // Act
            var result = await _orderService.SubmitOrderForProductionAsync(order.OrderId, 2, "notes");
            // Assert
            result.Should().BeFalse();
            _mockNotificationService.Verify(n => n.ShowWarning(It.Is<string>(s => s.Contains("not in 'Draft' status")), It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Once);
        }

        [Test]
        public async Task SubmitOrderForProductionAsync_WhenDbUpdateExceptionOccurs_ShouldReturnFalseAndShowError()
        {
            // Arrange
            var order = new Order { OrderId = 1, Status = OrderStatus.Draft };
            _ordersData.Add(order);
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new DbUpdateException("DB error"));
            // Act
            var result = await _orderService.SubmitOrderForProductionAsync(order.OrderId, 2, "notes");
            // Assert
            result.Should().BeFalse();
            _mockNotificationService.Verify(n => n.ShowError(It.Is<string>(s => s.Contains("Database error")), It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Once);
        }

        // --- Test methods for StartProductionAsync ---
        [Test]
        public async Task StartProductionAsync_WhenOrderIsReadyForProduction_ShouldSucceed()
        {
            // Arrange
            var order = new Order { OrderId = 1, OrderNumber = "ORD1", Status = OrderStatus.ReadyForProduction };
            _ordersData.Add(order);
            int techUserId = 3;
            var techUser = new UserDto { UserId = techUserId, UserName = "tech" };
            _mockUserService.Setup(s => s.GetUserByIdAsync(techUserId)).ReturnsAsync(techUser);
            string notes = "Starting now";

            // Act
            var result = await _orderService.StartProductionAsync(order.OrderId, techUserId, notes);

            // Assert
            result.Should().BeTrue();
            order.Status.Should().Be(OrderStatus.ProductionInProgress);
            order.ProductionTechUserId.Should().Be(techUserId);
            order.ProductionNotes.Should().Contain(notes);
            _mockUserActivityLogService.Verify(l => l.LogActivityAsync(techUserId, techUser.UserName, "StartProduction", It.IsAny<string>(), It.IsAny<bool>(), "Order", order.OrderId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _mockNotificationService.Verify(n => n.ShowSuccess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Once);
        }

        [Test]
        public async Task StartProductionAsync_OrderNotFound_ReturnsFalse()
        {
            var result = await _orderService.StartProductionAsync(999, 1, "notes");
            result.Should().BeFalse();
            _mockNotificationService.Verify(n => n.ShowError(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Once);
        }

        [Test]
        public async Task StartProductionAsync_InvalidStatus_ReturnsFalse()
        {
            _ordersData.Add(new Order { OrderId = 1, Status = OrderStatus.Draft });
            var result = await _orderService.StartProductionAsync(1, 1, "notes");
            result.Should().BeFalse();
            _mockNotificationService.Verify(n => n.ShowWarning(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Once);
        }


        // --- Test methods for CompleteProductionAsync ---
        [Test]
        public async Task CompleteProductionAsync_WhenOrderIsInProduction_ShouldSucceed()
        {
            // Arrange
            var order = new Order { OrderId = 1, OrderNumber = "ORD1", Status = OrderStatus.ProductionInProgress, ProductionTechUserId = 3 };
            _ordersData.Add(order);
            int techUserId = 3;
            var techUser = new UserDto { UserId = techUserId, UserName = "tech" };
             _mockUserService.Setup(s => s.GetUserByIdAsync(techUserId)).ReturnsAsync(techUser);
            string notes = "All done";

            // Act
            var result = await _orderService.CompleteProductionAsync(order.OrderId, techUserId, notes);

            // Assert
            result.Should().BeTrue();
            order.Status.Should().Be(OrderStatus.ReadyForSoftwareReview);
            order.ProductionTechUserId.Should().Be(techUserId);
            order.ProductionCompletedAt.Should().NotBeNull().And.BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2)); // Increased tolerance
            order.ProductionNotes.Should().Contain(notes);
            _mockUserActivityLogService.Verify(l => l.LogActivityAsync(techUserId, techUser.UserName, "CompleteProduction", It.IsAny<string>(), It.IsAny<bool>(), "Order", order.OrderId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task CompleteProductionAsync_OrderNotFound_ReturnsFalse()
        {
            var result = await _orderService.CompleteProductionAsync(999, 1, "notes");
            result.Should().BeFalse();
        }

        [Test]
        public async Task CompleteProductionAsync_InvalidStatus_ReturnsFalse()
        {
            _ordersData.Add(new Order { OrderId = 1, Status = OrderStatus.Draft });
            var result = await _orderService.CompleteProductionAsync(1, 1, "notes");
            result.Should().BeFalse();
        }

        // --- Test methods for StartSoftwareReviewAsync ---
        [Test]
        public async Task StartSoftwareReviewAsync_WhenOrderIsReadyForReview_ShouldSucceed()
        {
            // Arrange
            var order = new Order { OrderId = 1, OrderNumber = "ORD1", Status = OrderStatus.ReadyForSoftwareReview };
            _ordersData.Add(order);
            int reviewerUserId = 2;
            var reviewerUser = new UserDto { UserId = reviewerUserId, UserName = "reviewer" };
            _mockUserService.Setup(s => s.GetUserByIdAsync(reviewerUserId)).ReturnsAsync(reviewerUser);
            string notes = "Reviewing now";

            // Act
            var result = await _orderService.StartSoftwareReviewAsync(order.OrderId, reviewerUserId, notes);

            // Assert
            result.Should().BeTrue();
            order.Status.Should().Be(OrderStatus.SoftwareReviewInProgress);
            order.SoftwareReviewerUserId.Should().Be(reviewerUserId);
            order.SoftwareReviewNotes.Should().Contain(notes);
            _mockUserActivityLogService.Verify(l => l.LogActivityAsync(reviewerUserId, reviewerUser.UserName, "StartSoftwareReview", It.IsAny<string>(), It.IsAny<bool>(), "Order", order.OrderId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task StartSoftwareReviewAsync_OrderNotFound_ReturnsFalse()
        {
             var result = await _orderService.StartSoftwareReviewAsync(999, 1, "notes");
            result.Should().BeFalse();
        }

        [Test]
        public async Task StartSoftwareReviewAsync_InvalidStatus_ReturnsFalse()
        {
            _ordersData.Add(new Order { OrderId = 1, Status = OrderStatus.Draft });
            var result = await _orderService.StartSoftwareReviewAsync(1, 1, "notes");
            result.Should().BeFalse();
        }


        // --- Test methods for RejectOrderAsync ---
        [Test]
        public async Task RejectOrderAsync_WithValidPreviousStatusAndNotes_ShouldSucceed()
        {
            // Arrange
            var order = new Order { OrderId = 1, OrderNumber = "ORD1", Status = OrderStatus.ReadyForProduction };
            _ordersData.Add(order);
            int userId = 4;
            var adminUser = new UserDto { UserId = userId, UserName = "admin" };
            _mockUserService.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync(adminUser);
            string rejectionNotes = "Does not meet spec";

            // Act
            var result = await _orderService.RejectOrderAsync(order.OrderId, OrderStatus.Rejected, userId, rejectionNotes);

            // Assert
            result.Should().BeTrue();
            order.Status.Should().Be(OrderStatus.Rejected);
            order.Notes.Should().Contain(rejectionNotes);
            _mockUserActivityLogService.Verify(l => l.LogActivityAsync(userId, adminUser.UserName, "RejectOrder", It.Is<string>(s => s.Contains(rejectionNotes)), It.IsAny<bool>(), "Order", order.OrderId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task RejectOrderAsync_WhenNewStatusIsNotRejected_ShouldReturnFalseAndShowError()
        {
            // Act
            var result = await _orderService.RejectOrderAsync(1, OrderStatus.Draft, 1, "notes");
            // Assert
            result.Should().BeFalse();
            _mockNotificationService.Verify(n => n.ShowError(It.Is<string>(s => s.Contains("Only OrderStatus.Rejected is allowed")), It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Once);
        }

        [Test]
        public async Task RejectOrderAsync_WhenRejectionNotesAreMissing_ShouldReturnFalseAndShowWarning()
        {
             // Arrange
            var order = new Order { OrderId = 1, OrderNumber = "ORD1", Status = OrderStatus.ReadyForProduction };
            _ordersData.Add(order);
            // Act
            var result = await _orderService.RejectOrderAsync(order.OrderId, OrderStatus.Rejected, 1, " ");
            // Assert
            result.Should().BeFalse();
            _mockNotificationService.Verify(n => n.ShowWarning(It.Is<string>(s => s.Contains("Rejection notes are mandatory")), It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Once);
        }

        [Test]
        public async Task RejectOrderAsync_WhenCurrentStatusNotAllowedForRejection_ShouldReturnFalseAndShowWarning()
        {
            // Arrange
            var order = new Order { OrderId = 1, OrderNumber = "ORD1", Status = OrderStatus.Draft };
            _ordersData.Add(order);
             // Act
            var result = await _orderService.RejectOrderAsync(order.OrderId, OrderStatus.Rejected, 1, "notes");
            // Assert
            result.Should().BeFalse();
            _mockNotificationService.Verify(n => n.ShowWarning(It.Is<string>(s => s.Contains("cannot be rejected from this state")), It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Once);
        }


        // --- Test methods for CancelOrderAsync ---
        [Test]
        public async Task CancelOrderAsync_WhenOrderNotCompletedOrCancelled_ShouldSucceed()
        {
            // Arrange
            var order = new Order { OrderId = 1, OrderNumber = "ORD1", Status = OrderStatus.Draft };
            _ordersData.Add(order);
            int userId = 4;
            var adminUser = new UserDto { UserId = userId, UserName = "admin" };
            _mockUserService.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync(adminUser);
            string notes = "Customer request";

            // Act
            var result = await _orderService.CancelOrderAsync(order.OrderId, userId, notes);

            // Assert
            result.Should().BeTrue();
            order.Status.Should().Be(OrderStatus.Cancelled);
            order.Notes.Should().Contain(notes);
            _mockUserActivityLogService.Verify(l => l.LogActivityAsync(userId, adminUser.UserName, "CancelOrder", It.IsAny<string>(), It.IsAny<bool>(), "Order", order.OrderId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task CancelOrderAsync_WhenOrderIsCompleted_ShouldReturnFalseAndShowWarning()
        {
            // Arrange
            var order = new Order { OrderId = 1, OrderNumber = "ORD1", Status = OrderStatus.Completed };
            _ordersData.Add(order);
            // Act
            var result = await _orderService.CancelOrderAsync(order.OrderId, 1, "notes");
            // Assert
            result.Should().BeFalse();
            _mockNotificationService.Verify(n => n.ShowWarning(It.Is<string>(s => s.Contains("already Completed and cannot be cancelled")), It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Once);
        }

        [Test]
        public async Task CancelOrderAsync_WhenOrderIsAlreadyCancelled_ShouldReturnTrueAndShowInformation()
        {
             // Arrange
            var order = new Order { OrderId = 1, OrderNumber = "ORD1", Status = OrderStatus.Cancelled };
            _ordersData.Add(order);
            // Act
            var result = await _orderService.CancelOrderAsync(order.OrderId, 1, "notes");
            // Assert
            result.Should().BeTrue();
            _mockNotificationService.Verify(n => n.ShowInformation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
