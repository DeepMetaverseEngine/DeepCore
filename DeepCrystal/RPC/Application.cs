using DeepCore.IO;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeepCrystal.RPC
{
    public interface IRpcApplication
    {
        /// <summary>
        /// 发送Command到所有进程
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        Task<string> AppCommandAsync(string command);
        /// <summary>
        /// 收到进程间的Command
        /// </summary>
        event Func<string, Task<string>> OnAppCommandAsync;

        /// <summary>
        /// 发送进程级别广播
        /// </summary>
        /// <param name="msg"></param>
        void BroadcastAppMessage(ISerializable msg);
        /// <summary>
        /// 接收进程级别广播
        /// </summary>
        event Action<ISerializable> OnAppMessage;
    }
}
