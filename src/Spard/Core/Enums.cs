using System;

namespace Spard.Core
{
    /// <summary>
    /// Operators priorities
    /// </summary>
    public enum Priorities
    {
        Bracket = -20,
        Primitive = -10,
        Definition = 0,
        TypeDefinition = 0,
        Block = 1,
        FunctionTuple = 1,
        Translation = 5,
        Unification = 5,
        Function = 10,
        Bigger = 10,
        InlineTypeDefinition = 20,
        VariableJoiner = 30,
        Add = 40,
        Multiply = 45,
        TupleValue = 47,
        Or = 50,
        And = 53,
        NamedValue = 54,
        Qualifier = 55,
        Sequence = 60,
        Instruction = 105,
        FunctionCall = 108,
        Query = 109,
        Not = 80,
        StringValue = 90,
        Optional = 100,
        SeveralTime = 100,
        MultiTime = 100,
        Counter = 100,
        Range = 110,
        ComplexValue = 120,
        Set = 120
    }

    /// <summary>
    /// Location towards anything
    /// </summary>
    public enum Relationship
    {
        /// <summary>
        /// Location on the left
        /// </summary>
        Left,
        /// <summary>
        /// Location on the right
        /// </summary>
        Right
    }

    /// <summary>
    /// Function apply direction
    /// </summary>
    [Flags]
    public enum Directions
    {
        /// <summary>
        /// The function is not applicable
        /// </summary>
        None = 0,
        /// <summary>
        /// The function can be applied to the left
        /// </summary>
        Left = 1,
        /// <summary>
        /// The function can be applied to the right
        /// </summary>
        Right = 2,
        /// <summary>
        /// Two-way function
        /// </summary>
        Both = 3
    };

    /// <summary>
    /// Transformation mode
    /// </summary>
    public enum TransformMode
    {
        /// <summary>
        /// Skip untransformed objects and move futher
        /// </summary>
        Reading,
        /// <summary>
        /// Send untransformed object as-is to the output
        /// </summary>
        Modification,
        /// <summary>
        /// Stop when object cannot be transformed
        /// </summary>
        Function
    }

    /// <summary>
    /// Local transformation parameters
    /// </summary>
    [Flags]
    internal enum Parameters : uint
    {
        /// <summary>
        /// No extra parameters
        /// </summary>
        None = 0,
        /// <summary>
        /// Ignore template space chars
        /// </summary>
        IgnoreSP = 1,
        /// <summary>
        /// Use "lazy" algorithm while pattern matching
        /// </summary>
        IsLazy = 2,
        /// <summary>
        /// Keep original source of error
        /// </summary>
        KeepInitiator = 4,
        /// <summary>
        /// Call function from the right to the left
        /// </summary>
        Left = 8,
        /// <summary>
        /// Search for best match variant when no full match is found.
        /// Affects only on polyvariant expressions (Or, Sequence, etc.)
        /// </summary>
        SearchBestVariant = 16,
        /// <summary>
        /// Save set match
        /// </summary>
        Match = 32,
        /// <summary>
        /// Save set match as full match tree
        /// </summary>
        FullMatch = 64,
        /// <summary>
        /// Save variable value match
        /// </summary>
        MatchVar = 128,
        /// <summary>
        /// Obsolete
        /// </summary>
        Full = 256,
        /// <summary>
        /// Function is a multivalue function
        /// </summary>
        Multi = 512,
        /// <summary>
        /// Linebreak symbols does not match
        /// </summary>
        Line = 1024,
        /// <summary>
        /// Take into account the left recursion at the downstream analysis
        /// </summary>
        LeftRecursion = 2048,
        /// <summary>
        /// Optional property
        /// </summary>
        Optional = 4096,
        /// <summary>
        /// Ignore case in pattern matching
        /// </summary>
        CaseInsensitive = 8192,
        /// <summary>
        /// Match with source tail
        /// </summary>
        /// <remarks>Internal parameter</remarks>
        IsTail = 16384,
        /// <summary>
        /// Collect values tuple into single variable
        /// </summary>
        Collect = 2 << 15,
    }
}
