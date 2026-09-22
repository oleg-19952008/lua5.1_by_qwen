using System;
using Lua51Net.Core;

namespace Lua51Net.VM
{
    /// <summary>
    /// Виртуальная машина Lua для выполнения байт-кода
    /// Реализует все инструкции Lua 5.1 VM
    /// </summary>
    public class LuaVM
    {
        private readonly LuaState _state;
        private CallFrame[] _callStack;
        private int _frameIndex;
        private const int MaxFrames = 1024;

        public LuaVM(LuaState state)
        {
            _state = state;
            _callStack = new CallFrame[MaxFrames];
            _frameIndex = -1;
        }

        /// <summary>
        /// Выполнение функции Lua
        /// </summary>
        public int ExecuteFunction(LuaFunction func, int nargs, int nresults)
        {
            if (func.Prototype == null)
                return 0;

            PushCallFrame(func, nargs);
            
            var frame = CurrentFrame;
            var prototype = func.Prototype;
            var code = prototype.Code;
            var constants = prototype.Constants;
            var upvalues = func.Upvalues ?? new LuaValue[0];

            // Инициализация параметров в регистрах
            for (int i = 0; i < nargs && i < prototype.NumParameters; i++)
            {
                frame.Registers[i] = _state.Get(-(nargs - i));
            }
            
            // Очистка остальных регистров
            for (int i = prototype.NumParameters; i < frame.Registers.Length; i++)
            {
                frame.Registers[i] = LuaValue.Nil;
            }

            // Основной цикл выполнения
            while (frame.PC < code.Length)
            {
                Instruction instr = code[frame.PC];
                OpCode op = instr.OpCode;
                int a = instr.A;
                int b = instr.B;
                int c = instr.C;
                int bx = instr.Bx;
                int sbx = instr.Sbx;

                frame.PC++;

                switch (op)
                {
                    case OpCode.OP_MOVE:
                        frame.Registers[a] = frame.Registers[b];
                        break;

                    case OpCode.OP_LOADK:
                        frame.Registers[a] = constants[bx];
                        break;

                    case OpCode.OP_LOADBOOL:
                        frame.Registers[a] = LuaValue.CreateBoolean(b != 0);
                        if (c != 0)
                            frame.PC++;
                        break;

                    case OpCode.OP_LOADNIL:
                        for (int i = a; i <= b; i++)
                            frame.Registers[i] = LuaValue.Nil;
                        break;

                    case OpCode.OP_GETUPVAL:
                        frame.Registers[a] = upvalues[b];
                        break;

                    case OpCode.OP_GETGLOBAL:
                        LuaValue globalName = constants[bx];
                        _state.Push(globalName);
                        _state.GetTable((int)LuaType.LUA_TGLOBAL);
                        frame.Registers[a] = _state.Pop();
                        break;

                    case OpCode.OP_GETTABLE:
                        LuaValue tableVal = GetRKValue(frame, b);
                        LuaValue keyVal = GetRKValue(frame, c);
                        
                        if (tableVal.Type == LuaType.LUA_TTABLE)
                        {
                            LuaTable table = (LuaTable)tableVal.Value;
                            frame.Registers[a] = table[keyVal];
                        }
                        else
                        {
                            throw new LuaException("Attempt to index non-table");
                        }
                        break;

                    case OpCode.OP_SETGLOBAL:
                        LuaValue globalNameSet = constants[bx];
                        _state.Push(globalNameSet);
                        _state.Push(frame.Registers[a]);
                        _state.SetTable((int)LuaType.LUA_TGLOBAL);
                        break;

                    case OpCode.OP_SETUPVAL:
                        upvalues[b] = frame.Registers[a];
                        break;

                    case OpCode.OP_SETTABLE:
                        LuaValue tableSet = frame.Registers[a];
                        if (tableSet.Type == LuaType.LUA_TTABLE)
                        {
                            LuaTable tbl = (LuaTable)tableSet.Value;
                            LuaValue k = GetRKValue(frame, b);
                            LuaValue v = GetRKValue(frame, c);
                            tbl[k] = v;
                        }
                        else
                        {
                            throw new LuaException("Attempt to set value in non-table");
                        }
                        break;

                    case OpCode.OP_NEWTABLE:
                        frame.Registers[a] = LuaValue.CreateTable(new LuaTable());
                        break;

                    case OpCode.OP_SELF:
                        LuaValue obj = GetRKValue(frame, b);
                        frame.Registers[a + 1] = obj;
                        
                        if (obj.Type == LuaType.LUA_TTABLE)
                        {
                            LuaTable objTable = (LuaTable)obj.Value;
                            LuaValue key = GetRKValue(frame, c);
                            frame.Registers[a] = objTable[key];
                        }
                        else
                        {
                            frame.Registers[a] = LuaValue.Nil;
                        }
                        break;

                    case OpCode.OP_ADD:
                        frame.Registers[a] = ArithmeticOp(GetRKValue(frame, b), GetRKValue(frame, c), 
                            (x, y) => LuaValue.CreateNumber(x.ToNumber() + y.ToNumber()));
                        break;

                    case OpCode.OP_SUB:
                        frame.Registers[a] = ArithmeticOp(GetRKValue(frame, b), GetRKValue(frame, c),
                            (x, y) => LuaValue.CreateNumber(x.ToNumber() - y.ToNumber()));
                        break;

                    case OpCode.OP_MUL:
                        frame.Registers[a] = ArithmeticOp(GetRKValue(frame, b), GetRKValue(frame, c),
                            (x, y) => LuaValue.CreateNumber(x.ToNumber() * y.ToNumber()));
                        break;

                    case OpCode.OP_DIV:
                        frame.Registers[a] = ArithmeticOp(GetRKValue(frame, b), GetRKValue(frame, c),
                            (x, y) => LuaValue.CreateNumber(x.ToNumber() / y.ToNumber()));
                        break;

                    case OpCode.OP_MOD:
                        frame.Registers[a] = ArithmeticOp(GetRKValue(frame, b), GetRKValue(frame, c),
                            (x, y) => LuaValue.CreateNumber(x.ToNumber() % y.ToNumber()));
                        break;

                    case OpCode.OP_POW:
                        frame.Registers[a] = ArithmeticOp(GetRKValue(frame, b), GetRKValue(frame, c),
                            (x, y) => LuaValue.CreateNumber(Math.Pow(x.ToNumber(), y.ToNumber())));
                        break;

                    case OpCode.OP_UNM:
                        LuaValue un = GetRKValue(frame, b);
                        frame.Registers[a] = LuaValue.CreateNumber(-un.ToNumber());
                        break;

                    case OpCode.OP_NOT:
                        LuaValue notVal = GetRKValue(frame, b);
                        frame.Registers[a] = LuaValue.CreateBoolean(!notVal.ToBoolean());
                        break;

                    case OpCode.OP_LEN:
                        LuaValue lenVal = GetRKValue(frame, b);
                        if (lenVal.Type == LuaType.LUA_TTABLE)
                        {
                            LuaTable t = (LuaTable)lenVal.Value;
                            frame.Registers[a] = LuaValue.CreateNumber(t.Length);
                        }
                        else if (lenVal.Type == LuaType.LUA_TSTRING)
                        {
                            string s = (string)lenVal.Value;
                            frame.Registers[a] = LuaValue.CreateNumber(s.Length);
                        }
                        else
                        {
                            throw new LuaException("Attempt to get length of non-table/string");
                        }
                        break;

                    case OpCode.OP_CONCAT:
                        LuaValue concatResult = LuaValue.CreateString("");
                        for (int i = b; i <= c; i++)
                        {
                            LuaValue val = frame.Registers[i];
                            string current = concatResult.ToStringValue();
                            string next = val.ToStringValue();
                            concatResult = LuaValue.CreateString(current + next);
                        }
                        frame.Registers[a] = concatResult;
                        break;

                    case OpCode.OP_JMP:
                        frame.PC += sbx;
                        break;

                    case OpCode.OP_EQ:
                        LuaValue eqA = GetRKValue(frame, b);
                        LuaValue eqB = GetRKValue(frame, c);
                        bool eqResult = eqA == eqB;
                        if ((b != 0) == eqResult)
                            frame.PC++;
                        break;

                    case OpCode.OP_LT:
                        LuaValue ltA = GetRKValue(frame, b);
                        LuaValue ltB = GetRKValue(frame, c);
                        bool ltResult = ltA.ToNumber() < ltB.ToNumber();
                        if ((b != 0) == ltResult)
                            frame.PC++;
                        break;

                    case OpCode.OP_LE:
                        LuaValue leA = GetRKValue(frame, b);
                        LuaValue leB = GetRKValue(frame, c);
                        bool leResult = leA.ToNumber() <= leB.ToNumber();
                        if ((b != 0) == leResult)
                            frame.PC++;
                        break;

                    case OpCode.OP_TEST:
                        LuaValue testVal = frame.Registers[a];
                        if (testVal.ToBoolean() == (c != 0))
                            frame.PC++;
                        break;

                    case OpCode.OP_TESTSET:
                        LuaValue testSetVal = frame.Registers[b];
                        if (testSetVal.ToBoolean() == (c != 0))
                        {
                            frame.Registers[a] = testSetVal;
                            frame.PC++;
                        }
                        break;

                    case OpCode.OP_CALL:
                        int callNArgs = b - 1;
                        int callNResults = c - 1;
                        
                        if (callNArgs < 0) callNArgs = _state.GetTop() - frame.Base - a - 1;
                        
                        ExecuteCall(a, callNArgs, callNResults);
                        break;

                    case OpCode.OP_TAILCALL:
                        int tailNArgs = b - 1;
                        if (tailNArgs < 0) tailNArgs = _state.GetTop() - frame.Base - a - 1;
                        
                        ExecuteTailCall(a, tailNArgs);
                        break;

                    case OpCode.OP_RETURN:
                        int returnCount = b - 1;
                        if (returnCount < 0) returnCount = _state.GetTop() - frame.Base - a;
                        
                        return DoReturn(a, returnCount);

                    case OpCode.OP_FORLOOP:
                        double forIndex = frame.Registers[a].ToNumber();
                        double forLimit = frame.Registers[a + 1].ToNumber();
                        double forStep = frame.Registers[a + 2].ToNumber();
                        
                        forIndex += forStep;
                        frame.Registers[a] = LuaValue.CreateNumber(forIndex);
                        
                        if ((forStep > 0 && forIndex <= forLimit) || 
                            (forStep < 0 && forIndex >= forLimit))
                        {
                            frame.Registers[a + 3] = LuaValue.CreateNumber(forIndex);
                            frame.PC += sbx;
                        }
                        break;

                    case OpCode.OP_FORPREP:
                        double init = frame.Registers[a].ToNumber();
                        double limit = frame.Registers[a + 1].ToNumber();
                        double step = frame.Registers[a + 2].ToNumber();
                        
                        frame.Registers[a] = LuaValue.CreateNumber(init - step);
                        frame.PC += sbx;
                        break;

                    case OpCode.OP_TFORLOOP:
                        LuaValue iteratorFunc = frame.Registers[a];
                        LuaValue iteratorState = frame.Registers[a + 1];
                        LuaValue controlVar = frame.Registers[a + 2];
                        
                        _state.Push(iteratorFunc);
                        _state.Push(iteratorState);
                        _state.Push(controlVar);
                        _state.Call(2, c);
                        
                        LuaValue result = _state.Pop();
                        frame.Registers[a + 2] = result;
                        
                        if (result.Type != LuaType.LUA_TNIL)
                            frame.PC++;
                        break;

                    case OpCode.OP_SETLIST:
                        int listTableIndex = a;
                        int elementsPerBatch = c;
                        int lastBatchIndex = bx;
                        
                        if (elementsPerBatch == 0)
                            elementsPerBatch = _state.GetTop() - frame.Base - listTableIndex - 1;
                        
                        LuaTable listTable = (LuaTable)frame.Registers[listTableIndex].Value;
                        int baseIdx = (lastBatchIndex - 1) * elementsPerBatch;
                        
                        for (int i = 1; i <= elementsPerBatch; i++)
                        {
                            if (a + i < frame.Registers.Length)
                            {
                                listTable[LuaValue.CreateNumber(baseIdx + i)] = frame.Registers[a + i];
                            }
                        }
                        break;

                    case OpCode.OP_CLOSE:
                        CloseUpvalues(a);
                        break;

                    case OpCode.OP_CLOSURE:
                        LuaFunction closure = prototype.Functions[bx];
                        LuaValue[] newUpvalues = new LuaValue[closure.Prototype.NumUpvalues];
                        
                        for (int i = 0; i < newUpvalues.Length; i++)
                        {
                            Instruction nextInstr = code[frame.PC++];
                            if (nextInstr.OpCode == OpCode.OP_MOVE)
                            {
                                newUpvalues[i] = frame.Registers[nextInstr.A];
                            }
                            else if (nextInstr.OpCode == OpCode.OP_GETUPVAL)
                            {
                                newUpvalues[i] = upvalues[nextInstr.A];
                            }
                        }
                        
                        var newFunc = new LuaFunction
                        {
                            Prototype = closure.Prototype,
                            Upvalues = newUpvalues,
                            Name = closure.Name
                        };
                        frame.Registers[a] = LuaValue.CreateFunction(newFunc);
                        break;

                    case OpCode.OP_VARARG:
                        int varArgCount = b - 1;
                        if (varArgCount < 0) varArgCount = frame.VarArgCount;
                        
                        for (int i = 0; i < varArgCount; i++)
                        {
                            if (i < frame.VarArgs.Length)
                                frame.Registers[a + i] = frame.VarArgs[i];
                            else
                                frame.Registers[a + i] = LuaValue.Nil;
                        }
                        break;

                    default:
                        throw new LuaException($"Unknown opcode: {op}");
                }
            }

            return 0;
        }

        private LuaValue GetRKValue(CallFrame frame, int rk)
        {
            if ((rk & 0x100) != 0)
            {
                // Константа
                return frame.Function.Prototype.Constants[rk & 0xFF];
            }
            else
            {
                // Регистр
                return frame.Registers[rk];
            }
        }

        private LuaValue ArithmeticOp(LuaValue left, LuaValue right, Func<LuaValue, LuaValue, LuaValue> operation)
        {
            try
            {
                return operation(left, right);
            }
            catch
            {
                throw new LuaException("Arithmetic error");
            }
        }

        private void ExecuteCall(int funcIndex, int nargs, int nresults)
        {
            LuaValue func = CurrentFrame.Registers[funcIndex];
            
            if (func.Type != LuaType.LUA_TFUNCTION)
                throw new LuaException("Attempt to call non-function");

            LuaFunction luaFunc = (LuaFunction)func.Value;

            // Перемещение аргументов на стек состояния
            for (int i = 0; i < nargs; i++)
            {
                _state.Push(CurrentFrame.Registers[funcIndex + 1 + i]);
            }

            if (luaFunc.IsNative)
            {
                int results = luaFunc.NativeFunc(_state);
                
                // Получение результатов со стека
                for (int i = 0; i < results && i < nresults; i++)
                {
                    CurrentFrame.Registers[funcIndex + i] = _state.Pop();
                }
            }
            else
            {
                // Рекурсивный вызов для вложенных функций
                ExecuteFunction(luaFunc, nargs, nresults);
            }
        }

        private void ExecuteTailCall(int funcIndex, int nargs)
        {
            // Tail call оптимизация - заменяет текущий фрейм
            LuaValue func = CurrentFrame.Registers[funcIndex];
            
            if (func.Type != LuaType.LUA_TFUNCTION)
                throw new LuaException("Attempt to call non-function");

            // Подготовка нового вызова
            PopCallFrame();
            ExecuteCall(funcIndex, nargs, -1);
        }

        private int DoReturn(int retBase, int retCount)
        {
            // Сохранение возвращаемых значений
            LuaValue[] returns = new LuaValue[retCount];
            for (int i = 0; i < retCount; i++)
            {
                returns[i] = CurrentFrame.Registers[retBase + i];
            }

            PopCallFrame();

            if (_frameIndex >= 0)
            {
                // Возврат в вызывающую функцию
                var callerFrame = CurrentFrame;
                for (int i = 0; i < retCount; i++)
                {
                    callerFrame.Registers[callerFrame.ReturnBase + i] = returns[i];
                }
            }

            return retCount;
        }

        private void CloseUpvalues(int level)
        {
            // Закрытие upvalues на указанном уровне
            // В Lua 5.1 upvalues закрываются при выходе из области видимости
            // Здесь упрощенная реализация - в полной версии нужно отслеживать открытые upvalues
        }

        private void PushCallFrame(LuaFunction func, int nargs)
        {
            if (_frameIndex >= MaxFrames - 1)
                throw new LuaException("Stack overflow");

            _frameIndex++;
            var frame = _callStack[_frameIndex];
            frame.Function = func;
            frame.Base = _state.GetTop();
            frame.Registers = new LuaValue[func.Prototype.MaxStackSize];
            frame.ReturnBase = nargs + 1;
            frame.VarArgs = new LuaValue[0];
            frame.VarArgCount = 0;
            
            _callStack[_frameIndex] = frame;
        }

        private void PopCallFrame()
        {
            if (_frameIndex < 0)
                throw new LuaException("Stack underflow");
            
            _frameIndex--;
        }

        private CallFrame CurrentFrame
        {
            get
            {
                if (_frameIndex < 0)
                    throw new LuaException("No active call frame");
                return _callStack[_frameIndex];
            }
        }

        private struct CallFrame
        {
            public LuaFunction Function;
            public int Base;
            public LuaValue[] Registers;
            public int ReturnBase;
            public LuaValue[] VarArgs;
            public int VarArgCount;
            public int PC;
        }
    }
}
