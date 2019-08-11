namespace Spard.Common
{
    /// <summary>
    /// Expression which can be placed inside instructions
    /// </summary>
    internal interface IInstructionExpression
    {
        /// <summary>
        /// Does this expression require additional right argument
        /// </summary>
        bool RightArgumentNeeded { get; }
    }
}
