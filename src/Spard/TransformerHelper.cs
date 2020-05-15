using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Spard.Sources;
using System.Collections;
using System.Threading;

namespace Spard
{
    /// <summary>
    /// Class extending the capabilities of transformers
    /// </summary>
    /// <remarks>This method of extending the functionality was chosen instead of inheritance in order to extend the functionality
    /// of all classes that implement the ITransformer interface</remarks>
    public static class TransformerHelper
    {
        /// <summary>
        /// Convert data stream to array
        /// </summary>
        /// <param name="input">Input stream</param>
        /// <returns>Output array</returns>
        public static object[] TransformToArray(this Transformer transformer, IEnumerable input)
        {
            return transformer.Transform(input).ToArray();
        }

        /// <summary>
        /// Convert data stream to single value
        /// </summary>
        /// <param name="input">Input stream</param>
        /// <returns>Single value</returns>
        public static object TransformToSingle(this Transformer transformer, IEnumerable input)
        {
            var enumerator = transformer.Transform(input).GetEnumerator();
            return enumerator.MoveNext() ? enumerator.Current : null;
        }

        /// <summary>
        /// Convert data stream
        /// </summary>
        /// <param name="input">Input stream</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Output stream</returns>
        public static IEnumerable<object> Transform(this Transformer transformer, TextReader input, CancellationToken cancellationToken = default)
        {
            return transformer.Transform(new TextReaderProxy(input), cancellationToken);
        }

        /// <summary>
        /// Convert data source to array
        /// </summary>
        /// <param name="input">Data source</param>
        /// <returns>Output array</returns>
        public static object[] TransformToArray<TOutput>(this Transformer transformer, TextReader input)
        {
            return new List<object>(Transform(transformer, input)).ToArray();
        }

        /// <summary>
        /// Convert data source to single value
        /// </summary>
        /// <param name="input">Data source</param>
        /// <returns>Single value</returns>
        public static object TransformToSingle(this Transformer transformer, TextReader input)
        {
            var enumerator = Transform(transformer, input).GetEnumerator();
            return enumerator.MoveNext() ? enumerator.Current : null;
        }

        /// <summary>
        /// Convert to text
        /// </summary>
        /// <param name="input">Input stream</param>
        /// <returns>Resulting text</returns>
        public static string TransformToText(this Transformer transformer, IEnumerable input)
        {
            var result = new StringBuilder();
            foreach (var item in transformer.Transform(input))
            {
                result.Append(item);
            }
            return result.ToString();
        }

        /// <summary>
        /// Convert to text
        /// </summary>
        /// <param name="input">Data source</param>
        /// <returns>Resulting text</returns>
        public static string TransformToText(this Transformer transformer, TextReader input)
        {
            var result = new StringBuilder();
            foreach (var item in Transform(transformer, input))
            {
                result.Append(item);
            }
            return result.ToString();
        }

        /// <summary>
        /// Convert source stream with SPARD rules
        /// </summary>
        /// <param name="input">Input stream</param>
        /// <param name="transform">SPARD transforming rules</param>
        /// <returns>Converted stream</returns>
        public static IEnumerable<object> Transform(this IEnumerable input, string transform)
        {
            var transformer = TreeTransformer.Create(transform);
            return transformer.Transform(input);
        }

        /// <summary>
        /// Convert text to text with SPARD rules
        /// </summary>
        /// <param name="input">Input text</param>
        /// <param name="transform">SPARD transforming rules</param>
        /// <returns>Resulting text</returns>
        public static string Transform(this string input, string transform)
        {
            var transformer = TreeTransformer.Create(transform);
            return transformer.TransformToText(input);
        }

        /// <summary>
        /// Transform data source step by step
        /// </summary>
        /// <param name="input">Data source</param>        /// 
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Output portion stream</returns>
        public static IEnumerable<IEnumerable<object>> StepTransform(this Transformer transformer, TextReader input, CancellationToken cancellationToken = default)
        {
            return transformer.StepTransform(new TextReaderProxy(input), cancellationToken);
        }

        /// <summary>
        /// External function
        /// </summary>
        /// <param name="args">Function args</param>
        /// <param name="next">Should next (otherwise) first result be produced</param>
        /// <param name="direct">Should the function be applied in direct (otherwise reversed) order</param>
        /// <returns>Result of function call</returns>
        public delegate IEnumerable UserFunc(IEnumerable[] args, bool next, bool direct);
    }
}
