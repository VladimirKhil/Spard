using Spard.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Spard
{
    public abstract class CompiledTransformer: Transformer, ITransformFunction
    {
        public IEnumerable TransformCoreAll(object[] args, CancellationToken cancellationToken)
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

        public override IEnumerable<IEnumerable<object>> StepTransform(IEnumerable input, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Transformer ChainWith(Transformer transformer)
        {
            throw new NotImplementedException();
        }
    }
}
