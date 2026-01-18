using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace HexGame.Services.DependencyInjection;

/// <summary>
/// Lightweight dependency injection container with lifetime support.
/// </summary>
public class ServiceContainer : IServiceContainer, IServiceProvider
{
    private readonly ConcurrentDictionary<Type, ServiceDescriptor> _descriptors = new();
    private readonly ConcurrentDictionary<Type, object> _singletonInstances = new();
    private readonly List<IService> _initializedServices = new();
    private readonly object _lock = new();
    private bool _disposed;
    private bool _servicesInitialized;

    /// <summary>
    /// Gets the current service provider.
    /// </summary>
    public IServiceProvider Provider => this;

    #region Registration

    public IServiceContainer Register<TService, TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TService : class
        where TImplementation : class, TService
    {
        ThrowIfDisposed();

        var descriptor = new ServiceDescriptor(
            typeof(TService),
            typeof(TImplementation),
            lifetime,
            null,
            null
        );

        _descriptors[typeof(TService)] = descriptor;
        return this;
    }

    public IServiceContainer RegisterInstance<TService>(TService instance)
        where TService : class
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(instance);

        var descriptor = new ServiceDescriptor(
            typeof(TService),
            instance.GetType(),
            ServiceLifetime.Singleton,
            null,
            instance
        );

        _descriptors[typeof(TService)] = descriptor;
        _singletonInstances[typeof(TService)] = instance;
        return this;
    }

    public IServiceContainer RegisterFactory<TService>(Func<IServiceProvider, TService> factory, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TService : class
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(factory);

        var descriptor = new ServiceDescriptor(
            typeof(TService),
            typeof(TService),
            lifetime,
            sp => factory(sp),
            null
        );

        _descriptors[typeof(TService)] = descriptor;
        return this;
    }

    #endregion

    #region Resolution

    public TService Resolve<TService>() where TService : class
    {
        return (TService)Resolve(typeof(TService));
    }

    public bool TryResolve<TService>([NotNullWhen(true)] out TService? service) where TService : class
    {
        if (TryResolve(typeof(TService), out var obj))
        {
            service = (TService)obj!;
            return true;
        }

        service = null;
        return false;
    }

    public object Resolve(Type serviceType)
    {
        ThrowIfDisposed();

        if (!TryResolve(serviceType, out var service))
        {
            throw new InvalidOperationException($"Service of type '{serviceType.FullName}' is not registered.");
        }

        return service!;
    }

    public bool TryResolve(Type serviceType, out object? service)
    {
        ThrowIfDisposed();

        if (!_descriptors.TryGetValue(serviceType, out var descriptor))
        {
            service = null;
            return false;
        }

        service = GetOrCreateInstance(descriptor, null);
        return service != null;
    }

    public TService GetService<TService>() where TService : class
    {
        return Resolve<TService>();
    }

    public TService? GetServiceOrDefault<TService>() where TService : class
    {
        return TryResolve<TService>(out var service) ? service : null;
    }

    public object GetService(Type serviceType)
    {
        return Resolve(serviceType);
    }

    #endregion

    #region Instance Creation

    private object? GetOrCreateInstance(ServiceDescriptor descriptor, ServiceScope? scope)
    {
        return descriptor.Lifetime switch
        {
            ServiceLifetime.Singleton => GetOrCreateSingleton(descriptor),
            ServiceLifetime.Transient => CreateInstance(descriptor),
            ServiceLifetime.Scoped => scope?.GetOrCreateScoped(descriptor) ?? GetOrCreateSingleton(descriptor),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private object GetOrCreateSingleton(ServiceDescriptor descriptor)
    {
        if (descriptor.Instance != null)
        {
            return descriptor.Instance;
        }

        return _singletonInstances.GetOrAdd(descriptor.ServiceType, _ => CreateInstance(descriptor)!);
    }

    private object? CreateInstance(ServiceDescriptor descriptor)
    {
        if (descriptor.Factory != null)
        {
            return descriptor.Factory(this);
        }

        return CreateInstanceViaReflection(descriptor.ImplementationType);
    }

    private object? CreateInstanceViaReflection(Type type)
    {
        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        if (constructors.Length == 0)
        {
            // Try parameterless constructor
            return Activator.CreateInstance(type);
        }

        // Find constructor with most parameters that we can satisfy
        var bestConstructor = constructors
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault(c => CanResolveAllParameters(c));

        if (bestConstructor == null)
        {
            // Fall back to parameterless if available
            var parameterless = constructors.FirstOrDefault(c => c.GetParameters().Length == 0);
            if (parameterless != null)
            {
                return Activator.CreateInstance(type);
            }

            throw new InvalidOperationException(
                $"Cannot find a suitable constructor for type '{type.FullName}'. " +
                "Ensure all constructor parameters are registered in the container.");
        }

        var parameters = bestConstructor.GetParameters();
        var args = new object?[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var paramType = parameters[i].ParameterType;
            if (!TryResolve(paramType, out var paramValue))
            {
                if (parameters[i].HasDefaultValue)
                {
                    args[i] = parameters[i].DefaultValue;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Cannot resolve parameter '{parameters[i].Name}' of type '{paramType.FullName}' " +
                        $"for constructor of '{type.FullName}'.");
                }
            }
            else
            {
                args[i] = paramValue;
            }
        }

        return bestConstructor.Invoke(args);
    }

    private bool CanResolveAllParameters(ConstructorInfo constructor)
    {
        return constructor.GetParameters().All(p =>
            _descriptors.ContainsKey(p.ParameterType) || p.HasDefaultValue);
    }

    #endregion

    #region Scopes

    public IServiceScope CreateScope()
    {
        ThrowIfDisposed();
        return new ServiceScope(this);
    }

    #endregion

    #region Service Lifecycle

    public bool IsRegistered<TService>() where TService : class
    {
        return _descriptors.ContainsKey(typeof(TService));
    }

    public void InitializeServices()
    {
        ThrowIfDisposed();

        if (_servicesInitialized)
        {
            return;
        }

        lock (_lock)
        {
            if (_servicesInitialized)
            {
                return;
            }

            // Initialize all singleton services that implement IService
            foreach (var descriptor in _descriptors.Values.Where(d => d.Lifetime == ServiceLifetime.Singleton))
            {
                if (typeof(IService).IsAssignableFrom(descriptor.ImplementationType))
                {
                    var instance = GetOrCreateSingleton(descriptor);
                    if (instance is IService service && !_initializedServices.Contains(service))
                    {
                        service.Initialize();
                        _initializedServices.Add(service);
                    }
                }
            }

            _servicesInitialized = true;
        }
    }

    public void ShutdownServices()
    {
        lock (_lock)
        {
            // Shutdown in reverse order of initialization
            for (int i = _initializedServices.Count - 1; i >= 0; i--)
            {
                try
                {
                    _initializedServices[i].Shutdown();
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"Error shutting down service: {ex.Message}");
                }
            }

            _initializedServices.Clear();
            _servicesInitialized = false;
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed) return;

        ShutdownServices();

        // Dispose any disposable singletons
        foreach (var instance in _singletonInstances.Values)
        {
            if (instance is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"Error disposing service: {ex.Message}");
                }
            }
        }

        _singletonInstances.Clear();
        _descriptors.Clear();
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ServiceContainer));
        }
    }

    #endregion

    #region Nested Types

    private record ServiceDescriptor(
        Type ServiceType,
        Type ImplementationType,
        ServiceLifetime Lifetime,
        Func<IServiceProvider, object>? Factory,
        object? Instance
    );

    private class ServiceScope : IServiceScope, IServiceProvider
    {
        private readonly ServiceContainer _container;
        private readonly ConcurrentDictionary<Type, object> _scopedInstances = new();
        private bool _disposed;

        public IServiceProvider ServiceProvider => this;

        public ServiceScope(ServiceContainer container)
        {
            _container = container;
        }

        public object? GetOrCreateScoped(ServiceDescriptor descriptor)
        {
            return _scopedInstances.GetOrAdd(descriptor.ServiceType, _ => _container.CreateInstance(descriptor)!);
        }

        public TService GetService<TService>() where TService : class
        {
            return (TService)GetService(typeof(TService));
        }

        public TService? GetServiceOrDefault<TService>() where TService : class
        {
            if (_container._descriptors.TryGetValue(typeof(TService), out var descriptor))
            {
                return (TService?)_container.GetOrCreateInstance(descriptor, this);
            }
            return null;
        }

        public object GetService(Type serviceType)
        {
            if (!_container._descriptors.TryGetValue(serviceType, out var descriptor))
            {
                throw new InvalidOperationException($"Service of type '{serviceType.FullName}' is not registered.");
            }

            return _container.GetOrCreateInstance(descriptor, this)!;
        }

        public void Dispose()
        {
            if (_disposed) return;

            foreach (var instance in _scopedInstances.Values)
            {
                if (instance is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _scopedInstances.Clear();
            _disposed = true;
        }
    }

    #endregion
}

/// <summary>
/// Extension methods for fluent service registration.
/// </summary>
public static class ServiceContainerExtensions
{
    /// <summary>
    /// Registers a service as a singleton.
    /// </summary>
    public static IServiceContainer AddSingleton<TService, TImplementation>(this IServiceContainer container)
        where TService : class
        where TImplementation : class, TService
    {
        return container.Register<TService, TImplementation>(ServiceLifetime.Singleton);
    }

    /// <summary>
    /// Registers a service as a singleton with self-registration.
    /// </summary>
    public static IServiceContainer AddSingleton<TService>(this IServiceContainer container)
        where TService : class
    {
        return container.Register<TService, TService>(ServiceLifetime.Singleton);
    }

    /// <summary>
    /// Registers a service as transient.
    /// </summary>
    public static IServiceContainer AddTransient<TService, TImplementation>(this IServiceContainer container)
        where TService : class
        where TImplementation : class, TService
    {
        return container.Register<TService, TImplementation>(ServiceLifetime.Transient);
    }

    /// <summary>
    /// Registers a service as transient with self-registration.
    /// </summary>
    public static IServiceContainer AddTransient<TService>(this IServiceContainer container)
        where TService : class
    {
        return container.Register<TService, TService>(ServiceLifetime.Transient);
    }

    /// <summary>
    /// Registers a service as scoped.
    /// </summary>
    public static IServiceContainer AddScoped<TService, TImplementation>(this IServiceContainer container)
        where TService : class
        where TImplementation : class, TService
    {
        return container.Register<TService, TImplementation>(ServiceLifetime.Scoped);
    }

    /// <summary>
    /// Registers a service as scoped with self-registration.
    /// </summary>
    public static IServiceContainer AddScoped<TService>(this IServiceContainer container)
        where TService : class
    {
        return container.Register<TService, TService>(ServiceLifetime.Scoped);
    }
}
