using System;
using Lua51Net.Core;

namespace Lua51Net.VM.Libraries
{
    /// <summary>
    /// Библиотека math для Lua 5.1.4
    /// </summary>
    public static class MathLib
    {
        public static void Register(LuaState state)
        {
            // Создаем таблицу math
            state.NewTable();
            
            // Регистрируем функции
            RegisterFunction(state, "abs", Abs);
            RegisterFunction(state, "acos", Acos);
            RegisterFunction(state, "asin", Asin);
            RegisterFunction(state, "atan", Atan);
            RegisterFunction(state, "atan2", Atan2);
            RegisterFunction(state, "ceil", Ceil);
            RegisterFunction(state, "cos", Cos);
            RegisterFunction(state, "cosh", Cosh);
            RegisterFunction(state, "deg", Deg);
            RegisterFunction(state, "exp", Exp);
            RegisterFunction(state, "floor", Floor);
            RegisterFunction(state, "fmod", Fmod);
            RegisterFunction(state, "frexp", Frexp);
            RegisterFunction(state, "ldexp", Ldexp);
            RegisterFunction(state, "log", Log);
            RegisterFunction(state, "log10", Log10);
            RegisterFunction(state, "max", Max);
            RegisterFunction(state, "min", Min);
            RegisterFunction(state, "modf", Modf);
            RegisterFunction(state, "pow", Pow);
            RegisterFunction(state, "rad", Rad);
            RegisterFunction(state, "random", Random);
            RegisterFunction(state, "randomseed", RandomSeed);
            RegisterFunction(state, "sin", Sin);
            RegisterFunction(state, "sinh", Sinh);
            RegisterFunction(state, "sqrt", Sqrt);
            RegisterFunction(state, "tan", Tan);
            RegisterFunction(state, "tanh", Tanh);
            
            // Регистрируем константы
            RegisterConstant(state, "pi", Math.PI);
            RegisterConstant(state, "huge", double.MaxValue);
            
            // Устанавливаем в global
            state.SetGlobal("math");
        }

        private static void RegisterFunction(LuaState state, string name, LuaNativeFunction func)
        {
            state.PushCFunction(func);
            state.SetField(-2, name);
        }

        private static void RegisterConstant(LuaState state, string name, double value)
        {
            state.PushNumber(value);
            state.SetField(-2, name);
        }

        // math.abs(x)
        private static int Abs(LuaState state)
        {
            double x = state.ToNumber(1);
            state.PushNumber(Math.Abs(x));
            return 1;
        }

        // math.acos(x)
        private static int Acos(LuaState state)
        {
            double x = state.ToNumber(1);
            state.PushNumber(Math.Acos(x));
            return 1;
        }

        // math.asin(x)
        private static int Asin(LuaState state)
        {
            double x = state.ToNumber(1);
            state.PushNumber(Math.Asin(x));
            return 1;
        }

        // math.atan(x)
        private static int Atan(LuaState state)
        {
            double x = state.ToNumber(1);
            state.PushNumber(Math.Atan(x));
            return 1;
        }

        // math.atan2(y, x)
        private static int Atan2(LuaState state)
        {
            double y = state.ToNumber(1);
            double x = state.ToNumber(2);
            state.PushNumber(Math.Atan2(y, x));
            return 1;
        }

        // math.ceil(x)
        private static int Ceil(LuaState state)
        {
            double x = state.ToNumber(1);
            state.PushNumber(Math.Ceiling(x));
            return 1;
        }

        // math.cos(x)
        private static int Cos(LuaState state)
        {
            double x = state.ToNumber(1);
            state.PushNumber(Math.Cos(x));
            return 1;
        }

        // math.cosh(x)
        private static int Cosh(LuaState state)
        {
            double x = state.ToNumber(1);
            state.PushNumber(Math.Cosh(x));
            return 1;
        }

        // math.deg(x) - конвертирует радианы в градусы
        private static int Deg(LuaState state)
        {
            double x = state.ToNumber(1);
            state.PushNumber(x * 180.0 / Math.PI);
            return 1;
        }

        // math.exp(x)
        private static int Exp(LuaState state)
        {
            double x = state.ToNumber(1);
            state.PushNumber(Math.Exp(x));
            return 1;
        }

        // math.floor(x)
        private static int Floor(LuaState state)
        {
            double x = state.ToNumber(1);
            state.PushNumber(Math.Floor(x));
            return 1;
        }

        // math.fmod(x, y)
        private static int Fmod(LuaState state)
        {
            double x = state.ToNumber(1);
            double y = state.ToNumber(2);
            state.PushNumber(x % y);
            return 1;
        }

        // math.frexp(x) - возвращает мантиссу и экспоненту
        private static int Frexp(LuaState state)
        {
            double x = state.ToNumber(1);
            
            if (x == 0)
            {
                state.PushNumber(0);
                state.PushNumber(0);
                return 2;
            }
            
            int exponent = 0;
            double mantissa = x;
            
            // Нормализуем мантиссу в диапазон [0.5, 1)
            while (Math.Abs(mantissa) >= 1.0)
            {
                mantissa /= 2.0;
                exponent++;
            }
            
            while (Math.Abs(mantissa) < 0.5 && mantissa != 0)
            {
                mantissa *= 2.0;
                exponent--;
            }
            
            state.PushNumber(mantissa);
            state.PushNumber(exponent);
            return 2;
        }

        // math.ldexp(m, e) - возвращает m * 2^e
        private static int Ldexp(LuaState state)
        {
            double m = state.ToNumber(1);
            int e = (int)state.ToNumber(2);
            
            double result = m * Math.Pow(2.0, e);
            state.PushNumber(result);
            return 1;
        }

        // math.log(x [, base]) - натуральный логарифм или логарифм по основанию
        private static int Log(LuaState state)
        {
            double x = state.ToNumber(1);
            
            if (state.GetTop() >= 2)
            {
                double base_ = state.ToNumber(2);
                state.PushNumber(Math.Log(x) / Math.Log(base_));
            }
            else
            {
                state.PushNumber(Math.Log(x));
            }
            return 1;
        }

        // math.log10(x)
        private static int Log10(LuaState state)
        {
            double x = state.ToNumber(1);
            state.PushNumber(Math.Log10(x));
            return 1;
        }

        // math.max(...)
        private static int Max(LuaState state)
        {
            int top = state.GetTop();
            if (top == 0)
            {
                state.PushNumber(double.NegativeInfinity);
                return 1;
            }
            
            double max = state.ToNumber(1);
            for (int i = 2; i <= top; i++)
            {
                double val = state.ToNumber(i);
                if (val > max)
                {
                    max = val;
                }
            }
            
            state.PushNumber(max);
            return 1;
        }

        // math.min(...)
        private static int Min(LuaState state)
        {
            int top = state.GetTop();
            if (top == 0)
            {
                state.PushNumber(double.PositiveInfinity);
                return 1;
            }
            
            double min = state.ToNumber(1);
            for (int i = 2; i <= top; i++)
            {
                double val = state.ToNumber(i);
                if (val < min)
                {
                    min = val;
                }
            }
            
            state.PushNumber(min);
            return 1;
        }

        // math.modf(x) - возвращает целую и дробную части
        private static int Modf(LuaState state)
        {
            double x = state.ToNumber(1);
            double intPart = Math.Truncate(x);
            double fracPart = x - intPart;
            
            state.PushNumber(intPart);
            state.PushNumber(fracPart);
            return 2;
        }

        // math.pow(base, exp)
        private static int Pow(LuaState state)
        {
            double base_ = state.ToNumber(1);
            double exp = state.ToNumber(2);
            state.PushNumber(Math.Pow(base_, exp));
            return 1;
        }

        // math.rad(x) - конвертирует градусы в радианы
        private static int Rad(LuaState state)
        {
            double x = state.ToNumber(1);
            state.PushNumber(x * Math.PI / 180.0);
            return 1;
        }

        // math.random([m [, n]])
        private static int Random(LuaState state)
        {
            int top = state.GetTop();
            
            if (top == 0)
            {
                // Возвращает число в диапазоне [0, 1)
                state.PushNumber(RandomDouble());
                return 1;
            }
            else if (top == 1)
            {
                // Возвращает целое число в диапазоне [1, m]
                int m = (int)state.ToNumber(1);
                if (m <= 0)
                {
                    state.PushString("bad argument #1 to random (interval is empty)");
                    return state.Error();
                }
                state.PushNumber(RandomInt(1, m));
                return 1;
            }
            else
            {
                // Возвращает целое число в диапазоне [m, n]
                int m = (int)state.ToNumber(1);
                int n = (int)state.ToNumber(2);
                if (m > n)
                {
                    state.PushString("bad argument #2 to random (interval is empty)");
                    return state.Error();
                }
                state.PushNumber(RandomInt(m, n));
                return 1;
            }
        }

        // math.randomseed(x)
        private static int RandomSeed(LuaState state)
        {
            int seed = (int)state.ToNumber(1);
            RandomInit(seed);
            return 0;
        }

        // math.sin(x)
        private static int Sin(LuaState state)
        {
            double x = state.ToNumber(1);
            state.PushNumber(Math.Sin(x));
            return 1;
        }

        // math.sinh(x)
        private static int Sinh(LuaState state)
        {
            double x = state.ToNumber(1);
            state.PushNumber(Math.Sinh(x));
            return 1;
        }

        // math.sqrt(x)
        private static int Sqrt(LuaState state)
        {
            double x = state.ToNumber(1);
            state.PushNumber(Math.Sqrt(x));
            return 1;
        }

        // math.tan(x)
        private static int Tan(LuaState state)
        {
            double x = state.ToNumber(1);
            state.PushNumber(Math.Tan(x));
            return 1;
        }

        // math.tanh(x)
        private static int Tanh(LuaState state)
        {
            double x = state.ToNumber(1);
            state.PushNumber(Math.Tanh(x));
            return 1;
        }

        // Внутренний генератор случайных чисел
        private static System.Random _random = new System.Random();
        private static readonly object _randomLock = new object();

        private static void RandomInit(int seed)
        {
            lock (_randomLock)
            {
                _random = new System.Random(seed);
            }
        }

        private static double RandomDouble()
        {
            lock (_randomLock)
            {
                return _random.NextDouble();
            }
        }

        private static int RandomInt(int min, int max)
        {
            lock (_randomLock)
            {
                return _random.Next(min, max + 1);
            }
        }
    }
}
