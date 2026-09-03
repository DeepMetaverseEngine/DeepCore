using DeepCrystal.ORM;
using Gate.Server.Protocol;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Gate.Server.Service.Logic
{
    public interface ILogicModule
    {
        void OnSaveData(IObjectTransaction trans);
        Task OnClientEnterGameAsync();
        Task OnSessionReconnectAsync(SessionReconnectNotify notify);
        Task OnSessionDisconnectAsync(SessionDisconnectNotify notify);
    }
}
