using DeepCore.Log;
using DeepCore.Xml;
using DeepEditor.Common;
using System;
using System.IO;

namespace DeepEditorConsole
{
    public class ProjectRuntime
    {
        private static Logger log = new LazyLogger("ProjectRuntime");
        public static string EncodeHTML(string src)
        {
            var text = src;
            //text = System.Web.HttpUtility.HtmlEncode(text);
            text = text.Replace(" ", @"&#32;");
            text = text.Replace(",", @"&#44;");
            return text;
        }
        public static string DecodeHTML(string src)
        {
            var text = src;
            text = text.Replace(@"&#32;", " ");
            text = text.Replace(@"&#44;", ",");
            //text = System.Web.HttpUtility.HtmlDecode(src);
            return text;
        }
        public static T LoadXmlAs<T>(FileInfo path, T default_value = default)
        {
            try
            {
                var bin = FileSystemWorkSpace.ReadAllBytes(path);
                if (bin != null)
                {
                    return LoadXmlAs<T>(bin, default_value);
                }
            }
            catch
            {
                log.Error($"LoadXml Error : {path} Use Default {default_value}!!!");
            }
            return default_value;
        }
        private static T LoadXmlAs<T>(byte[] data, T default_value = default)
        {
            try
            {
                var xml = XmlUtil.LoadXML(data);
                if (xml != null)
                    return XmlUtil.XmlToObject<T>(xml);
            }
            catch (Exception err)
            {
                err.PrintStackTrace("LoadXml Error : " + data + "\n" + err.Message);
            }
            return default_value;
        }
        public void SaveXml(object obj, FileInfo file)
        {
            try
            {
                if (obj != null)
                {
                    var bytes = XmlUtil.ObjectToXmlBin(obj);
                    FileSystemWorkSpace.WriteAllBytes(file, bytes);
                    //                     using (FileStream xmlfs = new FileStream(file1, FileMode.Create, FileAccess.Write))
                    //                     {
                    //                         XmlUtil.SaveToXML(xmlfs, obj, true);
                    //                     }
                    //                     FS.Save(file);
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace();
            }
        }
    }

    public delegate void SavingAction(FileInfo path, object data);
    public delegate void SavedAction(FileInfo path, object data);

    public delegate void LoadingAction(FileInfo path);
    public delegate void LoadedAction(FileInfo path, object data);
}
