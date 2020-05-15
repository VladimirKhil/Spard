using System;
using System.Collections.Generic;
using System.Collections;
using System.Threading;
using Spard.Core;

namespace Spard
{
    /// <summary>
    /// A chain of transformers acting as a new transformer
    /// </summary>
    /// <typeparam name="TInput">Type of input data of the transformer chain</typeparam>
    /// <typeparam name="TOutput">Type of output data of the transformer chain</typeparam>
    internal sealed class TransformersChain: Transformer
    {
        private readonly ITransformer _input = null;
        private readonly ITransformer _output = null;

        public override event Action<int> ProgressChanged
        {
            add { _input.ProgressChanged += value; }
            remove { _input.ProgressChanged -= value; }
        }

        public TransformersChain(ITransformer input, ITransformer output)
        {
            _input = input;
            _output = output;
        }

        public override IEnumerable<object> Transform(IEnumerable input, CancellationToken cancellationToken = default)
        {
            return _output.Transform(_input.Transform(input, cancellationToken), cancellationToken);
        }

        public override IEnumerable<IEnumerable<object>> StepTransform(IEnumerable input, CancellationToken cancellationToken = default)
        {
            return _output.StepTransform(_input.Transform(input, cancellationToken), cancellationToken);
        }

        public override Transformer ChainWith(Transformer transformer)
        {
            return new TransformersChain(transformer, this);
        }
    }
}
