using System.Collections.Generic;
using System.Linq;
using Spard.Sources;
using Spard.Expressions;
using Spard.Core;

namespace Spard.Common
{
    /// <summary>
    /// Object which is keeping multiple matches
    /// </summary>
    internal sealed class MultiMatchManager
    {
        private IContext _initContext = null;

        /// <summary>
        /// Variable 'next' which is used in inner calls
        /// </summary>
        private bool _innerNext = false;
        /// <summary>
        /// Cached matches (kept for speed)
        /// </summary>
        private List<MultiMatch> _matches = new List<MultiMatch>();
        /// <summary>
        /// Index in _matches
        /// </summary>
        private int _count = 0;
        /// <summary>
        /// Current match offset
        /// </summary>
        private int index = 0;

        /// <summary>
        /// Variant with zero matches
        /// </summary>
        private bool zeroTurn = false;

        private List<string> collectVars = null;

        public MultiMatchManager()
        {
            
        }

        internal delegate bool CountChecker(int count, ref IContext context, bool next);

        private int CompareMatches(MultiMatch match1, MultiMatch match2)
        {
            var eq = -match1.Count.CompareTo(match2.Count);
            return eq != 0 ? eq : -match1.Position.CompareTo(match2.Position);
        }

        internal bool Match(Expression operand, ISource input, ref IContext context, bool next, CountChecker countFilter = null)
        {
            IContext workingContext = null;
            var initStart = input.Position;
            if (!next)
            {
                _initContext = context; // If the match is successful, we will still rewrite the context. Otherwise the value of initContext doesn’t bother us much
                workingContext = context.Clone();
                
                _count = -1;
                index = 1;
                _matches.Clear();
                zeroTurn = true;

                if (!context.GetParameter(Parameters.IsLazy))
                {
                    var startState = input.Position;


                    if (workingContext.Vars.TryGetValue("arg_Collect", out object val))
                    {
                        workingContext.Vars.Remove("arg_Collect"); // This should not work on lower levels
                        collectVars = (List<string>)val;
                    }

                    // A "greedy" comparison algorithm is used, so first we have to produce the longest match option first
                    // To get it, you need to go through short matches
                    // Therefore, it is worth caching them all in order to facilitate the further search of matches from long to short
                    do
                    {
                        next = false;

                        if (_count > -1)
                        {
                            // Start from previous match
                            index = _matches[_count].Count + 1;
                            workingContext = _matches[_count].Context.Clone();
                            input.Position = _matches[_count].Position;
                        }

                        // Each stage of the match can produce several results; we must investigate them all
                        // Variable _count is responsible for a sequential enumeration of options, guaranteeing the examination of each
                        while (operand.Match(input, ref workingContext, next))
                        {
                            if (collectVars != null) // Collecting variable value
                            {
                                foreach (var collectVar in collectVars)
                                {
                                    if (workingContext.Vars.TryGetValue(collectVar, out val))
                                    {
										var key = "_collect_" + collectVar;
										List<object> capture;
                                        if (!workingContext.Vars.TryGetValue(key, out object collectVal))
                                            capture = new List<object>();
                                        else
                                            capture = new List<object>(new object[] { collectVal });

                                        capture.Add(val);

                                        collectVal = capture;
                                        workingContext.Vars[key] = collectVal;
                                        
                                        workingContext.Vars.Remove(collectVar);
                                    }
                                }
                            }

                            next = true;
                            var moved = _count == -1 || !input.Position.Equals(_matches[_count].Position);
                            if (!_matches.Any(match => match.Position.Equals(input.Position) && match.Context.Equals(workingContext) && (match.Count == index || !moved)))
                            {
                                _matches.Add(new MultiMatch() { Count = index, Context = workingContext, Position = input.Position });
                            }

                            if (_count > -1)
                                input.Position = _matches[_count].Position;
                            else
                                input.Position = startState;
                        }

                        _count++;
                    } while (_count < _matches.Count);

                    _matches.Add(new MultiMatch { Count = 0, Context = context, Position = startState });

                    _count = 0;
                    // Sort matches to process them in decreasing order of length
                    _matches.Sort(CompareMatches);
                }
                else
                {
                    _innerNext = false;
                }
            }
            else if (_count == -2)
                return false;
            else
            {
                workingContext = _initContext;
            }

            if (!context.GetParameter(Parameters.IsLazy))
            {
                if (countFilter != null)
                {
                    // Check if current match count is acceptable by predicate
                    while (_count < _matches.Count)
                    {
                        var cont = _matches[_count].Context;
                        if (countFilter(_matches[_count].Count, ref cont, false))
                        {
                            _matches[_count].Context = cont;
                            break;
                        }

                        _count++;
                    }
                }

                // Greedy search algorithm
                // _count counter guarantees the issuance of matches one by one
                if (_count < _matches.Count)
                {
                    context = _matches[_count].Context;
                    input.Position = _matches[_count].Position;
                    _count++;

                    if (collectVars != null)
                    {
                        foreach (var collectVar in collectVars)
                        {
                            var key = "_collect_" + collectVar;
                            if (context.Vars.TryGetValue(key, out object val))
                            {
                                context.Vars[collectVar] = val;
                                context.Vars.Remove(key);
                            }
                        }
                    }
                }
                else
                {
                    //if (this.zeroTurn && (countFilter == null || countFilter(0, ref workingContext, false))) // Если допускается пустое совпадение
                    //{
                    //    this.zeroTurn = false;
                    //    context = workingContext;// (IContext<T>)this.initContext;
                    //    input.Position = this.initStart;

                    //    return true;
                    //}

                    input.Position = initStart;
                    _count = -2; // Matches are finished

                    return false;
                }

                return true;
            }
            else
            {
                // Non-greedy (lazy) option. No match cache is used. Matches are calculated by necessity
                if (zeroTurn && (countFilter == null || countFilter(0, ref workingContext, false))) // If empty match is allowed
                {
                    zeroTurn = false;
                    context = workingContext;

                    return true;
                }

                if (_innerNext && _count > -1)
                {
                    workingContext = _matches[_count].Context;
                    input.Position = _matches[_count].Position;
                }
                
                while (true) // We look at matches in _matches until we find something worthwhile
                {
                    if (operand.Match(input, ref workingContext, _innerNext))
                    {
                        _innerNext = true;
                        _matches.Add(new MultiMatch() { Count = index, Context = workingContext.Clone(), Position = input.Position });

                        context = workingContext;
                        return true;
                    }

                    _count++;

                    if (countFilter != null)
                    {
                        // Check the current matches count for validity
                        while (_count < _matches.Count && !countFilter(_matches[_count].Count, ref workingContext, false))
                            _count++;
                    }

                    if (_count >= _matches.Count) // Are there any more matches that can be continued?
                        break;

                    _innerNext = false;
                    index = _matches[_count].Count + 1;
                    workingContext = _matches[_count].Context;
                    input.Position = _matches[_count].Position;
                }

                _count = -2;
                input.Position = initStart;
                return false;
            }
        }
    }
}
