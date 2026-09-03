using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DeepCore.Log
{
    public class FileStreamLogFactory : LoggerFactory
    {
        private TextWriter writer;
        private FileStream output;
        public FileStreamLogFactory(FileStream output)
        {
            this.output = output;
            this.writer = new StreamWriter(output);
        }

        protected override Logger CreateLogger(object owner)
        {
            var log = new FileStreamLogger(writer, owner.ToString());
            return log;
        }
    }
    public class FileStreamLogger : Logger
    {
        private TextWriter writer;
        public FileStreamLogger(TextWriter writer, string name) : base(LoggerFactory.CurrentFactory, name)
        {
            this.writer = writer;
        }
        protected internal override void Print(LoggerLevel level, object format, string text, Exception err)
        {
            string msg = null;
            if (err != null)
                msg = (mName + " - " + text + " : " + err.Message + err.StackTrim());
            else
                msg = (mName + " - " + text);
            lock (writer)
            {
                writer.WriteLine(msg);
                writer.Flush();
            }
            PrintAttach(level, format, text, err, static (log, level, format, text, err) => log.Print(level, format, text, err));
        }
    }
}
