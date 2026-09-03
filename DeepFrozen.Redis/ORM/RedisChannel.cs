using DeepCore;
using DeepCore.Log;
using DeepCore.Threading;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RChannel = StackExchange.Redis.RedisChannel;

namespace DeepCrystal.ORM.Redis
{
    public class RedisChannel : ORMObject, IChannel
    {
        /*
        Take the following:
          var x =  new Action(() => { Console.Write("") ; });
          var y = new Action(() => { });
          var a = x.GetHashCode();
          var b = y.GetHashCode();
          Console.WriteLine(a == b); // True
          Console.WriteLine(x == y); // False
        */
        private readonly Logger log;
        private readonly ISubscriber subscriber;
        private readonly ITaskExecutor executor;
        private readonly string key_prefix;
        private readonly HashMap<RChannel, List<ActionPair>> channels;
        public string Channel => key_prefix;

        internal RedisChannel(string key, ISubscriber subscriber, ITaskExecutor exe)
        {
            this.key_prefix = key;
            this.subscriber = subscriber;
            this.executor = exe ?? ITaskExecutor.Default;
            this.log = LoggerFactory.GetLogger("RedisChannel:" + key);
            this.channels = new HashMap<RChannel, List<ActionPair>>();
        }
        protected List<KeyValuePair<RChannel, Action<RChannel, RedisValue>>> GetRelease()
        {
            var RELEASE = new List<KeyValuePair<RChannel, Action<RChannel, RedisValue>>>();
            lock (channels)
            {
                foreach (var ch in channels)
                {
                    foreach (var action in ch.Value)
                    {
                        RELEASE.Add(new KeyValuePair<RChannel, Action<RChannel, RedisValue>>(ch.Key, action.action));
                    }
                    ch.Value.Clear();
                }
                channels.Clear();
            }
            return RELEASE;
        }
        protected override void Disposing()
        {
            var release = GetRelease();
            foreach (var ch in release)
            {
                try
                {
                    subscriber.Unsubscribe(ch.Key, ch.Value);
                }
                catch (Exception err)
                {
                    log.Error(err);
                }
            }
        }
        protected override async ValueTask DisposingAsync()
        {
            var release = GetRelease();
            foreach (var ch in release)
            {
                try
                {
                    await subscriber.UnsubscribeAsync(ch.Key, ch.Value);
                }
                catch (Exception err)
                {
                    log.Error(err);
                }
            }
        }
        private void AddObserver(RChannel field, Delegate observer, Action<RChannel, RedisValue> action)
        {
            lock (channels)
            {
                var alist = channels.GetOrAdd(field, static _ => new List<ActionPair>());
                alist.Add(new ActionPair(observer, action));
            }
        }
        private Action<RChannel, RedisValue> RemoveObserver(RChannel field, Delegate observer)
        {
            lock (channels)
            {
                if (channels.TryGetValue(field, out var ch))
                {
                    var index = ch.FindIndex((a) => a.key == observer);
                    if (index >= 0)
                    {
                        var action = ch[index];
                        ch.RemoveAt(index);
                        return action.action;
                    }
                }
            }
            throw new Exception($"Channel Not Exist : {field} : {observer}");
        }
        class ActionPair
        {
            public readonly Delegate key;
            public readonly Action<RChannel, RedisValue> action;
            public ActionPair(Delegate k, Action<RChannel, RedisValue> v)
            {
                this.key = k;
                this.action = v;
            }
        }
        //-------------------------------------------------------------------------------------------------------------
        #region Async

        public Task PublishAsync(string fieldName, object message)
        {
            var channel = RChannel.Literal(key_prefix + ":" + fieldName);
            return executor.Execute(subscriber.PublishAsync(channel, RedisConverters.ToRedisValue(message)));
        }

        public Task SubscribeAsync<V>(string fieldName, ChannelObserver<V> observer)
        {
            var channel = RChannel.Literal(key_prefix + ":" + fieldName);
            var action = new Action<RChannel, RedisValue>((ch, msg) =>
            {
                executor.Execute(() => observer(ch, ORMFactory.Instance.DecodeObject<V>(msg)));
            });
            AddObserver(channel, observer, action);
            return executor.Execute(subscriber.SubscribeAsync(channel, action));
        }
        public Task UnsubscribeAsync<V>(string fieldName, ChannelObserver<V> observer)
        {
            var channel = RChannel.Literal(key_prefix + ":" + fieldName);
            var action = RemoveObserver(channel, observer);
            return executor.Execute(subscriber.UnsubscribeAsync(channel, action));
        }
        public Task SubscribePatternAsync(string pattern, ChannelObserver observer)
        {
            var channel = RChannel.Literal(key_prefix + ":" + pattern);
            var action = new Action<RChannel, RedisValue>((ch, msg) =>
            {
                executor.Execute(() => observer(ch, msg));
            });
            AddObserver(channel, observer, action);
            return executor.Execute(subscriber.SubscribeAsync(channel, action));
        }
        public Task UnsubscribePatternAsync(string pattern, ChannelObserver observer)
        {
            var channel = key_prefix + ":" + pattern;
            var action = RemoveObserver(channel, observer);
            return executor.Execute(subscriber.UnsubscribeAsync(channel, action));
        }
        public Task SubscribeAsync<V>(string fieldName, ChannelObserverAsync<V> observer)
        {
            var channel = RChannel.Literal(key_prefix + ":" + fieldName);
            var action = new Action<RChannel, RedisValue>((ch, msg) =>
            {
                executor.Execute(() => observer(ch, ORMFactory.Instance.DecodeObject<V>(msg)));
            });
            AddObserver(channel, observer, action);
            return executor.Execute(subscriber.SubscribeAsync(channel, action));
        }
        public Task UnsubscribeAsync<V>(string fieldName, ChannelObserverAsync<V> observer)
        {
            var channel = RChannel.Literal(key_prefix + ":" + fieldName);
            var action = RemoveObserver(channel, observer);
            return executor.Execute(subscriber.UnsubscribeAsync(channel, action));
        }
        public Task SubscribePatternAsync(string pattern, ChannelObserverAsync observer)
        {
            var channel = RChannel.Literal(key_prefix + ":" + pattern);
            var action = new Action<RChannel, RedisValue>((ch, msg) =>
            {
                executor.Execute(() => observer(ch, msg));
            });
            AddObserver(channel, observer, action);
            return executor.Execute(subscriber.SubscribeAsync(channel, action));
        }
        public Task UnsubscribePatternAsync(string pattern, ChannelObserverAsync observer)
        {
            var channel = RChannel.Literal(key_prefix + ":" + pattern);
            var action = RemoveObserver(channel, observer);
            return executor.Execute(subscriber.UnsubscribeAsync(channel, action));
        }
        #endregion
//-------------------------------------------------------------------------------------------------------------
#if ORM_SYNC
        #region Sync
        public void Publish(string fieldName, object message)
        {
            var channel = RChannel.Literal(key_prefix + ":" + fieldName);
            subscriber.Publish(channel, RedisConverters.ToRedisValue(message));
        }
        public void Subscribe<V>(string fieldName, ChannelObserver<V> observer)
        {
            var channel = RChannel.Literal(key_prefix + ":" + fieldName);
            var action = new Action<RChannel, RedisValue>((ch, msg) =>
            {
                executor.Execute(() => observer(ch, ORMFactory.Instance.DecodeObject<V>(msg)));
            });
            AddObserver(channel, observer, action);
            subscriber.Subscribe(channel, action);
        }
        public void Unsubscribe<V>(string fieldName, ChannelObserver<V> observer)
        {
            var channel = RChannel.Literal(key_prefix + ":" + fieldName);
            var action = RemoveObserver(channel, observer);
            subscriber.Unsubscribe(channel, action);
        }
        public void SubscribePattern(string pattern, ChannelObserver observer)
        {
            var channel = RChannel.Literal(key_prefix + ":" + pattern);
            var action = new Action<RChannel, RedisValue>((ch, msg) =>
            {
                executor.Execute(() => observer(ch, msg));
            });
            AddObserver(channel, observer, action);
            subscriber.Subscribe(channel, action);
        }
        public void UnsubscribePattern(string pattern, ChannelObserver observer)
        {
            var channel = RChannel.Literal(key_prefix + ":" + pattern);
            var action = RemoveObserver(channel, observer);
            subscriber.Unsubscribe(channel, action);
        }
        //-------------------------------------------------------------------------------------------------------------
        #endregion
#endif
        //-------------------------------------------------------------------------------------------------------------
    }


}
