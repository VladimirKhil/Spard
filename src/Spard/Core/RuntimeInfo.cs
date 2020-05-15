using Spard.Common;
using Spard.Expressions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Spard.Core
{
    /// <summary>
    /// Transform information
    /// </summary>
    public sealed class RuntimeInfo: IRuntimeInfo
    {
        private readonly Dictionary<Instruction, CacheData> _cache = new Dictionary<Instruction, CacheData>();

        /// <summary>
        /// Call stack
        /// </summary>
        internal Stack<StackFrame> StackTrace { get; } = new Stack<StackFrame>();

        /// <summary>
        /// Call stack
        /// </summary>
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        Stack<StackFrame> IRuntimeInfo.StackTrace { get { return StackTrace; } }

        /// <summary>
        /// Partial match information for failed overall match
        /// </summary>
        public MatchInfo BestTry { get; set; } = null;

        /// <summary>
        /// Whether to preserve the best variant of match from the unmatched upon failure
        /// </summary>
        public bool SearchBestVariant { get; set; } = false;

        /// <summary>
        /// Root Expression Tree. Used for global tree queries
        /// </summary>
        internal IExpressionRoot Root { get; }

        /// <summary>
        /// Root Expression Tree. Used for global tree queries
        /// </summary>
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        IExpressionRoot IRuntimeInfo.Root { get { return Root; } }

        public CancellationToken CancellationToken { get; private set; }

        /// <summary>
        /// Used time records
        /// </summary>
        public Dictionary<string, TimeSpan> UsedTime { get; private set; } = new Dictionary<string, TimeSpan>();

        internal RuntimeInfo(IExpressionRoot root, CancellationToken cancellationToken)
        {
            Root = root;
            CancellationToken = cancellationToken;
        }

        CacheData IRuntimeInfo.GetDict(Instruction instruction)
        {
            if (_cache.TryGetValue(instruction, out CacheData result))
                return result;

            result = new CacheData();
            _cache[instruction] = result;

            return result;
        }

        public string PrintStackTrace()
        {
            var result = new StringBuilder();
            foreach (var item in StackTrace)
            {
                result.AppendFormat("   {0}: {1}", item.InputPosition, item.Expression).AppendLine();
            }

            return result.ToString();
        }

        public void SaveBestTry(MatchInfo matchInfo)
        {
            BestTry = MatchInfo.Best(BestTry, matchInfo);
        }
    }
}
