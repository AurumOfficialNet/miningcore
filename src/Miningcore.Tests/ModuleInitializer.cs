using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Autofac;
using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using Miningcore.Configuration;
using Miningcore.Native;
using Miningcore.Nicehash;
using Miningcore.Rest;
using Miningcore.Tests.Util;
using Miningcore.Time;

namespace Miningcore.Tests;

public static class ModuleInitializer
{
    private static readonly object initLock = new object();

    private static bool isInitialized = false;
    private static IContainer container;
    private static Dictionary<string, CoinTemplate> coinTemplates;

    public static IContainer Container => container;
    public static Dictionary<string, CoinTemplate> CoinTemplates => coinTemplates;

    /// <summary>
    /// Initializes the module.
    /// </summary>
    public static void Initialize()
    {
        lock(initLock)
        {
            if(isInitialized)
                return;

            var builder = new ContainerBuilder();

            builder.RegisterAssemblyModules(typeof(AutofacModule).GetTypeInfo().Assembly);

            // Register RecyclableMemoryStreamManager for tests
            builder.RegisterInstance(new RecyclableMemoryStreamManager());
            
            // Register HttpClientFactory and MemoryCache for NicehashService
            builder.RegisterInstance(new MockHttpClientFactory());
            builder.RegisterInstance(new MemoryCache(new MemoryCacheOptions()));
            builder.RegisterType<SimpleRestClient>().AsSelf();
            builder.RegisterType<NicehashService>().AsSelf();

            // AutoMapper
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var amConf = new MapperConfiguration(cfg => { cfg.AddProfile(new AutoMapperProfile()); }, loggerFactory);

            builder.Register((ctx, parms) => amConf.CreateMapper());

            builder.RegisterType<MockMasterClock>().AsImplementedInterfaces();

            // Autofac Container
            container = builder.Build();

            isInitialized = true;

            // Load coin templates
            var basePath = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            var defaultDefinitions = Path.Combine(basePath, "coins.json");

            var coinDefs = new[]
            {
                defaultDefinitions
            };

            coinTemplates = CoinTemplateLoader.Load(container, coinDefs);

            Cryptonight.InitContexts(1);
        }
    }
}
