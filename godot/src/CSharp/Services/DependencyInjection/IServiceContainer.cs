namespace HexGame.Services.DependencyInjection;

/// <summary>
/// Service lifetime options for dependency injection.
/// </summary>
public enum ServiceLifetime
{
    /// <summary>
    /// A single instance is created and shared across all requests.
    /// </summary>
    Singleton,

    /// <summary>
    /// A new instance is created for each request.
    /// </summary>
    Transient,

    /// <summary>
    /// A single instance is created per scope.
    /// </summary>
    Scoped
}

/// <summary>
/// Interface for the dependency injection container.
/// </summary>
public interface IServiceContainer : IDisposable
{
    /// <summary>
    /// Registers a service with the specified lifetime.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="lifetime">The service lifetime.</param>
    /// <returns>The container for chaining.</returns>
    IServiceContainer Register<TService, TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TService : class
        where TImplementation : class, TService;

    /// <summary>
    /// Registers a service instance (always singleton).
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <param name="instance">The instance to register.</param>
    /// <returns>The container for chaining.</returns>
    IServiceContainer RegisterInstance<TService>(TService instance)
        where TService : class;

    /// <summary>
    /// Registers a service with a factory function.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <param name="factory">Factory function to create the service.</param>
    /// <param name="lifetime">The service lifetime.</param>
    /// <returns>The container for chaining.</returns>
    IServiceContainer RegisterFactory<TService>(Func<IServiceProvider, TService> factory, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TService : class;

    /// <summary>
    /// Resolves a service of the specified type.
    /// </summary>
    /// <typeparam name="TService">The service type to resolve.</typeparam>
    /// <returns>The resolved service.</returns>
    /// <exception cref="InvalidOperationException">If the service is not registered.</exception>
    TService Resolve<TService>() where TService : class;

    /// <summary>
    /// Tries to resolve a service of the specified type.
    /// </summary>
    /// <typeparam name="TService">The service type to resolve.</typeparam>
    /// <param name="service">The resolved service, or null if not found.</param>
    /// <returns>True if the service was resolved.</returns>
    bool TryResolve<TService>([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out TService? service)
        where TService : class;

    /// <summary>
    /// Resolves a service of the specified type.
    /// </summary>
    /// <param name="serviceType">The service type to resolve.</param>
    /// <returns>The resolved service.</returns>
    object Resolve(Type serviceType);

    /// <summary>
    /// Tries to resolve a service of the specified type.
    /// </summary>
    /// <param name="serviceType">The service type to resolve.</param>
    /// <param name="service">The resolved service, or null if not found.</param>
    /// <returns>True if the service was resolved.</returns>
    bool TryResolve(Type serviceType, out object? service);

    /// <summary>
    /// Creates a new scope for scoped services.
    /// </summary>
    /// <returns>A new service scope.</returns>
    IServiceScope CreateScope();

    /// <summary>
    /// Checks if a service is registered.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <returns>True if the service is registered.</returns>
    bool IsRegistered<TService>() where TService : class;

    /// <summary>
    /// Initializes all registered IService implementations.
    /// </summary>
    void InitializeServices();

    /// <summary>
    /// Shuts down all registered IService implementations.
    /// </summary>
    void ShutdownServices();
}

/// <summary>
/// Represents a scope for scoped services.
/// </summary>
public interface IServiceScope : IDisposable
{
    /// <summary>
    /// Gets the service provider for this scope.
    /// </summary>
    IServiceProvider ServiceProvider { get; }
}

/// <summary>
/// Service provider interface for resolving dependencies.
/// </summary>
public interface IServiceProvider
{
    /// <summary>
    /// Gets a service of the specified type.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <returns>The service instance.</returns>
    TService GetService<TService>() where TService : class;

    /// <summary>
    /// Gets a service of the specified type, or null if not registered.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <returns>The service instance or null.</returns>
    TService? GetServiceOrDefault<TService>() where TService : class;

    /// <summary>
    /// Gets a service of the specified type.
    /// </summary>
    /// <param name="serviceType">The service type.</param>
    /// <returns>The service instance.</returns>
    object GetService(Type serviceType);
}
