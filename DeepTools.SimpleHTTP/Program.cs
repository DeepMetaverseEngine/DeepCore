using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Net;
using System.IO;
using System.Text;

namespace DeepTools.SimpleHTTP
{

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            if (args.Length > 0)
            {
                Environment.CurrentDirectory = new DirectoryInfo(args[0]).FullName;
            }
            HttpListener server = new HttpListener();
            server.Prefixes.Add("http://127.0.0.1/");
            server.Prefixes.Add("http://localhost/");
            server.Prefixes.Add("http://0.0.0.0/");
            server.Start();
            Console.WriteLine("Listening : " + Environment.CurrentDirectory);
            while (true)
            {
                HttpListenerContext context = server.GetContext();
                HttpListenerResponse response = context.Response;
                string page = Directory.GetCurrentDirectory() + context.Request.Url.LocalPath;
                if (System.IO.File.Exists(page))
                {
                    TextReader tr = new StreamReader(page);
                    string msg = tr.ReadToEnd();
                    byte[] buffer = Encoding.UTF8.GetBytes(msg);
                    response.ContentLength64 = buffer.Length;
                    Stream st = response.OutputStream;
                    st.Write(buffer, 0, buffer.Length);
                    st.Flush();
                }
                context.Response.Close();
            }

        }
    }

}