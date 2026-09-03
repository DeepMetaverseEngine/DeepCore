using DeepCore;
using DeepCore.IO;
using DeepCore.Log;
using IniParser;
using System.Diagnostics;
using System.Text;

namespace DeepCrystal
{
    public abstract class ExeLauncher : Disposable
    {
        public bool UseShellExecute = false;
        public ExeLauncher() { AsSynchronizedDisposing(); }
        protected ProcessStartInfo CreateStartInfo()
        {
            var redis_start = new ProcessStartInfo();
            //redis_start.LoadUserProfile = true;
            return redis_start;
        }
    }
    public class RedisLauncher : ExeLauncher
    {
        private static LazyLogger log = new LazyLogger(nameof(RedisLauncher));
        public string RedisServer_Path_Name = "redis\\redis-server.exe";
        public string RedisCLI_Path_Name = "redis\\redis-cli.exe";
        public int Port = 6379;
        public Process Redis { get; private set; }
        public FileInfo RedisServerEXE { get; private set; }
        public FileInfo RedisClientEXE { get; private set; }
        public bool Find_Redis_EXE(string workDir, out FileInfo redis_server_exe, out FileInfo redis_cli_exe)
        {
            var current_dir = new DirectoryInfo(workDir);
            while (current_dir != null && current_dir.Exists && current_dir != current_dir.Root)
            {
                var redis_server = Path.Combine(current_dir.FullName, RedisServer_Path_Name);
                var redis_cli = Path.Combine(current_dir.FullName, RedisCLI_Path_Name);
                if (File.Exists(redis_server) && File.Exists(redis_cli))
                {
                    redis_server_exe = RedisServerEXE = new FileInfo(redis_server);
                    redis_cli_exe = RedisClientEXE = new FileInfo(redis_cli);
                    return true;
                }
                current_dir = current_dir.Parent;
            }
            redis_server_exe = null;
            redis_cli_exe = null;
            return false;
        }
        public RedisLauncher Start_Redis_EXE(string workDir)
        {
            try
            {
                if (Find_Redis_EXE(workDir, out var redis_server_exe, out var redis_cli_exe))
                {
                    log.Info($"Find redis Exe : {redis_server_exe.FullName}");
                    var redis_start = CreateStartInfo();
                    redis_start.WorkingDirectory = Path.GetDirectoryName(redis_server_exe.FullName);
                    redis_start.FileName = redis_server_exe.FullName;
                    redis_start.Arguments = $"redis.windows.conf  --port {Port}";
                    redis_start.UseShellExecute = UseShellExecute;
                    log.Info($"Starting... : {redis_start.FileName}");
                    Redis = Process.Start(redis_start);
                }
            }
            catch { throw; }
            return this;
        }
        protected override void Disposing()
        {
            if (Redis != null)
            {
                try
                {
                    var script = new StringBuilder();
                    script.AppendLine($"shutdown SAVE");
                    script.AppendLine($"quit");
                    var redis = CreateStartInfo();
                    redis.WorkingDirectory = Path.GetDirectoryName(RedisClientEXE.FullName);
                    redis.FileName = RedisClientEXE.FullName;
                    redis.Arguments = $"-p {Port}";
                    redis.UseShellExecute = false;
                    redis.RedirectStandardInput = true;
                    log.Info($"Shutdown... : {Redis.StartInfo.FileName}");
                    var my = Process.Start(redis);
                    my.StandardInput.WriteLine(script.ToString());
                    my.StandardInput.Flush();
                    my.WaitForExit();
                }
                catch (Exception err) { log.Error(err); }
                try
                {
                    Redis?.WaitForExit();
                }
                catch (Exception err) { log.Error(err); }
            }
        }
    }

    public class MySQLLauncher : ExeLauncher
    {
        private static LazyLogger log = new LazyLogger(nameof(MySQLLauncher));
        public string MySQLD_Path_Name = $"mysql\\bin\\mysqld.exe";
        public string MySQLC_Path_Name = $"mysql\\bin\\mysql.exe";
        public string MyINI_Path_Name = $"mysql\\my.ini";
        public string Password = "123456";
        public string User = "root";
        public int Port = 3306;

        public Process MySQL { get; private set; }
        public FileInfo MySQLServerEXE { get; private set; }
        public FileInfo MySQLClientEXE { get; private set; }
        public bool Find_MySQL_EXE(string workDir, out FileInfo mysqld_exe, out FileInfo mysql_exe, out FileInfo my_ini)
        {
            var current_dir = new DirectoryInfo(workDir);
            while (current_dir != null && current_dir.Exists && current_dir != current_dir.Root)
            {
                var _mysqld_exe = Path.Combine(current_dir.FullName, MySQLD_Path_Name);
                var _mysqlc_exe = Path.Combine(current_dir.FullName, MySQLC_Path_Name);
                var _my_ini = Path.Combine(current_dir.FullName, MyINI_Path_Name);
                if (File.Exists(_mysqld_exe) && File.Exists(_mysqlc_exe) && File.Exists(_my_ini))
                {
                    mysqld_exe = MySQLServerEXE = new FileInfo(_mysqld_exe);
                    mysql_exe = MySQLClientEXE = new FileInfo(_mysqlc_exe);
                    my_ini = new FileInfo(_my_ini);
                    return true;
                }
                current_dir = current_dir.Parent;
            }
            mysqld_exe = null;
            mysql_exe = null;
            my_ini = null;
            return false;
        }
        public MySQLLauncher Start_MySQL_EXE(string workDir)
        {
            try
            {
                if (Find_MySQL_EXE(workDir, out var mysqld_exe, out var mysqlc_exe, out var my_ini))
                {
                    log.Info($"Find mysqld Exe : {mysqld_exe.FullName}");
                    {
                        var mysqld = CreateStartInfo();
                        mysqld.WorkingDirectory = Path.GetDirectoryName(mysqld_exe.FullName);
                        mysqld.FileName = mysqld_exe.FullName;
                        mysqld.Arguments = $"--initialize-insecure --user={User} --console";
                        mysqld.UseShellExecute = true;
                        var redis = Process.Start(mysqld);
                        redis.WaitForExit(30000);
                    }
                    {
                        var parser = new FileIniDataParser();
                        parser.Parser.Configuration.CommentString = "#";
                        parser.Parser.Configuration.AssigmentSpacer = "";
                        var ini_prop = parser.ReadFile(my_ini.FullName, CUtils.UTF8_BOM);
                        if (ini_prop["mysqld"]["port"] != Port.ToString())
                        {
                            log.Info($"Redirect Port : {Port}");
                            ini_prop["mysqld"]["port"] = Port.ToString();
                            my_ini = new FileInfo(Path.Combine(my_ini.Directory.FullName,
                                Path.GetFileNameWithoutExtension(my_ini.Name) + $"_{Port}{my_ini.Extension}"));
                            log.Info($"Generate : {my_ini.FullName}");
                            parser.WriteFile(my_ini.FullName, ini_prop, CUtils.UTF8);
                        }
                    }
                    {
                        var mysqld = CreateStartInfo();
                        mysqld.WorkingDirectory = Path.GetDirectoryName(mysqld_exe.FullName);
                        mysqld.FileName = mysqld_exe.FullName;
                        mysqld.Arguments = $"--defaults-file=\"{my_ini.FullName}\"  --console";
                        mysqld.UseShellExecute = UseShellExecute;
                        log.Info($"Starting... : {mysqld.FileName}");
                        MySQL = Process.Start(mysqld);
                        MySQL.WaitForExit(1000);
                    }
                    //Thread.Sleep(1000);
                    {
                        // init user password
                        var script = new StringBuilder();
                        script.AppendLine($"use mysql;");
                        script.AppendLine($"ALTER USER '{User}'@'localhost' IDENTIFIED BY '{Password}';");
                        script.AppendLine($"alter user '{User}'@'localhost' identified with mysql_native_password by '{Password}';");
                        script.AppendLine($"exit;");

                        var mysql = CreateStartInfo();
                        mysql.WorkingDirectory = Path.GetDirectoryName(mysqlc_exe.FullName);
                        mysql.FileName = mysqlc_exe.FullName;
                        mysql.Arguments = $"--port={Port} -u{User}";
                        mysql.UseShellExecute = false;
                        mysql.RedirectStandardInput = true;
                        log.Info($"ALTER USER : {User}");
                        var my = Process.Start(mysql);
                        try
                        {
                            my.StandardInput.WriteLine(script.ToString());
                            my.StandardInput.Flush();
                        }
                        catch (Exception err)
                        {
                            log.Error(err.Message);
                        }
                        my.WaitForExit(30000);
                    }
                    Thread.Sleep(1000);
                }
            }
            catch { throw; }
            return this;
        }
        protected override void Disposing()
        {
            if (MySQL != null)
            {
                try
                {
                    var script = new StringBuilder();
                    script.AppendLine($"shutdown;");
                    script.AppendLine($"quit;");
                    var mysql = CreateStartInfo();
                    mysql.WorkingDirectory = Path.GetDirectoryName(MySQLClientEXE.FullName);
                    mysql.FileName = MySQLClientEXE.FullName;
                    mysql.Arguments = $"--port={Port} -u{User} -p{Password}";
                    mysql.UseShellExecute = false;
                    mysql.RedirectStandardInput = true;
                    log.Info($"Shutdown... : {MySQL.StartInfo.FileName}");
                    var my = Process.Start(mysql);
                    my.StandardInput.WriteLine(script.ToString());
                    my.StandardInput.Flush();
                    my.WaitForExit();
                }
                catch (Exception err) { log.Error(err); }
                try
                {
                    MySQL?.WaitForExit();
                }
                catch (Exception err) { log.Error(err); }
            }
        }


    }
}
