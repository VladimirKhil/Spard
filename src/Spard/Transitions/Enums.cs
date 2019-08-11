namespace Spard.Transitions
{
    public enum InputSetType
    {
        /// <summary>
        /// Allow items from Values
        /// </summary>
        Include,
        /// <summary>
        /// Do not allow items from Values (if Values is empty, everything is allowed)
        /// </summary>
        Exclude,
        /// <summary>
        /// Do not move futher
        /// </summary>
        Zero
    }
}
