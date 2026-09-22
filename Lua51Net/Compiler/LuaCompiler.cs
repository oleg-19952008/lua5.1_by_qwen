using System;
using System.Collections.Generic;
using Lua51Net.Core;

namespace Lua51Net.Compiler
{
    /// <summary>
    /// Парсер и компилятор Lua в байт-код
    /// Преобразует токены в AST, затем генерирует байт-код Lua 5.1
    /// </summary>
    public class LuaCompiler
    {
        private readonly LuaLexer _lexer;
        private Token _current;
        private readonly List<Instruction> _instructions;
        private readonly List<LuaValue> _constants;
        private readonly List<LuaFunction> _functions;
        private int _lineNumber;

        public LuaCompiler(string source)
        {
            _lexer = new LuaLexer(source);
            _instructions = new List<Instruction>();
            _constants = new List<LuaValue>();
            _functions = new List<LuaFunction>();
            _lineNumber = 1;
            NextToken();
        }

        public LuaPrototype Compile()
        {
            var prototype = ParseChunk();
            return prototype;
        }

        private void NextToken()
        {
            _current = _lexer.NextToken();
            _lineNumber = _current.Line;
        }

        private bool Match(TokenType type)
        {
            if (_current.Type == type)
            {
                NextToken();
                return true;
            }
            return false;
        }

        private void Expect(TokenType type)
        {
            if (_current.Type != type)
                throw new LuaException($"Expected {type}, got {_current.Type} at line {_lineNumber}");
            NextToken();
        }

        private LuaPrototype ParseChunk()
        {
            var prototype = new LuaPrototype
            {
                Source = "chunk",
                LineDefined = _lineNumber,
                NumParameters = 0,
                IsVarArg = 0,
                MaxStackSize = 2
            };

            ParseBlock();

            // Добавляем RETURN в конце
            Emit(Instruction.Create(OpCode.OP_RETURN, 0, 1, 0));

            prototype.Code = _instructions.ToArray();
            prototype.Constants = _constants.ToArray();
            prototype.Functions = _functions.ToArray();
            prototype.LineInfo = new int[prototype.Code.Length];

            return prototype;
        }

        private void ParseBlock()
        {
            while (!IsBlockEnd())
            {
                ParseStatement();
            }
        }

        private bool IsBlockEnd()
        {
            return _current.Type == TokenType.END || 
                   _current.Type == TokenType.ELSE || 
                   _current.Type == TokenType.ELSEIF ||
                   _current.Type == TokenType.UNTIL ||
                   _current.Type == TokenType.EOS;
        }

        private void ParseStatement()
        {
            switch (_current.Type)
            {
                case TokenType.IF:
                    ParseIf();
                    break;
                case TokenType.WHILE:
                    ParseWhile();
                    break;
                case TokenType.DO:
                    NextToken();
                    ParseBlock();
                    Expect(TokenType.END);
                    break;
                case TokenType.FOR:
                    ParseFor();
                    break;
                case TokenType.REPEAT:
                    ParseRepeat();
                    break;
                case TokenType.FUNCTION:
                    ParseFunctionDef();
                    break;
                case TokenType.LOCAL:
                    ParseLocal();
                    break;
                case TokenType.BREAK:
                    NextToken();
                    // Обработка break
                    break;
                case TokenType.RETURN:
                    ParseReturn();
                    break;
                default:
                    ParseAssignmentOrCall();
                    break;
            }
        }

        private void ParseIf()
        {
            Expect(TokenType.IF);
            ParseExpression(); // Условие
            Expect(TokenType.THEN);

            int jumpIfFalse = _instructions.Count;
            Emit(Instruction.Create(OpCode.OP_JMP, 0, 0, 1)); // Заглушка

            ParseBlock();

            int jumpToEnd = -1;
            if (Match(TokenType.ELSEIF))
            {
                jumpToEnd = _instructions.Count;
                Emit(Instruction.Create(OpCode.OP_JMP, 0, 0, 1)); // Заглушка
                _instructions[jumpIfFalse] = Instruction.CreateABx(OpCode.OP_JMP, 0, _instructions.Count - jumpIfFalse - 1);
                
                ParseIf(); // Рекурсивно для elseif
                
                int finalJump = _instructions.Count;
                Emit(Instruction.Create(OpCode.OP_JMP, 0, 0, 1));
                _instructions[jumpToEnd] = Instruction.CreateABx(OpCode.OP_JMP, 0, _instructions.Count - jumpToEnd - 1);
                return;
            }

            if (Match(TokenType.ELSE))
            {
                jumpToEnd = _instructions.Count;
                Emit(Instruction.Create(OpCode.OP_JMP, 0, 0, 1)); // Заглушка
                _instructions[jumpIfFalse] = Instruction.CreateABx(OpCode.OP_JMP, 0, _instructions.Count - jumpIfFalse - 1);
                
                ParseBlock();
                
                int finalJump = _instructions.Count;
                Emit(Instruction.Create(OpCode.OP_JMP, 0, 0, 1));
                _instructions[jumpToEnd] = Instruction.CreateABx(OpCode.OP_JMP, 0, _instructions.Count - jumpToEnd - 1);
            }
            else
            {
                _instructions[jumpIfFalse] = Instruction.CreateABx(OpCode.OP_JMP, 0, _instructions.Count - jumpIfFalse - 1);
            }

            Expect(TokenType.END);
        }

        private void ParseWhile()
        {
            Expect(TokenType.WHILE);
            
            int loopStart = _instructions.Count;
            ParseExpression(); // Условие
            
            int jumpIfFalse = _instructions.Count;
            Emit(Instruction.Create(OpCode.OP_JMP, 0, 0, 1)); // Заглушка

            ParseBlock();
            
            // Jump back to condition
            Emit(Instruction.CreateABx(OpCode.OP_JMP, 0, loopStart - _instructions.Count - 1));
            
            _instructions[jumpIfFalse] = Instruction.CreateABx(OpCode.OP_JMP, 0, _instructions.Count - jumpIfFalse - 1);
            
            Expect(TokenType.END);
        }

        private void ParseFor()
        {
            Expect(TokenType.FOR);
            
            string varName = ((Token)_current).Value.ToString();
            Expect(TokenType.IDENTIFIER);
            Expect(TokenType.ASSIGN); // =
            
            ParseExpression(); // Start
            Expect(TokenType.COMMA);
            ParseExpression(); // Limit
            Expect(TokenType.COMMA);
            ParseExpression(); // Step
            
            Expect(TokenType.DO);

            int forPrep = _instructions.Count;
            Emit(Instruction.CreateABx(OpCode.OP_FORPREP, 0, 1)); // Заглушка

            ParseBlock();

            int forLoop = _instructions.Count;
            Emit(Instruction.CreateABx(OpCode.OP_FORLOOP, 0, forPrep - forLoop - 1));

            _instructions[forPrep] = Instruction.CreateABx(OpCode.OP_FORPREP, 0, forLoop - forPrep - 1);

            Expect(TokenType.END);
        }

        private void ParseRepeat()
        {
            Expect(TokenType.REPEAT);
            
            int repeatStart = _instructions.Count;
            ParseBlock();
            
            Expect(TokenType.UNTIL);
            ParseExpression(); // Условие
            
            // Если условие ложно, прыгаем назад
            Emit(Instruction.CreateABx(OpCode.OP_JMP, 0, repeatStart - _instructions.Count - 1));
        }

        private void ParseFunctionDef()
        {
            Expect(TokenType.FUNCTION);
            
            string funcName = null;
            bool isMethod = false;
            
            if (_current.Type == TokenType.IDENTIFIER)
            {
                funcName = ((Token)_current).Value.ToString();
                NextToken();
            }
            
            if (Match(TokenType.DOT))
            {
                funcName = ((Token)_current).Value.ToString();
                NextToken();
            }
            else if (Match(TokenType.COLON))
            {
                isMethod = true;
                funcName = ((Token)_current).Value.ToString();
                NextToken();
            }

            Expect(TokenType.LPAREN);
            
            var parameters = new List<string>();
            if (isMethod)
                parameters.Add("self");
            
            while (_current.Type != TokenType.RPAREN && _current.Type != TokenType.EOS)
            {
                if (_current.Type == TokenType.IDENTIFIER)
                {
                    parameters.Add(((Token)_current).Value.ToString());
                    NextToken();
                }
                else if (_current.Type == TokenType.COMMA)
                {
                    NextToken();
                }
                else if (_current.Type == TokenType.DOTS)
                {
                    // Vararg
                    NextToken();
                    break;
                }
                else
                {
                    break;
                }
            }
            
            Expect(TokenType.RPAREN);
            
            // Компиляция тела функции будет добавлена отдельно
            ParseBlock();
            Expect(TokenType.END);
        }

        private void ParseLocal()
        {
            Expect(TokenType.LOCAL);
            
            if (_current.Type == TokenType.FUNCTION)
            {
                ParseFunctionDef();
            }
            else
            {
                // Local variables
                while (_current.Type == TokenType.IDENTIFIER)
                {
                    NextToken();
                    if (Match(TokenType.COMMA))
                        continue;
                    break;
                }
                
                if (Match(TokenType.ASSIGN))
                {
                    ParseExpression();
                }
            }
        }

        private void ParseReturn()
        {
            Expect(TokenType.RETURN);
            
            if (_current.Type != TokenType.END && 
                _current.Type != TokenType.ELSE && 
                _current.Type != TokenType.ELSEIF &&
                _current.Type != TokenType.UNTIL &&
                _current.Type != TokenType.EOS)
            {
                ParseExpression();
            }
            
            Emit(Instruction.Create(OpCode.OP_RETURN, 0, 1, 0));
        }

        private void ParseAssignmentOrCall()
        {
            // Упрощенная реализация присваивания
            if (_current.Type == TokenType.IDENTIFIER)
            {
                string name = ((Token)_current).Value.ToString();
                NextToken();
                
                if (Match(TokenType.ASSIGN))
                {
                    ParseExpression();
                    // SETGLOBAL
                    int constIdx = AddConstant(LuaValue.CreateString(name));
                    Emit(Instruction.CreateABx(OpCode.OP_SETGLOBAL, 0, constIdx));
                }
                else
                {
                    // Function call
                    ParseFunctionCall(name);
                }
            }
        }

        private void ParseFunctionCall(string name)
        {
            Expect(TokenType.LPAREN);
            
            int nargs = 0;
            while (_current.Type != TokenType.RPAREN && _current.Type != TokenType.EOS)
            {
                ParseExpression();
                nargs++;
                if (Match(TokenType.COMMA))
                    continue;
                break;
            }
            
            Expect(TokenType.RPAREN);
            
            // CALL instruction
            Emit(Instruction.Create(OpCode.OP_CALL, 0, nargs + 1, 1));
        }

        private void ParseExpression()
        {
            ParsePrimaryExpression();
            
            // Обработка операторов приоритета
            while (true)
            {
                OpCode? op = null;
                
                switch (_current.Type)
                {
                    case TokenType.PLUS: op = OpCode.OP_ADD; break;
                    case TokenType.MINUS: op = OpCode.OP_SUB; break;
                    case TokenType.MUL: op = OpCode.OP_MUL; break;
                    case TokenType.DIV: op = OpCode.OP_DIV; break;
                    case TokenType.MOD: op = OpCode.OP_MOD; break;
                    case TokenType.POW: op = OpCode.OP_POW; break;
                    case TokenType.CONCAT: op = OpCode.OP_CONCAT; break;
                    case TokenType.LT: op = OpCode.OP_LT; break;
                    case TokenType.GT: op = OpCode.OP_LT; break;
                    case TokenType.LE: op = OpCode.OP_LE; break;
                    case TokenType.GE: op = OpCode.OP_LE; break;
                    case TokenType.EQ: op = OpCode.OP_EQ; break;
                    case TokenType.NE: op = OpCode.OP_EQ; break;
                    default:
                        return;
                }
                
                NextToken();
                ParsePrimaryExpression();
                
                // Emit arithmetic instruction
                Emit(Instruction.Create((OpCode)op.Value, 0, 0, 0));
            }
        }

        private void ParsePrimaryExpression()
        {
            switch (_current.Type)
            {
                case TokenType.NUMBER:
                    double num = (double)((Token)_current).Value;
                    int constIdx = AddConstant(LuaValue.CreateNumber(num));
                    Emit(Instruction.CreateABx(OpCode.OP_LOADK, 0, constIdx));
                    NextToken();
                    break;
                    
                case TokenType.STRING:
                    string str = (string)((Token)_current).Value;
                    constIdx = AddConstant(LuaValue.CreateString(str));
                    Emit(Instruction.CreateABx(OpCode.OP_LOADK, 0, constIdx));
                    NextToken();
                    break;
                    
                case TokenType.TRUE:
                    Emit(Instruction.Create(OpCode.OP_LOADBOOL, 0, 1, 0));
                    NextToken();
                    break;
                    
                case TokenType.FALSE:
                    Emit(Instruction.Create(OpCode.OP_LOADBOOL, 0, 0, 0));
                    NextToken();
                    break;
                    
                case TokenType.NIL:
                    Emit(Instruction.Create(OpCode.OP_LOADNIL, 0, 0, 0));
                    NextToken();
                    break;
                    
                case TokenType.IDENTIFIER:
                    string ident = (string)((Token)_current).Value;
                    NextToken();
                    // GETGLOBAL
                    constIdx = AddConstant(LuaValue.CreateString(ident));
                    Emit(Instruction.CreateABx(OpCode.OP_GETGLOBAL, 0, constIdx));
                    break;
                    
                case TokenType.LPAREN:
                    NextToken();
                    ParseExpression();
                    Expect(TokenType.RPAREN);
                    break;
                    
                case TokenType.LBRACE:
                    ParseTableConstructor();
                    break;
                    
                case TokenType.FUNCTION:
                    ParseInlineFunction();
                    break;
                    
                default:
                    throw new LuaException($"Unexpected token in expression: {_current.Type} at line {_lineNumber}");
            }
        }

        private void ParseTableConstructor()
        {
            Expect(TokenType.LBRACE);
            
            Emit(Instruction.Create(OpCode.OP_NEWTABLE, 0, 0, 0));
            
            while (_current.Type != TokenType.RBRACE && _current.Type != TokenType.EOS)
            {
                if (_current.Type == TokenType.IDENTIFIER || _current.Type == TokenType.STRING)
                {
                    // Key-value pair
                    ParseExpression(); // Key
                    Expect(TokenType.ASSIGN);
                    ParseExpression(); // Value
                    Emit(Instruction.Create(OpCode.OP_SETTABLE, 0, 0, 0));
                }
                else
                {
                    // Array element
                    ParseExpression();
                    Emit(Instruction.Create(OpCode.OP_SETTABLE, 0, 0, 0));
                }
                
                if (Match(TokenType.COMMA) || Match(TokenType.SEMICOLON))
                    continue;
                break;
            }
            
            Expect(TokenType.RBRACE);
        }

        private void ParseInlineFunction()
        {
            Expect(TokenType.FUNCTION);
            Expect(TokenType.LPAREN);
            
            var parameters = new List<string>();
            while (_current.Type != TokenType.RPAREN && _current.Type != TokenType.EOS)
            {
                if (_current.Type == TokenType.IDENTIFIER)
                {
                    parameters.Add(((Token)_current).Value.ToString());
                    NextToken();
                }
                else if (_current.Type == TokenType.COMMA)
                {
                    NextToken();
                }
                else
                {
                    break;
                }
            }
            
            Expect(TokenType.RPAREN);
            
            // Сохраняем текущее состояние и создаем новую функцию
            var savedInstructions = _instructions;
            var savedConstants = _constants;
            
            _instructions = new List<Instruction>();
            _constants = new List<LuaValue>();
            
            ParseBlock();
            Expect(TokenType.END);
            
            Emit(Instruction.Create(OpCode.OP_RETURN, 0, 1, 0));
            
            var funcPrototype = new LuaPrototype
            {
                Code = _instructions.ToArray(),
                Constants = _constants.ToArray(),
                Functions = new LuaFunction[0],
                NumParameters = (byte)parameters.Count,
                MaxStackSize = 2
            };
            
            var luaFunc = new LuaFunction
            {
                Prototype = funcPrototype,
                Name = "anonymous"
            };
            
            _functions.Add(luaFunc);
            
            // Восстанавливаем состояние
            _instructions = savedInstructions;
            _constants = savedConstants;
            
            int funcIdx = _functions.Count - 1;
            Emit(Instruction.CreateABx(OpCode.OP_CLOSURE, 0, funcIdx));
        }

        private int AddConstant(LuaValue value)
        {
            _constants.Add(value);
            return _constants.Count - 1;
        }

        private void Emit(Instruction instr)
        {
            _instructions.Add(instr);
        }
    }
}
