namespace Spard.Exceptions
{
    /// <summary>
    /// Set definition not found
    /// </summary>
    public sealed class SetDefinitionNotFoundException: SpardException
    {
        /// <summary>
        /// Set name and attributes
        /// </summary>
        private readonly string[] _setNameAndAttributes;

        public override string Message => $"Set \"{string.Join(", ", _setNameAndAttributes)}\" definition was not found";

        public SetDefinitionNotFoundException(string[] setNameAndAttributes)
        {
            _setNameAndAttributes = setNameAndAttributes;
        }

        public SetDefinitionNotFoundException(string message) : base(message)
        {
        }

        public SetDefinitionNotFoundException(string message, System.Exception innerException) : base(message, innerException)
        {
        }

        public SetDefinitionNotFoundException()
        {
        }
    }
}
