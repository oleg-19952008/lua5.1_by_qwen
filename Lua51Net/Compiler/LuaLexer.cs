using System;
using System.IO;
using Lua51Net.Core;

namespace Lua51Net.Compiler
{
    /// <summary>
    /// Лексер для токенизации исходного кода Lua
    /// </summary>
    public class LuaLexer
    {
        private readonly string _source;
        private int _pos;
        private int _line;
        private int _column;

        public LuaLexer(string source)
        {
            _source = source;
            _pos = 0;
            _line = 1;
            _column = 0;
        }

        public Token NextToken()
        {
            SkipWhitespace();
            SkipComment();

            if (_pos >= _source.Length)
                return new Token(TokenType.EOS, null, _line, _column);

            char c = CurrentChar;

            // Числа
            if (char.IsDigit(c) || (c == '.' && _pos + 1 < _source.Length && char.IsDigit(_source[_pos + 1])))
                return ReadNumber();

            // Идентификаторы и ключевые слова
            if (char.IsLetter(c) || c == '_')
                return ReadIdentifier();

            // Строки
            if (c == '"' || c == '\'')
                return ReadString();

            // Двухсимвольные операторы
            if (_pos + 1 < _source.Length)
            {
                string two = _source.Substring(_pos, 2);
                switch (two)
                {
                    case "==": Advance(2); return new Token(TokenType.EQ, "==", _line, _column);
                    case "~=": Advance(2); return new Token(TokenType.NE, "~=", _line, _column);
                    case "<=": Advance(2); return new Token(TokenType.LE, "<=", _line, _column);
                    case ">=": Advance(2); return new Token(TokenType.GE, ">=", _line, _column);
                    case "..": Advance(2); return new Token(TokenType.CONCAT, "..", _line, _column);
                    case "::": Advance(2); return new Token(TokenType.LABEL, "::", _line, _column);
                }
            }

            // Односимвольные токены
            switch (c)
            {
                case '+': Advance(); return new Token(TokenType.PLUS, "+", _line, _column);
                case '-': Advance(); return new Token(TokenType.MINUS, "-", _line, _column);
                case '*': Advance(); return new Token(TokenType.MUL, "*", _line, _column);
                case '/': Advance(); return new Token(TokenType.DIV, "/", _line, _column);
                case '%': Advance(); return new Token(TokenType.MOD, "%", _line, _column);
                case '^': Advance(); return new Token(TokenType.POW, "^", _line, _column);
                case '#': Advance(); return new Token(TokenType.LEN, "#", _line, _column);
                case '<': Advance(); return new Token(TokenType.LT, "<", _line, _column);
                case '>': Advance(); return new Token(TokenType.GT, ">", _line, _column);
                case '=': Advance(); return new Token(TokenType.ASSIGN, "=", _line, _column);
                case '(': Advance(); return new Token(TokenType.LPAREN, "(", _line, _column);
                case ')': Advance(); return new Token(TokenType.RPAREN, ")", _line, _column);
                case '{': Advance(); return new Token(TokenType.LBRACE, "{", _line, _column);
                case '}': Advance(); return new Token(TokenType.RBRACE, "}", _line, _column);
                case '[': Advance(); return new Token(TokenType.LBRACKET, "[", _line, _column);
                case ']': Advance(); return new Token(TokenType.RBRACKET, "]", _line, _column);
                case ';': Advance(); return new Token(TokenType.SEMICOLON, ";", _line, _column);
                case ':': Advance(); return new Token(TokenType.COLON, ":", _line, _column);
                case ',': Advance(); return new Token(TokenType.COMMA, ",", _line, _column);
                case '.': Advance(); return new Token(TokenType.DOT, ".", _line, _column);
            }

            throw new LuaException($"Unexpected character '{c}' at line {_line}, column {_column}");
        }

        private Token ReadNumber()
        {
            int start = _pos;
            int startLine = _line;
            int startCol = _column;

            while (_pos < _source.Length && (char.IsDigit(CurrentChar) || CurrentChar == '.' || 
                   CurrentChar == 'e' || CurrentChar == 'E' || CurrentChar == '+' || CurrentChar == '-'))
            {
                Advance();
            }

            string numStr = _source.Substring(start, _pos - start);
            if (double.TryParse(numStr, out double value))
                return new Token(TokenType.NUMBER, value, startLine, startCol);
            
            throw new LuaException($"Invalid number '{numStr}' at line {startLine}");
        }

        private Token ReadIdentifier()
        {
            int start = _pos;
            int startLine = _line;
            int startCol = _column;

            while (_pos < _source.Length && (char.IsLetterOrDigit(CurrentChar) || CurrentChar == '_'))
            {
                Advance();
            }

            string ident = _source.Substring(start, _pos - start);

            // Ключевые слова Lua 5.1
            TokenType type = TokenType.IDENTIFIER;
            object value = ident;

            switch (ident)
            {
                case "and": type = TokenType.AND; break;
                case "break": type = TokenType.BREAK; break;
                case "do": type = TokenType.DO; break;
                case "else": type = TokenType.ELSE; break;
                case "elseif": type = TokenType.ELSEIF; break;
                case "end": type = TokenType.END; break;
                case "false": type = TokenType.FALSE; value = false; break;
                case "for": type = TokenType.FOR; break;
                case "function": type = TokenType.FUNCTION; break;
                case "if": type = TokenType.IF; break;
                case "in": type = TokenType.IN; break;
                case "local": type = TokenType.LOCAL; break;
                case "nil": type = TokenType.NIL; value = null; break;
                case "not": type = TokenType.NOT; break;
                case "or": type = TokenType.OR; break;
                case "repeat": type = TokenType.REPEAT; break;
                case "return": type = TokenType.RETURN; break;
                case "then": type = TokenType.THEN; break;
                case "true": type = TokenType.TRUE; value = true; break;
                case "until": type = TokenType.UNTIL; break;
                case "while": type = TokenType.WHILE; break;
            }

            return new Token(type, value, startLine, startCol);
        }

        private Token ReadString()
        {
            char quote = CurrentChar;
            Advance();

            int start = _pos;
            int startLine = _line;
            int startCol = _column;

            while (_pos < _source.Length && CurrentChar != quote)
            {
                if (CurrentChar == '\\')
                {
                    Advance();
                    if (_pos < _source.Length)
                    {
                        char escaped = CurrentChar;
                        switch (escaped)
                        {
                            case 'n': CurrentChar = '\n'; break;
                            case 't': CurrentChar = '\t'; break;
                            case 'r': CurrentChar = '\r'; break;
                            case '\\': break;
                            case '"': break;
                            case '\'': break;
                        }
                        Advance();
                    }
                }
                else
                {
                    if (CurrentChar == '\n')
                    {
                        _line++;
                        _column = 0;
                    }
                    Advance();
                }
            }

            if (_pos >= _source.Length)
                throw new LuaException($"Unterminated string starting at line {startLine}");

            Advance(); // Закрывающая кавычка

            string str = _source.Substring(start, _pos - start - 1);
            return new Token(TokenType.STRING, str, startLine, startCol);
        }

        private void SkipWhitespace()
        {
            while (_pos < _source.Length && char.IsWhiteSpace(CurrentChar))
            {
                if (CurrentChar == '\n')
                {
                    _line++;
                    _column = 0;
                }
                else
                {
                    _column++;
                }
                Advance();
            }
        }

        private void SkipComment()
        {
            if (_pos + 1 < _source.Length && CurrentChar == '-' && _source[_pos + 1] == '-')
            {
                Advance(2);
                
                // Многострочный комментарий
                if (_pos < _source.Length && CurrentChar == '[')
                {
                    Advance();
                    int level = 0;
                    if (_pos < _source.Length && CurrentChar == '=')
                    {
                        while (_pos < _source.Length && CurrentChar == '=')
                        {
                            level++;
                            Advance();
                        }
                        if (_pos < _source.Length && CurrentChar == '[')
                        {
                            Advance();
                            // Пропуск до ]=...]=
                            while (_pos < _source.Length)
                            {
                                if (CurrentChar == ']')
                                {
                                    int checkPos = _pos;
                                    Advance();
                                    int closeLevel = 0;
                                    while (_pos < _source.Length && CurrentChar == '=')
                                    {
                                        closeLevel++;
                                        Advance();
                                    }
                                    if (closeLevel == level && _pos < _source.Length && CurrentChar == ']')
                                    {
                                        Advance();
                                        return;
                                    }
                                }
                                else
                                {
                                    if (CurrentChar == '\n')
                                    {
                                        _line++;
                                        _column = 0;
                                    }
                                    Advance();
                                }
                            }
                        }
                    }
                }
                
                // Однострочный комментарий
                while (_pos < _source.Length && CurrentChar != '\n')
                {
                    Advance();
                }
            }
        }

        private char CurrentChar => _pos < _source.Length ? _source[_pos] : '\0';

        private void Advance(int count = 1)
        {
            for (int i = 0; i < count && _pos < _source.Length; i++)
            {
                _pos++;
                _column++;
            }
        }
    }

    /// <summary>
    /// Типы токенов Lua
    /// </summary>
    public enum TokenType
    {
        EOS,
        IDENTIFIER,
        NUMBER,
        STRING,
        
        // Ключевые слова
        AND, BREAK, DO, ELSE, ELSEIF, END, FALSE, FOR, FUNCTION, IF, IN, LOCAL, NIL, NOT, OR, REPEAT, RETURN, THEN, TRUE, UNTIL, WHILE,
        
        // Операторы
        PLUS, MINUS, MUL, DIV, MOD, POW, LEN, LT, GT, LE, GE, EQ, NE, ASSIGN, CONCAT,
        
        // Разделители
        LPAREN, RPAREN, LBRACE, RBRACE, LBRACKET, RBRACKET, SEMICOLON, COLON, COMMA, DOT, LABEL
    }

    /// <summary>
    /// Токен
    /// </summary>
    public class Token
    {
        public TokenType Type { get; }
        public object Value { get; }
        public int Line { get; }
        public int Column { get; }

        public Token(TokenType type, object value, int line, int column)
        {
            Type = type;
            Value = value;
            Line = line;
            Column = column;
        }

        public override string ToString()
        {
            return $"{Type}: {Value} ({Line}:{Column})";
        }
    }
}
