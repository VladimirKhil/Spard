namespace Spard.Service.Models;

/// <summary>
/// Describes SPARD example configuration info.
/// </summary>
public sealed class ExampleModel
{
    /// <summary>
    /// Localized example name. Every Key is a Culture code, and corresponding value is a name in that Culture.
    /// </summary>
    public Dictionary<string, string> Name { get; set; } = new();

    /// <summary>
    /// Example input data.
    /// </summary>
    public string Input { get; set; } = "";

    /// <summary>
    /// SPARD transformation rules.
    /// </summary>
    public string Transform { get; set; } = "";
}
