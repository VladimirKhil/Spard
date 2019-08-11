namespace Spard.Common
{
    /// <summary>
    /// The unique key of the recursive state. Allows you to check whether we were in the same state before (i.e., did not enter into recursion)
    /// </summary>
    internal sealed class RecursiveStateKey
    {
        /// <summary>
        /// Input position
        /// </summary>
        private readonly int _position;

        /// <summary>
        /// Name of called set
        /// </summary>
        private readonly string _name;

        /// <summary>
        /// Arguments of called set
        /// </summary>
        private readonly object[] _args = null;

        /// <summary>
        /// Index of rule which is used
        /// </summary>
        private readonly int _index;

        /// <summary>
        /// Creates the key
        /// </summary>
        /// <param name="position">Input position</param>
        /// <param name="name">Name of called set</param>
        /// <param name="args">Arguments of called set</param>
        /// <param name="index">Index of rule which is used</param>
        public RecursiveStateKey(int position, string name, object[] args, int index)
        {
            _position = position;
            _name = name;
            _args = args;
            _index = index;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is RecursiveStateKey stateKey))
                return base.Equals(obj);

            if (_position != stateKey._position || _name != stateKey._name || _args.Length != stateKey._args.Length || _index != stateKey._index)
                return false;

            for (int i = 0; i < _args.Length; i++)
            {
                if (!object.Equals(_args[i], stateKey._args[i]))
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return _position + _args.Length + 10 * _index;
        }
    }
}
