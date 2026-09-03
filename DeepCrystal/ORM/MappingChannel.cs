using System;
using System.Threading.Tasks;

namespace DeepCrystal.ORM
{

    /// <summary>
    /// 订阅频道
    /// </summary>
    public interface IChannel : IAsyncDisposable, IDisposable
    {
        string Channel { get; }

        Task PublishAsync(string fieldName, object message);
        Task SubscribeAsync<V>(string fieldName, ChannelObserver<V> observer);
        Task UnsubscribeAsync<V>(string fieldName, ChannelObserver<V> observer);
        Task SubscribePatternAsync(string pattern, ChannelObserver observer);
        Task UnsubscribePatternAsync(string pattern, ChannelObserver observer);
        Task SubscribeAsync<V>(string fieldName, ChannelObserverAsync<V> observer);
        Task UnsubscribeAsync<V>(string fieldName, ChannelObserverAsync<V> observer);
        Task SubscribePatternAsync(string pattern, ChannelObserverAsync observer);
        Task UnsubscribePatternAsync(string pattern, ChannelObserverAsync observer);
#if ORM_SYNC
        void Publish(string fieldName, object message);
        void Subscribe<V>(string fieldName, ChannelObserver<V> observer);
        void Unsubscribe<V>(string fieldName, ChannelObserver<V> observer);
        void SubscribePattern(string pattern, ChannelObserver observer);
        void UnsubscribePattern(string pattern, ChannelObserver observer);
#endif
    }

    public delegate void ChannelObserver<V>(string fieldName, V message);
    public delegate void ChannelObserver(string fieldName, IConvertible message);
    public delegate Task ChannelObserverAsync<V>(string fieldName, V message);
    public delegate Task ChannelObserverAsync(string fieldName, IConvertible message);

}
