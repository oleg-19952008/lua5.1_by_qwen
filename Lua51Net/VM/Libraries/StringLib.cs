using System;
using System.Text;
using Lua51Net.Core;

namespace Lua51Net.VM.Libraries
{
    /// <summary>
    /// Библиотека string для Lua 5.1.4
    /// </summary>
    public static class StringLib
    {
        public static void Register(LuaState state)
        {
            // Создаем таблицу string
            state.NewTable();
            
            // Регистрируем функции
            RegisterFunction(state, "len", Len);
            RegisterFunction(state, "sub", Sub);
            RegisterFunction(state, "lower", Lower);
            RegisterFunction(state, "upper", Upper);
            RegisterFunction(state, "rep", Rep);
            RegisterFunction(state, "reverse", Reverse);
            RegisterFunction(state, "char", Char);
            RegisterFunction(state, "byte", Byte);
            RegisterFunction(state, "find", Find);
            RegisterFunction(state, "match", Match);
            RegisterFunction(state, "gsub", Gsub);
            RegisterFunction(state, "gmatch", Gmatch);
            RegisterFunction(state, "format", Format);
            RegisterFunction(state, "dump", Dump);
            
            // Устанавливаем в global
            state.SetGlobal("string");
        }

        private static void RegisterFunction(LuaState state, string name, LuaNativeFunction func)
        {
            state.PushCFunction(func);
            state.SetField(-2, name);
        }

        // string.len(s)
        private static int Len(LuaState state)
        {
            string s = state.ToString(1);
            if (s == null)
            {
                state.PushString("attempt to get length of nil value");
                return state.Error();
            }
            state.PushNumber(s.Length);
            return 1;
        }

        // string.sub(s, i [, j])
        private static int Sub(LuaState state)
        {
            string s = state.ToString(1) ?? "";
            double startDouble = state.ToNumber(2);
            int len = s.Length;
            
            int start = PosRelAt(startDouble, len);
            if (start < 1) start = 1;
            if (start > len + 1) start = len + 1;
            
            int end = len;
            if (state.GetTop() >= 3)
            {
                double endDouble = state.ToNumber(3);
                end = PosRelAt(endDouble, len);
                if (end < start - 1) end = start - 1;
            }
            
            if (start > len)
            {
                state.PushString("");
            }
            else if (end >= len)
            {
                state.PushString(s.Substring(start - 1));
            }
            else
            {
                state.PushString(s.Substring(start - 1, end - start + 1));
            }
            
            return 1;
        }

        // string.lower(s)
        private static int Lower(LuaState state)
        {
            string s = state.ToString(1);
            if (s == null)
            {
                state.PushString("");
                return 1;
            }
            // Lua lower/upper только для ASCII символов
            char[] chars = s.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] >= 'A' && chars[i] <= 'Z')
                    chars[i] = (char)(chars[i] + 32);
            }
            state.PushString(new string(chars));
            return 1;
        }

        // string.upper(s)
        private static int Upper(LuaState state)
        {
            string s = state.ToString(1);
            if (s == null)
            {
                state.PushString("");
                return 1;
            }
            // Lua lower/upper только для ASCII символов
            char[] chars = s.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] >= 'a' && chars[i] <= 'z')
                    chars[i] = (char)(chars[i] - 32);
            }
            state.PushString(new string(chars));
            return 1;
        }

        // string.rep(s, n)
        private static int Rep(LuaState state)
        {
            string s = state.ToString(1);
            if (s == null)
            {
                state.PushString("bad argument #1 to rep (string expected)");
                return state.Error();
            }
            
            double nDouble = state.ToNumber(2);
            int n = (int)nDouble;
            
            // Проверка на отрицательное число
            if (nDouble < 0)
            {
                state.PushString("");
                return 1;
            }
            
            if (n <= 0 || string.IsNullOrEmpty(s))
            {
                state.PushString("");
                return 1;
            }
            
            StringBuilder sb = new StringBuilder(s.Length * n);
            for (int i = 0; i < n; i++)
            {
                sb.Append(s);
            }
            
            state.PushString(sb.ToString());
            return 1;
        }

        // string.reverse(s)
        private static int Reverse(LuaState state)
        {
            string s = state.ToString(1) ?? "";
            char[] chars = s.ToCharArray();
            Array.Reverse(chars);
            state.PushString(new string(chars));
            return 1;
        }

        // string.char(...)
        private static int Char(LuaState state)
        {
            int top = state.GetTop();
            StringBuilder sb = new StringBuilder();
            
            for (int i = 1; i <= top; i++)
            {
                LuaValue val = state.Get(i);
                if (val.Type != LuaType.LUA_TNUMBER)
                {
                    state.PushString($"string.char: bad argument #{i} to char (number expected, got {val.Type.ToString()})");
                    return state.Error();
                }
                
                double num = val.ToNumber();
                // Проверка на NaN и infinity
                if (double.IsNaN(num) || double.IsInfinity(num))
                {
                    state.PushString($"string.char: bad argument #{i} to char (number has no integer representation)");
                    return state.Error();
                }
                
                int code = (int)num;
                if (code < 0 || code > 255)
                {
                    state.PushString($"string.char: bad argument #{i} to char (value out of range)");
                    return state.Error();
                }
                sb.Append((char)code);
            }
            
            state.PushString(sb.ToString());
            return 1;
        }

        // string.byte(s [, i [, j]])
        private static int Byte(LuaState state)
        {
            LuaValue sVal = state.Get(1);
            if (sVal.Type != LuaType.LUA_TSTRING && sVal.Type != LuaType.LUA_TNUMBER)
            {
                state.PushString("bad argument #1 to byte (string expected)");
                return state.Error();
            }
            
            string s = sVal.ToStringValue();
            int len = s.Length;
            
            int i = 1;
            if (state.GetTop() >= 2)
            {
                i = PosRelAt(state.ToNumber(2), len);
                if (i < 1) i = 1;
                if (i > len) i = len + 1;
            }
            
            if (state.GetTop() >= 3)
            {
                int j = PosRelAt(state.ToNumber(3), len);
                if (j < i) j = i - 1;
                if (j > len) j = len;
                
                int count = j - i + 1;
                for (int k = 0; k < count; k++)
                {
                    state.PushNumber((double)s[i - 1 + k]);
                }
                return count;
            }
            else
            {
                if (i > len)
                {
                    state.PushNil();
                    return 0;
                }
                state.PushNumber((double)s[i - 1]);
                return 1;
            }
        }

        // string.find(s, pattern [, init [, plain]])
        private static int Find(LuaState state)
        {
            string s = state.ToString(1) ?? "";
            string pattern = state.ToString(2) ?? "";
            
            int init = 1;
            if (state.GetTop() >= 3)
            {
                init = PosRelAt(state.ToNumber(3), s.Length);
                if (init < 1) init = 1;
                if (init > s.Length) init = s.Length + 1;
            }
            
            bool plain = false;
            if (state.GetTop() >= 4)
            {
                plain = state.ToBoolean(4);
            }
            
            int index;
            if (plain)
            {
                index = s.IndexOf(pattern, init - 1);
            }
            else
            {
                // Упрощенная реализация без полной поддержки Lua patterns
                index = s.IndexOf(pattern, init - 1);
            }
            
            if (index == -1)
            {
                state.PushNil();
                return 0;
            }
            
            state.PushNumber(index + 1);
            state.PushNumber(index + pattern.Length);
            return 2;
        }

        // string.match(s, pattern [, init])
        private static int Match(LuaState state)
        {
            string s = state.ToString(1) ?? "";
            string pattern = state.ToString(2) ?? "";
            
            int init = 1;
            if (state.GetTop() >= 3)
            {
                init = PosRelAt(state.ToNumber(3), s.Length);
                if (init < 1) init = 1;
            }
            
            // Упрощенная реализация
            int index = s.IndexOf(pattern, init - 1);
            if (index == -1)
            {
                state.PushNil();
                return 0;
            }
            
            state.PushString(pattern);
            return 1;
        }

        // string.gsub(s, pattern, repl [, n])
        private static int Gsub(LuaState state)
        {
            string s = state.ToString(1) ?? "";
            string pattern = state.ToString(2) ?? "";
            
            int maxCount = -1;
            if (state.GetTop() >= 4)
            {
                maxCount = (int)state.ToNumber(4);
            }
            
            int count = 0;
            string result = s.Replace(pattern, maxCount, ref count);
            
            state.PushString(result);
            state.PushNumber(count);
            return 2;
        }

        // string.gmatch(s, pattern)
        private static int Gmatch(LuaState state)
        {
            // Возвращает итератор
            state.PushString("gmatch iterator not fully implemented");
            return 1;
        }

        // string.format(formatstring, ...)
        private static int Format(LuaState state)
        {
            string format = state.ToString(1);
            if (format == null)
            {
                state.PushString("bad argument #1 to format (string expected)");
                return state.Error();
            }
            
            StringBuilder result = new StringBuilder();
            int argIndex = 2;
            
            for (int i = 0; i < format.Length; i++)
            {
                if (format[i] == '%')
                {
                    i++;
                    if (i >= format.Length) break;
                    
                    // Парсинг спецификатора формата Lua
                    // Формат: %[flags][width][.precision]specifier
                    
                    int width = -1;
                    int precision = -1;
                    string flags = "";
                    
                    // Чтение флагов
                    while (i < format.Length && "+-#0 ".IndexOf(format[i]) >= 0)
                    {
                        flags += format[i];
                        i++;
                    }
                    
                    // Чтение ширины
                    if (i < format.Length && char.IsDigit(format[i]))
                    {
                        width = 0;
                        while (i < format.Length && char.IsDigit(format[i]))
                        {
                            width = width * 10 + (format[i] - '0');
                            i++;
                        }
                    }
                    
                    // Чтение точности
                    if (i < format.Length && format[i] == '.')
                    {
                        i++;
                        precision = 0;
                        while (i < format.Length && char.IsDigit(format[i]))
                        {
                            precision = precision * 10 + (format[i] - '0');
                            i++;
                        }
                    }
                    
                    if (i >= format.Length) break;
                    
                    char specifier = format[i];
                    
                    switch (specifier)
                    {
                        case '%':
                            result.Append('%');
                            break;
                        case 's':
                        {
                            string s = state.ToString(argIndex++);
                            if (s == null) s = "nil";
                            if (precision >= 0 && s.Length > precision)
                                s = s.Substring(0, precision);
                            if (width > 0 && flags.Contains("-"))
                                result.Append(s.PadRight(width));
                            else if (width > 0)
                                result.Append(s.PadLeft(width));
                            else
                                result.Append(s);
                            break;
                        }
                        case 'd':
                        case 'i':
                        {
                            long num = (long)state.ToNumber(argIndex++);
                            string numStr = num.ToString();
                            if (precision >= 0 && precision > numStr.Length)
                                numStr = numStr.PadLeft(precision, '0');
                            if (width > 0 && flags.Contains("-"))
                                result.Append(numStr.PadRight(width));
                            else if (width > 0)
                                result.Append(numStr.PadLeft(width));
                            else
                                result.Append(numStr);
                            break;
                        }
                        case 'f':
                        {
                            double num = state.ToNumber(argIndex++);
                            string numStr = precision >= 0 ? num.ToString($"F{precision}") : num.ToString("F6");
                            if (width > 0 && flags.Contains("-"))
                                result.Append(numStr.PadRight(width));
                            else if (width > 0)
                                result.Append(numStr.PadLeft(width));
                            else
                                result.Append(numStr);
                            break;
                        }
                        case 'g':
                        {
                            double num = state.ToNumber(argIndex++);
                            string numStr = precision >= 0 ? num.ToString($"G{precision}") : num.ToString("G");
                            if (width > 0 && flags.Contains("-"))
                                result.Append(numStr.PadRight(width));
                            else if (width > 0)
                                result.Append(numStr.PadLeft(width));
                            else
                                result.Append(numStr);
                            break;
                        }
                        case 'e':
                        {
                            double num = state.ToNumber(argIndex++);
                            string numStr = precision >= 0 ? num.ToString($"E{precision}") : num.ToString("E");
                            if (width > 0 && flags.Contains("-"))
                                result.Append(numStr.PadRight(width));
                            else if (width > 0)
                                result.Append(numStr.PadLeft(width));
                            else
                                result.Append(numStr);
                            break;
                        }
                        case 'E':
                        {
                            double num = state.ToNumber(argIndex++);
                            string numStr = precision >= 0 ? num.ToString($"E{precision}") : num.ToString("E");
                            if (width > 0 && flags.Contains("-"))
                                result.Append(numStr.PadRight(width));
                            else if (width > 0)
                                result.Append(numStr.PadLeft(width));
                            else
                                result.Append(numStr);
                            break;
                        }
                        case 'c':
                        {
                            char c = (char)(int)state.ToNumber(argIndex++);
                            result.Append(c);
                            break;
                        }
                        case 'o':
                        {
                            int num = (int)state.ToNumber(argIndex++);
                            string numStr = Convert.ToString(num, 8);
                            if (width > 0 && flags.Contains("-"))
                                result.Append(numStr.PadRight(width));
                            else if (width > 0)
                                result.Append(numStr.PadLeft(width));
                            else
                                result.Append(numStr);
                            break;
                        }
                        case 'x':
                        {
                            int num = (int)state.ToNumber(argIndex++);
                            string numStr = Convert.ToString(num, 16);
                            if (width > 0 && flags.Contains("-"))
                                result.Append(numStr.PadRight(width));
                            else if (width > 0)
                                result.Append(numStr.PadLeft(width));
                            else
                                result.Append(numStr);
                            break;
                        }
                        case 'X':
                        {
                            int num = (int)state.ToNumber(argIndex++);
                            string numStr = Convert.ToString(num, 16).ToUpper();
                            if (width > 0 && flags.Contains("-"))
                                result.Append(numStr.PadRight(width));
                            else if (width > 0)
                                result.Append(numStr.PadLeft(width));
                            else
                                result.Append(numStr);
                            break;
                        }
                        case 'q':
                        {
                            string qs = state.ToString(argIndex++);
                            if (qs == null) qs = "";
                            result.Append('"').Append(qs.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n")).Append('"');
                            break;
                        }
                        default:
                            result.Append('%').Append(specifier);
                            break;
                    }
                }
                else
                {
                    result.Append(format[i]);
                }
            }
            
            state.PushString(result.ToString());
            return 1;
        }

        // string.dump(function)
        private static int Dump(LuaState state)
        {
            state.PushString("dump not implemented");
            return 1;
        }

        private static int PosRelAt(double pos, int len)
        {
            if (pos < 0)
                return len + (int)pos + 1;
            return (int)pos;
        }

        private static string Replace(this string s, string oldValue, int maxCount, ref int count)
        {
            if (string.IsNullOrEmpty(oldValue))
                return s;
            
            count = 0;
            StringBuilder result = new StringBuilder();
            int startIndex = 0;
            int index;
            
            while ((index = s.IndexOf(oldValue, startIndex)) != -1)
            {
                if (maxCount >= 0 && count >= maxCount)
                {
                    result.Append(s.Substring(startIndex));
                    break;
                }
                
                result.Append(s.Substring(startIndex, index - startIndex));
                count++;
                startIndex = index + oldValue.Length;
            }
            
            if (startIndex < s.Length)
                result.Append(s.Substring(startIndex));
            
            return result.ToString();
        }
    }
}
