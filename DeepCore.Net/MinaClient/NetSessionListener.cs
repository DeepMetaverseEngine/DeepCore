using System;

namespace DeepCore.MinaClient
{
    public interface IMinaClientSessionListener
    {
        void OnSessionOpened(IMinaClientSession session);

        void OnSessionClosed(IMinaClientSession session);

        void OnMessageReceived(IMinaClientSession session, Object data);

        void OnMessageSent(IMinaClientSession session, Object data);

        void OnError(IMinaClientSession session, Exception err);
    }
}
