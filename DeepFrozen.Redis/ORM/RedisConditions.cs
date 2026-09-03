using StackExchange.Redis;

namespace DeepCrystal.ORM.Redis
{
    public struct RedisCondition : ICondition
    {
        public Condition cond;
        internal RedisCondition(Condition c)
        {
            this.cond = c;
        }
    }

    public class RedisConditions : IConditions
    {
        internal RedisConditions() { }

        public ICondition HashEqual(string key, string hashField, object value)
        {
            return new RedisCondition(Condition.HashEqual(key, hashField, RedisConverters.ToRedisValue(value)));
        }
        public ICondition HashExists(string key, string hashField)
        {
            return new RedisCondition(Condition.HashExists(key, hashField));
        }
		
		public ICondition KeyExists(string key)
		{
			 return new RedisCondition(Condition.KeyExists(key));
		}
		public ICondition KeyNotExists(string key)
		{
			 return new RedisCondition(Condition.KeyNotExists(key));
		}
		
        public ICondition HashNotEqual(string key, string hashField, object value)
        {
            return new RedisCondition(Condition.HashNotEqual(key, hashField, RedisConverters.ToRedisValue(value)));
        }
        public ICondition HashNotExists(string key, string hashField)
        {
            return new RedisCondition(Condition.HashNotExists(key, hashField));
        }

        public ICondition SetLengthEqual(string key, long length)
        {
            return new RedisCondition(Condition.SetLengthEqual(key, length));
        }
        public ICondition SetLengthGreaterThan(string key, long length)
        {
            return new RedisCondition(Condition.SetLengthGreaterThan(key, length));
        }
        public ICondition SetLengthLessThan(string key, long length)
        {
            return new RedisCondition(Condition.SetLengthLessThan(key, length));
        }

        public ICondition ListIndexEqual(string key, long index, object value)
        {
            return new RedisCondition(Condition.ListIndexEqual(key, index, RedisConverters.ToRedisValue(value)));
        }
        public ICondition ListIndexExists(string key, long index)
        {
            return new RedisCondition(Condition.ListIndexExists(key, index));
        }
        public ICondition ListIndexNotEqual(string key, long index, object value)
        {
            return new RedisCondition(Condition.ListIndexNotEqual(key, index, RedisConverters.ToRedisValue(value)));
        }
        public ICondition ListIndexNotExists(string key, long index)
        {
            return new RedisCondition(Condition.ListIndexNotExists(key, index));
        }
        public ICondition ListLengthEqual(string key, long length)
        {
            return new RedisCondition(Condition.ListLengthEqual(key, length));
        }
        public ICondition ListLengthGreaterThan(string key, long length)
        {
            return new RedisCondition(Condition.ListLengthGreaterThan(key, length));
        }
        public ICondition ListLengthLessThan(string key, long length)
        {
            return new RedisCondition(Condition.ListLengthLessThan(key, length));
        }

        public ICondition SortedSetLengthEqual(string key, long length)
        {
            return new RedisCondition(Condition.SortedSetLengthEqual(key, length));
        }
        public ICondition SortedSetLengthGreaterThan(string key, long length)
        {
            return new RedisCondition(Condition.SortedSetLengthGreaterThan(key, length));
        }
        public ICondition SortedSetLengthLessThan(string key, long length)
        {
            return new RedisCondition(Condition.SortedSetLengthLessThan(key, length));
        }

        public ICondition StringEqual(string key, object value)
        {
            return new RedisCondition(Condition.StringEqual(key, RedisConverters.ToRedisValue(value)));
        }
        public ICondition StringLengthEqual(string key, long length)
        {
            return new RedisCondition(Condition.StringLengthEqual(key, length));
        }
        public ICondition StringLengthGreaterThan(string key, long length)
        {
            return new RedisCondition(Condition.StringLengthGreaterThan(key, length));
        }
        public ICondition StringLengthLessThan(string key, long length)
        {
            return new RedisCondition(Condition.StringLengthLessThan(key, length));
        }
        public ICondition StringNotEqual(string key, object value)
        {
            return new RedisCondition(Condition.StringNotEqual(key, RedisConverters.ToRedisValue(value)));
        }
    }

}
