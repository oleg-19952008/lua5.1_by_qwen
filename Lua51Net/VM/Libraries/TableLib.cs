using System;
using Lua51Net.Core;

namespace Lua51Net.VM.Libraries
{
    /// <summary>
    /// Библиотека table для Lua 5.1.4
    /// </summary>
    public static class TableLib
    {
        public static void Register(LuaState state)
        {
            // Создаем таблицу table
            state.NewTable();
            
            // Регистрируем функции
            RegisterFunction(state, "concat", Concat);
            RegisterFunction(state, "insert", Insert);
            RegisterFunction(state, "remove", Remove);
            RegisterFunction(state, "sort", Sort);
            RegisterFunction(state, "maxn", Maxn);
            RegisterFunction(state, "foreach", Foreach);
            RegisterFunction(state, "foreachi", Foreachi);
            RegisterFunction(state, "getn", Getn);
            
            // Устанавливаем в global
            state.SetGlobal("table");
        }

        private static void RegisterFunction(LuaState state, string name, LuaNativeFunction func)
        {
            state.PushCFunction(func);
            state.SetField(-2, name);
        }

        // table.concat(table [, sep [, i [, j]]])
        private static int Concat(LuaState state)
        {
            LuaTable table = state.ToTable(1);
            if (table == null)
            {
                state.PushString("");
                return 1;
            }
            
            string sep = "";
            if (state.GetTop() >= 2)
            {
                sep = state.ToString(2) ?? "";
            }
            
            int i = 1;
            if (state.GetTop() >= 3)
            {
                i = (int)state.ToNumber(3);
            }
            
            int j = table.Length;
            if (state.GetTop() >= 4)
            {
                j = (int)state.ToNumber(4);
            }
            
            if (i > j)
            {
                state.PushString("");
                return 1;
            }
            
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int k = i; k <= j; k++)
            {
                if (k > i)
                    sb.Append(sep);
                
                LuaValue val = table[LuaValue.CreateNumber(k)];
                if (val.Type != LuaType.LUA_TSTRING && val.Type != LuaType.LUA_TNUMBER)
                {
                    state.PushString("invalid value (")
                        .Append(val.Type.ToString())
                        .Append(") at index ")
                        .Append(k)
                        .Append(" in table for concat");
                    return state.Error();
                }
                sb.Append(val.ToStringValue());
            }
            
            state.PushString(sb.ToString());
            return 1;
        }

        // table.insert(table, [pos,] value)
        private static int Insert(LuaState state)
        {
            LuaTable table = state.ToTable(1);
            if (table == null)
            {
                return 0;
            }
            
            int pos;
            LuaValue value;
            
            if (state.GetTop() == 2)
            {
                // insert(table, value) - вставка в конец
                pos = table.Length + 1;
                value = state.Get(2);
            }
            else
            {
                // insert(table, pos, value)
                pos = (int)state.ToNumber(2);
                value = state.Get(3);
            }
            
            // Сдвигаем элементы
            for (int k = table.Length + 1; k > pos; k--)
            {
                table[LuaValue.CreateNumber(k)] = table[LuaValue.CreateNumber(k - 1)];
            }
            
            table[LuaValue.CreateNumber(pos)] = value;
            return 0;
        }

        // table.remove(table [, pos])
        private static int Remove(LuaState state)
        {
            LuaTable table = state.ToTable(1);
            if (table == null)
            {
                state.PushNil();
                return 1;
            }
            
            int pos = table.Length;
            if (state.GetTop() >= 2)
            {
                pos = (int)state.ToNumber(2);
            }
            
            if (pos < 1 || pos > table.Length)
            {
                state.PushNil();
                return 1;
            }
            
            // Получаем значение для возврата
            LuaValue retValue = table[LuaValue.CreateNumber(pos)];
            
            // Сдвигаем элементы
            for (int k = pos; k < table.Length; k++)
            {
                table[LuaValue.CreateNumber(k)] = table[LuaValue.CreateNumber(k + 1)];
            }
            
            // Очищаем последний элемент
            table[LuaValue.CreateNumber(table.Length)] = LuaValue.Nil;
            
            state.Push(retValue);
            return 1;
        }

        // table.sort(table [, comp])
        private static int Sort(LuaState state)
        {
            LuaTable table = state.ToTable(1);
            if (table == null)
            {
                return 0;
            }
            
            int len = table.Length;
            if (len <= 1)
            {
                return 0;
            }
            
            // Получаем компаратор (если есть)
            LuaFunction comp = null;
            if (state.GetTop() >= 2 && state.IsFunction(2))
            {
                comp = state.ToFunction(2);
            }
            
            // Копируем значения в массив для сортировки
            LuaValue[] values = new LuaValue[len];
            for (int i = 0; i < len; i++)
            {
                values[i] = table[LuaValue.CreateNumber(i + 1)];
            }
            
            // Сортировка с использованием компаратора
            Array.Sort(values, (a, b) =>
            {
                if (comp != null)
                {
                    // Вызов пользовательского компаратора
                    state.PushFunction(comp);
                    state.Push(a);
                    state.Push(b);
                    state.Call(2, 1);
                    bool result = state.ToBoolean(-1);
                    state.Pop();
                    
                    // В Lua компаратор должен возвращать true если a < b
                    // Если false, то a >= b
                    return result ? -1 : (a.Equals(b) ? 0 : 1);
                }
                else
                {
                    // Сравнение по умолчанию (как в Lua 5.1)
                    if (a.Type == LuaType.LUA_TNUMBER && b.Type == LuaType.LUA_TNUMBER)
                    {
                        double na = a.ToNumber();
                        double nb = b.ToNumber();
                        return na.CompareTo(nb);
                    }
                    else if (a.Type == LuaType.LUA_TSTRING && b.Type == LuaType.LUA_TSTRING)
                    {
                        string sa = a.ToStringValue();
                        string sb = b.ToStringValue();
                        return string.Compare(sa, sb, StringComparison.Ordinal);
                    }
                    else
                    {
                        // Разные типы - сравниваем по строковому представлению
                        string sa = a.ToStringValue();
                        string sb = b.ToStringValue();
                        return string.Compare(sa, sb, StringComparison.Ordinal);
                    }
                }
            });
            
            // Записываем отсортированные значения обратно
            for (int i = 0; i < len; i++)
            {
                table[LuaValue.CreateNumber(i + 1)] = values[i];
            }
            
            return 0;
        }

        // table.maxn(table)
        private static int Maxn(LuaState state)
        {
            LuaTable table = state.ToTable(1);
            if (table == null)
            {
                state.PushNumber(0);
                return 1;
            }
            
            // maxn возвращает максимальный числовой ключ (не обязательно длину)
            int maxN = 0;
            // Проверяем array part
            maxN = table.Length;
            
            state.PushNumber(maxN);
            return 1;
        }

        // table.foreach(table, f)
        private static int Foreach(LuaState state)
        {
            LuaTable table = state.ToTable(1);
            if (table == null)
            {
                return 0;
            }
            
            LuaFunction func = state.ToFunction(2);
            if (func == null)
            {
                return 0;
            }
            
            // Итерация по всем элементам (упрощенно только по array part)
            int len = table.Length;
            for (int i = 1; i <= len; i++)
            {
                LuaValue key = LuaValue.CreateNumber(i);
                LuaValue val = table[key];
                
                state.PushFunction(func);
                state.Push(key);
                state.Push(val);
                state.Call(2, 1);
                
                // Если функция вернула true, прекращаем итерацию
                if (state.ToBoolean(-1))
                {
                    state.Pop();
                    break;
                }
                state.Pop();
            }
            
            return 0;
        }

        // table.foreachi(table, f)
        private static int Foreachi(LuaState state)
        {
            LuaTable table = state.ToTable(1);
            if (table == null)
            {
                return 0;
            }
            
            LuaFunction func = state.ToFunction(2);
            if (func == null)
            {
                return 0;
            }
            
            // Итерация только по числовым ключам
            int len = table.Length;
            for (int i = 1; i <= len; i++)
            {
                LuaValue val = table[LuaValue.CreateNumber(i)];
                
                state.PushFunction(func);
                state.PushNumber(i);
                state.Push(val);
                state.Call(2, 1);
                
                // Если функция вернула true, прекращаем итерацию
                if (state.ToBoolean(-1))
                {
                    state.Pop();
                    break;
                }
                state.Pop();
            }
            
            return 0;
        }

        // table.getn(table)
        private static int Getn(LuaState state)
        {
            LuaTable table = state.ToTable(1);
            if (table == null)
            {
                state.PushNumber(0);
                return 1;
            }
            
            state.PushNumber(table.Length);
            return 1;
        }
    }
}
