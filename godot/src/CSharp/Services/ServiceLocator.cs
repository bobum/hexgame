namespace HexGame.Services;

/// <summary>
/// Central registry for game services. Provides global access to shared systems
/// while maintaining testability through interface-based registration.
/// </summary>
/// <remarks>
/// This replaces GDScript's autoload/singleton pattern with a more flexible
/// service container that supports dependency injection for testing.
/// </remarks>
public static class ServiceLocator
{
    private static readonly Dictionary<Type, IService> _services = new();
    private static readonly object _lock = new();
    private static bool _initialized;

    /// <summary>
    /// Registers a service instance for the specified interface type.
    /// </summary>
    /// <typeparam name="T">The service interface type.</typeparam>
    /// <param name="service">The service implementation instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if service is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if a service of this type is already registered.</exception>
    public static void Register<T>(T service) where T : class, IService
    {
        ArgumentNullException.ThrowIfNull(service);

        lock (_lock)
        {
            var type = typeof(T);
            if (_services.ContainsKey(type))
            {
                throw new InvalidOperationException($"Service of type {type.Name} is already registered.");
            }

            _services[type] = service;
            service.Initialize();
        }
    }

    /// <summary>
    /// Retrieves a registered service by its interface type.
    /// </summary>
    /// <typeparam name="T">The service interface type.</typeparam>
    /// <returns>The registered service instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no service of this type is registered.</exception>
    public static T Get<T>() where T : class, IService
    {
        lock (_lock)
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var service))
            {
                return (T)service;
            }

            throw new InvalidOperationException($"Service of type {type.Name} is not registered.");
        }
    }

    /// <summary>
    /// Attempts to retrieve a registered service by its interface type.
    /// </summary>
    /// <typeparam name="T">The service interface type.</typeparam>
    /// <param name="service">The service instance if found, null otherwise.</param>
    /// <returns>True if the service was found, false otherwise.</returns>
    public static bool TryGet<T>([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out T? service) where T : class, IService
    {
        lock (_lock)
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var foundService))
            {
                service = (T)foundService;
                return true;
            }

            service = null;
            return false;
        }
    }

    /// <summary>
    /// Checks if a service of the specified type is registered.
    /// </summary>
    /// <typeparam name="T">The service interface type.</typeparam>
    /// <returns>True if a service of this type is registered.</returns>
    public static bool Has<T>() where T : class, IService
    {
        lock (_lock)
        {
            return _services.ContainsKey(typeof(T));
        }
    }

    /// <summary>
    /// Unregisters a service by its interface type.
    /// </summary>
    /// <typeparam name="T">The service interface type.</typeparam>
    /// <returns>True if the service was found and removed.</returns>
    public static bool Unregister<T>() where T : class, IService
    {
        lock (_lock)
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var service))
            {
                service.Shutdown();
                _services.Remove(type);
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Clears all registered services. Calls Shutdown() on each service.
    /// Typically called during game shutdown or between test runs.
    /// </summary>
    public static void Clear()
    {
        lock (_lock)
        {
            foreach (var service in _services.Values)
            {
                service.Shutdown();
            }
            _services.Clear();
            _initialized = false;
        }
    }

    /// <summary>
    /// Gets whether the ServiceLocator has been initialized.
    /// </summary>
    public static bool IsInitialized
    {
        get
        {
            lock (_lock)
            {
                return _initialized;
            }
        }
    }

    /// <summary>
    /// Marks the ServiceLocator as initialized after all core services are registered.
    /// </summary>
    public static void MarkInitialized()
    {
        lock (_lock)
        {
            _initialized = true;
        }
    }

    /// <summary>
    /// Gets the count of registered services. Useful for debugging.
    /// </summary>
    public static int ServiceCount
    {
        get
        {
            lock (_lock)
            {
                return _services.Count;
            }
        }
    }
}
