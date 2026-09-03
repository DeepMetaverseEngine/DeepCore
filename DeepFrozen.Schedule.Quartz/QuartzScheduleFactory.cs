using DeepCore;
using DeepCore.Log;
using DeepCore.Reflection;
using DeepCrystal.Schedule;
using Quartz;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace DeepCrystal.Schedule.QuartzImpl
{
    public class QuartzScheduleFactory : ScheduleFactory
    {
        static QuartzScheduleFactory()
        {
            Common.Logging.LogManager.Adapter = new LoggerAdapter();
        }
        private Quartz.ISchedulerFactory factory;
        private TimeZoneInfo timezone;

        public QuartzScheduleFactory(Properties properties, TimeZoneInfo timezone)
        {
            this.timezone = timezone;
            if (properties == null)
            {
                this.factory = new Quartz.Impl.StdSchedulerFactory();
            }
            else
            {
                var prop = new NameValueCollection();
                foreach (var e in properties)
                {
                    prop.Add(e.Key, e.Value);
                }
                this.factory = new Quartz.Impl.StdSchedulerFactory(prop);
            }
        }

        public override ISchedule GetScheduler(string group, MissFirePolicy missfire)
        {
            var scheduler = factory.GetScheduler();
            return new QuartzScheduleImpl(group, missfire, scheduler, timezone);
        }
    }

    public class QuartzScheduleImpl : ISchedule
    {
        private readonly string group_name;
        private readonly MissFirePolicy missfire;
        private readonly Quartz.IScheduler scheduler;
        private readonly TimeZoneInfo timezone;

        public List<string> AllJobs
        {
            get
            {
                var ret = new List<string>();
                foreach (var se in scheduler.GetJobKeys(Quartz.Impl.Matchers.GroupMatcher<JobKey>.GroupEquals(group_name)))
                {
                    ret.Add(se.Name);
                }
                return ret;
            }
        }
        public QuartzScheduleImpl(string name, MissFirePolicy missfire, Quartz.IScheduler scheduler, TimeZoneInfo timezone)
        {
            this.group_name = name;
            this.missfire = missfire;
            this.scheduler = scheduler;
            this.timezone = timezone;
        }

        public IJobDetail GetOrCreateCornJob<T>(string name, string corn_expression, IDictionary<string, object> data_map) where T : IJob
        {
            var job = scheduler.GetJobDetail(new JobKey(name, group_name));
            if (job == null)
            {
                job = Quartz.JobBuilder.Create<ScheduleJob>()
                    .WithIdentity(name, group_name)
                    .WithDescription(typeof(T).FullName)
                    .RequestRecovery()
                    .UsingJobData(new JobDataMap(data_map))
                    .UsingJobData("_corn_expression", corn_expression)
                    .UsingJobData("_job_type", typeof(T).FullName)
                    .Build();
                var trigger = TriggerBuilder.Create()
                    .WithIdentity(name, group_name)
                    .WithCronSchedule(corn_expression, process_cron_trigger)
                    .ForJob(name, group_name)
                    .Build();
                scheduler.ScheduleJob(job, trigger);
            }
            return new QuartzJobDetail(job, typeof(T));
        }
        public IJobDetail GetJob(string name)
        {
            var job = scheduler.GetJobDetail(new JobKey(name, group_name));
            if (job == null) return null;
            return new QuartzJobDetail(job, null);
        }
        public void RemoveJob(string name)
        {
            scheduler.DeleteJob(new JobKey(name, group_name));
        }
        public void Start()
        {
            scheduler.Start();
        }
        public void Shutdown()
        {
            scheduler.Shutdown();
        }

        private void process_cron_trigger(CronScheduleBuilder x)
        {
            switch (missfire)
            {
                case MissFirePolicy.FireHistory:
                    x.WithMisfireHandlingInstructionIgnoreMisfires();
                    break;
                case MissFirePolicy.FireOnceNow:
                    x.WithMisfireHandlingInstructionFireAndProceed();
                    break;
                case MissFirePolicy.DoNothing:
                default:
                    x.WithMisfireHandlingInstructionDoNothing();
                    break;
            }
            x.InTimeZone(timezone);
        }
    }

    public class ScheduleJob : Quartz.IJob
    {
        private IJob exe_job;
        private Type exe_type;
        private Logger log = LoggerFactory.GetLogger("CornJob");
        public void Execute(IJobExecutionContext context)
        {
            try
            {
                if (exe_job == null)
                {
                    string typeName = context.JobDetail.Description;
                    exe_type = ReflectionUtil.GetType(typeName);
                    exe_job = ReflectionUtil.CreateInterface<IJob>(typeName);
                }
                exe_job.Execute(new QuartzJobExecutionContext(context, exe_type, log));
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }
    }
    public struct QuartzJobDetail : DeepCrystal.Schedule.IJobDetail
{
        public readonly Quartz.IJobDetail job;
        public readonly Type job_type;
        public readonly string corn_expression;
        public QuartzJobDetail(Quartz.IJobDetail job, Type type)
        {
            this.job = job;
            if (type != null)
                this.job_type = type;
            else
                this.job_type = ReflectionUtil.GetType(job.Description);
            this.corn_expression = job.JobDataMap.GetString("_corn_expression");
        }
        public string Name { get { return job.Key.Name; } }
        public string GroupName { get { return job.Key.Group; } }
        public Type JobType { get { return job_type; } }
        public IDictionary<string, object> JobDataMap { get { return job.JobDataMap; } }
        public string CornExpression { get { return corn_expression; } }
    }
    public struct QuartzJobExecutionContext : IJobExeContext
    {
        public readonly IJobExecutionContext context;
        public readonly Type job_type;
        public readonly Logger log;
        public QuartzJobExecutionContext(IJobExecutionContext ctx, Type jobType, Logger log)
        {
            this.context = ctx;
            this.job_type = jobType;
            this.log = log;
        }
        public string Name { get { return context.JobDetail.Key.Name; } }
        public Type JobType { get { return job_type; } }
        public Logger Log { get { return log; } }
        public IDictionary<string, object> JobDataMap { get { return context.MergedJobDataMap; } }
        public TimeSpan JobRunTime { get { return context.JobRunTime; } }
        public DateTimeOffset? FireTimeUtc { get { return context.FireTimeUtc; } }
        public DateTimeOffset? ScheduledFireTimeUtc { get { return context.ScheduledFireTimeUtc; } }
        public DateTimeOffset? NextFireTimeUtc { get { return context.NextFireTimeUtc; } }
        public DateTimeOffset? PreviousFireTimeUtc { get { return context.PreviousFireTimeUtc; } }

    }
}
