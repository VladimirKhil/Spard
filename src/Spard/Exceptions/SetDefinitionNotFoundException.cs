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
        public string[] SetNameAndAttributes { get; internal set; }

        public override string Message
        {
            get
            {
                return string.Format("Set \"{0}\" definition was not found", string.Join(", ", SetNameAndAttributes));
            }
        }

        public SetDefinitionNotFoundException()
        {

        }
    }
}
