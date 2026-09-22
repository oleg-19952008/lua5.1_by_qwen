using System;
using Lua51Net.Core;

namespace Lua51Net.VM.Libraries
{
    /// <summary>
    /// Базовая библиотека (base library) для Lua 5.1.4
    /// Реализует основные функции: print, tonumber, tostring, type, pairs, ipairs, next
    /// </summary>
    public static class BaseLib
    {
        public static void Register(LuaState state)
        {
            // print
            RegisterFunction(state, "print", Print);
            
            // tonumber
            RegisterFunction(state, "tonumber", ToNumber);
            
            // tostring
            RegisterFunction(state, "tostring", ToString);
            
            // type
            RegisterFunction(state, "type", Type);
            
            // pairs
            RegisterFunction(state, "pairs", Pairs);
            
            // ipairs
            RegisterFunction(state, "ipairs", IPairs);
            
            // next
            RegisterFunction(state, "next", Next);
            
            // assert
            RegisterFunction(state, "assert", Assert);
            
            // error
            RegisterFunction(state, "error", Error);
            
            // getmetatable
            RegisterFunction(state, "getmetatable", GetMetatable);
            
            // setmetatable
            RegisterFunction(state, "setmetatable", SetMetatable);
            
            // rawequal
            RegisterFunction(state, "rawequal", RawEqual);
            
            // rawget
            RegisterFunction(state, "rawget", RawGet);
            
            // rawset
            RegisterFunction(state, "rawset", RawSet);
            
            // collectgarbage
            RegisterFunction(state, "collectgarbage", CollectGarbage);
            
            // pcall
            RegisterFunction(state, "pcall", PCall);
            
            // xpcall
            RegisterFunction(state, "xpcall", XPCall);
            
            // select
            RegisterFunction(state, "select", Select);
            
            // unpack
            RegisterFunction(state, "unpack", Unpack);
            
            // loadstring
            RegisterFunction(state, "loadstring", LoadString);
            
            // load
            RegisterFunction(state, "load", Load);
            
            // getfenv
            RegisterFunction(state, "getfenv", GetFEnv);
            
            // setfenv
            RegisterFunction(state, "setfenv", SetFEnv);
            
            // _G (глобальная таблица)
            state.NewTable();
            state.SetGlobal("_G");
            
            // _VERSION
            state.PushString("Lua 5.1");
            state.SetGlobal("_VERSION");
        }

        private static void RegisterFunction(LuaState state, string name, LuaNativeFunction func)
        {
            state.PushCFunction(func);
            state.SetGlobal(name);
        }

        // print(...)
        private static int Print(LuaState state)
        {
            int n = state.GetTop();
            for (int i = 1; i <= n; i++)
            {
                LuaValue value = state.Get(i);
                if (value.Type == LuaType.LUA_TSTRING)
                {
                    Console.Write(value.ToStringValue());
                }
                else if (value.Type == LuaType.LUA_TNUMBER)
                {
                    Console.Write(value.ToNumber());
                }
                else if (value.Type == LuaType.LUA_TBOOLEAN)
                {
                    Console.Write(value.ToBoolean() ? "true" : "false");
                }
                else if (value.Type == LuaType.LUA_TNIL)
                {
                    Console.Write("nil");
                }
                else if (value.Type == LuaType.LUA_TTABLE)
                {
                    Console.Write($"table: {value.Value?.GetHashCode():X}");
                }
                else if (value.Type == LuaType.LUA_TFUNCTION)
                {
                    Console.Write($"function: {value.Value?.GetHashCode():X}");
                }
                else if (value.Type == LuaType.LUA_TTHREAD)
                {
                    Console.Write($"thread: {value.Value?.GetHashCode():X}");
                }
                else if (value.Type == LuaType.LUA_TUSERDATA)
                {
                    Console.Write($"userdata: {value.Value?.GetHashCode():X}");
                }
                else
                {
                    Console.Write(value.ToStringValue());
                }
                
                if (i < n)
                    Console.Write("\t");
            }
            Console.WriteLine();
            return 0;
        }

        // tonumber(e [, base])
        private static int ToNumber(LuaState state)
        {
            LuaValue value = state.Get(1);
            
            if (value.Type == LuaType.LUA_TNUMBER)
            {
                state.PushValue(1);
                return 1;
            }
            
            if (value.Type == LuaType.LUA_TSTRING)
            {
                string str = value.ToStringValue();
                int baseNum = 10;
                
                if (state.GetTop() >= 2 && state.IsNumber(2))
                {
                    baseNum = (int)state.ToNumber(2);
                }
                
                // Обработка разных оснований
                if (baseNum == 10)
                {
                    if (double.TryParse(str, out double num))
                    {
                        state.PushNumber(num);
                        return 1;
                    }
                }
                else if (baseNum == 16)
                {
                    // Шестнадцатеричное число
                    str = str.Trim();
                    if (str.StartsWith("0x") || str.StartsWith("0X"))
                    {
                        str = str.Substring(2);
                    }
                    if (int.TryParse(str, System.Globalization.NumberStyles.HexNumber, null, out int hexNum))
                    {
                        state.PushNumber(hexNum);
                        return 1;
                    }
                }
                else if (baseNum >= 2 && baseNum <= 36)
                {
                    // Другие основания
                    try
                    {
                        long parsedNum = Convert.ToInt64(str, baseNum);
                        state.PushNumber(parsedNum);
                        return 1;
                    }
                    catch
                    {
                        // Не удалось распарсить
                    }
                }
            }
            
            state.PushNil();
            return 1;
        }

        // tostring(v)
        private static int ToString(LuaState state)
        {
            LuaValue value = state.Get(1);
            string result = value.ToStringValue();
            state.PushString(result);
            return 1;
        }

        // type(v)
        private static int Type(LuaState state)
        {
            LuaType t = state.Type(1);
            string typeName = t switch
            {
                LuaType.LUA_TNIL => "nil",
                LuaType.LUA_TBOOLEAN => "boolean",
                LuaType.LUA_TLIGHTUSERDATA => "lightuserdata",
                LuaType.LUA_TNUMBER => "number",
                LuaType.LUA_TSTRING => "string",
                LuaType.LUA_TTABLE => "table",
                LuaType.LUA_TFUNCTION => "function",
                LuaType.LUA_TUSERDATA => "userdata",
                LuaType.LUA_TTHREAD => "thread",
                _ => "unknown"
            };
            state.PushString(typeName);
            return 1;
        }

        // pairs(t)
        private static int Pairs(LuaState state)
        {
            state.PushCFunction(Next);
            state.PushValue(1);
            state.PushNil();
            return 3;
        }

        // ipairs(t)
        private static int IPairs(LuaState state)
        {
            state.PushCFunction(IPairsIterator);
            state.PushValue(1);
            state.PushNumber(0);
            return 3;
        }

        private static int IPairsIterator(LuaState state)
        {
            LuaTable table = state.ToTable(1);
            int i = (int)state.ToNumber(2);
            i++;
            
            LuaValue key = LuaValue.CreateNumber(i);
            LuaValue value = table[key];
            
            if (value.Type == LuaType.LUA_TNIL)
            {
                state.PushNil();
                return 1;
            }
            
            state.PushNumber(i);
            state.Push(value);
            return 2;
        }

        // next(table [, index])
        private static int Next(LuaState state)
        {
            LuaTable table = state.ToTable(1);
            if (table == null)
            {
                state.PushString("bad argument #1 to 'next' (table expected)");
                return state.Error();
            }

            LuaValue key;
            if (state.GetTop() >= 2)
            {
                key = state.Get(2);
            }
            else
            {
                key = LuaValue.Nil;
            }

            // Используем метод Next() из LuaTable для корректной итерации
            if (table.Next(key, out LuaValue nextKey, out LuaValue nextValue))
            {
                state.Push(nextKey);
                state.Push(nextValue);
                return 2;
            }
            else
            {
                state.PushNil();
                return 1;
            }
        }

        // assert(v [, message])
        private static int Assert(LuaState state)
        {
            if (!state.ToBoolean(1))
            {
                string msg = state.ToString(2) ?? "assertion failed!";
                state.PushString(msg);
                return state.Error();
            }
            return state.GetTop();
        }

        // error(message [, level])
        private static int Error(LuaState state)
        {
            return state.Error();
        }

        // getmetatable(object)
        private static int GetMetatable(LuaState state)
        {
            state.GetMetatable(1);
            return 1;
        }

        // setmetatable(object, metatable)
        private static int SetMetatable(LuaState state)
        {
            state.SetMetatable(1);
            return 1;
        }

        // rawequal(v1, v2)
        private static int RawEqual(LuaState state)
        {
            LuaValue v1 = state.Get(1);
            LuaValue v2 = state.Get(2);
            state.PushBoolean(v1 == v2);
            return 1;
        }

        // rawget(table, index)
        private static int RawGet(LuaState state)
        {
            state.RawGet(1);
            return 1;
        }

        // rawset(table, index, value)
        private static int RawSet(LuaState state)
        {
            state.RawSet(1);
            return 0;
        }

        // collectgarbage([opt [, arg]])
        private static int CollectGarbage(LuaState state)
        {
            string option = state.ToString(1) ?? "collect";
            
            switch (option)
            {
                case "collect":
                    GC.Collect();
                    state.PushNumber(0);
                    break;
                case "count":
                    state.PushNumber(GC.GetTotalMemory(false) / 1024.0);
                    break;
                case "step":
                    GC.Collect();
                    state.PushBoolean(true);
                    break;
                default:
                    state.PushNumber(0);
                    break;
            }
            
            return 1;
        }

        // pcall(f [, arg1, ...])
        private static int PCall(LuaState state)
        {
            if (!state.IsFunction(1))
            {
                state.PushBoolean(false);
                state.PushString("attempt to call non-function");
                return 2;
            }
            
            int nargs = state.GetTop() - 1;
            
            try
            {
                int results = state.PCall(nargs, -1, 0);
                
                if (results != 0)
                {
                    state.PushBoolean(false);
                    state.Insert(-2);
                    return 2;
                }
                
                state.PushBoolean(true);
                state.Insert(-nargs - 1);
                return state.GetTop();
            }
            catch (Exception e)
            {
                state.PushBoolean(false);
                state.PushString(e.Message);
                return 2;
            }
        }

        // xpcall(f, err)
        private static int XPCall(LuaState state)
        {
            // Упрощенная реализация xpcall
            return PCall(state);
        }

        // select(index, ...)
        private static int Select(LuaState state)
        {
            if (state.Type(1) == LuaType.LUA_TSTRING && state.ToString(1) == "#")
            {
                state.PushNumber(state.GetTop() - 1);
                return 1;
            }
            
            int start = (int)state.ToNumber(1);
            int top = state.GetTop();
            
            if (start < 0)
                start = top + start;
            else if (start > top)
                start = top;
            
            for (int i = start; i < top; i++)
            {
                state.Push(state.Get(i + 1));
            }
            
            return top - start;
        }

        // unpack(list [, i [, j]])
        private static int Unpack(LuaState state)
        {
            LuaTable table = state.ToTable(1);
            int start = state.IsNumber(2) ? (int)state.ToNumber(2) : 1;
            int end = state.IsNumber(3) ? (int)state.ToNumber(3) : table.Length;
            
            for (int i = start; i <= end; i++)
            {
                state.Push(table[LuaValue.CreateNumber(i)]);
            }
            
            return end - start + 1;
        }

        // loadstring(string [, chunkname])
        private static int LoadString(LuaState state)
        {
            try
            {
                string source = state.ToString(1);
                string chunkName = state.ToString(2) ?? "chunk";
                
                var compiler = new Lua51Net.Compiler.LuaCompiler(source);
                var prototype = compiler.Compile();
                
                var func = new LuaFunction
                {
                    Prototype = prototype,
                    Name = chunkName
                };
                
                state.PushFunction(func);
                return 1;
            }
            catch (Exception e)
            {
                state.PushNil();
                state.PushString(e.Message);
                return 2;
            }
        }

        // load(func [, chunkname])
        private static int Load(LuaState state)
        {
            return LoadString(state);
        }

        // getfenv(f)
        private static int GetFEnv(LuaState state)
        {
            // Упрощенная реализация - возвращает глобальную таблицу
            state.GetGlobal("_G");
            return 1;
        }

        // setfenv(f, t)
        private static int SetFEnv(LuaState state)
        {
            // Упрощенная реализация
            state.PushValue(1);
            return 1;
        }
    }
}
