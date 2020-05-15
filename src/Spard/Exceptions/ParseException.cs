namespace Spard.Exceptions
{
    /// <summary>
    /// Parser error
    /// </summary>
    public sealed class ParseException: SpardException
    {
        /// <summary>
        /// Line number
        /// </summary>
        public int LineNum { get; }
        /// <summary>
        /// Column number
        /// </summary>
        public int ColumnNum { get; }

        internal ParseException(int lineNum, int columnNum, string message)
            : base(message)
        {
            LineNum = lineNum;
            ColumnNum = columnNum;
        }

        internal ParseException(string message)
            : base(message)
        {

        }

        public ParseException()
        {
        }

        public ParseException(string message, System.Exception innerException) : base(message, innerException)
        {
        }

        public override string ToString() => $"({LineNum}, {ColumnNum}) {Message}";
    }
}
