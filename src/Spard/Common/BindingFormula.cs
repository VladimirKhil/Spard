using Spard.Expressions;
using System.Collections.Generic;

namespace Spard.Common
{
    internal sealed class BindingFormula
    {
        public HashSet<string> LeftVars { get; internal set; }
        public HashSet<string> RightVars { get; internal set; }

        public Expression LeftExpression { get; set; }
        public Expression RightExpression { get; set; }

        public BindingFormula(HashSet<string> leftVars, HashSet<string> rightVars)
        {
            LeftVars = leftVars;
            RightVars = rightVars;
        }

        public BindingFormula(Query leftVar, Query rightVar)
        {
            LeftVars = new HashSet<string>(new string[] { leftVar.Name });
            RightVars = new HashSet<string>(new string[] { rightVar.Name });

            LeftExpression = leftVar;
            RightExpression = rightVar;
        }

        public BindingFormula Clone()
        {
            return new BindingFormula(new HashSet<string>(LeftVars), new HashSet<string>(RightVars))
            {
                LeftExpression = LeftExpression,
                RightExpression = RightExpression
            };
        }
    }
}
