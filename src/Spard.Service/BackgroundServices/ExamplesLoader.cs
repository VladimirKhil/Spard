using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spard.Service.Contract;
using Spard.Service.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Spard.Service.BackgroundServices
{
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
                _logger.LogWarning($"Examples file \"{indexFilePath}\" not found!");
                return Task.CompletedTask;
            }

            var examplesDict = JsonSerializer.Deserialize<Dictionary<string, ExampleModel>>(File.ReadAllText(indexFilePath));

            foreach (var exampleEntry in examplesDict)
            {
                if (!int.TryParse(exampleEntry.Key, out var id))
                {
                    _logger.LogWarning($"Example id \"{exampleEntry.Key}\" is not a number!");
                    continue;
                }

                var spardFileName = Path.Combine(examplesFolderPath, $"{exampleEntry.Key}.spard");

                if (!File.Exists(spardFileName))
                {
                    _logger.LogWarning($"Example SPARD file \"{spardFileName}\" not found!");
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
}
