using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Spard.Common;
using Spard.Data;
using Spard.Core;

namespace Spard.Transitions
{
    /// <summary>
    /// Defines base class for normal and final transform states.
    /// </summary>
    internal abstract class TransitionStateBase
    {
#if DEBUG
        private static int Index = 0;
        public int _index = Index++;

        public override string ToString() => _index.ToString();
#endif

        /// <summary>
        /// Is it a final state.
        /// </summary>
        protected internal abstract bool IsFinal { get; }

        /// <summary>
        /// Intermediate result insertion index (state asterisk).
        /// -2: the state does not have intermediate result
        /// -1: intermediate result of high priority (it is returned immediately)
        /// 0 or highter: insertion index.
        /// </summary>
        protected internal int IntermediateResultIndex { get; set; } = -2;

        /// <summary>
        /// Goes to next state based on input item.
        /// </summary>
        /// <param name="item">Input item.</param>
        /// <param name="context">
        /// Transformation context (transformation dynamics;
        /// allows you to accumulate the necessary information about previously viewed items).
        /// </param>
        /// <returns>Next state or null if the transition is not possible (transformation fails).</returns>
        protected internal abstract TransitionStateBase Move(object item, ref TransitionContext context, out IEnumerable result);

        /// <summary>
        /// Gets transformation result.
        /// </summary>
        protected internal abstract IEnumerable GetResult(TransitionContext context);

        /// <summary>
        /// Builds the transition description as a table.
        /// </summary>
        internal VisualTable BuildVisualTable()
        {
            var columns = new List<InputSet>();
            var rows = new List<TransitionStateBase>();
            var table = new Dictionary<Tuple<InputSet, TransitionStateBase>, TransitionLink>();

            var active = new Queue<TransitionStateBase>();
            active.Enqueue(this);

            // We collect data (it is necessary to traverse the graph)
            while (active.Any())
            {
                var stateBase = active.Dequeue();
                if (rows.Contains(stateBase))
                    continue;

                rows.Add(stateBase);

                if (stateBase.IsFinal)
                    continue;

                var state = stateBase as TransitionState;
                foreach (var cell in state.Table)
                {
                    var test = new InputSet(InputSetType.Include, cell.Key);
                    if (!columns.Contains(test))
                        columns.Add(test);

                    table[Tuple.Create(test, stateBase)] = cell.Value;
                    active.Enqueue(cell.Value.State);
                }

                foreach (var cell in state.SecondTable)
                {
                    if (!columns.Contains(cell.Item1))
                        columns.Add(cell.Item1);

                    table[Tuple.Create(cell.Item1, stateBase)] = cell.Item2;
                    active.Enqueue(cell.Item2.State);
                }
            }

            // Form a table
            var result = new VisualTable(columns.ToArray(), rows.ToArray());

            for (int i = 0; i < columns.Count; i++)
            {
                for (int j = 0; j < rows.Count; j++)
                {
                    if (table.TryGetValue(Tuple.Create(columns[i], rows[j]), out TransitionLink link))
                        result.Data[j, i] = link;
                }
            }

            return result;
        }

        internal void Save(System.IO.Stream stream)
        {
            var list = new List<TransitionStateBase>();
            var active = new Queue<TransitionStateBase>();
            active.Enqueue(this);

            list.Add(this);

            using (var writer = new System.IO.StreamWriter(stream))
            {
                while (active.Any())
                {
                    var stateBase = active.Dequeue();

#if DEBUG
                    Debug.WriteLine(stateBase._index);
#endif

                    if (stateBase != this)
                        writer.WriteLine();
                    
                    if (stateBase.IsFinal)
                    {
                        writer.Write('=');
                        writer.Write(((FinalTransitionState)stateBase).ResultString);
                        continue;
                    }

                    if (stateBase is BadTransitionState bad)
                    {
                        writer.Write('b');
                        writer.Write(bad.BadLength);
                        continue;
                    }

                    var state = (TransitionState)stateBase;
                    foreach (var cell in state.Table)
                    {
                        Save(list, active, writer, new InputSet(InputSetType.Include, cell.Key), cell.Value);
                    }

                    foreach (var cell in state.SecondTable)
                    {
                        Save(list, active, writer, cell.Item1, cell.Item2);
                    }
                }
            }
        }

        private void Save(List<TransitionStateBase> list, Queue<TransitionStateBase> active, System.IO.StreamWriter writer, InputSet key, TransitionLink link)
        {
            var index = list.IndexOf(link.State);

            if (index == -1)
            {
                list.Add(link.State);
                index = list.Count - 1;
                active.Enqueue(link.State);
            }

            writer.Write(key);
            writer.Write(index);

            if (link.Actions.Count > 0)
            {
                writer.Write("{");
                writer.Write(string.Join(";", link.Actions.Select(act => Escape(act.ToString()))));
                writer.Write("}");
            }
        }

        private static string Escape(string s) => s.Transform("'; => '\\'; \n '\\ => '\\'\\");

        internal static TransitionStateBase Load(System.IO.Stream stream)
        {
            var states = new List<TransitionStateBase>();
            var futureLinks = new Dictionary<int, List<Tuple<int, object, IEnumerable<TransitionAction>>>>();
            var futureLinks2 = new Dictionary<int, List<Tuple<int, InputSet, IEnumerable<TransitionAction>>>>();

            var spard = "($sign :: '+|'-)($var :: .&!'\\|'\\.|'([lazy](.+)'))($state :: <d>+)('{($a :: [lazy](.+))'})? => $sign $var $state $a";
            var extractor = TreeTransformer.Create(spard);
            extractor.Mode = TransformMode.Function;

            var actionsSpard = "($a :: [lazy](.+)((.&!'\\)|('\\'\\)))(';|%) => $a";
            var actionsExtractor = TreeTransformer.Create(actionsSpard);
            actionsExtractor.Mode = TransformMode.Function;

            var currentState = -1;

            using (var reader = new System.IO.StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    currentState++;

#if DEBUG
                    Debug.WriteLine(currentState);
#endif

                    TransitionStateBase state;
                    List<Tuple<int, object, IEnumerable<TransitionAction>>> links;
                    List<Tuple<int, InputSet, IEnumerable<TransitionAction>>> links2;
                    if (line[0] == '=')
                    {
                        var expression = Expressions.ExpressionBuilder.Parse(line.Substring(1), false) ?? Expressions.Empty.Instance;

                        state = new FinalTransitionState(expression);
                        states.Add(state);
                    }
                    else if (line[0] == '+' || line[0] == '-')
                    {
                        state = new TransitionState();
                        states.Add(state);

                        foreach (TupleValue match in extractor.Transform(line))
                        {
                            var include = match.Items[0].ToString() == "+";
                            var values = InputSet.Unescape(match.Items[1].ToString());
                            var nextState = int.Parse(match.Items[2].ToString());

                            var actions = new List<TransitionAction>();

                            if (match.Items[3] != BindingManager.UnsetValue)
                            {
                                var actionString = match.Items[3].ToString();

                                foreach (IEnumerable<object> actionsMatch in actionsExtractor.StepTransform(actionString))
                                {
                                    var actionR = new string(actionsMatch.Cast<char>().ToArray());
                                    TransitionAction action = null;
                                    switch (actionR[0])
                                    {
                                        case 'a':
                                            {
                                                var args = actionR.Split(',');
                                                action = new AppendVarAction(int.Parse(args[0].Substring(1)), args[1], string.IsNullOrWhiteSpace(args[2]) ? null : args[3]);
                                                break;
                                            }

                                        case 'c':
                                            {
                                                var args = actionR.Split(',');
                                                action = new CopyVarAction(int.Parse(args[0].Substring(1)), args[1], args[2]);
                                                break;
                                            }

                                        case 'n':
                                            {
                                                var args = actionR.Split(',');
                                                action = new RenameVarAction(args[0].Substring(1), args[1]);
                                                break;
                                            }

                                        case 'i':
                                            {
                                                var args = actionR.Split(',');
                                                Expressions.Expression expression;
                                                if (args.Length == 1)
                                                    expression = null;
                                                else
                                                    expression = Expressions.ExpressionBuilder.Parse(args[1], false);

                                                action = new InsertResultAction(expression, int.Parse(args[0].Substring(1)));
                                                break;
                                            }

                                        case 'r':
                                            {
                                                action = new ReturnResultAction(int.Parse(actionR.Substring(1)));
                                                break;
                                            }
                                    }

                                    if (action != null)
                                        actions.Add(action);
                                }
                            }

                            if (values.Length == 1 && include)
                            {
                                object value = values[0];

                                if (object.Equals(value, '\0'))
                                    value = InputSet.EndOfSource;

                                if (nextState > currentState)
                                {
                                    if (!futureLinks.TryGetValue(nextState, out links))
                                    {
                                        futureLinks[nextState] = links = new List<Tuple<int, object, IEnumerable<TransitionAction>>>();
                                    }                                   

                                    links.Add(Tuple.Create(currentState, (object)value, (IEnumerable<TransitionAction>)actions));
                                }
                                else
                                {
                                    ((TransitionState)state).Table[value] = new TransitionLink(states[nextState], actions);
                                }
                            }
                            else
                            {
                                var inputSetType = include ? InputSetType.Include : InputSetType.Exclude;

                                var valString = values.Length == 1 ? values[0].ToString() : values.Substring(1, values.Length - 2);
                                object[] valsArray = valString.Cast<object>().ToArray();
                                for (int i = 0; i < valsArray.Length; i++)
                                {
                                    if (object.Equals(valsArray[i], '\0'))
                                        valsArray[i] = InputSet.EndOfSource;
                                }

                                var input = new InputSet(inputSetType, valsArray);
                                if (nextState > currentState)
                                {
                                    if (!futureLinks2.TryGetValue(nextState, out links2))
                                    {
                                        futureLinks2[nextState] = links2 = new List<Tuple<int, InputSet, IEnumerable<TransitionAction>>>();
                                    }

                                    links2.Add(Tuple.Create(currentState, input, (IEnumerable<TransitionAction>)actions));
                                }
                                else
                                {
                                    ((TransitionState)state).SecondTable.Add(Tuple.Create(input, new TransitionLink(states[nextState], actions)));
                                }
                            }
                        }
                    }
                    else
                    {
                        state = new BadTransitionState(int.Parse(line.Substring(1)));
                        states.Add(state);
                    }

                    if (futureLinks.TryGetValue(currentState, out links))
                    {
                        foreach (var item in links)
                        {
                            ((TransitionState)states[item.Item1]).Table[item.Item2] = new TransitionLink(state, item.Item3);
                        }
                    }

                    if (futureLinks2.TryGetValue(currentState, out links2))
                    {
                        foreach (var item in links2)
                        {
                            ((TransitionState)states[item.Item1]).SecondTable.Add(Tuple.Create(item.Item2, new TransitionLink(state, item.Item3)));
                        }
                    }
                }
            }

            return states.Count > 0 ? states[0] : null;
        }
    }
}
