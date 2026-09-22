using System;
using System.Diagnostics;
using System.Globalization;
using Lua51Net.Core;

namespace Lua51Net.VM.Libraries
{
    /// <summary>
    /// Библиотека os для Lua 5.1.4
    /// </summary>
    public static class OsLib
    {
        public static void Register(LuaState state)
        {
            // Создаем таблицу os
            state.NewTable();
            
            // Регистрируем функции
            RegisterFunction(state, "date", Date);
            RegisterFunction(state, "time", Time);
            RegisterFunction(state, "execute", Execute);
            
            // Устанавливаем в global
            state.SetGlobal("os");
        }

        private static void RegisterFunction(LuaState state, string name, LuaNativeFunction func)
        {
            state.PushCFunction(func);
            state.SetField(-2, name);
        }

        // os.date([format [, time]])
        private static int Date(LuaState state)
        {
            string format = state.ToString(1) ?? "%c";
            double timeValue = 0;
            
            if (state.GetTop() >= 2)
            {
                timeValue = state.ToNumber(2);
            }
            
            // Конвертируем время Lua (секунды с 1970-01-01) в DateTime
            DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds((long)timeValue).LocalDateTime;
            
            // Если формат начинается с '!', используем UTC
            bool useUtc = false;
            if (format.Length > 0 && format[0] == '!')
            {
                useUtc = true;
                dateTime = DateTimeOffset.FromUnixTimeSeconds((long)timeValue).UtcDateTime;
                format = format.Substring(1).TrimStart();
            }
            
            // Специальный случай: "*t" возвращает таблицу с компонентами времени
            if (format == "*t")
            {
                state.NewTable();
                
                state.PushNumber(dateTime.Year);
                state.SetField(-2, "year");
                
                state.PushNumber(dateTime.Month);
                state.SetField(-2, "month");
                
                state.PushNumber(dateTime.Day);
                state.SetField(-2, "day");
                
                state.PushNumber(dateTime.Hour);
                state.SetField(-2, "hour");
                
                state.PushNumber(dateTime.Minute);
                state.SetField(-2, "min");
                
                state.PushNumber(dateTime.Second);
                state.SetField(-2, "sec");
                
                state.PushNumber((int)dateTime.DayOfWeek == 0 ? 7 : (int)dateTime.DayOfWeek);
                state.SetField(-2, "wday");
                
                // Вычисляем день года (1-366)
                int dayOfYear = dateTime.DayOfYear;
                state.PushNumber(dayOfYear);
                state.SetField(-2, "yday");
                
                // Определяем, является ли год високосным
                bool isLeap = DateTime.IsLeapYear(dateTime.Year);
                state.PushBoolean(isLeap);
                state.SetField(-2, "isdst");
                
                return 1;
            }
            
            // Форматирование строки даты
            string result = FormatDate(format, dateTime);
            state.PushString(result);
            return 1;
        }

        // os.time([table])
        private static int Time(LuaState state)
        {
            if (state.GetTop() == 0)
            {
                // Возвращаем текущее время в секундах с 1970-01-01
                long timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
                state.PushNumber(timestamp);
                return 1;
            }
            
            // Получаем таблицу с компонентами времени
            LuaValue tableValue = state.Get(1);
            if (tableValue.Type != LuaType.LUA_TTABLE)
            {
                state.PushString("bad argument #1 to time (table expected, got " + tableValue.Type + ")");
                return state.Error();
            }
            
            LuaTable table = (LuaTable)tableValue.Value;
            
            // Извлекаем компоненты времени из таблицы
            int year = (int)table[LuaValue.CreateString("year")].ToNumber();
            int month = (int)table[LuaValue.CreateString("month")].ToNumber();
            int day = (int)table[LuaValue.CreateString("day")].ToNumber();
            
            int hour = 12; // Значение по умолчанию
            int minute = 0;
            int second = 0;
            
            if (table[LuaValue.CreateString("hour")].Type != LuaType.LUA_TNIL)
                hour = (int)table[LuaValue.CreateString("hour")].ToNumber();
            
            if (table[LuaValue.CreateString("min")].Type != LuaType.LUA_TNIL)
                minute = (int)table[LuaValue.CreateString("min")].ToNumber();
            
            if (table[LuaValue.CreateString("sec")].Type != LuaType.LUA_TNIL)
                second = (int)table[LuaValue.CreateString("sec")].ToNumber();
            
            try
            {
                DateTime dateTime = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Local);
                long timestamp = new DateTimeOffset(dateTime).ToUnixTimeSeconds();
                state.PushNumber(timestamp);
            }
            catch (Exception e)
            {
                state.PushString("invalid date: " + e.Message);
                return state.Error();
            }
            
            return 1;
        }

        // os.execute([command])
        private static int Execute(LuaState state)
        {
            string command = state.ToString(1) ?? "";
            
            if (string.IsNullOrEmpty(command))
            {
                // Если команда не указана, возвращаем true если shell доступен
                // В .NET мы всегда можем запустить процессы, так что возвращаем 1
                state.PushNumber(1);
                return 1;
            }
            
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = GetShellCommand(),
                    Arguments = command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                using (Process process = Process.Start(psi))
                {
                    process.WaitForExit();
                    int exitCode = process.ExitCode;
                    
                    // В Lua 5.1 os.execute возвращает код возврата
                    state.PushNumber(exitCode);
                    return 1;
                }
            }
            catch (Exception e)
            {
                // Ошибка при выполнении команды
                state.PushNil();
                state.PushString(e.Message);
                state.PushNumber(-1);
                return 3;
            }
        }

        private static string GetShellCommand()
        {
            // Определяем команду оболочки в зависимости от ОС
            if (Environment.OSVersion.Platform == PlatformID.Win32NT ||
                Environment.OSVersion.Platform == PlatformID.Win32S ||
                Environment.OSVersion.Platform == PlatformID.Win32Windows ||
                Environment.OSVersion.Platform == PlatformID.WinCE)
            {
                return "cmd.exe";
            }
            else
            {
                return "/bin/sh";
            }
        }

        private static string FormatDate(string format, DateTime dateTime)
        {
            // Преобразуем спецификаторы формата Lua в формат .NET
            string result = "";
            
            for (int i = 0; i < format.Length; i++)
            {
                if (format[i] == '%')
                {
                    if (i + 1 >= format.Length)
                    {
                        result += '%';
                        break;
                    }
                    
                    char specifier = format[i + 1];
                    i++; // Пропускаем следующий символ
                    
                    switch (specifier)
                    {
                        case '%':
                            result += '%';
                            break;
                        case 'a':
                            result += dateTime.ToString("ddd");
                            break;
                        case 'A':
                            result += dateTime.ToString("dddd");
                            break;
                        case 'b':
                            result += dateTime.ToString("MMM");
                            break;
                        case 'B':
                            result += dateTime.ToString("MMMM");
                            break;
                        case 'c':
                            result += dateTime.ToString("f");
                            break;
                        case 'd':
                            result += dateTime.ToString("dd");
                            break;
                        case 'H':
                            result += dateTime.ToString("HH");
                            break;
                        case 'I':
                            result += dateTime.ToString("hh");
                            break;
                        case 'j':
                            result += dateTime.DayOfYear.ToString("D3");
                            break;
                        case 'm':
                            result += dateTime.ToString("MM");
                            break;
                        case 'M':
                            result += dateTime.ToString("mm");
                            break;
                        case 'p':
                            result += dateTime.ToString("tt");
                            break;
                        case 'S':
                            result += dateTime.ToString("ss");
                            break;
                        case 'U':
                            // Неделя года (воскресенье - первый день недели)
                            result += GetWeekNumber(dateTime, DayOfWeek.Sunday).ToString("D2");
                            break;
                        case 'w':
                            result += ((int)dateTime.DayOfWeek).ToString();
                            break;
                        case 'W':
                            // Неделя года (понедельник - первый день недели)
                            result += GetWeekNumber(dateTime, DayOfWeek.Monday).ToString("D2");
                            break;
                        case 'x':
                            result += dateTime.ToString("d");
                            break;
                        case 'X':
                            result += dateTime.ToString("T");
                            break;
                        case 'y':
                            result += dateTime.ToString("yy");
                            break;
                        case 'Y':
                            result += dateTime.ToString("yyyy");
                            break;
                        case 'Z':
                            // Часовой пояс
                            result += dateTime.ToString("zzz");
                            break;
                        default:
                            result += '%' + specifier;
                            break;
                    }
                }
                else
                {
                    result += format[i];
                }
            }
            
            return result;
        }

        private static int GetWeekNumber(DateTime dateTime, DayOfWeek firstDayOfWeek)
        {
            // Вычисляем номер недели в году
            Calendar calendar = CultureInfo.InvariantCulture.Calendar;
            CalendarWeekRule rule = CalendarWeekRule.FirstFourDayWeek;
            return calendar.GetWeekOfYear(dateTime, rule, firstDayOfWeek);
        }
    }
}
