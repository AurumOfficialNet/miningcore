using System.Threading;
using System.Threading.Tasks;
using Miningcore.Nicehash;

namespace Miningcore.Tests.Util;

public class MockNicehashService : NicehashService
{
    public MockNicehashService() : base(null, null)
    {
    }
}
