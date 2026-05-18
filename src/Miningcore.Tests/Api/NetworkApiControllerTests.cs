using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Miningcore.Api.Controllers;
using Miningcore.Api.Middlewares;
using Miningcore.Api.Responses;
using Miningcore.Configuration;
using Miningcore.Messaging;
using Miningcore.Mining;
using Miningcore.Persistence;
using Miningcore.Persistence.Model;
using Miningcore.Persistence.Repositories;
using Miningcore.Tests.Util;
using Miningcore.Time;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Xunit;

namespace Miningcore.Tests.Api;

public class NetworkApiControllerTests
{
    [Fact]
    public async Task Routes_ReturnExpectedJson()
    {
        await using var daemon = await FakeDaemonServer.StartAsync();
        using var host = await BuildBackendHostAsync(daemon.Port);

        var client = host.GetTestClient();

        var overview = await ReadJsonAsync<NetworkOverviewResponse>(client, "/api/network/acg/overview");
        Assert.Equal(42UL, overview.BlockHeight);
        Assert.Equal(123.45, overview.NetworkHashrate, 3);
        Assert.Equal(678.9, overview.NetworkDifficulty, 1);
        Assert.Equal(11, overview.ConnectedPeers);
        Assert.Equal(321, overview.MempoolTransactions);
        Assert.Equal(123456L, overview.MempoolBytes);
        Assert.Equal(1000m, overview.TotalSupply);
        Assert.NotEmpty(overview.HashrateSeries);
        Assert.NotEmpty(overview.DifficultySeries);

        var blocks = await ReadJsonAsync<NetworkBlockSummaryResponse[]>(client, "/api/network/acg/blocks");
        Assert.NotEmpty(blocks);
        Assert.Equal("block-5", blocks[0].Hash);
        Assert.Equal(2, blocks[0].TxCount);

        var block = await ReadJsonAsync<NetworkBlockDetailResponse>(client, "/api/network/acg/block/5");
        Assert.Equal(5UL, block.Height);
        Assert.Equal("block-5", block.Hash);
        Assert.Equal("block-4", block.PrevHash);
        Assert.Equal(2, block.Transactions.Length);
        Assert.Single(block.Transactions[1].Inputs);
        Assert.Equal("miner-1", block.Transactions[1].Inputs[0].Address);

        var top100 = await ReadJsonAsync<NetworkTopHolderResponse[]>(client, "/api/network/acg/top100");
        Assert.Equal(2, top100.Length);
        Assert.Equal(1, top100[0].Rank);
        Assert.Equal("addr1", top100[0].Address);
        Assert.True(top100[0].PercentOfSupply > top100[1].PercentOfSupply);
    }

    private static async Task<T> ReadJsonAsync<T>(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        var json = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, $"GET {path} -> {(int) response.StatusCode} {json}");
        Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);

        return JsonConvert.DeserializeObject<T>(json)!;
    }

    private static async Task<IHost> BuildBackendHostAsync(int daemonPort)
    {
        Miningcore.Tests.ModuleInitializer.Initialize();

        var clusterConfig = new ClusterConfig
        {
            Pools = new[]
            {
                new PoolConfig
                {
                    Id = "acg",
                    Enabled = true,
                    Daemons = new[]
                    {
                        new DaemonEndpointConfig
                        {
                            Host = "127.0.0.1",
                            Port = daemonPort,
                            User = "user",
                            Password = "password"
                        }
                    }
                }
            }
        };

        var statsRepo = Substitute.For<IStatsRepository>();
        statsRepo.GetLastPoolStatsAsync(Arg.Any<System.Data.IDbConnection>(), "acg", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new Miningcore.Persistence.Model.PoolStats
            {
                BlockHeight = 42,
                NetworkHashrate = 123.45,
                NetworkDifficulty = 678.9,
                Created = DateTime.UtcNow
            }));

        statsRepo.GetPoolPerformanceBetweenAsync(Arg.Any<System.Data.IDbConnection>(), "acg", Arg.Any<SampleInterval>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new[]
            {
                new Miningcore.Persistence.Model.PoolStats { Created = DateTime.UtcNow.AddHours(-1), NetworkHashrate = 100, NetworkDifficulty = 200 },
                new Miningcore.Persistence.Model.PoolStats { Created = DateTime.UtcNow, NetworkHashrate = 150, NetworkDifficulty = 250 }
            }));

        var balanceRepo = Substitute.For<IBalanceRepository>();
        balanceRepo.GetPoolBalancesOverThresholdAsync(Arg.Any<System.Data.IDbConnection>(), "acg", 0m)
            .Returns(Task.FromResult(new[]
            {
                new Miningcore.Persistence.Model.Balance { Address = "addr1", Amount = 60m },
                new Miningcore.Persistence.Model.Balance { Address = "addr2", Amount = 40m }
            }));

        var connectionFactory = Substitute.For<IConnectionFactory>();
        connectionFactory.OpenConnectionAsync().Returns(Task.FromResult(Substitute.For<System.Data.IDbConnection>()));

        var builder = new HostBuilder()
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureContainer<ContainerBuilder>(container =>
            {
                container.RegisterInstance(clusterConfig);
                container.RegisterInstance(Miningcore.Tests.ModuleInitializer.Container.Resolve<AutoMapper.IMapper>()).AsImplementedInterfaces();
                container.RegisterInstance(new MockMasterClock()).AsImplementedInterfaces();
                container.RegisterInstance(new MockMessageBus()).AsImplementedInterfaces();
                container.RegisterInstance(connectionFactory).AsImplementedInterfaces();
                container.RegisterInstance(statsRepo).AsImplementedInterfaces();
                container.RegisterInstance(balanceRepo).AsImplementedInterfaces();
                container.RegisterInstance(new Newtonsoft.Json.JsonSerializerSettings
                {
                    ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver()
                });
                container.RegisterInstance(new ConcurrentDictionary<string, IMiningPool>());
                container.RegisterType<NetworkApiController>().AsSelf();
            })
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services.AddHttpContextAccessor();
                    services.AddMvc(options => options.EnableEndpointRouting = false)
                        .AddControllersAsServices()
                        .AddApplicationPart(typeof(NetworkApiController).Assembly);
                });

                webHost.Configure(app =>
                {
                    app.UseMiddleware<ApiExceptionHandlingMiddleware>();
                    app.UseMvc();
                });
            });

        var host = await builder.StartAsync();
        return host;
    }

    private sealed class FakeDaemonServer : IAsyncDisposable
    {
        private readonly IHost host;

        private FakeDaemonServer(IHost host, int port)
        {
            this.host = host;
            Port = port;
        }

        public int Port { get; }

        public static async Task<FakeDaemonServer> StartAsync()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            var builder = new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseKestrel();
                    webHost.UseUrls($"http://127.0.0.1:{port}");
                    webHost.Configure(app =>
                    {
                        app.Run(async context =>
                        {
                            if(!string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase))
                            {
                                context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                                return;
                            }

                            var requestJson = await new StreamReader(context.Request.Body).ReadToEndAsync();
                            var requestToken = JToken.Parse(requestJson);

                            if(requestToken.Type == JTokenType.Array)
                            {
                                var responses = requestToken.Children().Select(HandleRpcRequest).ToArray();
                                await context.Response.WriteAsync(JsonConvert.SerializeObject(responses));
                                return;
                            }

                            var response = HandleRpcRequest(requestToken);
                            await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
                        });
                    });
                });

            var host = builder.Build();
            await host.StartAsync();
            return new FakeDaemonServer(host, port);
        }

        public async ValueTask DisposeAsync()
        {
            await host.StopAsync();
            host.Dispose();
        }

        private static object HandleRpcRequest(JToken request)
        {
            var method = request.Value<string>("method");
            var id = request["id"]?.ToObject<object>();
            var parameters = request["params"] as JArray;

            return method switch
            {
                "getblockchaininfo" => new { jsonrpc = "2.0", id, result = new { blocks = 5UL, difficulty = 1234.5 } },
                "getnetworkinfo" => new { jsonrpc = "2.0", id, result = new { connections = 11 } },
                "getmempoolinfo" => new { jsonrpc = "2.0", id, result = new { size = 321, bytes = 123456L } },
                "gettxoutsetinfo" => new { jsonrpc = "2.0", id, result = new { total_amount = 1000.0m } },
                "getnetworkhashps" => new { jsonrpc = "2.0", id, result = 4321.0 },
                "getblockhash" => new { jsonrpc = "2.0", id, result = $"block-{parameters![0].Value<int>()}" },
                "getblock" => BuildBlockResponse(id, parameters!),
                "getrawtransaction" => BuildRawTransactionResponse(id, parameters!),
                _ => new { jsonrpc = "2.0", id, error = new { code = -32601, message = $"Unknown method {method}" } }
            };
        }

        private static object BuildBlockResponse(object id, JArray parameters)
        {
            var hash = parameters[0]!.Value<string>();
            var height = int.Parse(hash.Split('-').Last());
            var spendTxId = $"spend-{height}";

            return new
            {
                jsonrpc = "2.0",
                id,
                result = new
                {
                    hash,
                    height,
                    previousblockhash = height > 0 ? $"block-{height - 1}" : null,
                    time = 1_700_000_000 + height,
                    difficulty = 99.5,
                    tx = new object[]
                    {
                        new
                        {
                            txid = $"tx-{height}",
                            hash = $"tx-{height}",
                            vin = new object[]
                            {
                                new { coinbase = "01" }
                            },
                            vout = new object[]
                            {
                                new { n = 0, value = 50m, scriptPubKey = new { address = "miner-1" } }
                            }
                        },
                        new
                        {
                            txid = spendTxId,
                            hash = spendTxId,
                            vin = new object[]
                            {
                                new { txid = $"tx-{Math.Max(0, height - 1)}", vout = 0 }
                            },
                            vout = new object[]
                            {
                                new { n = 0, value = 49.5m, scriptPubKey = new { address = "miner-2" } }
                            }
                        }
                    }
                }
            };
        }

        private static object BuildRawTransactionResponse(object id, JArray parameters)
        {
            var txid = parameters[0]!.Value<string>();

            return new
            {
                jsonrpc = "2.0",
                id,
                result = new
                {
                    txid,
                    vout = new object[]
                    {
                        new { n = 0, value = 50m, scriptPubKey = new { address = "miner-1" } }
                    }
                }
            };
        }
    }
}
