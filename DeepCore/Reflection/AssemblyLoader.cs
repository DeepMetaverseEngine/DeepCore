using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DeepCore.Reflection
{
    public class AssemblyLoader : Disposable
    {
        protected struct AsmInfo
        {
            public Assembly asm;
            public FileInfo path;
        }

        protected readonly AppDomain domain;
        protected readonly HashMap<string, AsmInfo> dlls = new HashMap<string, AsmInfo>();
        protected readonly HashMap<string, AsmInfo> asms = new HashMap<string, AsmInfo>();
        protected readonly List<Assembly> list = new List<Assembly>();

        public Assembly[] LoadedAssembies { get { return list.ToArray(); } }

        public AssemblyLoader(AppDomain domain)
        {
            this.domain = domain;
            this.domain.AssemblyResolve += this.Domain_AssemblyResolve;
            this.domain.AssemblyLoad += this.Domain_AssemblyLoad;
        }
        protected override void Disposing()
        {
            this.domain.AssemblyResolve -= this.Domain_AssemblyResolve;
            this.domain.AssemblyLoad -= this.Domain_AssemblyLoad;
        }

        public virtual Type FindType(string fullName)
        {
            foreach (var dll in list)
            {
                var type = dll.GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }
            return null;
        }

        public virtual List<Assembly> LoadDlls(FileInfo[] dlls)
        {
            List<Assembly> ret = new List<Assembly>(dlls.Length);
            foreach (FileInfo file in dlls)
            {
                var asm = LoadDll(file);
                if (asm != null)
                {
                    ret.Add(asm);
                }
            }
            return ret;
        }
        public List<Assembly> LoadDlls(DirectoryInfo dirinfo)
        {
            List<Assembly> ret = new List<Assembly>();
            foreach (FileInfo file in dirinfo.GetFiles())
            {
                var ext = file.Extension.ToLower();
                if (ext.Equals(".dll") || ext.Equals(".exe"))
                {
                    var asm = LoadDll(file);
                    if (asm != null)
                    {
                        ret.Add(asm);
                    }
                }
            }
            return ret;
        }

        public virtual Assembly LoadDll(FileInfo dll)
        {
            try
            {
                if (!dlls.ContainsKey(dll.FullName))
                {
                    var asm = LoadAssembly(dll);
                    if (asm != null)
                    {
                        asm.GetTypes();
                        dlls.Put(dll.FullName, new AsmInfo() { asm = asm, path = dll, });
                        asms.Put(asm.FullName, new AsmInfo() { asm = asm, path = dll, });
                        list.Add(asm);
                        ReflectionUtil.RegistAssembly(asm, 1);
                        return asm;
                    }
                }
            }
            catch (Exception err)
            {
                Console.Error.WriteLine($"Load Dll Error : {err.Message} : {dll.FullName}");
            }
            return null;
        }

        protected virtual Assembly LoadAssembly(FileInfo dll)
        {
            Console.WriteLine("LoadAssembly : " + dll.FullName);
            var name = AssemblyName.GetAssemblyName(dll.FullName);
            Assembly asm;
            try
            {
                asm = domain.Load(name);
            }
            catch
            {
                var raw = File.ReadAllBytes(dll.FullName);
                asm = domain.Load(raw);
            }
            return asm;
        }

        protected virtual Assembly Domain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Console.WriteLine("Domain_AssemblyResolve : " + args.Name);
            //程序集
            AsmInfo ass;
            //获取加载失败的程序集的全名
            var assName = new AssemblyName(args.Name);
            //判断Dlls集合中是否有已加载的同名程序集
            if (dlls.TryGetValue(assName.FullName, out ass))
            {
                return ass.asm;
            }
            if (asms.TryGetValue(assName.FullName, out ass))
            {
                return ass.asm;
            }
            return null;
        }
        protected virtual void Domain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            var asm = args.LoadedAssembly;
            var dll = new FileInfo(args.LoadedAssembly.Location);
            asms.TryGetOrCreate(asm.FullName, out var f1, cr => new AsmInfo() { asm = asm, path = dll });
            dlls.TryGetOrCreate(dll.FullName, out var f2, cr => new AsmInfo() { asm = asm, path = dll });
            Console.WriteLine("Domain_AssemblyLoad : " + args.LoadedAssembly.FullName);
        }
    }
}

namespace System.Reflection
{
    public static class AssemblyExt
    {
        public static DirectoryInfo LocationDirectory(this Assembly asm)
        {
            return new FileInfo(asm.Location).Directory;
        }
        public static DirectoryInfo AssemblyDirectory(this Type asm)
        {
            return new FileInfo(asm.Assembly.Location).Directory;
        }
        public static FileInfo GetExeFile(this Assembly asm)
        {
            var file = asm.Location;
            if (file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                file = file.Substring(0, file.Length - 4) + ".exe";
            }
            return new FileInfo(file);
        }
    }
}