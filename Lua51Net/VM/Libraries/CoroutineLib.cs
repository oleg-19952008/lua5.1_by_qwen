using System;
using Lua51Net.Core;

namespace Lua51Net.VM.Libraries
{
    /// <summary>
    /// Библиотека coroutine - работа с корутинами
    /// </summary>
    public static class CoroutineLib
    {
        /// <summary>
        /// Регистрирует библиотеку coroutine в LuaState
        /// </summary>
        public static void Register(LuaState state)
        {
            // Создаем таблицу coroutine
            state.NewTable();
            
            // coroutine.create(f)
            state.PushCFunction(Create);
            state.SetField(-2, "create");
            
            // coroutine.resume(co [, val1, ...])
            state.PushCFunction(Resume);
            state.SetField(-2, "resume");
            
            // coroutine.yield(...)
            state.PushCFunction(Yield);
            state.SetField(-2, "yield");
            
            // coroutine.status(co)
            state.PushCFunction(Status);
            state.SetField(-2, "status");
            
            // coroutine.running()
            state.PushCFunction(Running);
            state.SetField(-2, "running");
            
            // coroutine.wrap(f)
            state.PushCFunction(Wrap);
            state.SetField(-2, "wrap");
            
            // Регистрируем таблицу coroutine в глобальной области
            state.SetGlobal("coroutine");
        }

        /// <summary>
        /// coroutine.create(f) - создает новую корутину с функцией f
        /// </summary>
        private static int Create(LuaState state)
        {
            if (!state.IsFunction(1))
            {
                throw new LuaException("bad argument #1 to 'create' (function expected)");
            }

            // Получаем функцию
            LuaValue func = state.Get(1);
            
            // Создаем новую корутину
            LuaState thread = state.CreateThread();
            
            // Помещаем функцию в стек новой корутины
            thread.Push(func);
            
            // Возвращаем корутину (она уже на стеке главного состояния)
            return 1;
        }

        /// <summary>
        /// coroutine.resume(co [, val1, ...]) - запускает или продолжает корутину
        /// </summary>
        private static int Resume(LuaState state)
        {
            if (!state.IsThread(1))
            {
                state.PushBoolean(false);
                state.PushString("attempt to resume non-coroutine");
                return 2;
            }

            LuaState co = state.ToThread(1);
            
            if (co == null)
            {
                state.PushBoolean(false);
                state.PushString("invalid coroutine");
                return 2;
            }

            // Перемещаем аргументы в стек корутины
            int nargs = state.GetTop() - 1;
            for (int i = 0; i < nargs; i++)
            {
                LuaValue arg = state.Get(2 + i);
                co.Push(arg);
            }

            // Запускаем корутину
            int result = co.Resume(state, nargs);
            
            // Проверяем результат
            if (result > 0 && state.IsString(-1))
            {
                // Ошибка
                string errorMsg = state.ToString(-1);
                state.Pop(); // Удаляем сообщение об ошибке
                
                state.PushBoolean(false);
                state.PushString(errorMsg);
                return 2;
            }
            else
            {
                // Успех - первый результат true, за ним следуют результаты корутины
                var results = new System.Collections.Generic.List<LuaValue>();
                for (int i = 0; i < result; i++)
                {
                    results.Add(state.Pop());
                }
                
                state.PushBoolean(true);
                for (int i = results.Count - 1; i >= 0; i--)
                {
                    state.Push(results[i]);
                }
                
                return result + 1;
            }
        }

        /// <summary>
        /// coroutine.yield(...) - приостанавливает корутину и возвращает значения
        /// </summary>
        private static int Yield(LuaState state)
        {
            int nresults = state.GetTop();
            
            // Yield возвращает управление вызвавшему коду
            return state.Yield(nresults);
        }

        /// <summary>
        /// coroutine.status(co) - возвращает статус корутины
        /// </summary>
        private static int Status(LuaState state)
        {
            if (!state.IsThread(1))
            {
                throw new LuaException("bad argument #1 to 'status' (coroutine expected)");
            }

            LuaState co = state.ToThread(1);
            
            if (co == null)
            {
                state.PushString("dead");
                return 1;
            }

            string status;
            switch (co.Status)
            {
                case ThreadStatus.LUA_OK:
                    // Проверяем, выполняется ли сейчас эта корутина
                    status = "running";
                    break;
                    
                case ThreadStatus.LUA_YIELD:
                    status = "suspended";
                    break;
                    
                case ThreadStatus.LUA_ERRRUN:
                case ThreadStatus.LUA_ERRSYNTAX:
                case ThreadStatus.LUA_ERRMEM:
                case ThreadStatus.LUA_ERRERR:
                    status = "dead";
                    break;
                    
                default:
                    status = "normal";
                    break;
            }

            state.PushString(status);
            return 1;
        }

        /// <summary>
        /// coroutine.running() - возвращает текущую выполняющуюся корутину и флаг main
        /// </summary>
        private static int Running(LuaState state)
        {
            // В текущей реализации мы не отслеживаем текущую корутину глобально
            // Возвращаем nil и true (главный поток)
            state.PushNil();
            state.PushBoolean(true);
            return 2;
        }

        /// <summary>
        /// coroutine.wrap(f) - создает корутину и возвращает функцию-обертку
        /// </summary>
        private static int Wrap(LuaState state)
        {
            if (!state.IsFunction(1))
            {
                throw new LuaException("bad argument #1 to 'wrap' (function expected)");
            }

            // Создаем корутину
            LuaValue func = state.Get(1);
            LuaState co = state.CreateThread();
            co.Push(func);
            
            // Удаляем корутину из стека (она хранится в замыкании)
            state.Pop();
            
            // Создаем функцию-обертку
            LuaNativeFunction wrapFunc = (s) => WrapCall(s, co);
            state.PushCFunction(wrapFunc);
            
            return 1;
        }

        /// <summary>
        /// Вызов функции-обертки для coroutine.wrap
        /// </summary>
        private static int WrapCall(LuaState state, LuaState co)
        {
            try
            {
                // Перемещаем аргументы в корутину
                int nargs = state.GetTop();
                for (int i = 0; i < nargs; i++)
                {
                    LuaValue arg = state.Get(1 + i);
                    co.Push(arg);
                }
                
                // Запускаем корутину
                int results = co.Resume(state, nargs);
                
                // Проверяем на ошибку
                if (results > 0 && state.IsString(-1))
                {
                    // Ошибка в корутине
                    throw new LuaException(state.ToString(-1));
                }
                
                return results;
            }
            catch (Exception e)
            {
                throw new LuaException(e.Message);
            }
        }
    }
}
