using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CFIT.AppTools
{
    public static class CollectionExt
    {
        public static bool CheckEnumeratorValid(this IEnumerator enumerator)
        {
            try
            {
                return enumerator.Current != null;
            }
            catch
            {
                return false;
            }
        }

        public static T Dequeue<T>(this ConcurrentQueue<T> queue)
        {
            if (queue.TryDequeue(out T result))
                return result;
            else
                return default;
        }

        public static bool Add<K, V>(this ConcurrentDictionary<K, V> dictionary, K key, V value)
        {
            return dictionary.TryAdd(key, value);
        }

        public static bool Remove<K, V>(this ConcurrentDictionary<K, V> dictionary, K key)
        {
            return dictionary.TryRemove(key, out _);
        }

        public static bool Add<T>(this ConcurrentDictionary<T, bool> dictionary, T key)
        {
            return dictionary.TryAdd(key, true);
        }

        public static bool Remove<T>(this ConcurrentDictionary<T, bool> dictionary, T value)
        {
            return dictionary.TryRemove(value, out _);
        }

        public static void AddOrUpdate<K,V>(this Dictionary<K, V> dictionary, K key, V value)
        {
            if (dictionary.ContainsKey(key))
                dictionary[key] = value;
            else
                dictionary.Add(key, value);
        }
    }
}
