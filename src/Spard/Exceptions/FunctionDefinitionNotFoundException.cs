namespace Spard.Exceptions
{
    /// <summary>
    /// Function definition is not found
    /// </summary>
    public sealed class FunctionDefinitionNotFoundException : SpardException
    {
        /// <summary>
        /// Function name
        /// </summary>
        public string FunctionName { get; internal set; }

        public override string Message
        {
            get
            {
                return string.Format("Function \"{0}\" definitions was not found", FunctionName);
            }
        }
    }
}
