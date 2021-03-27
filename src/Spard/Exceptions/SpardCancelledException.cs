using System;

namespace Spard.Exceptions
{
    /// <summary>
    /// Signals that transformation was cancelled. 
    /// </summary>
    public sealed class SpardCancelledException : SpardException
    {
        public SpardCancelledException()
        {
        }

        public SpardCancelledException(string message) : base(message)
        {
        }

        public SpardCancelledException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
