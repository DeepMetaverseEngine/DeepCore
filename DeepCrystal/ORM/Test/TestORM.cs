using DeepCore;
using DeepCore.Threading;
using DeepCore.Xml;
using DeepCrystal.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DeepCrystal.ORM
{

    public class TestTransaction
    {
        protected readonly DirectoryInfo dir;
        protected readonly string filePrefix;
        private ArrayList<TestMappingReference> test_entries = new ArrayList<TestMappingReference>();

        public TestTransaction(DirectoryInfo dir, string perfix, IObjectTransaction trans, IMappingAdapter db = null, ITaskExecutor exe = null)
        {
            this.dir = dir;
            this.filePrefix = perfix;
            if (ORMFactory.IsTest)
            {
                trans.DebugForEachMappingObject((mapping) =>
                {
                    var reference = Accept(mapping);
                    if (reference != null && reference.IsTopMapping)
                    {
                        var data = mapping.Data;
                        test_entries.Add(new TestMappingReference(reference, db, exe));
                    }
                });
            }
        }
        public async Task<bool> CheckAsync(bool trace = false)
        {
            bool result = true;
            if (ORMFactory.IsTest)
            {
                foreach (var entry in test_entries)
                {
                    if (!await CheckAsync(entry, trace))
                    {
                        result = false;
                    }
                }
            }
            return result;
        }
        protected virtual MappingReference Accept(MappingObject mapping)
        {
            if (mapping is MappingReference reference)
            {
                var dataType = mapping.Data;
                return reference;
            }
            return null;
        }
        protected virtual string FormatNamePrefix(Type dataType, string suffix)
        {
            var time = DateTime.Now;
            var dir = this.dir.FullName + Path.DirectorySeparatorChar + dataType.Name;
            var prefix = dir + Path.DirectorySeparatorChar + suffix + Path.DirectorySeparatorChar + filePrefix + "_" + time.ToString("yyyyMMdd_HHmmss");
            return prefix;
        }
        protected virtual async Task<bool> CheckAsync(TestMappingReference entry, bool trace)
        {
            var fileA = new FileInfo(FormatNamePrefix(entry.DataType, "A") + ".xml");
            var fileB = new FileInfo(FormatNamePrefix(entry.DataType, "B") + ".xml");
            if (!await entry.TestAsync())
            {
                entry.SaveDump(fileA, fileB);
                return false;
            }
            else if (trace)
            {
                entry.SaveDump(fileA);
            }
            return true;
        }
    }

    public class TestMappingReference
    {
        protected readonly MappingObjectXmlSerializer ser = new MappingObjectXmlSerializer();
        private readonly MappingReference src;
        private readonly IMappingAdapter db;
        private readonly ITaskExecutor exe;
        public string Key { get => src.Key; }
        public Type DataType { get => src.DataType; }
        public bool IsSucceed { get => (XmlA == XmlB); }
        public string XmlA { get; private set; }
        public string XmlB { get; private set; }
        public Exception Error { get; private set; }

        public TestMappingReference(MappingReference mapping, IMappingAdapter db = null, ITaskExecutor exe = null)
        {
            if (exe == null) { exe = mapping.Executor; }
            if (db == null) { db = mapping.Adapter; }
            this.db = db;
            this.src = mapping;
            this.exe = exe;
            this.XmlA = ser.ObjectToXml(src.Data).ToXmlString();
        }

        public async Task<bool> TestAsync()
        {
            try
            {
                using (var orm = new MappingReference(null, src.Key, src.DataType, exe, db))
                {
                    var dataB = await orm.LoadDataAsync();
                    this.XmlB = ser.ObjectToXml(dataB).ToXmlString();
                    if (XmlA == XmlB)
                    {
                        return true;
                    }
                }
            }
            catch (Exception err)
            {
                this.Error = err;
                throw;
            }
            return false;
        }

        public void SaveDump(FileInfo fileA, FileInfo fileB)
        {
            DeepCore.IO.CFiles.CreateDir(fileA.Directory);
            DeepCore.IO.CFiles.CreateDir(fileB.Directory);
            if (Error != null)
            {
                File.WriteAllText(fileA.FullName, XmlA);
                File.WriteAllText(fileB.FullName, Error.Message + Environment.NewLine + Error.StackTrace);
            }
            else
            {
                File.WriteAllText(fileA.FullName, XmlA);
                File.WriteAllText(fileB.FullName, XmlB);
            }
        }
        public void SaveDump(FileInfo fileA)
        {
            DeepCore.IO.CFiles.CreateDir(fileA.Directory);
            File.WriteAllText(fileA.FullName, XmlA);
        }


        public static async Task<bool> RunTestAsync(MappingReference mapping, Func<TestMappingReference, Task> beginTest)
        {
            TestMappingReference test = new TestMappingReference(mapping);
            await beginTest(test);
            return await test.TestAsync();
        }
        public static async Task<TestMappingReference> RunTestAsync(MappingReference mapping, Func<TestMappingReference, Task> beginTest, FileInfo fileA, FileInfo fileB)
        {
            TestMappingReference test = new TestMappingReference(mapping);
            await beginTest(test);
            if (!await test.TestAsync())
            {
                test.SaveDump(fileA, fileB);
            }
            return test;
        }
    }

}
