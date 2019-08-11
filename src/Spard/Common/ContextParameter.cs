using Spard.Core;

namespace Spard.Common
{
    internal sealed class ContextParameter
    {
        private readonly Parameters _parameter;
        private readonly bool _set;

        public ContextParameter(Parameters parameter, bool set)
        {
            _parameter = parameter;
            _set = set;
        }

        public void Free(IContext context)
        {
            context.SetParameter(_parameter, _set);
        }
    }
}
