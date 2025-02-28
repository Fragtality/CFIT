using System;
using System.Reflection;

namespace CFIT.AppTools
{
    public static class Constructors
    {
        public static TInstance CreateInstance<TInstance>(this Type type)
        {
            ConstructorInfo ctor = type?.GetConstructor(new Type[] { }) ?? throw new Exception($"Could not find Constructor for Type '{type?.Name}'");
            object instance = ctor?.Invoke(new object[] { }) ?? throw new Exception($"Could not invoke Constructor for Type '{type?.Name}'");
            return (TInstance)instance;
        }

        public static TInstance CreateInstance<TInstance, TParam>(this Type type, TParam param)
        {
            ConstructorInfo ctor = type?.GetConstructor(new Type[] { typeof(TParam) }) ?? throw new Exception($"Could not find Constructor for Type '{type?.Name}'");
            object instance = ctor?.Invoke(new object[] { param }) ?? throw new Exception($"Could not invoke Constructor for Type '{type?.Name}'");
            return (TInstance)instance;
        }

        public static TInstance CreateInstance<TInstance, TParam1, TParam2>(this Type type, TParam1 param1, TParam2 param2)
        {
            ConstructorInfo ctor = type?.GetConstructor(new Type[] { typeof(TParam1), typeof(TParam2) }) ?? throw new Exception($"Could not find Constructor for Type '{type?.Name}'");
            object instance = ctor?.Invoke(new object[] { param1, param2 }) ?? throw new Exception($"Could not invoke Constructor for Type '{type?.Name}'");
            return (TInstance)instance;
        }

        public static TInstance CreateInstance<TInstance, TParam1, TParam2, TParam3>(this Type type, TParam1 param1, TParam2 param2, TParam3 param3)
        {
            ConstructorInfo ctor = type?.GetConstructor(new Type[] { typeof(TParam1), typeof(TParam2), typeof(TParam3) }) ?? throw new Exception($"Could not find Constructor for Type '{type?.Name}'");
            object instance = ctor?.Invoke(new object[] { param1, param2, param3 }) ?? throw new Exception($"Could not invoke Constructor for Type '{type?.Name}'");
            return (TInstance)instance;
        }
    }
}
