using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DeepCore.ORM
{

    /// <summary>
    /// 标记对象为持久化
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class PersistTypeAttribute : System.Attribute
    {
        public string Flag { get; private set; }
        public PersistTypeAttribute(string flag = null)
        {
            this.Flag = flag;
        }
    }

    /// <summary>
    /// 存储策略
    /// </summary>
    public enum PersistStrategy
    {
        /// <summary>
        /// 缓存在内存，定时或者强制刷新时写入
        /// </summary>
        CacheInMemory = 0,
        /// <summary>
        /// 立即读取
        /// </summary>
        LoadImmediately = 1,
        /// <summary>
        /// 立即写入
        /// </summary>
        SaveImmediately = 2,
        /// <summary>
        /// 立即读取/写入
        /// </summary>
        SaveLoadImmediately = 3,
        /// <summary>
        /// 字段只读，一般创建时确定，不会改变
        /// </summary>
        Primary = 4,
    }
    /// <summary>
    /// 标记字段为持久化
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class PersistFieldAttribute : System.Attribute
    {
        public string Flag { get; private set; }
        public PersistStrategy Strategy { get; private set; }
        public PersistFieldAttribute(PersistStrategy strategy = PersistStrategy.CacheInMemory, string flag = null)
        {
            this.Flag = flag;
            this.Strategy = strategy;
        }

        public static bool IsCacheInMemory(MemberInfo field)
        {
            return IsPersistenceStrategy(field, PersistStrategy.CacheInMemory);
        }
        public static bool IsLoadImmediately(MemberInfo field)
        {
            return IsPersistenceStrategy(field, PersistStrategy.LoadImmediately);
        }
        public static bool IsSaveImmediately(MemberInfo field)
        {
            return IsPersistenceStrategy(field, PersistStrategy.SaveImmediately);
        }
        public static bool IsSaveLoadImmediately(MemberInfo field)
        {
            return IsPersistenceStrategy(field, PersistStrategy.SaveLoadImmediately);
        }
        public static bool IsPrimary(MemberInfo field)
        {
            return IsPersistenceStrategy(field, PersistStrategy.Primary);
        }
        private static bool IsPersistenceStrategy(MemberInfo field, PersistStrategy strategy)
        {
            var attr = PropertyUtil.GetAttribute<PersistFieldAttribute>(field);
            if (attr != null) return attr.Strategy == strategy;
            return false;
        }
    }

    /// <summary>
    /// 字段读取时不为空
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false)]
    public class PersistNotNullAttribute : System.Attribute
    {

    }

}
