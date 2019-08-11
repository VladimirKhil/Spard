using System.Collections.Generic;

namespace Spard.Common
{
    /// <summary>
    /// A table catching left recursions in a stack of template calls.
    /// The table key is the tuple of the position in the input, the name of the set and its arguments.
    /// If the table already has an entry with such a key, the left recursion may occur.
    /// The value of the table is a table of states in which this set has already been called (all records of calls to the same set).
    /// </summary>
    internal sealed class RecursiveStateTable : Dictionary<RecursiveStateKey, RecursiveState>
    {
        public RecursiveStateTable()
        {

        }

        public RecursiveStateTable(IDictionary<RecursiveStateKey, RecursiveState> dictionary)
            : base(dictionary)
        {

        }

        public RecursiveStateTable Clone()
        {
            var result = new RecursiveStateTable();
            foreach (var data in this)
            {
                result[data.Key] = data.Value.Clone();
            }
            return result;
        }
    }
}
