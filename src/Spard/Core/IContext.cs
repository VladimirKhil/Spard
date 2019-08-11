using System;
using System.Collections.Generic;
using Spard.Common;

namespace Spard.Core
{
    /// <summary>
    /// Interface of transformation context
    /// </summary>
    internal interface IContext: IEquatable<IContext>
    {
        /// <summary>
        /// Context variables
        /// </summary>
        Dictionary<string, object> Vars { get; }

        /// <summary>
        /// Should we search for best match variant when no full match is found.
        /// Affects only on polyvariant expressions (Or, Sequence, etc.)
        /// </summary>
        bool SearchBestVariant { get; }

        /// <summary>
        /// Main transformation tree. Provides methods to get functions and sets definitions
        /// </summary>
        IExpressionRoot Root { get; }

        /// <summary>
        /// Transform parameters
        /// </summary>
        Parameters Parameters { get; }

        /// <summary>
        /// Information about current transformation
        /// </summary>
        IRuntimeInfo Runtime { get; }

        /// <summary>
        /// Is parameter set
        /// </summary>
        /// <param name="parameter">Context parameter</param>
        /// <returns>Whether the specified context parameter is set</returns>
        bool GetParameter(Parameters parameter);

        /// <summary>
        /// Set parameter
        /// </summary>
        /// <param name="key">Parameter name</param>
        /// <param name="value">Is parameter set or unset</param>
        void SetParameter(Parameters parameter, bool set);

        /// <summary>
        /// Use parameter with special value
        /// </summary>
        /// <param name="key">Parameter name</param>
        /// <param name="set">Is parameter set or unset</param>
        /// <returns>Object which can be released. The release return parameter to its previous value</returns>
        ContextParameter UseParameter(Parameters parameter, bool set = true);

        /// <summary>
        /// Clone this context
        /// </summary>
        /// <returns>Context clone</returns>
        IContext Clone();

        DefinitionsTable DefinitionsTable { get; }

        void AddMatch(object value, int index = -1);

        object GetValue(string name);
        void SetValue(string name, object value);

        bool IsIgnoredItem(object item);
        void AddFormula(BindingFormula bindingFormula);
    }
}
