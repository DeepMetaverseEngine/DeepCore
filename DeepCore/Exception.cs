using DeepCore.Log;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace System
{
    public static class ExceptionExt
    {
        private static Logger log = new LazyLogger("Exception");
        public static string ToFullMessage(this Exception err)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{err.Message}{err.StackTrim()}");
            err = err.InnerException;
            while (err != null)
            {
                sb.AppendLine("InnerException : " + $"{err.Message}{err.StackTrim()}");
                err = err.InnerException;
            }
            return sb.ToString();
        }
        public static void PrintStackTrace(this Exception err)
        {
            log.Error(err.Message, err);
        }
        public static void PrintStackTrace(this Exception err, string prefix)
        {
            log.Error($"{prefix }\n\t{err.Message}", err);
        }
        public static void PrintStackTrace(this Exception err, IO.TextWriter output)
        {
            output.WriteLine(err.Message + err.StackTrim());
        }
        public static void PrintStackTrace(this Exception err, string prefix, IO.TextWriter output)
        {
            output.WriteLine(prefix + err.Message + err.StackTrim());
        }
        public static string StackTrim(this Exception err)
        {
            if (err.StackTrace != null)
            {
                return Environment.NewLine + err.StackTrace.TrimEnd();
            }
            return string.Empty;
        }
    }
}
