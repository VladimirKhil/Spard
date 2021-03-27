using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Spard.Core;
using Spard.Exceptions;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Spard.Executor
{
    /// <summary>
    /// Represents SPARD interpreter as a console application.
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            var result = ProcessArgs(args);

            if (result == 1)
            {
                PrintHelp();

#if DEBUG
                Console.ReadLine();
#endif

                return 0;
            }

#if DEBUG
            Console.ReadLine();
#endif

            return result;
        }

        private static int ProcessArgs(string[] args)
        {
            if (args.Length == 0 || args.Length == 1 && args[0] == "/?")
            {
                return 1;
            }

            var mode = args[0];

            switch (mode)
            {
                case "run":
                    return Run(args);

                case "compile":
                    return 1;

                case "table":
                    return CreateTable(args);

                case "code":
                    return CreateCode(args, false);

                case "tablecompile":
                    return CreateCode(args, true);

                default:
                    break;
            }

            return 1;
        }

        private static bool ProcessRulesFile(ref string rulesFile)
        {
            if (!Path.IsPathRooted(rulesFile))
            {
                rulesFile = Path.Combine(AppContext.BaseDirectory, rulesFile);
            }

            if (!File.Exists(rulesFile))
            {
                Console.Error.WriteLine("ERROR: Rules file '{0}' not found!", rulesFile);
                return false;
            }

            return true;
        }

        private static TreeTransformer BuildTransformer(string rulesFile, TextReader rulesReader)
        {
            try
            {
                var transformer = TreeTransformer.Create(rulesReader);

                transformer.Mode = TransformMode.Function;
                transformer.SearchBestVariant = true;

                return transformer;
            }
            catch (ParseException exc)
            {
                Console.Error.Write("{0}({1},{2}): PARSE ERROR: {3}", rulesFile, exc.LineNum, exc.ColumnNum, exc.Message);
                return null;
            }
            catch (Exception exc)
            {
                Console.Error.Write("{0}: PARSE ERROR: {1}", rulesFile, exc);
                return null;
            }
        }

        /// <summary>
        /// Perform transformation by interpreter
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static int Run(string[] args)
        {
            if (args.Length == 1)
            {
                return 1;
            }

            var rulesFile = args[1];
            var readRules = rulesFile == "/rules" && args.Length > 1;

            var argsIndex = 2;
            int rulesLength = 0;

            if (readRules)
            {
                if (args.Length == 2)
                {
                    return 1;
                }

                argsIndex++;
                int.TryParse(args[2], out rulesLength);
            }
            else
            {
                if (!ProcessRulesFile(ref rulesFile))
                {
                    return -1;
                }
            }

            var verbose = false;
            var useTable = false;
            TextReader input = Console.In;
            string inputFileName = null;
            int maxMilliseconds = 0;

            for (; argsIndex < args.Length; argsIndex++)
            {
                var arg = args[argsIndex];
                switch (arg)
                {
                    case "/v":
                    case "/verbose":
                        verbose = true;
                        break;

                    case "/t":
                    case "/time":
                        if (argsIndex + 1 < args.Length)
                        {
                            var msec = args[++argsIndex];
                            int.TryParse(msec, out maxMilliseconds);
                        }
                        break;

                    case "/table":
                        useTable = true;
                        break;

                    default:
                        if (inputFileName == null)
                        {
                            if (!Path.IsPathRooted(arg))
                            {
                                arg = Path.Combine(AppContext.BaseDirectory, arg);
                            }

                            if (!File.Exists(arg))
                            {
                                Console.Error.WriteLine("ERROR: Input data file '{0}' not found!", arg);
                                return -1;
                            }

                            input = new StreamReader(File.OpenRead(arg));
                            inputFileName = arg;
                        }

                        break;
                }
            }

            TreeTransformer transformer;
            if (readRules)
            {
                transformer = BuildTransformer("", new SubReader(input, rulesLength));
            }
            else
            {
                using var reader = new StreamReader(File.OpenRead(rulesFile));
                transformer = BuildTransformer(rulesFile, reader);
            }

            if (transformer == null)
            {
                return -2;
            }

            try
            {
                if (maxMilliseconds > 0)
                {
                    using var source = new CancellationTokenSource(maxMilliseconds);
                    var task = Task.Run(() => Transform(transformer, verbose, useTable, input, inputFileName, source.Token), source.Token);

                    try
                    {
                        task.Wait();
                    }
                    catch (AggregateException exc)
                    {
                        throw exc.InnerException;
                    }

                    return task.Result;
                }

                return Transform(transformer, verbose, useTable, input, inputFileName);
            }
            catch (TransformException exc)
            {
                if (exc.BestTry != null)
                {
                    var position = exc.ErrorPosition;
                    Console.Error.WriteLine("{0}({1},{2}): PROCESSING ERROR", inputFileName, position.Item1, position.Item2);

                    if (verbose)
                    {
                        Console.Error.WriteLine("BEST STACK TRACE:\r\n{0}", exc.PrintBestTryStackTrace());
                    }
                }
                else
                {
                    Console.Error.WriteLine("{0}: UNKNOWN PROCESSING ERROR", inputFileName);
                }

                return 2;
            }
            catch (SpardException exc)
            {
                Console.Error.WriteLine("PROCESSING ERROR: " + exc.Message);
                return 3;
            }
            catch (OperationCanceledException)
            {
                Console.Error.WriteLine("PROCESSING ERROR: valid processing time has expired!");
                return 3;
            }
            catch (Exception exc)
            {
                Console.Error.WriteLine("CRITICAL PROCESSING ERROR: " + exc.Message);
                return 3;
            }
            finally
            {
                if (inputFileName != null)
                {
                    input.Dispose();
                }
            }
        }

        private static int CreateTable(string[] args)
        {
            if (args.Length == 1)
            {
                return 1;
            }

            var rulesFile = args[1];
            var readRules = rulesFile == "/rules";
            if (!readRules && !ProcessRulesFile(ref rulesFile))
            {
                return -1;
            }

            var maxMilliseconds = 0;

            for (int argsIndex = 2; argsIndex < args.Length; argsIndex++)
            {
                var arg = args[argsIndex];
                switch (arg)
                {
                    case "/t":
                    case "/time":
                        if (argsIndex + 1 < args.Length)
                        {
                            var msec = args[++argsIndex];
                            int.TryParse(msec, out maxMilliseconds);
                        }

                        break;
                }
            }

            TreeTransformer transformer;
            if (readRules)
            {
                transformer = BuildTransformer("", Console.In);
            }
            else
            {
                using var reader = new StreamReader(File.OpenRead(rulesFile));
                transformer = BuildTransformer(rulesFile, reader);
            }

            if (transformer == null)
            {
                return -2;
            }

            if (maxMilliseconds > 0)
            {
                var source = new CancellationTokenSource();
                var task = Task.Run(() => VisualizeTable(transformer), source.Token);
                source.CancelAfter(maxMilliseconds);

                try
                {
                    task.Wait();
                }
                catch (AggregateException exc)
                {
                    throw exc.InnerException;
                }

                return task.Result;
            }
            
            return VisualizeTable(transformer);
        }

        private static int VisualizeTable(TreeTransformer transformer)
        {
            TableTransformer tableTransformer;
            try
            {
                tableTransformer = transformer.BuildTableTransformer();
            }
            catch (OperationCanceledException)
            {
                Console.Error.WriteLine("TABLE-TRANSFORMER CONSTRUCTION ERROR: valid processing time has expired!");
                return 3;
            }
            catch (Exception exc)
            {
                Console.Error.WriteLine("TABLE-TRANSFORMER CONSTRUCTION ERROR: " + exc.Message);
                return 3;
            }

            var visualTable = tableTransformer.Visualize();

            var sb = new StringBuilder();
            for (int j = 0; j < visualTable.GetLength(0); j++)
            {
                for (int i = 0; i < visualTable.GetLength(1); i++)
                {
                    sb.AppendFormat("{0, 14}", visualTable[j, i]);
                }

                sb.AppendLine();
            }

            Console.Out.Write(sb.ToString());

            return 0;
        }

        private static int CreateCode(string[] args, bool compile)
        {
            if (args.Length == 1)
                return 1;

            var rulesFile = args[1];
            var readRules = rulesFile == "/rules";

            if (!readRules && !ProcessRulesFile(ref rulesFile))
                return -1;

            int maxMilliseconds = 0;

            for (int argsIndex = 2; argsIndex < args.Length; argsIndex++)
            {
                var arg = args[argsIndex];
                switch (arg)
                {
                    case "/t":
                    case "/time":
                        if (argsIndex + 1 < args.Length)
                        {
                            var msec = args[++argsIndex];
                            int.TryParse(msec, out maxMilliseconds);
                        }

                        break;
                }
            }

            TreeTransformer transformer;
            if (readRules)
            {
                transformer = BuildTransformer("", Console.In);
            }
            else
            {
                using var reader = new StreamReader(File.OpenRead(rulesFile));
                transformer = BuildTransformer(rulesFile, reader);
            }

            if (transformer == null)
                return -2;

            var sb = new StringBuilder();
            var output = compile ? new StringWriter(sb) : Console.Out;
            int result;

            try
            {
                if (maxMilliseconds > 0)
                {
                    var source = new CancellationTokenSource();
                    var task = Task.Run(() => GenerateSource(transformer, output, source.Token), source.Token);
                    source.CancelAfter(maxMilliseconds);

                    try
                    {
                        task.Wait();
                    }
                    catch (AggregateException exc)
                    {
                        throw exc.InnerException;
                    }

                    result = task.Result;
                }
                else
                    result = GenerateSource(transformer, output);
            }
            finally
            {
                if (compile)
                {
                    output.Dispose();
                }
            }
            
            if (!compile || result != 0)
                return result;

            var syntaxTree = CSharpSyntaxTree.ParseText(sb.ToString());

            var compilation = CSharpCompilation.Create("Spard.Transform", new[] { syntaxTree }, null, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var compilationResult = compilation.Emit("out.dll");

            if (compilationResult.Success)
                return 0;

            var failures = compilationResult.Diagnostics.Where(diagnostic =>
                diagnostic.IsWarningAsError ||
                diagnostic.Severity == DiagnosticSeverity.Error);

            foreach (Diagnostic diagnostic in failures)
            {
                Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
            }

            return -3;
        }

        private static int GenerateSource(TreeTransformer transformer, TextWriter writer, CancellationToken cancellationToken = default)
        {
            TableTransformer tableTransformer;
            try
            {
                tableTransformer = transformer.BuildTableTransformer(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Console.Error.WriteLine("TABLE-TRANSFORMER CONSTRUCTION ERROR: valid processing time has expired!");
                return 3;
            }
            catch (Exception exc)
            {
                Console.Error.WriteLine("TABLE-TRANSFORMER CONSTRUCTION ERROR: " + exc.Message);
                return 3;
            }

            tableTransformer.GenerateSourceCode(writer);
            return 0;
        }

        private static void PrintHelp()
        {
            Console.WriteLine("SPARD interpreter");
            Console.WriteLine("Version {0}", Assembly.GetEntryAssembly().GetName().Version);

            Console.WriteLine();
            Console.WriteLine("Usage: Spard.exe run [/rules <length>|<rules.file>] [<input.file>] [/verbose|/v] [/time|/t <time ms>]");

            Console.WriteLine();
            Console.WriteLine("/rules: transformation rules should be read from <input.file> (if provided) or from input stream. In this case, the first <length> characters are considered to be rules entry, all others are input data");
            Console.WriteLine("<rules.file>: name of the file with SPARD transformation rules");
            Console.WriteLine("<input.file>: name of the file containing input data");
            Console.WriteLine("/verbose (/v): display of extended diagnostic messages flag");
            Console.WriteLine("/time (/t): limit execution time in milliseconds");

            Console.WriteLine();
            Console.WriteLine("Input data is read from the input stream or from <input.file>, if this parameter is set");
            Console.WriteLine("Output data is sent to the output stream");
            Console.WriteLine("Error messages about parsing the rules file or direct data processing are displayed in the error stream");

            Console.WriteLine();
            Console.WriteLine("Returned values are:");

            Console.WriteLine();
            Console.WriteLine("0: processing completed successfully");
            Console.WriteLine("1: rules file parse error");
            Console.WriteLine("2: input text processing error");
            Console.WriteLine("3: internal transformer error");
            Console.WriteLine("-1: other errors");

            Console.WriteLine();
            Console.WriteLine("Usage: Spard.exe /?");

            Console.WriteLine();
            Console.WriteLine("Shows help about this app");
        }

        private static int Transform(TreeTransformer transformer, bool verbose, bool useTable, TextReader input, string inputFileName, CancellationToken cancellationToken = default)
        {
#if DEBUG
            var stopwatch = Stopwatch.StartNew();
#endif
            if (useTable)
            {
                var tableTransformer = transformer.BuildTableTransformer(cancellationToken);
                foreach (var partialResult in tableTransformer.Transform(input, cancellationToken))
                {
                    Console.Out.Write(partialResult);
                }
            }
            else
            {
                foreach (var partialResult in transformer.StepTransform(input, cancellationToken))
                {
                    Console.Out.Write(new string(partialResult.Cast<char>().ToArray()));
                }
            }

#if DEBUG
            Debug.WriteLine("Elapsed time: " + stopwatch.Elapsed);
#endif

            return 0;
        }
    }
}
