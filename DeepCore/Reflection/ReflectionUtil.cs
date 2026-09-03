using DeepCore;
using DeepCore.Log;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;

namespace DeepCore.Reflection
{
    public static class ReflectionUtil
    {
        public static readonly object[] ZERO_ARGS = new object[0];
        //-------------------------------------------------------------------------------------------------
        #region _CacheAllAssemblyTypes_

        private static object all_lock = new object();
        private static Logger log = new ConsoleLogger("LoadDll");
        //private static List<Assembly> all_asm = null;
        private static List<Type> all_type = null;
        private static HashMap<string, Type> types_cache = new HashMap<string, Type>();
        public static IReadOnlyList<Type> RegisteredTypes
        {
            get
            {
                if (all_type != null) { return all_type; }
                InitAllType();
                return all_type;
            }
        }
        public static void InitRuntimeTypes(IEnumerable<Type> types)
        {
            lock (all_lock)
            {
                all_type = new List<Type>();
                foreach (var t in types)
                {
                    RegistType(t, 1);
                }
            }
        }
        private static IReadOnlyList<Type> InitAllType()
        {
            if (all_type != null) return all_type;
            lock (all_lock)
            {
                if (all_type == null)
                {
                    Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
                    all_type = new List<Type>();
                    foreach (Assembly asm in asms)
                    {
                        try
                        {
                            foreach (var t in GetRuntimeTypes(asm))
                            {
                                RegistType(t, 1);
                            }
                        }
                        catch (Exception err)
                        {
                            log.Warn("Add Assembly Error : " + asm.FullName + " : " + err.Message);
                        }
                    }
                }
            }
            return all_type;
        }
        private static IReadOnlyList<Type> RegistType(Type t, int verbos)
        {
            try
            {
                if (t.IsReflectibleType())
                {
                    if (!all_type.Contains(t))
                    {
                        all_type.Add(t);
                    }
                }
            }
            catch (Exception err)
            {
                if (verbos >= 2) log.Warn(err);
                if (verbos >= 1) log.Warn("Add Type Error : " + t.FullName + " : " + err.Message);
            }
            return all_type;
        }
        public static IReadOnlyList<Type> RegistAssembly(Assembly asm, int verbos = 1)
        {
            lock (all_lock)
            {
                InitAllType();
                try
                {
                    foreach (var t in GetRuntimeTypes(asm))
                    {
                        RegistType(t, verbos);
                    }
                }
                catch (Exception err)
                {
                    if (verbos >= 2) log.Warn(err);
                    if (verbos >= 1) log.Warn("Add Assembly Error : " + asm.FullName + " : " + err.Message);
                }
            }
            return all_type;
        }
        public static IReadOnlyList<Type> RegistTypes(Type[] asm, int verbos = 1)
        {
            lock (all_lock)
            {
                InitAllType();
                try
                {
                    foreach (var t in asm)
                    {
                        RegistType(t, verbos);
                    }
                }
                catch (Exception err)
                {
                    if (verbos >= 2) log.Warn(err);
                }
            }
            return all_type;
        }

        #endregion

        //------------------------------------------------------------------------------------------------- 
        #region Assembly

        public static List<Assembly> LoadDlls(int verbos = 1)
        {
            var dir = new FileInfo(typeof(ReflectionUtil).Assembly.Location).Directory;
            return LoadDlls(dir, null, null, null, verbos);
        }
        public static List<Assembly> LoadDlls(DirectoryInfo dirinfo, int verbos = 1)
        {
            return LoadDlls(dirinfo, null, null, null, verbos);
        }
        public static List<Assembly> LoadDllsNoLock(DirectoryInfo dirinfo, int verbos = 1)
        {
            return LoadDlls(dirinfo, null, (d, f, n) =>
            {
                if (verbos >= 1) log.Info("Load Raw Assembly : " + f.Name);
                var raw = File.ReadAllBytes(f.FullName);
                return d.Load(raw);
            }, null, verbos);
        }
        public static List<Assembly> LoadDlls(DirectoryInfo dirinfo, Predicate<FileInfo> filter, int verbos = 1)
        {
            return LoadDlls(dirinfo, null, null, filter, verbos);
        }
        public static List<Assembly> LoadDlls(
            DirectoryInfo dirinfo,
            Func<AppDomain, FileInfo, AssemblyName> resolveAssemblyName,
            Func<AppDomain, FileInfo, AssemblyName, Assembly> loadAssembly,
            Predicate<FileInfo> filter,
            int verbos = 1)
        {
            return LoadDlls(AppDomain.CurrentDomain, dirinfo, resolveAssemblyName, loadAssembly, filter, verbos);
        }
        public static List<Assembly> LoadDlls(
            AppDomain domain,
            DirectoryInfo dirinfo,
            Func<AppDomain, FileInfo, AssemblyName> resolveAssemblyName,
            Func<AppDomain, FileInfo, AssemblyName, Assembly> loadAssembly,
            Predicate<FileInfo> filter,
            int verbos)
        {
            var dlls = new HashMap<string, Assembly>();
            var _resolve = new ResolveEventHandler((object sender, ResolveEventArgs args) =>
            {
                //程序集
                //获取加载失败的程序集的全名
                var assName = new AssemblyName(args.Name);
                //判断Dlls集合中是否有已加载的同名程序集
                if (dlls.TryGetValue(assName.FullName, out var asm))
                {
                    return asm;
                }
                return null;
            });
            var _load = new AssemblyLoadEventHandler((object sender, AssemblyLoadEventArgs args) =>
            {
                if (verbos >= 2) Console.WriteLine("LoadedAssembly : " + args.LoadedAssembly.FullName);
            });
            try
            {
                domain.AssemblyResolve += _resolve;
                domain.AssemblyLoad += _load;
                var ret = new List<Assembly>();
                foreach (var file in dirinfo.GetFiles())
                {
                    var ext = file.Extension.ToLower();
                    if ((filter != null && filter(file)) ||
                        (filter == null && ext.Equals(".dll")))
                    {
                        var asm = LoadAssemby(domain, file, verbos, resolveAssemblyName, loadAssembly);
                        if (asm != null)
                        {
                            dlls.Put(asm.FullName, asm);
                            ret.Add(asm);
                        }
                    }
                }
                return ret;
            }
            finally
            {
                domain.AssemblyResolve -= _resolve;
                domain.AssemblyLoad -= _load;
            }
        }
        public static Assembly LoadAssemby(AppDomain domain, FileInfo file, int verbos,
            Func<AppDomain, FileInfo, AssemblyName> resolveAssemblyName = null,
            Func<AppDomain, FileInfo, AssemblyName, Assembly> loadAssembly = null)
        {
            try
            {
                AssemblyName asmName = null;
                if (resolveAssemblyName != null)
                {
                    asmName = resolveAssemblyName(domain, file);
                }
                else
                {
                    asmName = AssemblyName.GetAssemblyName(file.FullName);
                }
                if (asmName != null)
                {
                    Assembly asm = ExistAssembly(domain, asmName);
                    if (asm == null)
                    {
                        if (loadAssembly != null)
                        {
                            asm = loadAssembly(domain, file, asmName);
                        }
                        else
                        {
                            try
                            {
                                asm = domain.Load(asmName);
                            }
                            catch (Exception err1)
                            {
                                if (verbos >= 2) log.Warn(err1);
                                var raw = File.ReadAllBytes(file.FullName);
                                try
                                {
                                    asm = domain.Load(raw);
                                    if (verbos >= 1) log.Info("Load Raw Assembly : " + file.Name);
                                }
                                catch (Exception err2)
                                {
                                    if (verbos >= 2) log.Warn(err2);
                                    asm = Assembly.ReflectionOnlyLoad(raw);
                                    if (verbos >= 1) log.Info("Load Raw ReflectionOnlyLoad : " + file.Name);
                                }
                            }
                        }
                    }
                    if (asm != null)
                    {
                        if (verbos >= 2) log.Info("Regist Assembly : " + file.Name);
                        RegistAssembly(asm, verbos);
                    }
                    return asm;
                }
                else
                {
                    if (verbos >= 1) log.Info("Ignore Assembly : " + file.Name);
                }
            }
            catch (BadImageFormatException berr)
            {
                if (verbos >= 2) log.Warn(berr);
                if (verbos >= 1) log.Warn("Ignore Assembly : " + file.Name);
            }
            catch (Exception err)
            {
                if (verbos >= 2) log.Error(err);
                if (verbos >= 1) log.Error("Load Assembly Error : " + file.Name + " : " + err.Message);
            }
            return null;
        }

        public static Assembly ExistAssembly(AppDomain domain, AssemblyName name)
        {
            Assembly[] asms = domain.GetAssemblies();
            foreach (Assembly asm in asms)
            {
                if (asm.FullName.Equals(name.FullName))
                {
                    return asm;
                }
            }
            return null;
        }
        public static Type LoadTypeFromDLL<T>(string path)
        {
            Assembly asm = Assembly.ReflectionOnlyLoadFrom(path);
            asm = AppDomain.CurrentDomain.Load(asm.FullName);
            if (asm != null) { RegistAssembly(asm, 1); }
            Type data_plugin_type = ReflectionUtil.FindTypeFromAssembly(asm, typeof(T));
            return data_plugin_type;
        }
        public static T LoadInterfaceFromDLL<T>(string path)
        {
            Assembly asm = Assembly.ReflectionOnlyLoadFrom(path);
            asm = AppDomain.CurrentDomain.Load(asm.FullName);
            if (asm != null) { RegistAssembly(asm, 1); }
            Type data_plugin_type = ReflectionUtil.FindTypeFromAssembly(asm, typeof(T));
            T formula = (T)ReflectionUtil.CreateInstance(data_plugin_type);
            return formula;
        }
        public static T LoadInterfaceFromAssemblyName<T>(string assembly_name)
        {
            Assembly asm = AppDomain.CurrentDomain.Load(assembly_name);
            if (asm != null) { RegistAssembly(asm, 1); }
            Type data_plugin_type = ReflectionUtil.FindTypeFromAssembly(asm, typeof(T));
            T formula = (T)ReflectionUtil.CreateInstance(data_plugin_type);
            return formula;
        }
        public static Type FindTypeFromAssembly(Assembly assembly, Type baseType)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (baseType.IsAssignableFrom(type))
                {
                    return type;
                }
            }
            return null;
        }
        public static List<Type> FindTypesFromAssembly(Assembly assembly, Type baseType)
        {
            var ret = new List<Type>();
            foreach (Type type in assembly.GetTypes())
            {
                if (baseType.IsAssignableFrom(type))
                {
                    ret.Add(type);
                }
            }
            return ret;
        }
        public static List<Type> FindTypesFromAssembly(Assembly assembly, Predicate<Type> func)
        {
            var ret = new List<Type>();
            foreach (Type type in assembly.GetTypes())
            {
                if (func(type))
                {
                    ret.Add(type);
                }
            }
            return ret;
        }

        #endregion
        //-------------------------------------------------------------------------------------------------
        #region Type
        //private static List<Type> base_types = new List<Type>();
        public static bool IsReflectibleType(this Type type)
        {
            if (type.IsDefined(typeof(ReflectibleAttribute), true))
            //if (type.TryGetAttribute<ReflectibleAttribute>(out var reflectible))
            {
                return true;
            }
            foreach (var sub in type.GetInterfaces())
            {
                if (IsReflectibleType(sub))
                {
                    return true;
                }
            }
            return false;
        }
        public static Boolean IsAnonymousType(this Type type)
        {
            Boolean hasCompilerGeneratedAttribute = type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Length > 0;
            Boolean nameContainsAnonymousType = type.FullName.Contains("AnonymousType");
            Boolean isAnonymousType = hasCompilerGeneratedAttribute && nameContainsAnonymousType;

            return isAnonymousType;
        }
        public static IEnumerable<Type> GetRuntimeTypes(Assembly asm)
        {
            var list = new List<Type>();
            {
                foreach (var type in asm.GetTypes())
                {
                    list.Add(type);
                }
            }
            return list;
        }
        public static List<Type> GetRuntimeTypes()
        {
            var list = new List<Type>();
            var asms = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in asms)
            {
                try
                {
                    foreach (var type in asm.GetTypes())
                    {
                        list.Add(type);
                    }
                }
                catch (Exception err)
                {
                    log.Error(err);
                }
            }
            return list;
        }
        //-------------------------------------------------------------------------------------------------
        public static Type GetType(string text)
        {
            if (text == null) return null;
            if (types_cache.TryGetValue(text, out var ret))
            {
                return ret;
            }
            lock (types_cache)
            {
                InitAllType();
                var type = Type.GetType(text);
                if (type != null)
                {
                    types_cache.Put(text, type);
                    return type;
                }
                type = GetTypeFromGeneric(text);
                if (type != null)
                {
                    types_cache.Put(text, type);
                    return type;
                }
                type = GetTypeFromAssemble(text);
                if (type != null)
                {
                    types_cache.Put(text, type);
                    return type;
                }
            }
            return ret;
        }

        class ClassTextNode : TextRegion<ClassTextNode>
        {
            protected override void OnAttachToParent(TextRegion root)
            {

            }
        }
        //DeepCore.HashMap`2[[System.String, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[DeepCore.ArrayList`1[[DeepMetaGame.Data.Template.IUnitTemplateAbility, DeepMetaGame.Data, Version=1.2.10.0, Culture=neutral, PublicKeyToken=null]], DeepCore, Version=1.2.17.0, Culture=neutral, PublicKeyToken=null]]
        //[DeepCore.ArrayList`1[[DeepMetaGame.Data.Template.IUnitTemplateAbility, DeepMetaGame.Data, Version=1.2.10.0, Culture=neutral, PublicKeyToken=null]]
        public static Type GetTypeFromGeneric(string text)
        {
            if (text.TryIndexOf('`', out var L) && text.TryIndexOf('[', out var R))
            {
                var root = TextRegion.GetTextRegion<ClassTextNode>(text, TextRegionConfig.GET_TYPES);
                return GetTypeFromGeneric(root);
            }
            return null;
        }
        private static Type GetTypeFromGeneric(ClassTextNode root)
        {
            var text = root.Text;
            if (text.TryIndexOf('`', out var L) && text.TryIndexOf('[', out var R))
            {
                try
                {
                    var baseName = text.Substring(0, R);
                    var baseType = GetType(baseName);
                    var parmCountText = text.Substring(L + 1, R - L - 1);
                    var parmCount = int.Parse(parmCountText);
                    if (baseType != null)
                    {
                        var pnode = root[0];
                        var paramArgs = new Type[parmCount];
                        for (int i = 0; i < paramArgs.Length; i++)
                        {
                            paramArgs[i] = GetType(pnode[i].Text);
                        }
                        return baseType.MakeGenericType(paramArgs);
                    }
                }
                catch { }
            }
            return null;
        }


        public static Type GetTypeFromAssemble(string text)
        {
            var asms = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in asms)
            {
                try
                {
                    var type = asm.GetType(text, false, true);
                    if (type != null)
                    {
                        return type;
                    }
                }
                catch (Exception err)
                {
                    log.Error(err);
                }
            }
            return null;
        }
        //---------------------------------------------------------------------------------------------------------
        public static object CreateInstance(string type, params object[] args)
        {
            Type data_plugin_type = ReflectionUtil.GetType(type);
            if (data_plugin_type == null)
            {
                throw new Exception("Can not find type : " + type);
            }
            return ReflectionUtil.CreateInstance(data_plugin_type, args);
        }
        public static object CreateInstance(string type)
        {
            Type data_plugin_type = ReflectionUtil.GetType(type);
            if (data_plugin_type == null)
            {
                throw new Exception("Can not find type : " + type);
            }
            return ReflectionUtil.CreateInstance(data_plugin_type);
        }
        public static object CreateInstance(Type type, params object[] args)
        {
            try
            {
                if (type.IsArray)
                {
                    int[] lengths = new int[type.GetArrayRank()];
                    for (int i = 0; i < lengths.Length; i++)
                    {
                        if (i < args.Length)
                        {
                            lengths[i] = (int)args[i];
                        }
                        else
                        {
                            lengths[i] = 0;
                        }
                    }
                    var elementType = type.GetElementType();
                    return Array.CreateInstance(elementType, lengths);
                }
                else
                {
                    return DeepActivator.CreateInstance(type, args);
                }
            }
            catch (Exception err)
            {
                throw new Exception(err.Message + " : " + type.FullName, err);
            }
        }
        public static object CreateInstance(Type type)
        {
            return CreateInstance(type, new object[0]);
        }
        public static T CreateInstance<T>(Type type)
        {
            return (T)CreateInstance(type, new object[0]);
        }
        public static T CreateInstance<T>(Type type, params object[] args)
        {
            return (T)CreateInstance(type, args);
        }
        //---------------------------------------------------------------------------------------------------------
        public static T CreateInterface<T>(string type)
        {
            return CreateInterface<T>(type, new object[0]);
        }
        public static T CreateInterface<T>(Type type)
        {
            return CreateInterface<T>(type, new object[0]);
        }
        public static T CreateInterface<T>(string type, params object[] args)
        {
            Type data_plugin_type = ReflectionUtil.GetType(type);
            if (data_plugin_type == null)
            {
                throw new Exception("Can not find type : " + type);
            }
            T formula = (T)ReflectionUtil.CreateInstance(data_plugin_type, args);
            return formula;
        }
        public static T CreateInterface<T>(Type type, params object[] args)
        {
            T formula = (T)ReflectionUtil.CreateInstance(type, args);
            return formula;
        }
        //---------------------------------------------------------------------------------------------------------
        public static object CreateGenericInstance(Type type, params Type[] typeArguments)
        {
            var g_type = type.MakeGenericType(typeArguments);
            return ReflectionUtil.CreateInstance(g_type);
        }
        public static T CreateGenericInstance<T>(Type type, params Type[] typeArguments)
        {
            var g_type = type.MakeGenericType(typeArguments);
            return (T)ReflectionUtil.CreateInstance(g_type);
        }
        public static IList CreateGenericArrayList(params Type[] typeArguments)
        {
            return ReflectionUtil.CreateGenericInstance<IList>(typeof(ArrayList<>), typeArguments);
        }
        public static IDictionary CreateGenericHashMap(params Type[] typeArguments)
        {
            return ReflectionUtil.CreateGenericInstance<IDictionary>(typeof(HashMap<,>), typeArguments);
        }

        //------------------------------------------------------------------------------------------------------------------------------------------

        public static List<Type> GetSubTypes(Type superType, bool sort = false)
        {
            List<Type> ret = new List<Type>();
            foreach (Type sub in RegisteredTypes)
            {
                if (sub.IsClass && superType.IsAssignableFrom(sub))
                {
                    ret.Add(sub);
                }
            }
            if (sort) ret.Sort(new TypeComparer());
            return ret;
        }
        public static List<Type> GetNoneVirtualTypes(IEnumerable<Type> types, bool sort = false)
        {
            List<Type> ret = new List<Type>();
            foreach (Type sub in types)
            {
                if (sub.IsClass && !sub.IsAbstract)
                {
                    ret.Add(sub);
                }
            }
            if (sort) ret.Sort(new TypeComparer());
            return ret;
        }

        /// <summary>
        /// 获取所有匹配的非虚子类
        /// </summary>
        /// <param name="superType"></param>
        /// <returns></returns>
        public static List<Type> GetNoneVirtualSubTypes(Type superType, bool sort = false)
        {
            List<Type> ret = new List<Type>();
            foreach (Type sub in RegisteredTypes)
            {
                if (sub.IsClass && !sub.IsAbstract && superType.IsAssignableFrom(sub))
                {
                    ret.Add(sub);
                }
            }
            if (sort) ret.Sort(new TypeComparer());
            return ret;
        }
        public static List<Type> GetNoneVirtualSubTypes(Type superType, Assembly asm, bool sort = false)
        {
            List<Type> ret = new List<Type>();
            var types = GetRuntimeTypes(asm);
            foreach (Type sub in types)
            {
                if (sub.IsClass && !sub.IsAbstract && superType.IsAssignableFrom(sub))
                {
                    ret.Add(sub);
                }
            }
            if (sort) ret.Sort(new TypeComparer());
            return ret;
        }

        /// <summary>
        /// 获取所有匹配的非虚子类，每个类必须标注DescAttribute
        /// </summary>
        /// <param name="superType"></param>
        /// <returns></returns>
        public static List<TypeDescAttribute> GetNoneVirtualSubDescTypes(Type superType, bool sort = false)
        {
            List<TypeDescAttribute> ret = new List<TypeDescAttribute>();
            foreach (Type sub in RegisteredTypes)
            {
                if (sub.IsClass && !sub.IsAbstract && superType.IsAssignableFrom(sub))
                {
                    DescAttribute attr = PropertyUtil.GetAttribute<DescAttribute>(sub);
                    if (attr != null)
                    {
                        ret.Add(new TypeDescAttribute(sub));
                    }
                }
            }
            if (sort) ret.Sort(new ToStringComparer<TypeDescAttribute>());
            return ret;
        }
        public static List<TypeDescAttribute> GetNoneVirtualSubDescTypes(Type superType, Assembly asm, bool sort = false)
        {
            List<TypeDescAttribute> ret = new List<TypeDescAttribute>();
            Type[] types = asm.GetTypes();
            foreach (Type sub in types)
            {
                if (sub.IsClass && !sub.IsAbstract && superType.IsAssignableFrom(sub))
                {
                    DescAttribute attr = PropertyUtil.GetAttribute<DescAttribute>(sub);
                    if (attr != null)
                    {
                        ret.Add(new TypeDescAttribute(sub));
                    }
                }
            }
            if (sort) ret.Sort(new ToStringComparer<TypeDescAttribute>());
            return ret;
        }

        public static object CreateDefaultInstance(Type type)
        {
            //             if (type.IsArray)
            //             {
            //                 return Array.CreateInstance(type.GetElementType(), 0);
            //             }
            //             else if (type.GetInterface(typeof(IDictionary).Name) != null)
            //             {
            //                 var map = (IDictionary)g2dpp.FieldValue;
            //                 map.Clear();
            //             }
            //             else if (g2dpp.DecleardFieldType.GetInterface(typeof(IList).Name) != null)
            //             {
            //                 var list = (IList)g2dpp.FieldValue;
            //                 list.Clear();
            //             }
            try
            {
                if (type.IsInterface || type.IsAbstract)
                {
                    foreach (var sub in RegisteredTypes)
                    {
                        if (sub.IsClass && !sub.IsAbstract && type.IsAssignableFrom(sub))
                        {
                            return DeepActivator.CreateInstance(sub);
                        }
                    }
                }
                return DeepActivator.CreateInstance(type);
            }
            catch
            {
                return null;
            }
        }

        public static bool TryChangeType(object src, Type dstType, Type declearType, out object dst)
        {
            if (declearType.IsInstanceOfType(src) && declearType.IsAssignableFrom(dstType))
            {
                dst = DeepActivator.CreateInstance(dstType);
                PropertyUtil.CopyFieldsTo(src, dst);
                return true;
            }
            dst = src;
            return false;
        }

        #endregion
        //------------------------------------------------------------------------------------------------------------------------------------------
        #region Method

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fulltext"></param>
        /// <returns></returns>
        public static MethodInfo GetStaticMethod(string fulltext)
        {
            int index_type = fulltext.IndexOf(';');
            if (index_type > 0 && index_type < fulltext.Length - 1)
            {
                var type = GetType(fulltext.Substring(index_type + 1));
                var index_method = fulltext.IndexOf('(');
                if (index_method > index_type && index_method < fulltext.Length - 1)
                {
                    var method_name = fulltext.Substring(index_type + 1, index_method - index_type - 1);
                    // check args
                    if (type != null && CUtils.GetTextRegion(fulltext, '(', ')', out var method_args))
                    {
                        var args_text = method_args.Split(',');
                        var args_types = new Type[args_text.Length];
                        for (int i = 0; i < args_text.Length; i++)
                        {
                            args_types[i] = GetType(args_text[i]);
                        }
                        return type.GetMethod(method_name, args_types);
                    }
                }
                else
                {
                    return type.GetMethod(fulltext.Substring(index_type + 1));
                }
            }
            return null;
        }
        /// <summary>
        /// DeepCrystal.ORM.ORMFactory;IsMappingType
        /// </summary>
        public static object CallStaticMethod(string fulltext, params object[] args)
        {
            var path = fulltext.Split(';');
            var type = GetType(path[0]);
            if (type != null)
            {
                var method = type.GetMethod(path[1]);
                if (method != null)
                {
                    return method.Invoke(null, args);
                }
            }
            return null;
        }
        public static T CallStaticMethod<T>(string fulltext, params object[] args)
        {
            var path = fulltext.Split(';');
            var type = GetType(path[0]);
            if (type != null)
            {
                var method = type.GetMethod(path[1]);
                if (method != null)
                {
                    return (T)method.Invoke(null, args);
                }
            }
            return default(T);
        }

        #endregion
        //------------------------------------------------------------------------------------------------------------------------------------------
    }

    public static class ReflectionExt
    {
        public static object ToDefaultValue(this Type type)
        {
            if (typeof(string) == (type)) return string.Empty;
            if (type.IsPrimitive)
            {
                if (typeof(bool) == (type)) return (bool)false;
                if (typeof(int) == (type)) return (int)0;
                if (typeof(uint) == (type)) return (uint)0;
                if (typeof(long) == (type)) return (long)0;
                if (typeof(ulong) == (type)) return (ulong)0;
                if (typeof(short) == (type)) return (short)0;
                if (typeof(ushort) == (type)) return (ushort)0;
                if (typeof(float) == (type)) return (float)0;
                if (typeof(double) == (type)) return (double)0;
                if (typeof(byte) == (type)) return (byte)0;
                if (typeof(sbyte) == (type)) return (sbyte)0;
                if (typeof(char) == (type)) return (char)0;
            }
            if (typeof(byte[]) == (type)) return new byte[0];
            if (typeof(DateTime) == (type)) return DateTime.Now;
            if (typeof(TimeSpan) == (type)) return TimeSpan.FromSeconds(0);
            if (type == typeof(BigInteger)) return new BigInteger(0);
            if (type.IsEnum) return Enum.GetValues(type).GetValue(0);
            if (type.IsArray) return Array.CreateInstance(type.GetElementType(), 0);
            return DeepActivator.CreateInstance(type);
        }

        public static string ToPrimitiveName(this Type type)
        {
            if (type == typeof(object)) return "object";
            if (type == typeof(string)) return "string";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(byte)) return "byte";
            if (type == typeof(char)) return "char";
            if (type == typeof(decimal)) return "decimal";
            if (type == typeof(double)) return "double";
            if (type == typeof(short)) return "short";
            if (type == typeof(int)) return "int";
            if (type == typeof(long)) return "long";
            if (type == typeof(sbyte)) return "sbyte";
            if (type == typeof(float)) return "float";
            if (type == typeof(ushort)) return "ushort";
            if (type == typeof(uint)) return "uint";
            if (type == typeof(ulong)) return "ulong";
            if (type == typeof(void)) return "void";
            return null;
        }

        public static string ToNoGenericName(this Type type)
        {
            var pname = type.ToPrimitiveName();
            if (pname != null) { return pname; }
            if (type.IsGenericType)
            {
                var sb = new StringBuilder();
                {
                    int tidx = type.Name.IndexOf('`');
                    if (tidx > 0)
                    {
                        sb.Append(type.Name.Substring(0, tidx));
                    }
                    else
                    {
                        sb.Append(type.Name);
                    }
                    return sb.ToString();
                }
            }
            return type.Name;
        }

        public static string ToNoGenericFullName(this Type type)
        {
            var pname = type.ToPrimitiveName();
            if (pname != null) { return pname; }
            if (type.IsGenericType)
            {
                var sb = new StringBuilder();
                {
                    int tidx = type.FullName.IndexOf('`');
                    if (tidx > 0)
                    {
                        sb.Append(type.FullName.Substring(0, tidx));
                    }
                    else
                    {
                        sb.Append(type.FullName);
                    }
                    return sb.ToString();
                }
            }
            return type.FullName;
        }

        /// <summary>
        /// 返回书写习惯的类型字符串
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string ToTypeDefineName(this Type type)
        {
            var pname = type.ToPrimitiveName();
            if (pname != null) { return pname; }
            if (type.IsGenericType)
            {
                var sb = new StringBuilder();
                {
                    int tidx = type.Name.IndexOf('`');
                    if (tidx > 0)
                    {
                        sb.Append(type.Name.Substring(0, tidx));
                    }
                    else
                    {
                        sb.Append(type.Name);
                    }
                    sb.Append("<");
                    Type[] g_args = type.GetGenericArguments();
                    for (int i = 0; i < g_args.Length; i++)
                    {
                        sb.Append(ToTypeDefineName(g_args[i]));
                        if (i < g_args.Length - 1)
                        {
                            sb.Append(", ");
                        }
                    }
                    sb.Append(">");
                    return sb.ToString();
                }
            }
            return type.Name;
        }


        /// <summary>
        /// 返回书写习惯的类型字符串
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string ToTypeDefineFullName(this Type type,
            string L = "<",
            string R = ">",
            string ARG = ", ",
            string DOT = ".")
        {
            var pname = type.ToPrimitiveName();
            if (pname != null) { return pname; }
            if (type.IsGenericType)
            {
                var sb = new StringBuilder();
                {
                    int tidx = type.FullName.IndexOf('`');
                    if (tidx > 0)
                    {
                        sb.Append(type.FullName.Substring(0, tidx));
                    }
                    else
                    {
                        sb.Append(type.FullName);
                    }
                    sb.Append(L);
                    Type[] g_args = type.GetGenericArguments();
                    for (int i = 0; i < g_args.Length; i++)
                    {
                        sb.Append(ToTypeDefineFullName(g_args[i], L, R, ARG, DOT));
                        if (i < g_args.Length - 1)
                        {
                            sb.Append(ARG);
                        }
                    }
                    sb.Append(R);
                    return sb.ToString().Replace("+", DOT);
                }
            }
            else if (type.IsGenericParameter)
            {
                return type.Name;
            }
            return type.FullName.Replace("+", DOT);
        }

        public static string ToTypeDefineFullNameNoArgs(this Type type,
      string L = "<",
      string R = ">",
      string ARG = ", ",
      string DOT = ".")
        {
            var pname = type.ToPrimitiveName();
            if (pname != null) { return pname; }
            if (type.IsGenericType)
            {
                var sb = new StringBuilder();
                {
                    int tidx = type.FullName.IndexOf('`');
                    if (tidx > 0)
                    {
                        sb.Append(type.FullName.Substring(0, tidx));
                    }
                    else
                    {
                        sb.Append(type.FullName);
                    }
                    sb.Append(L);
                    Type[] g_args = type.GetGenericArguments();
                    for (int i = 0; i < g_args.Length; i++)
                    {
                        //sb.Append(ToTypeDefineFullName(g_args[i], L, R, ARG, DOT));
                        if (i < g_args.Length - 1)
                        {
                            sb.Append(ARG);
                        }
                    }
                    sb.Append(R);
                    return sb.ToString().Replace("+", DOT);
                }
            }
            else if (type.IsGenericParameter)
            {
                return type.Name;
            }
            return type.FullName.Replace("+", DOT);
        }

        /// <summary>
        /// 返回书写习惯的类型字符串
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string ToTypeDefineFullName(this Assembly asm)
        {
            var d = asm.FullName.IndexOf(',');
            if (d > 0)
            {
                return asm.FullName.Substring(0, d);
            }
            return asm.FullName;
        }

        public static string ToSaveFullName(this Type type) => ToTypeDefineFullName(type, "[", "]", ",", ".");


        public static bool IsPrimitiveData(this Type objType)
        {
            if (objType.IsEnum) return true;
            if (objType.IsPrimitive) return true;
            if (objType == typeof(string)) return true;
            if (objType == typeof(DateTime)) return true;
            if (objType == typeof(TimeSpan)) return true;
            if (objType == typeof(BigInteger)) return true;
            return false;
        }
        public static bool IsGenericList(this Type type)
        {
            return type.IsGenericType && type.IsInterfaceOf(typeof(IList));
        }
        public static bool IsGenericMap(this Type type)
        {
            return type.IsGenericType && type.IsInterfaceOf(typeof(IDictionary));
        }
        public static bool IsGenericList(this Type type, out Type etype)
        {
            if (type.IsGenericType && type.IsInterfaceOf(typeof(IList)))
            {
                etype = type.GetGenericArguments()[0];
                return true;
            }
            etype = null;
            return false;
        }
        public static bool IsGenericMap(this Type type, out Type ktype, out Type vtype)
        {
            if (type.IsGenericType && type.IsInterfaceOf(typeof(IDictionary)))
            {
                var kv = type.GetGenericArguments();
                ktype = kv[0];
                vtype = kv[1];
                return true;
            }
            ktype = null;
            vtype = null;
            return false;
        }
        public static bool IsGenericType(this Type type, out Type etype)
        {
            if (type.IsGenericType)
            {
                var kv = type.GetGenericArguments();
                etype = kv[0];
                return true;
            }
            etype = null;
            return false;
        }
        public static bool IsGenericTypeArgs(this Type type, out Type[] etype)
        {
            if (type.IsGenericType)
            {
                var kv = type.GetGenericArguments();
                etype = kv;
                return true;
            }
            etype = null;
            return false;
        }

        public static bool IsInterfaceOf(this Type type, Type interfaceType)
        {
            return Array.Exists(type.GetInterfaces(), t => t == interfaceType);
        }

        public static bool IsGenericTypeOf(this Type type, Type genericType)
        {
            if (type.IsGenericType)
            {
                return (type.GetGenericTypeDefinition() == genericType);
            }
            return false;
        }
        public static bool IsGenericArgumentsOf(this Type type, params Type[] genericArgs)
        {
            if (type.IsGenericType)
            {
                return CUtils.ArraysEqual(type.GetGenericArguments(), genericArgs);
            }
            return false;
        }
        public static bool IsGenericInterfaceOf(this Type type, Type interfaceType, params Type[] genericArgs)
        {
            if (type.IsGenericType)
            {
                if (IsInterfaceOf(type, interfaceType))
                {
                    return CUtils.ArraysEqual(type.GetGenericArguments(), genericArgs);
                }
            }
            return false;
        }
    }

    public static class PropertyExt
    {
#if false
        public static Attribute GetAttributeByType(this Type field, Type attributeType, bool inherit = true)
        {
            try
            {
                object[] cas = field.GetCustomAttributes(attributeType, inherit);
                if (cas != null && cas.Length > 0)
                {
                    return cas[0] as Attribute;
                }
                if (inherit)
                {
                    foreach (var inter in field.GetInterfaces())
                    {
                        if (inter.GetAttributeByType(attributeType) is Attribute tt)
                        {
                            return tt;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace();
            }
            return null;
        }

        public static T GetAttribute<T>(this Type member, bool inherit = true) where T : System.Attribute
        {
            try
            {
                object[] cas = member.GetCustomAttributes(typeof(T), inherit);
                if (cas != null && cas.Length > 0)
                {
                    return cas[0] as T;
                }
                if (inherit)
                {
                    foreach (var inter in member.GetInterfaces())
                    {
                        if (inter.GetAttribute<T>() is T tt)
                        {
                            return tt;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace();
            }
            return null;
        }


        public static Attribute[] GetAttributesByType(this Type field, Type attributeType, bool inherit = true)
        {
            try
            {
                object[] cas = field.GetCustomAttributes(attributeType, inherit);
                if (cas != null && cas.Length > 0)
                {
                    return Array.ConvertAll(cas, e => (Attribute)e);
                }
                if (inherit)
                {
                    foreach (var inter in field.GetInterfaces())
                    {
                        if (inter.GetAttributesByType(attributeType) is Attribute[] tt)
                        {
                            return tt;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace();
            }
            return null;
        }

        public static T[] GetAttributes<T>(this Type member, bool inherit = true) where T : System.Attribute
        {
            try
            {
                object[] cas = member.GetCustomAttributes(typeof(T), inherit);
                if (cas != null && cas.Length > 0)
                {
                    return Array.ConvertAll(cas, e => (T)e);
                }
                if (inherit)
                {
                    foreach (var inter in member.GetInterfaces())
                    {
                        if (inter.GetAttributes<T>() is T[] tt)
                        {
                            return tt;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace();
            }
            return null;
        }


        public static bool TryGetAttribute<T>(this Type member, out T attr, bool inherit = true) where T : System.Attribute
        {
            attr = GetAttribute<T>(member, inherit);
            return attr != null;
        }
        public static bool TryGetAttributes<T>(this Type member, out T[] attr, bool inherit = true) where T : System.Attribute
        {
            attr = member.GetAttributes<T>();
            return attr != null && attr.Length > 0;
        }

#endif
        //-----------------------------------------------------------------------------------------------------------------------------------------------------


        public static Attribute GetAttributeByType(this ICustomAttributeProvider field, Type attributeType, bool inherit = true)
        {
            try
            {
                object[] cas = field.GetCustomAttributes(attributeType, inherit);
                if (cas != null && cas.Length > 0)
                {
                    return cas[0] as Attribute;
                }
                if (inherit && field is Type mtype)
                {
                    foreach (var inter in mtype.GetInterfaces())
                    {
                        if (inter.GetAttributeByType(attributeType, false) is Attribute tt)
                        {
                            return tt;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace();
            }
            return null;
        }

        public static Attribute GetAttributeByName(this ICustomAttributeProvider field, string name, bool inherit = true)
        {
            try
            {
                object[] cas = field.GetCustomAttributes(inherit);
                if (cas != null && cas.Length > 0)
                {
                    return Array.Find(cas, a => a.GetType().Name == name) as Attribute;
                }
                if (inherit && field is Type mtype)
                {
                    foreach (var inter in mtype.GetInterfaces())
                    {
                        if (inter.GetAttributeByName(name, false) is Attribute tt)
                        {
                            return tt;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace();
            }
            return null;
        }

        public static T GetAttribute<T>(this ICustomAttributeProvider member, bool inherit = true) where T : System.Attribute
        {
            try
            {
                object[] cas = member.GetCustomAttributes(typeof(T), inherit);
                if (cas != null && cas.Length > 0)
                {
                    return cas[0] as T;
                }
                if (inherit && member is Type mtype)
                {
                    foreach (var inter in mtype.GetInterfaces())
                    {
                        if (inter.GetAttribute<T>(false) is T tt)
                        {
                            return tt;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace();
            }
            return null;
        }


        public static Attribute[] GetAttributesByType(this ICustomAttributeProvider field, Type attributeType, bool inherit = true)
        {
            try
            {
                object[] cas = field.GetCustomAttributes(attributeType, inherit);
                if (cas != null && cas.Length > 0)
                {
                    return Array.ConvertAll(cas, e => (Attribute)e);
                }
                if (inherit && field is Type mtype)
                {
                    foreach (var inter in mtype.GetInterfaces())
                    {
                        if (inter.GetAttributesByType(attributeType, false) is Attribute[] tt)
                        {
                            return tt;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace();
            }
            return null;
        }

        public static Attribute[] GetAttributesByName(this ICustomAttributeProvider field, string name, bool inherit = true)
        {
            try
            {
                object[] cas = field.GetCustomAttributes(inherit);
                if (cas != null && cas.Length > 0)
                {
                    return Array.ConvertAll(Array.FindAll(cas, a => a.GetType().Name == name), e => (Attribute)e);
                }
                if (inherit && field is Type mtype)
                {
                    foreach (var inter in mtype.GetInterfaces())
                    {
                        if (inter.GetAttributesByName(name, false) is Attribute[] tt)
                        {
                            return tt;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace();
            }
            return null;
        }

        public static T[] GetAttributes<T>(this ICustomAttributeProvider member, bool inherit = true) where T : System.Attribute
        {
            try
            {
                object[] cas = member.GetCustomAttributes(typeof(T), inherit);
                if (cas != null && cas.Length > 0)
                {
                    return Array.ConvertAll(cas, e => (T)e);
                }
                if (inherit && member is Type mtype)
                {
                    foreach (var inter in mtype.GetInterfaces())
                    {
                        if (inter.GetAttributes<T>(false) is T[] tt)
                        {
                            return tt;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace();
            }
            return null;
        }

        public static bool TryGetAttributeByType(this ICustomAttributeProvider member, Type attrType, out Attribute attr, bool inherit = true)
        {
            attr = GetAttributeByType(member, attrType, inherit);
            return attr != null;
        }
        public static bool TryGetAttributesByType(this ICustomAttributeProvider member, Type attrType, out Attribute[] attr, bool inherit = true)
        {
            attr = GetAttributesByType(member, attrType, inherit);
            return attr != null;
        }
        public static bool TryGetAttribute<T>(this ICustomAttributeProvider member, out T attr, bool inherit = true) where T : System.Attribute
        {
            attr = GetAttribute<T>(member, inherit);
            return attr != null;
        }
        public static bool TryGetAttributes<T>(this ICustomAttributeProvider member, out T[] attr, bool inherit = true) where T : System.Attribute
        {
            attr = member.GetAttributes<T>();
            return attr != null && attr.Length > 0;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------
        public static bool IsNumberType(this Type type)
        {
            if (type == typeof(sbyte)) return true;
            if (type == typeof(byte)) return true;
            if (type == typeof(short)) return true;
            if (type == typeof(ushort)) return true;
            if (type == typeof(int)) return true;
            if (type == typeof(uint)) return true;
            if (type == typeof(long)) return true;
            if (type == typeof(ulong)) return true;
            if (type == typeof(float)) return true;
            if (type == typeof(double)) return true;
            if (type == typeof(decimal)) return true;
            return false;
        }
        public static object TextNumberAdd(this Type type, string textValue, decimal add)
        {
            if (type == typeof(sbyte)) { return (sbyte)(Parser.StringToObject<sbyte>(textValue) + (decimal)add); }
            if (type == typeof(byte)) { return (byte)(Parser.StringToObject<byte>(textValue) + (decimal)add); }
            if (type == typeof(short)) { return (short)(Parser.StringToObject<short>(textValue) + (decimal)add); }
            if (type == typeof(ushort)) { return (ushort)(Parser.StringToObject<ushort>(textValue) + (decimal)add); }
            if (type == typeof(int)) { return (int)(Parser.StringToObject<int>(textValue) + (decimal)add); }
            if (type == typeof(uint)) { return (uint)(Parser.StringToObject<uint>(textValue) + (decimal)add); }
            if (type == typeof(long)) { return (long)(Parser.StringToObject<long>(textValue) + (decimal)add); }
            if (type == typeof(ulong)) { return (ulong)(Parser.StringToObject<ulong>(textValue) + (decimal)add); }
            if (type == typeof(float)) { return (float)(Parser.StringToObject<float>(textValue) + (float)add); }
            if (type == typeof(double)) { return (double)(Parser.StringToObject<double>(textValue) + (double)add); }
            if (type == typeof(decimal)) { return (decimal)(Parser.StringToObject<decimal>(textValue) + (decimal)add); }
            return null;
        }
    }
}