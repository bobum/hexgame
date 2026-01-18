namespace HexGame.Services;

/// <summary>
/// Base interface for all game services registered with the ServiceLocator.
/// Services should implement this interface to be managed by the service container.
/// </summary>
public interface IService
{
    /// <summary>
    /// Called when the service is registered with the ServiceLocator.
    /// Use this for initialization that requires other services.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Called when the service is being unregistered or the game is shutting down.
    /// Use this to clean up resources.
    /// </summary>
    void Shutdown();
}
