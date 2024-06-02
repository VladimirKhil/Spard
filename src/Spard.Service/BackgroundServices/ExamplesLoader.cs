using Spard.Service.Contracts;
using Spard.Service.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spard.Service.BackgroundServices;

/// <summary>
/// Loads SPARD examples from <see cref="ExampleFolderName" /> folder into <see cref="IExamplesRepository" />.
/// </summary>
internal sealed class ExamplesLoader(
    IExamplesRepository examplesRepository,
    ILogger<ExamplesLoader> logger) : BackgroundService
{
    /// <summary>
    /// Folder name containing examples.
    /// </summary>
    public const string ExampleFolderName = "Examples";

    /// <summary>
    /// Index file name containing examples info.
    /// </summary>
    public const string IndexFileName = "index.json";

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var examplesFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ExampleFolderName);
        var indexFilePath = Path.Combine(examplesFolderPath, IndexFileName);

        if (!File.Exists(indexFilePath))
        {
            logger.LogWarning("Examples file {fileName} not found!", indexFilePath);
            return Task.CompletedTask;
        }

        var examplesDict = JsonSerializer.Deserialize(
            File.ReadAllText(indexFilePath),
            ExampleModelsContext.Default.DictionaryStringExampleModel)
            ?? [];

        foreach (var exampleEntry in examplesDict)
        {
            if (!int.TryParse(exampleEntry.Key, out var id))
            {
                logger.LogWarning("Example id {exampleId} is not a number!", exampleEntry.Key);
                continue;
            }

            var spardFileName = Path.Combine(examplesFolderPath, $"{exampleEntry.Key}.spard");

            if (!File.Exists(spardFileName))
            {
                logger.LogWarning("Example SPARD file {fileName} not found!", spardFileName);
                continue;
            }

            var spardText = File.ReadAllText(spardFileName);

            var example = new ExampleModel
            {
                Name = exampleEntry.Value.Name,
                Input = exampleEntry.Value.Input,
                Transform = spardText
            };

            examplesRepository.AddExample(id, example);
        }

        return Task.CompletedTask;
    }
}

[JsonSerializable(typeof(Dictionary<string, ExampleModel>))]
internal partial class ExampleModelsContext : JsonSerializerContext { }
