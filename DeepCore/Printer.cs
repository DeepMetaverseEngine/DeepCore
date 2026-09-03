using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DeepCore
{
    public static class Printer
    {
        public static string ToVisibleName(this Type type)
        {
            if (type.DeclaringType != null)
            {
                return type.DeclaringType.ToTypeDefineName() + "." + type.ToTypeDefineName();
            }
            return type.ToTypeDefineName();
        }

        public static void PrintTitle(this TextWriter output, string name, object value, string prefix = "    ", int namePlaceHolder = 24)
        {
            output.WriteLine(string.Format("{0}{1} : {2}]",
                prefix,
                CUtils.FillPlaceHolder("[" + name, namePlaceHolder, ' ', 1),
                value));
        }
        public static void PrintLine(this TextWriter output, string name, object value, string prefix = "    ", int namePlaceHolder = 24, string suffix = "")
        {
            output.WriteLine(string.Format("{0}{1} = {2}{3}",
                   prefix,
                   CUtils.FillPlaceHolder(name, namePlaceHolder, ' ', 1),
                   value,
                   suffix));
        }
        public static void PrintLineSeparator(this TextWriter output, int totalPlaceHolder = 64)
        {
            output.WriteLine(CUtils.SequenceChar('-', totalPlaceHolder));
        }


        public static void FullStackTrace(this Exception err, TextWriter output, Action<Exception, TextWriter> format)
        {
            if (format != null)
            {
                format(err, output);
            }
            else
            {
                output.Write("Exception : ");
                output.Write(err.GetType().FullName);
                output.Write(" : ");
                output.Write(err.Message);
                output.WriteLine(err.StackTrim());
            }
            err = err.InnerException;
            while (err != null)
            {
                if (format != null)
                {
                    format(err, output);
                }
                else
                {
                    output.Write("InnerException : ");
                    output.Write(err.GetType().FullName);
                    output.Write(" : ");
                    output.Write(err.Message);
                    output.WriteLine(err.StackTrim());
                }
                err = err.InnerException;
            }
        }
        public static void FullStackTrace(this Exception err, TextWriter output)
        {
            FullStackTrace(err, output, null);
        }
        public static string FullStackTrace(this Exception err)
        {
            var output = new StringWriter();
            {
                FullStackTrace(err, output);
                return output.ToString();
            }
        }
        public static string FullStackTrace(this Exception err, Action<Exception, TextWriter> format)
        {
            var output = new StringWriter();
            {
                FullStackTrace(err, output, format);
                return output.ToString();
            }
        }
    }

}
