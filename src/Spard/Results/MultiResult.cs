using Spard.Core;
using System.Collections;
using System.Collections.Generic;

namespace Spard.Results
{
    /// <summary>
    /// Result which allow polysemy
    /// Provides transparent and consistent getting of results of all calls
    /// </summary>
    internal sealed class MultiResult : IMultiResult
    {
        private readonly ITransformer _transformer = null;

        public MultiResult(ITransformer transformer)
        {
            this._transformer = transformer;
        }

        public IEnumerator GetEnumerator()
        {
            return null;// _transformer.Transform().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Get another transformation result in the same step
        /// </summary>
        /// <returns>Another transformation result</returns>
        public IEnumerable GetAnotherVariant()
        {
            return null;// _transformer.TransformAnother();
        }

        /// <summary>
        /// Get all transformation results
        /// </summary>
        /// <returns>Get all transformation results on current transformation step</returns>
        public IEnumerable<IEnumerable> GetAllVariants()
        {
            return null;
            //yield return _transformer.Transform();

            //do
            //{
            //    var res = _transformer.TransformAnother().ToArray();
            //    if (res.Length == 0)
            //        yield break;

            //    yield return res;
            //} while (true);
        }
    }
}
