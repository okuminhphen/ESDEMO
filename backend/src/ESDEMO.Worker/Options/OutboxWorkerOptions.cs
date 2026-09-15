using System.ComponentModel.DataAnnotations;

namespace ESDEMO.Worker.Options;

public sealed class OutboxWorkerOptions
{
    public const string SectionName = "OutboxWorker";

    [Range(1, 100)]
    public int BatchSize { get; set; } = 20;

    [Range(1, 60)]
    public int PollingIntervalSeconds { get; set; } = 2;

    [Range(5, 300)]
    public int LeaseSeconds { get; set; } = 30;

    [Range(1, 3600)]
    public int MaxRetryDelaySeconds { get; set; } = 300;

    [Range(1, 60)]
    public int PublishConfirmationTimeoutSeconds { get; set; } = 10;
}
