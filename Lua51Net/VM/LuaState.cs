using System;
using System.Collections.Generic;
using Lua51Net.Core;

namespace Lua51Net.VM
{
    /// <summary>
    /// Статус корутины
    /// </summary>
    public enum ThreadStatus
    {
        LUA_OK = 0,           // Нормальное завершение или готов к выполнению
        LUA_YIELD = 1,        // Приостановлена (yielded)
        LUA_ERRRUN = 2,       // Ошибка во время выполнения
        LUA_ERRSYNTAX = 3,    // Синтаксическая ошибка
        LUA_ERRMEM = 4,       // Ошибка памяти
        LUA_ERRERR = 5        // Ошибка в обработчике ошибок
    }

    /// <summary>
    /// Состояние виртуальной машины Lua (эквивалент lua_State)
    /// </summary>
    public class LuaState
    {
        private readonly Stack<LuaValue> _stack = new Stack<LuaValue>();
        private readonly List<LuaTable> _tables = new List<LuaTable>();
        private LuaTable _globals;
        private bool _isDisposed;
        
        // Поля для поддержки корутин
        private LuaState _mainThread;  // Ссылка на главное состояние (для потоков)
        private LuaState _caller;      // Вызвавшая корутина (для resume)
        private ThreadStatus _status;  // Статус корутины
        private int _baseIndex;        // Базовый индекс стека для этой корутины

        public LuaState() : this(null)
        {
        }

        private LuaState(LuaState mainThread)
        {
            _mainThread = mainThread ?? this;
            _status = ThreadStatus.LUA_OK;
            _globals = new LuaTable();
            InitializeStandardLibs();
        }

        private void InitializeStandardLibs()
        {
            // Инициализация стандартных библиотек
            Libraries.StringLib.Register(this);
            Libraries.TableLib.Register(this);
            Libraries.IOLib.Register(this);
            Libraries.CoroutineLib.Register(this);
        }

        #region Stack Operations

        public int GetTop()
        {
            return _stack.Count;
        }

        public void SetTop(int index)
        {
            if (index >= _stack.Count)
                return;
            
            while (_stack.Count > index)
                _stack.Pop();
        }

        public void Push(LuaValue value)
        {
            _stack.Push(value);
        }

        public LuaValue Pop()
        {
            if (_stack.Count == 0)
                return LuaValue.Nil;
            return _stack.Pop();
        }

        public void PushValue(int index)
        {
            Push(Get(index));
        }

        public LuaValue Get(int index)
        {
            int idx = ToAbsolute(index);
            if (idx < 0 || idx >= _stack.Count)
                return LuaValue.Nil;
            
            var stackArray = new List<LuaValue>(_stack);
            stackArray.Reverse();
            return stackArray[idx];
        }

        public void Set(int index, LuaValue value)
        {
            int idx = ToAbsolute(index);
            if (idx < 0 || idx >= _stack.Count)
                return;

            var stackArray = new List<LuaValue>(_stack);
            stackArray.Reverse();
            stackArray[idx] = value;
            _stack.Clear();
            for (int i = stackArray.Count - 1; i >= 0; i--)
                _stack.Push(stackArray[i]);
        }

        public void Insert(int index, LuaValue value)
        {
            int idx = ToAbsolute(index);
            if (idx < 0 || idx > _stack.Count)
                return;

            var stackArray = new List<LuaValue>(_stack);
            stackArray.Reverse();
            stackArray.Insert(idx, value);
            _stack.Clear();
            for (int i = stackArray.Count - 1; i >= 0; i--)
                _stack.Push(stackArray[i]);
        }

        public void Remove(int index)
        {
            int idx = ToAbsolute(index);
            if (idx < 0 || idx >= _stack.Count)
                return;

            var stackArray = new List<LuaValue>(_stack);
            stackArray.Reverse();
            stackArray.RemoveAt(idx);
            _stack.Clear();
            for (int i = stackArray.Count - 1; i >= 0; i--)
                _stack.Push(stackArray[i]);
        }

        public void Replace(int index, LuaValue value)
        {
            Set(index, value);
        }

        public int ToAbsolute(int index)
        {
            if (index > 0)
                return index - 1;
            return _stack.Count + index;
        }

        #endregion

        #region Type Checking

        public LuaType Type(int index)
        {
            LuaValue value = Get(index);
            return value.Type;
        }

        public bool IsNil(int index)
        {
            return Type(index) == LuaType.LUA_TNIL;
        }

        public bool IsNumber(int index)
        {
            LuaType t = Type(index);
            return t == LuaType.LUA_TNUMBER || 
                   (t == LuaType.LUA_TSTRING && double.TryParse(Get(index).ToStringValue(), out _));
        }

        public bool IsString(int index)
        {
            LuaType t = Type(index);
            return t == LuaType.LUA_TSTRING || t == LuaType.LUA_TNUMBER;
        }

        public bool IsFunction(int index)
        {
            return Type(index) == LuaType.LUA_TFUNCTION;
        }

        public bool IsTable(int index)
        {
            return Type(index) == LuaType.LUA_TTABLE;
        }

        public bool IsThread(int index)
        {
            return Type(index) == LuaType.LUA_TTHREAD;
        }

        public bool IsBoolean(int index)
        {
            return Type(index) == LuaType.LUA_TBOOLEAN;
        }

        #endregion

        #region Conversion

        public double ToNumber(int index)
        {
            return Get(index).ToNumber();
        }

        public bool ToBoolean(int index)
        {
            return Get(index).ToBoolean();
        }

        public string ToString(int index)
        {
            LuaValue value = Get(index);
            if (value.Type == LuaType.LUA_TNIL)
                return null;
            return value.ToStringValue();
        }

        public LuaFunction ToFunction(int index)
        {
            LuaValue value = Get(index);
            if (value.Type != LuaType.LUA_TFUNCTION)
                return null;
            return (LuaFunction)value.Value;
        }

        public LuaTable ToTable(int index)
        {
            LuaValue value = Get(index);
            if (value.Type != LuaType.LUA_TTABLE)
                return null;
            return (LuaTable)value.Value;
        }

        public LuaState ToThread(int index)
        {
            LuaValue value = Get(index);
            if (value.Type != LuaType.LUA_TTHREAD)
                return null;
            return (LuaState)value.Value;
        }

        public object ToUserData(int index)
        {
            LuaValue value = Get(index);
            if (value.Type != LuaType.LUA_TUSERDATA)
                return null;
            return value.Value;
        }

        #endregion

        #region Push Operations

        public void PushNil()
        {
            Push(LuaValue.Nil);
        }

        public void PushNumber(double n)
        {
            Push(LuaValue.CreateNumber(n));
        }

        public void PushBoolean(bool b)
        {
            Push(LuaValue.CreateBoolean(b));
        }

        public void PushString(string s)
        {
            if (s == null)
                PushNil();
            else
                Push(LuaValue.CreateString(s));
        }

        public void PushFunction(LuaFunction func)
        {
            Push(LuaValue.CreateFunction(func));
        }

        public void PushTable(LuaTable table)
        {
            Push(LuaValue.CreateTable(table));
        }

        public void PushThread(LuaState thread)
        {
            Push(LuaValue.CreateThread(thread));
        }

        public void PushUserData(object obj)
        {
            Push(LuaValue.CreateUserData(obj));
        }

        public void PushCFunction(LuaNativeFunction func)
        {
            var luaFunc = new LuaFunction { NativeFunc = func };
            PushFunction(luaFunc);
        }

        #endregion

        #region Table Operations

        public void NewTable()
        {
            var table = new LuaTable();
            _tables.Add(table);
            Push(LuaValue.CreateTable(table));
        }

        public void SetField(int index, string field)
        {
            LuaValue value = Get(-1); // Получаем значение с вершины стека
            Pop(); // Удаляем значение из стека
            PushString(field); // Ключ
            Push(value); // Значение
            SetTable(index);
        }

        public void GetMetaTable(int index)
        {
            // Упрощенная реализация - возвращает nil
            PushNil();
        }

        public void GetField(int index, string field)
        {
            PushString(field);
            GetTable(index);
        }

        public void GetTable(int index)
        {
            LuaValue key = Pop();
            LuaValue table = Get(index);
            
            if (table.Type != LuaType.LUA_TTABLE)
            {
                PushNil();
                return;
            }

            LuaTable t = (LuaTable)table.Value;
            Push(t[key]);
        }

        public void SetTable(int index)
        {
            LuaValue value = Pop();
            LuaValue key = Pop();
            LuaValue table = Get(index);

            if (table.Type != LuaType.LUA_TTABLE)
                return;

            LuaTable t = (LuaTable)table.Value;
            t[key] = value;
        }

        public void RawGet(int index)
        {
            GetTable(index);
        }

        public void RawSet(int index)
        {
            SetTable(index);
        }

        public int RawLen(int index)
        {
            LuaValue value = Get(index);
            if (value.Type == LuaType.LUA_TTABLE)
                return ((LuaTable)value.Value).Length;
            if (value.Type == LuaType.LUA_TSTRING)
                return ((string)value.Value).Length;
            return 0;
        }

        #endregion

        #region Global Environment

        public void GetGlobal(string name)
        {
            Push(LuaValue.CreateString(name));
            Push(LuaValue.Nil); // Placeholder для таблицы global
            GetTable(-2);
            Remove(-2);
        }

        public void SetGlobal(string name)
        {
            LuaValue value = Pop();
            _globals[LuaValue.CreateString(name)] = value;
        }

        #endregion

        #region Function Call

        public int Call(int nargs, int nresults)
        {
            LuaValue func = Get(-(nargs + 1));
            
            if (func.Type != LuaType.LUA_TFUNCTION)
                throw new InvalidOperationException("Attempt to call non-function");

            LuaFunction luaFunc = (LuaFunction)func.Value;

            if (luaFunc.IsNative)
            {
                // Вызов C# функции
                int result = luaFunc.NativeFunc(this);
                return result;
            }
            else
            {
                // Вызов Lua функции через VM
                return ExecuteFunction(luaFunc, nargs, nresults);
            }
        }

        private int ExecuteFunction(LuaFunction func, int nargs, int nresults)
        {
            // Создание VM для выполнения байт-кода
            var vm = new LuaVM(this);
            return vm.ExecuteFunction(func, nargs, nresults);
        }

        public int PCall(int nargs, int nresults, int errfunc)
        {
            try
            {
                return Call(nargs, nresults);
            }
            catch (Exception e)
            {
                PushString(e.Message);
                return 1; // error
            }
        }

        #endregion

        #region Metatables

        private LuaTable _metatables = new LuaTable();

        public void GetMetatable(int index)
        {
            LuaValue value = Get(index);
            // Упрощенная реализация
            PushNil();
        }

        public void SetMetatable(int index)
        {
            LuaValue mt = Pop();
            // Упрощенная реализация
        }

        #endregion

        #region Thread/Coroutine

        /// <summary>
        /// Создает новую корутину (поток) в этом состоянии
        /// </summary>
        public LuaState CreateThread()
        {
            // Создаем новое состояние Lua как отдельную корутину
            var thread = new LuaState(_mainThread);
            thread._caller = this;
            thread._status = ThreadStatus.LUA_OK;
            
            // Помещаем корутину в стек как значение типа thread
            Push(LuaValue.CreateThread(thread));
            
            return thread;
        }

        /// <summary>
        /// Возвращает статус корутины
        /// </summary>
        public ThreadStatus Status => _status;

        /// <summary>
        /// Проверяет, является ли это состояние главным потоком
        /// </summary>
        public bool IsMainThread => _mainThread == this;

        /// <summary>
        /// Запускает или возобновляет выполнение корутины
        /// Возвращает количество результатов на стеке
        /// </summary>
        public int Resume(LuaState from, int nargs)
        {
            if (_status != ThreadStatus.LUA_OK && _status != ThreadStatus.LUA_YIELD)
            {
                // Корутина не может быть запущена (уже завершена или в ошибке)
                from.PushString("cannot resume " + 
                    (_status == ThreadStatus.LUA_ERRRUN ? "dead" : 
                     _status == ThreadStatus.LUA_ERRSYNTAX ? "suspended" : "coroutine"));
                return 1;
            }

            // Сохраняем вызывающую корутину
            _caller = from;
            
            if (_status == ThreadStatus.LUA_YIELD)
            {
                // Возобновление после yield - аргументы становятся результатами yield
                _status = ThreadStatus.LUA_OK;
                return nargs;
            }
            else
            {
                // Первый запуск корутины
                // Аргументы передаются в функцию корутины
                _status = ThreadStatus.LUA_OK;
                
                // Получаем функцию корутины из стека
                if (nargs > 0 && Get(-nargs).Type == LuaType.LUA_TFUNCTION)
                {
                    // Вызываем функцию с аргументами
                    try
                    {
                        int results = Call(nargs, -1); // -1 означает все результаты
                        _status = ThreadStatus.LUA_OK;
                        return results;
                    }
                    catch (Exception e)
                    {
                        _status = ThreadStatus.LUA_ERRRUN;
                        from.PushString(e.Message);
                        return 1;
                    }
                }
                else
                {
                    // Нет функции для запуска
                    _status = ThreadStatus.LUA_OK;
                    return 0;
                }
            }
        }

        /// <summary>
        /// Приостанавливает выполнение корутины и возвращает управление вызвавшему коду
        /// Возвращает количество значений, переданных через yield
        /// </summary>
        public int Yield(int nresults)
        {
            if (_caller == null)
            {
                throw new LuaException("attempt to yield across metamethod/C-call boundary");
            }

            // Сохраняем статус
            _status = ThreadStatus.LUA_YIELD;
            
            // Перемещаем nresults со стека этой корутины в стек вызвавшей
            for (int i = 0; i < nresults; i++)
            {
                LuaValue value = Pop();
                _caller.Push(value);
            }
            
            return nresults;
        }

        /// <summary>
        /// Внутренний метод для обработки возврата из корутины
        /// </summary>
        internal void SetStatus(ThreadStatus status)
        {
            _status = status;
        }

        #endregion

        #region Reference System

        private readonly Stack<int> _freeRefs = new Stack<int>();
        private readonly Dictionary<int, LuaValue> _refs = new Dictionary<int, LuaValue>();
        private int _nextRef = 0;

        public int Ref(int t)
        {
            LuaValue value = Get(t);
            
            if (value.Type == LuaType.LUA_TNIL)
                return -1; // LUA_REFNIL

            int reference;
            if (_freeRefs.Count > 0)
                reference = _freeRefs.Pop();
            else
                reference = _nextRef++;

            _refs[reference] = value;
            return reference;
        }

        public void Unref(int t, int reference)
        {
            if (reference < 0)
                return;

            if (_refs.ContainsKey(reference))
            {
                _refs.Remove(reference);
                _freeRefs.Push(reference);
            }
        }

        public void RawGetI(int t, int n)
        {
            LuaValue table = Get(t);
            if (table.Type != LuaType.LUA_TTABLE)
            {
                PushNil();
                return;
            }

            LuaTable tbl = (LuaTable)table.Value;
            Push(tbl[LuaValue.CreateNumber(n)]);
        }

        public void RawSetI(int t, int n)
        {
            LuaValue value = Pop();
            LuaValue table = Get(t);

            if (table.Type != LuaType.LUA_TTABLE)
                return;

            LuaTable tbl = (LuaTable)table.Value;
            tbl[LuaValue.CreateNumber(n)] = value;
        }

        #endregion

        #region Error Handling

        public int Error()
        {
            throw new LuaException(ToString(-1));
        }

        public void Where(int level)
        {
            // Добавление информации о стеке вызовов
        }

        #endregion

        #region Memory Management

        public void CollectGarbage(int what, int data)
        {
            // GC будет работать автоматически через .NET GC
        }

        public int GetGCCount()
        {
            return (int)(GC.GetTotalMemory(false) / 1024);
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _stack.Clear();
                _tables.Clear();
                _globals?.Clear();
                _isDisposed = true;
            }
        }

        ~LuaState()
        {
            Dispose();
        }

        #endregion
    }

    /// <summary>
    /// Исключение Lua
    /// </summary>
    public class LuaException : Exception
    {
        public LuaException(string message) : base(message) { }
        public LuaException(string message, Exception inner) : base(message, inner) { }
    }
}
