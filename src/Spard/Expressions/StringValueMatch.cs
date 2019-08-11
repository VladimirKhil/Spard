using System;
using System.Collections.Generic;
using System.Text;
using Spard.Common;
using Spard.Sources;
using Spard.Transitions;
using Spard.Core;

namespace Spard.Expressions
{
    /// <summary>
    /// String value
    /// </summary>
    public class StringValueMatch : Primitive, IInstructionExpression
    {
        protected internal override Priorities Priority
        {
            get { return Value.Length < 2 ? Priorities.Primitive : Priorities.StringValue; }
        }

        protected internal override string Sign
        {
            get { return ""; }
        }

        /// <summary>
        /// Valus of the string
        /// </summary>
        public string Value { get; set; } // TODO: delete 'set'

        public StringValueMatch()
        {
            Value = "";
        }

        /// <summary>
        /// Creates a string
        /// </summary>
        /// <param name="value">String value</param>
        public StringValueMatch(object value)
        {
            this.Value = value.ToString();
        }

        /// <summary>
        /// Creates a string
        /// </summary>
        /// <param name="value">String value</param>
        public StringValueMatch(string value)
        {
            this.Value = value;
        }

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            if (next)
                return false;

            var initStart = input.Position;
            var ignoreSP = context.GetParameter(Parameters.IgnoreSP);
            var caseInsensitive = context.GetParameter(Parameters.CaseInsensitive);
            object c = null;

            for (int j = 0; j < Value.Length; j++)
            {
                if (input.EndOfSource || !object.Equals((c = input.Read()), Value[j]))
                {
                    if (caseInsensitive && c is char && char.ToLower((char)c) == char.ToLower(Value[j]))
                        continue;

                    if (!input.EndOfSource && ignoreSP && context.IsIgnoredItem(c))
                    {
                        j--;
                        continue;
                    }

                    if (context.SearchBestVariant)
                    {
                        if (context.Vars.TryGetValue(Context.MatchKey, out object m))
                        {
                            context.Runtime.SaveBestTry(new MatchInfo(input.Position - (input.EndOfSource ? 0 : 1), context.Runtime.StackTrace.ToArray(), match: m));
                        }
                    }

                    input.Position = initStart;
                    return false;
                }
            }
            return true;
        }

        internal override object Apply(IContext context)
        {
            return Value;
        }

        internal override TransitionTable BuildTransitionTableCore(TransitionSettings settings, bool isLast)
        {
            var length = Value.Length;
            if (length == 0)
                return null;

            var table = new TransitionTable();
            if (length == 1)
                table[new InputSet(InputSetType.Include, Value[0])] = TransitionTableResultCollection.Empty.CloneCollection();
            else
                table[new InputSet(InputSetType.Include, Value[0])] = TransitionTableResultCollection.Create(new StringValueMatch(Value.Substring(1)));

            return table;
        }

        public override string ToString()
        {
            var result = new StringBuilder();
            bool escape = false, escapeNext = false, quotes = false;
            for (int i = 0; i < Value.Length; i++)
            {
                var c = Value[i];
                escape = i == 0 ? !Char.IsLetterOrDigit(c) : escapeNext;
                escapeNext = i == Value.Length - 1 ? false : !Char.IsLetterOrDigit(Value[i + 1]);
                if (escape)
                {
                    if (c == '"' || c == '\'')
                    {
                        result.Append('\'');
                    }
                    else if (!quotes)
                    {
                        if (escapeNext)
                        {
                            result.Append("\"");
                            quotes = true;
                        }
                        else
                            result.Append('\'');
                    }
                }
                else
                {
                    if (quotes)
                    {
                        result.Append("\"");
                        quotes = false;
                    }
                }
                result.Append(c);
            }
            if (quotes)
                result.Append("\"");
            return result.ToString();
        }

        bool IInstructionExpression.RightArgumentNeeded
        {
            get { return Value != "matches"; }
        }

        public override bool Equals(Expression other)
        {
            if (this == other)
                return true;

            return other is StringValueMatch stringValue && Value == stringValue.Value;
        }

        internal override bool EqualsSmart(Expression other, Dictionary<string, string> varsMap)
        {
            return Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
