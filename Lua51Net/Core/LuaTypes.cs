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
    /// Реализует семантику сравнения по спецификации Lua 5.1
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
            // По спецификации Lua 5.1: false и nil - ложные значения, все остальные - истинные
            if (Type == LuaType.LUA_TNIL) return false;
            if (Type == LuaType.LUA_TBOOLEAN) return (bool)Value;
            return true;
        }

        public double ToNumber()
        {
            if (Type == LuaType.LUA_TNUMBER) return (double)Value;
            if (Type == LuaType.LUA_TSTRING)
            {
                string s = (string)Value;
                // Пытаемся распарсить число из строки
                if (double.TryParse(s, System.Globalization.NumberStyles.Any, 
                    System.Globalization.CultureInfo.InvariantCulture, out var num))
                    return num;
            }
            return 0;
        }

        public string ToStringValue()
        {
            if (Type == LuaType.LUA_TNIL) return "nil";
            if (Type == LuaType.LUA_TNUMBER) return ((double)Value).ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (Type == LuaType.LUA_TBOOLEAN) return ((bool)Value) ? "true" : "false";
            return Value?.ToString() ?? "nil";
        }

        /// <summary>
        /// Сравнение по спецификации Lua 5.1:
        /// - nil сравнивается только с nil (равен)
        /// - boolean сравнивается по значению
        /// - number сравнивается по значению
        /// - string сравнивается по содержимому
        /// - table/function/userdata/thread сравниваются по ссылке (identity)
        /// - lightuserdata сравнивается по указателю
        /// - userdata могут иметь пользовательское равенство через __eq метаметод
        /// </summary>
        public bool Equals(LuaValue other)
        {
            if (Type != other.Type) return false;
            
            switch (Type)
            {
                case LuaType.LUA_TNIL:
                    return true; // Все nil равны
                    
                case LuaType.LUA_TBOOLEAN:
                    return (bool)Value == (bool)other.Value;
                    
                case LuaType.LUA_TNUMBER:
                    return (double)Value == (double)other.Value;
                    
                case LuaType.LUA_TSTRING:
                    return (string)Value == (string)other.Value;
                    
                case LuaType.LUA_TLIGHTUSERDATA:
                    return (IntPtr)Value == (IntPtr)other.Value;
                    
                case LuaType.LUA_TTABLE:
                case LuaType.LUA_TFUNCTION:
                case LuaType.LUA_TUSERDATA:
                case LuaType.LUA_TTHREAD:
                    // Сравнение по ссылке (identity comparison)
                    return ReferenceEquals(Value, other.Value);
                    
                default:
                    return false;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is LuaValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            if (Value == null) return 0;
            
            // Для ссылочных типов используем RuntimeHelpers.GetHashCode для identity hash
            if (Type == LuaType.LUA_TTABLE || Type == LuaType.LUA_TFUNCTION || 
                Type == LuaType.LUA_TUSERDATA || Type == LuaType.LUA_TTHREAD)
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Value);
            }
            
            return Value.GetHashCode();
        }

        public static bool operator ==(LuaValue left, LuaValue right) => left.Equals(right);
        public static bool operator !=(LuaValue left, LuaValue right) => !left.Equals(right);
    }

    /// <summary>
    /// Таблица Lua (гибридная array/hash структура по спецификации Lua 5.1)
    /// Array part хранит значения с целочисленными ключами 1..N для эффективного доступа
    /// Hash part хранит все остальные ключи
    /// </summary>
    public class LuaTable
    {
        private readonly Dictionary<LuaValue, LuaValue> _map = new Dictionary<LuaValue, LuaValue>();
        private readonly List<LuaValue> _array = new List<LuaValue>();
        private LuaTable _metatable;

        /// <summary>
        /// Мета-таблица для этой таблицы
        /// </summary>
        public LuaTable Metatable
        {
            get => _metatable;
            set => _metatable = value;
        }

        /// <summary>
        /// Индексатор для доступа к элементам таблицы
        /// Реализует доступ к array part для положительных целых ключей (1-based)
        /// Отрицательные индексы не поддерживаются для таблиц согласно Lua 5.1 spec
        /// </summary>
        public LuaValue this[LuaValue key]
        {
            get
            {
                // Проверка array part для положительных целых чисел (1-based indexing)
                if (key.Type == LuaType.LUA_TNUMBER)
                {
                    double num = key.ToNumber();
                    // Array part использует только положительные целые числа >= 1
                    if (num >= 1 && num <= _array.Count && num == Math.Floor(num))
                        return _array[(int)num - 1];
                }
                
                // Поиск в hash part (поддерживает любые ключи, включая отрицательные числа)
                if (_map.TryGetValue(key, out var value))
                    return value;
                
                // Если не найдено, проверяем метатаблицу (__index)
                if (_metatable != null)
                {
                    LuaValue indexFunc = _metatable[LuaValue.CreateString("__index")];
                    if (indexFunc.Type == LuaType.LUA_TTABLE)
                    {
                        return ((LuaTable)indexFunc.Value)[key];
                    }
                    else if (indexFunc.Type == LuaType.LUA_TFUNCTION)
                    {
                        // Вызов функции __index(table, key)
                        // Эта логика должна обрабатываться на уровне VM
                        // Здесь возвращаем nil
                    }
                }
                
                return LuaValue.Nil;
            }
            set
            {
                // Запись в array part для положительных целых чисел (1-based)
                if (key.Type == LuaType.LUA_TNUMBER)
                {
                    double num = key.ToNumber();
                    // Array part использует только положительные целые числа >= 1
                    if (num >= 1 && num == Math.Floor(num))
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
                
                // Запись в hash part (для отрицательных индексов и других ключей)
                _map[key] = value;
            }
        }

        /// <summary>
        /// Прямой доступ к raw value без учета метатаблицы
        /// </summary>
        public LuaValue RawGet(LuaValue key)
        {
            if (key.Type == LuaType.LUA_TNUMBER)
            {
                double num = key.ToNumber();
                // Array part использует только положительные целые числа >= 1
                if (num >= 1 && num <= _array.Count && num == Math.Floor(num))
                    return _array[(int)num - 1];
            }
            
            if (_map.TryGetValue(key, out var value))
                return value;
            
            return LuaValue.Nil;
        }

        /// <summary>
        /// Прямая запись без вызова метаметодов
        /// </summary>
        public void RawSet(LuaValue key, LuaValue value)
        {
            if (key.Type == LuaType.LUA_TNUMBER)
            {
                double num = key.ToNumber();
                // Array part использует только положительные целые числа >= 1
                if (num >= 1 && num == Math.Floor(num))
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

        /// <summary>
        /// Итерация next() по таблице согласно Lua 5.1 spec
        /// Возвращает следующую пару ключ-значение после заданного ключа
        /// </summary>
        public bool Next(LuaValue currentKey, out LuaValue nextKey, out LuaValue nextValue)
        {
            nextKey = LuaValue.Nil;
            nextValue = LuaValue.Nil;
            
            bool foundCurrent = currentKey.Type == LuaType.LUA_TNIL;
            
            // Сначала проходим по array part
            for (int i = 0; i < _array.Count; i++)
            {
                LuaValue key = LuaValue.CreateNumber(i + 1);
                LuaValue value = _array[i];
                
                if (!foundCurrent)
                {
                    if (key.Equals(currentKey))
                        foundCurrent = true;
                }
                else
                {
                    nextKey = key;
                    nextValue = value;
                    return true;
                }
            }
            
            // Затем проходим по hash part
            foreach (var kvp in _map)
            {
                if (!foundCurrent)
                {
                    if (kvp.Key.Equals(currentKey))
                        foundCurrent = true;
                }
                else
                {
                    nextKey = kvp.Key;
                    nextValue = kvp.Value;
                    return true;
                }
            }
            
            return false; // Конец итерации
        }

        /// <summary>
        /// Длина таблицы (# оператор)
        /// Согласно Lua 5.1: длина массива - это наибольший положительный целый индекс n
        /// такой что table[n] не nil и table[n+1] nil
        /// </summary>
        public int Length
        {
            get
            {
                // Проверяем только array part
                int len = _array.Count;
                while (len > 0 && _array[len - 1].Type == LuaType.LUA_TNIL)
                    len--;
                return len;
            }
        }

        /// <summary>
        /// Возвращает все записи таблицы для итерации
        /// </summary>
        public IEnumerable<KeyValuePair<LuaValue, LuaValue>> Entries
        {
            get
            {
                // Сначала array part
                for (int i = 0; i < _array.Count; i++)
                {
                    yield return new KeyValuePair<LuaValue, LuaValue>(LuaValue.CreateNumber(i + 1), _array[i]);
                }
                // Затем hash part
                foreach (var kvp in _map)
                    yield return kvp;
            }
        }

        /// <summary>
        /// Очистка таблицы
        /// </summary>
        public void Clear()
        {
            _map.Clear();
            _array.Clear();
            _metatable = null;
        }
        
        /// <summary>
        /// Проверка наличия ключа в таблице (без учета метатаблицы)
        /// </summary>
        public bool ContainsKey(LuaValue key)
        {
            if (key.Type == LuaType.LUA_TNUMBER)
            {
                double num = key.ToNumber();
                // Array part использует только положительные целые числа >= 1
                if (num >= 1 && num <= _array.Count && num == Math.Floor(num))
                    return _array[(int)num - 1].Type != LuaType.LUA_TNIL;
            }
            return _map.ContainsKey(key);
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

        public static Instruction CreateABC(OpCode op, int a, int b, int c)
        {
            return new Instruction
            {
                Value = (uint)((int)op | (a << 6) | (b << 16) | (c << 25))
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
