namespace Miningcore.Api.Responses;

public class ReadinessResponse
{
    public string Status { get; set; }
    public DateTime TimestampUtc { get; set; }
    public DependencyCheckResult[] Checks { get; set; }
}

public class DependencyCheckResult
{
    public string Name { get; set; }
    public string Status { get; set; }
    public bool Required { get; set; }
    public long DurationMs { get; set; }
    public string Details { get; set; }
}
