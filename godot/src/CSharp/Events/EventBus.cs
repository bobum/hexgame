namespace HexGame.Events;

/// <summary>
/// Central event bus for decoupled communication between game systems.
/// Supports type-safe event subscription and publication.
/// </summary>
public class EventBus : IService
{
    private readonly Dictionary<Type, List<Delegate>> _subscribers = new();
    private readonly Queue<(Type Type, IGameEvent Event)> _eventQueue = new();
    private readonly object _lock = new();
    private bool _isProcessing;

    #region IService Implementation

    /// <summary>
    /// Initializes the EventBus service.
    /// </summary>
    public void Initialize()
    {
        // No initialization needed
    }

    /// <summary>
    /// Shuts down the EventBus and clears all subscriptions.
    /// </summary>
    public void Shutdown()
    {
        lock (_lock)
        {
            _subscribers.Clear();
            _eventQueue.Clear();
        }
    }

    #endregion

    #region Subscribe/Unsubscribe

    /// <summary>
    /// Subscribes to events of a specific type.
    /// </summary>
    /// <typeparam name="T">The event type to subscribe to.</typeparam>
    /// <param name="handler">The handler to call when the event is published.</param>
    public void Subscribe<T>(Action<T> handler) where T : IGameEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        lock (_lock)
        {
            var type = typeof(T);
            if (!_subscribers.TryGetValue(type, out var handlers))
            {
                handlers = new List<Delegate>();
                _subscribers[type] = handlers;
            }
            handlers.Add(handler);
        }
    }

    /// <summary>
    /// Unsubscribes from events of a specific type.
    /// </summary>
    /// <typeparam name="T">The event type to unsubscribe from.</typeparam>
    /// <param name="handler">The handler to remove.</param>
    /// <returns>True if the handler was found and removed.</returns>
    public bool Unsubscribe<T>(Action<T> handler) where T : IGameEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        lock (_lock)
        {
            var type = typeof(T);
            if (_subscribers.TryGetValue(type, out var handlers))
            {
                return handlers.Remove(handler);
            }
            return false;
        }
    }

    /// <summary>
    /// Removes all subscriptions for a specific event type.
    /// </summary>
    /// <typeparam name="T">The event type.</typeparam>
    public void ClearSubscriptions<T>() where T : IGameEvent
    {
        lock (_lock)
        {
            _subscribers.Remove(typeof(T));
        }
    }

    /// <summary>
    /// Removes all subscriptions.
    /// </summary>
    public void ClearAllSubscriptions()
    {
        lock (_lock)
        {
            _subscribers.Clear();
        }
    }

    #endregion

    #region Publish

    /// <summary>
    /// Publishes an event immediately to all subscribers.
    /// </summary>
    /// <typeparam name="T">The event type.</typeparam>
    /// <param name="gameEvent">The event to publish.</param>
    public void Publish<T>(T gameEvent) where T : IGameEvent
    {
        ArgumentNullException.ThrowIfNull(gameEvent);

        List<Delegate>? handlersCopy;
        lock (_lock)
        {
            if (!_subscribers.TryGetValue(typeof(T), out var handlers))
            {
                return;
            }
            // Copy to avoid issues if handlers modify subscriptions
            handlersCopy = new List<Delegate>(handlers);
        }

        foreach (var handler in handlersCopy)
        {
            try
            {
                ((Action<T>)handler)(gameEvent);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"EventBus: Error in handler for {typeof(T).Name}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Queues an event for deferred processing.
    /// Call ProcessQueue() to deliver queued events.
    /// </summary>
    /// <typeparam name="T">The event type.</typeparam>
    /// <param name="gameEvent">The event to queue.</param>
    public void QueueEvent<T>(T gameEvent) where T : IGameEvent
    {
        ArgumentNullException.ThrowIfNull(gameEvent);

        lock (_lock)
        {
            _eventQueue.Enqueue((typeof(T), gameEvent));
        }
    }

    /// <summary>
    /// Processes all queued events.
    /// Typically called once per frame.
    /// </summary>
    public void ProcessQueue()
    {
        lock (_lock)
        {
            if (_isProcessing || _eventQueue.Count == 0)
            {
                return;
            }
            _isProcessing = true;
        }

        try
        {
            while (true)
            {
                (Type Type, IGameEvent Event) item;
                lock (_lock)
                {
                    if (_eventQueue.Count == 0)
                    {
                        break;
                    }
                    item = _eventQueue.Dequeue();
                }

                PublishInternal(item.Type, item.Event);
            }
        }
        finally
        {
            lock (_lock)
            {
                _isProcessing = false;
            }
        }
    }

    private void PublishInternal(Type eventType, IGameEvent gameEvent)
    {
        List<Delegate>? handlersCopy;
        lock (_lock)
        {
            if (!_subscribers.TryGetValue(eventType, out var handlers))
            {
                return;
            }
            handlersCopy = new List<Delegate>(handlers);
        }

        foreach (var handler in handlersCopy)
        {
            try
            {
                handler.DynamicInvoke(gameEvent);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"EventBus: Error in handler for {eventType.Name}: {ex.Message}");
            }
        }
    }

    #endregion

    #region Diagnostics

    /// <summary>
    /// Gets the number of subscribers for a specific event type.
    /// </summary>
    /// <typeparam name="T">The event type.</typeparam>
    /// <returns>Number of subscribers.</returns>
    public int GetSubscriberCount<T>() where T : IGameEvent
    {
        lock (_lock)
        {
            return _subscribers.TryGetValue(typeof(T), out var handlers) ? handlers.Count : 0;
        }
    }

    /// <summary>
    /// Gets the total number of event types with subscribers.
    /// </summary>
    public int EventTypeCount
    {
        get
        {
            lock (_lock)
            {
                return _subscribers.Count;
            }
        }
    }

    /// <summary>
    /// Gets the number of queued events.
    /// </summary>
    public int QueuedEventCount
    {
        get
        {
            lock (_lock)
            {
                return _eventQueue.Count;
            }
        }
    }

    #endregion
}
