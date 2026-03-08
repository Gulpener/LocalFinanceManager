using Microsoft.ML;

namespace LocalFinanceManager.Services;

/// <summary>
/// Singleton in-memory cache for the active ML model transformer.
/// Avoids repeated deserialization of model bytes from the database on every prediction request.
/// Thread-safe via a simple lock.
/// </summary>
public interface IMLModelCache
{
    /// <summary>Returns the cached transformer and its version, or (null, 0) if no model is cached.</summary>
    (ITransformer? Transformer, int Version) GetCached();

    /// <summary>Stores a transformer for the given model version.</summary>
    void Set(int version, ITransformer transformer);

    /// <summary>Evicts the cached model so that the next request reloads from the database.</summary>
    void Invalidate();
}

/// <summary>
/// Default implementation of <see cref="IMLModelCache"/>.
/// Registered as a singleton so the model is loaded at most once per version across all HTTP requests.
/// </summary>
public sealed class MLModelCache : IMLModelCache
{
    private readonly object _lock = new();
    private ITransformer? _transformer;
    private int _version;

    public (ITransformer? Transformer, int Version) GetCached()
    {
        lock (_lock)
        {
            return (_transformer, _version);
        }
    }

    public void Set(int version, ITransformer transformer)
    {
        lock (_lock)
        {
            _version = version;
            _transformer = transformer;
        }
    }

    public void Invalidate()
    {
        lock (_lock)
        {
            _version = 0;
            _transformer = null;
        }
    }
}
