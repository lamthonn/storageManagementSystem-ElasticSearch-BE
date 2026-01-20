namespace DATN.Api.Utils.Cache_service
{
    public interface IRedisCacheService
    {
        bool UseCacheDone { get; }

        // Set cache by module name and parameters
        Task SetAsync<T>(string moduleName, object parameters, T value, TimeSpan? expirationTime = null);

        // Get cache by module name and parameters
        Task<T> GetAsync<T>(string moduleName, object parameters);

        // Clear cache by module name
        Task ClearByModuleAsync(string moduleName);

        // Clear cache by parameters    
        Task ClearByParametersAsync(string moduleName, object parameters);

        // Clear all cache
        Task ClearAllAsync();
    }
}
