using Autofac;
using Miningcore.Configuration;
using Miningcore.Mining;
using Miningcore.Tests.Util;
using Xunit;

namespace Miningcore.Tests.Persistence;

[Collection("ProgramReflectionSerial")]
[Trait("Category", "PreUpgrade")]
public class ProgramPersistenceConfigurationTests : TestBase
{
    [Fact]
    public void Program_OriginalPersistenceConfiguration_ThrowsWhenRequiredButMissing()
    {
        ProgramTestReflection.SetProgramLogger();
        ProgramTestReflection.SetProgramClusterConfig(new ClusterConfig
        {
            ShareRelay = null,
            Persistence = null,
            PaymentProcessing = new ClusterPaymentProcessingConfig { Enabled = true },
            Pools = []
        });

        var builder = new ContainerBuilder();

        var ex = Assert.Throws<PoolStartupException>(() => ProgramTestReflection.InvokeProgramPrivateStatic("ConfigurePersistence", builder));
        Assert.Contains("Persistence is not configured", ex.Message);
    }

    [Fact]
    public void Program_OriginalPostgresConfiguration_ValidatesMissingHost()
    {
        ProgramTestReflection.SetProgramLogger();

        var builder = new ContainerBuilder();
        var pg = new PostgresConfig
        {
            Host = null,
            Port = 5432,
            Database = "miningcore",
            User = "miner"
        };

        var ex = Assert.Throws<PoolStartupException>(() => ProgramTestReflection.InvokeProgramPrivateStatic("ConfigurePostgres", pg, builder));
        Assert.Contains("invalid or missing 'host'", ex.Message);
    }
}