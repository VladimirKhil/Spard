using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spard.Transitions
{
    /// <summary>
    /// Single input item test. Defines a set of acceptable items
    /// </summary>
    public sealed class InputSet: IEquatable<InputSet>
    {
        /// <summary>
        /// Test with zero offset
        /// </summary>
        public static InputSet Zero = new InputSet(InputSetType.Zero);

        /// <summary>
        /// Test type
        /// </summary>
        public InputSetType Type { get; set; }
        /// <summary>
        /// Affected values
        /// </summary>
        public IValues Values { get; set; }

        /// <summary>
        /// Is allowed items set an empty set
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return Type == InputSetType.Include && !Values.Any();
            }
        }

        public bool IsFinishing
        {
            get
            {
                return Type == InputSetType.Include && Values.Count() == 1 && Values.First() == InputSet.EndOfSource;
            }
        }

        public InputSet(InputSetType type, params object[] values)
        {
            Type = type;
            Values = new ValuesList(values);
        }

        public InputSet(InputSetType type, IValues values)
        {
            Type = type;
            Values = values;
        }

        /// <summary>
        /// End of source marker
        /// </summary>
        internal static object EndOfSource = new object();

        internal static InputSet IncludeEOS = new InputSet(InputSetType.Include, new object[] { EndOfSource });
        internal static InputSet ExcludeEOS = new InputSet(InputSetType.Exclude, new object[] { EndOfSource });

        public override int GetHashCode()
        {
            return Values.Count().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is InputSet other)
                return Equals(other);

            return base.Equals(obj);
        }

        public bool Equals(InputSet other)
        {
            var length = Values.Count();
            if (Type != other.Type || length != other.Values.Count())
                return false;

            foreach (var item in Values)
            {
                if (!other.Values.Contains(item))
                    return false;
            }

            return true;
        }

        public override string ToString()
        {
            if (Type == InputSetType.Zero)
                return "0";

            if (Values.Count() == 1)
                return (Type == InputSetType.Include ? "+" : "-") + Escape(Values.First());

            return string.Format("{0}({1})",
                Type == InputSetType.Include ? "+" : "-",
                string.Join("", Values.Select(Escape))
                );
        }

        public static string Escape(object value)
        {
            if (value == EndOfSource)
                return "\\0";

            if (object.Equals(value, '\r'))
                return "\\r";

            if (object.Equals(value, '\n'))
                return "\\n";

            if (object.Equals(value, '\\'))
                return "\\\\";

            return value.ToString();
        }

        internal bool Contains(object item)
        {
            return Values.Contains(item) ^ Type == InputSetType.Exclude;
        }

        /// <summary>
        /// Split tow sets into common part and two unique parts
        /// </summary>
        /// <param name="other"></param>
        /// <returns>Returns A^B, A-B and B-A</returns>
        internal Tuple<InputSet, InputSet, InputSet> IntersectAndTwoExcepts(InputSet other)
        {
            return Tuple.Create(
                Intersect(other),
                Except(other),
                other.Except(this));
        }

        /// <summary>
        /// Two sets intersection
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        internal InputSet Intersect(InputSet other)
        {
            switch (Type)
            {
                case InputSetType.Zero:
                    if (other.Type == InputSetType.Zero)
                        return this;
                    else
                        return new InputSet(InputSetType.Include);

                case InputSetType.Include:
                    switch (other.Type)
                    {
                        case InputSetType.Zero:
                            return new InputSet(InputSetType.Include);

                        case InputSetType.Include:
                            return new InputSet(InputSetType.Include, Values.Intersect(other.Values).ToArray());

                        default:
                        case InputSetType.Exclude:
                            return new InputSet(InputSetType.Include, Values.Except(other.Values).ToArray());
                    }

                default:
                case InputSetType.Exclude:
                    switch (other.Type)
                    {
                        case InputSetType.Zero:
                            return new InputSet(InputSetType.Include);

                        case InputSetType.Include:
                            return new InputSet(InputSetType.Include, other.Values.Except(Values).ToArray());

                        default:
                        case InputSetType.Exclude:
                            return new InputSet(InputSetType.Exclude, Values.Union(other.Values).ToArray());
                    }
            }
        }

        /// <summary>
        /// Two sets subtraction
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        internal InputSet Except(InputSet other)
        {
            switch (Type)
            {
                case InputSetType.Zero:
                    return new InputSet(InputSetType.Include);

                case InputSetType.Include:
                    switch (other.Type)
                    {
                        case InputSetType.Zero:
                            return this;

                        case InputSetType.Include:
                            return new InputSet(InputSetType.Include, Values.Except(other.Values).ToArray());

                        default:
                        case InputSetType.Exclude:
                            return new InputSet(InputSetType.Include, Values.Intersect(other.Values).ToArray());
                    }

                default:
                case InputSetType.Exclude:
                    switch (other.Type)
                    {
                        case InputSetType.Zero:
                            return this;

                        case InputSetType.Include:
                            return new InputSet(InputSetType.Exclude, Values.Union(other.Values).ToArray());

                        default:
                        case InputSetType.Exclude:
                            return new InputSet(InputSetType.Include, other.Values.Except(Values).ToArray());
                    }
            }
        }

        internal static string Unescape(string s)
        {
            var result = new StringBuilder();
            var length = s.Length;

            for (int i = 0; i < length; i++)
            {
                if (s[i] == '\\')
                {
                    char c;
                    switch (s[i + 1])
                    {
                        case '0':
                            c = default(char);
                            break;

                        case 'r':
                            c = '\r';
                            break;

                        case 'n':
                            c = '\n';
                            break;

                        default:
                            c = s[i + 1];
                            break;
                    }

                    result.Append(c);
                    i++;
                }
                else
                    result.Append(s[i]);
            }

            return result.ToString();
        }
    }
}
