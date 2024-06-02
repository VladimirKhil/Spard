using System.Diagnostics.Metrics;

namespace Spard.Service.Metrics;

/// <summary>
/// Holds service metrics.
/// </summary>
public sealed class OtelMetrics
{
    public const string MeterName = "Spard";

    private Counter<int> TransformCounter { get; }

    private Counter<int> TransformTableCounter { get; }

    private Counter<int> GenerateTableCounter { get; }

    private Counter<int> GenerateSourceCodeCounter { get; }

    public OtelMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        TransformCounter = meter.CreateCounter<int>("transforms");
        TransformTableCounter = meter.CreateCounter<int>("table-transforms");
        GenerateTableCounter = meter.CreateCounter<int>("tables-generated");
        GenerateSourceCodeCounter = meter.CreateCounter<int>("sources-generated");
    }

    public void Transform() => TransformCounter.Add(1);

    public void TransformTable() => TransformTableCounter.Add(1);

    public void GenerateTable() => GenerateTableCounter.Add(1);

    public void GenerateSourceCode() => GenerateSourceCodeCounter.Add(1);
}
