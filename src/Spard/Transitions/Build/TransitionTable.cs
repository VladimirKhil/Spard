using Spard.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Spard.Transitions
{
    /// <summary>
    /// Таблица переходов: для каждого условия на входной элемент указано выражение для проверки оставшихся элементов.
    /// Если выражение не задано, фиксируется успех.
    /// Если ни из условий не подошло, фиксируется неуспех сопоставления.
    /// Ключ - условие, выполняемое на текущем одном входном элементе. 
    /// Все ключи должны быть взаимоисключающими
    /// </summary>
    internal sealed class TransitionTable : Dictionary<InputSet, TransitionTableResultCollection>
    {
        /// <summary>
        /// Клонировать таблицу переходов
        /// </summary>
        /// <returns></returns>
        internal TransitionTable CloneTable()
        {
            var newTable = new TransitionTable();

            foreach (var item in this)
            {
                newTable[item.Key] = item.Value.CloneCollection();
            }

            return newTable;
        }

        /// <summary>
        /// Осуществляет слияние таблиц перехода
        /// </summary>
        /// <param name="tables">Таблицы перехода</param>
        /// <returns>Общая таблица перехода</returns>
        internal static TransitionTable Join(TransitionTable[] tables)
        {
            if (tables.Length == 0)
                return new TransitionTable();

            if (tables.Length == 1)
                return tables[0];

            var result = new TransitionTable();
            TransitionTableResultCollection collection;

            // Порядок следования таблиц важен

            foreach (var table in tables)
            {
                foreach (var item in table)
                {
                    var activeKey = item.Key; // Допускаемое множество, которое при движении вдоль ключей результата будет уменьшаться

                    foreach (var resItem in result.ToArray())
                    {
                        // Сравниваем с имеющимся ключом. Есть три части: (1) activeKey ^ resItem, (2) resItem - activeKey, (3) activeKey - resItem
                        var intersect = resItem.Key.Intersect(activeKey);

                        // Если (1) пусто, движемся дальше (нет пересечения)
                        if (intersect.IsEmpty)
                            continue;

                        collection = resItem.Value;

                        var otherPartKey = resItem.Key.Except(activeKey);

                        // Отщепляем (2) в отдельную запись таблицы
                        if (!otherPartKey.IsEmpty)
                        {
                            result.Remove(resItem.Key);
                            result[otherPartKey] = collection.CloneCollection();
                            result[intersect] = collection;
                        }

                        // Уменьшаем activeKey и движемся дальше
                        activeKey = activeKey.Except(resItem.Key);

                        // Выполняем слияние:

                        // Отфильтровывает из таблицы лишние записи (те, которые стоят после записей с результатом и поэтому недостижимы)
                        if (collection.Count > 0 && collection.Last().IsResult) // важно: IsResult, а не IsFinished
                        {
                            if (activeKey.IsEmpty)
                                break;

                            continue;
                        }

                        foreach (var subItem in item.Value)
                        {
                            if (collection.Contains(subItem))
                                continue;

                            collection.Add(subItem.CloneResult());

                            if (subItem.IsResult) // важно: IsResult, а не IsFinished
                                break; // Последующие записи недостижимы
                        }

                        if (activeKey.IsEmpty)
                            break;
                    }

                    // Если сохранилось хоть что-то, занесём это в словарь
                    if (!activeKey.IsEmpty)
                    {
                        result[activeKey] = collection = new TransitionTableResultCollection();
                        
                        foreach (var subItem in item.Value)
                        {
                            if (collection.Contains(subItem))
                                continue;

                            collection.Add(subItem);
                            if (subItem.IsResult)
                                break; // Последующие записи недостижимы
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Осуществляет перечение таблиц перехода
        /// </summary>
        /// <param name="tables">Таблицы перехода</param>
        /// <returns>Общая таблица перехода</returns>
        internal static TransitionTable Intersect(TransitionTable[] tables)
        {
            if (tables.Length == 0)
                return new TransitionTable();

            if (tables.Length == 1)
                return tables[0];

            var result = new TransitionTable();

            // Построим объединение методом динамического программирования
            var currentRow = new List<Tuple<InputSet, List<TransitionTableResult[]>>>();
            
            foreach (var item in tables[0])
            {
                currentRow.Add(Tuple.Create(item.Key, item.Value.Select(ttr => new TransitionTableResult[] { ttr }).ToList()));
            }

            // Порядок следования таблиц важен

            for (int i = 1; i < tables.Length; i++)
            {
                var nextRow = new List<Tuple<InputSet, List<TransitionTableResult[]>>>();

                foreach (var cell in currentRow)
                {
                    foreach (var item in tables[i]) // порядок foreach'ей важен
                    {
                        var key = item.Key.Intersect(cell.Item1);
                        if (key.IsEmpty)
                            continue;

                        var newList = new List<TransitionTableResult[]>();

                        // Ключи сошлись, теперь вычислим декартово сопряжение списков
                        foreach (var val in item.Value)
                        {
                            foreach (var tuple in cell.Item2)
                            {
                                if (tuple.Any(expr => IsOpposite(expr, val)))
                                    continue;

                                var newItem = val;

                                //if (tuple[0] == TransitionTableResult.Empty && newItem != TransitionTableResult.Empty)
                                //{
                                //    // Попробуем проверить, есть ли нулевой переход
                                //    var table = newItem.Expression.BuildTransitionTable(isLast);
                                //    TransitionTableResultCollection zeroCollection;
                                //    if (!table.table.TryGetValue(InputSet.Zero, out zeroCollection))
                                //        continue;

                                //    //newItem = zeroCollection[0];
                                //}
                                //else if (tuple.All(t => t != TransitionTableResult.Empty) && newItem == TransitionTableResult.Empty)
                                //{
                                //    // А здесь придётся проверить всех уже пройденных
                                //    //var newExprList2 = new TransitionTableResult[i + 1];
                                //    var good = true;
                                //    for (int j = 0; j < i; j++)
                                //    {
                                //        var table = tuple[j].Expression.BuildTransitionTable(isLast);
                                //        TransitionTableResultCollection zeroCollection;
                                //        if (!table.table.TryGetValue(InputSet.Zero, out zeroCollection))
                                //        {
                                //            good = false;
                                //            break;
                                //        }

                                //        //newExprList2[j] = zeroCollection[0];
                                //    }

                                //    if (!good)
                                //        continue;

                                //    //newExprList2[i] = newItem.CloneResult();
                                //    //newList.Add(newExprList2);
                                //    //continue;
                                //}

                                var newExprList = new TransitionTableResult[i + 1];

                                for (int j = 0; j < tuple.Length; j++)
                                {
                                    newExprList[j] = tuple[j].CloneResult();
                                }

                                newExprList[i] = newItem.CloneResult();
                                newList.Add(newExprList);
                            }
                        }

                        if (newList.Any())
                            nextRow.Add(Tuple.Create(key, newList));
                    }
                }

                currentRow = nextRow;
            }

            foreach (var item in currentRow)
            {
                var resultCollection = new TransitionTableResultCollection();

                foreach (var exprList in item.Item2)
                {
                    var hasEmpty = exprList.Any(ttr => ttr.IsFinished);
                    var nonEmpty = exprList.Where(ttr => !ttr.IsFinished).Select(
                        ttr => hasEmpty ? new ZeroMoveProxy(ttr.Expression) : ttr.Expression
                    ).ToArray();

                    if (nonEmpty.Length == 0)
                    {
                        resultCollection.Add(TransitionTableResult.Empty.CloneResult());
                    }
                    else if (nonEmpty.Length == 1)
                    {
                        resultCollection.Add(new TransitionTableResult(nonEmpty[0]));
                    }
                    else
                    {
                        var multiZeros = nonEmpty.Where(expr =>
                        {
                            return (hasEmpty ? ((ZeroMoveProxy)expr).Operand : expr) is MultiTime multiTime && multiTime.reversed;
                        }).ToArray();

                        if (multiZeros.Length < nonEmpty.Length - 1)
                        {
                            resultCollection.Add(new TransitionTableResult(new And(nonEmpty)));
                        }
                        else if (multiZeros.Length == nonEmpty.Length - 1)
                        {
                            resultCollection.Add(new TransitionTableResult(nonEmpty.Except(multiZeros).First()));
                        }
                        else // все .*
                        {
                            if (hasEmpty)
                                resultCollection.Add(TransitionTableResult.Empty.CloneResult());
                            else
                                resultCollection.Add(new TransitionTableResult(nonEmpty.First()));
                        }
                    }
                }

                result[item.Item1] = resultCollection;
            }

            return result;
        }

        private static bool IsOpposite(TransitionTableResult expr, TransitionTableResult val)
        {
            if (expr.IsResult || val.IsResult || expr.Expression == null || val.Expression == null)
                return false;

            return IsOpposite(expr.Expression, val.Expression);
        }

        private static bool IsOpposite(Expression a, Expression b)
        {
            if (a is And and)
            {
                return and._operands.Any(op => IsOpposite(op, b));
            }

            and = b as And;
            if (and != null)
            {
                return and._operands.Any(op => IsOpposite(a, op));
            }

            var not2 = b as Not;
            if (a is Not not)
            {
                if (not2 != null)
                    return IsOpposite(not.Operand, not2.Operand);

                return not.Operand.ToString() == b.ToString();
            }

            if (not2 != null)
                return not2.Operand.ToString() == a.ToString();

            return false;
        }

        /// <summary>
        /// Задать список всех использованных в таблице имён переменных
        /// </summary>
        internal void SetUsedVars()
        {
            foreach (var collection in Values)
            {
                collection.SetUsedVars();
            }
        }
    }
}
