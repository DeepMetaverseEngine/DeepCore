using DeepCore;
using DeepCore.IO;
using DeepCrystal.Persistence.File;
using DeepCrystal.Schedule.QuartzImpl;
using System;
using System.IO;

namespace DeepCrystal.Schedule.QuartzImpl
{
    public class QuartzTestFactory : QuartzScheduleFactory
    {
        public QuartzTestFactory(DirectoryInfo quartzDbDir, DirectoryInfo fileDbDir) 
            : base(InitQuartz(quartzDbDir), TimeZoneInfo.Local)
        {
            
            
        }

        private static DeepCore.Properties InitQuartz(DirectoryInfo quartz_db_dir)
        {
            Console.Out.WriteLine("Init SQLite Tables ...");
            var quartz_db_file = new FileInfo(quartz_db_dir.FullName + "/quartz.db3");
            if (quartz_db_file.Exists == false)
            {
                //var sqlite_db_file = new FileInfo("./quartz_database/tables/tables_sqlite.sql");
                var metas = typeof(QuartzScheduleFactory).Assembly.GetManifestResourceNames();
                var sqlite_db_file = IOUtil.LoadFromAssembly(typeof(QuartzScheduleFactory), "/quartz_database/tables/tables_sqlite.sql");
                string command_txt = CUtils.UTF8.GetString(sqlite_db_file);
                //Console.Out.WriteLine(command_txt);
                //创建数据库文件
                DeepCore.IO.CFiles.CreateDir(quartz_db_dir);
                System.Data.SQLite.SQLiteConnection.CreateFile(quartz_db_file.FullName);


                string strConnectionString = string.Empty;/*SQLite连接字符串，刚开始没有，暂时留空*/
                string strDataSource = quartz_db_file.FullName;//SQLite数据库文件存放物理地址
                                                               //用SQLiteConnectionStringBuilder构建SQLite连接字符串
                var scBuilder = new System.Data.SQLite.SQLiteConnectionStringBuilder();
                scBuilder.DataSource = strDataSource;//SQLite数据库地址
                scBuilder.Password = "";//密码
                strConnectionString = scBuilder.ToString();
                using (var connection = new System.Data.SQLite.SQLiteConnection(strConnectionString))
                {
                    //打开数据连接
                    connection.Open();
                    //Command
                    var command = new System.Data.SQLite.SQLiteCommand(connection);
                    command.CommandText = command_txt;
                    //"CREATE TABLE tb_User(ID int,UserName varchar(60));INSERT INTO [tb_User](ID,UserName) VALUES(1,'A')";// "CREATE TABLE tb_User(ID int,UserName varchar(60));";
                    command.CommandType = System.Data.CommandType.Text;
                    //执行SQL
                    Console.Out.WriteLine("Execute Init SQLite ...");
                    int iResult = command.ExecuteNonQuery();
                    Console.Out.WriteLine("Result = " + iResult);
                    //可省略步骤=======关闭连接
                    connection.Close();
                }
            }
            Console.Out.WriteLine("Init Quartz Properties ...");
            var quartz_prop = new DeepCore.Properties();
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
                quartz_prop["quartz.jobStore.clustered"] = "false";
                quartz_prop["quartz.jobStore.lockHandler.type"] = "Quartz.Impl.AdoJobStore.UpdateLockRowSemaphore, Quartz";
                quartz_prop["quartz.jobStore.txIsolationLevelSerializable"] = "true";

                quartz_prop["quartz.dataSource.default.provider"] = "SQLite-1-0-105";
                quartz_prop["quartz.dataSource.default.connectionString"] = "Data Source=" + quartz_db_file.FullName + ";Version=3;";
                quartz_prop["quartz.dataSource.default.maxConnections"] = "10";

                Console.Out.WriteLine(quartz_prop.ToString());
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
            return quartz_prop;
        }
    }
}
