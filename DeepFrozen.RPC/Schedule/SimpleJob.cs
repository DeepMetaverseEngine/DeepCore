using DeepCore;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Threading;
using DeepCrystal.RPC;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DeepFrozen.Schedule
{

    public class QuartzScheduleFactory : Disposable
    {
        //-----------------------------------------------------------------------------------------------------------------------------
#if false
        public static QuartzScheduleFactory InitQuartzRedis(TimeZoneInfo timezone, string redisConn = "127.0.0.1:6379,allowAdmin=true,syncTimeout=5000")
 {
     var quartz_prop = new DeepCore.Properties();
     {
         quartz_prop["quartz.scheduler.instanceName"] = "RedisScheduler";
         quartz_prop["quartz.scheduler.instanceId"] = "instance_one";
         //quartz_prop["quartz.threadPool.type"] = $"{typeof(Quartz.Simpl.SimpleThreadPool).FullName}, Quartz";
         quartz_prop["quartz.threadPool.type"] = $"{typeof(Quartz.Simpl.DefaultThreadPool).FullName}, Quartz";
         quartz_prop["quartz.serializer.type"] = "json";
         quartz_prop["quartz.threadPool.threadCount"] = "10";
 
         quartz_prop["quartz.jobStore.type"] = "QuartzRedisJobStore.JobStore.RedisJobStore,QuartzRedisJobStore.JobStore";
         quartz_prop["quartz.jobStore.redisConfiguration"] = redisConn;
         quartz_prop["quartz.jobStore.keyPrefix"] = "RedisScheduler";
 
         Console.Out.WriteLine(quartz_prop.ToString());
     }
 
     return new QuartzScheduleFactory(quartz_prop, timezone);
 }
        public static QuartzScheduleFactory InitQuartzSQLite(TimeZoneInfo timezone, DirectoryInfo quartz_db_dir = null)
        {
            quartz_db_dir ??= new DirectoryInfo(Environment.CurrentDirectory + "/quartz");
            Console.Out.WriteLine("Init SQLite Tables ...");
            var sqlLiteVer = "SQLite-1-0-112";
            var quartz_db_file = new FileInfo(quartz_db_dir.FullName + "/quartz.db3");
            if (quartz_db_file.Exists == false || quartz_db_file.Length == 0)
            {
                try
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
                    //scBuilder.Password = "123456";//密码
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
                catch
                {
                    CFiles.Delete(quartz_db_file.FullName);
                    throw;
                }
            }
            Console.Out.WriteLine("Init Quartz Properties ...");
            var quartz_prop = new DeepCore.Properties();
            {
                quartz_prop["quartz.scheduler.instanceName"] = "SQLiteScheduler";
                quartz_prop["quartz.scheduler.instanceId"] = "instance_one";
                //quartz_prop["quartz.threadPool.type"] = $"{typeof(Quartz.Simpl.SimpleThreadPool).FullName}, Quartz";
                quartz_prop["quartz.threadPool.type"] = $"{typeof(Quartz.Simpl.DefaultThreadPool).FullName}, Quartz";
                quartz_prop["quartz.serializer.type"] = "json";
                quartz_prop["quartz.threadPool.threadCount"] = "10";
                quartz_prop["quartz.jobStore.misfireThreshold"] = "60000";
                quartz_prop["quartz.jobStore.type"] = $"{typeof(Quartz.Impl.AdoJobStore.JobStoreTX).FullName}, Quartz";
                quartz_prop["quartz.jobStore.driverDelegateType"] = $"{typeof(Quartz.Impl.AdoJobStore.StdAdoDelegate).FullName}, Quartz";
                quartz_prop["quartz.jobStore.useProperties"] = "false";
                quartz_prop["quartz.jobStore.dataSource"] = "default";
                quartz_prop["quartz.jobStore.tablePrefix"] = "QRTZ_";
                quartz_prop["quartz.jobStore.clustered"] = "false";
                quartz_prop["quartz.jobStore.driverDelegateType"] = $"{typeof(Quartz.Impl.AdoJobStore.SQLiteDelegate).FullName}, Quartz";

                quartz_prop["quartz.dataSource.default.provider"] = sqlLiteVer;
                quartz_prop["quartz.dataSource.default.connectionString"] = $"Data Source={quartz_db_file.FullName};Version=3;";
                quartz_prop["quartz.dataSource.default.maxConnections"] = "10";

                Console.Out.WriteLine(quartz_prop.ToString());
            }
            {
                var metaData = new Quartz.Impl.AdoJobStore.Common.DbMetadata()
                {
                    AssemblyName = typeof(System.Data.SQLite.AssemblySourceIdAttribute).Assembly.FullName,
                    BindByName = true,
                    //CommandBuilderType = typeof(System.Data.SQLite.SQLiteCommandBuilder),
                    CommandType = typeof(System.Data.SQLite.SQLiteCommand),
                    ConnectionType = typeof(System.Data.SQLite.SQLiteConnection),
                    ExceptionType = typeof(System.Data.SQLite.SQLiteException),
                    ParameterDbType = typeof(System.Data.SQLite.TypeAffinity),
                    ParameterDbTypePropertyName = "DbType",
                    ParameterNamePrefix = "@",
                    ParameterType = typeof(System.Data.SQLite.SQLiteParameter),
                    UseParameterNamePrefixInParameterCollection = true,
                };
                Quartz.Impl.AdoJobStore.Common.DbProvider.RegisterDbMetadata(sqlLiteVer, metaData);
            }
            return new QuartzScheduleFactory(null, quartz_prop, timezone);
        }
#endif
        //-----------------------------------------------------------------------------------------------------------------------------
        #region Instance
        private static QuartzScheduleFactory s_instance;
        public static QuartzScheduleFactory Factory
        {
            get { return s_instance; }
        }
        private static Logger log = LoggerFactory.GetLogger(nameof(Quartz));
        private SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private Quartz.IScheduler persist_scheduler;
        private Quartz.IScheduler runtime_scheduler;
        private TimeZoneInfo timezone;
        private ConcurrentDictionary<string, CornJobTaskQueue> taskMap = new ConcurrentDictionary<string, CornJobTaskQueue>();
        public QuartzScheduleFactory(
            TimeZoneInfo timezone,
            DeepCore.Properties runtime_properties = null,
            DeepCore.Properties psersist_properties = null)
        {
            QuartzScheduleFactory.s_instance = this;
            AsSynchronizedDisposing();
            this.timezone = timezone;
            try
            {
                var prop = new NameValueCollection();
                if (runtime_properties != null)
                {
                    foreach (var e in runtime_properties)
                    {
                        prop.Add(e.Key, e.Value);
                    }
                }
                var factory = new StdSchedulerFactory(prop);
                this.runtime_scheduler = factory.GetScheduler().WaitForResult();
                this.runtime_scheduler.Start().Wait();
            }
            catch (Exception err)
            {
                log.Error(err);
            }
            try
            {

                var prop = new NameValueCollection();
                if (psersist_properties != null)
                {
                    foreach (var e in psersist_properties)
                    {
                        prop.Add(e.Key, e.Value);
                    }
                }
                var factory = new StdSchedulerFactory(prop);
                this.persist_scheduler = factory.GetScheduler().WaitForResult();
                this.persist_scheduler.Start().Wait();
            }
            catch (Exception err)
            {
                log.Error(err);
                this.persist_scheduler = this.runtime_scheduler;
            }
        }
        ~QuartzScheduleFactory()
        {
            try { _semaphoreSlim.Dispose(); } catch { }
        }
        protected override void Disposing()
        {
            try
            {
                persist_scheduler.Shutdown().Wait();
            }
            catch (Exception err) { log.Error(err); }
            try
            {
                runtime_scheduler.Shutdown().Wait();
            }
            catch (Exception err) { log.Error(err); }
        }
        #endregion
        //-----------------------------------------------------------------------------------------------------------------------------
        public async Task<Disposable> CreateCornJobAsync(RemoteAddress svc, string corn_expression, CornJobMissFirePolicy missFire, object state, Action<ICornJobContext> callback)
        {
            var jobName = $"{corn_expression}";
            if (missFire != CornJobMissFirePolicy.DoNothing)
            {
                jobName = $"{svc.ServiceName}-{corn_expression}-{missFire}";
            }
            var queue = taskMap.GetOrAdd(jobName, (n) => new CornJobTaskQueue(n));
            var task = new CornJobTask(queue, jobName, state, callback);
            if (missFire == CornJobMissFirePolicy.DoNothing)
            {
                await GetOrCreateCornJob(this.runtime_scheduler, jobName, corn_expression, missFire);
            }
            else
            {
                await GetOrCreateCornJob(this.persist_scheduler, jobName, corn_expression, missFire);
            }
            return task;
        }

        internal async Task GetOrCreateCornJob(IScheduler scheduler, string jobName, string corn_expression, CornJobMissFirePolicy missFire)
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                var job = await scheduler.GetJobDetail(new JobKey(jobName));
                if (job == null)
                {
                    var jb = Quartz.JobBuilder.Create<SimpleQuartzJob>()
                        .WithIdentity(jobName)
                        .WithDescription(typeof(SimpleQuartzJob).FullName)
                        .RequestRecovery()
                        .UsingJobData("_corn_expression", corn_expression)
                        .UsingJobData("_job_type", typeof(SimpleQuartzJob).FullName);
                    job = jb.Build();
                    var trigger = TriggerBuilder.Create()
                        .WithIdentity(jobName)
                        .WithCronSchedule(corn_expression, process_cron_trigger)
                        .ForJob(jobName)
                        .Build();
                    await scheduler.ScheduleJob(job, trigger);
                }
            }
            catch (Exception err)
            {
                log.Error(err);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
            void process_cron_trigger(CronScheduleBuilder x)
            {
                switch (missFire)
                {
                    case CornJobMissFirePolicy.FireHistory:
                        x.WithMisfireHandlingInstructionIgnoreMisfires();
                        break;
                    case CornJobMissFirePolicy.FireOnceNow:
                        x.WithMisfireHandlingInstructionFireAndProceed();
                        break;
                    case CornJobMissFirePolicy.DoNothing:
                    default:
                        x.WithMisfireHandlingInstructionDoNothing();
                        break;
                }
                x.InTimeZone(timezone);
            }
        }
        internal bool TryGetJobTaskQueue(string jobName, out CornJobTaskQueue queue)
        {
            return taskMap.TryGetValue(jobName, out queue);
        }
        internal class CornJobTaskQueue
        {
            public readonly string jobName;
            private readonly LinkedList<CornJobTask> jobQueue = new LinkedList<CornJobTask>();
            public CornJobTaskQueue(string jobName)
            {
                this.jobName = jobName;
            }
            internal LinkedListNode<CornJobTask> Add(CornJobTask node)
            {
                lock (jobQueue)
                {
                    return jobQueue.AddLast(node);
                }
            }
            internal void Remove(LinkedListNode<CornJobTask> node)
            {
                lock (jobQueue)
                {
                    jobQueue.Remove(node);
                }
            }
            internal void InvokeAll(IJobExecutionContext context)
            {
                var list = new List<CornJobTask>();
                {
                    lock (jobQueue) { list.AddRange(jobQueue); }
                    foreach (var task in list)
                    {
                        if (!task.IsDisposed)
                        {
                            task.Invoke(context);
                        }
                    }
                }
            }
        }
        internal class CornJobTask : Disposable
        {
            private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(nameof(CornJobTask));
            private readonly LinkedListNode<CornJobTask> linkNode;
            private CornJobTaskQueue queue;
            public string jobName;
            private object state;
            private Action<ICornJobContext> callback;
            public CornJobTask(CornJobTaskQueue queue, string jobName, object state, Action<ICornJobContext> callback)
            {
                Alloc.RecordConstructor(jobName);
                this.queue = queue;
                this.jobName = jobName;
                this.state = state;
                this.callback = callback;
                this.linkNode = queue.Add(this);
            }
            ~CornJobTask()
            {
                Alloc.RecordDestructor(jobName);
            }
            protected override void RecordDisposing()
            {
                Alloc.RecordDispose(jobName);
            }
            protected override void Disposing()
            {
                this.callback = null;
                this.state = null;
                this.queue.Remove(this.linkNode);
            }
            internal void Invoke(IJobExecutionContext context)
            {
                callback(new SimpleJobContext(context, state));
            }
        }
        internal struct SimpleJobContext : ICornJobContext
        {
            private IJobExecutionContext context;
            private object state;

            public DateTimeOffset FireTimeUtc => context.FireTimeUtc;
            public DateTimeOffset? ScheduledFireTimeUtc => context.ScheduledFireTimeUtc;
            public DateTimeOffset? NextFireTimeUtc => context.NextFireTimeUtc;
            public DateTimeOffset? PreviousFireTimeUtc => context.PreviousFireTimeUtc;
            public object State => state;
            public SimpleJobContext(IJobExecutionContext context, object state)
            {
                this.context = context;
                this.state = state;
            }
        }
    }
    public class SimpleQuartzJob : IJob
    {
        public virtual Task Execute(IJobExecutionContext context)
        {
            if (QuartzScheduleFactory.Factory.TryGetJobTaskQueue(context.JobDetail.Key.Name, out var queue))
            {
                queue.InvokeAll(context);
            }
            return Task.CompletedTask;
        }
    }
}