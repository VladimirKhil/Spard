namespace Spard.Exceptions
{
    internal sealed class SpardFormatException: SpardException
    {
        public SpardFormatException()
        {

        }

        public SpardFormatException(string message) 
            : base(message)
        {

        }
    }
}
