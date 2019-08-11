using Spard.Common;
using Spard.Expressions;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Spard.Core
{
    internal interface IRuntimeInfo
    {
        IExpressionRoot Root { get; }

        CancellationToken CancellationToken { get; }

        Stack<StackFrame> StackTrace { get; }

        void SaveBestTry(MatchInfo matchInfo);

        /// <summary>
        /// Get cache
        /// </summary>
        /// <param name="instruction"></param>
        /// <returns></returns>
        CacheData GetDict(Instruction instruction);

        Dictionary<string, TimeSpan> UsedTime { get; }
    }
}
