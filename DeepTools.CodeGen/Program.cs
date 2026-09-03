using DeepCore;
using DeepCore.IO;
using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace DeepTools.CodeGen
{
    public class Program
    {
        public static string Usage
        {
            get
            {
                var sb = new StringBuilder();
                sb.AppendLine("----------------------------");
                sb.AppendLine("Build");
                sb.AppendLine("-id    InputDirectory");
                sb.AppendLine("-if    InputFiles");
                sb.AppendLine("-wd    WorkDirectory");
                sb.AppendLine("-od    OutputDirectory");
                sb.AppendLine("-ext   OutputExtension");
                sb.AppendLine("-of    OutputFile");

                sb.AppendLine(" -t    TemplatFile");
                sb.AppendLine("-ns    NameSpace");
                sb.AppendLine("-mi    BeginMessageID");

                sb.AppendLine("-fts   FilterString");
                sb.AppendLine("-ftf   FilterFile");

                sb.AppendLine("-load  LoadClasses");

                sb.AppendLine("----------------------------");
                sb.AppendLine("Build Simple");
                sb.AppendLine("-pn    ProjectName");
                sb.AppendLine("-ns    NameSpace 命名空间");
                sb.AppendLine("-dll   NameSpace 命名空间");
                sb.AppendLine("----------------------------");
                sb.AppendLine("-environments.   常量前缀表");


                sb.Append("Sample: build message and orm batch" + BuildSampleUsage);

                return sb.ToString();
            }
        }
        public static string BuildSampleUsage = @"
@echo ---------------------------------------------------------------------------
@echo - GEN GS.Server.ORM
@echo ---------------------------------------------------------------------------
@set gen_dir=%ProjectDir%..\TestORM.Gen\
@set gen_ref=%TargetDir%DeepCrystal.dll
@set gen_ref=%gen_ref%;%TargetDir%TestORM.dll
@echo ---------------------------------------------------------------------------
@del /Q %gen_dir%\generated_orm\*.cs
@%TargetDir%codegen -ns:TestORM -wd:%TargetDir% -if:%gen_ref% -od:%gen_dir%\generated_orm        -t:csharp-orm.xml                           
@%TargetDir%codegen -ns:TestORM -wd:%TargetDir% -if:%gen_ref% -od:%gen_dir%\generated_orm        -t:csharp-orm-ids.xml -of:%gen_dir%\generated_orm\auto.cs                   
@%TargetDir%codegen -ns:TestORM -wd:%TargetDir% -if:%gen_ref% -od:%gen_dir%\generated_orm        -t:csharp-trm.xml 
@%TargetDir%codegen -ns:TestORM -wd:%TargetDir% -if:%gen_ref% -od:%gen_dir%\generated_orm        -t:csharp-trm-ids.xml -of:%gen_dir%\generated_orm\auto_t.cs                 
@rem dotnet build -o %TargetDir% %ProjectDir%
@echo CSC: %ERRORLEVEL%
@echo ---------------------------------------------------------------------------
@del /Q %gen_dir%\generated_msg\*.cs
@%TargetDir%codegen -ns:TestORM -wd:%TargetDir% -if:%gen_ref% -od:%gen_dir%\generated_msg\code   -t:csharp-code.xml  
@%TargetDir%codegen -ns:TestORM -wd:%TargetDir% -if:%gen_ref% -od:%gen_dir%\generated_msg\clone  -t:csharp-clone.xml 
@%TargetDir%codegen -ns:TestORM -wd:%TargetDir% -if:%gen_ref% -od:%gen_dir%\generated_msg        -t:csharp-codec.xml   -of:%gen_dir%\generated_msg\codec.cs    
@echo CSC: %ERRORLEVEL%
@echo ---------------------------------------------------------------------------
";
        public static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            try
            {
                if (args.Length == 0)
                {
                    Console.WriteLine(Usage);
                    return;
                }
                else
                {
                    var prop = Properties.ParseArgs(args, ":");
                    if (prop.TryGetValue("-wd", out var work_dir))
                    {
                        var wd = new DirectoryInfo(work_dir);
                        Environment.CurrentDirectory = wd.FullName;
                    }
                    else
                    {
                        var wd = new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                        Environment.CurrentDirectory = wd.FullName;
                    }
                    if (prop.TryGetValue("-od", out var outputDir))
                    {
                        Gen(prop);
                    }
                    else if (prop.TryGetValue("-pn", out var projectName) && prop.TryGetValue("-ns", out var nameSpace))
                    {
                        var files = new FileInfo[0];
                        if (prop.TryGetValue("-dll", out var dll))
                        {
                            files = dll.Split(",").Convert1D((i, n) => new FileInfo(Path.Combine(Environment.CurrentDirectory, n)));
                        }
                        TryGenSimple_MSG(projectName, nameSpace, files);
                        TryGenSimple_ORM(projectName, nameSpace, files);
                    }
                    Environment.ExitCode = 0;
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace();
                Environment.ExitCode = -1;
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------
        public static SerializerGenerator Begin(Properties prop, XmlDocument template = null)
        {
            var input_dir = prop.Get("-id");
            var input_files = prop.Get("-if");
            var output_dir = prop.Get("-od");
            var output_ext = prop.Get("-ext");
            var output_file = prop.Get("-of");
            var template_file = prop.Get("-t");
            var code_namespace = prop.Get("-ns");
            var code_message_id = prop.Get("-mi");
            var filter_file = prop.Get("-ftf");
            var filter_string = prop.Get("-fts");
            var load = prop.Get("-load");
            var inn = prop.Get("-inn");
            var work_dir = prop.Get("-wd");
            var environments = prop.SubProperties("-environments.");
            var inputs = new List<FileInfo>();
            if (input_files != null)
            {
                var fs = input_files.Split(';');
                foreach (var fi in fs)
                {
                    if (!string.IsNullOrEmpty(fi))
                    {
                        if (work_dir != null && !File.Exists(fi))
                        {
                            inputs.Add(new FileInfo(work_dir + Path.DirectorySeparatorChar + fi));
                        }
                        else
                        {
                            inputs.Add(new FileInfo(fi));
                        }
                    }
                }
            }
            if (input_dir != null && new DirectoryInfo(input_dir).Exists)
            {
                inputs.AddRange(CFiles.ListAllFiles(new DirectoryInfo(input_dir), (f) =>
                {
                    var ext = f.Extension.ToLower();
                    return ext.EndsWith(".dll") || ext.EndsWith(".exe");
                }));
            }
            bool use_build_in = inn != null && bool.Parse(inn);
            SerializerGenerator.SetCodeTemplate(typeof(Program).Assembly, use_build_in);
            if (template == null)
            {
                if (template_file != null)
                {
                    template = SerializerGenerator.LoadTemplate(template_file);
                }
                else
                {
                    template = SerializerGenerator.LoadTemplate("csharp-code.xml");
                }
            }
            //var asms = ReflectionUtil.LoadDlls(targetDir, file => file.Extension.ToLower().Equals(".dll"));
            var gen = new SerializerGenerator(template);
            if (code_namespace != null)
            {
                gen.SetCodeNamespace(code_namespace);
            }
            if (environments != null)
            {
                gen.SetEnvironments(environments);
            }
            if (code_message_id != null)
            {
                gen.SetBeginMessageID(Parser.ParseInt(code_message_id));
            }
            if (filter_string != null)
            {
                gen.SetFilter(new StringFilters(filter_string));
            }
            else if (filter_file != null)
            {
                gen.SetFilter(new StringFilters(File.ReadAllText(filter_file)));
            }
            foreach (var file in inputs)
            {
                gen.AddDll(file);
            }
            if (load != null)
            {
                gen.LoadClasses(load);
            }
            gen.SetOutExtension(output_ext);
            gen.SetOutDirectory(output_dir);
            gen.SetOutFile(output_file);
            return gen;
        }
        public static IReadOnlyCollection<string> Gen(Properties prop, XmlDocument template = null)
        {
            var gen = Begin(prop, template);
            gen.Execute();
            return gen.OutputFiles;
        }

        //----------------------------------------------------------------------------------------------------------------------------
        #region Error Code

        public static void GenJsonMessageCode(DirectoryInfo targetDir, FileInfo outputFile, params FileInfo[] inputFiles)
        {
            var ifs = inputFiles.Convert1D((i, file) => file?.FullName);
            ifs = ifs.ArrayExcludeNull();
            Console.WriteLine("---------------------------------------------------------------------------");
            Console.WriteLine("- GEN JSON MSG CODE : " + targetDir.FullName);
            Console.WriteLine("---------------------------------------------------------------------------");
            {
                CFiles.CreateFile(outputFile);
                Gen(Properties.ParseArgs(new string[] {
                    $"-ns:",
                    $"-wd:{targetDir.FullName}",
                    $"-if:{ifs.ArrayToString(";")}",
                    $"-of:{outputFile.FullName}",
                    $"-t:json-response-code.xml",
                }, ":"));
            }
        }
        public static void GenJsonMessageCode(DirectoryInfo targetDir, string outputFile, params string[] inputFilesPath)
        {
            var files = CFiles.ListAllFiles(targetDir, inputFilesPath).ToArray();
            GenJsonMessageCode(targetDir, new FileInfo(outputFile), files);
        }
        public static bool TryGenSimple_MessageCodeJson(DirectoryInfo targetDir, string projectName, params FileInfo[] inputFiles)
        {
            if (Program.TryFindSolutionProjectDir(targetDir, projectName, out var outputDir))
            {
                Program.GenJsonMessageCode(targetDir, new FileInfo($"{outputDir.FullName}/msg_code.json"), inputFiles);
                return true;
            }
            else
            {
                Console.WriteLine($"Can Not Find Project \"{projectName}\"!!!");
            }
            return false;
        }
        public static bool TryGenSimple_MessageCodeJson(DirectoryInfo targetDir, string projectName, params string[] inputFilesPath)
        {
            if (Program.TryFindSolutionProjectDir(targetDir, projectName, out var outputDir))
            {
                Program.GenJsonMessageCode(targetDir, $"{outputDir.FullName}/msg_code.json", inputFilesPath);
                return true;
            }
            else
            {
                Console.WriteLine($"Can Not Find Project \"{projectName}\"!!!");
            }
            return false;
        }
        public static bool TryGenSimple_MessageCodeJson(string projectName, params FileInfo[] inputFiles)
        {
            return TryGenSimple_MessageCodeJson(new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory), projectName, inputFiles);
        }
        public static bool TryGenSimple_MessageCodeJson(string projectName, params string[] inputFilesPath)
        {
            return TryGenSimple_MessageCodeJson(new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory), projectName, inputFilesPath);
        }

        #endregion
        //----------------------------------------------------------------------------------------------------------------------------
        #region Simple Gen
        public static void GenSimpleORM(DirectoryInfo targetDir, DirectoryInfo outputDir, string nameSpace, params FileInfo[] inputFiles)
        {
            var ifs = inputFiles.Convert1D((i, file) => file?.FullName);
            ifs = ifs.ArrayExcludeNull();
            Console.WriteLine("---------------------------------------------------------------------------");
            Console.WriteLine("- GEN ORM : " + targetDir);
            Console.WriteLine("---------------------------------------------------------------------------");
            {
                if (outputDir.Exists) outputDir.Delete(true);
                outputDir.Create();
                Gen(Properties.ParseArgs(new string[] {
                    $"-ns:{nameSpace}",
                    $"-wd:{targetDir.FullName}",
                    $"-if:{ifs.ArrayToString(";")}",
                    $"-od:{outputDir.FullName}",
                    $"-t:csharp-orm.xml",
                }, ":"));
                Gen(Properties.ParseArgs(new string[] {
                    $"-ns:{nameSpace}",
                    $"-wd:{targetDir.FullName}",
                    $"-if:{ifs.ArrayToString(";")}",
                    $"-od:{outputDir.FullName}",
                    $"-t:csharp-orm-ids.xml",
                    $"-of:{outputDir.FullName}{Path.DirectorySeparatorChar}auto.cs",
                }, ":"));
                Gen(Properties.ParseArgs(new string[] {
                    $"-ns:{nameSpace}",
                    $"-wd:{targetDir.FullName}",
                    $"-if:{ifs.ArrayToString(";")}",
                    $"-od:{outputDir.FullName}",
                    $"-t:csharp-trm.xml",
                }, ":"));
                Gen(Properties.ParseArgs(new string[] {
                    $"-ns:{nameSpace}",
                    $"-wd:{targetDir.FullName}",
                    $"-if:{ifs.ArrayToString(";")}",
                    $"-od:{outputDir.FullName}",
                    $"-t:csharp-trm-ids.xml",
                    $"-of:{outputDir.FullName}{Path.DirectorySeparatorChar}auto_t.cs",
                }, ":"));
            }
        }
        public static void GenSimpleMSG(DirectoryInfo targetDir, DirectoryInfo outputDir, string nameSpace, params FileInfo[] inputFiles)
        {
            var ifs = inputFiles.Convert1D((i, file) => file?.FullName);
            ifs = ifs.ArrayExcludeNull();
            Console.WriteLine("---------------------------------------------------------------------------");
            Console.WriteLine("- GEN MSG : " + targetDir.FullName);
            Console.WriteLine("---------------------------------------------------------------------------");
            {
                if (outputDir.Exists) outputDir.Delete(true);
                outputDir.Create();
                var files = new SortedSet<string>();
                {
                    var gen1 = Gen(Properties.ParseArgs(new string[] {
                        $"-ns:{nameSpace}",
                        $"-wd:{targetDir.FullName}",
                        $"-if:{ifs.ArrayToString(";")}",
                        $"-od:{outputDir.FullName}{Path.DirectorySeparatorChar}code",
                        $"-t:csharp-code.xml",
                    }, ":"));
                    files.AddRange(gen1);
                }
                {
                    var gen1 = Gen(Properties.ParseArgs(new string[] {
                        $"-ns:{nameSpace}",
                        $"-wd:{targetDir.FullName}",
                        $"-if:{ifs.ArrayToString(";")}",
                        $"-od:{outputDir.FullName}",
                        $"-t:csharp-code-create.xml",
                        $"-of:{outputDir.FullName}{Path.DirectorySeparatorChar}codec.create.cs",
                    }, ":"));
                    files.AddRange(gen1);
                }
                {
                    var gen2 = Gen(Properties.ParseArgs(new string[] {
                        $"-ns:{nameSpace}",
                        $"-wd:{targetDir.FullName}",
                        $"-if:{ifs.ArrayToString(";")}",
                        $"-od:{outputDir.FullName}",
                        $"-t:csharp-codec.xml",
                        $"-of:{outputDir.FullName}{Path.DirectorySeparatorChar}codec.cs",
                        }, ":"));
                    files.AddRange(gen2);
                }
                {
                    var hashCode = GenCodeHASH(files);
                    Gen(Properties.ParseArgs(new string[] {
                        $"-ns:{nameSpace}",
                        $"-wd:{targetDir.FullName}",
                        $"-if:{ifs.ArrayToString(";")}",
                        $"-od:{outputDir.FullName}",
                        $"-t:csharp-codec-hash.xml",
                        $"-of:{outputDir.FullName}{Path.DirectorySeparatorChar}codec.hash.cs",
                        $"-environments.CODE_HASH:{hashCode}",
                    }, ":"));
                }
            }
        }
        public static void GenSimpleMETA(DirectoryInfo targetDir, DirectoryInfo outputDir, string nameSpace, string temp_xml_name, params FileInfo[] inputFiles)
        {
            var ifs = inputFiles.Convert1D((i, file) => file?.FullName);
            ifs = ifs.ArrayExcludeNull();
            Console.WriteLine("---------------------------------------------------------------------------");
            Console.WriteLine("- GEN META : " + targetDir.FullName);
            Console.WriteLine("---------------------------------------------------------------------------");
            {
                if (outputDir.Exists) outputDir.Delete(true);
                outputDir.Create();
                var gen2 = Gen(Properties.ParseArgs(new string[] {
                    $"-ns:{nameSpace}",
                    $"-wd:{targetDir.FullName}",
                    $"-if:{ifs.ArrayToString(";")}",
                    $"-od:{outputDir.FullName}",
                    $"-t:{temp_xml_name}",
                    $"-of:{outputDir.FullName}{Path.DirectorySeparatorChar}meta.cs",
                }, ":"));
            }
        }
        public static void GenSimpleREF(DirectoryInfo targetDir, DirectoryInfo outputDir, string nameSpace, params FileInfo[] inputFiles)
        {
            var ifs = inputFiles.Convert1D((i, file) => file?.FullName);
            Console.WriteLine("---------------------------------------------------------------------------");
            Console.WriteLine("- GEN REF : " + targetDir.FullName);
            Console.WriteLine("---------------------------------------------------------------------------");
            {
                if (outputDir.Exists) outputDir.Delete(true);
                outputDir.Create();
                var gen2 = Begin(Properties.ParseArgs(new string[] {
                    $"-ns:{nameSpace}",
                    $"-wd:{targetDir.FullName}",
                    $"-if:{ifs.ArrayToString(";")}",
                    $"-od:{outputDir.FullName}",
                    $"-t:csharp-ref.xml",
                    $"-of:{outputDir.FullName}{Path.DirectorySeparatorChar}reflectible.cs",
                }, ":"));
//                 var types = gen2.AssembyTypes(t =>
//                 {
//                     if (t.TryGetAttribute<ReflectibleAttribute>(out var r, true))
//                     {
//                         if (!t.IsAbstract)
//                         {
//                             return true;
//                         }
//                     }
//                     return false;   
//                 });
//                 gen2.AddTypes(types);
                gen2.Execute();
            }
        }
        public static string GenCodeHASH(SortedSet<string> files)
        {
            var sb = new StringBuilder();
            foreach (var file in files)
            {
                sb.AppendLine(CMD5.CalculateMD5(new FileInfo(file)));
            }
            return CMD5.CalculateMD5(sb.ToString());
        }
        #endregion
        //----------------------------------------------------------------------------------------------------------------------------
        #region Codec And ORM

        //----------------------------------------------------------------------------------------------------------------------------
        public static void GenSimple_MSG_ORM(DirectoryInfo targetDir, DirectoryInfo outputDir, string nameSpace, params FileInfo[] files)
        {
            var asms = ReflectionUtil.LoadDlls(targetDir, file => file.Extension.ToLower().Equals(".dll"));
            var ifs = files.Length > 0 ? files : asms.ConvertAll(asm => new FileInfo(asm.Location)).ToArray();
            GenSimpleORM(targetDir, outputDir.CreateSubdirectory("generated_orm"), nameSpace, ifs);
            GenSimpleMSG(targetDir, outputDir.CreateSubdirectory("generated_msg"), nameSpace, ifs);
        }
        public static bool TryGenSimple_MSG_ORM(DirectoryInfo targetDir, string projectName, string nameSpace, params FileInfo[] files)
        {
            if (DeepTools.CodeGen.Program.TryFindSolutionProjectDir(targetDir, projectName, out var outputDir))
            {
                DeepTools.CodeGen.Program.GenSimple_MSG_ORM(targetDir, outputDir, nameSpace, files);
                return true;
            }
            else
            {
                Console.WriteLine($"Can Not Find Project \"{projectName}\"!!!");
            }
            return false;
        }
        public static bool TryGenSimple_MSG_ORM(string projectName, string nameSpace, params FileInfo[] files)
        {
            return TryGenSimple_MSG_ORM(new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory), projectName, nameSpace, files);
        }
        //----------------------------------------------------------------------------------------------------------------------------
        public static void GenSimple_MSG(DirectoryInfo targetDir, DirectoryInfo outputDir, string nameSpace, params FileInfo[] files)
        {
            var asms = ReflectionUtil.LoadDlls(targetDir, file => file.Extension.ToLower().Equals(".dll"));
            var ifs = files.Length > 0 ? files : asms.ConvertAll(asm => File.Exists(asm.Location) ? new FileInfo(asm.Location) : null).ToArray();
            GenSimpleMSG(targetDir, outputDir.CreateSubdirectory("generated_msg"), nameSpace, ifs);
        }
        public static DirectoryInfo TryGenSimple_MSG(DirectoryInfo targetDir, string projectName, string nameSpace, params FileInfo[] files)
        {
            if (DeepTools.CodeGen.Program.TryFindSolutionProjectDir(targetDir, projectName, out var outputDir))
            {
                DeepTools.CodeGen.Program.GenSimple_MSG(targetDir, outputDir, nameSpace, files);
                return outputDir;
            }
            else
            {
                Console.WriteLine($"Can Not Find Project \"{projectName}\"!!!");
            }
            return null;
        }
        public static DirectoryInfo TryGenSimple_MSG(DirectoryInfo targetDir, string projectName, string nameSpace, params string[] fileNames)
        {
            var files = CFiles.ListAllFiles(targetDir, fileNames: fileNames).ToArray();
            if (DeepTools.CodeGen.Program.TryFindSolutionProjectDir(targetDir, projectName, out var outputDir))
            {
                DeepTools.CodeGen.Program.GenSimple_MSG(targetDir, outputDir, nameSpace, files);
                return outputDir;
            }
            else
            {
                Console.WriteLine($"Can Not Find Project \"{projectName}\"!!!");
            }
            return null;
        }
        public static DirectoryInfo TryGenSimple_MSG(string projectName, string nameSpace, params FileInfo[] files)
        {
            return TryGenSimple_MSG(new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory), projectName, nameSpace, files);
        }
        public static DirectoryInfo TryGenSimple_MSG(string projectName, string nameSpace, params string[] fileNames)
        {
            return TryGenSimple_MSG(new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory), projectName, nameSpace, fileNames);
        }
        //----------------------------------------------------------------------------------------------------------------------------
        public static void GenSimple_ORM(DirectoryInfo targetDir, DirectoryInfo outputDir, string nameSpace, params FileInfo[] files)
        {
            var asms = ReflectionUtil.LoadDlls(targetDir, file => file.Extension.ToLower().Equals(".dll"));
            var ifs = files.Length > 0 ? files : asms.ConvertAll(asm => File.Exists(asm.Location) ? new FileInfo(asm.Location) : null).ToArray();
            GenSimpleORM(targetDir, outputDir.CreateSubdirectory("generated_orm"), nameSpace, ifs);
        }
        public static DirectoryInfo TryGenSimple_ORM(DirectoryInfo targetDir, string projectName, string nameSpace, params FileInfo[] files)
        {
            if (DeepTools.CodeGen.Program.TryFindSolutionProjectDir(targetDir, projectName, out var outputDir))
            {
                DeepTools.CodeGen.Program.GenSimple_ORM(targetDir, outputDir, nameSpace, files);
                return outputDir;
            }
            else
            {
                Console.WriteLine($"Can Not Find Project \"{projectName}\"!!!");
            }
            return null;
        }
        public static DirectoryInfo TryGenSimple_ORM(DirectoryInfo targetDir, string projectName, string nameSpace, params string[] fileNames)
        {
            var files = CFiles.ListAllFiles(targetDir, fileNames: fileNames).ToArray();
            if (DeepTools.CodeGen.Program.TryFindSolutionProjectDir(targetDir, projectName, out var outputDir))
            {
                DeepTools.CodeGen.Program.GenSimple_ORM(targetDir, outputDir, nameSpace, files);
                return outputDir;
            }
            else
            {
                Console.WriteLine($"Can Not Find Project \"{projectName}\"!!!");
            }
            return null;
        }
        public static DirectoryInfo TryGenSimple_ORM(string projectName, string nameSpace, params FileInfo[] files)
        {
            return TryGenSimple_ORM(new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory), projectName, nameSpace, files);
        }
        public static DirectoryInfo TryGenSimple_ORM(string projectName, string nameSpace, params string[] fileNames)
        {
            return TryGenSimple_ORM(new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory), projectName, nameSpace, fileNames);
        }
        //----------------------------------------------------------------------------------------------------------------------------
        public static void GenSimple_META(DirectoryInfo targetDir, DirectoryInfo outputDir, string nameSpace, string temp_xml_name, params FileInfo[] files)
        {
            var asms = ReflectionUtil.LoadDlls(targetDir, file => file.Extension.ToLower().Equals(".dll"));
            var ifs = files.Length > 0 ? files : asms.ConvertAll(asm => File.Exists(asm.Location) ? new FileInfo(asm.Location) : null).ToArray();
            GenSimpleMETA(targetDir, outputDir.CreateSubdirectory("generated_meta"), nameSpace, temp_xml_name, ifs);
        }
        public static DirectoryInfo TryGenSimple_META(DirectoryInfo targetDir, string projectName, string nameSpace, string temp_xml_name, params FileInfo[] files)
        {
            if (DeepTools.CodeGen.Program.TryFindSolutionProjectDir(targetDir, projectName, out var outputDir))
            {
                DeepTools.CodeGen.Program.GenSimple_META(targetDir, outputDir, nameSpace, temp_xml_name, files);
                return outputDir;
            }
            else
            {
                Console.WriteLine($"Can Not Find Project \"{projectName}\"!!!");
            }
            return null;
        }
        public static DirectoryInfo TryGenSimple_META(DirectoryInfo targetDir, string projectName, string nameSpace, string temp_xml_name, params string[] fileNames)
        {
            var files = CFiles.ListAllFiles(targetDir, fileNames: fileNames).ToArray();
            if (DeepTools.CodeGen.Program.TryFindSolutionProjectDir(targetDir, projectName, out var outputDir))
            {
                DeepTools.CodeGen.Program.GenSimple_META(targetDir, outputDir, nameSpace, temp_xml_name, files);
                return outputDir;
            }
            else
            {
                Console.WriteLine($"Can Not Find Project \"{projectName}\"!!!");
            }
            return null;
        }
        public static DirectoryInfo TryGenSimple_META(string projectName, string nameSpace, string temp_xml_name, params FileInfo[] files)
        {
            return TryGenSimple_META(new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory), projectName, nameSpace, temp_xml_name, files);
        }
        public static DirectoryInfo TryGenSimple_META(string projectName, string nameSpace, string temp_xml_name, params string[] fileNames)
        {
            return TryGenSimple_META(new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory), projectName, nameSpace, temp_xml_name, fileNames);
        }
        //----------------------------------------------------------------------------------------------------------------------------
        public static void GenSimple_REF(DirectoryInfo targetDir, DirectoryInfo outputDir, string nameSpace, params FileInfo[] files)
        {
            GenSimpleREF(targetDir, outputDir.CreateSubdirectory("generated_ref"), nameSpace, files);
        }
        public static DirectoryInfo TryGenSimple_REFD(DirectoryInfo targetDir, string projectName, string nameSpace, params FileInfo[] files)
        {
            if (DeepTools.CodeGen.Program.TryFindSolutionProjectDir(targetDir, projectName, out var outputDir))
            {
                DeepTools.CodeGen.Program.GenSimple_REF(targetDir, outputDir, nameSpace, files);
                return outputDir;
            }
            else
            {
                Console.WriteLine($"Can Not Find Project \"{projectName}\"!!!");
            }
            return null;
        }
        public static DirectoryInfo TryGenSimple_REF(string projectName, string nameSpace, params FileInfo[] files)
        {
            return TryGenSimple_REFD(new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory), projectName, nameSpace, files);
        }
        public static DirectoryInfo TryGenSimple_REFD(DirectoryInfo targetDir, string projectName, string nameSpace, params string[] fileNames)
        {
            var files = CFiles.ListAllFiles(targetDir, fileNames: fileNames).ToArray();
            return TryGenSimple_REFD(targetDir, projectName, nameSpace, files);
        }
        public static DirectoryInfo TryGenSimple_REF(string projectName, string nameSpace, params string[] fileNames)
        {
            return TryGenSimple_REFD(new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory), projectName, nameSpace, fileNames);
        }
        #endregion
        //----------------------------------------------------------------------------------------------------------------------------
        public static bool TryFindSolutionProjectDir(string projectName, out DirectoryInfo projectDir)
        {
            return TryFindSolutionProjectDir(new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory), projectName, out projectDir);
        }
        public static bool TryFindSolutionProjectDir(DirectoryInfo workDir, string projectName, out DirectoryInfo projectDir)
        {
            while (workDir.Root.FullName != workDir.FullName)
            {
                var sub = workDir.GetDirectories(projectName);
                if (sub.Length == 0)
                {
                    workDir = workDir.Parent;
                }
                else if (File.Exists(sub[0].FullName + Path.DirectorySeparatorChar + projectName + ".csproj"))
                {
                    projectDir = sub[0];
                    return true;
                }
                else
                {
                    workDir = workDir.Parent;
                }
            }
            projectDir = null;
            return false;
        }

        //----------------------------------------------------------------------------------------------------------------------------
    }
}
