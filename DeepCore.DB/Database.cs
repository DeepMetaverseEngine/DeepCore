using DeepCore.IO;
using DeepCore.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DeepCore.DB
{
    public class LevelDBDatabaseDriver : IDatabaseDriver
    {
        private readonly DirectoryInfo SaveDir;
        public override DirectoryInfo DataPath => SaveDir;
        public LevelDBDatabaseDriver(string saveDir)
        {
            this.SaveDir = new DirectoryInfo(saveDir);
            CFiles.CreateDir(SaveDir);
        }
        protected override void Disposing()
        {
            CFiles.Delete(SaveDir.FullName);
        }
        public override IDatabase OpenRead(TemplateDataCenter path)
        {
            return new DB(new LevelDB.Options() { CreateIfMissing = true }, SaveDir.FullName);
        }
        public override IDatabase OpenWrite(TemplateDataCenter path)
        {
            return new DB(new LevelDB.Options() { CreateIfMissing = true, }, SaveDir.FullName);
        }

        public class DB : IDatabase
        {
            private readonly string path;
            private LevelDB.DB m_Config;
            private LevelDB.ReadOptions m_ReadOption = new LevelDB.ReadOptions()
            {
            };
            private LevelDB.WriteOptions m_WriteOption = new LevelDB.WriteOptions()
            {
            };
            public DB(LevelDB.Options op, string path)
            {
                CFiles.CreateDir(path);
                this.path = path;
                this.m_Config = new LevelDB.DB(op, path);
            }
            public void Dispose()
            {
                this.m_Config.Dispose();
            }
            public byte[] Get(string key)
            {
                return m_Config.Get(CUtils.UTF8.GetBytes(key), m_ReadOption);
            }
            public Task<byte[]> GetAsync(string key)
            {
                var c = m_Config.Get(CUtils.UTF8.GetBytes(key), m_ReadOption);
                return Task.FromResult(c);
            }
            public void Put(string key, byte[] value)
            {
                m_Config.Put(CUtils.UTF8.GetBytes(key), value, m_WriteOption);
            }
            public void Dump()
            {
                //var e = m_Config.GetEnumerator();
                var sb = new StringBuilder();
                foreach (var e in (m_Config as IEnumerable<KeyValuePair<string, string>>))
                {
                    sb.Append(e.Key).Append("\t").AppendLine(e.Value);
                }
                var root = new DirectoryInfo(this.path);
                var f = new FileInfo($"{root.Parent}\\{root.Name}\\.dump.txt");
                CFiles.WriteAllText(f, sb.ToString(), CUtils.UTF8_BOM);
            }
        }
    }

}
