using DeepCore.Log;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DeepFrozen.RPC.Remote
{
    public class RpcException : Exception
    {
        private static Logger log = new LazyLogger(nameof(RpcException));
        private string innerTrace;
        public override string StackTrace
        {
            get
            {
                if (innerTrace != null)
                {
                    if (!string.IsNullOrEmpty(base.StackTrace))
                    {
                        return base.StackTrace.TrimEnd() + (Environment.NewLine + "From: " + Environment.NewLine + innerTrace.ToString());
                    }
                    else
                    {
                        return "From: " + Environment.NewLine + innerTrace.ToString();
                    }
                }
                return base.StackTrace;
            }
        }
        public RpcException(string message, string trace) : base(message)
        {
            this.innerTrace = trace;
            //log.Error(this);
        }
        public RpcException(Exception err, StackTrace trace) : base(err.Message, err)
        {
            this.innerTrace = trace?.ToString();
            //log.Error(this);
        }
        public RpcException(Exception err, string trace) : base(err.Message, err)
        {
            this.innerTrace = trace;
            //log.Error(this);
        }
    }
}
