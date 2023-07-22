namespace Spard.Service.Contract;

/// <summary>
/// Describes SPARD table transform result.
/// </summary>
public sealed class TransformTableResult
{
    /// <summary>
    /// Result text.
    /// </summary>
    public string Result { get; set; } = "";

    /// <summary>
    /// Does standard SPARD transformer generate the same result.
    /// </summary>
    public bool IsStandardResultTheSame { get; set; }

    /// <summary>
    /// Time for parsing expression and building standard transformer.
    /// </summary>
    public TimeSpan ParseDuration { get; set; }

    /// <summary>
    /// Time for building table transformer.
    /// </summary>
    public TimeSpan TableBuildDuration { get; set; }

    /// <summary>
    /// Standard transform duration.
    /// </summary>
    public TimeSpan StandardTransformDuration { get; set; }

    /// <summary>
    /// Table transform duration.
    /// </summary>
    public TimeSpan TableTransformDuration { get; set; }
}
