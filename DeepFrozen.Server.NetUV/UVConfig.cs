using System;
using System.Collections.Generic;
using System.Text;

namespace DeepFrozen.Server.NetUV
{
    public class UVConfig
    {
        public string Name;
        public bool NoDelay= false;
        public bool KeepAlive = false;
        public int KeepAliveInterval = 30000;
        public int BackLog = 128;
        public bool DualStack  = false;
        public bool SimultaneousAccepts = true;
        public int MaxConnections = 0;
        public int MaxRequestLength  = 4 * 1024 * 1024;
        public int RecvBufferSize = 16384;
        public int SendBufferSize  = 16384;
        public int Port = 19000;
    }
}
