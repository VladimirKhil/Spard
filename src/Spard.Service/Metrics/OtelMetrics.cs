using System.Diagnostics.Metrics;

namespace Spard.Service.Metrics;

/// <summary>
/// Holds service metrics.
/// </summary>
public sealed class OtelMetrics
{
    private Counter<int> TransformCounter { get; }

    private Counter<int> TransformTableCounter { get; }

    private Counter<int> GenerateTableCounter { get; }

    private Counter<int> GenerateSourceCodeCounter { get; }

    public string MeterName { get; }

    public OtelMetrics(string meterName = "Spard")
    {
        var meter = new Meter(meterName);
        MeterName = meterName;

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
