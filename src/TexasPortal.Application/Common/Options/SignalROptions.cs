namespace TexasPortal.Application.Common.Options;

public sealed class SignalROptions
{
    public const string SectionName = "SignalR";

    public bool Enabled { get; set; } = true;
    public string HubRoute { get; set; } = "/hubs/texas-sync";
    public int ClientTimeoutSeconds { get; set; } = 30;
    public int KeepAliveSeconds { get; set; } = 15;
    public int HandshakeTimeoutSeconds { get; set; } = 15;
    public string? RedisConnectionString { get; set; }
}
