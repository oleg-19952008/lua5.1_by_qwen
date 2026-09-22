using System;
using System.Text;
using Lua51Net.Core;

namespace Lua51Net.Compiler
{
    /// <summary>
    /// Лексер для токенизации исходного кода Lua 5.1
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
            SkipWhitespaceAndComments();

            if (_pos >= _source.Length)
                return new Token(TokenType.EOS, null, _line, _column);

            char c = CurrentChar;

            // Числа
            if (char.IsDigit(c) || (c == '.' && _pos + 1 < _source.Length && char.IsDigit(_source[_pos + 1])))
                return ReadNumber();

            // Идентификаторы и ключевые слова (с поддержкой UTF-8)
            if (char.IsLetter(c) || c == '_' || IsUtf8Letter(c))
                return ReadIdentifier();

            // Строки (обычные и длинные)
            if (c == '"' || c == '\'')
                return ReadString();
            
            // Длинные строки и комментарии [[ ... ]]
            if (c == '[')
            {
                int lookahead = _pos + 1;
                int level = 0;
                
                // Проверка на длинную конструкцию [=*[
                while (lookahead < _source.Length && _source[lookahead] == '=')
                    lookahead++;
                
                if (lookahead < _source.Length && _source[lookahead] == '[')
                {
                    // Это длинная конструкция, вычисляем уровень
                    level = lookahead - _pos - 1;
                    return ReadLongString(level);
                }
            }

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

            // Hexadecimal numbers: 0x...
            if (_pos + 1 < _source.Length && CurrentChar == '0' && (CurrentChar == 'x' || CurrentChar == 'X'))
            {
                Advance(2);
                while (_pos < _source.Length && (char.IsDigit(CurrentChar) || 
                    (CurrentChar >= 'a' && CurrentChar <= 'f') || (CurrentChar >= 'A' && CurrentChar <= 'F')))
                {
                    Advance();
                }
            }
            else
            {
                while (_pos < _source.Length && (char.IsDigit(CurrentChar) || CurrentChar == '.'))
                {
                    Advance();
                }
                
                // Exponent part
                if (_pos < _source.Length && (CurrentChar == 'e' || CurrentChar == 'E'))
                {
                    Advance();
                    if (_pos < _source.Length && (CurrentChar == '+' || CurrentChar == '-'))
                        Advance();
                    while (_pos < _source.Length && char.IsDigit(CurrentChar))
                        Advance();
                }
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
            
            StringBuilder sb = new StringBuilder();

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
                            case 'n': sb.Append('\n'); Advance(); break;
                            case 't': sb.Append('\t'); Advance(); break;
                            case 'r': sb.Append('\r'); Advance(); break;
                            case '\\': sb.Append('\\'); Advance(); break;
                            case '"': sb.Append('"'); Advance(); break;
                            case '\'': sb.Append('\''); Advance(); break;
                            case 'a': sb.Append('\a'); Advance(); break;
                            case 'b': sb.Append('\b'); Advance(); break;
                            case 'f': sb.Append('\f'); Advance(); break;
                            case 'v': sb.Append('\v'); Advance(); break;
                            case 'z': // Skip whitespace after \z
                                Advance();
                                while (_pos < _source.Length && char.IsWhiteSpace(CurrentChar))
                                    Advance();
                                break;
                            case '\n': // Line continuation
                                _line++;
                                _column = 0;
                                Advance();
                                break;
                            default:
                                // Decimal escape: \ddd (up to 3 digits, max 255)
                                if (char.IsDigit(escaped))
                                {
                                    int numDigits = 1;
                                    int digitValue = escaped - '0';
                                    Advance();
                                    
                                    while (numDigits < 3 && _pos < _source.Length && char.IsDigit(CurrentChar))
                                    {
                                        int nextDigit = CurrentChar - '0';
                                        int newValue = digitValue * 10 + nextDigit;
                                        if (newValue > 255) break;
                                        digitValue = newValue;
                                        numDigits++;
                                        Advance();
                                    }
                                    sb.Append((char)digitValue);
                                }
                                else
                                {
                                    sb.Append(escaped);
                                    Advance();
                                }
                                break;
                        }
                    }
                }
                else
                {
                    if (CurrentChar == '\n')
                    {
                        _line++;
                        _column = 0;
                    }
                    sb.Append(CurrentChar);
                    Advance();
                }
            }

            if (_pos >= _source.Length)
                throw new LuaException($"Unterminated string starting at line {startLine}");

            Advance(); // Закрывающая кавычка

            return new Token(TokenType.STRING, sb.ToString(), startLine, startCol);
        }

        private void SkipWhitespaceAndComments()
        {
            while (_pos < _source.Length)
            {
                // Skip whitespace
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
                
                // Check for comment
                if (_pos + 1 < _source.Length && CurrentChar == '-' && _source[_pos + 1] == '-')
                {
                    SkipComment();
                }
                else
                {
                    break;
                }
            }
        }

        private void SkipComment()
        {
            // We're already at '--'
            Advance(2);
            
            // Check for long comment --[=[ ... ]=]
            if (_pos < _source.Length && CurrentChar == '[')
            {
                int lookahead = _pos + 1;
                int level = 0;
                
                // Count '=' characters
                while (lookahead < _source.Length && _source[lookahead] == '=')
                    lookahead++;
                
                // Check for closing '['
                if (lookahead < _source.Length && _source[lookahead] == '[')
                {
                    level = lookahead - _pos - 1;
                    Advance(); // Skip opening '['
                    
                    // Skip any '=' after '['
                    for (int i = 0; i < level; i++)
                        Advance();
                    Advance(); // Skip the final '['
                    
                    // Now skip until we find ]=...]= with matching level
                    while (_pos < _source.Length)
                    {
                        if (CurrentChar == ']')
                        {
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
                    return;
                }
            }
            
            // Single-line comment: skip until end of line
            while (_pos < _source.Length && CurrentChar != '\n')
            {
                Advance();
            }
        }

        private Token ReadLongString(int level)
        {
            int startLine = _line;
            int startCol = _column;
            
            // Skip opening [ and =s and [
            Advance(); // Skip '['
            for (int i = 0; i < level; i++)
                Advance(); // Skip '='s
            Advance(); // Skip final '['
            
            // Optional newline right after opening bracket should be skipped
            if (_pos < _source.Length && CurrentChar == '\n')
            {
                _line++;
                _column = 0;
                Advance();
            }
            
            StringBuilder sb = new StringBuilder();
            string closingBracket = "]" + new string('=', level) + "]";
            
            while (_pos < _source.Length)
            {
                // Check for closing bracket
                bool foundClosing = true;
                int checkPos = _pos;
                
                if (_source[checkPos] != ']')
                    foundClosing = false;
                else
                {
                    checkPos++;
                    for (int i = 0; i < level && checkPos < _source.Length; i++)
                    {
                        if (_source[checkPos] != '=')
                        {
                            foundClosing = false;
                            break;
                        }
                        checkPos++;
                    }
                    if (foundClosing && (checkPos >= _source.Length || _source[checkPos] != ']'))
                        foundClosing = false;
                }
                
                if (foundClosing)
                {
                    // Skip the closing bracket
                    Advance(); // ']'
                    for (int i = 0; i < level; i++)
                        Advance(); // '='s
                    Advance(); // final ']'
                    return new Token(TokenType.STRING, sb.ToString(), startLine, startCol);
                }
                
                if (CurrentChar == '\n')
                {
                    _line++;
                    _column = 0;
                }
                sb.Append(CurrentChar);
                Advance();
            }
            
            throw new LuaException($"Unterminated long string starting at line {startLine}");
        }

        private bool IsUtf8Letter(char c)
        {
            // Check if character is a Unicode letter (for UTF-8 identifier support)
            // In C#, char is UTF-16, but this handles basic multilingual plane
            UnicodeCategory category = char.GetUnicodeCategory(c);
            return category == UnicodeCategory.LowercaseLetter || 
                   category == UnicodeCategory.UppercaseLetter || 
                   category == UnicodeCategory.TitlecaseLetter || 
                   category == UnicodeCategory.ModifierLetter || 
                   category == UnicodeCategory.OtherLetter;
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
        LPAREN, RPAREN, LBRACE, RBRACE, LBRACKET, RBRACKET, SEMICOLON, COLON, COMMA, DOT, DOTS, LABEL
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
