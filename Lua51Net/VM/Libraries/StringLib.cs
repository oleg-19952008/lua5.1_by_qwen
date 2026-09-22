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
                state.PushNumber(0);
            }
            else
            {
                state.PushNumber(s.Length);
            }
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
            if (start > len) start = len + 1;
            
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
            string s = state.ToString(1) ?? "";
            state.PushString(s.ToLower());
            return 1;
        }

        // string.upper(s)
        private static int Upper(LuaState state)
        {
            string s = state.ToString(1) ?? "";
            state.PushString(s.ToUpper());
            return 1;
        }

        // string.rep(s, n [, sep])
        private static int Rep(LuaState state)
        {
            string s = state.ToString(1) ?? "";
            int n = (int)state.ToNumber(2);
            
            string sep = "";
            if (state.GetTop() >= 3)
            {
                sep = state.ToString(3) ?? "";
            }
            
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < n; i++)
            {
                if (i > 0 && !string.IsNullOrEmpty(sep))
                    sb.Append(sep);
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
                int code = (int)state.ToNumber(i);
                if (code < 0 || code > 255)
                {
                    state.PushString($"string.char: bad argument #{i} to char (number out of range)");
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
            string s = state.ToString(1) ?? "";
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
                    state.PushNumber(s[i - 1 + k]);
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
                state.PushNumber(s[i - 1]);
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
            string format = state.ToString(1) ?? "";
            StringBuilder result = new StringBuilder();
            int argIndex = 2;
            
            for (int i = 0; i < format.Length; i++)
            {
                if (format[i] == '%')
                {
                    i++;
                    if (i >= format.Length) break;
                    
                    char specifier = format[i];
                    switch (specifier)
                    {
                        case '%':
                            result.Append('%');
                            break;
                        case 's':
                            result.Append(state.ToString(argIndex++) ?? "nil");
                            break;
                        case 'd':
                        case 'i':
                            result.Append((long)state.ToNumber(argIndex++));
                            break;
                        case 'f':
                            result.Append(state.ToNumber(argIndex++).ToString("F6"));
                            break;
                        case 'g':
                            result.Append(state.ToNumber(argIndex++).ToString("G"));
                            break;
                        case 'e':
                        case 'E':
                            result.Append(state.ToNumber(argIndex++).ToString("E"));
                            break;
                        case 'c':
                            result.Append((char)(int)state.ToNumber(argIndex++));
                            break;
                        case 'o':
                            result.Append(Convert.ToString((int)state.ToNumber(argIndex++), 8));
                            break;
                        case 'x':
                            result.Append(Convert.ToString((int)state.ToNumber(argIndex++), 16));
                            break;
                        case 'X':
                            result.Append(Convert.ToString((int)state.ToNumber(argIndex++), 16).ToUpper());
                            break;
                        case 'q':
                            string qs = state.ToString(argIndex++) ?? "";
                            result.Append('"').Append(qs.Replace("\"", "\"\"")).Append('"');
                            break;
                        default:
                            if (char.IsDigit(specifier))
                            {
                                // Обработка ширины поля (упрощенно)
                                result.Append('%').Append(specifier);
                            }
                            else
                            {
                                result.Append('%').Append(specifier);
                            }
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
            if (pos >= 0)
                return (int)pos;
            return len + (int)pos + 1;
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
