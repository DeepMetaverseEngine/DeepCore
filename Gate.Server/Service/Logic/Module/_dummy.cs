using DeepCrystal.ORM;
using DeepCrystal.RPC;
using Gate.Server.Protocol;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Gate.Server.Service.Logic.Module
{
    public class _dummy : IServiceModule<LogicService>, ILogicModule
    {
        public _dummy(LogicService service) : base(service)
        {
        }
        public Task OnClientEnterGameAsync()
        {
            return Task.CompletedTask;
        }
        public void OnSaveData(IObjectTransaction trans)
        {
        }
        public Task OnSessionDisconnectAsync(SessionDisconnectNotify notify)
        {
            return Task.CompletedTask;
        }
        public Task OnSessionReconnectAsync(SessionReconnectNotify notify)
        {
            return Task.CompletedTask;
        }
    }
}
