using DeepCore;
using DeepCore.Reflection;
using DeepCrystal.Command;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DeepFrozen.RPC.Command
{
    //-------------------------------------------------------------------------------------------------------------------

    public class RPCServerConsoleCommandList : ServerConsoleCommandList
    {
        [Desc("Show Rpc Statistics Info")]
        public class CMD_ST : AbstractCommand
        {
            private Type etype = typeof(DeepFrozen.RPC.Invoker.RpcStatistics.SortField);
            public override string Key { get { return "st"; } }
            public override string Help
            {
                get
                {
                    return "st <sort(" + CUtils.ListToString(Enum.GetNames(etype)) + ")>";
                }
            }
            public override void DoCommand(string arg, TextWriter output)
            {
                var sort = DeepFrozen.RPC.Invoker.RpcStatistics.SortField.MAX;
                if (CUtils.TryParseEnum(etype, arg, true, out var sortobj))
                {
                    sort = (DeepFrozen.RPC.Invoker.RpcStatistics.SortField)sortobj;
                }
                DeepFrozen.RPC.Invoker.RpcStatistics.PrintStatus(output, sort, " ", 64, 150);
            }
        }


    }
}
