using System;
using System.Collections.Generic;
using System.Reflection;

namespace Lua51Net.Core
{
    /// <summary>
    /// Типы значений Lua 5.1
    /// </summary>
    public enum LuaType
    {
        LUA_TNONE = -1,
        LUA_TNIL = 0,
        LUA_TBOOLEAN = 1,
        LUA_TLIGHTUSERDATA = 2,
        LUA_TNUMBER = 3,
        LUA_TSTRING = 4,
        LUA_TTABLE = 5,
        LUA_TFUNCTION = 6,
        LUA_TUSERDATA = 7,
        LUA_TTHREAD = 8
    }

    /// <summary>
    /// Базовое значение Lua
    /// </summary>
    public struct LuaValue : IEquatable<LuaValue>
    {
        public LuaType Type;
        public object Value;

        public static LuaValue Nil => new LuaValue { Type = LuaType.LUA_TNIL };
        
        public static LuaValue CreateBoolean(bool b) => new LuaValue { Type = LuaType.LUA_TBOOLEAN, Value = b };
        
        public static LuaValue CreateNumber(double n) => new LuaValue { Type = LuaType.LUA_TNUMBER, Value = n };
        
        public static LuaValue CreateString(string s) => new LuaValue { Type = LuaType.LUA_TSTRING, Value = s };
        
        public static LuaValue CreateTable(LuaTable table) => new LuaValue { Type = LuaType.LUA_TTABLE, Value = table };
        
        public static LuaValue CreateFunction(LuaFunction func) => new LuaValue { Type = LuaType.LUA_TFUNCTION, Value = func };
        
        public static LuaValue CreateUserData(object obj) => new LuaValue { Type = LuaType.LUA_TUSERDATA, Value = obj };
        
        public static LuaValue CreateLightUserData(IntPtr ptr) => new LuaValue { Type = LuaType.LUA_TLIGHTUSERDATA, Value = ptr };
        
        public static LuaValue CreateThread(LuaState thread) => new LuaValue { Type = LuaType.LUA_TTHREAD, Value = thread };

        public bool ToBoolean()
        {
            if (Type == LuaType.LUA_TNIL) return false;
            if (Type == LuaType.LUA_TBOOLEAN) return (bool)Value;
            return true;
        }

        public double ToNumber()
        {
            if (Type == LuaType.LUA_TNUMBER) return (double)Value;
            if (Type == LuaType.LUA_TSTRING && double.TryParse((string)Value, out var num))
                return num;
            return 0;
        }

        public string ToStringValue()
        {
            return Value?.ToString() ?? "nil";
        }

        public bool Equals(LuaValue other)
        {
            if (Type != other.Type) return false;
            if (Type == LuaType.LUA_TNIL) return true;
            return Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            return obj is LuaValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value?.GetHashCode() ?? 0;
        }

        public static bool operator ==(LuaValue left, LuaValue right) => left.Equals(right);
        public static bool operator !=(LuaValue left, LuaValue right) => !left.Equals(right);
    }

    /// <summary>
    /// Таблица Lua (hash + array part)
    /// </summary>
    public class LuaTable
    {
        private readonly Dictionary<LuaValue, LuaValue> _map = new Dictionary<LuaValue, LuaValue>();
        private readonly List<LuaValue> _array = new List<LuaValue>();

        public LuaValue this[LuaValue key]
        {
            get
            {
                if (key.Type == LuaType.LUA_TNUMBER)
                {
                    double num = key.ToNumber();
                    if (num >= 1 && num <= _array.Count && num == (int)num)
                        return _array[(int)num - 1];
                }
                return _map.TryGetValue(key, out var value) ? value : LuaValue.Nil;
            }
            set
            {
                if (key.Type == LuaType.LUA_TNUMBER)
                {
                    double num = key.ToNumber();
                    if (num >= 1 && num == (int)num)
                    {
                        int index = (int)num;
                        if (index <= _array.Count)
                            _array[index - 1] = value;
                        else
                        {
                            while (_array.Count < index)
                                _array.Add(LuaValue.Nil);
                            _array[index - 1] = value;
                        }
                        return;
                    }
                }
                _map[key] = value;
            }
        }

        public void Next(ref LuaValue key, out LuaValue value)
        {
            // Реализация next() для итерации по таблице
            value = LuaValue.Nil;
            // Упрощенная версия - полная реализация сложнее
        }

        public int Length
        {
            get
            {
                int len = _array.Count;
                while (len > 0 && _array[len - 1].Type == LuaType.LUA_TNIL)
                    len--;
                return len;
            }
        }

        public void Clear()
        {
            _map.Clear();
            _array.Clear();
        }
    }

    /// <summary>
    /// Прототип функции (загруженный байт-код)
    /// </summary>
    public class LuaPrototype
    {
        public string Source;
        public int LineDefined;
        public int LastLineDefined;
        public byte NumParameters;
        public byte IsVarArg;
        public byte MaxStackSize;
        public byte NumUpvalues;
        
        public Instruction[] Code;
        public LuaValue[] Constants;
        public LuaPrototype[] Functions;
        public int[] LineInfo;
        public LocVars[] LocVars;
        public string[] Upvalues;
    }

    /// <summary>
    /// Инструкция байт-кода Lua
    /// </summary>
    public struct Instruction
    {
        public uint Value;

        public OpCode OpCode => (OpCode)((Value >> 0) & 0x3F);
        public int A => (int)((Value >> 6) & 0xFF);
        public int B => (int)((Value >> 16) & 0x1FF);
        public int C => (int)((Value >> 25) & 0x7F);
        public int Bx => (int)((Value >> 16) & 0x3FFFF);
        public int Sbx => Bx - 131071;

        public static Instruction Create(OpCode op, int a, int b, int c)
        {
            return new Instruction
            {
                Value = (uint)((int)op | (a << 6) | (b << 16) | (c << 25))
            };
        }

        public static Instruction CreateABx(OpCode op, int a, int bx)
        {
            return new Instruction
            {
                Value = (uint)((int)op | (a << 6) | ((bx & 0x3FFFF) << 16))
            };
        }
    }

    /// <summary>
    /// Коды операций Lua 5.1
    /// </summary>
    public enum OpCode
    {
        OP_MOVE = 0,
        OP_LOADK,
        OP_LOADBOOL,
        OP_LOADNIL,
        OP_GETUPVAL,
        OP_GETGLOBAL,
        OP_GETTABLE,
        OP_SETGLOBAL,
        OP_SETUPVAL,
        OP_SETTABLE,
        OP_NEWTABLE,
        OP_SELF,
        OP_ADD,
        OP_SUB,
        OP_MUL,
        OP_DIV,
        OP_MOD,
        OP_POW,
        OP_UNM,
        OP_NOT,
        OP_LEN,
        OP_CONCAT,
        OP_JMP,
        OP_EQ,
        OP_LT,
        OP_LE,
        OP_TEST,
        OP_TESTSET,
        OP_CALL,
        OP_TAILCALL,
        OP_RETURN,
        OP_FORLOOP,
        OP_FORPREP,
        OP_TFORLOOP,
        OP_SETLIST,
        OP_CLOSE,
        OP_CLOSURE,
        OP_VARARG
    }

    /// <summary>
    /// Локальная переменная
    /// </summary>
    public struct LocVars
    {
        public string Name;
        public int StartPc;
        public int EndPc;
    }

    /// <summary>
    /// Делегат для C# функций, вызываемых из Lua
    /// </summary>
    public delegate int LuaNativeFunction(LuaState state);

    /// <summary>
    /// Функция Lua (нативная или замыкание)
    /// </summary>
    public class LuaFunction
    {
        public LuaNativeFunction NativeFunc;
        public LuaPrototype Prototype;
        public LuaValue[] Upvalues;
        public string Name;

        public bool IsNative => NativeFunc != null;
    }
}
