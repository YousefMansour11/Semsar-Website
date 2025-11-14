using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ICacheService
    {
        T? Get<T>(string key);
        void Set<T>(string key, T value, TimeSpan? absoluteExpiration = null);
        void Remove(string key);
        void RegisterKey(string key);
        void InvalidateByPrefix(string prefix);
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null);
    }
}
