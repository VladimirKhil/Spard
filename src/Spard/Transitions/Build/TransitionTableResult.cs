using System;
using System.Collections.Generic;
using Spard.Expressions;

namespace Spard.Transitions
{
    /// <summary>
    /// Представление результата перехода в таблице переходов
    /// </summary>
    internal sealed class TransitionTableResult: IEquatable<TransitionTableResult>
    {
        /// <summary>
        /// Пустой результат (успех без возвращения какого-либо значения)
        /// Важно: IsResult = false
        /// </summary>
        internal static TransitionTableResult Empty = new TransitionTableResult(null);

        /// <summary>
        /// Выражение для проверки оставшейся части входа (если IsResult = false) или для построения результата в противном случае
        /// </summary>
        internal Expression Expression { get; private set; }
        
        /// <summary>
        /// Является ли Expression результатом (в противном случае - предикатом)
        /// </summary>
        internal bool IsResult { get; private set; }
        
        /// <summary>
        /// Изменения контекста
        /// </summary>
        internal ContextChange ContextChange { get; private set; }

        /// <summary>
        /// Модификации графа состояний
        /// </summary>
        /// <remarks>
        /// По мере считывания входа и движения по состояниям мы можем получать промежуточный результат
        /// Он всегда один и всегда последний (так как он закрывает все нижележащие правила)
        /// Если выше него есть незаконченные правила, то мы не можем знать наверняка, реализуется ли этот результат вообще
        /// в одном из ветвлений состояния (хоть где-то)
        /// Поэтому мы запоминаем место, где должен быть сохранён промежуточный результат
        /// И если встречается состояние, в котором этот результат всплывает на поверхность, мы реализуем вставку
        /// </remarks>
        internal ModificationsList Modifications { get; set; }
        
        /// <summary>
        /// Результат нулевого сдвига
        /// </summary>
        internal Expression ZeroMoveResult { get; set; }

        internal int ZeroStop { get; set; }

        /// <summary>
        /// Имена всех использованных в выражении переменных
        /// </summary>
        internal string[] UsedVars { get; set; }

        internal bool IsFinished
        {
            get { return Expression == null; }
        }

        /// <summary>
        /// Накапливаемое значение индекса промежуточного результата
        /// </summary>
        public int IntermediateResultIndex { get; internal set; }

        public TransitionTableResult(Expression expression, bool isResult = false, ContextChange contextChange = null)
        {
            Expression = expression;
            IsResult = isResult;
            ContextChange = contextChange;
        }

        public ModificationsList GetInsertionPoints()
        {
            return Modifications;
        }

        public override string ToString()
        {
            if (Expression == null)
                return "";

            return Expression.ToString();
        }

        internal TransitionTableResult CloneResult()
        {
            //if (this == Empty)
            //    return this;

            return new TransitionTableResult(Expression, IsResult)
            {
                ContextChange = ContextChange,
                Modifications = Modifications?.Clone(),
                ZeroMoveResult = ZeroMoveResult,
                ZeroStop = ZeroStop,
                UsedVars = UsedVars,
                IntermediateResultIndex = IntermediateResultIndex
            };
        }

        public override bool Equals(object obj)
        {
            if (obj is TransitionTableResult other)
                return Equals(other);

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            var hash = ZeroStop * 31;

            hash += Expression != null ? Expression.GetHashCode() : 0;

            return hash;
        }

        public bool Equals(TransitionTableResult other)
        {
            if (this == other)
                return true;

            if (ZeroStop != other.ZeroStop)
                return false;

            if (Expression == other.Expression)
                return true;

            if (Expression == null || other.Expression == null)
                return false;

            return Expression.Equals(other.Expression);
        }

        internal bool EqualsSmart(TransitionTableResult other, Dictionary<string, string> varsMap)
        {
            if (this == other)
                return true;

            if (ZeroStop != other.ZeroStop)
                return false;

            if (Expression == other.Expression)
                return true;

            if (Expression == null || other.Expression == null)
                return false;

            return Expression.EqualsSmart(other.Expression, varsMap);
        }

        internal void CalculateUsedVars()
        {
            var usedVars = new List<string>();
            Expression.Traverse(usedVars);
            UsedVars = usedVars.ToArray();
        }

        internal void RenameVar(string oldName, string newName)
        {
            Expression = Expression.RenameVar(oldName, new Query(new StringValueMatch(newName)));

            var newUsedNames = new List<string>(UsedVars);
            newUsedNames.Remove(oldName);
            newUsedNames.Add(newName);

            UsedVars = newUsedNames.ToArray();
        }
    }
}
