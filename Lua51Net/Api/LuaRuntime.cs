using System;
using Lua51Net.Core;
using Lua51Net.VM;

namespace Lua51Net.Api
{
    /// <summary>
    /// Основной API для работы с Lua из C# кода
    /// Предоставляет удобный интерфейс для загрузки и выполнения скриптов
    /// </summary>
    public class LuaRuntime : IDisposable
    {
        private readonly LuaState _state;
        private readonly LuaVM _vm;
        private bool _isDisposed;

        public LuaRuntime()
        {
            _state = new LuaState();
            _vm = new LuaVM(_state);
            RegisterStandardLibs();
        }

        private void RegisterStandardLibs()
        {
            // Базовые функции (_G)
            RegisterBaseFunctions();
            
            // Математическая библиотека
            RegisterMathLib();
            
            // Библиотека строк
            RegisterStringLib();
            
            // Библиотека таблиц
            RegisterTableLib();
            
            // Библиотека ввода-вывода
            RegisterIOLib();
            
            // Библиотека ОС
            RegisterOSLib();
            
            // Библиотека отладки
            RegisterDebugLib();
            
            // Загрузчик модулей
            RegisterPackageLib();
        }

        #region Base Functions

        private void RegisterBaseFunctions()
        {
            // assert
            _state.PushCFunction(Assert);
            _state.SetGlobal("assert");

            // collectgarbage
            _state.PushCFunction(CollectGarbage);
            _state.SetGlobal("collectgarbage");

            // error
            _state.PushCFunction(Error);
            _state.SetGlobal("error");

            // getfenv
            _state.PushCFunction(GetFEnv);
            _state.SetGlobal("getfenv");

            // getmetatable
            _state.PushCFunction(GetMetatable);
            _state.SetGlobal("getmetatable");

            // ipairs
            _state.PushCFunction(IPairs);
            _state.SetGlobal("ipairs");

            // load
            _state.PushCFunction(Load);
            _state.SetGlobal("load");

            // loadstring
            _state.PushCFunction(LoadString);
            _state.SetGlobal("loadstring");

            // next
            _state.PushCFunction(Next);
            _state.SetGlobal("next");

            // pairs
            _state.PushCFunction(Pairs);
            _state.SetGlobal("pairs");

            // pcall
            _state.PushCFunction(PCall);
            _state.SetGlobal("pcall");

            // print
            _state.PushCFunction(Print);
            _state.SetGlobal("print");

            // rawequal
            _state.PushCFunction(RawEqual);
            _state.SetGlobal("rawequal");

            // rawget
            _state.PushCFunction(RawGet);
            _state.SetGlobal("rawget");

            // rawset
            _state.PushCFunction(RawSet);
            _state.SetGlobal("rawset");

            // select
            _state.PushCFunction(Select);
            _state.SetGlobal("select");

            // setfenv
            _state.PushCFunction(SetFEnv);
            _state.SetGlobal("setfenv");

            // setmetatable
            _state.PushCFunction(SetMetatable);
            _state.SetGlobal("setmetatable");

            // tonumber
            _state.PushCFunction(ToNumber);
            _state.SetGlobal("tonumber");

            // tostring
            _state.PushCFunction(ToString);
            _state.SetGlobal("tostring");

            // type
            _state.PushCFunction(Type);
            _state.SetGlobal("type");

            // unpack
            _state.PushCFunction(Unpack);
            _state.SetGlobal("unpack");

            // xpcall
            _state.PushCFunction(XPCall);
            _state.SetGlobal("xpcall");

            // _G (глобальная таблица)
            _state.NewTable();
            _state.SetGlobal("_G");

            // _VERSION
            _state.PushString("Lua 5.1");
            _state.SetGlobal("_VERSION");
        }

        private int Assert(LuaState state)
        {
            if (!state.ToBoolean(1))
            {
                string msg = state.ToString(2) ?? "assertion failed!";
                state.PushString(msg);
                return state.Error();
            }
            return state.GetTop();
        }

        private int CollectGarbage(LuaState state)
        {
            string option = state.ToString(1) ?? "collect";
            int data = state.IsNumber(2) ? (int)state.ToNumber(2) : 0;
            
            state.GetGCCount();
            return 1;
        }

        private int Error(LuaState state)
        {
            return state.Error();
        }

        private int GetFEnv(LuaState state)
        {
            // Упрощенная реализация
            state.PushNil();
            return 1;
        }

        private int GetMetatable(LuaState state)
        {
            state.GetMetatable(1);
            return 1;
        }

        private int IPairs(LuaState state)
        {
            // Возвращает iterator функцию, состояние и начальный индекс 0
            _state.PushCFunction(IPairsIterator);
            _state.PushValue(1); // Таблица
            _state.PushNumber(0);
            return 3;
        }

        private int IPairsIterator(LuaState state)
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

        private int Load(LuaState state)
        {
            return LoadString(state);
        }

        private int LoadString(LuaState state)
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

        private int Next(LuaState state)
        {
            LuaTable table = state.ToTable(1);
            // Упрощенная реализация
            state.PushNil();
            return 1;
        }

        private int Pairs(LuaState state)
        {
            state.PushCFunction(Next);
            state.PushValue(1);
            state.PushNil();
            return 3;
        }

        private int PCall(LuaState state)
        {
            int nargs = state.GetTop() - 1;
            int results = state.PCall(nargs, -1, 0);
            
            if (results != 0)
            {
                // Error occurred
                state.PushBoolean(false);
                state.Insert(-2);
                return 2;
            }
            
            state.PushBoolean(true);
            state.Insert(-nargs - 1);
            return state.GetTop();
        }

        private int Print(LuaState state)
        {
            int n = state.GetTop();
            for (int i = 1; i <= n; i++)
            {
                Console.Write(state.ToString(i));
                if (i < n)
                    Console.Write("\t");
            }
            Console.WriteLine();
            return 0;
        }

        private int RawEqual(LuaState state)
        {
            LuaValue v1 = state.Get(1);
            LuaValue v2 = state.Get(2);
            state.PushBoolean(v1 == v2);
            return 1;
        }

        private int RawGet(LuaState state)
        {
            state.RawGet(1);
            return 1;
        }

        private int RawSet(LuaState state)
        {
            state.RawSet(1);
            return 0;
        }

        private int Select(LuaState state)
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

        private int SetFEnv(LuaState state)
        {
            // Упрощенная реализация
            state.PushNil();
            return 1;
        }

        private int SetMetatable(LuaState state)
        {
            state.SetMetatable(1);
            return 1;
        }

        private int ToNumber(LuaState state)
        {
            LuaValue value = state.Get(1);
            double baseNum = state.IsNumber(2) ? state.ToNumber(2) : 10;
            
            if (value.Type == LuaType.LUA_TNUMBER)
            {
                state.PushValue(1);
                return 1;
            }
            
            if (value.Type == LuaType.LUA_TSTRING)
            {
                string str = (string)value.Value;
                if (double.TryParse(str, out double num))
                {
                    state.PushNumber(num);
                    return 1;
                }
            }
            
            state.PushNil();
            return 1;
        }

        private int ToString(LuaState state)
        {
            LuaValue value = state.Get(1);
            state.PushString(value.ToStringValue());
            return 1;
        }

        private int Type(LuaState state)
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

        private int Unpack(LuaState state)
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

        private int XPCall(LuaState state)
        {
            // Упрощенная реализация xpcall
            return PCall(state);
        }

        #endregion

        #region Math Library

        private void RegisterMathLib()
        {
            _state.NewTable();
            
            // Константы
            _state.PushNumber(Math.PI);
            _state.SetField(-2, "pi");
            
            _state.PushNumber(1e308);
            _state.SetField(-2, "huge");
            
            // Функции
            RegisterMathFunction("abs", Math.Abs);
            RegisterMathFunction("acos", Math.Acos);
            RegisterMathFunction("asin", Math.Asin);
            RegisterMathFunction("atan", Math.Atan);
            RegisterMathFunction("ceil", Math.Ceiling);
            RegisterMathFunction("cos", Math.Cos);
            RegisterMathFunction("cosh", Math.Cosh);
            RegisterMathFunction("deg", x => x * 180.0 / Math.PI);
            RegisterMathFunction("exp", Math.Exp);
            RegisterMathFunction("floor", Math.Floor);
            RegisterMathFunction("log", Math.Log);
            RegisterMathFunction("log10", Math.Log10);
            RegisterMathFunction("rad", x => x * Math.PI / 180.0);
            RegisterMathFunction("sin", Math.Sin);
            RegisterMathFunction("sinh", Math.Sinh);
            RegisterMathFunction("sqrt", Math.Sqrt);
            RegisterMathFunction("tan", Math.Tan);
            RegisterMathFunction("tanh", Math.Tanh);
            
            _state.PushCFunction(MathRandom);
            _state.SetField(-2, "random");
            
            _state.PushCFunction(MathRandomSeed);
            _state.SetField(-2, "randomseed");
            
            _state.PushCFunction(MathPow);
            _state.SetField(-2, "pow");
            
            _state.PushCFunction(MathMod);
            _state.SetField(-2, "mod");
            
            _state.PushCFunction(MathMax);
            _state.SetField(-2, "max");
            
            _state.PushCFunction(MathMin);
            _state.SetField(-2, "min");
            
            _state.SetGlobal("math");
        }

        private void RegisterMathFunction(string name, Func<double, double> func)
        {
            _state.PushCFunction(state =>
            {
                double x = state.ToNumber(1);
                state.PushNumber(func(x));
                return 1;
            });
            _state.SetField(-2, name);
        }

        private int MathRandom(LuaState state)
        {
            // Простая реализация random
            Random rand = new Random();
            
            int n = state.GetTop();
            if (n == 0)
            {
                state.PushNumber(rand.NextDouble());
            }
            else if (n == 1)
            {
                int m = (int)state.ToNumber(1);
                state.PushNumber(rand.Next(1, m + 1));
            }
            else
            {
                int m = (int)state.ToNumber(1);
                int n2 = (int)state.ToNumber(2);
                state.PushNumber(rand.Next(m, n2 + 1));
            }
            
            return 1;
        }

        private int MathRandomSeed(LuaState state)
        {
            // Seed не поддерживается в этой реализации
            return 0;
        }

        private int MathPow(LuaState state)
        {
            double x = state.ToNumber(1);
            double y = state.ToNumber(2);
            state.PushNumber(Math.Pow(x, y));
            return 1;
        }

        private int MathMod(LuaState state)
        {
            double x = state.ToNumber(1);
            double y = state.ToNumber(2);
            state.PushNumber(x % y);
            return 1;
        }

        private int MathMax(LuaState state)
        {
            int n = state.GetTop();
            double max = state.ToNumber(1);
            
            for (int i = 2; i <= n; i++)
            {
                double val = state.ToNumber(i);
                if (val > max)
                    max = val;
            }
            
            state.PushNumber(max);
            return 1;
        }

        private int MathMin(LuaState state)
        {
            int n = state.GetTop();
            double min = state.ToNumber(1);
            
            for (int i = 2; i <= n; i++)
            {
                double val = state.ToNumber(i);
                if (val < min)
                    min = val;
            }
            
            state.PushNumber(min);
            return 1;
        }

        #endregion

        #region String Library

        private void RegisterStringLib()
        {
            _state.NewTable();
            
            _state.PushCFunction(StringLen);
            _state.SetField(-2, "len");
            
            _state.PushCFunction(StringSub);
            _state.SetField(-2, "sub");
            
            _state.PushCFunction(StringLower);
            _state.SetField(-2, "lower");
            
            _state.PushCFunction(StringUpper);
            _state.SetField(-2, "upper");
            
            _state.PushCFunction(StringRep);
            _state.SetField(-2, "rep");
            
            _state.PushCFunction(StringReverse);
            _state.SetField(-2, "reverse");
            
            _state.PushCFunction(StringFormat);
            _state.SetField(-2, "format");
            
            _state.PushCFunction(StringByte);
            _state.SetField(-2, "byte");
            
            _state.PushCFunction(StringChar);
            _state.SetField(-2, "char");
            
            _state.SetGlobal("string");
        }

        private int StringLen(LuaState state)
        {
            string s = state.ToString(1);
            state.PushNumber(s?.Length ?? 0);
            return 1;
        }

        private int StringSub(LuaState state)
        {
            string s = state.ToString(1) ?? "";
            int start = (int)state.ToNumber(2);
            int end = state.IsNumber(3) ? (int)state.ToNumber(3) : s.Length;
            
            if (start < 0)
                start = s.Length + start + 1;
            if (end < 0)
                end = s.Length + end + 1;
            
            if (start < 1)
                start = 1;
            if (end > s.Length)
                end = s.Length;
            
            if (start > end)
            {
                state.PushString("");
            }
            else
            {
                state.PushString(s.Substring(start - 1, end - start + 1));
            }
            
            return 1;
        }

        private int StringLower(LuaState state)
        {
            string s = state.ToString(1) ?? "";
            state.PushString(s.ToLower());
            return 1;
        }

        private int StringUpper(LuaState state)
        {
            string s = state.ToString(1) ?? "";
            state.PushString(s.ToUpper());
            return 1;
        }

        private int StringRep(LuaState state)
        {
            string s = state.ToString(1) ?? "";
            int n = (int)state.ToNumber(2);
            string sep = state.IsString(3) ? state.ToString(3) : "";
            
            string result = string.Join(sep, Enumerable.Repeat(s, n));
            state.PushString(result);
            return 1;
        }

        private int StringReverse(LuaState state)
        {
            string s = state.ToString(1) ?? "";
            char[] chars = s.ToCharArray();
            Array.Reverse(chars);
            state.PushString(new string(chars));
            return 1;
        }

        private int StringFormat(LuaState state)
        {
            string format = state.ToString(1) ?? "";
            // Упрощенная реализация format
            state.PushString(format);
            return 1;
        }

        private int StringByte(LuaState state)
        {
            string s = state.ToString(1) ?? "";
            int i = state.IsNumber(2) ? (int)state.ToNumber(2) : 1;
            
            if (i < 1 || i > s.Length)
            {
                state.PushNil();
                return 1;
            }
            
            state.PushNumber(s[i - 1]);
            return 1;
        }

        private int StringChar(LuaState state)
        {
            int n = state.GetTop();
            char[] chars = new char[n];
            
            for (int i = 0; i < n; i++)
            {
                chars[i] = (char)state.ToNumber(i + 1);
            }
            
            state.PushString(new string(chars));
            return 1;
        }

        #endregion

        #region Table Library

        private void RegisterTableLib()
        {
            _state.NewTable();
            
            _state.PushCFunction(TableConcat);
            _state.SetField(-2, "concat");
            
            _state.PushCFunction(TableInsert);
            _state.SetField(-2, "insert");
            
            _state.PushCFunction(TableRemove);
            _state.SetField(-2, "remove");
            
            _state.PushCFunction(TableSort);
            _state.SetField(-2, "sort");
            
            _state.SetGlobal("table");
        }

        private int TableConcat(LuaState state)
        {
            LuaTable table = state.ToTable(1);
            string sep = state.IsString(2) ? state.ToString(2) : "";
            int start = state.IsNumber(3) ? (int)state.ToNumber(3) : 1;
            int end = state.IsNumber(4) ? (int)state.ToNumber(4) : table.Length;
            
            var parts = new List<string>();
            for (int i = start; i <= end; i++)
            {
                LuaValue val = table[LuaValue.CreateNumber(i)];
                parts.Add(val.ToStringValue());
            }
            
            state.PushString(string.Join(sep, parts));
            return 1;
        }

        private int TableInsert(LuaState state)
        {
            LuaTable table = state.ToTable(1);
            int pos = state.IsNumber(2) ? (int)state.ToNumber(2) : table.Length + 1;
            LuaValue value = state.Get(state.IsNumber(2) ? 3 : 2);
            
            table[LuaValue.CreateNumber(pos)] = value;
            return 0;
        }

        private int TableRemove(LuaState state)
        {
            LuaTable table = state.ToTable(1);
            int pos = state.IsNumber(2) ? (int)state.ToNumber(2) : table.Length;
            
            LuaValue value = table[LuaValue.CreateNumber(pos)];
            table[LuaValue.CreateNumber(pos)] = LuaValue.Nil;
            
            state.Push(value);
            return 1;
        }

        private int TableSort(LuaState state)
        {
            // Упрощенная реализация sort
            return 0;
        }

        #endregion

        #region IO Library

        private void RegisterIOLib()
        {
            _state.NewTable();
            
            _state.PushCFunction(IOWrite);
            _state.SetField(-2, "write");
            
            _state.PushCFunction(IORead);
            _state.SetField(-2, "read");
            
            _state.PushCFunction(IOPrint);
            _state.SetField(-2, "print");
            
            _state.SetGlobal("io");
            
            // stdin, stdout, stderr
            _state.NewTable();
            _state.SetGlobal("stdin");
            
            _state.NewTable();
            _state.SetGlobal("stdout");
            
            _state.NewTable();
            _state.SetGlobal("stderr");
        }

        private int IOWrite(LuaState state)
        {
            int n = state.GetTop();
            for (int i = 1; i <= n; i++)
            {
                Console.Write(state.ToString(i));
            }
            return 0;
        }

        private int IORead(LuaState state)
        {
            string line = Console.ReadLine();
            if (line == null)
            {
                state.PushNil();
            }
            else
            {
                state.PushString(line);
            }
            return 1;
        }

        private int IOPrint(LuaState state)
        {
            return Print(state);
        }

        #endregion

        #region OS Library

        private void RegisterOSLib()
        {
            _state.NewTable();
            
            _state.PushCFunction(OSDate);
            _state.SetField(-2, "date");
            
            _state.PushCFunction(OSSleep);
            _state.SetField(-2, "sleep");
            
            _state.PushCFunction(OSTime);
            _state.SetField(-2, "time");
            
            _state.SetGlobal("os");
        }

        private int OSDate(LuaState state)
        {
            DateTime now = DateTime.Now;
            state.PushString(now.ToString("yyyy-MM-dd HH:mm:ss"));
            return 1;
        }

        private int OSSleep(LuaState state)
        {
            int ms = (int)state.ToNumber(1);
            System.Threading.Thread.Sleep(ms);
            return 0;
        }

        private int OSTime(LuaState state)
        {
            state.PushNumber(DateTimeOffset.Now.ToUnixTimeSeconds());
            return 1;
        }

        #endregion

        #region Debug Library

        private void RegisterDebugLib()
        {
            _state.NewTable();
            
            _state.PushCFunction(DebugTraceback);
            _state.SetField(-2, "traceback");
            
            _state.PushCFunction(DebugGetInfo);
            _state.SetField(-2, "getinfo");
            
            _state.SetGlobal("debug");
        }

        private int DebugTraceback(LuaState state)
        {
            state.PushString("stack traceback:");
            return 1;
        }

        private int DebugGetInfo(LuaState state)
        {
            state.NewTable();
            return 1;
        }

        #endregion

        #region Package Library

        private void RegisterPackageLib()
        {
            _state.NewTable();
            
            _state.NewTable(); // loaded
            _state.SetField(-2, "loaded");
            
            _state.NewTable(); // preload
            _state.SetField(-2, "preload");
            
            _state.SetGlobal("package");
        }

        #endregion

        #region Execution

        public void DoString(string script)
        {
            try
            {
                var compiler = new Lua51Net.Compiler.LuaCompiler(script);
                var prototype = compiler.Compile();
                
                var func = new LuaFunction
                {
                    Prototype = prototype,
                    Name = "main_chunk"
                };
                
                _state.PushFunction(func);
                _state.Call(0, 0);
            }
            catch (Exception e)
            {
                throw new LuaException($"Error executing script: {e.Message}", e);
            }
        }

        public void DoFile(string path)
        {
            string script = System.IO.File.ReadAllText(path);
            DoString(script);
        }

        public LuaValue GetGlobal(string name)
        {
            _state.GetGlobal(name);
            return _state.Pop();
        }

        public void SetGlobal(string name, LuaValue value)
        {
            _state.Push(value);
            _state.SetGlobal(name);
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _state?.Dispose();
                _isDisposed = true;
            }
        }

        #endregion
    }

    /// <summary>
    /// Extension methods для удобства работы с LuaState
    /// </summary>
    public static class LuaStateExtensions
    {
        public static void PushValue(this LuaState state, int index)
        {
            state.Push(state.Get(index));
        }

        public static void SetField(this LuaState state, int index, string field)
        {
            state.Push(LuaValue.CreateString(field));
            state.Insert(-2);
            state.SetTable(index);
        }

        public static void GetField(this LuaState state, int index, string field)
        {
            state.Push(LuaValue.CreateString(field));
            state.GetTable(index);
        }
    }
}
