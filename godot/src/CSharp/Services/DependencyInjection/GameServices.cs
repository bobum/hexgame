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
        return Container.TryResolve(out service);
    }

    #region Service Registration

    private static void RegisterCoreServices(int gridWidth, int gridHeight)
    {
        // Event bus (singleton, fundamental to all communication)
        _container!.AddSingleton<EventBus>();

        // Command history (singleton for undo/redo)
        _container.AddSingleton<CommandHistory>();

        // Hex grid (singleton, core game state)
        _container.RegisterFactory<HexGrid>(sp =>
        {
            var grid = new HexGrid(gridWidth, gridHeight);
            grid.Initialize();
            return grid;
        }, ServiceLifetime.Singleton);
    }

    private static void RegisterGameSystems()
    {
        // Map generation
        _container!.AddSingleton<IMapGenerator, MapGenerator>();

        // Unit management
        _container.RegisterFactory<IUnitManager>(sp =>
        {
            var grid = sp.GetService<HexGrid>();
            var manager = new UnitManager(grid);
            manager.SetupPool();
            return manager;
        }, ServiceLifetime.Singleton);

        // Pathfinding
        _container.RegisterFactory<IPathfinder>(sp =>
        {
            var grid = sp.GetService<HexGrid>();
            var unitManager = sp.GetService<IUnitManager>();
            return new Pathfinder(grid, unitManager);
        }, ServiceLifetime.Singleton);

        // Turn management
        _container.RegisterFactory<TurnManager>(sp =>
        {
            var unitManager = sp.GetService<IUnitManager>();
            return new TurnManager(unitManager);
        }, ServiceLifetime.Singleton);

        // Game state machine
        _container.AddSingleton<GameStateMachine>();
    }

    private static void RegisterRendering()
    {
        // Rendering system (coordinates all renderers)
        _container!.RegisterFactory<RenderingSystem>(sp =>
        {
            var grid = sp.GetService<HexGrid>();
            return new RenderingSystem(grid);
        }, ServiceLifetime.Singleton);

        // Individual renderers registered as transient (created on demand)
        _container.RegisterFactory<TerrainRenderer>(sp =>
        {
            var grid = sp.GetService<HexGrid>();
            return new TerrainRenderer(grid);
        }, ServiceLifetime.Transient);

        _container.RegisterFactory<UnitRenderer>(sp =>
        {
            var grid = sp.GetService<HexGrid>();
            var unitManager = sp.GetService<IUnitManager>();
            return new UnitRenderer(grid, unitManager);
        }, ServiceLifetime.Transient);

        _container.RegisterFactory<HighlightRenderer>(sp =>
        {
            var grid = sp.GetService<HexGrid>();
            return new HighlightRenderer(grid);
        }, ServiceLifetime.Transient);
    }

    private static void RegisterInputHandling()
    {
        // Input manager
        _container!.RegisterFactory<InputManager>(sp =>
        {
            var grid = sp.GetService<HexGrid>();
            var eventBus = sp.GetService<EventBus>();
            return new InputManager(grid, eventBus);
        }, ServiceLifetime.Singleton);

        // Selection manager
        _container.RegisterFactory<SelectionManager>(sp =>
        {
            var unitManager = sp.GetService<IUnitManager>();
            var pathfinder = sp.GetService<IPathfinder>();
            var turnManager = sp.GetService<TurnManager>();
            var eventBus = sp.GetService<EventBus>();
            var commandHistory = sp.GetService<CommandHistory>();
            return new SelectionManager(unitManager, pathfinder, turnManager, eventBus, commandHistory);
        }, ServiceLifetime.Singleton);
    }

    private static void RegisterAI()
    {
        // AI Manager
        _container!.RegisterFactory<AIManager>(sp =>
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
        _container.AddTransient<SimpleAIController>();
        _container.AddTransient<DefensiveAIController>();
    }

    private static void RegisterPersistence()
    {
        // Save manager
        _container!.RegisterFactory<SaveManager>(sp =>
        {
            var eventBus = sp.GetService<EventBus>();
            return new SaveManager(eventBus);
        }, ServiceLifetime.Singleton);
    }

    private static void RegisterBridge()
    {
        // GDScript bridge (singleton for signal emission)
        _container!.AddSingleton<GDScriptBridge>();

        // Signal hub (connects C# events to GDScript)
        _container.RegisterFactory<SignalHub>(sp =>
        {
            var eventBus = sp.GetService<EventBus>();
            var bridge = sp.GetService<GDScriptBridge>();
            return new SignalHub(eventBus, bridge);
        }, ServiceLifetime.Singleton);
    }

    #endregion
}
