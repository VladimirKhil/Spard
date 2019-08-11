namespace Spard.Common
{
    /// <summary>
    /// String reference wrapper
    /// </summary>
    internal sealed class StringWrapper
    {
        private StringWrapper _clone = null;
        
        /// <summary>
        /// Object value
        /// </summary>
        internal string Value { get; set; }

        internal void Unclone()
        {
            _clone = null;
        }

        public override string ToString()
        {
            return Value;
        }

        public object Clone()
        {
            if (_clone == null)
            {
                _clone = new StringWrapper() { Value = Value };
            }

            return _clone;
        }
    }
}
