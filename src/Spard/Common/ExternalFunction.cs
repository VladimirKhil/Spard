using Spard.Core;
using System.Collections;
using System.Threading;

namespace Spard.Common
{
    /// <summary>
    /// External function
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    /// <typeparam name="TOutput"></typeparam>
    internal sealed class ExternalFunction: ITransformFunction
    {
        private readonly TransformerHelper.UserFunc _func;
        private bool _direct = true;

        public ExternalFunction(TransformerHelper.UserFunc func)
        {
            _func = func;
        }

        public ITransformFunction Reverse()
        {
            return new ExternalFunction(_func) { _direct = false };
        }

        public IEnumerable TransformCoreAll(object[] args, CancellationToken cancellationToken)
        {
            return null;
            //var results = new List<IEnumerable>();
            //var next = false;
            //IEnumerable res;

            ////do
            ////{
            ////    res = this.func(args.ToArray<Results.IResult<TInput>>(), next, this.direct).Cast<TOutput>();
            ////    results.Add(res);
            ////} while (res.Any());

            //return Results.ObjectResult<object>.Create(results.SelectMany<object>(r => r));
        }
    }
}
