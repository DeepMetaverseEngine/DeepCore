using DeepCore.Reflection;
using System;
using System.Collections.Generic;

namespace DeepCrystal.Schedule
{
    public abstract class ScheduleFactory
    {
        #region Singleton
        private static ScheduleFactory s_instance;
        public static ScheduleFactory Instance { get { return s_instance; } }
        public static ScheduleFactory SetFactory(Type type)
        {
            return Activator.CreateInstance(type) as ScheduleFactory;
        }
        public static ScheduleFactory SetFactory(string fullName)
        {
            return Activator.CreateInstance(ReflectionUtil.GetType(fullName)) as ScheduleFactory;
        }
        #endregion

        public ScheduleFactory() { s_instance = this; }

        public abstract ISchedule GetScheduler(string group, MissFirePolicy missfire = MissFirePolicy.DoNothing);

    }

    public enum MissFirePolicy
    {
        DoNothing,
        FireOnceNow,
        FireHistory,
    }

    public interface ISchedule
    {
        List<string> AllJobs { get; }

        IJobDetail GetOrCreateCornJob<T>(string name, string corn_expression, IDictionary<string, object> data_map = null) where T : IJob;

        IJobDetail GetJob(string name);

        void RemoveJob(string name);

        void Start();

        void Shutdown();
    }

    public interface IJobDetail
    {
        string Name { get; }
        string GroupName { get; }
        string CornExpression { get; }
        Type JobType { get; }
        IDictionary<string, object> JobDataMap { get; }
    }

    public interface IJobExeContext
    {

        string Name { get; }
        Type JobType { get; }
        DeepCore.Log.Logger Log { get; }
        IDictionary<string, object> JobDataMap { get; }

        TimeSpan JobRunTime { get; }

        /// <summary>
        /// 本次：实际执行时间
        /// </summary>
        DateTimeOffset? FireTimeUtc { get; }
        /// <summary>
        /// 本次：计划执行时间
        /// </summary>
        DateTimeOffset? ScheduledFireTimeUtc { get; }
        /// <summary>
        /// 计划下次执行时间
        /// </summary>
        DateTimeOffset? NextFireTimeUtc { get; }
        /// <summary>
        /// 计划上次执行时间
        /// </summary>
        DateTimeOffset? PreviousFireTimeUtc { get; }
    }

    public interface IJob
    {
        void Execute(IJobExeContext state);
    }

}
