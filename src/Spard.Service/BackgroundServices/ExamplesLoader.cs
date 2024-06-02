using Spard.Service.Contracts;
using Spard.Service.Models;
using System.Text.Json;

namespace Spard.Service.BackgroundServices;

/// <summary>
/// Loads SPARD examples from <see cref="ExampleFolderName" /> folder into <see cref="IExamplesRepository" />.
/// </summary>
internal sealed class ExamplesLoader : BackgroundService
{
    /// <summary>
    /// Folder name containing examples.
    /// </summary>
    public const string ExampleFolderName = "Examples";

    /// <summary>
    /// Index file name containing examples info.
    /// </summary>
    public const string IndexFileName = "index.json";

    private readonly IExamplesRepository _examplesRepository;
    private readonly ILogger<ExamplesLoader> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="ExamplesLoader" /> class.
    /// </summary>
    public ExamplesLoader(IExamplesRepository examplesRepository, ILogger<ExamplesLoader> logger)
    {
        _examplesRepository = examplesRepository;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var examplesFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ExampleFolderName);
        var indexFilePath = Path.Combine(examplesFolderPath, IndexFileName);

        if (!File.Exists(indexFilePath))
        {
            _logger.LogWarning("Examples file {fileName} not found!", indexFilePath);
            return Task.CompletedTask;
        }

        var examplesDict = JsonSerializer.Deserialize<Dictionary<string, ExampleModel>>(File.ReadAllText(indexFilePath))
            ?? new Dictionary<string, ExampleModel>();

        foreach (var exampleEntry in examplesDict)
        {
            if (!int.TryParse(exampleEntry.Key, out var id))
            {
                _logger.LogWarning("Example id {exampleId} is not a number!", exampleEntry.Key);
                continue;
            }

            var spardFileName = Path.Combine(examplesFolderPath, $"{exampleEntry.Key}.spard");

            if (!File.Exists(spardFileName))
            {
                _logger.LogWarning("Example SPARD file {fileName} not found!", spardFileName);
                continue;
            }

            var spardText = File.ReadAllText(spardFileName);

            var example = new ExampleModel
            {
                Name = exampleEntry.Value.Name,
                Input = exampleEntry.Value.Input,
                Transform = spardText
            };

            _examplesRepository.AddExample(id, example);
        }

        return Task.CompletedTask;
    }
}
