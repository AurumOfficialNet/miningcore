using Microsoft.AspNetCore.Http;
using Miningcore.Configuration;
using Miningcore.Util;
using NLog;
using System.Net;
using System.Net.Mime;

namespace Miningcore.Api.Middlewares;

public class AdminApiAuthorizationMiddleware
{
    public AdminApiAuthorizationMiddleware(RequestDelegate next, ClusterConfig clusterConfig)
    {
        this.next = next;
        logger = LogManager.GetCurrentClassLogger();

        trustedProxyIpWhitelist = BuildIpWhitelist(clusterConfig.Api?.AdminTrustedProxyIpWhitelist);

        allowedSubjects = (clusterConfig.Api?.AdminClientAllowSubjects ?? Array.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        allowedSerials = (clusterConfig.Api?.AdminClientAllowSerials ?? Array.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(NormalizeSerial)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private const string VerifyHeader = "X-Admin-Client-Verify";
    private const string SubjectHeader = "X-Admin-Client-Subject";
    private const string IssuerHeader = "X-Admin-Client-Issuer";
    private const string SerialHeader = "X-Admin-Client-Serial";

    private readonly RequestDelegate next;
    private readonly ILogger logger;
    private readonly HashSet<string> allowedSubjects;
    private readonly HashSet<string> allowedSerials;
    private readonly IPAddress[] trustedProxyIpWhitelist;

    public async Task Invoke(HttpContext context)
    {
        if(!IsAdminRequest(context.Request.Path))
        {
            await next(context);
            return;
        }

        var remoteAddress = context.Connection.RemoteIpAddress;

        if(remoteAddress == null || !trustedProxyIpWhitelist.Any(x => x.Equals(remoteAddress)))
        {
            logger.Warn(() => $"Rejected admin request from untrusted proxy {remoteAddress}");
            throw new ApiException("Admin access denied", HttpStatusCode.Forbidden);
        }

        var verify = context.Request.Headers[VerifyHeader].ToString();
        var subject = context.Request.Headers[SubjectHeader].ToString();
        var issuer = context.Request.Headers[IssuerHeader].ToString();
        var serial = NormalizeSerial(context.Request.Headers[SerialHeader].ToString());

        if(!string.Equals(verify, "SUCCESS", StringComparison.OrdinalIgnoreCase))
        {
            logger.Warn(() => $"Rejected admin request due to failed certificate verification from {remoteAddress}");
            throw new ApiException("Admin access denied", HttpStatusCode.Forbidden);
        }

        if((!string.IsNullOrWhiteSpace(serial) && allowedSerials.Contains(serial)) ||
            (!string.IsNullOrWhiteSpace(subject) && allowedSubjects.Contains(subject.Trim())))
        {
            await next(context);
            return;
        }

        logger.Warn(() => $"Rejected admin request for unrecognized identity subject={subject}, issuer={issuer}, serial={serial}");
        throw new ApiException("Admin access denied", HttpStatusCode.Forbidden);
    }

    private static bool IsAdminRequest(PathString path)
    {
        var value = path.Value;

        return !string.IsNullOrEmpty(value) &&
            value.StartsWith("/api/admin", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeSerial(string serial)
    {
        if(string.IsNullOrWhiteSpace(serial))
            return null;

        return serial
            .Replace(":", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Trim()
            .ToUpperInvariant();
    }

    private static IPAddress[] BuildIpWhitelist(string[] configuredAddresses)
    {
        var result = configuredAddresses?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(IPAddress.Parse)
            .Distinct()
            .ToList() ?? new List<IPAddress>();

        if(result.Count == 0)
        {
            result.Add(IPAddress.Loopback);
            result.Add(IPAddress.IPv6Loopback);
            result.Add(IPUtils.IPv4LoopBackOnIPv6);
        }

        return result.ToArray();
    }
}
