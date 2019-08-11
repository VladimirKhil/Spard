using Spard.Core;
using Spard.Sources;

namespace Spard.Expressions
{
    /// <summary>
    /// Line beginning
    /// </summary>
    public sealed class OpenLine: Primitive
    {
        public static OpenLine Instance = new OpenLine();

        private OpenLine()
        {

        }

        protected internal override string Sign
        {
            get { return "^"; }
        }

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            if (next)
                return false;

            var initStart = input.Position;

            if (!context.GetParameter(Parameters.Line))
                return initStart == 0;

            if (initStart == 0)
                return true;
            else
                input.Position--;

            var last = input.Read();
            var current = input.Read();
            input.Position = initStart;
            return object.Equals(last, '\r') && !object.Equals(current, '\n') || object.Equals(last, '\n');
        }

        internal override object Apply(IContext context)
        {
            return null;
        }
    }
}
