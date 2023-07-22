namespace Spard.Service.Contract;

/// <summary>
/// Describes SPARD execution example base info.
/// </summary>
public class SpardExampleBaseInfo
{
    /// <summary>
    /// Example identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Example name.
    /// </summary>
    public string Name { get; set; } = "";
}
