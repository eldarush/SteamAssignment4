using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace RabbitThingy.Messaging;

/// <summary>
/// Facade for messaging operations
/// </summary>
public class MessagingFacade : IDisposable
{
    private readonly ILogger<MessagingFacade> _logger;
    private readonly ConcurrentBag<IDisposable> _disposables = [];

    /// <summary>
    /// Initializes a new instance of the MessagingFacade class
    /// </summary>
    /// <param name="logger">The logger instance</param>
    public MessagingFacade(
        ILogger<MessagingFacade> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Disposes of all managed resources
    /// </summary>
    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            try
            {
                disposable.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing {DisposableType}", disposable.GetType().Name);
            }
        }

        _disposables.Clear();
    }
}