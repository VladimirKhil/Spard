using Spard.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spard.Transitions
{
    internal static class ExpressionExtensions
    {
        /// <summary>
        /// Rename all uses of a variable in the expression tree
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="oldName"></param>
        /// <param name="newName"></param>
        /// <returns></returns>
        internal static Expression RenameVar(this Expression expression, string oldName, Query newName)
        {
            if (expression is Query variable && variable.Name == oldName)
                return newName;

            var clone = expression.CloneCore();
            var newOperands = new List<Expression>();
            foreach (var item in expression.Operands())
            {
                newOperands.Add(RenameVar(item, oldName, newName));
            }

            clone.SetOperands(newOperands);
            return clone;
        }

        /// <summary>
        /// Removes ZeroMoveProxy from expression tree
        /// </summary>
        /// <param name="expression">Expression tree</param>
        /// <returns>Cleaned expression</returns>
        internal static Expression Free(this Expression expression)
        {
            if (expression is ZeroMoveProxy zeroMoveProxy)
                return Free(zeroMoveProxy.Operand);

            expression.SetOperands(expression.Operands().Select(op => Free(op)));
            return expression;
        }

        internal static void Traverse(this Expression expression, List<string> usedVariables)
        {
            if (expression is Query query)
            {
                var name = query.Name;
                if (!usedVariables.Contains(name))
                    usedVariables.Add(name);

                return;
            }

            foreach (var child in expression.Operands())
            {
                if (child != null)
                    Traverse(child, usedVariables);
            }
        }
    }
}
