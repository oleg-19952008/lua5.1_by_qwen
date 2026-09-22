using System;
using System.IO;
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
            
            // loadfile
            RegisterFunction(state, "loadfile", LoadFile);
            
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
                throw new LuaException(msg);
            }
            // Возвращаем все аргументы (как в оригинале Lua)
            return state.GetTop();
        }

        // error(message [, level])
        private static int Error(LuaState state)
        {
            LuaValue msg = state.Get(1);
            int level = 1;
            if (state.IsNumber(2))
            {
                level = (int)state.ToNumber(2);
            }
            
            // Формируем сообщение об ошибке с информацией о позиции
            string errorMsg = msg.ToStringValue();
            throw new LuaException(errorMsg);
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
                // Сохраняем позицию для результатов
                int baseIndex = state.GetTop() - nargs;
                
                // Вызываем функцию
                int results = state.PCall(nargs, LuaState.LUA_MULTRET, 0);
                
                if (results != 0)
                {
                    // Ошибка произошла
                    state.PushBoolean(false);
                    // Перемещаем ошибку на вторую позицию
                    state.Insert(-2);
                    return 2;
                }
                
                // Успех - вставляем true перед результатами
                int resultCount = state.GetTop() - baseIndex + 1;
                state.PushBoolean(true);
                state.Insert(baseIndex);
                
                return state.GetTop() - baseIndex + 1;
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
            if (!state.IsFunction(1))
            {
                state.PushBoolean(false);
                state.PushString("attempt to call non-function");
                return 2;
            }
            
            if (!state.IsFunction(2))
            {
                state.PushBoolean(false);
                state.PushString("attempt to call non-function (error handler)");
                return 2;
            }
            
            try
            {
                // Вызываем функцию с защищённым вызовом
                int nargs = state.GetTop() - 2; // Все аргументы кроме f и err
                
                // Сохраняем обработчик ошибок
                state.PushValue(2);
                
                int results = state.PCall(nargs, LuaState.LUA_MULTRET, 0);
                
                if (results != 0)
                {
                    // Произошла ошибка - вызываем обработчик
                    LuaValue errMsg = state.Pop();
                    
                    // Вызываем функцию обработки ошибок
                    state.Push(errMsg);
                    int errResults = state.Call(1, 1);
                    
                    state.PushBoolean(false);
                    state.Insert(-2);
                    return 2;
                }
                
                // Успех
                state.PushBoolean(true);
                state.Insert(-(state.GetTop() - (state.GetTop() - nargs)));
                
                return state.GetTop();
            }
            catch (Exception e)
            {
                try
                {
                    // Пытаемся вызвать обработчик ошибок
                    state.PushValue(2);
                    state.PushString(e.Message);
                    state.Call(1, 1);
                    
                    LuaValue errResult = state.Pop();
                    state.PushBoolean(false);
                    state.Push(errResult);
                    return 2;
                }
                catch
                {
                    state.PushBoolean(false);
                    state.PushString("error in error handler: " + e.Message);
                    return 2;
                }
            }
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
                LuaValue val = state.Get(1);
                if (val.Type != LuaType.LUA_TSTRING)
                {
                    state.PushNil();
                    state.PushString("bad argument #1 to 'loadstring' (string expected)");
                    return 2;
                }
                
                string source = val.ToStringValue();
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

        // loadfile([filename])
        private static int LoadFile(LuaState state)
        {
            try
            {
                string filename = state.ToString(1) ?? "";
                string chunkName = "@" + filename;
                
                if (!File.Exists(filename))
                {
                    state.PushNil();
                    state.PushString($"Cannot open {filename}: No such file or directory");
                    return 2;
                }
                
                string source = File.ReadAllText(filename);
                
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
            // load может принимать функцию-читатель или строку
            LuaValue arg1 = state.Get(1);
            
            if (arg1.Type == LuaType.LUA_TSTRING)
            {
                return LoadString(state);
            }
            else if (arg1.Type == LuaType.LUA_TFUNCTION)
            {
                // Вызываем функцию для получения кусков кода
                // Упрощенная реализация - вызываем функцию один раз
                state.PushValue(1);
                state.Call(0, 1);
                LuaValue result = state.Pop();
                
                if (result.Type == LuaType.LUA_TNIL)
                {
                    state.PushNil();
                    state.PushString("EOF");
                    return 2;
                }
                
                // Вставляем функцию обратно и вызываем LoadString
                state.Push(result);
                state.Insert(1);
                return LoadString(state);
            }
            else
            {
                state.PushNil();
                state.PushString("bad argument #1 to 'load' (string or function expected)");
                return 2;
            }
        }

        // getfenv(f)
        private static int GetFEnv(LuaState state)
        {
            // Получаем функцию или уровень стека
            LuaValue f = state.Get(1);
            
            if (f.Type == LuaType.LUA_TNIL || f.Type == LuaType.LUA_TNUMBER)
            {
                // Если аргумент - число, это уровень стека
                int level = f.Type == LuaType.LUA_TNUMBER ? (int)f.ToNumber() : 1;
                
                // Для простоты возвращаем _G для любого уровня
                // В полной реализации нужно было бы получить окружение из кадра стека
                state.GetGlobal("_G");
                return 1;
            }
            else if (f.Type == LuaType.LUA_TFUNCTION)
            {
                // Для функции возвращаем её окружение
                // В полной реализации нужно хранить environment в LuaFunction
                state.GetGlobal("_G");
                return 1;
            }
            else
            {
                // Для других типов - возвращаем nil
                state.PushNil();
                return 1;
            }
        }

        // setfenv(f, t)
        private static int SetFEnv(LuaState state)
        {
            LuaValue f = state.Get(1);
            LuaValue t = state.Get(2);
            
            if (t.Type != LuaType.LUA_TTABLE)
            {
                throw new LuaException("'setfenv' cannot set environment to non-table");
            }
            
            if (f.Type == LuaType.LUA_TNIL || f.Type == LuaType.LUA_TNUMBER)
            {
                // Установка окружения для текущего уровня
                // В полной реализации нужно модифицировать кадр стека
                // Здесь просто сохраняем в глобальном состоянии
                state.SetGlobal("_ENV_CURRENT");
                state.PushValue(1);
                return 1;
            }
            else if (f.Type == LuaType.LUA_TFUNCTION)
            {
                // Установка окружения для функции
                // В полной реализации нужно сохранить таблицу в LuaFunction
                state.SetGlobal("_ENV_FUNC");
                state.PushValue(1);
                return 1;
            }
            else
            {
                throw new LuaException("'setfenv' cannot modify environment of non-function/non-number");
            }
        }
    }
}
