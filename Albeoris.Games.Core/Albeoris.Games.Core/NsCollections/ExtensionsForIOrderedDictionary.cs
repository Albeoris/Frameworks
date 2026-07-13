using Albeoris.Games.Core.Abstractions.NsCollections;

namespace Albeoris.Games.Core.NsCollections;

public static class ExtensionsForIOrderedDictionary
{
    public static void Add<TKey, TValue>(this IOrderedDictionary<TKey, TValue> dictionary, TKey key, TValue value) where TKey : notnull
    {
        if (!dictionary.TryAdd(key, value))
            throw new ArgumentException($"The key [{key}] is already in the dictionary.", nameof(key));
    }
    
    public static void Remove<TKey, TValue>(this IOrderedDictionary<TKey, TValue> dictionary, TKey key, out TValue removedValue) where TKey : notnull
    {
        if (!dictionary.TryRemove(key, out removedValue!))
            throw new ArgumentException($"The key [{key}] is not in the dictionary.", nameof(key));
    }
}