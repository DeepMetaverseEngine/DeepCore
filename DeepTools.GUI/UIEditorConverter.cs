using DeepCore.GUI.Cell;
using DeepCore.IO;
using DeepCore.Xml;
using DeepTools.Voxel;
using System;
using System.IO;

namespace DeepCore.GUI.Data
{
    public class UIEditorConverter
    {
        public delegate bool TryConvertToBin(FileInfo xml, FileInfo bin);
        public delegate bool TryConvertToCPJ(FileInfo xml, FileInfo bin);


        public void ConvertXML(DirectoryInfo dir)
        {
            foreach (FileInfo sub in dir.GetFiles())
            {
                ConvertXML(sub);
            }
            foreach (DirectoryInfo sub in dir.GetDirectories())
            {
                ConvertXML(sub);
            }
        }
        public void ConvertXML(FileInfo xml_file)
        {
            //             if (xml_file.FullName.EndsWith(".gui.xml"))
            //             {
            //                 try
            //                 {
            //                     string bin_name = xml_file.FullName.Substring(0, xml_file.FullName.LastIndexOf(".gui.xml")) + ".gui.bin";
            //                     FileInfo bin_file = new FileInfo(bin_name);
            //                     if (TryConvertGUI == null || !TryConvertGUI.Invoke(xml_file, bin_file))
            //                     {
            //                         Console.Write(string.Format("ConvertXML: {0} -> {1}", xml_file.FullName, Path.GetFileName(bin_name)));
            //                         var xml = XmlUtil.LoadXML(xml_file.FullName);
            //                         if (xml != null && xml.DocumentElement.Name == UIEditorMeta.UERoot_ClassName)
            //                         {
            //                             var meta = UIEditorMeta.CreateFromXml(xml);
            //                             var bin = UIEditorMeta.SaveToBin(meta);
            //                             System.IO.File.WriteAllBytes(bin_file.FullName, bin);
            //                             var meta2 = UIEditorMeta.CreateFromBin(bin);
            //                             var bin2 = UIEditorMeta.SaveToBin(meta2);
            //                             if (!CUtils.ArraysEqual<byte>(bin, bin2))
            //                             {
            //                                 throw new Exception("Bad IO");
            //                             }
            //                         }
            //                         Console.WriteLine(" OK!");
            //                     }
            //                 }
            //                 catch (Exception err)
            //                 {
            //                     Console.WriteLine(err.Message);
            //                     Console.WriteLine(err.StackTrace);
            //                 }
            //             }
        }


        public void ConvertCPJ(DirectoryInfo dir)
        {
            foreach (FileInfo sub in dir.GetFiles())
            {
                ConvertCPJ(sub);
            }
            foreach (DirectoryInfo sub in dir.GetDirectories())
            {
                ConvertCPJ(sub);
            }
        }
        public void ConvertCPJ(FileInfo xml_file)
        {
            if (xml_file.FullName.EndsWith(".xml"))
            {
                var res_name = Path.GetFileNameWithoutExtension(xml_file.FullName);
                if (File.Exists(xml_file.Directory.Parent.FullName + Path.DirectorySeparatorChar + res_name + ".cpj") ||
                    File.Exists(xml_file.Directory.Parent.FullName + Path.DirectorySeparatorChar + res_name + ".xcpj"))
                {
                    try
                    {
                        string bin_name = xml_file.Directory.FullName + Path.DirectorySeparatorChar + res_name + ".bin";
                        FileInfo bin_file = new FileInfo(bin_name);
                        {
                            Console.Write(string.Format("ConvertCPJ: {0} -> {1}", xml_file.FullName, Path.GetFileName(bin_name)));
                            var cpj = CPJFileLoader.LoadXML(xml_file.FullName);
                            var bin = CPJFileLoader.SaveToBin(cpj);
                            VoxelConverter.SaveCPJMapToVoxel(cpj, xml_file.Directory);
                            CFiles.WriteAllBytes(bin_file.FullName, bin);
                            Console.WriteLine(" OK!");
                        }
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine(err.Message);
                        Console.WriteLine(err.StackTrace);
                    }
                }
            }
        }


        public void TestCPJ(DirectoryInfo dir)
        {
            foreach (FileInfo sub in dir.GetFiles())
            {
                TestCPJ(sub);
            }
            foreach (DirectoryInfo sub in dir.GetDirectories())
            {
                TestCPJ(sub);
            }
        }
        public void TestCPJ(FileInfo xml_file)
        {
            if (xml_file.FullName.EndsWith(".xml"))
            {
                var res_name = Path.GetFileNameWithoutExtension(xml_file.FullName);
                if (File.Exists(xml_file.Directory.Parent.FullName + Path.DirectorySeparatorChar + res_name + ".cpj") ||
                    File.Exists(xml_file.Directory.Parent.FullName + Path.DirectorySeparatorChar + res_name + ".xcpj"))
                {
                    try
                    {
                        string bin_name = xml_file.Directory.FullName + Path.DirectorySeparatorChar + res_name + ".bin";
                        FileInfo bin_file = new FileInfo(bin_name);
                        Console.Write(string.Format("TestCPJ: {0}", bin_file.FullName));
                        if (bin_file.Exists)
                        {
                            var binfile = CPJFileLoader.LoadBin(bin_name);
                            if (binfile != null)
                            {
                                Console.WriteLine(" OK!");
                                return;
                            }
                        }
                        Console.WriteLine(" Failed!");
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine(err.Message);
                        Console.WriteLine(err.StackTrace);
                    }
                }
            }
        }
    }
}
