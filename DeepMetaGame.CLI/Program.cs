
using DeepCore;
using DeepCore.IO;
using DeepEditorConsole;

class Program
{
    static string USAGE = @"
Usage: DeepMetaGame.CLI <command> [options]
Commands:
  init                 Initialize the project
  rename               Rename the project
Options:
  -git-url=<url>       Specify the Git URL for the submodule
  -root=<path>         Specify the root directory for the project
";

    static string TEMPLATE_GIT_URL = "git@github.com:DeepMetaverseEngine/DeepTemplate.git";//"git@git.code.tencent.com:DeepMeta/DeepTemplate.git";
    static string DEEPMETA_GIT_URL = "git@github.com:DeepMetaverseEngine/DeepMeta.git";//"git@git.code.tencent.com:DeepMeta/DeepMeta.git";


    [STAThread]
    static int Main(string[] args)
    {
        // 获得当前登录的Windows用户标示
        try
        {
            var pargs = Properties.ParseArgs(args);
            var root = new DirectoryInfo(".");
            if (pargs.TryGetValue("-root", out var _root))
            {
                root = new DirectoryInfo(_root);
            }
            if (args.Length > 1)
            {
                Environment.CurrentDirectory = root.FullName;
                Console.WriteLine($"Current Path: {root.FullName}");
                var cmd = args[0];
                var prj = args[1];
                switch (cmd)
                {
                    case "init":
                        return init(pargs, root);
                    case "rename":
                        Console.Write("Input Your Project Name :");
                        var projName = Console.ReadLine().Trim();
                        if (string.IsNullOrWhiteSpace(projName))
                        {
                            Console.WriteLine($"Project Name Must Be Nonblank Text !!");
                            Console.WriteLine(USAGE);
                            return -1;
                        }
                        Console.WriteLine($"Current Project Name: {projName}");
                        return rename_proj(pargs, root, projName);
                    default:
                        Console.WriteLine($"Unknown command: {cmd}");
                        Console.WriteLine(USAGE);
                        return 0;
                }
            }
            Console.WriteLine(USAGE);
            return init(pargs, root);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception: " + ex.Message);
            Console.WriteLine(USAGE);
            return -1;
        }
        finally
        {
            Console.WriteLine($"Press Any Key To Exit.");
            Console.ReadKey();
        }
    }
    const string TEMP_NAME = "_Temp_";
    static int init(Properties pargs, DirectoryInfo root)
    {
        var projName = root.Name;
        var GitPath = Path.Combine(root.FullName, ".git");
        if (!Directory.Exists(GitPath))
        {
            Console.WriteLine($"Current Project Name: {projName}");
            Console.WriteLine("### Clone DeepMeta Templates ###");
            //git archive --remote=git@github.com:user/repo.git HEAD --format=zip --output=remote_project.zip
            var code = Exec.Run("git", $"clone {TEMPLATE_GIT_URL} \"{root.FullName}\"", root.FullName);
            //var code = Exec.Run("git", $"archive --remote={TEMPLATE_GIT_URL} HEAD --format=zip --output=_temp_.zip", root.FullName);
            if (code == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("### Clone DeepMeta Templates Complete ! ###");
                Console.ResetColor();
                var submodules = Properties.ParseLines(File.ReadAllLines(Path.Combine(root.FullName, ".gitmodules")));
                foreach (var e in submodules)
                {
                    if (e.Value.EndsWith("DeepMeta.git"))
                    {
                        DEEPMETA_GIT_URL = e.Value;
                        Console.WriteLine("Redirect DeepMeta Git Url : " + DEEPMETA_GIT_URL);
                        break;
                    }
                }
            }
            Thread.Sleep(1000);
            Exec.Cmd("rd", $" /s /q \"{GitPath}\"");
            Exec.Cmd("del", $" /s /q \"{Path.Combine(root.FullName, ".gitmodules")}\"");
            Exec.Run("git", "init");
            return rename_proj(pargs, root, projName);
        }
        else
        {
            return rename_proj(pargs, root, projName);
        }
    }
    static int rename_proj(Properties pargs, DirectoryInfo root, string projName)
    {
        var temp_sln_dir = new DirectoryInfo(Path.Combine(root.FullName, $"{TEMP_NAME}SLN"));
        var proj_sln_dir = new DirectoryInfo(Path.Combine(root.FullName, $"{projName}SLN"));
        if (temp_sln_dir.Exists && !proj_sln_dir.Exists)
        {
            Console.WriteLine("### Rename SLN Directory ###");
            // Rename the directory using the 'ren' command
            {
                var code = Exec.Cmd("ren", $"\"{temp_sln_dir.FullName}\" \"{proj_sln_dir.Name}\"", root.FullName);
                if (code != 0)
                {
                    return code;
                }
            }
            proj_sln_dir.Refresh();
            // Rename all subdirectories and files that start with TEMP_NAME
            while (proj_sln_dir.FindDirectory(d => d.Name.StartsWith(TEMP_NAME)) is DirectoryInfo tempDir)
            {
                var projDir = new DirectoryInfo(Path.Combine(tempDir.Parent.FullName, tempDir.Name.Replace(TEMP_NAME, projName)));
                var code = Exec.Cmd("ren", $"\"{tempDir.FullName}\" \"{projDir.Name}\"");
                if (code != 0)
                {
                    return code;
                }
            }
            proj_sln_dir.Refresh();
            // Rename all files that start with TEMP_NAME
            while (proj_sln_dir.FindFile(d => d.Name.StartsWith(TEMP_NAME)) is FileInfo tempFile)
            {
                var projDir = new FileInfo(Path.Combine(tempFile.Directory.FullName, tempFile.Name.Replace(TEMP_NAME, projName)));
                var code = Exec.Cmd("ren", $"\"{tempFile.FullName}\" \"{projDir.Name}\"");
                if (code != 0)
                {
                    return code;
                }
            }
            proj_sln_dir.Refresh();
            // Replace content in all relevant files
            {
                var subfiles = proj_sln_dir.GetFiles("*", SearchOption.AllDirectories);
                foreach (var sub in subfiles)
                {
                    if (sub.Name.EndsWith(".cs")
                        || sub.Name.EndsWith(".txt")
                        || sub.Name.EndsWith(".slnx")
                        || sub.Name.EndsWith(".csproj")
                        || sub.Name.EndsWith(".bat")
                        || sub.Name.EndsWith(".json")
                        || sub.Name.EndsWith(".config"))
                    {
                        replace_all(sub, TEMP_NAME, projName);
                    }
                }
            }
            proj_sln_dir.Refresh();
            // Clone DeepMeta submodule
            {
                var DeepMetaPath = Path.Combine(proj_sln_dir.FullName, "DeepMeta");
                Exec.Cmd("rd", $" /s /q \"{DeepMetaPath}\"");
                Exec.Cmd("del", $" /s /q \"{Path.Combine(root.FullName, ".gitmodules")}\"");
                Console.WriteLine("### Clone DeepMeta submodule ###");
                Exec.Run("git", $"submodule add --progress {DEEPMETA_GIT_URL} DeepMeta", proj_sln_dir.FullName);
                Exec.Run("git", $"pull \"origin\"  master:master", DeepMetaPath);
                Exec.Run("git", $"lfs pull", DeepMetaPath);
            }
            proj_sln_dir.Refresh();
        }
        return rename_unity(pargs, root, projName);
    }
    static int rename_unity(Properties pargs, DirectoryInfo root, string projName)
    {
        var temp_unity_dir = new DirectoryInfo(Path.Combine(root.FullName, $"{TEMP_NAME}Unity"));
        var proj_unity_dir = new DirectoryInfo(Path.Combine(root.FullName, $"{projName}Unity"));
        if (temp_unity_dir.Exists && !proj_unity_dir.Exists)
        {
            Console.WriteLine("### Rename Unity Directory ###");
            // Rename the directory using the 'ren' command
            {
                var code = Exec.Cmd("ren", $"\"{temp_unity_dir.FullName}\" \"{proj_unity_dir.Name}\"", root.FullName);
                if (code != 0)
                {
                    return code;
                }
            }
            proj_unity_dir.Refresh();
            // Rename all subdirectories and files that start with TEMP_NAME
            var scripts_dir = new DirectoryInfo(Path.Combine(proj_unity_dir.FullName, "Assets", "Scripts"));
            while (scripts_dir.FindFile(d => d.Name.StartsWith(TEMP_NAME)) is FileInfo tempFile)
            {
                var projDir = new FileInfo(Path.Combine(tempFile.Directory.FullName, tempFile.Name.Replace(TEMP_NAME, projName)));
                var code = Exec.Cmd("ren", $"\"{tempFile.FullName}\" \"{projDir.Name}\"");
                if (code != 0)
                {
                    return code;
                }
            }
            proj_unity_dir.Refresh();
            scripts_dir.Refresh();
            // Replace content in all relevant files
            {
                var subfiles = scripts_dir.GetFiles("*", SearchOption.AllDirectories);
                foreach (var sub in subfiles)
                {
                    if (sub.Name.EndsWith(".cs"))
                    {
                        replace_all(sub, TEMP_NAME, projName);
                    }
                }
            }
        }
        return rename_editor(pargs, root, projName);
    }
    static int rename_editor(Properties pargs, DirectoryInfo root, string projName)
    {
        var editor_dir = new DirectoryInfo(Path.Combine(root.FullName, $"GameEditor"));
        //if (replace_all(new FileInfo(Path.Combine(root.FullName, $"GameEditor.bat")), TEMP_NAME, projName))
        {
            var list = CFiles.ListAllFiles(editor_dir, d => d.Name.StartsWith(TEMP_NAME));
            //while (editor_dir.FindFile(d => d.Name.StartsWith(TEMP_NAME)) is FileInfo tempFile)
            foreach (var tempFile in list)
            {
                var dstFile = new FileInfo(Path.Combine(tempFile.Directory.FullName, tempFile.Name.Replace(TEMP_NAME, projName)));
                if (tempFile.Name.EndsWith(".dll") ||
                    tempFile.Name.EndsWith(".exe") ||
                    tempFile.Name.EndsWith(".pdb") ||
                    tempFile.Name.EndsWith(".config") ||
                    tempFile.Name.EndsWith(".json"))
                {
                    var code = Exec.Cmd("copy", $" /y \"{tempFile.FullName}\" \"{dstFile.FullName}\"");
                    if (code != 0)
                    {
                        return code;
                    }
                    replace_all(dstFile, TEMP_NAME, projName);
                }
                //                 else
                //                 {
                //                     var code = Exec.Cmd("ren", $"\"{tempFile.FullName}\" \"{projDir.Name}\"");
                //                     if (code != 0)
                //                     {
                //                         return code;
                //                     }
                //                 }
            }
            // Replace content in all relevant files
            {
                var subfiles = editor_dir.GetFiles("*", SearchOption.AllDirectories);
                foreach (var sub in subfiles)
                {
                    if (sub.Name.EndsWith(".cs") || 
                        sub.Name.EndsWith(".xml") || 
                        sub.Name.EndsWith(".csproj") ||
                        sub.Name.EndsWith(".bat"))
                    {
                        replace_all(sub, TEMP_NAME, projName);
                    }
                }
            }
        }
        return 0;
    }

    private static bool replace_all(FileInfo sub, string src, string dst)
    {
        var content = Resource.LoadData(sub.FullName);
        var text = CUtils.DecodeUTF8(content, out var encoding);
        if (CUtils.TryReplaceAll(ref text, TEMP_NAME, dst) > 0)
        {
            CFiles.WriteAllText(sub, text, encoding);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Rename File Content : {sub.FullName}");
            Console.ResetColor();
            return true;
        }
        return false;
    }
    /*
    static int init(Properties pargs, DirectoryInfo root, string gitURL)
    {
        var projName = root.Name;
        Environment.CurrentDirectory = root.FullName;
        Console.WriteLine($"Current Path: {root.FullName}");
        Console.WriteLine($"Current Project Name: {projName}");

        var GitPath = Path.Combine(root.FullName, ".git");
        var SlnPath = Path.Combine(root.FullName, $"{projName}SLN");
        var UnityPath = Path.Combine(root.FullName, $"{projName}Unity");
        var GameEditorPath = Path.Combine(root.FullName, "GameEditor");
        if (!Directory.Exists(GitPath))
        {
            Exec.Run("git", "init");
        }
        if (!Directory.Exists(SlnPath))
        {
            Console.WriteLine("### Make Solution folder ###");
            Console.WriteLine(SlnPath);
            Directory.CreateDirectory(SlnPath);
        }
        else
        {
            Console.WriteLine($"Solution Folder : {SlnPath}");
        }
        var DeepMetaPath = Path.Combine(SlnPath, "DeepMeta");
        if (!Directory.Exists(DeepMetaPath))
        {
            Console.WriteLine("### Clone DeepMeta ###");
            Exec.Run("git", $"submodule add {gitURL} DeepMeta", SlnPath);
            Exec.Run("git", $"git pull \"origin\"  master:master", DeepMetaPath);
            Exec.Run("git", $"git lfs pull", DeepMetaPath);
        }
        if (!File.Exists(Path.Combine(root.FullName, ".gitattributes")))
        {
            var gitattributes = Resource.LoadFromAssembly(typeof(Program), "_gitattributes");
            CFiles.WriteAllBytes(Path.Combine(root.FullName, ".gitattributes"), gitattributes);
            var git_ignore = Resource.LoadFromAssembly(typeof(Program), "_gitignore");
            CFiles.WriteAllBytes(Path.Combine(root.FullName, ".gitignore"), git_ignore);
        }
        var SrcPath = Path.Combine(SlnPath, $"{projName}Src");
        if (!Directory.Exists(SrcPath))
        {
            try
            {
                Console.WriteLine("### Copy Source Files ###");
                var temp_dirs = new DirectoryInfo(DeepMetaPath).GetDirectories();
                foreach (var dir in temp_dirs)
                {
                    if (dir.Name.StartsWith("_Temp_"))
                    {
                        var target_proj = $"{projName}SLN\\{projName}Src\\{dir.Name.Replace("_Temp_", projName)}";
                        CFiles.ShellXCopy(root, $"{projName}SLN\\DeepMeta\\{dir.Name}", target_proj);
                        if (projName!= "_Temp_")
                        {
                            var subfiles = new DirectoryInfo(target_proj).GetFiles("*", SearchOption.AllDirectories);
                            foreach (var sub in subfiles)
                            {
                                if (sub.Name.StartsWith("_Temp_"))
                                {
                                    var dstname = sub.Name.Replace("_Temp_", projName);
                                    Console.WriteLine($"    {sub.FullName} -> {dstname}");
                                    CFiles.ShellRename(sub.Directory, sub.Name, dstname);
                                    var dst = Path.Combine(sub.Directory.FullName, sub.Name.Replace("_Temp_", projName));
                                    var content = Resource.LoadData(dst);
                                    var text = CUtils.DecodeUTF8(content, out var encoding);
                                    text = text.ReplaceAll("_Temp_", projName);
                                    CFiles.WriteAllText(dst, text, encoding);
                                }
                                else if (sub.Name.EndsWith(".cs")
                                    || sub.Name.EndsWith(".txt")
                                    || sub.Name.EndsWith(".bat")
                                    || sub.Name.EndsWith(".json")
                                    || sub.Name.EndsWith(".config"))
                                {
                                    var content = Resource.LoadData(sub.FullName);
                                    var text = CUtils.DecodeUTF8(content, out var encoding);
                                    text = text.ReplaceAll("_Temp_", projName);
                                    CFiles.WriteAllText(sub, text, encoding);
                                }
                            }
                        }
                    }
                }
                {
                    var git_ignore = Resource.LoadFromAssembly(typeof(Program), "_gitignore");
                    CFiles.WriteAllBytes(Path.Combine(SrcPath, ".gitignore"), git_ignore);
                }
            }
            catch (Exception err)
            {
                CFiles.Delete(SrcPath);
                Console.WriteLine($"Error: {err}");
                return -1;
            }
            //CFiles.ShellXCopy(root, $"{projName}SLN\\DeepMeta\\_Temp_*", $"{projName}SLN\\{projName}Src");
        }
        var SlnFilePath = Path.Combine(SlnPath, $"{projName}.slnx");
        if (!Directory.Exists(SlnFilePath))
        {
            {
                var srcSLNX = Resource.LoadFromAssembly(typeof(Program), "_Temp_.slnx");
                var text = CUtils.DecodeUTF8(srcSLNX, out var encoding);
                text = text.ReplaceAll("_Temp_", projName);
                CFiles.WriteAllText(SlnFilePath, text, encoding);
            }
        }
        if (!Directory.Exists(UnityPath))
        {
            Console.WriteLine("### Make Unity Project folder ###");
            Console.WriteLine(UnityPath);
            Directory.CreateDirectory(UnityPath);
        }
        else
        {
            Console.WriteLine($"Unity Project Folder : {UnityPath}");
        }
        if (!Directory.Exists(GameEditorPath))
        {
            Console.WriteLine("### Make Game Editor folder ###");
            Console.WriteLine(GameEditorPath);
            Directory.CreateDirectory(GameEditorPath);
        }
        else
        {
            Console.WriteLine($"Game Editor Folder : {GameEditorPath}");
        }
        return 0;
    }
     */


}

/*
@echo off

cd %~dp0

echo Current Path: %~dp0
for %%I in (.) do set "DIR_NAME=%%~nxI"
echo Current Project Name: %DIR_NAME%


SET PROJECT_NAME=%DIR_NAME%

if not exist .git (

echo ----------------------------------------------------------------------
echo ### Make Solution folder ### 
git init
if not exist %PROJECT_NAME%SLN (
    md %PROJECT_NAME%SLN
)
cd %PROJECT_NAME%SLN

git submodule add git@github.com:DeepMetaverseEngine/DeepMeta.git DeepMeta

cd ..
)
echo ----------------------------------------------------------------------
echo ### Make Unity Project folder ### 
if not exist %PROJECT_NAME%Unity (
    md %PROJECT_NAME%Unity
)

echo ----------------------------------------------------------------------
echo ### Make Game Editor folder ### 
if not exist GameEditor (
    md GameEditor
)

echo ----------------------------------------------------------------------
pause
 */