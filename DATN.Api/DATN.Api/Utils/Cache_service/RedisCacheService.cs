using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Reflection;

namespace DATN.Api.Utils.Cache_service
{
    public class RedisConfig
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string? Pwd { get; set; } // Optional
        public double Timeout { get; set; }
        public string CachePrefix { get; set; } = "DATN";

    }

    public class RedisCacheService : IRedisCacheService
    {
        private readonly IDatabase _database;
        private readonly IMemoryCacheService _cache;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly RedisCacheService _redis;
        private readonly string _cachePrefix;
        private static bool useCacheDone = true;

        public bool UseCacheDone
        {
            get { return useCacheDone; }
        }

        public RedisCacheService(IConnectionMultiplexer connectionMultiplexer, IMemoryCacheService cache, IOptions<RedisConfig> options)
        {
            _connectionMultiplexer = connectionMultiplexer;
            _cachePrefix = options.Value.CachePrefix;
            //public RedisCacheService(string connectionString)
            //_redis = ConnectionMultiplexer.Connect(connectionString);
            _database = connectionMultiplexer.GetDatabase();
            _cache = cache;
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
        public async Task SetAsync<T>(string moduleName, object parameters, T value, TimeSpan? expirationTime = null)
        {
            try
            {
                if (useCacheDone == false)
                {
                    _cache.Set(moduleName, parameters, value, expirationTime);
                    return;
                }
                string key = GenerateKey(moduleName, parameters);
                string serializedValue = System.Text.Json.JsonSerializer.Serialize(value);
                await _database.StringSetAsync(key, serializedValue, expirationTime ?? TimeSpan.FromMinutes(10));
            }
            catch
            {
                useCacheDone = false;
            }
        }

        // Get cache by module name and parameters
        public async Task<T> GetAsync<T>(string moduleName, object parameters)
        {
            try
            {
                if (useCacheDone == false)
                {
                    return _cache.Get<T>(moduleName, parameters);
                }
                string key = GenerateKey(moduleName, parameters);
                var value = await _database.StringGetAsync(key);

                if (value.HasValue)
                {
                    return System.Text.Json.JsonSerializer.Deserialize<T>(value);
                }
            }
            catch (Exception ex)
            {
                useCacheDone = false;
            }
            return default;
        }

        // Clear cache by module name
        public async Task ClearByModuleAsync(string moduleName)
        {
            try
            {
                if (useCacheDone == false)
                {
                    _cache.ClearByModule(moduleName);
                    return;
                }
                var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
                var keys = server.Keys(pattern: $"*{moduleName}*").ToList();

                foreach (var key in keys)
                {
                    await _database.KeyDeleteAsync(key);
                }
            }
            catch
            {
                useCacheDone = false;
            }
        }

        // Clear cache by parameters
        public async Task ClearByParametersAsync(string moduleName, object parameters)
        {
            try
            {
                if (useCacheDone == false)
                {
                    _cache.ClearByParameters(moduleName, parameters);
                    return;
                }
                string key = GenerateKey(moduleName, parameters);
                await _database.KeyDeleteAsync(key);
            }
            catch
            {
                useCacheDone = false;
            }
        }

        // Clear all cache
        public async Task ClearAllAsync()
        {
            try
            {
                if (useCacheDone == false)
                {
                    _cache.ClearAll();
                    return;
                }
                var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
                var keys = server.Keys().ToList();

                foreach (var key in keys)
                {
                    await _database.KeyDeleteAsync(key);
                }
            }
            catch
            {
                useCacheDone = false;
            }
        }
    }
}
