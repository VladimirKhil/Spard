using Spard.Sources;

namespace Spard.Core
{
    /// <summary>
    /// SPARD Recognizer
    /// </summary>
    interface IRecognizer
    {
        /// <summary>
        /// Recognize expression in input stream
        /// </summary>
        /// <param name="input">Input stream</param>
        /// <param name="context">Recongniztion context</param>
        /// <param name="next">Shold search for next recognizion variant (false to search for the first variant)</param>
        /// <returns>Is recognition variant found</returns>
        bool Match(ISource input, ref IContext context, bool next);
    }
}
