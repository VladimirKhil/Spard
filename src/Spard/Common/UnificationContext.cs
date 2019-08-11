using Spard.Expressions;
using System.Collections.Generic;

namespace Spard.Common
{
    public sealed class UnificationContext
    {
        public Dictionary<Expression, Expression> BindingTable { get; } = new Dictionary<Expression, Expression>();
    }
}
