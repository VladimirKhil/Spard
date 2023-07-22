namespace Spard.Service.Contract;

/// <summary>
/// Describes SPARD transformation request.
/// </summary>
public sealed class TransformRequest
{
    /// <summary>
    /// Transformation input.
    /// </summary>
    public string Input { get; set; } = "";

    /// <summary>
    /// SPARD transformation rules.
    /// </summary>
    public string Transform { get; set; } = "";
}
