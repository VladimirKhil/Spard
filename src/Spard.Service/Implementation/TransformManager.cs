using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Spard.Core;
using Spard.Exceptions;
using Spard.Service.Configuration;
using Spard.Service.Contract;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Spard.Service.Implementation
{
    /// <inheritdoc />
    internal sealed class TransformManager : ITransformManager
    {
        private readonly SpardConfiguration _options;

        public TransformManager(IOptions<SpardConfiguration> options)
        {
            _options = options.Value;
        }

        public Task<ProcessResult<string>> TransformAsync(TransformRequest transformRequest, CancellationToken cancellationToken = default)
        {
            var transformer = BuildTransformer(transformRequest.Transform);

            return RunBackgroundTaskAsync(token => Transform(transformer, transformRequest.Input, token), cancellationToken);
        }

        public async Task<TransformTableResult> TransformTableAsync(TransformRequest transformRequest, CancellationToken cancellationToken = default)
        {
            var parseStopwatch = Stopwatch.StartNew();

            var transformer = BuildTransformer(transformRequest.Transform);

            parseStopwatch.Stop();

            var classicResult = await RunBackgroundTaskAsync(token => Transform(transformer, transformRequest.Input, token), cancellationToken);

            var tableStopwatch = Stopwatch.StartNew();

            var tableTransformer = transformer.BuildTableTransformer(cancellationToken);

            tableStopwatch.Stop();

            var tableResult = await RunBackgroundTaskAsync(token => TransformTable(tableTransformer, transformRequest.Input, token), cancellationToken);

            return new TransformTableResult
            {
                Result = tableResult.Result,
                IsStandardResultTheSame = classicResult.Result == tableResult.Result,
                ParseDuration = parseStopwatch.Elapsed,
                StandardTransformDuration = classicResult.Duration,
                TableBuildDuration = tableStopwatch.Elapsed,
                TableTransformDuration = tableResult.Duration
            };
        }

        public Task<ProcessResult<string>> GenerateTableAsync(string transform, CancellationToken cancellationToken = default)
        {
            var transformer = BuildTransformer(transform);

            return RunBackgroundTaskAsync(token => VisualizeTable(transformer, token), cancellationToken);
        }

        public Task<ProcessResult<string>> GenerateSourceCodeAsync(string transform, CancellationToken cancellationToken = default)
        {
            var transformer = BuildTransformer(transform);

            return RunBackgroundTaskAsync(token => GenerateSourceCode(transformer, token), cancellationToken);
        }

        /// <summary>
        /// Creates SPARD standard transformer from SPARD rules.
        /// </summary>
        /// <param name="transform">SPARD transform rules.</param>
        /// <returns>Created transformer.</returns>
        private static TreeTransformer BuildTransformer(string transform)
        {
            try
            {
                var transformer = TreeTransformer.Create(transform);

                transformer.Mode = TransformMode.Function;
                transformer.SearchBestVariant = true;

                return transformer;
            }
            catch (ParseException exc)
            {
                throw new Exception($"({exc.LineNum},{exc.ColumnNum}): Parse error: {exc.Message}");
            }
            catch (Exception exc)
            {
                throw new Exception("Parse error", exc);
            }
        }

        private async Task<ProcessResult<T>> RunBackgroundTaskAsync<T>(Func<CancellationToken, T> func, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            using var source = new CancellationTokenSource(_options.TransformMaximumDuration);
            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(source.Token, cancellationToken);
            var task = Task.Run(() => func(linkedSource.Token), linkedSource.Token);

            await task;

            stopwatch.Stop();

            return new ProcessResult<T> { Result = task.Result, Duration = stopwatch.Elapsed };
        }

        private static string Transform(TreeTransformer transformer, string input, CancellationToken cancellationToken = default)
        {
            var result = new StringBuilder();

            foreach (var partialResult in transformer.StepTransform(input, cancellationToken))
            {
                result.Append(new string(partialResult.Cast<char>().ToArray()));
            }

            return result.ToString();
        }

        private static string TransformTable(TableTransformer tableTransformer, string input, CancellationToken cancellationToken = default)
        {
            var result = new StringBuilder();

            foreach (var partialResult in tableTransformer.Transform(input, cancellationToken))
            {
                result.Append(partialResult);
            }

            return result.ToString();
        }

        private static string VisualizeTable(TreeTransformer transformer, CancellationToken cancellationToken = default)
        {
            var tableTransformer = transformer.BuildTableTransformer(cancellationToken);

            var visualTable = tableTransformer.Visualize();

            var sb = new StringBuilder();
            for (var j = 0; j < visualTable.GetLength(0); j++)
            {
                for (var i = 0; i < visualTable.GetLength(1); i++)
                {
                    sb.AppendFormat("{0, 14}", visualTable[j, i]);
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        private string GenerateSourceCode(TreeTransformer transformer, CancellationToken cancellationToken = default)
        {
            var tableTransformer = transformer.BuildTableTransformer(cancellationToken);

            var result = new StringBuilder();

            using (var writer = new StringWriter(result))
            {
                tableTransformer.GenerateSourceCode(writer);
            }

            return result.ToString();
        }
    }
}
