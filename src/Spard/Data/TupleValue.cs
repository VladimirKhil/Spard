using System.Text;

namespace Spard.Data
{
    public sealed class TupleValue
    {
        public object[] Items { get; set; }

        public TupleValue()
        {

        }

        public TupleValue(params object[] items)
        {
            Items = items;
        }

        public override string ToString()
        {
			var result = new StringBuilder();

			foreach (var item in Items)
			{
				if (result.Length > 0)
					result.Append(' ');

				if (item is TupleValue tupleValue)
				{
					result.Append('{').Append(item).Append('}');
				}
				else
				{
					result.Append(item);
				}
			}

            return result.ToString();
        }
    }
}
