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
            this.LineNum = lineNum;
            this.ColumnNum = columnNum;
        }

        internal ParseException(string message)
            : base(message)
        {

        }

        public override string ToString()
        {
            return string.Format("({0}, {1}) {2}", LineNum, ColumnNum, Message);
        }
    }
}
