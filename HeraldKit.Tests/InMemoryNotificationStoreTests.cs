using NUnit.Framework;
using HeraldKit.Models;
using HeraldKit.Implementations;
using System;
using FluentAssertions;

[TestFixture]
public class InMemoryNotificationStoreTests
{
    private InMemoryNotificationStore _store;

    [SetUp]
    public void Setup()
    {
        _store = new InMemoryNotificationStore();
    }

    [Test]
    public void Add_WhenCalled_IncreasesCountAndUnreadCount()
    {
        // Arrange
        var message = new NotificationMessage("Test Add");

        // Act
        _store.Add(message);

        // Assert
        _store.GetAll().Should().HaveCount(1);
        _store.GetUnreadCount().Should().Be(1);
        _store.GetAll().Should().Contain(message);
    }

    [Test]
    public void MarkAsRead_WhenUnreadMessageExists_MarksAsReadAndDecreasesUnreadCount()
    {
        // Arrange
        var message = new NotificationMessage("Test Mark");
        _store.Add(message);
        int initialUnread = _store.GetUnreadCount();

        // Act
        bool result = _store.MarkAsRead(message.Id);

        // Assert
        result.Should().BeTrue();
        _store.GetUnreadCount().Should().Be(initialUnread - 1);
        _store.GetById(message.Id).IsRead.Should().BeTrue(); // Assumes GetById is implemented
    }

    [Test]
    public void Add_WhenCalled_FiresStoreChangedEventWithCorrectArgs()
    {
        // Arrange
        var message = new NotificationMessage("Test Event");
        StoreChangedEventArgs receivedArgs = null;
        _store.StoreChanged += (sender, args) => { receivedArgs = args; };

        // Act
        _store.Add(message);

        // Assert
        receivedArgs.Should().NotBeNull();
        receivedArgs.Action.Should().Be(StoreChangeAction.Added);
        receivedArgs.AffectedIds.Should().HaveCount(1);
        receivedArgs.AffectedIds.Should().Contain(message.Id);
    }

    // ... Add tests for Remove, ClearAll, GetById, MarkAllAsRead, etc. ...
}