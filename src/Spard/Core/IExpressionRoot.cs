using Spard.Expressions;
using System;

namespace Spard.Core
{
    /// <summary>
    /// Main transformation tree
    /// </summary>
    internal interface IExpressionRoot
    {
        /// <summary>
        /// The depth of current function call
        /// </summary>
        int FunctionCallDepth { get; set; }

        /// <summary>
        /// Should we collect Match as simple sequence and not an object
        /// </summary>
        bool SimpleMatch { get; set; }

        /// <summary>
        /// Get set definition
        /// </summary>
        /// <param name="setName">Set name</param>
        /// <param name="numOfParams">Number of set arguments</param>
        /// <returns>Collection of set definitions with given name and number of arguments</returns>
        Definition[] GetSet(string module, string setName, int numOfParams);

        /// <summary>
        /// Get function by name
        /// </summary>
        /// <param name="module">Name of module containing the function</param>
        /// <param name="functionName">Function name</param>
        /// <param name="direction">Function direction</param>
        /// <returns>Transformation which the function describes</returns>
        ITransformFunction GetFunction(string module, string functionName, Directions direction);
        
        /// <summary>
        /// Get variable type
        /// </summary>
        /// <param name="name">Variable name</param>
        /// <returns>Type of variable with given name</returns>
        Expression GetVariableType(string name);
    }
}
