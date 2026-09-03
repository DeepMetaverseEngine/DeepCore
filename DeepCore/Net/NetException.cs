using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Net
{
    public class NetException : Exception
    {
        public NetException(string message)
            : base(message)
        {

        }
        public NetException(string message, Exception err)
            : base(message, err)
        {

        }
    }
}
