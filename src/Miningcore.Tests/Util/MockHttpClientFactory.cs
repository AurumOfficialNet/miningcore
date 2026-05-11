using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Miningcore.Tests.Util;

public class MockHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        return new HttpClient();
    }
}
