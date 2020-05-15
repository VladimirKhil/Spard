using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Spard.Compilation;
using Spard.Compilation.CSharp;
using Spard.Core;
using Spard.Transitions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Spard
{
    /// <summary>
    /// Table transformer. It is an optimized version of transformer based on transformation trees.
    /// Unlike the latter, it performs the transformation without recursive calls and back moves (in one pass of the input text).
    /// The transformer works in a mode similar to the Turing machine: the result of the conversion is determined by the current state of the transformer
    /// and the current input value. The correspondences between these parameters are stored in the table - hence the name of the transformer.
    /// Worker classes are located in Transitions/States
    /// Transitions ar described by graph in TransitionStateBase (using various subclasses)
    /// Initial state is _initialState
    /// </summary>
    public sealed class TableTransformer : Transformer, ITransformFunction
    {
        /// <summary>
        /// Transformer initial state
        /// </summary>
        private readonly TransitionStateBase _initialState;

        /// <summary>
        /// Has error happened
        /// </summary>
        public bool Error { get; set; }

        /// <summary>
        /// Creates table transformer
        /// </summary>
        /// <param name="state">Transition table, on the basis of which the tranformer table is built</param>
        internal TableTransformer(TransitionStateBase state)
        {
            _initialState = state;
        }

        public override IEnumerable<object> Transform(IEnumerable input, CancellationToken cancellationToken = default)
        {
            var state = _initialState;
            var context = new TransitionContext();

            var sourceEnumerator = input.GetEnumerator();
            IEnumerable result;

            var beforeStart = true;

            bool isNotFinal;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                isNotFinal = sourceEnumerator.MoveNext();
                if (isNotFinal)
                {
                    state = state.Move(sourceEnumerator.Current, ref context, out result);
                    beforeStart = false;
                }
                else
                {
                    if (beforeStart) // All is OK
                        yield break;

                    state = state.Move(InputSet.EndOfSource, ref context, out result); // Need to push the tail of the result
                }

                if (state == null) // Tranformation error
                {
                    // Produce what has accumulated
                    // NB: only the highest chain of results is always saved
                    // She presses under all the rest
                    foreach (var res in context.Results)
                    {
                        foreach (var item in res.Data)
                        {
                            yield return item;
                        }
                    }

                    // Is always works in function mode, additional modes were taken into account when creating the transformer

                    Error = true;
                    yield break;
                }

                if (result != null)
                {
                    foreach (var res in result)
                    {
                        yield return res;
                    }
                }

                if (state.IsFinal)
                {
                    foreach (var res in state.GetResult(context))
                    {
                        yield return res;
                    }

                    state = _initialState;
                    context = new TransitionContext();
                    beforeStart = true;
                }

            } while (isNotFinal);
        }

        public override Transformer ChainWith(Transformer transformer)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IEnumerable<object>> StepTransform(IEnumerable input, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Visualize table transformer. Builds a table of states and transitions between them
        /// </summary>
        /// <returns>Transformer visualization</returns>
        public string[,] Visualize()
        {
            var visualTable = _initialState.BuildVisualTable();
            return visualTable.ToSimpleTable();
        }

        public CompiledTransformer Compile()
        {
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                var wr = new IndentWriter(writer);
                CSCodeBuilder.CreateTransformerCode(_initialState, wr);
                wr.WriteLine("return new CompiledTransformerImpl();");
            }

            var source = sb.ToString();

            return CSharpScript.EvaluateAsync<CompiledTransformer>(source, ScriptOptions.Default
                .WithReferences(typeof(Enumerable).GetTypeInfo().Assembly,
                    typeof(TableTransformer).GetTypeInfo().Assembly,
                    Assembly.Load(new AssemblyName("System.Threading.Tasks, Version=4.0.10.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")))
                .WithImports("System", "System.Linq", "System.Text", "System.Collections", "System.Collections.Generic", "System.Threading", "Spard", "Spard.Data")).Result;
        }

        /// <summary>
        /// Generate source code of the transformer
        /// </summary>
        /// <returns>C# source code that performs the transformation</returns>
        public void GenerateSourceCode(TextWriter writer)
        {
            var wr = new IndentWriter(writer);
            CSCodeBuilder.WriteUsings(wr);
            CSCodeBuilder.CreateTransformerCode(_initialState, wr);
        }

        public void Save(Stream stream)
        {
            _initialState.Save(stream);
        }

        public static TableTransformer Load(System.IO.Stream stream)
        {
            return new TableTransformer(Transitions.TransitionStateBase.Load(stream));
        }

        IEnumerable ITransformFunction.TransformCoreAll(object[] args, CancellationToken cancellationToken)
        {
            IEnumerable result;

            if (args.Length == 0)
            {
                result = TransformEmpty();
            }
            else if (args.Length == 1)
            {
                if (args[0] is IEnumerable enumerable)
                {
                    var casted = enumerable;
                    if (casted.Cast<object>().Any())
                        result = Transform(casted, cancellationToken);
                    else
                        result = TransformEmpty();
                }
                else
                    result = Transform(new object[] { args[0] }, cancellationToken);
            }
            else
            {
                throw new NotImplementedException();
                //var source = new TupleSource
                //{
                //    Sources = args.Select(item => ValueConverter.ConvertToSource(ValueConverter.ConvertToEnumerable(item))).ToArray()
                //};

                //var runtime = new RuntimeInfo(this.root, cancellationToken);
                //runtime.SearchBestVariant = this.SearchBestVariant;

                //result = Transform(source);
            }

            return result;
        }

        private IEnumerable TransformEmpty()
        {
            throw new NotImplementedException();
        }
    }
}
