using System;
using Spard.Sources;
using Spard.Common;
using Spard.Core;

namespace Spard.Expressions
{
    public sealed class Translation : Binary
    {
        protected internal override Priorities Priority
        {
            get
            {
                return Priorities.Translation;
            }
        }

        protected internal override string Sign
        {
            get
            {
                return "~";
            }
        }

        public override Expression CloneCore()
        {
            return new Translation();
        }

        internal override object Apply(IContext context)
        {
            throw new NotImplementedException();
        }

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            var matcher = context.GetParameter(Parameters.Left) ? _right : _left;
            var result = context.GetParameter(Parameters.Left) ? _left : _right;

            var param = context.UseParameter(Parameters.Match);

            var match = matcher.Match(input, ref context, next);
            param.Free(context);

            if (match)
            {
                var ctx = context.Clone();
                ctx.Vars[Context.TranslateKey] = new object();

                var matchValue = result.Apply(ctx);

                // Replacing Match
                context.Vars.Remove(Context.MatchKey);
                context.AddMatch(matchValue);
            }

            return match;
        }
    }
}
