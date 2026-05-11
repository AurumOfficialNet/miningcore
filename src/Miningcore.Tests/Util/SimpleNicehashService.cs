using System.Threading;
using System.Threading.Tasks;
using Miningcore.Nicehash;

namespace Miningcore.Tests.Util;

// Simple mock that satisfies the contract without complex dependencies
public class SimpleNicehashService : NicehashService
{
    public SimpleNicehashService() : base(new MockHttpClientFactory(), new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()))
    {
    }
}
