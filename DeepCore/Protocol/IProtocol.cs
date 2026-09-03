using DeepCore.IO;
using DeepCore.Reflection;

namespace DeepCore.Protocol
{
    /// <summary>
    /// 网络序列化标识接口
    /// </summary>

    [Reflectible] public interface INetProtocol : ISerializable { }

    [Reflectible] public interface INetProtocolS2C { }

    [Reflectible] public interface INetProtocolC2S { }

    [Reflectible] public interface INetProtocolBotIgnore { }

    [Reflectible] public interface INetRequest : INetProtocol { }
    [Reflectible] public interface INetResponse : INetProtocol { }

    [Reflectible] public interface IWormholeProtocol : INetProtocol { }


    //---------------------------------------------------------------------------------------------
    /// <summary>
    /// 请求
    /// </summary>
    public abstract class Request : INetRequest
    {
        public override string ToString()
        {
            return string.Format("{0}", GetType().Name);
        }
    }

    /// <summary>
    /// 回馈
    /// </summary>
    public abstract class Response : INetResponse
    {
        [MessageCodeAttribute("成功")]
        public const int CODE_OK = 200;
        [MessageCodeAttribute("未知错误")]
        public const int CODE_ERROR = 500;
        /// <summary>
        /// 返回码
        /// </summary>
        public int s2c_code = CODE_OK;
        /// <summary>
        /// 返回信息（优先网络消息，如果网络消息为空，则从MessageCode中找）
        /// </summary>
        public string s2c_msg;
        /// <summary>
        /// 内部消息，用于一个系统的反馈，传递给原始请求
        /// </summary>
        public Response InnerResponse;

        /// <summary>
        /// 请求是否成功
        /// </summary>
        [DependOnProperty(nameof(s2c_code))]
        public bool IsSuccess
        {
            get { return s2c_code >= 200 && s2c_code <= 299; }
        }

        public override string ToString()
        {
            return string.Format("{0}: {1} : {2}", GetType().Name, s2c_code, s2c_msg);
        }
        public static bool CheckSuccess(Response rsp)
        {
            return (rsp != null && rsp.IsSuccess);
        }
        public virtual void EndRead()
        {
            if (s2c_msg == null)
            {
                if (s2c_code == CODE_OK || s2c_code == CODE_ERROR)
                {
                    if (InnerResponse != null)
                    {
                        InnerResponse.EndRead();
                        s2c_msg = InnerResponse.s2c_msg;
                    }
                }
                if (s2c_msg == null)
                {
                    if (MessageCodeManager.Instance != null)
                    {
                        s2c_msg = MessageCodeManager.Instance.GetCodeMessage(this);
                    }
                }
                if (s2c_msg == null && InnerResponse != null)
                {
                    s2c_msg = InnerResponse.s2c_msg;
                }
            }
        }
    }

    /// <summary>
    /// 单向通知
    /// </summary>
    public abstract class Notify : INetProtocol
    {
    }

    //---------------------------------------------------------------------------------------------

    //---------------------------------------------------------------------------------------------
}
