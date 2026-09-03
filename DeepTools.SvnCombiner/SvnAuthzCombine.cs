using DeepCore;
using DeepCore.Xml;
using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace DeepTools.SvnCombiner
{
    public class SvnAuthzCombine
    {
        private readonly List<FileInfo> inputfiles = new List<FileInfo>();
        private readonly DirectoryInfo inputdir;
        private readonly FileInfo outputfile;
        //Create an instance of a ini file parser
        private readonly FileIniDataParser fileIniData = new FileIniDataParser();
        private readonly FileInfo rootAuthzFile;
        private readonly KeyDataCollection rootAuthzCombine;
        private readonly Filter filter = new Filter();


        public SvnAuthzCombine(DirectoryInfo input, FileInfo output)
        {
            Console.WriteLine(CUtils.SequenceChar('-', 20) + DateTime.Now.ToString() + CUtils.SequenceChar('-', 20));
            // This is a special ini file where we use the '#' character for comment lines
            // instead of ';' so we need to change the configuration of the parser:
            fileIniData.Parser.Configuration.CommentString = "#";
            fileIniData.Parser.Configuration.LinkNextLineString = ",";
            //fileIniData.Parser.Configuration.AllowDuplicateKeys = true;
            outputfile = output;
            inputdir = input;
            rootAuthzFile = new FileInfo(inputdir.FullName + "\\authz.ini");
            if (rootAuthzFile.Exists)
            {
                inputfiles.Add(rootAuthzFile);
                Console.WriteLine("Append : " + rootAuthzFile.FullName);
                var rootAuthz = fileIniData.ReadFile(rootAuthzFile.FullName, Encoding.UTF8);
                rootAuthzCombine = rootAuthz.Sections["combine"];
            }
            else
            {
                throw new Exception("root authz not exist : " + rootAuthzFile.FullName);
            }
            foreach (var sub in inputdir.GetDirectories())
            {
                var sf = new FileInfo(sub.FullName + "\\authz.ini");
                if (sf.Exists)
                {
                    inputfiles.Add(sf);
                    Console.WriteLine("Append : " + sf.FullName);
                }
            }
        }


        public void Combine()
        {
            if (inputdir != null && outputfile != null)
            {
                IniData out_ini = new IniData();
                out_ini.Configuration.NewLineStr = "\r\n";
                out_ini.Configuration.CommentString = "#";
                //out_ini.Configuration.AllowDuplicateKeys = true;
                foreach (var inputfile in inputfiles)
                {
                    AddInputFile(out_ini, inputfile);
                }
                if (outputfile.Exists) { outputfile.Delete(); }
                fileIniData.WriteFile(outputfile.FullName, out_ini, Encoding.UTF8);
            }
        }

        public void SetFilter(string txt)
        {
            filter.Load(txt);
        }


        private void AddInputFile(IniData outIni, FileInfo input)
        {
            Console.WriteLine(CUtils.SequenceChar('-', 10) + "BEGIN : " + input.FullName + CUtils.SequenceChar('-', 100 - input.FullName.Length));
            try
            {
                //Parse the ini file
                IniData parsedData = fileIniData.ReadFile(input.FullName, Encoding.UTF8);
                //Write down the contents of the ini file to the console
                Console.WriteLine();
                Console.WriteLine(parsedData);
                Console.WriteLine();

                parsedData.ClearAllComments();
                bool firstSection = true;
                foreach (var se in new List<SectionData>(parsedData.Sections))
                {
                    if (se.SectionName == "groups")
                    {
                        parsedData.Global.AddKey("# " + SubPath(input), GetAuthzCombine(input));
                        //var klist = new List<KeyData>(se.Keys);
                        //var gname = input.Directory.Name;
                        //se.ClearKeyData();
                        foreach (var key in se.Keys)
                        {
                            key.Comments.Add("##  " + SubPath(input) + "      ");
                            break;
                        }
                    }
                    else if (se.SectionName == "combine")
                    {
                        parsedData.Sections.RemoveSection(se.SectionName);
                    }
                    else if (se.SectionName == "alias")
                    {

                    }
                    else if (Regex.IsMatch(se.SectionName, @"(\w+:)") || (input.Equals(rootAuthzFile) && se.SectionName.Trim().Equals("/")))
                    {
                        if (firstSection)
                        {
                            firstSection = false;
                            se.LeadingComments.Add(CUtils.SequenceChar('#', Math.Max(40, SubPath(input).Length + 10)));
                            se.LeadingComments.Add("##  " + SubPath(input) + "      ");
                            se.LeadingComments.Add(CUtils.SequenceChar('#', Math.Max(40, SubPath(input).Length + 10)));
                        }
                        if (ValidateRepo(input, se) == false)
                        {
                            Console.WriteLine(CUtils.FormatBlockTableString("Error : repo not validate : " + input.Directory.Name + "\n " + input.FullName));
                            se.ClearKeyData();
                            se.Keys.AddKey("*", "");
                        }
                        filter.Process(se);
                        se.Keys.AddKey("@admin", "rw");
                    }
                    else
                    {
                        parsedData.Sections.RemoveSection(se.SectionName);
                        parsedData.Global.AddKey("#Error [" + se.SectionName + "]", "Unknow Section @ " + SubPath(input));
                    }
                }
                outIni.Merge(parsedData);
            }
            catch (Exception err)
            {
                outIni.Global.AddKey("#Error : Process Input File : " + SubPath(input), err.Message);
                Console.WriteLine(CUtils.FormatBlockTableString("Error : Process Input File : " + input.FullName + "\n" + err.Message));
            }
            finally
            {
                Console.WriteLine(CUtils.SequenceChar('-', 10) + "END : " + input.FullName + CUtils.SequenceChar('-', 102 - input.FullName.Length));
            }
        }

        private bool ValidateRepo(FileInfo file, SectionData se)
        {
            if (file.Equals(rootAuthzFile)) { return true; }
            if (rootAuthzCombine != null)
            {
                try
                {
                    var dname = file.Directory.Name;
                    if (rootAuthzCombine.ContainsKey(dname))
                    {
                        var repos = rootAuthzCombine[dname];
                        var repos_list = Regex.Split(repos, @"\s*,\s*");
                        var rname = se.SectionName.Substring(0, se.SectionName.LastIndexOf(":"));
                        if (repos_list.Contains(rname))
                        {
                            return true;
                        }
                    }
                }
                catch (Exception err)
                {
                    Console.WriteLine(CUtils.FormatBlockTableString("Error : ValidateRepo : " + file.FullName + "\n" + err.Message));
                }
            }
            return false;
        }

        private string GetAuthzCombine(FileInfo file)
        {
            if (file.Equals(rootAuthzFile)) { return "[ALL]"; }
            if (rootAuthzCombine != null)
            {
                var dname = file.Directory.Name;
                if (rootAuthzCombine.ContainsKey(dname))
                {
                    var repos = rootAuthzCombine[dname];

                    if (repos != null)
                    {
                        return repos;
                    }
                }
            }
            return "[ACCESS DENIED]";
        }

        private string SubPath(FileInfo file)
        {
            return file.FullName.Substring(inputdir.FullName.Length);
        }

        public class Filter
        {
            struct FilterInfo
            {
                public Regex regex;
                public string key;
                public string value;
            }
            private List<FilterInfo> filters = new List<FilterInfo>();
            public Filter() { }
            public void Load(string txt)
            {
                var xml = XmlUtil.FromString(txt);
                foreach (XmlElement e in xml.DocumentElement)
                {
                    filters.Add(new FilterInfo()
                    {
                        regex = new Regex(e["regex"].InnerText),
                        key = e["key"].InnerText,
                        value = e["value"].InnerText,
                    });
                }
            }
            public void Process(SectionData se)
            {
                foreach (var f in filters)
                {
                    if (f.regex.IsMatch(se.SectionName))
                    {
                        se.Keys.AddKey(f.key, f.value);
                    }
                }
            }
        }
    }
}
