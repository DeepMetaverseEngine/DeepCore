using DeepCore.Reflection;
using System;
using System.Collections;
using System.Numerics;

namespace DeepCore
{
    public static class Test
    {
        /// <summary>
        /// 随机填充类字段
        /// </summary>
        /// <param name="data"></param>
        public static object GetRandomValue(this Random random, Type type,  int arrayLengthMin = -1, int arrayLengthMax = 200)
        {
            if (arrayLengthMax < arrayLengthMin)
            {
                arrayLengthMax = arrayLengthMin;
            }
            //             if (arrayLength < 0)
            //             {
            //                 arrayLength = random.Next(10, 100);
            //             }
            try
            {
                if (type == typeof(object))
                {
                    switch (random.Next(18))
                    {
                        case 0: type = typeof(object); break;
                        case 1: type = typeof(string); break;
                        case 2: type = typeof(bool); break;
                        case 3: type = typeof(byte); break;
                        case 4: type = typeof(char); break;
                        case 5: type = typeof(decimal); break;
                        case 6: type = typeof(double); break;
                        case 7: type = typeof(short); break;
                        case 8: type = typeof(int); break;
                        case 9: type = typeof(long); break;
                        case 10: type = typeof(sbyte); break;
                        case 11: type = typeof(float); break;
                        case 12: type = typeof(ushort); break;
                        case 13: type = typeof(uint); break;
                        case 14: type = typeof(ulong); break;
                        case 15: type = typeof(byte[]); break;
                        case 16: type = typeof(DateTime); break;
                        case 17: type = typeof(TimeSpan); break;
                        case 18: type = typeof(BigInteger); break;
                    }
                }
                if (type == (typeof(string)))
                {
                    return random.Next().ToString();
                }
                else if (type == (typeof(bool)))
                {
                    return random.Next() % 2 == 1;
                }
                else if (type == (typeof(int)))
                {
                    return random.Next();
                }
                else if (type == (typeof(uint)))
                {
                    return (uint)random.Next();
                }
                else if (type == (typeof(long)))
                {
                    return (long)random.Next();
                }
                else if (type == (typeof(ulong)))
                {
                    return (ulong)random.Next();
                }
                else if (type == (typeof(short)))
                {
                    return (short)random.Next();
                }
                else if (type == (typeof(ushort)))
                {
                    return (ushort)random.Next();
                }
                else if (type == (typeof(byte)))
                {
                    return (byte)random.Next();
                }
                else if (type == (typeof(sbyte)))
                {
                    return (sbyte)random.Next();
                }
                else if (type == (typeof(char)))
                {
                    return random.Next(10).ToString()[0];
                }
                else if (type == (typeof(float)))
                {
                    return (float)random.NextDouble();
                }
                else if (type == (typeof(double)))
                {
                    return random.NextDouble();
                }
                else if (type == (typeof(DateTime)))
                {
                    return DateTime.Now;
                }
                else if (type == (typeof(TimeSpan)))
                {
                    return TimeSpan.FromMilliseconds(random.Next());
                }
                else if (type == (typeof(BigInteger)))
                {
                    return new BigInteger(random.Next());
                }
                else if (type == (typeof(byte[])))
                {
                    var len = random.Next(arrayLengthMin, arrayLengthMax);
                    if (len < 0) return null;
                    var buffer = new byte[len];
                    random.NextBytes(buffer);
                    return buffer;
                }
                else if (type.IsEnum)
                {
                    return random.GetRandomInArray(Enum.GetValues(type));
                }
                else if (type.IsArray && type.HasElementType)
                {
                    var len = random.Next(arrayLengthMin, arrayLengthMax);
                    if (len < 0) return null;
                    var ret = Array.CreateInstance(type.GetElementType(), len);
                    for (int i = 0; i < len; i++)
                    {
                        ret.SetValue(random.GetRandomValue(type.GetElementType(), arrayLengthMin, arrayLengthMax), i);
                    }
                    return ret;
                }
                else if (type.IsInterfaceOf(typeof(IList)) && type.IsGenericType)
                {
                    var len = random.Next(arrayLengthMin, arrayLengthMax);
                    if (len < 0) return null;
                    var ret = (IList)DeepActivator.CreateInstance(type);
                    var gargs = type.GetGenericArguments();
                    for (int i = 0; i < len; i++)
                    {
                        ret.Add(random.GetRandomValue(gargs[0], arrayLengthMin, arrayLengthMax));
                    }
                    return ret;
                }
                else if (type.IsInterfaceOf(typeof(IDictionary)) && type.IsGenericType)
                {
                    var len = random.Next(arrayLengthMin, arrayLengthMax);
                    if (len < 0) return null;
                    var ret = (IDictionary)DeepActivator.CreateInstance(type);
                    var gargs = type.GetGenericArguments();
                    for (int i = 0; i < len; i++)
                    {
                        var key = random.GetRandomValue(gargs[0], arrayLengthMin, arrayLengthMax);
                        var value = random.GetRandomValue(gargs[1], arrayLengthMin, arrayLengthMax);
                        ret[key] = value;
                    }
                    return ret;
                }
                else if (type.IsClass)
                {
                    if (type.IsAbstract)
                    {
                        var subtype = random.GetRandomInCollection(ReflectionUtil.GetNoneVirtualSubTypes(type));
                        if (subtype != null)
                        {
                            return random.GetRandomValue(subtype, arrayLengthMin, arrayLengthMax);
                        }
                    }
                    else
                    {
                        var ret = DeepActivator.CreateInstance(type);
                        foreach (var field in type.GetFields())
                        {
                            if (!field.IsStatic && !field.IsInitOnly && !field.IsLiteral && field.IsPublic)
                            {
                                field.SetValue(ret, random.GetRandomValue(field.FieldType, arrayLengthMin, arrayLengthMax));
                            }
                        }
                        return ret;
                    }
                    return null;
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }
        public static T GetRandomValue<T>(this Random random, int arrayLengthMin = -1, int arrayLengthMax = 200)
        {
            return (T)GetRandomValue(random, typeof(T), arrayLengthMin, arrayLengthMax);
        }
        public static object FillRandomValue(this Random random, object ret,  int arrayLengthMin = -1, int arrayLengthMax = 200)
        {
            try
            {
                var type = ret.GetType();
                foreach (var field in type.GetFields())
                {
                    if (!field.IsStatic && !field.IsInitOnly && !field.IsLiteral && field.IsPublic)
                    {
                        field.SetValue(ret, random.GetRandomValue(field.FieldType, arrayLengthMin, arrayLengthMax));
                    }
                }
                return ret;
            }
            catch { return null; }
        }
    }
}
