namespace Spard.Service.Contract;

/// <summary>
/// Describes process result.
/// </summary>
/// <typeparam name="T">Result type.</typeparam>
public sealed class ProcessResult<T>
{
    /// <summary>
    /// Result text.
    /// </summary>
    public T? Result { get; set; }

    /// <summary>
    /// Process execution time.
    /// </summary>
    public TimeSpan Duration { get; set; }
}
