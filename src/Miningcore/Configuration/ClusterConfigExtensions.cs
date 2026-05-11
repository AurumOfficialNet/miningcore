using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Autofac;
using JetBrains.Annotations;
using Miningcore.Blockchain.Bitcoin;
using Miningcore.Configuration;
using Miningcore.Crypto;
using Miningcore.Crypto.Hashing;
using Miningcore.Crypto.Hashing.Algorithms;
using Miningcore.Extensions;
using Miningcore.Messaging;
using Miningcore.Nicehash;
using Miningcore.Persistence;
using Miningcore.Persistence.Repositories;
using Miningcore.Time;
using Miningcore.Util;
using NBitcoin;
using Newtonsoft.Json;

namespace Miningcore.Configuration;

public abstract partial class CoinTemplate
{
    public T As<T>() where T : CoinTemplate
    {
        return (T) this;
    }

    public abstract string GetAlgorithmName();

    /// <summary>
    /// json source file where this template originated from
    /// </summary>
    [JsonIgnore]
    public string Source { get; set; }
}

public partial class BitcoinTemplate
{
    private readonly Lazy<BigInteger> diff1Value;
    private readonly Lazy<BigInteger> diff1BValue;

    [JsonIgnore]
    public BigInteger Diff1Value => diff1Value.Value;

    [JsonIgnore]
    public BigInteger Diff1BValue => diff1BValue.Value;
    public BitcoinTemplate()
    {
        coinbaseHasherValue = new Lazy<IHashAlgorithm>(() =>
            HashAlgorithmFactory.GetHash(ComponentContext, CoinbaseHasher));

        headerHasherValue = new Lazy<IHashAlgorithm>(() =>
            HashAlgorithmFactory.GetHash(ComponentContext, HeaderHasher));

        blockHasherValue = new Lazy<IHashAlgorithm>(() =>
            HashAlgorithmFactory.GetHash(ComponentContext, BlockHasher));

        posBlockHasherValue = new Lazy<IHashAlgorithm>(() =>
            HashAlgorithmFactory.GetHash(ComponentContext, PoSBlockHasher));

        diff1Value = new Lazy<BigInteger>(() =>
        {
            var network = GetNetwork(ChainName.Mainnet); // Default to mainnet for initialization
            if(string.IsNullOrEmpty(network?.Diff1))
                return BitcoinConstants.Diff1;
                
            return BigInteger.Parse(network.Diff1, NumberStyles.HexNumber);
        });

        diff1BValue = new Lazy<BigInteger>(() =>
        {
            var network = GetNetwork(ChainName.Mainnet); // Default to mainnet for initialization
            if(string.IsNullOrEmpty(network?.Diff1))
                return BitcoinConstants.Diff1;

            return BigInteger.Parse(network.Diff1, NumberStyles.HexNumber);
        });
    }

    private readonly Lazy<IHashAlgorithm> coinbaseHasherValue;
    private readonly Lazy<IHashAlgorithm> headerHasherValue;
    private readonly Lazy<IHashAlgorithm> blockHasherValue;
    private readonly Lazy<IHashAlgorithm> posBlockHasherValue;

    public IComponentContext ComponentContext { get; [UsedImplicitly] init; }

    public IHashAlgorithm CoinbaseHasherValue => coinbaseHasherValue.Value;
    public IHashAlgorithm HeaderHasherValue => headerHasherValue.Value;
    public IHashAlgorithm BlockHasherValue => blockHasherValue.Value;
    public IHashAlgorithm PoSBlockHasherValue => posBlockHasherValue.Value;

    public BitcoinNetworkParams GetNetwork(ChainName chain)
    {
        if(Networks == null || Networks.Count == 0)
            return null;

        if(chain == ChainName.Mainnet)
            return Networks["main"];
        else if(chain == ChainName.Testnet)
            return Networks["test"];
        else if(chain == ChainName.Regtest)
            return Networks["regtest"];

        throw new NotSupportedException("unsupported network type");
    }

    #region Overrides of CoinTemplate

    public override string GetAlgorithmName()
    {
        var hash = HeaderHasherValue;

        if(hash.GetType() == typeof(DigestReverser))
            return ((DigestReverser) hash).Upstream.GetType().Name;

        return hash.GetType().Name;
    }

    #endregion
}

public partial class EquihashCoinTemplate
{
    public partial class EquihashNetworkParams
    {
        public EquihashNetworkParams()
        {
            diff1Value = new Lazy<BigInteger>(() =>
            {
                if(string.IsNullOrEmpty(Diff1))
                    throw new InvalidOperationException("Diff1 has not yet been initialized");

                return BigInteger.Parse(Diff1, NumberStyles.HexNumber);
            });

            diff1BValue = new Lazy<BigInteger>(() =>
            {
                if(string.IsNullOrEmpty(Diff1))
                    throw new InvalidOperationException("Diff1 has not yet been initialized");

                return BigInteger.Parse(Diff1, NumberStyles.HexNumber);
            });
        }

        private readonly Lazy<BigInteger> diff1Value;
        private readonly Lazy<BigInteger> diff1BValue;

        [JsonIgnore]
        public BigInteger Diff1Value => diff1Value.Value;

        [JsonIgnore]
        public BigInteger Diff1BValue => diff1BValue.Value;

        [JsonIgnore]
        public ulong FoundersRewardSubsidySlowStartShift => FoundersRewardSubsidySlowStartInterval / 2;

        [JsonIgnore]
        public ulong LastFoundersRewardBlockHeight => FoundersRewardSubsidyHalvingInterval + FoundersRewardSubsidySlowStartShift - 1;
    }

    public EquihashNetworkParams GetNetwork(ChainName chain)
    {
        if(chain == ChainName.Mainnet)
            return Networks["main"];
        else if(chain == ChainName.Testnet)
            return Networks["test"];
        else if(chain == ChainName.Regtest)
            return Networks["regtest"];

        throw new NotSupportedException("unsupported network type");
    }

    #region Overrides of CoinTemplate

    public override string GetAlgorithmName()
    {
        // TODO: return variant
        return "Equihash";
    }

    #endregion
}

public partial class CryptonoteCoinTemplate
{
    #region Overrides of CoinTemplate

    public override string GetAlgorithmName()
    {
//        switch(Hash)
//        {
//            case CryptonightHashType.RandomX:
//                return "RandomX";
//        }

        return Hash.ToString();
    }

    #endregion
}

public partial class EthereumCoinTemplate
{
    #region Overrides of CoinTemplate

    public override string GetAlgorithmName()
    {
        return "Ethhash";
    }

    #endregion
}

public partial class ErgoCoinTemplate
{
    #region Overrides of CoinTemplate

    public override string GetAlgorithmName()
    {
        return "Autolykos";
    }

    #endregion
}

public partial class PoolConfig
{
    /// <summary>
    /// Back-reference to coin template for this pool
    /// </summary>
    [JsonIgnore]
    public CoinTemplate Template { get; set; }
}
