using Spard.Core;
using Spard.Expressions;
using Spard.Transitions.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Spard.Transitions
{
    /// <summary>
    /// The builder of a transition diagram from a tree tranformer
    /// </summary>
    internal sealed class TransitionGraphBuilder
    {
        internal static TransitionStateBase Create(TransitionTableResultCollection collection, IExpressionRoot root, bool isExpression = false, Directions direction = Directions.Right, CancellationToken? cancellationToken = null)
        {
            return new TransitionGraphBuilder(cancellationToken ?? CancellationToken.None, root, isExpression, direction).Build(collection);
        }

        private CancellationToken _cancellationToken;

        /// <summary>
        /// Is the table created only from single expression (otherwise - from the entire converter)
        /// </summary>
        private readonly bool _isExpression = false;

        /// <summary>
        /// Common set of states; allows you to repeatedly not create previously obtained states and organize circular references
        /// Stores links between records and states
        /// </summary>
        private Dictionary<TransitionTableResultCollection, TransitionStateBase> _allStates = new Dictionary<TransitionTableResultCollection, TransitionStateBase>();

        /// <summary>
        /// Backlinks table
        /// </summary>
        private readonly Dictionary<TransitionStateBase, TransitionTableResultCollection> _backStates = new Dictionary<TransitionStateBase, TransitionTableResultCollection>();
        /// <summary>
        /// Processing states queue
        /// </summary>
        private Queue<TransitionState> _workingStates = new Queue<TransitionState>();

        /// <summary>
        /// Initial state
        /// </summary>
        private readonly TransitionState _initialState = new TransitionState();

        /// <summary>
        /// Transition table from the initial state
        /// </summary>
        private TransitionTable _initialTable = null;

        /// <summary>
        /// Dead-end states (may give the last partial result, but no longer lead to overall success)
        /// </summary>
        private Dictionary<TransitionTableResultCollection, TransitionStateBase> _badStates = new Dictionary<TransitionTableResultCollection, TransitionStateBase>();

        /// <summary>
        /// States including result insertion points
        /// </summary>
        private Dictionary<TransitionStateBase, PlannedLinksModification> _statesModifications = new Dictionary<TransitionStateBase, PlannedLinksModification>();

        /// <summary>
        /// Graph building settings
        /// </summary>
        private readonly TransitionSettings _settings;

        private TransitionGraphBuilder(CancellationToken cancellationToken, IExpressionRoot root, bool isExpression, Directions direction)
        {
            _cancellationToken = cancellationToken;
            _settings = new TransitionSettings(root, direction);
            _isExpression = isExpression;
        }

        /// <summary>
        /// Create a transformation state based on the transition table entry
        /// (expressions are replaced with references to other states generated from the tables of these expressions;
        /// cycles can be created)
        /// </summary>
        /// <param name="collection"></param>
        /// <remarks>The central function in the construction of the table transformer</remarks>
        /// <returns></returns>
        private TransitionStateBase Build(TransitionTableResultCollection collection)
        {
            _allStates[collection] = _initialState;
            _backStates[_initialState] = collection;
            _workingStates.Enqueue(_initialState);

            collection.CalculateUsedVars();

            // The main cycle of transition graph building
            while (_workingStates.Any())
            {
                _cancellationToken.ThrowIfCancellationRequested();

                // The current pair "status - result set" being processed.
                var workingState = _workingStates.Dequeue();
                var workingCollection = _backStates[workingState];

                var table = BuildNextTable(workingState, workingCollection, out bool checkStateForExistence);

                if (_initialTable == null)
                    _initialTable = table;

                // We create own state for each key of a transition table
                foreach (var record in table)
                {
                    ProcessRecord(workingState, workingCollection, checkStateForExistence, record);
                }

                foreach (var item in workingCollection)
                {
                    item.Modifications = null;
                }

                //Optimize(workingState);
            }

            return _initialState;
        }

        /// <summary>
        /// Try to perform state optimization
        /// </summary>
        /// <param name="workingState"></param>
        private void Optimize(TransitionState workingState)
        {
            // If this state only absorbs characters until the input is complete, it can be processed faster
            if (workingState.table.Count != 1 || workingState.secondTable.Count != 1)
                return;

            var singleMove = workingState.table.First();
            var restMove = workingState.secondTable[0];

            if (singleMove.Key != InputSet.EndOfSource || restMove.Item1 != InputSet.ExcludeEOS)
                return;

            var targetLink = singleMove.Value;
            var currentLink = restMove.Item2;

            var targetState = targetLink.State;
            var currentState = currentLink.State;

            if (!targetState.IsFinal || currentState != workingState)
                return;

            if (targetLink.Actions.Count > 0 || currentLink.Actions.Count != 1)
                return;

            if (!(currentLink.Actions[0] is AppendVarAction action) || action.Item != null || action.Depth != 0)
                return;

            var name = action.Name;
            if (!(((FinalTransitionState)targetState).Result is Query query) || query.Name != name)
                return;

            var a = query.Name;
        }

        /// <summary>
        /// Process one of the transitions of the current transition table. Create new states if necessary
        /// </summary>
        /// <param name="workingState">Current state</param>
        /// <param name="workingCollection">Current set of transform rules</param>
        /// <param name="checkStateForExistence">Check whether there is already such a state</param>
        /// <param name="record">Processing transition rule</param>
        private void ProcessRecord(TransitionState workingState, TransitionTableResultCollection workingCollection, bool checkStateForExistence, KeyValuePair<InputSet, TransitionTableResultCollection> record)
        {
            PlannedLinksModification modification = null;
            Expression modificationResult = null;
            IEnumerable<PlannedLinksModification> delta = null;

            var collection = record.Value; // New expressions to transform
            var last = collection.Last(); // Lowest of the expressions

            var actions = ProcessContextChange(collection, workingCollection.Last());

            var topModifications = collection[0].Modifications;
            var bottomModifications = last.Modifications?.Clone();

            // Whether the previously saved result popped out
            if (topModifications != null && topModifications.Any())
            {
                PopNewResult(collection, topModifications, actions);
            }

            // Does it contain the result
            var hasResult = last.IsResult;
            var hasZeroResult = last.ZeroMoveResult != null;
            var isFinal = hasResult && collection.Count == 1;

            var isBad = collection[0].ZeroStop > 0 || _badStates.ContainsValue(workingState);
            var isExisting = false;

            // It is necessary to check whether there is already such a state in the list of states
            if (!isBad && checkStateForExistence && _allStates.TryGetValue(collection, out TransitionStateBase state) && (!hasResult || workingState != _initialState || state != _initialState))
            {
                isExisting = true;
                ProcessExistingState(workingState, workingCollection, state, ref modification, ref modificationResult, ref delta, collection, last, actions, bottomModifications, hasResult, hasZeroResult, isFinal);
            }
            else if (isBad && _badStates.TryGetValue(collection, out state))
            {
                // Need to register
                if (!_statesModifications.TryGetValue(state, out modification))
                    modification = new PlannedLinksModification();

                modificationResult = last.Expression;
            }
            else
            {
                // Is the state final
                if (isFinal && !isBad)
                {
                    state = new FinalTransitionState(collection[0].Expression);

                    // Should some results be deleted (they were overwritten by the current result)
                    var prevLastResult = workingCollection[workingCollection.Count - 1];
                    var oldList = prevLastResult.Modifications;
                    if (oldList != null && oldList.Any())
                    {
                        if (bottomModifications != null)
                        {
                            delta = oldList.Except(bottomModifications); // Deleted results
                            if (delta.Any())
                            {
                                modificationResult = null;
                                modification = new PlannedLinksModification(true);
                            }
                        }
                        else
                        {
                            // It is necessary to remove not a fixed number of previous results, but all
                            actions.Add(new InsertResultAction(null, -1));
                        }
                    }
                }
                else
                {
                    state = ProcessCommonState(workingCollection, ref modification, ref modificationResult, ref delta, collection, last, hasResult, hasZeroResult, isBad);
                }

                if (!isBad)
                {
                    _allStates[collection] = state;                    
                }

                _backStates[state] = collection;
            }

            // Add a transition edge from the current state to the target
            // TODO: here you can search for the existing edge in workingState.table and workingState.secondTable and reuse it
            var link = new TransitionLink(state, actions);

            if (isExisting && _isExpression)
                link.Actions.Add(new IncreaseIntermediateResultIndexAction(workingCollection[0].IntermediateResultIndex));

            var recordKey = record.Key;
            if (recordKey.Type == InputSetType.Include && recordKey.Values.Count() == 1)
                workingState.table[recordKey.Values.First()] = link;
            // TODO: the underlying code is slower, although it breaks everything up in Dictionary
            // Most likely, it will be faster if you strongly fill _secondTable
            // This place should be left for additional research if the question of critical performance optimization arises
            //if (recordKey.Type == InputSetType.Include)
            //{
            //    foreach (var value in recordKey.Values)
            //    {
            //        workingState.table[value] = link;
            //    }
            //}
            else
                workingState.secondTable.Add(Tuple.Create(recordKey, link));

            if (modification != null)
            {
                modification.RegisterForInsertResult(link, modificationResult);

                if (delta != null)
                {
                    foreach (var mod in delta)
                    {
                        mod.RegisterForRemove(modification, link);
                    }
                }
            }
        }

        private TransitionStateBase ProcessCommonState(TransitionTableResultCollection workingCollection, ref PlannedLinksModification modification, ref Expression modificationResult, ref IEnumerable<PlannedLinksModification> delta, TransitionTableResultCollection collection, TransitionTableResult last, bool hasResult, bool hasZeroResult, bool isBad)
        {
            TransitionStateBase state;
            // Ordinary state
            if (isBad)
            {
                state = new BadTransitionState(collection[0].ZeroStop);
                _badStates[collection] = state;
            }
            else
            {
                state = new TransitionState();
                _workingStates.Enqueue((TransitionState)state);
            }

            if (hasResult || hasZeroResult) // Is there a possible result
            {
                if (!hasZeroResult)
                    modificationResult = last.Expression;
                else if (!hasResult)
                    modificationResult = last.ZeroMoveResult;
                else
                {
                    modificationResult = new Sequence(
                            last.ZeroMoveResult,
                            last.Expression
                        );
                }

                var insertListLast = last.Modifications;
                if (insertListLast == null)
                    last.Modifications = insertListLast = new ModificationsList();

                // Should some results be deleted (they were overwritten by the current result)
                var prevLastResult = workingCollection[workingCollection.Count - 1];
                var oldList = prevLastResult.Modifications;
                if (oldList != null)
                {
                    delta = oldList.Except(insertListLast); // Deleted results
                }

                if (!(modificationResult is Empty) || delta != null && delta.Any())
                {
                    modification = new PlannedLinksModification();
                    _statesModifications[state] = modification;
                    insertListLast.Add(modification);
                }
            }

            return state;
        }

        private void ProcessExistingState(TransitionState workingState, TransitionTableResultCollection workingCollection, TransitionStateBase state, ref PlannedLinksModification modification, ref Expression insertionResult, ref IEnumerable<PlannedLinksModification> delta, TransitionTableResultCollection collection, TransitionTableResult last, List<TransitionAction> actions, ModificationsList bottomModifications, bool hasResult, bool hasZeroResult, bool isFinal)
        {
            // Some variables may need to be renamed
            var currentVars = collection.UsedVars;
            var newVars = _backStates[state].UsedVars;

            var defferedActions = new List<TransitionAction>();
            var tempIndex = 0;

            for (int i = 0; i < currentVars.Length; i++)
            {
                if (currentVars[i] != newVars[i])
                {
                    var renamed = false;
                    for (int j = i + 1; j < currentVars.Length; j++)
                    {
                        if (currentVars[j] == newVars[i])
                        {
                            // Name collision, we need to use a temporary variable
                            var tempName = "Temp" + tempIndex++;
                            actions.Add(new RenameVarAction(currentVars[i], tempName));
                            defferedActions.Add(new RenameVarAction(tempName, newVars[i]));
                            renamed = true;
                            break;
                        }
                    }

                    if (!renamed)
                        actions.Add(new RenameVarAction(currentVars[i], newVars[i]));
                }
            }

            actions.AddRange(defferedActions);

            // We may need to preserve the result
            if ((hasResult || hasZeroResult) && !isFinal)
            {
                // Need to register
                if (!_statesModifications.TryGetValue(state, out modification))
                    modification = new PlannedLinksModification();

                if (hasResult)
                    insertionResult = last.Expression;

                if (hasZeroResult)
                {
                    actions.Add(new InsertResultAction(last.ZeroMoveResult));
                    actions.Add(new ReturnResultAction());
                }

                // This functionality is questionable. It generates an extra state
                //if (!hasZeroResult)
                //{
                //    var hasStop = workingState.table.ContainsKey(InputSet.EndOfSource);
                //    if (!hasStop)
                //    {
                //        foreach (var item in workingState.secondTable)
                //        {
                //            if (!item.Item1.Intersect(InputSet.IncludeEOS).IsEmpty)
                //            {
                //                hasStop = true;
                //                break;
                //            }
                //        }
                //    }

                //    if (!hasStop)
                //    {
                //        var finalState = new FinalTransitionState(insertionResult);
                //        var finalLink = new TransitionLink(finalState, actions);

                //        workingState.table[InputSet.EndOfSource] = finalLink;
                //    }
                //}

                // Should some results be deleted (they were overwritten by the current result)
                var prevLastResult = workingCollection[workingCollection.Count - 1];
                var oldList = prevLastResult.GetInsertionPoints();
                if (oldList != null)
                {
                    if (bottomModifications != null)
                        delta = oldList.Except(bottomModifications); // Deleted results
                    else
                    {
                        // It is necessary to remove not a fixed number of previous results, but all
                        actions.Add(new InsertResultAction(null, -1));
                    }
                }
            }
            else if (isFinal)
            {
                if (hasZeroResult)
                {
                    actions.Insert(0, new InsertResultAction(last.ZeroMoveResult));
                    actions.Insert(1, new ReturnResultAction());
                }

                // Should some results be deleted (they were overwritten by the current result)
                var prevLastResult = workingCollection[workingCollection.Count - 1];
                var oldList = prevLastResult.GetInsertionPoints();
                if (oldList != null)
                {
                    if (bottomModifications != null)
                    {
                        delta = oldList.Except(bottomModifications); // Deleted results
                        if (delta.Any())
                        {
                            insertionResult = null;
                            modification = new PlannedLinksModification(true);
                        }
                    }
                    else
                    {
                        // It is necessary to remove not a fixed number of previous results, but all
                        actions.Add(new InsertResultAction(null, -1));
                    }
                }
            }
        }

        /// <summary>
        /// Return the result because it has floated to the surface (it turned out to be in the upper rule)
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="modificationsList"></param>
        /// <param name="actions"></param>
        private static void PopNewResult(TransitionTableResultCollection collection, List<PlannedLinksModification> modificationsList, List<TransitionAction> actions)
        {
            // The last item has the biggest number of accumulated results.
            // Subtracting them, we find out the number of results that are not implemented in this step
            // In the current transformation theory, this number is an invariant to the path between states
            var downList = collection.Last().Modifications;
            actions.Add(new ReturnResultAction(downList.Count - modificationsList.Count));

            foreach (var point in modificationsList)
            {
                point.Apply(); // The result was realized, so the transformation should be applied

                for (int i = 1; i < collection.Count; i++)
                {
                    collection[i].Modifications.Remove(point);
                }
            }

            collection[0].Modifications = null;

            for (int i = 1; i < collection.Count; i++)
            {
                if (collection[i].Modifications.Count == 0)
                    collection[i].Modifications = null;
            }
        }

        /// <summary>
        /// Build a transition table to new states from the current
        /// </summary>
        /// <param name="workingState">Current state</param>
        /// <param name="workingCollection">The set of expressions associated with the current state</param>
        /// <param name="toRemove"></param>
        /// <param name="checkStateForExistence">Check whether there is already such a state in the state graph (otherwise it is a unique state)</param>
        /// <returns>Formed transition table</returns>
        private TransitionTable BuildNextTable(TransitionState workingState, TransitionTableResultCollection workingCollection, out bool checkStateForExistence)
        {
            var tables = new TransitionTable[workingCollection.Count];

            TransitionTable currentTable;
            checkStateForExistence = true;

            // Build tables for each of the expressions, then combine them
            for (int i = 0; i < workingCollection.Count; i++)
            {
                var tableResult = workingCollection[i];

                if (!tableResult.IsResult)
                {
                    if (tableResult.Expression == null)
                    {
                        currentTable = new TransitionTable();
                        if (i == 0)
                            workingState.IntermediateResultIndex = -1;
                        else
                        {
                            workingState.IntermediateResultIndex = tableResult.IntermediateResultIndex;
                            for (int j = 0; j < i; j++)
                            {
                                foreach (var item in tables[j])
                                {
                                    foreach (var res in item.Value)
                                    {
                                        res.IntermediateResultIndex++;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        currentTable = tableResult.Expression.BuildTransitionTable(_settings, true).CloneTable();

                        // Used variables inheritance
                        foreach (var record in currentTable)
                        {
                            foreach (var item in record.Value)
                            {
                                item.UsedVars = tableResult.UsedVars;
                                item.IntermediateResultIndex = tableResult.IntermediateResultIndex;
                            }
                        }
                    }
                }
                else
                {
                    // Resulting state
                    if (_badStates.ContainsValue(workingState))
                    {
                        // The state is bad
                        currentTable = new TransitionTable();

                        foreach (var point in tableResult.Modifications)
                        {
                            point.Apply();
                        }

                        tableResult.Modifications = null;
                    }
                    else
                    {
                        // We can state reparing expression since that moment
                        currentTable = _initialTable.CloneTable();

                        // The exit condition at the end of the input (everything is fine, we finished in time)
                        currentTable[InputSet.IncludeEOS] = TransitionTableResultCollection.Create(Empty.Instance, true);
                    }
                }

                tables[i] = currentTable;
                var points = tableResult.Modifications;

                if (points != null)
                {
                    // We transfer result insertion points to derived expressions
                    foreach (var record in currentTable)
                    {
                        foreach (var item in record.Value)
                        {
                            item.Modifications = new ModificationsList(points); // You can't just copy; each point can be added later to every state
                        }
                    }
                }

                if (tableResult.ZeroStop > 0)
                {
                    if (currentTable.ContainsKey(InputSet.Zero))
                        currentTable.Remove(InputSet.Zero);

                    foreach (var item in currentTable)
                    {
                        foreach (var ttr in item.Value)
                        {
                            ttr.ZeroStop = tableResult.ZeroStop + 1;
                        }
                    }
                }

                if (currentTable.ContainsKey(InputSet.Zero))
                {
                    if (_isExpression)
                    {
                        workingState.IntermediateResultIndex = 0;
                        workingCollection[0].IntermediateResultIndex = 1;
                        currentTable.Remove(InputSet.Zero);
                    }
                    else
                    {
                        checkStateForExistence &= !ProcessZeroMove(currentTable);
                    }
                }
            }

            var table = TransitionTable.Join(tables);
            table.SetUsedVars();

            return table;
        }

        /// <summary>
        /// Handle context changes, form necessary actions
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="previousLastResult">The last expression from the list in the previous step.
        /// Only it can contain the result, because it would suppress all subordinate expressions.</param>
        /// <returns></returns>
        private static List<TransitionAction> ProcessContextChange(TransitionTableResultCollection collection, TransitionTableResult previousLastResult)
        {
            var actions = new List<TransitionAction>();

            // We split all the rules into equivalence classes by ContextChange
            var changes = collection.GroupBy(ttr => ttr.ContextChange).ToArray();
            var renamedAny = false;
            
            for (var i = changes.Length - 1; i >= 0; i--) // Enumerating all classes with a key != null
            {
                var change = changes[i];
                //if (change.Key == null)
                //    continue;

                var isUsed = CheckIfNameUsed(changes, i, change, out string usedName);

                CopyVarAction copyAction = null;
                string newName;
                if (isUsed) // Naming conflict. We need to rename the variable
                {
                    RenameVariableUsage(collection, actions, change, usedName, out copyAction, out newName);
                    renamedAny = true;

                    if (change.Key == null)
                        continue;
                }
                else
                {
                    if (change.Key == null)
                        continue;

                    newName = change.Key.Item1;
                }

                var rule = change.First(); // Top rule for this class

                // The last rule of either the current step or the previous one
                var modificationsSource = rule.IsResult ? previousLastResult : collection.Last();
                var insertListBottom = modificationsSource.Modifications;

                var currentModifications = rule.Modifications;

                var newAction = new AppendVarAction(0, newName, rule.ContextChange.Item2);
                actions.Add(newAction);

                if (insertListBottom != null)
                {
                    // These rules will complete the result, and the desired context will be one level deeper
                    foreach (var modification in insertListBottom)
                    {
                        modification.RegisterForIncreaseDepth(newAction);
                        if (copyAction != null)
                            modification.RegisterForIncreaseDepth(copyAction);
                    }

                    if (currentModifications != null)
                    {
                        foreach (var modification in currentModifications)
                        {
                            modification.RegisterForDecreaseDepth(newAction);
                            if (copyAction != null)
                                modification.RegisterForDecreaseDepth(copyAction);
                        }
                    }
                }
            }

            if (renamedAny)
            {
                collection.SetUsedVars();
            }

            return actions;
        }

        /// <summary>
        /// Is this variable used in another class (i.e., the same name but a different value)
        /// </summary>
        /// <param name="changes"></param>
        /// <param name="change"></param>
        /// <returns></returns>
        private static bool CheckIfNameUsed(IGrouping<ContextChange, TransitionTableResult>[] changes, int i, IGrouping<ContextChange, TransitionTableResult> change, out string usedName)
        {
            // Is this variable used in another class (i.e., the same name but a different value)
            var isUsed = false;
            usedName = null;

            // If the class variable is present in other classes (in the key or in the expression), rename it (give a unique name)
            for (var j = i - 1; j >= 0; j--)
            {
                var otherChange = changes[j];
                if (otherChange.Key == change.Key)
                    continue;

                if (otherChange.Key != null && change.Key != null && otherChange.Key.Item1 == change.Key.Item1)
                {
                    usedName = otherChange.Key.Item1;
                    isUsed = true;
                    break;
                }

                if (!isUsed)
                {
                    if (otherChange.Key != null)
                    {
                        foreach (var ttr in change)
                        {
                            if (ttr.UsedVars.Contains(otherChange.Key.Item1))
                            {
                                usedName = otherChange.Key.Item1;
                                isUsed = true;
                                break;
                            }
                        }
                    }
                }

                if (!isUsed)
                {
                    if (change.Key != null)
                    {
                        foreach (var ttr in otherChange)
                        {
                            if (ttr.UsedVars.Contains(change.Key.Item1))
                            {
                                usedName = change.Key.Item1;
                                isUsed = true;
                                break;
                            }
                        }
                    }
                }

                if (isUsed)
                    break;
            }

            return isUsed;
        }

        /// <summary>
        /// Rename the used variable by copying the previous value
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="actions"></param>
        /// <param name="change"></param>
        /// <param name="copyAction"></param>
        /// <param name="newName"></param>
        private static void RenameVariableUsage(TransitionTableResultCollection collection, List<TransitionAction> actions, IGrouping<ContextChange, TransitionTableResult> change, string usedName, out CopyVarAction copyAction, out string newName)
        {
            var allUsedNames = new HashSet<string>();

            foreach (var item in collection)
            {
                foreach (var name in item.UsedVars)
                {
                    allUsedNames.Add(name);
                }
            }

            // Rename the variable (with a unique name, which is important)
            int y = 0;
            do
            {
                newName = "V" + y++;
            } while (allUsedNames.Contains(newName));

            foreach (var ttr in change)
            {
                if (ttr.UsedVars.Contains(usedName))
                {
                    ttr.RenameVar(usedName, newName);
                }
            }

            copyAction = new CopyVarAction(0, usedName, newName);
            actions.Add(copyAction);
        }

        #region ZeroMove

        /// <summary>
        /// Work out a zero shift in the table (it needs to be replaced with real shifts)
        /// </summary>
        /// <param name="table"></param>
        /// <returns>Is the current state unique (it will not be possible to replace it with one of the already formed states in the graph)</returns>
        private bool ProcessZeroMove(TransitionTable table)
        {
            if (_initialTable == null)
            {
                // Zero transition from the initial state is not allowed
                table.Remove(InputSet.Zero);
                return true;
            }

            // If a zero shift is needed to get the result, then we replace it with transitions from the initial state
            var zeroCollection = table[InputSet.Zero];
            var zeroExpression = zeroCollection[0].Expression;
            var isResult = zeroCollection[0].IsResult; // if false, then there is a postcondition

            if (isResult)
            {
                ProcessZeroResult(table, zeroCollection, zeroExpression);
            }
            else
            {
                ProcessZeroTransition(table, zeroExpression);
            }

            table.Remove(InputSet.Zero);

            return false;
        }

        private void ProcessZeroTransition(TransitionTable table, Expression zeroExpression)
        {
            // Filters table
            var filters = zeroExpression.Free().BuildTransitionTable(_settings, true).CloneTable();

            // We must add this collection for all keys above it to the end, for all keys below - to the beginning
            // It is necessary to add a stop for EOF
            // We must add a transition for all other keys

            foreach (var filter in filters)
            {
                var filterKey = filter.Key;

                if (filterKey.Type == InputSetType.Zero)
                    continue; // You cannot make an empty move twice

                // We split this key from all other keys in the table
                // ??? Are all paths after Zero (before = false) unreachable?
                var before = true;
                foreach (var workingLink in table.ToArray())
                {
                    if (object.Equals(InputSet.Zero, workingLink.Key))
                    {
                        before = false;
                        continue;
                    }

                    var crossKey = filterKey.IntersectAndTwoExcepts(workingLink.Key);
                    if (!crossKey.Item1.IsEmpty)
                    {
                        /*if (before)
                        {
                            filterKey = crossKey.Item2;
                            if (filterKey.IsEmpty)
                                break;
                        }
                        else*/
                        if (!before)
                        {
                            // We will never get into other branches
                            table.Remove(workingLink.Key);
                            if (!crossKey.Item3.IsEmpty)
                            {
                                table[crossKey.Item3] = workingLink.Value;
                            }
                        }
                    }
                }

                if (filterKey.IsEmpty)
                    continue;

                foreach (var initialLink in _initialTable)
                {
                    if (object.Equals(initialLink.Key, InputSet.IncludeEOS)) // EOF does not suit us
                        continue;

                    var initialFilterCross = filterKey.IntersectAndTwoExcepts(initialLink.Key);

                    if (initialFilterCross.Item1.IsEmpty)
                        continue;

                    TransitionTableResultCollection coll;
                    if (filter.Value[0].IsResult)
                    {
                        coll = initialLink.Value.CloneCollection();// ZeroClone(initialLink.Value, zeroForwardResults, zeroStopResults);

                        foreach (var item in coll)
                        {
                            item.ZeroMoveResult = filter.Value[0].Expression;
                        }
                    }
                    else
                    {
                        var freeExpr = filter.Value[0].Expression.Free();

                        coll = new TransitionTableResultCollection();

                        if (freeExpr is Function func)
                        {
                            foreach (var item in initialLink.Value)
                            {
                                Expression expr;
                                if (item.IsResult)
                                {
                                    expr = new Function(func.Left, new Sequence(func.Right, item.Expression));
                                    coll.Add(new TransitionTableResult(expr));
                                }
                                else
                                {
                                    // Positive option - we will continue with the input
                                    var itemFunc = (Function)item.Expression;
                                    expr = new Function(new And(func.Left, itemFunc.Left), new Sequence(func.Right, itemFunc.Right));

                                    coll.Add(new TransitionTableResult(expr));

                                    // Negative option - we will finish the current chain, but we will not go further
                                    coll.Add(new TransitionTableResult(func) { ZeroStop = 1 });
                                }
                            }
                        }
                        else
                        {
                            foreach (var item in initialLink.Value)
                            {
                                Expression expr;
                                if (item.IsResult)
                                {
                                    expr = freeExpr;
                                    coll.Add(new TransitionTableResult(expr));
                                }
                                else
                                {
                                    // Positive option - we will continue with the input
                                    expr = new And(freeExpr, item.Expression);

                                    coll.Add(new TransitionTableResult(expr));

                                    // Negative option - we will bring the current chain, but we will not go further
                                    coll.Add(new TransitionTableResult(freeExpr) { ZeroStop = 1 });
                                }
                            }
                        }
                    }

                    AppendFilterCollection(table, initialFilterCross.Item1, coll);
                    //tables[i].Table[initialFilterCross.Item1] = coll;

                    filterKey = initialFilterCross.Item2;
                    if (filterKey.IsEmpty)
                        break;
                }

                // EOF
                if (!filterKey.Intersect(InputSet.IncludeEOS).IsEmpty)
                {
                    AppendFilterCollection(table, InputSet.IncludeEOS, TransitionTableResultCollection.Create(filter.Value[0].Expression, true));
                    //tables[i].Table[null] = TransitionTableResultCollection.Create(filter.Value[0].Expression, true);
                    filterKey = filterKey.Except(InputSet.IncludeEOS);
                }

                if (!filterKey.IsEmpty)
                {
                    var coll = TransitionTableResultCollection.Create(filter.Value[0].Expression, filter.Value[0].IsResult);

                    foreach (var item in coll)
                    {
                        item.ZeroStop = 1;
                    }

                    AppendFilterCollection(table, filterKey, coll);
                    //tables[i].Table[filterKey] = coll;
                }
            }
        }

        private void ProcessZeroResult(TransitionTable table, TransitionTableResultCollection zeroCollection, Expression zeroExpression)
        {
            // We must add this collection for all keys above it to the end
            // It is necessary to add a stop for EOF
            // We must add a transition for all other keys

            // Important: all keys below 0 are unreachable (см. (|b) => 1, [lazy](a*) => 1)
            // Removing them
            var before = true;
            foreach (var workingLink in table.ToArray())
            {
                if (object.Equals(InputSet.Zero, workingLink.Key))
                {
                    before = false;
                    continue;
                }

                if (!before)
                    table.Remove(workingLink.Key);
            }

            foreach (var initialLink in _initialTable)
            {
                if (object.Equals(initialLink.Key, InputSet.IncludeEOS)) // EOF is not suitable for us
                    continue; // Is EOF possible in _initialTable?

                // before = true;

                var initialKey = initialLink.Key;
                // NB: It is worth noting that it may not be necessary to cut those keys at all
                // Maybe we just need to add the missing values
                // but in general IsResult = true, so apparently everything is correct
                foreach (var workingLink in table.ToArray())
                {
                    if (object.Equals(InputSet.Zero, workingLink.Key))
                    {
                        break; // Come to the end
                               //before = false;
                               //continue;
                    }

                    var crossKey = initialKey.IntersectAndTwoExcepts(workingLink.Key);
                    if (!crossKey.Item1.IsEmpty)
                    {
                        //if (before) // that rule is earlier than ours, we adjust to it
                        //{
                        initialKey = crossKey.Item2;

                        if (initialKey.IsEmpty)
                            break;
                        //}
                        //else // now we are above and we can split the key from this rule
                        //{
                        //    table.Remove(workingLink.Key);

                        //    if (!crossKey.Item3.IsEmpty)
                        //    {
                        //        table[crossKey.Item3] = workingLink.Value;
                        //    }
                        //}
                    }
                }

                if (!initialKey.IsEmpty)
                {
                    // Options that do not take into account the transition from the current state
                    // We just return the result (zeroCollection[0].Expression)
                    // and continue to perform the transformation further, as if moving from the initial state
                    var newResults = new TransitionTableResultCollection();
                    foreach (var res in initialLink.Value)
                    {
                        var clone = res.CloneResult();
                        clone.ZeroMoveResult = zeroCollection[0].Expression;
                        newResults.Add(clone);
                    }

                    table[initialKey] = newResults;
                }
            }

            // Attach EOF
            var before2 = true;

            var initialKey2 = InputSet.IncludeEOS;

            foreach (var workingLink in table.ToArray())
            {
                if (object.Equals(InputSet.Zero, workingLink.Key))
                {
                    before2 = false;
                    continue;
                }

                var crossKey = initialKey2.IntersectAndTwoExcepts(workingLink.Key);
                if (!crossKey.Item1.IsEmpty)
                {
                    if (!crossKey.Item3.IsEmpty)
                    {
                        // Splitting key
                        table.Remove(workingLink.Key);
                        table[crossKey.Item3] = workingLink.Value.CloneCollection();
                        table[crossKey.Item1] = workingLink.Value;
                    }

                    //// Adding values

                    //var zeroResultApplied = zeroExpression.Apply(new Context((RuntimeInfo)null));

                    if (before2)
                    {
                        int k = 0;
                        while (k < workingLink.Value.Count && !workingLink.Value[k].IsResult)
                            k++;

                        if (k > 0)
                            workingLink.Value.RemoveRange(0, k);

                        if (!workingLink.Value.Any())
                            workingLink.Value.Add(new TransitionTableResult(zeroExpression, true));
                    }
                    else
                    {
                        workingLink.Value.Clear();
                        workingLink.Value.Add(new TransitionTableResult(zeroExpression, true));
                    }

                    initialKey2 = crossKey.Item2;

                    if (initialKey2.IsEmpty)
                        break;
                }
            }

            if (!initialKey2.IsEmpty)
            {
                table[initialKey2] = zeroCollection;
            }

            // All other keys simply return the result and go into a "bad" state
            var otherKey = new InputSet(InputSetType.Exclude);
            foreach (var item in table)
            {
                otherKey = otherKey.Except(item.Key);
                if (otherKey.IsEmpty)
                    break;
            }

            if (!otherKey.IsEmpty)
            {
                var clone = zeroCollection[0].CloneResult();
                clone.ZeroStop = 1;

                var coll = new TransitionTableResultCollection
                {
                    clone
                };

                table[otherKey] = coll;
            }
        }

        private static void AppendFilterCollection(Dictionary<InputSet, TransitionTableResultCollection> table, InputSet inputSet, TransitionTableResultCollection collection)
        {
            var before = true;
            foreach (var workingLink in table.ToArray())
            {
                if (object.Equals(InputSet.Zero, workingLink.Key))
                {
                    before = false;
                    continue;
                }

                var crossKey = inputSet.IntersectAndTwoExcepts(workingLink.Key);
                if (!crossKey.Item1.IsEmpty)
                {
                    if (!crossKey.Item3.IsEmpty)
                    {
                        table.Remove(workingLink.Key);
                        table[crossKey.Item3] = workingLink.Value.CloneCollection();
                        table[crossKey.Item1] = workingLink.Value;
                    }

                    if (before)
                    {
                        workingLink.Value.AddRange(collection.CloneCollection());
                    }
                    else
                    {
                        // We do not get here yet
                    }

                    inputSet = crossKey.Item2;
                    if (inputSet.IsEmpty)
                        return;
                }
            }

            if (!inputSet.IsEmpty)
            {
                table[inputSet] = collection;
            }
        }

        #endregion
    }
}
