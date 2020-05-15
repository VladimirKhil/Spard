using System.Collections;
using System.Text;

namespace Spard.Data
{
    internal sealed class EnumerableValue
    {
        public IEnumerable Value { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();

            foreach (var item in Value)
            {
                sb.Append(item);
            }

            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj is EnumerableValue enumerableValue)
                return object.Equals(Value, enumerableValue.Value);

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
