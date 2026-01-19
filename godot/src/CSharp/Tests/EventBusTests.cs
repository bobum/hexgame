namespace HexGame.Tests;

/// <summary>
/// Unit tests for EventBus.
/// </summary>
public partial class EventBusTests : Node
{
    private EventBus _eventBus = null!;

    public override void _Ready()
    {
        GD.Print("=== EventBus Tests ===");
        _eventBus = new EventBus();
        _eventBus.Initialize();

        TestSubscribeAndPublish();
        TestUnsubscribe();
        TestMultipleSubscribers();
        TestQueuedEvents();
        TestClearSubscriptions();

        _eventBus.Shutdown();
        GD.Print("=== All EventBus Tests Passed ===");
    }

    private void TestSubscribeAndPublish()
    {
        bool eventReceived = false;
        UnitCreatedEvent? receivedEvent = null;

        void Handler(UnitCreatedEvent e)
        {
            eventReceived = true;
            receivedEvent = e;
        }

        _eventBus.Subscribe<UnitCreatedEvent>(Handler);
        _eventBus.Publish(new UnitCreatedEvent(1, 0, 5, 5, 1));

        Assert(eventReceived, "Event should be received");
        Assert(receivedEvent != null, "Event data should not be null");
        Assert(receivedEvent!.UnitId == 1, "UnitId should be 1");
        Assert(receivedEvent.Q == 5 && receivedEvent.R == 5, "Coordinates should be (5, 5)");

        _eventBus.Unsubscribe<UnitCreatedEvent>(Handler);
        GD.Print("  [PASS] Subscribe and publish");
    }

    private void TestUnsubscribe()
    {
        int callCount = 0;
        void Handler(TurnStartedEvent e) => callCount++;

        _eventBus.Subscribe<TurnStartedEvent>(Handler);
        _eventBus.Publish(new TurnStartedEvent(1, 1));
        Assert(callCount == 1, "Should be called once");

        _eventBus.Unsubscribe<TurnStartedEvent>(Handler);
        _eventBus.Publish(new TurnStartedEvent(2, 1));
        Assert(callCount == 1, "Should not be called after unsubscribe");

        GD.Print("  [PASS] Unsubscribe");
    }

    private void TestMultipleSubscribers()
    {
        int count1 = 0, count2 = 0;
        void Handler1(TurnEndedEvent e) => count1++;
        void Handler2(TurnEndedEvent e) => count2++;

        _eventBus.Subscribe<TurnEndedEvent>(Handler1);
        _eventBus.Subscribe<TurnEndedEvent>(Handler2);
        _eventBus.Publish(new TurnEndedEvent(1, 1));

        Assert(count1 == 1 && count2 == 1, "Both handlers should be called");

        _eventBus.ClearSubscriptions<TurnEndedEvent>();
        GD.Print("  [PASS] Multiple subscribers");
    }

    private void TestQueuedEvents()
    {
        int callCount = 0;
        void Handler(CellHoveredEvent e) => callCount++;

        _eventBus.Subscribe<CellHoveredEvent>(Handler);

        // Queue events
        _eventBus.QueueEvent(new CellHoveredEvent(1, 1));
        _eventBus.QueueEvent(new CellHoveredEvent(2, 2));
        Assert(callCount == 0, "Queued events should not fire immediately");
        Assert(_eventBus.QueuedEventCount == 2, "Should have 2 queued events");

        // Process queue
        _eventBus.ProcessQueue();
        Assert(callCount == 2, "Both queued events should fire");
        Assert(_eventBus.QueuedEventCount == 0, "Queue should be empty");

        _eventBus.ClearSubscriptions<CellHoveredEvent>();
        GD.Print("  [PASS] Queued events");
    }

    private void TestClearSubscriptions()
    {
        int count = 0;
        void Handler(MapGenerationStartedEvent e) => count++;

        _eventBus.Subscribe<MapGenerationStartedEvent>(Handler);
        Assert(_eventBus.GetSubscriberCount<MapGenerationStartedEvent>() == 1, "Should have 1 subscriber");

        _eventBus.ClearSubscriptions<MapGenerationStartedEvent>();
        Assert(_eventBus.GetSubscriberCount<MapGenerationStartedEvent>() == 0, "Should have 0 subscribers");

        _eventBus.Publish(new MapGenerationStartedEvent(32, 32, 12345));
        Assert(count == 0, "Handler should not be called after clear");

        GD.Print("  [PASS] Clear subscriptions");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception($"Assertion failed: {message}");
        }
    }
}
