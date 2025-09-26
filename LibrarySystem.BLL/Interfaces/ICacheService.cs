namespace LibrarySystem.BLL.Interfaces;

public interface ICacheService
{
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
    void Remove(string key);
    void Invalidate(params string[] keys);
}
