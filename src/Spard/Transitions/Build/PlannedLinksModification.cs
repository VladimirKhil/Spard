using System;
using System.Collections.Generic;
using System.Linq;

namespace Spard.Transitions
{
    /// <summary>
    /// Planned modification in the state graph.
    /// May or may not be needed.
    /// It is used to save information about already completed edges if necessary to make some changes with them in the future
    /// </summary>
    internal sealed class PlannedLinksModification
    {
        /// <summary>
        /// Planned results inserts
        /// </summary>
        private List<Tuple<TransitionLink, InsertResultAction>> plannedResuls = new List<Tuple<TransitionLink, InsertResultAction>>();
        private List<Tuple<TransitionLink, PlannedLinksModification>> removers = new List<Tuple<TransitionLink, PlannedLinksModification>>();

        /// <summary>
        /// Actions that need to increase the depth
        /// </summary>
        private List<ContextAction> toIncreaseDepth = new List<ContextAction>();
        /// <summary>
        /// Actions that need to decrease the depth
        /// </summary>
        private List<ContextAction> toDecreaseDepth = new List<ContextAction>();

        /// <summary>
        /// Has this modification been made
        /// </summary>
        private bool isApplied;

        public PlannedLinksModification(bool isApplied = false)
        {
            this.isApplied = isApplied;
        }

        /// <summary>
        /// Add new result insert to edge
        /// </summary>
        /// <param name="link">Processing edge</param>
        /// <param name="result">Result to add</param>
        /// <param name="removeLastCount">The number last results to exclude</param>
        internal void RegisterForInsertResult(TransitionLink link, Expressions.Expression result, int removeLastCount = 0)
        {
            var action = new InsertResultAction(result, removeLastCount);
            plannedResuls.Add(Tuple.Create(link, action));

            if (isApplied)
            {
                AppendInsertion(link, action);
            }
        }

        /// <summary>
        /// Properly insert an action to add a result before returning the result.
        /// I do not remember why it is so important, but oh well
        /// </summary>
        /// <param name="link"></param>
        /// <param name="action"></param>
        private static void AppendInsertion(TransitionLink link, InsertResultAction action)
        {
            if (link.Actions.Count > 0)
            {
                if (link.Actions.Last() is ReturnResultAction returnResult)
                {
                    // We need to add an insert before returning the result
                    link.Actions.Insert(link.Actions.Count - 1, action);

                    if (action.ProduceResult())
                        returnResult.IncreaseLeftResultsCount(); // This result must be kept

                    return;
                }
            }

            link.Actions.Add(action);
        }

        /// <summary>
        /// Schedule result deletion in another modification associated with some edge
        /// </summary>
        /// <param name="modification"></param>
        /// <param name="link"></param>
        internal void RegisterForRemove(PlannedLinksModification modification, TransitionLink link)
        {
            if (isApplied)
                modification.SetRemove(link);
            else
                removers.Add(Tuple.Create(link, modification));
        }

        /// <summary>
        /// Delete the result associated with the specified edge
        /// </summary>
        /// <param name="link"></param>
        private void SetRemove(TransitionLink link)
        {
            foreach (var item in plannedResuls.Where(t => t.Item1 == link))
            {
                item.Item2.IncreaseRemoveLastCount();
            }
        }

        internal void RegisterForIncreaseDepth(ContextAction newAction)
        {
            if (isApplied)
                newAction.IncreaseDepth();
            else
                toIncreaseDepth.Add(newAction);
        }

        internal void RegisterForDecreaseDepth(ContextAction newAction)
        {
            if (isApplied)
                newAction.DecreaseDepth();
            else
                toDecreaseDepth.Add(newAction);
        }

        /// <summary>
        /// Apply all prepared modifications over the transition graph
        /// </summary>
        internal void Apply()
        {
            if (isApplied)
                return;

            isApplied = true;

            foreach (var link in plannedResuls)
            {                
                AppendInsertion(link.Item1, link.Item2);
            }

            foreach (var item in removers)
            {
                item.Item2.SetRemove(item.Item1);
            }

            foreach (var item in toIncreaseDepth)
            {
                item.IncreaseDepth();
            }

            foreach (var item in toDecreaseDepth)
            {
                item.DecreaseDepth();
            }

            removers.Clear();
            toIncreaseDepth.Clear();
            toDecreaseDepth.Clear();
        }
    }
}
