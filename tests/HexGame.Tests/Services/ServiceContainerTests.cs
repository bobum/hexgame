using FluentAssertions;
using HexGame.Services;
using HexGame.Services.DependencyInjection;
using Xunit;

namespace HexGame.Tests.Services;

/// <summary>
/// Tests for the ServiceContainer dependency injection system.
/// </summary>
public class ServiceContainerTests
{
    [Fact]
    public void AddSingleton_RegistersService()
    {
        using var container = new ServiceContainer();

        container.AddSingleton<ITestService, TestService>();

        container.TryResolve<ITestService>(out var service).Should().BeTrue();
        service.Should().NotBeNull();
    }

    [Fact]
    public void AddSingleton_ReturnsSameInstance()
    {
        using var container = new ServiceContainer();
        container.AddSingleton<ITestService, TestService>();

        var instance1 = container.Resolve<ITestService>();
        var instance2 = container.Resolve<ITestService>();

        instance1.Should().BeSameAs(instance2);
    }

    [Fact]
    public void AddTransient_ReturnsNewInstanceEachTime()
    {
        using var container = new ServiceContainer();
        container.AddTransient<ITestService, TestService>();

        var instance1 = container.Resolve<ITestService>();
        var instance2 = container.Resolve<ITestService>();

        instance1.Should().NotBeSameAs(instance2);
    }

    [Fact]
    public void RegisterInstance_ReturnsSameInstance()
    {
        using var container = new ServiceContainer();
        var instance = new TestService();

        container.RegisterInstance<ITestService>(instance);

        var resolved = container.Resolve<ITestService>();
        resolved.Should().BeSameAs(instance);
    }

    [Fact]
    public void RegisterFactory_UsesFactoryToCreateInstance()
    {
        using var container = new ServiceContainer();
        int factoryCallCount = 0;

        container.RegisterFactory<ITestService>(sp =>
        {
            factoryCallCount++;
            return new TestService();
        }, ServiceLifetime.Singleton);

        container.Resolve<ITestService>();

        factoryCallCount.Should().Be(1);
    }

    [Fact]
    public void RegisterFactory_Transient_CallsFactoryEachTime()
    {
        using var container = new ServiceContainer();
        int factoryCallCount = 0;

        container.RegisterFactory<ITestService>(sp =>
        {
            factoryCallCount++;
            return new TestService();
        }, ServiceLifetime.Transient);

        container.Resolve<ITestService>();
        container.Resolve<ITestService>();
        container.Resolve<ITestService>();

        factoryCallCount.Should().Be(3);
    }

    [Fact]
    public void TryResolve_UnregisteredService_ReturnsFalse()
    {
        using var container = new ServiceContainer();

        var result = container.TryResolve<ITestService>(out var service);

        result.Should().BeFalse();
        service.Should().BeNull();
    }

    [Fact]
    public void Resolve_UnregisteredService_Throws()
    {
        using var container = new ServiceContainer();

        var action = () => container.Resolve<ITestService>();

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ConstructorInjection_ResolvesConcreteType()
    {
        using var container = new ServiceContainer();
        container.AddSingleton<ITestService, TestService>();
        container.AddSingleton<ServiceWithDependency>();

        var service = container.Resolve<ServiceWithDependency>();

        service.Should().NotBeNull();
        service.Dependency.Should().NotBeNull();
    }

    [Fact]
    public void ConstructorInjection_ResolvesDependencies()
    {
        using var container = new ServiceContainer();
        container.AddSingleton<ITestService, TestService>();
        container.AddSingleton<IServiceWithDependency, ServiceWithDependency>();

        var service = container.Resolve<IServiceWithDependency>();

        service.Should().NotBeNull();
        service.Dependency.Should().NotBeNull();
    }

    [Fact]
    public void InitializeServices_CallsInitializeOnIServiceImplementations()
    {
        using var container = new ServiceContainer();
        container.AddSingleton<TestServiceWithInit>();

        var service = container.Resolve<TestServiceWithInit>();
        service.Initialized.Should().BeFalse();

        container.InitializeServices();

        service.Initialized.Should().BeTrue();
    }

    [Fact]
    public void ShutdownServices_CallsShutdownOnIServiceImplementations()
    {
        using var container = new ServiceContainer();
        container.AddSingleton<TestServiceWithInit>();
        container.InitializeServices();

        var service = container.Resolve<TestServiceWithInit>();
        service.ShutDown.Should().BeFalse();

        container.ShutdownServices();

        service.ShutDown.Should().BeTrue();
    }

    [Fact]
    public void CreateScope_ReturnsScopedContainer()
    {
        using var container = new ServiceContainer();
        container.AddSingleton<ITestService, TestService>();

        using var scope = container.CreateScope();

        scope.Should().NotBeNull();
        scope.ServiceProvider.Should().NotBeNull();
    }

    [Fact]
    public void ScopedService_ReturnsSameInstanceWithinScope()
    {
        using var container = new ServiceContainer();
        container.Register<ITestService, TestService>(ServiceLifetime.Scoped);

        using var scope = container.CreateScope();
        var instance1 = scope.ServiceProvider.GetService<ITestService>();
        var instance2 = scope.ServiceProvider.GetService<ITestService>();

        instance1.Should().BeSameAs(instance2);
    }

    [Fact]
    public void ScopedService_ReturnsDifferentInstanceInDifferentScopes()
    {
        using var container = new ServiceContainer();
        container.Register<ITestService, TestService>(ServiceLifetime.Scoped);

        ITestService instance1, instance2;
        using (var scope1 = container.CreateScope())
        {
            instance1 = scope1.ServiceProvider.GetService<ITestService>();
        }
        using (var scope2 = container.CreateScope())
        {
            instance2 = scope2.ServiceProvider.GetService<ITestService>();
        }

        instance1.Should().NotBeSameAs(instance2);
    }

    [Fact]
    public void Dispose_DisposesCreatedServices()
    {
        var container = new ServiceContainer();
        container.AddSingleton<DisposableService>();
        var service = container.Resolve<DisposableService>();
        service.IsDisposed.Should().BeFalse();

        container.Dispose();

        service.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public void Provider_GetService_ResolvesService()
    {
        using var container = new ServiceContainer();
        container.AddSingleton<ITestService, TestService>();

        var service = container.Provider.GetService<ITestService>();

        service.Should().NotBeNull();
    }

    #region Test Types

    private interface ITestService { }

    private class TestService : ITestService { }

    private interface IServiceWithDependency
    {
        ITestService Dependency { get; }
    }

    private class ServiceWithDependency : IServiceWithDependency
    {
        public ITestService Dependency { get; }

        public ServiceWithDependency(ITestService dependency)
        {
            Dependency = dependency;
        }
    }

    private class TestServiceWithInit : IService
    {
        public bool Initialized { get; private set; }
        public bool ShutDown { get; private set; }

        public void Initialize() => Initialized = true;
        public void Shutdown() => ShutDown = true;
    }

    private class DisposableService : IDisposable
    {
        public bool IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }

    #endregion
}
