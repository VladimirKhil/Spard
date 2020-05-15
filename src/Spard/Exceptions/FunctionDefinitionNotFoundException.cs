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

        public override string Message => $"Function \"{FunctionName}\" definitions was not found";

        public FunctionDefinitionNotFoundException(string message) : base(message)
        {
        }

        public FunctionDefinitionNotFoundException(string message, System.Exception innerException) : base(message, innerException)
        {
        }

        public FunctionDefinitionNotFoundException()
        {
        }
    }
}
