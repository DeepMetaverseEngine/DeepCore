using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonSchedule.QuartzImpl
{
    class Test
    {
        public static void Main()
        {
            {
                var quartz_db_file = new FileInfo("./.quartz/quartz.db3");
                if (quartz_db_file.Exists == false)
                {
                    //创建数据库文件
                    System.Data.SQLite.SQLiteConnection.CreateFile(quartz_db_file.FullName);
                }
                var quartz_prop = new CommonLang.Properties();
                {
                    quartz_prop["quartz.scheduler.instanceName"] = "TestScheduler";
                    quartz_prop["quartz.scheduler.instanceId"] = "instance_one";
                    quartz_prop["quartz.threadPool.type"] = "Quartz.Simpl.SimpleThreadPool, Quartz";
                    quartz_prop["quartz.threadPool.threadCount"] = "5";
                    quartz_prop["quartz.threadPool.threadPriority"] = "Normal";
                    quartz_prop["quartz.jobStore.misfireThreshold"] = "60000";
                    quartz_prop["quartz.jobStore.type"] = "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz";
                    quartz_prop["quartz.jobStore.useProperties"] = "false";
                    quartz_prop["quartz.jobStore.dataSource"] = "default";
                    quartz_prop["quartz.jobStore.tablePrefix"] = "QRTZ_";
                    quartz_prop["quartz.jobStore.clustered"] = "true";
                    quartz_prop["quartz.jobStore.lockHandler.type"] = "Quartz.Impl.AdoJobStore.UpdateLockRowSemaphore, Quartz";
                    quartz_prop["quartz.dataSource.default.provider"] = "SQLite-1-0-105";
                    quartz_prop["quartz.dataSource.default.connectionString"] = "Data Source=" + quartz_db_file.FullName + ";Version=3";
                }
                {
                    //"System.Data.SQLite,Version=1.0.66.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139";
                    var metaData = new Quartz.Impl.AdoJobStore.Common.DbMetadata();
                    metaData.AssemblyName = typeof(System.Data.SQLite.AssemblySourceIdAttribute).Assembly.FullName;
                    metaData.BindByName = true;
                    metaData.CommandBuilderType = typeof(System.Data.SQLite.SQLiteCommandBuilder);
                    metaData.CommandType = typeof(System.Data.SQLite.SQLiteCommand);
                    metaData.ConnectionType = typeof(System.Data.SQLite.SQLiteConnection);
                    metaData.ExceptionType = typeof(System.Data.SQLite.SQLiteException);
                    metaData.ParameterDbType = typeof(System.Data.SQLite.TypeAffinity);
                    metaData.ParameterDbTypePropertyName = "DbType";
                    metaData.ParameterNamePrefix = "@";
                    metaData.ParameterType = typeof(System.Data.SQLite.SQLiteParameter);
                    metaData.UseParameterNamePrefixInParameterCollection = true;
                    Quartz.Impl.AdoJobStore.Common.DbProvider.RegisterDbMetadata("SQLite-1-0-105", metaData);
                }
                CommonLang.File.CFiles.CreateDir(quartz_db_file.Directory);
                new QuartzScheduleFactory(quartz_prop);
            }
        }
    }
}
