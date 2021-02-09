namespace Spard.Service.Contract
{
    /// <summary>
    /// Describes full SPARD example info.
    /// </summary>
    public sealed class SpardExampleInfo : SpardExampleBaseInfo
    {
        /// <summary>
        /// Input data.
        /// </summary>
        public string Input { get; set; }

        /// <summary>
        /// SPARD transformation rules.
        /// </summary>
        public string Transform { get; set; }
    }
}
