using Microsoft.Extensions.Caching.Memory;
using System.Reflection;

namespace DATN.Api.Utils.Cache_service
{
    public class MemoryCacheService : IMemoryCacheService
    {
        private readonly IMemoryCache _cache;
        private readonly List<string> _cacheKeys;

        public MemoryCacheService(IMemoryCache memoryCache)
        {
            _cache = memoryCache;
            _cacheKeys = new List<string>();
        }


        private static IDictionary<string, string> ConvertToDictionary(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            var dictionary = new Dictionary<string, string>();

            // Get all properties of the object
            foreach (PropertyInfo property in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.CanRead) // Ensure the property is readable
                {
                    var value = property.GetValue(obj)?.ToString(); // Get property value and convert to string
                    dictionary[property.Name] = value ?? string.Empty; // Use empty string for null values
                }
            }

            return dictionary;
        }
        private string GenerateKey(string moduleName, object parameters)
        {
            try
            {
                var paramKey = string.Join("&", ConvertToDictionary(parameters).Select(kvp => $"{kvp.Key}={kvp.Value}"));
                return $"{moduleName}:{paramKey}";
            }
            catch
            {
                return $"{moduleName}:{parameters}";
            }
        }

        // Set cache by module name and parameters
        public void Set<T>(string moduleName, object parameters, T value, TimeSpan? expirationTime = null)
        {
            string key = GenerateKey(moduleName, parameters);
            if (!_cacheKeys.Contains(key))
            {
                _cacheKeys.Add(key);
            }
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expirationTime ?? TimeSpan.FromMinutes(10)
            };
            _cache.Set(key, value, cacheEntryOptions);
        }

        // Get from cache by module name and parameters
        public T Get<T>(string moduleName, object parameters)
        {
            string key = GenerateKey(moduleName, parameters);
            if (_cache.TryGetValue(key, out T value))
            {
                return value;
            }
            return default;
        }

        // Clear cache by module name
        public void ClearByModule(string moduleName)
        {
            var keysToRemove = _cacheKeys.Where(key => key.Contains($"{moduleName}")).ToList();

            if (_cache is MemoryCache memoryCache)
            {
                foreach (var key in keysToRemove)
                {
                    _cache.Remove(key);
                    _cacheKeys.Remove(key);
                }
            }
        }

        // Clear cache by parameters
        public void ClearByParameters(string moduleName, object parameters)
        {
            string key = GenerateKey(moduleName, parameters);
            if (_cacheKeys.Contains(key))
            {
                _cacheKeys.Remove(key);
            }
            _cache.Remove(key);
        }

        // Clear all cache
        public void ClearAll()
        {
            if (_cache is MemoryCache memoryCache)
            {
                _cacheKeys.Clear();
                memoryCache.Compact(1.0);
            }
        }
    }
}
