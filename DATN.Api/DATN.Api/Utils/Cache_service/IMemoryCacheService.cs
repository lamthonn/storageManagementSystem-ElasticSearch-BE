namespace DATN.Api.Utils.Cache_service
{
    public interface IMemoryCacheService
    {
        // Set cache by module name and parameters
        void Set<T>(string moduleName, object parameters, T value, TimeSpan? expirationTime = null);

        // Get from cache by module name and parameters
        T Get<T>(string moduleName, object parameters);

        // Clear cache by module name
        void ClearByModule(string moduleName);

        // Clear cache by parameters
        void ClearByParameters(string moduleName, object parameters);

        // Clear all cache
        void ClearAll();
    }
}
