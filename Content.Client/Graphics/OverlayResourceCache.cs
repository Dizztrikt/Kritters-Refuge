using Robust.Client.Graphics;

namespace Content.Client.Graphics;

/// <summary>
/// Stores disposable overlay resources separately for each viewport.
/// </summary>
public sealed class OverlayResourceCache<T> : IDisposable where T : class, IDisposable
{
    private readonly Dictionary<long, CacheEntry> _cache = new();

    public T GetForViewport(IClydeViewport viewport, Func<IClydeViewport, T> factory)
    {
        if (_cache.TryGetValue(viewport.Id, out var entry))
            return entry.Data;

        entry = new CacheEntry
        {
            Data = factory(viewport),
            Viewport = new WeakReference<IClydeViewport>(viewport),
        };

        _cache.Add(viewport.Id, entry);
        viewport.ClearCachedResources += OnClearCachedResources;
        return entry.Data;
    }

    private void OnClearCachedResources(ClearCachedViewportResourcesEvent args)
    {
        if (!_cache.Remove(args.ViewportId, out var entry))
            return;

        entry.Data.Dispose();
        if (args.Viewport != null)
            args.Viewport.ClearCachedResources -= OnClearCachedResources;
    }

    public void Dispose()
    {
        foreach (var entry in _cache.Values)
        {
            if (entry.Viewport.TryGetTarget(out var viewport))
                viewport.ClearCachedResources -= OnClearCachedResources;

            entry.Data.Dispose();
        }

        _cache.Clear();
    }

    private struct CacheEntry
    {
        public T Data;
        public WeakReference<IClydeViewport> Viewport;
    }
}
