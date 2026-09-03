using System;
using System.Collections.Generic;
using System.Linq;

namespace DeepCore.Event.EventSystem
{
    public interface IMessagePayload
    {
        object Who { get; }
        DateTime When { get; }
        object WhatObject { get; }
    }

    public class MessagePayload<T> : IMessagePayload
    {
        public object Who { get; }
        public T What { get; }
        public DateTime When { get; }
        public object WhatObject { get; }

        public MessagePayload(T payload, object source)
        {
            Who = source;
            What = payload;
            When = DateTime.UtcNow;
            WhatObject = What;
        }

        public override string ToString()
        {
            return $"{Who}:{What} --{When}";
        }
    }

    public interface IMessageChannel : IDisposable
    {
        void Publish<T>(object source, T message);
        void Subscribe<T>(Action<MessagePayload<T>> subscription);
        void Unsubscribe<T>(Action<MessagePayload<T>> subscription);
        bool Enabled { get; set; }
    }

    public interface IMessageBroker : IDisposable
    {
        void CloseChannel(string name);
        IMessageChannel CreateChannel(string name);
        bool IsExistChannel(string name);
        bool Publish<T>(string name, object source, T message);
        void Subscribe<T>(string name, Action<MessagePayload<T>> subscription);
        void Unsubscribe<T>(string name, Action<MessagePayload<T>> subscription);
        void Subscribe(string name, Action<IMessagePayload> subscription);
        void Unsubscribe(string name, Action<IMessagePayload> subscription);
    }


    public class MessageChannel : IMessageChannel
    {
        private readonly HashMap<Type, List<Delegate>> mSubscribers = new HashMap<Type, List<Delegate>>();
        private readonly List<Delegate> mGlobalSubscribers = new List<Delegate>();
        public readonly string Name;

        public MessageChannel(string name)
        {
            Name = name;
            Enabled = true;
        }

        public void Dispose()
        {
            mSubscribers.Clear();
            mGlobalSubscribers.Clear();
        }

        public void Publish<T>(object source, T message)
        {
            if (!Enabled || message == null || source == null)
            {
                return;
            }

            var payload = new MessagePayload<T>(message, source);
            //var all = new List<Action<IMessagePayload>>();
            using (var all = CollectionObjectPool<Action<IMessagePayload>>.AllocList())
            {
                lock (mGlobalSubscribers)
                {
                    foreach (var t in mGlobalSubscribers)
                    {
                        all.Add((Action<IMessagePayload>)t);
                    }
                }

                lock (mSubscribers)
                {
                    var delegates = mSubscribers.Get(typeof(T));
                    if (delegates != null)
                    {
                        foreach (var t in delegates)
                        {
                            all.Add((Action<IMessagePayload>)t);
                        }
                    }
                }

                foreach (var action in all)
                {
                    action.Invoke(payload);
                }
            }
        }

        public void Subscribe<T>(Action<MessagePayload<T>> subscription)
        {
            lock (mSubscribers)
            {
                var delegates = mSubscribers.GetOrAdd(typeof(T), CreateActionFactory);
                delegates.Add(subscription);
            }
        }

        private List<Delegate> CreateActionFactory(Type type)
        {
            return new List<Delegate>();
        }

        public void Unsubscribe<T>(Action<MessagePayload<T>> subscription)
        {
            lock (mSubscribers)
            {
                var delegates = mSubscribers.Get(typeof(T));
                delegates.Remove(subscription);
            }
        }

        public void Subscribe(Action<IMessagePayload> subscription)
        {
            lock (mGlobalSubscribers)
            {
                mGlobalSubscribers.Add(subscription);
            }
        }


        public void Unsubscribe(Action<IMessagePayload> subscription)
        {
            lock (mGlobalSubscribers)
            {
                mGlobalSubscribers.Remove(subscription);
            }
        }

        public bool Enabled { get; set; }
    }


    public sealed class MessageBroker : IMessageBroker
    {
        private readonly SafeDictionary<string, MessageChannel> mChannels = new SafeDictionary<string, MessageChannel>();

        public const string UnknownChannel = nameof(UnknownChannel);

        private readonly MessageChannel mUnknownChannel;

        public void Dispose()
        {
            using (var p = mChannels.LockWrite())
            {
                foreach (var entry in p.Data)
                {
                    entry.Value.Dispose();
                }
            }

            mChannels.Dispose();
        }

        public MessageBroker()
        {
            mUnknownChannel = new MessageChannel(UnknownChannel);
        }

        private MessageChannel GetChannel(string name)
        {
            return mChannels.TryGetValue(name, out var channel) ? channel : mUnknownChannel;
        }

        public IMessageChannel CreateChannel(string name)
        {
            return mChannels.GetOrAdd(name, CreateMessageChannel);
        }

        private static MessageChannel CreateMessageChannel(string s)
        {
            return new MessageChannel(s);
        }

        public bool Publish<T>(string name, object source, T message)
        {
            var channel = GetChannel(name);
            if (channel == null)
            {
                mUnknownChannel.Publish(source, message);
                return false;
            }

            channel.Publish(source, message);
            return true;
        }

        public bool IsExistChannel(string name)
        {
            return mChannels.ContainsKey(name);
        }

        public void CloseChannel(string name)
        {
            var channel = mChannels.RemoveByKey(name);
            channel?.Dispose();
        }

        public void Subscribe<T>(string name, Action<MessagePayload<T>> subscription)
        {
            var channel = GetChannel(name);
            channel?.Subscribe(subscription);
        }

        public void Unsubscribe<T>(string name, Action<MessagePayload<T>> subscription)
        {
            var channel = GetChannel(name);
            channel?.Unsubscribe(subscription);
        }

        public void Subscribe(string name, Action<IMessagePayload> subscription)
        {
            var channel = GetChannel(name);
            channel?.Subscribe(subscription);
        }

        public void Unsubscribe(string name, Action<IMessagePayload> subscription)
        {
            var channel = GetChannel(name);
            channel?.Unsubscribe(subscription);
        }
    }
}