using Spard.Sources;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System;
using System.Text;

namespace Spard.Common
{
    internal static class ValueConverter
    {
        internal static BigInteger ConvertToNumber(object value)
        {
			if (value is string stringValue)
				return BigInteger.Parse(stringValue);

			if (value is IEnumerable<char> charEnumerable)
				return BigInteger.Parse(new string(charEnumerable.ToArray()));

			if (value is IEnumerable<object> enumerable)
			{
				if (enumerable.Count() == 1)
				{
					var first = enumerable.First();

					if (first is BigInteger)
						return (BigInteger)first;
				}

				return BigInteger.Parse(new string(enumerable.Cast<char>().ToArray()));
			}

			return BigInteger.Parse(value.ToString());
        }

        internal static string Escape(object value)
        {
            if (value == null)
                return null;

            var str = value.ToString();
            var result = new StringBuilder();
            for (int i = 0; i < str.Length; i++)
            {
                var c = str[i];
                switch (c)
                {
                    case ':':
                    case '\'':
                    case '}':
                    case '{':
                        result.Append('\'').Append(c);
                        break;

                    default:
                        result.Append(c);
                        break;
                }
            }

            return result.ToString();
        }

        internal static IEnumerable<object> ConvertToEnumerable(object value)
        {
            // EnumerableValue does not expand into an IEnumerable intentionally (it was invented for this purpose)
            if (value == null)
                return Array.Empty<object>();

            if (value == BindingManager.NullValue)
                return BindingManager.NullValue;

			if (value is IEnumerable enumerable)
			{
				var result = Enumerable.Empty<object>();
				foreach (var item in enumerable)
				{
					var res = ConvertToEnumerable(item);
					if (res == BindingManager.NullValue)
						return BindingManager.NullValue;

					result = result.Concat(res);
				}

				return result;
			}

			return new object[] { value };
        }

        internal static object GetValue(this ISource source, int startIndex, int length)
        {
            if (length == 0)
                return Enumerable.Empty<object>();

            var result = source.Subarray(startIndex, length);

            if (length == 1)
                return result.Cast<object>().First();

            return result;
        }

        internal static object Evaluate(object value)
        {
			if (value is IEnumerable enumerable)
				return enumerable.Cast<object>().ToArray();

			return value;
        }

        internal static object ConvertToSingle(object value)
        {
			if (value is IEnumerable enumerable && enumerable.Cast<object>().Count() == 1)
				return enumerable.Cast<object>().FirstOrDefault();

			return value;
        }

        internal static ISource ConvertToSource(IEnumerable source)
        {
			if (source is string stringInput)
				return new StringSource(stringInput);

			return new BufferedSource(source);
        }

        internal static ISource ConvertToSource(object value)
        {
			if (value is IEnumerable enumerable)
				return ConvertToSource(enumerable);

			return new BufferedSource(new object[] { value });
        }

		internal static string ConvertToString(object value)
		{
			if (value is string s)
				return s;

			if (value is object[] objArray)
				return string.Join(",", objArray.Select(item => ValueConverter.Escape(item)));

			if (value is IEnumerable<object> enumerable)
			{
				var result = new StringBuilder();

				foreach (var item in enumerable)
				{
					if (item is char c)
						result.Append(c);
					else
						result.Append('{').Append(item).Append('}');
				}

				return result.ToString();
			}

			return value.ToString();
		}
    }
}
