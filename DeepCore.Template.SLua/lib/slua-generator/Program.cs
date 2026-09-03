using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SLua;
using System.Reflection;
using System.IO;

namespace generator
{
    class Program
    {
        static Type LoadTypeFromName(string name)
        {
            Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly asm in asms)
            {
                var t = asm.GetType(name);
                if (t != null)
                {
                    return t;
                }
            }
            Console.WriteLine("LoadTypeFromName failed " + name);
            return null;
        }

        
        static void Main(string[] args)
        {
            string dlldir = args[0];
            string bind_file = Path.GetFullPath(args[1]);
            string[] lines = File.ReadAllLines(bind_file);
            string dir = args[2];

            //Dictionary<string, Assembly> asss = new Dictionary<string, Assembly>();

            //AppDomain currentDomain = AppDomain.CurrentDomain;
            //currentDomain.AssemblyResolve += new ResolveEventHandler(currentDomain_AssemblyResolve);

            SLuaSetting.Instance.UnityEngineGeneratePath = dir + SLuaSetting.Instance.UnityEngineGeneratePath;

            LoadDlls(new DirectoryInfo(Path.GetFullPath(dlldir)));

            //var names = assembly.GetReferencedAssemblies();
            //foreach (var name in names)
            //{
            //    if (!asss.ContainsKey(name.FullName))
            //    {
            //        int i = 1;
            //    }
            //}
            CustomExport.OnAddCustomClass = (LuaCodeGen.ExportGenericDelegate add) =>
            {
                // below lines only used for demostrate how to add custom class to export, can be delete on your app

                add(typeof(System.Func<int>), null);
                add(typeof(System.Action<int, string>), null);
                add(typeof(System.Action<int, Dictionary<int, object>>), null);
                add(typeof(List<int>), "ListInt");
                add(typeof(Dictionary<int, string>), "DictIntStr");
                add(typeof(string), "String");

                for (int i = 0; i < lines.Length; i++)
                {
                    try
                    {
                        add(LoadTypeFromName(lines[i]), null);
                    }
                    catch (Exception e)
                    {
                        throw;
                    }
                }
                // add your custom class here
                // add( type, typename)
                // type is what you want to export
                // typename used for simplify generic type name or rename, like List<int> named to "ListInt", if not a generic type keep typename as null or rename as new type name
            };
            LuaCodeGen.Custom();
        }

        static Assembly currentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Assembly MyAssembly;
            string strTempAssmbPath = "";
            strTempAssmbPath = System.IO.Path.GetFullPath(".");
            if (!strTempAssmbPath.EndsWith("\\")) strTempAssmbPath += "\\";
            strTempAssmbPath += args.Name.Substring(0, args.Name.IndexOf(",")) + ".dll";

            MyAssembly = Assembly.LoadFrom(strTempAssmbPath);

            return MyAssembly;
        }

        public static List<Assembly> LoadDlls(DirectoryInfo dirinfo)
        {
            AppDomain domain = AppDomain.CurrentDomain;
            List<Assembly> ret = new List<Assembly>();
            foreach (FileInfo file in dirinfo.GetFiles())
            {
                var ext = file.Extension.ToLower();
                if (ext.Equals(".dll") || ext.Equals(".exe"))
                {
                    try
                    {
                        Assembly asm = Assembly.ReflectionOnlyLoadFrom(file.FullName);
                        if (!ExistAssembly(domain, asm.FullName))
                        {
                            domain.Load(asm.GetName());
                        }
                        ret.Add(asm);
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine(err.Message);
                    }
                }
            }
            return ret;
        }

    public static bool ExistAssembly(AppDomain domain, string name)
    {
        Assembly[] asms = domain.GetAssemblies();
        foreach (Assembly asm in asms)
        {
            if (asm.FullName.Equals(name))
            {
                return true;
            }
        }
        return false;
        }
    }
}