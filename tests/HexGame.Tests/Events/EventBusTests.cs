using FluentAssertions;
using HexGame.Events;
using Xunit;

namespace HexGame.Tests.Events;

/// <summary>
/// Tests for the EventBus pub/sub system.
/// </summary>
public class EventBusTests
{
    [Fact]
    public void Subscribe_AddsHandler()
    {
        var eventBus = new EventBus();

        eventBus.Subscribe<TestEvent>(_ => { });

        eventBus.GetSubscriberCount<TestEvent>().Should().Be(1);
    }

    [Fact]
    public void Subscribe_MultipleTimes_AddsMultipleHandlers()
    {
        var eventBus = new EventBus();

        eventBus.Subscribe<TestEvent>(_ => { });
        eventBus.Subscribe<TestEvent>(_ => { });
        eventBus.Subscribe<TestEvent>(_ => { });

        eventBus.GetSubscriberCount<TestEvent>().Should().Be(3);
    }

    [Fact]
    public void Unsubscribe_RemovesHandler()
    {
        var eventBus = new EventBus();
        Action<TestEvent> handler = _ => { };

        eventBus.Subscribe(handler);
        var result = eventBus.Unsubscribe(handler);

        result.Should().BeTrue();
        eventBus.GetSubscriberCount<TestEvent>().Should().Be(0);
    }

    [Fact]
    public void Unsubscribe_NonExistentHandler_ReturnsFalse()
    {
        var eventBus = new EventBus();

        var result = eventBus.Unsubscribe<TestEvent>(_ => { });

        result.Should().BeFalse();
    }

    [Fact]
    public void Publish_CallsSubscriber()
    {
        var eventBus = new EventBus();
        TestEvent? receivedEvent = null;
        eventBus.Subscribe<TestEvent>(e => receivedEvent = e);

        var testEvent = new TestEvent("test");
        eventBus.Publish(testEvent);

        receivedEvent.Should().NotBeNull();
        receivedEvent!.Message.Should().Be("test");
    }

    [Fact]
    public void Publish_CallsAllSubscribers()
    {
        var eventBus = new EventBus();
        int callCount = 0;
        eventBus.Subscribe<TestEvent>(_ => callCount++);
        eventBus.Subscribe<TestEvent>(_ => callCount++);
        eventBus.Subscribe<TestEvent>(_ => callCount++);

        eventBus.Publish(new TestEvent("test"));

        callCount.Should().Be(3);
    }

    [Fact]
    public void Publish_NoSubscribers_DoesNotThrow()
    {
        var eventBus = new EventBus();

        var action = () => eventBus.Publish(new TestEvent("test"));

        action.Should().NotThrow();
    }

    [Fact]
    public void Publish_HandlerThrows_ContinuesToOtherHandlers()
    {
        var eventBus = new EventBus();
        int callCount = 0;
        eventBus.Subscribe<TestEvent>(_ => callCount++);
        eventBus.Subscribe<TestEvent>(_ => throw new InvalidOperationException("Test exception"));
        eventBus.Subscribe<TestEvent>(_ => callCount++);

        eventBus.Publish(new TestEvent("test"));

        // Both non-throwing handlers should have been called
        callCount.Should().Be(2);
    }

    [Fact]
    public void QueueEvent_DoesNotCallSubscriberImmediately()
    {
        var eventBus = new EventBus();
        int callCount = 0;
        eventBus.Subscribe<TestEvent>(_ => callCount++);

        eventBus.QueueEvent(new TestEvent("test"));

        callCount.Should().Be(0);
        eventBus.QueuedEventCount.Should().Be(1);
    }

    [Fact]
    public void ProcessQueue_CallsSubscribersForQueuedEvents()
    {
        var eventBus = new EventBus();
        int callCount = 0;
        eventBus.Subscribe<TestEvent>(_ => callCount++);

        eventBus.QueueEvent(new TestEvent("test1"));
        eventBus.QueueEvent(new TestEvent("test2"));
        eventBus.ProcessQueue();

        callCount.Should().Be(2);
        eventBus.QueuedEventCount.Should().Be(0);
    }

    [Fact]
    public void ClearSubscriptions_RemovesAllSubscribersForType()
    {
        var eventBus = new EventBus();
        eventBus.Subscribe<TestEvent>(_ => { });
        eventBus.Subscribe<TestEvent>(_ => { });
        eventBus.Subscribe<AnotherTestEvent>(_ => { });

        eventBus.ClearSubscriptions<TestEvent>();

        eventBus.GetSubscriberCount<TestEvent>().Should().Be(0);
        eventBus.GetSubscriberCount<AnotherTestEvent>().Should().Be(1);
    }

    [Fact]
    public void ClearAllSubscriptions_RemovesAllSubscribers()
    {
        var eventBus = new EventBus();
        eventBus.Subscribe<TestEvent>(_ => { });
        eventBus.Subscribe<AnotherTestEvent>(_ => { });

        eventBus.ClearAllSubscriptions();

        eventBus.EventTypeCount.Should().Be(0);
    }

    [Fact]
    public void Shutdown_ClearsAllSubscriptionsAndQueue()
    {
        var eventBus = new EventBus();
        eventBus.Subscribe<TestEvent>(_ => { });
        eventBus.QueueEvent(new TestEvent("test"));

        eventBus.Shutdown();

        eventBus.EventTypeCount.Should().Be(0);
        eventBus.QueuedEventCount.Should().Be(0);
    }

    [Fact]
    public void EventTypeCount_ReturnsCorrectCount()
    {
        var eventBus = new EventBus();

        eventBus.Subscribe<TestEvent>(_ => { });
        eventBus.Subscribe<AnotherTestEvent>(_ => { });

        eventBus.EventTypeCount.Should().Be(2);
    }

    [Fact]
    public void GameEventBase_SetsTimestamp()
    {
        var before = DateTime.UtcNow;
        var testEvent = new TestEvent("test");
        var after = DateTime.UtcNow;

        testEvent.Timestamp.Should().BeOnOrAfter(before);
        testEvent.Timestamp.Should().BeOnOrBefore(after);
    }

    #region Test Event Types

    private record TestEvent(string Message) : GameEventBase;
    private record AnotherTestEvent(int Value) : GameEventBase;

    #endregion
}
