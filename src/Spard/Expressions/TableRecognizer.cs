using Spard.Core;
using Spard.Sources;
using Spard.Transitions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Spard.Expressions
{
    /// <summary>
    /// Tabular Recognizer.
    /// It is embedded in the expression tree
    /// </summary>
    public sealed class TableRecognizer: Primitive
    {
        private Expression _origin;
        private TransitionStateBase _initialState;

        private int _initStart, _currentPosition;
        private TransitionStateBase _currentState;
        private TransitionContext _context;

        private IContext _initContext;

        private bool _isFinished = false;
        private List<CachedResult> _cachedResults = new List<CachedResult>();
        private int _cacheIndex = -1;

        /// <summary>
        /// Saved successful parsing state
        /// </summary>
        private sealed class CachedResult
        {
            /// <summary>
            /// Input position
            /// </summary>
            public int InputPosition { get; set; }
            /// <summary>
            /// Variables lengths
            /// </summary>
            public Dictionary<string, int> VariablesLength { get; set; } = new Dictionary<string, int>();
        }

        protected internal override string Sign
        {
            get { return ""; }
        }

        internal TableRecognizer(Expression origin)
        {
            this._origin = origin;
        }

        public static TableRecognizer Create(Expression origin)
        {
            var recognizer = new TableRecognizer(origin);

            recognizer.Build(null);

            return recognizer;
        }

        internal void Build(IExpressionRoot root)
        {
            var collection = TransitionTableResultCollection.Create(_origin);
            _initialState = TransitionGraphBuilder.Create(collection, root, true);
        }

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            if (next && _isFinished)
            {
                if (_cacheIndex > -1)
                {
                    SetResult(input, ref context);
                    return true;
                }

                return false;
            }

            if (!next)
            {
                _initStart = _currentPosition = input.Position;
                _initContext = context;

                _currentState = _initialState;
                this._context = new TransitionContext();

                _isFinished = false;
                _cachedResults.Clear();
                _cacheIndex = -1;

                // There may be an intermediate result for the initial state
                var resultIndex = _currentState.IntermediateResultIndex;
                if (resultIndex == -1)
                {
                    _currentPosition = input.Position;
                    return true;
                }
                else if (resultIndex > -1)
                {
                    // Saving result
                    var cachedResult = new CachedResult { InputPosition = input.Position };

                    foreach (var item in this._context.Vars)
                    {
                        cachedResult.VariablesLength[item.Key] = item.Value.Count;
                    }

                    _cachedResults.Insert(resultIndex, cachedResult);
                }
            }
            else
                input.Position = _currentPosition;

            if (input.EndOfSource)
                return false;

            do
            {
                var c = input.Read();
                var newState = _currentState.Move(c, ref this._context, out IEnumerable result);

                if (newState == null)
                {
                    _isFinished = true;
                    if (_cachedResults.Count > 0)
                    {
                        _cacheIndex = _cachedResults.Count - 1;
                        SetResult(input, ref context);
                        return true; // We recognized some fragment and it’s good - that’s what we needed.
                    }

                    input.Position = _initStart;
                    return false;
                }

                _currentState = newState;

                var resultIndex = _currentState.IntermediateResultIndex + this._context.ResultIndexIncrease;
                if (resultIndex == -1)
                {
                    _currentPosition = input.Position;

                    context = _initContext.Clone();

                    foreach (var item in this._context.Vars)
                    {
                        context.Vars[item.Key] = item.Value;
                    }

                    return true;
                }
                else if (resultIndex > -1)
                {
                    // Saving result
                    var cachedResult = new CachedResult { InputPosition = input.Position };

                    foreach (var item in this._context.Vars)
                    {
                        cachedResult.VariablesLength[item.Key] = item.Value.Count;
                    }

                    _cachedResults.Insert(resultIndex, cachedResult);
                }

            } while (!input.EndOfSource);

            _isFinished = true;
            if (_cachedResults.Count > 0)
            {
                _cacheIndex = _cachedResults.Count - 1;
                SetResult(input, ref context);
                return true; // We recognized some fragment and it’s good - that’s what we needed
            }

            input.Position = _initStart;
            return false;
        }

        private void SetResult(ISource input, ref IContext context)
        {
            var cachedResult = _cachedResults[_cacheIndex--];
            input.Position = cachedResult.InputPosition;

            context = _initContext.Clone();

            foreach (var item in cachedResult.VariablesLength)
            {
                context.SetValue(item.Key, this._context.Vars[item.Key].Take(item.Value));
            }
        }

        internal override object Apply(IContext context)
        {
            return _origin.Apply(context);
        }

        public override Expression CloneCore()
        {
            return new TableRecognizer(_origin) { _initialState = _initialState };
        }

        public override string ToString()
        {
            return "[>>> " + _origin.ToString() + "]";
        }
    }
}
