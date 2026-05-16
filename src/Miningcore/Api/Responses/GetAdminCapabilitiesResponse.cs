namespace Miningcore.Api.Responses;

public class AdminCapabilitiesResponse
{
    public bool CanAccessAdmin { get; set; }
    public string[] Permissions { get; set; }
}
