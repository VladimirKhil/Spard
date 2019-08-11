using System;

namespace Spard.Exceptions
{
    /// <summary>
    /// SPARD transformation common error
    /// </summary>
    public class SpardException : Exception
    {
        public SpardException()
        {

        }

        public SpardException(string message)
            : base(message)
        {

        }

        public SpardException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }
}
