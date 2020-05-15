using System;
using System.Collections.Generic;
using Spard.Common;
using System.Linq;
using System.Diagnostics;
using Spard.Data;
using System.Collections;

namespace Spard.Core
{
    /// <summary>
    /// Tranformation context. Allows to transfer variables values between different objects in transformation
    /// </summary>
    internal sealed class Context: IContext
    {
        /// <summary>
        /// Context variables
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Dictionary<string, object> _vars = new Dictionary<string, object>();

        /// <summary>
        /// Information about current transformation
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IRuntimeInfo _runtime = null;

        /// <summary>
        /// Context variables
        /// </summary>
        public Dictionary<string, object> Vars { get { return _vars; } }

        /// <summary>
        /// Binding formulas
        /// </summary>
        internal List<BindingFormula> Formulas { get; } = new List<BindingFormula>();

        /// <summary>
        /// Should we search for best match variant when no full match is found.
        /// Affects only on polyvariant expressions (Or, Sequence, etc.)
        /// </summary>
        public bool SearchBestVariant
        {
            get { return GetParameter(Parameters.SearchBestVariant); }
        }

        /// <summary>
        /// Information about current transformation
        /// </summary>
        public IRuntimeInfo Runtime { get { return _runtime; } }

        /// <summary>
        /// Main transformation tree. Provides methods to get functions and sets definitions
        /// </summary>
        public IExpressionRoot Root { get { return _runtime.Root; } }

        /// <summary>
        /// Transform parameters
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Parameters _parameters = Parameters.None;

        /// <summary>
        /// Inherited transform parameters when creating child context
        /// </summary>
        private const Parameters InheritedParameters = Parameters.IgnoreSP | Parameters.CaseInsensitive | Parameters.FullMatch | Parameters.LeftRecursion | Parameters.SearchBestVariant;

        /// <summary>
        /// Transform parameters
        /// </summary>
        public Parameters Parameters
        {
            get { return _parameters; }
        }

        private const string DefinitionsKey = "__definitions";
        internal const string MatchKey = "match";
        internal const string TranslateKey = "__translate";

        public DefinitionsTable DefinitionsTable
        {
            get
            {
                if (!Vars.TryGetValue(DefinitionsKey, out object val))
                {
                    var definitionsTable = new DefinitionsTable();
                    Vars[DefinitionsKey] = definitionsTable;

                    return definitionsTable;
                }

                return (DefinitionsTable)val;
            }
        }

        internal Context(IRuntimeInfo runtime)
        {
            _runtime = runtime;
        }

        internal Context(IContext context)
        {
            _runtime = context.Runtime;

            _parameters = context.Parameters & InheritedParameters;
            if (GetParameter(Parameters.FullMatch))
                _parameters |= Parameters.Match;
        }

        /// <summary>
        /// Clone this context
        /// </summary>
        /// <returns>Context clone</returns>
        public IContext Clone()
        {
            var context = new Context(_runtime)
            {
                _parameters = _parameters
            };

            foreach (var item in _vars)
            {
                context._vars[item.Key] = item.Value;
            }

            context.Formulas.AddRange(Formulas.Select(f => f.Clone()));
            
            return context;
        }

        /// <summary>
        /// Is parameter set
        /// </summary>
        /// <param name="parameter">Context parameter</param>
        /// <returns>Whether the specified context parameter is set</returns>
        public bool GetParameter(Parameters parameter)
        {
            return (_parameters & parameter) > 0;
        }

        /// <summary>
        /// Set parameter
        /// </summary>
        /// <param name="key">Parameter name</param>
        /// <param name="set">Is parameter set or unset</param>
        public void SetParameter(Parameters parameter, bool set)
        {
            if (set)
                _parameters |= parameter;
            else
                _parameters &= ~parameter;
        }

        /// <summary>
        /// Use parameter with special value
        /// </summary>
        /// <param name="key">Parameter name</param>
        /// <param name="set">Is parameter set or unset</param>
        /// <returns>Object which can be released. The release return parameter to its previous value</returns>
        public ContextParameter UseParameter(Parameters parameter, bool set = true)
        {
            var was = GetParameter(parameter);
            SetParameter(parameter, set);
            return new ContextParameter(parameter, was);
        }

        public bool Equals(IContext other)
        {
            if (_parameters != other.Parameters)
                return false;

            foreach (var item in _vars)
            {
                if (!char.IsUpper(item.Key[0]) && item.Key[0] != '$')
                    continue;

                if (!other.Vars.TryGetValue(item.Key, out object val) || !Equals(item.Value, val))
                    return false;
            }

            return true;
        }
        
        public bool IsIgnoredItem(object item)
        {
            if (!(item is char))
                return false;

            return Char.IsWhiteSpace((char)(object)item) && !object.Equals(item, '\r') && !object.Equals(item, '\n'); 
        }

        public object GetValue(string name)
        {
            if (_vars.TryGetValue(name, out object value))
                return value;

            return null;
        }

        public void SetValue(string name, object value)
        {
            if (_vars.ContainsKey(name))
                throw new Exception(string.Format("Variable {0} already exists!", name));

            _vars[name] = value;

            for (int i = 0; i < Formulas.Count; i++)
            {
                var formula = Formulas[i];

                formula.LeftVars.Remove(name);
                if (formula.LeftVars.Count == 0)
                {
                    Formulas.RemoveAt(i--);
                    BindingManager.UnifySimple(formula.LeftExpression, formula.RightExpression, this);
                    continue;
                }

                formula.RightVars.Remove(name);
                if (formula.RightVars.Count == 0)
                {
                    Formulas.RemoveAt(i--);
                    BindingManager.UnifySimple(formula.RightExpression, formula.LeftExpression, this);
                }
            }
        }

        public void AddMatch(object value, int index = -1)
        {
            var simpleMatch = Root.SimpleMatch;

            if (!simpleMatch)
            {
                if (value is NamedValue namedValue)
                {
                    if (namedValue.Value is IEnumerable enumerable)
                        namedValue.Value = new EnumerableValue { Value = enumerable };
                }
            }

            if (!_vars.TryGetValue(MatchKey, out object match))
            {
                _vars[MatchKey] = value;
                return;
            }

            var currentValue = match;
            if (currentValue is TupleValue tupleValue)
            {
                var processed = false;

                if (value is NamedValue newValue)
                {
                    for (int i = 0; i < tupleValue.Items.Length; i++)
                    {
                        if (tupleValue.Items[i] is NamedValue namedValue && namedValue.Name == newValue.Name)
                        {
                            var currentVals = tupleValue.Items;
                            var newVals = new object[currentVals.Length];
                            Array.Copy(currentVals, newVals, currentVals.Length);

                            object newVal;

                            if (namedValue.Value is object[] items)
                            {
                                var newItems = new object[items.Length + 1];
                                Array.Copy(items, newItems, items.Length);
                                newItems[items.Length] = newValue.Value;

                                newVal = newItems;
                            }
                            else
                            {
                                newVal = new object[] { namedValue.Value, newValue.Value };
                            }

                            newVals[i] = new NamedValue { Name = namedValue.Name, Value = newVal };
                            _vars[MatchKey] = new TupleValue { Items = newVals };

                            processed = true;
                            break;
                        }
                    }
                }

                if (!processed)
                {
                    var currentVals = tupleValue.Items;
                    var newVals = new object[currentVals.Length + 1];
                    Array.Copy(currentVals, newVals, currentVals.Length);
                    newVals[currentVals.Length] = value;

                    _vars[MatchKey] = new TupleValue { Items = newVals };
                }
            }
            else
            {
                if (currentValue is NamedValue namedValue && value is NamedValue newValue && namedValue.Name == newValue.Name)
                {
                    if (namedValue.Value is object[] currentVals)
                    {
                        var newVals = new object[currentVals.Length + 1];
                        Array.Copy(currentVals, newVals, currentVals.Length);
                        newVals[currentVals.Length] = newValue.Value;

                        _vars[MatchKey] = new NamedValue { Name = namedValue.Name, Value = newVals };
                    }
                    else
                    {
                        _vars[MatchKey] = new NamedValue { Name = namedValue.Name, Value = new object[] { namedValue.Value, newValue.Value } };
                    }
                }
                else
                    _vars[MatchKey] = new TupleValue { Items = new object[] { currentValue, value } };
            }
        }

        public void AddFormula(BindingFormula bindingFormula)
        {
            Formulas.Add(bindingFormula);
        }
    }
}
