using System;
using System.IO;
using Lua51Net.Core;

namespace Lua51Net.VM.Libraries
{
    /// <summary>
    /// Библиотека io для Lua 5.1.4
    /// </summary>
    public static class IOLib
    {
        // Ключи для хранения файлов в таблице metatable
        private const string FILE_METATABLE = "FILE*";
        
        public static void Register(LuaState state)
        {
            // Создаем таблицу io
            state.NewTable();
            
            // Стандартные файлы
            state.PushUserData(Console.In);
            state.SetGlobal("stdin");
            
            state.PushUserData(Console.Out);
            state.SetGlobal("stdout");
            
            state.PushUserData(Console.Error);
            state.SetGlobal("stderr");
            
            // Регистрируем функции
            RegisterFunction(state, "close", Close);
            RegisterFunction(state, "flush", Flush);
            RegisterFunction(state, "input", Input);
            RegisterFunction(state, "lines", Lines);
            RegisterFunction(state, "open", Open);
            RegisterFunction(state, "output", Output);
            RegisterFunction(state, "popen", Popen);
            RegisterFunction(state, "read", Read);
            RegisterFunction(state, "tmpfile", Tmpfile);
            RegisterFunction(state, "type", Type);
            RegisterFunction(state, "write", Write);
            
            // Устанавливаем в global
            state.SetGlobal("io");
        }

        private static void RegisterFunction(LuaState state, string name, LuaNativeFunction func)
        {
            state.PushCFunction(func);
            state.SetField(-2, name);
        }

        // io.close([file])
        private static int Close(LuaState state)
        {
            TextWriter writer = null;
            TextReader reader = null;
            
            if (state.GetTop() == 0)
            {
                // Закрываем stdout по умолчанию
                writer = Console.Out;
            }
            else if (state.IsUserData(1))
            {
                var userData = state.ToUserData(1);
                if (userData is TextWriter tw)
                    writer = tw;
                else if (userData is TextReader tr)
                    reader = tr;
            }
            
            if (writer != null && writer == Console.Out)
            {
                // Не закрываем стандартный вывод
                state.PushBoolean(true);
                return 1;
            }
            
            if (reader != null && reader == Console.In)
            {
                // Не закрываем стандартный ввод
                state.PushBoolean(true);
                return 1;
            }
            
            try
            {
                if (writer != null)
                    writer.Close();
                else if (reader != null)
                    reader.Close();
                
                state.PushBoolean(true);
                return 1;
            }
            catch (Exception e)
            {
                state.PushNil();
                state.PushString(e.Message);
                return 2;
            }
        }

        // io.flush()
        private static int Flush(LuaState state)
        {
            Console.Out.Flush();
            state.PushBoolean(true);
            return 1;
        }

        // io.input([file])
        private static int Input(LuaState state)
        {
            if (state.GetTop() == 0)
            {
                // Возвращаем stdin
                state.PushUserData(Console.In);
                return 1;
            }
            
            if (state.IsString(1))
            {
                // Открываем файл для чтения
                string filename = state.ToString(1);
                try
                {
                    var reader = new StreamReader(filename);
                    state.PushUserData(reader);
                    return 1;
                }
                catch (Exception e)
                {
                    state.PushNil();
                    state.PushString(e.Message);
                    return 2;
                }
            }
            
            state.PushValue(1);
            return 1;
        }

        // io.lines([filename, ...])
        private static int Lines(LuaState state)
        {
            if (state.GetTop() == 0)
            {
                // Чтение из stdin
                state.PushCFunction(ReadLineFromStdin);
                return 1;
            }
            
            string filename = state.ToString(1);
            try
            {
                var reader = new StreamReader(filename);
                state.PushUserData(reader);
                state.PushCFunction(ReadLineFromFile);
                state.PushValue(-2); // файл как upvalue
                state.PushNumber(0); // позиция
                state.Closure(3); // создаем замыкание
                return 1;
            }
            catch (Exception e)
            {
                state.PushNil();
                state.PushString(e.Message);
                return 2;
            }
        }

        private static int ReadLineFromStdin(LuaState state)
        {
            string line = Console.ReadLine();
            if (line == null)
            {
                state.PushNil();
                return 1;
            }
            state.PushString(line);
            return 1;
        }

        private static int ReadLineFromFile(LuaState state)
        {
            // Упрощенная реализация
            return ReadLineFromStdin(state);
        }

        // io.open(filename [, mode])
        private static int Open(LuaState state)
        {
            string filename = state.ToString(1);
            string mode = state.ToString(2) ?? "r";
            
            try
            {
                Stream stream;
                switch (mode)
                {
                    case "r":
                    case "rb":
                        stream = File.OpenRead(filename);
                        state.PushUserData(new StreamReader(stream));
                        break;
                    case "w":
                    case "wb":
                        stream = File.Create(filename);
                        state.PushUserData(new StreamWriter(stream));
                        break;
                    case "a":
                    case "ab":
                        stream = File.AppendText(filename);
                        state.PushUserData(new StreamWriter(stream));
                        break;
                    case "r+":
                    case "r+b":
                    case "rb+":
                        stream = File.Open(filename, FileMode.Open, FileAccess.ReadWrite);
                        state.PushUserData(stream);
                        break;
                    case "w+":
                    case "w+b":
                    case "wb+":
                        stream = File.Open(filename, FileMode.Create, FileAccess.ReadWrite);
                        state.PushUserData(stream);
                        break;
                    case "a+":
                    case "a+b":
                    case "ab+":
                        stream = File.Open(filename, FileMode.Append, FileAccess.ReadWrite);
                        state.PushUserData(stream);
                        break;
                    default:
                        state.PushNil();
                        state.PushString($"Unknown mode: {mode}");
                        return 2;
                }
                
                return 1;
            }
            catch (Exception e)
            {
                state.PushNil();
                state.PushString(e.Message);
                return 2;
            }
        }

        // io.output([file])
        private static int Output(LuaState state)
        {
            if (state.GetTop() == 0)
            {
                // Возвращаем stdout
                state.PushUserData(Console.Out);
                return 1;
            }
            
            if (state.IsString(1))
            {
                // Открываем файл для записи
                string filename = state.ToString(1);
                try
                {
                    var writer = new StreamWriter(filename);
                    state.PushUserData(writer);
                    return 1;
                }
                catch (Exception e)
                {
                    state.PushNil();
                    state.PushString(e.Message);
                    return 2;
                }
            }
            
            state.PushValue(1);
            return 1;
        }

        // io.popen(prog [, mode])
        private static int Popen(LuaState state)
        {
            // Упрощенная реализация - не поддерживается полностью
            state.PushNil();
            state.PushString("popen not fully implemented");
            return 2;
        }

        // io.read(...)
        private static int Read(LuaState state)
        {
            // Чтение из stdin по умолчанию
            string line = Console.ReadLine();
            if (line == null)
            {
                state.PushNil();
                return 1;
            }
            state.PushString(line);
            return 1;
        }

        // io.tmpfile()
        private static int Tmpfile(LuaState state)
        {
            try
            {
                string tempFile = Path.GetTempFileName();
                var stream = File.Open(tempFile, FileMode.Open, FileAccess.ReadWrite);
                state.PushUserData(stream);
                return 1;
            }
            catch (Exception e)
            {
                state.PushNil();
                state.PushString(e.Message);
                return 2;
            }
        }

        // io.type(obj)
        private static int Type(LuaState state)
        {
            var userData = state.ToUserData(1);
            
            if (userData == null)
            {
                state.PushNil();
                return 1;
            }
            
            if (userData is StreamReader || userData is StreamWriter || userData is Stream)
            {
                state.PushString("file");
                return 1;
            }
            
            state.PushNil();
            return 1;
        }

        // io.write(...)
        private static int Write(LuaState state)
        {
            int top = state.GetTop();
            for (int i = 1; i <= top; i++)
            {
                string s = state.ToString(i);
                if (s != null)
                {
                    Console.Write(s);
                }
            }
            
            state.PushUserData(Console.Out);
            return 1;
        }
    }
}
