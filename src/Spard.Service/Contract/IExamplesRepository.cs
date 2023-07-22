using Spard.Service.Helpers;
using Spard.Service.Models;

namespace Spard.Service.Contract;

/// <summary>
/// Provides methods for working with SPARD examples.
/// </summary>
public interface IExamplesRepository
{
    /// <summary>
    /// Adds new example.
    /// </summary>
    /// <param name="exampleId">Example id.</param>
    /// <param name="exampleModel">Example information.</param>
    void AddExample(int exampleId, ExampleModel exampleModel);

    /// <summary>
    /// Gets all examples.
    /// </summary>
    /// <param name="culture">Culture to use.</param>
    IEnumerable<SpardExampleBaseInfo> GetExamples(string culture = CultureHelper.DefaultCulture);

    /// <summary>
    /// Gets example by Id.
    /// </summary>
    /// <param name="id">Example Id.</param>
    /// <param name="culture">Culture to use.</param>
    SpardExampleInfo? TryGetExample(int id, string culture = CultureHelper.DefaultCulture);
}
