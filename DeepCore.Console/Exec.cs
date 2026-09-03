using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepEditorConsole
{
    public static class Exec
    {
        public static ConsoleColor ForegroundColor = ConsoleColor.Green;
        public static int Run(string cmd, string args)
        {
            return Run(cmd, args, Environment.CurrentDirectory);
        }
        public static int Run(string cmd, string args, string workingDirectory)
        {
            try
            {
                Console.ForegroundColor = ForegroundColor;
                Console.WriteLine($"{cmd} {args}");
                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = cmd;
                process.StartInfo.Arguments = args;
                process.StartInfo.WorkingDirectory = workingDirectory;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.Verb = "runas";
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine(e.Data);
                    }
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine(e.Data);
                    }
                };
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                return process.ExitCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                return -1;
            }
            finally
            {
                Console.ResetColor();
            }
        }
        public static int Cmd(string cmd, string args)
        {
            return Cmd(cmd, args, Environment.CurrentDirectory);
        }
        public static int Cmd(string cmd, string args, string workingDirectory)
        {
            return Run("cmd", $"/C {cmd} {args}", workingDirectory);
        }
    }
}
