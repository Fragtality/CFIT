using System;

namespace CFIT.AppTools
{
    public static class EnumExtensions
    {
        public static bool TryParseEnumValue<T>(object parameter, out T enumValue) where T : struct
        {
            if (!(parameter is string stringValue) || !Enum.TryParse<T>(stringValue, out enumValue))
            {
                enumValue = default;
                return false;
            }
            else
                return true;
        }

        public static uint FlagAdd(uint var, uint value)
        {
            return var | value;
        }

        public static uint FlagRemove(uint var, uint value)
        {
            return var & ~value;
        }

        public static bool IsEnumType<E>(this Enum @enum)
        {
            return @enum?.GetType()?.Name == typeof(E)?.Name;
        }

        public static E ToEnumValue<E>(this Enum @enum)
        {
            return (E)(object)@enum.ToInt32();
        }

        public static int ToInt32<E>(this E value) where E : Enum => (int)(object)value;

        public static uint ToUInt32<E>(this E value) where E : Enum => (uint)(object)value;

        private enum DummyEnum { }

        public static bool HasFlag<T>(this T value, uint flag) where T : Enum
        {
            return value.HasFlag((DummyEnum)(object)flag);
        }

        public static bool HasFlag<T>(this T value, int flag) where T : Enum
        {
            return value.HasFlag((DummyEnum)(object)flag);
        }

        public static bool HasFlag(this Enum value, int flag)
        {
            return (value.ToInt32() & flag) == flag;
        }

        public static bool HasFlag(this uint value, uint flag)
        {
            return (value & flag) == flag;
        }

        public static bool HasFlag(this uint value, Enum eFlag)
        {
            uint flag = eFlag.ToUInt32();
            return (value & flag) == flag;
        }

        public static bool HasFlag(this int value, int flag)
        {
            return (value & flag) == flag;
        }

        public static bool HasFlag(this int value, Enum eFlag)
        {
            int flag = eFlag.ToInt32();
            return (value & flag) == flag;
        }
    }
}
