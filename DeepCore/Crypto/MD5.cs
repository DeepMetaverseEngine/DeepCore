using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace DeepCore
{
    public static class CMD5
    {
        public static string ToHexString(byte[] data)
        {
            var sb = new StringBuilder();
            {
                for (int i = 0; i < data.Length; i++)
                {
                    byte d = data[i];
                    sb.Append(d.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        public static string CalculateMD5(Stream stream)
        {
            System.Security.Cryptography.MD5 md5Hasher = System.Security.Cryptography.MD5.Create();
            md5Hasher.ComputeHash(stream);
            return ToHexString(md5Hasher.Hash);
        }

        public static string CalculateMD5(byte[] data)
        {
            using (var ms = new DeepCore.IO.MemoryStream(data))
            {
                return CalculateMD5(ms);
            }
        }
        public static string CalculateMD5(string text, Encoding encoding)
        {
            using (var ms = new DeepCore.IO.MemoryStream(encoding.GetBytes(text)))
            {
                return CalculateMD5(ms);
            }
        }
        public static string CalculateMD5(string text)
        {
            using (var ms = new DeepCore.IO.MemoryStream(CUtils.UTF8.GetBytes(text)))
            {
                return CalculateMD5(ms);
            }
        }
        public static string CalculateMD5(FileInfo file)
        {
            using (FileStream ms = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
            {
                return CalculateMD5(ms);
            }
        }
    }
}
