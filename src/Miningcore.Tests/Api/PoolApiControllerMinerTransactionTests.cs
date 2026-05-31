using System;
using System.Collections.Concurrent;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Miningcore.Api.Controllers;
using Miningcore.Api.Middlewares;
using Miningcore.Configuration;
using Miningcore.Mining;
using Miningcore.Payments.Abstractions;
using Miningcore.Persistence;
using Miningcore.Persistence.Repositories;
using ModelMinerTransaction = Miningcore.Persistence.Model.MinerTransaction;
using Miningcore.Tests.Util;
using Miningcore.Time;
using Newtonsoft.Json;
using NSubstitute;
using Xunit;

namespace Miningcore.Tests.Api;

public class PoolApiControllerMinerTransactionTests
{
    private static readonly DateTime BaseTime = new DateTime(2026, 5, 17, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetMinerTransactions_ReturnsExpectedRecords()
    {
        var miningTxRepo = BuildMockRepo(count: 2);
        using var host = await BuildHostAsync(miningTxRepo);

        var client = host.GetTestClient();
        var txns = await GetJsonAsync<MinerTransactionDto[]>(
            client, "/api/coins/btc/miner/addr1/transactions?page=0&pageSize=15");

        Assert.Equal(2, txns.Length);
        Assert.Equal("tx1", txns[0].TxId);
        Assert.Equal("addr1", txns[0].Address);
        Assert.Equal(1.5m, txns[0].Amount);
        Assert.Equal("coinbase", txns[0].Category);
        Assert.Equal(100L, txns[0].BlockHeight);
        Assert.Equal("https://explorer.test/tx/tx1", txns[0].TransactionInfoLink);
        Assert.Equal("https://explorer.test/address/addr1", txns[0].AddressInfoLink);
    }

    [Fact]
    public async Task GetMinerTransactions_EmptyResult_ReturnsEmptyArray()
    {
        var miningTxRepo = Substitute.For<IMinerTransactionRepository>();
        miningTxRepo.PageTransactionsAsync(Arg.Any<IDbConnection>(), "btc", "addr1", 0, 15, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Array.Empty<ModelMinerTransaction>()));

        using var host = await BuildHostAsync(miningTxRepo);
        var client = host.GetTestClient();

        var txns = await GetJsonAsync<MinerTransactionDto[]>(
            client, "/api/coins/btc/miner/addr1/transactions?page=0&pageSize=15");

        Assert.Empty(txns);
    }

    [Fact]
    public async Task GetMinerTransactions_MissingAddress_Returns404()
    {
        var miningTxRepo = BuildMockRepo(count: 0);
        using var host = await BuildHostAsync(miningTxRepo);
        var client = host.GetTestClient();
        var ct = TestContext.Current.CancellationToken;

        var response = await client.GetAsync("/api/coins/btc/miner//transactions", ct);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMinerTransactionsV2_ReturnsPageCountAndData()
    {
        var miningTxRepo = BuildMockRepo(count: 3, totalCount: 30);
        using var host = await BuildHostAsync(miningTxRepo);

        var client = host.GetTestClient();
        var result = await GetJsonAsync<PagedResultResponse<MinerTransactionDto[]>>(
            client, "/api/v2/coins/btc/miner/addr1/transactions?page=0&pageSize=15");

        Assert.Equal(3, result.Result.Length);
        // pageCount = floor(30 / 15) = 2
        Assert.Equal(2u, result.PageCount);
    }

    [Fact]
    public async Task GetMinerTransactionsV2_EnrichesExplorerLinks()
    {
        var miningTxRepo = BuildMockRepo(count: 1, totalCount: 1);
        using var host = await BuildHostAsync(miningTxRepo);

        var client = host.GetTestClient();
        var result = await GetJsonAsync<PagedResultResponse<MinerTransactionDto[]>>(
            client, "/api/v2/coins/btc/miner/addr1/transactions?page=0&pageSize=15");

        Assert.Single(result.Result);
        Assert.Equal("https://explorer.test/tx/tx1", result.Result[0].TransactionInfoLink);
        Assert.Equal("https://explorer.test/address/addr1", result.Result[0].AddressInfoLink);
    }

    [Fact]
    public async Task GetMinerTransactions_UnknownCoin_Returns404()
    {
        var miningTxRepo = BuildMockRepo(count: 0);
        using var host = await BuildHostAsync(miningTxRepo);
        var client = host.GetTestClient();
        var ct = TestContext.Current.CancellationToken;

        var response = await client.GetAsync("/api/coins/unknown-coin/miner/addr1/transactions", ct);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ------------------------------------------------------------------ //
    //  Helpers
    // ------------------------------------------------------------------ //

    private static IMinerTransactionRepository BuildMockRepo(int count, uint totalCount = 0)
    {
        var rows = Enumerable.Range(1, count).Select(i => new ModelMinerTransaction
        {
            Coin = "btc",
            TxId = $"tx{i}",
            Address = "addr1",
            Amount = 1.5m,
            Category = "coinbase",
            BlockHeight = 100,
            BlockHash = "blockhash1",
            BlockTime = BaseTime,
            Created = BaseTime
        }).ToArray();

        var repo = Substitute.For<IMinerTransactionRepository>();
        repo.PageTransactionsAsync(Arg.Any<IDbConnection>(), "btc", "addr1", Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(rows));
        repo.GetTransactionCountAsync(Arg.Any<IDbConnection>(), "btc", "addr1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(totalCount > 0 ? totalCount : (uint) count));

        return repo;
    }

    private static async Task<T> GetJsonAsync<T>(HttpClient client, string path)
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await client.GetAsync(path, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        Assert.True(response.IsSuccessStatusCode, $"GET {path} -> {(int) response.StatusCode}: {body}");
        return JsonConvert.DeserializeObject<T>(body)!;
    }

    private static async Task<IHost> BuildHostAsync(IMinerTransactionRepository miningTxRepo)
    {
        Miningcore.Tests.ModuleInitializer.Initialize();

        var poolTemplate = new BitcoinTemplate
        {
            Family = CoinFamily.Bitcoin,
            ExplorerTxLink = "https://explorer.test/tx/{0}",
            ExplorerAccountLink = "https://explorer.test/address/{0}"
        };

        var clusterConfig = new ClusterConfig
        {
            Pools = new[]
            {
                new PoolConfig
                {
                    Id = "btc1",
                    Coin = "btc",
                    Enabled = true,
                    Template = poolTemplate
                }
            }
        };

        var connectionFactory = Substitute.For<IConnectionFactory>();
        connectionFactory.OpenConnectionAsync()
            .Returns(Task.FromResult(Substitute.For<IDbConnection>()));

        var statsRepo = Substitute.For<IStatsRepository>();
        var blocksRepo = Substitute.For<IBlockRepository>();
        var paymentsRepo = Substitute.For<IPaymentRepository>();
        var minerRepo = Substitute.For<IMinerRepository>();
        var shareRepo = Substitute.For<IShareRepository>();
        var payoutSchedulerState = Substitute.For<IPayoutSchedulerState>();
        var clock = new MockMasterClock { CurrentTime = BaseTime };

        var builder = new HostBuilder()
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureContainer<ContainerBuilder>(container =>
            {
                container.RegisterInstance(clusterConfig);
                container.RegisterInstance(Miningcore.Tests.ModuleInitializer.Container.Resolve<AutoMapper.IMapper>()).AsImplementedInterfaces();
                container.RegisterInstance(clock).AsImplementedInterfaces();
                container.RegisterInstance(new MockMessageBus()).AsImplementedInterfaces();
                container.RegisterInstance(connectionFactory).AsImplementedInterfaces();
                container.RegisterInstance(statsRepo).AsImplementedInterfaces();
                container.RegisterInstance(blocksRepo).AsImplementedInterfaces();
                container.RegisterInstance(paymentsRepo).AsImplementedInterfaces();
                container.RegisterInstance(minerRepo).AsImplementedInterfaces();
                container.RegisterInstance(shareRepo).AsImplementedInterfaces();
                container.RegisterInstance(miningTxRepo).AsImplementedInterfaces();
                container.RegisterInstance(payoutSchedulerState).AsImplementedInterfaces();
                container.RegisterInstance(new ConcurrentDictionary<string, IMiningPool>());
                container.RegisterType<PoolApiController>().AsSelf();
            })
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services.AddHttpContextAccessor();
                    services.AddMvc(options => options.EnableEndpointRouting = false)
                        .AddControllersAsServices()
                        .AddApplicationPart(typeof(PoolApiController).Assembly);
                });
                webHost.Configure(app =>
                {
                    app.UseMiddleware<ApiExceptionHandlingMiddleware>();
                    app.UseMvc();
                });
            });

        return await builder.StartAsync();
    }

    // Minimal paged wrapper matching the wire format
    private sealed class PagedResultResponse<T>
    {
        public uint PageCount { get; set; }
        public T Result { get; set; }
    }

    // Local DTO matching the JSON fields returned by the API (avoids type ambiguity)
    private sealed class MinerTransactionDto
    {
        public string TxId { get; set; }
        public string Address { get; set; }
        public string AddressInfoLink { get; set; }
        public decimal Amount { get; set; }
        public string Category { get; set; }
        public long BlockHeight { get; set; }
        public string BlockHash { get; set; }
        public string TransactionInfoLink { get; set; }
        public DateTime BlockTime { get; set; }
    }
}
