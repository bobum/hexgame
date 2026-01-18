using HexGame.AI;
using HexGame.Bridge;
using HexGame.Commands;
using HexGame.Core;
using HexGame.GameState;
using HexGame.Generation;
using HexGame.Input;
using HexGame.Pathfinding;
using HexGame.Persistence;
using HexGame.Rendering;
using HexGame.Units;

namespace HexGame.Services.DependencyInjection;

/// <summary>
/// Game-specific service registration and configuration.
/// </summary>
public static class GameServices
{
    private static ServiceContainer? _container;
    private static readonly object _lock = new();

    /// <summary>
    /// Gets the global service container.
    /// </summary>
    public static IServiceContainer Container
    {
        get
        {
            if (_container == null)
            {
                throw new InvalidOperationException("Service container has not been configured. Call ConfigureServices first.");
            }
            return _container;
        }
    }

    /// <summary>
    /// Gets the service provider for resolving dependencies.
    /// </summary>
    public static IServiceProvider Provider => _container?.Provider
        ?? throw new InvalidOperationException("Service container has not been configured.");

    /// <summary>
    /// Configures all game services with proper dependency injection.
    /// </summary>
    /// <param name="gridWidth">Initial grid width.</param>
    /// <param name="gridHeight">Initial grid height.</param>
    /// <returns>The configured service container.</returns>
    public static IServiceContainer ConfigureServices(int gridWidth = 64, int gridHeight = 64)
    {
        lock (_lock)
        {
            if (_container != null)
            {
                _container.Dispose();
            }

            _container = new ServiceContainer();

            // Register core services
            RegisterCoreServices(gridWidth, gridHeight);

            // Register game systems
            RegisterGameSystems();

            // Register rendering
            RegisterRendering();

            // Register input handling
            RegisterInputHandling();

            // Register AI
            RegisterAI();

            // Register persistence
            RegisterPersistence();

            // Register bridge (C#/GDScript interop)
            RegisterBridge();

            return _container;
        }
    }

    /// <summary>
    /// Initializes all registered services.
    /// </summary>
    public static void Initialize()
    {
        Container.InitializeServices();
    }

    /// <summary>
    /// Shuts down and disposes all services.
    /// </summary>
    public static void Shutdown()
    {
        lock (_lock)
        {
            _container?.Dispose();
            _container = null;
        }
    }

    /// <summary>
    /// Resolves a service from the container.
    /// </summary>
    public static T Get<T>() where T : class
    {
        return Container.Resolve<T>();
    }

    /// <summary>
    /// Tries to resolve a service from the container.
    /// </summary>
    public static bool TryGet<T>([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out T? service) where T : class
    {
        if (_container == null)
        {
            service = null;
            return false;
        }
        return _container.TryResolve(out service);
    }

    #region Service Registration

    private static void RegisterCoreServices(int gridWidth, int gridHeight)
    {
        var container = _container!;

        // Event bus (singleton, fundamental to all communication)
        container.AddSingleton<EventBus>();

        // Command history (singleton for undo/redo)
        container.AddSingleton<CommandHistory>();

        // Hex grid (singleton, core game state)
        container.RegisterFactory<HexGrid>(sp =>
        {
            var grid = new HexGrid(gridWidth, gridHeight);
            grid.Initialize();
            return grid;
        }, ServiceLifetime.Singleton);
    }

    private static void RegisterGameSystems()
    {
        var container = _container!;

        // Map generation
        container.AddSingleton<IMapGenerator, MapGenerator>();

        // Unit management
        container.RegisterFactory<IUnitManager>(sp =>
        {
            var grid = sp.GetService<HexGrid>();
            var manager = new UnitManager(grid);
            manager.SetupPool();
            return manager;
        }, ServiceLifetime.Singleton);

        // Pathfinding
        container.RegisterFactory<IPathfinder>(sp =>
        {
            var grid = sp.GetService<HexGrid>();
            var unitManager = sp.GetService<IUnitManager>();
            return new Pathfinder(grid, unitManager);
        }, ServiceLifetime.Singleton);

        // Turn management
        container.RegisterFactory<TurnManager>(sp =>
        {
            var unitManager = sp.GetService<IUnitManager>();
            return new TurnManager(unitManager);
        }, ServiceLifetime.Singleton);

        // Game state machine
        container.AddSingleton<GameStateMachine>();
    }

    private static void RegisterRendering()
    {
        var container = _container!;

        // Rendering system (coordinates all renderers)
        container.AddSingleton<RenderingSystem>();

        // Individual renderers registered as transient (created on demand)
        container.RegisterFactory<TerrainRenderer>(sp =>
        {
            var grid = sp.GetService<HexGrid>();
            return new TerrainRenderer(grid);
        }, ServiceLifetime.Transient);

        container.RegisterFactory<UnitRenderer>(sp =>
        {
            var unitManager = sp.GetService<IUnitManager>();
            return new UnitRenderer(unitManager);
        }, ServiceLifetime.Transient);

        container.AddTransient<HighlightRenderer>();
    }

    private static void RegisterInputHandling()
    {
        var container = _container!;

        // Input manager (Node-based, uses parameterless constructor)
        container.AddSingleton<InputManager>();

        // Selection manager
        container.RegisterFactory<SelectionManager>(sp =>
        {
            var unitManager = sp.GetService<IUnitManager>();
            var pathfinder = sp.GetService<IPathfinder>();
            var commandHistory = sp.GetService<CommandHistory>();
            var eventBus = sp.GetService<EventBus>();
            return new SelectionManager(unitManager, pathfinder, commandHistory, eventBus);
        }, ServiceLifetime.Singleton);
    }

    private static void RegisterAI()
    {
        var container = _container!;

        // AI Manager
        container.RegisterFactory<AIManager>(sp =>
        {
            var grid = sp.GetService<HexGrid>();
            var unitManager = sp.GetService<IUnitManager>();
            var pathfinder = sp.GetService<IPathfinder>();
            var turnManager = sp.GetService<TurnManager>();
            var commandHistory = sp.GetService<CommandHistory>();
            var eventBus = sp.GetService<EventBus>();
            return new AIManager(grid, unitManager, pathfinder, turnManager, commandHistory, eventBus);
        }, ServiceLifetime.Singleton);

        // AI Controllers (transient - created per AI player)
        container.AddTransient<SimpleAIController>();
        container.AddTransient<DefensiveAIController>();
    }

    private static void RegisterPersistence()
    {
        var container = _container!;

        // Save manager
        container.RegisterFactory<SaveManager>(sp =>
        {
            var eventBus = sp.GetService<EventBus>();
            return new SaveManager(eventBus);
        }, ServiceLifetime.Singleton);
    }

    private static void RegisterBridge()
    {
        var container = _container!;

        // GDScript bridge (singleton for signal emission)
        container.AddSingleton<GDScriptBridge>();

        // Signal hub (connects C# events to GDScript)
        container.RegisterFactory<SignalHub>(sp =>
        {
            var eventBus = sp.GetService<EventBus>();
            var bridge = sp.GetService<GDScriptBridge>();
            return new SignalHub(eventBus, bridge);
        }, ServiceLifetime.Singleton);
    }

    #endregion
}
