namespace TexasPortal.Application.Common.Options;

public sealed class WorkerOptions
{
    public const string SectionName = "Worker";

    public bool Enabled { get; set; } = true;
    public int PollingIntervalMilliseconds { get; set; } = 1000;
    public int BatchSize { get; set; } = 50;
    public int MaxRetries { get; set; } = 3;
}
